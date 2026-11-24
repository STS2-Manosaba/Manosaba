"""澤渡可可 簡化戰鬥蒙地卡羅模擬。
規則依 Manosaba/Characters/SawatariCoco 原始碼；敵人為抽象化（固定戰鬥長度、排程死亡、攻擊意圖曲線）。
價值換算：6 傷 = 1 能量、5 點避免失血 = 1 能量。
"""
from __future__ import annotations
import random, math, json, sys
from collections import defaultdict

DMG_PER_E = 6.0
HP_PER_E = 5.0

# 戰鬥：(類型, 平均回合, gamma 形狀, 重要性, 敵人數分佈, 每敵攻擊基礎傷害)
ENC = {
    "hallway": (4.0, 4.0, 1.0, [(1, .35), (2, .40), (3, .25)], 7.0),
    "elite":   (6.5, 4.0, 2.0, [(1, .6), (2, .3), (3, .1)], 13.0),
    "boss":    (10.0, 5.0, 3.0, [(1, .8), (2, .2)], 17.0),
}
ACT_SCALE = [1.0, 1.35, 1.7]
RUN = []  # 每幕 10 一般、2 菁英、1 首領
for act in range(3):
    seq = ["hallway"] * 10
    seq.insert(4, "elite"); seq.insert(9, "elite")
    seq.append("boss")
    RUN += [(act, k) for k in seq]
EXP_GAIN = {"hallway": 1, "elite": 2, "boss": 3}
LV_TABLE = [0, 2, 7, 15, 30]


def relic_level(exp):
    lv = 1
    for i, t in enumerate(LV_TABLE):
        if exp >= t:
            lv = i + 1
    return lv


def majoka_mult(s):
    if s <= 0:
        return 1.0
    k = 166.6 ** 1.8365
    p = s ** 1.8365
    return 1 + 3 * p / (p + k)


# 牌的靜態「替代價值」（LucidDream 選牌、SlayTheSpireII 保留用）
STATIC_V = defaultdict(lambda: 1.0, {
    "FlameBait": 3.5, "ShowerStream": 4.0, "PunkCatTop": 3.0, "InmuKing": 6.0, "CybercatTop": 4.3,
    "SharpTongue": 3.2, "OnAir": 2.0, "PhoneLiveStream": 2.2, "ForMyOshi": 2.7, "CybercatShoes": 2.4,
    "PunkCatSkirt": 2.4, "PunkCatShoes": 2.1, "TalkTrash": 2.2, "MysteriousCatPants": 3.0,
    "CybercatSkirt": 2.25, "Strike": 1.0, "Defend": 1.0, "Trauma": 0.3, "TreeHollow": 5.0,
    "Clairvoyance": 6.0, "CutieCatsShoes": 3.0, "MysteriousCatHeadwear": 1.6, "MysteriousCatTop": 1.6,
})


class Card:
    __slots__ = ("name", "token", "replay")
    def __init__(self, name, token=False):
        self.name = name; self.token = token; self.replay = 0


class Enemy:
    __slots__ = ("alive", "fan", "weak", "vuln", "poison", "intent", "stun", "dies_at", "dmg")
    def __init__(self, dies_at, dmg):
        self.alive = True; self.fan = False; self.weak = 0; self.vuln = 0; self.poison = 0
        self.intent = "attack"; self.stun = False; self.dies_at = dies_at; self.dmg = dmg


class Combat:
    def __init__(self, sim, deck, kind, act, rl, rng):
        self.sim = sim; self.rng = rng; self.kind = kind; self.act = act; self.rl = rl
        mean, shape, imp, ndist, base = ENC[kind]
        self.length = max(2, round(rng.gammavariate(shape, mean / shape)))
        n = rng.choices([a for a, _ in ndist], [w for _, w in ndist])[0]
        dmg = base * ACT_SCALE[act] / (1 + 0.35 * (n - 1))
        # 敵人依序死亡，最後一隻在戰鬥最後一回合死亡
        self.enemies = [Enemy(max(1, round(self.length * (i + 1) / n)), dmg) for i in range(n)]
        self.imp = imp
        self.draw = [Card(c.name) for c in deck]; rng.shuffle(self.draw)
        self.hand: list[Card] = []; self.discard: list[Card] = []; self.exhausted = 0
        self.block = 0; self.str = 0; self.dex = 0; self.majoka = 0; self.intang = 0
        self.hiding = rl; self.live = 3 if rl >= 2 else 0
        self.energy = 0; self.maxe = 3; self.turn = 0
        self.powers = defaultdict(int)  # FanService/OnlineShopping/IWTPAG/FourDPocket/clair
        self.fs_cd = 0; self.fs_dup = 3
        self.slots = {}  # slot -> (series, score)
        self.set_done = set(); self.score_ms = set(); self.highscore = 0
        self.dealt = 0.0; self.hp_lost = 0.0; self.takeover = 0
        self.clair_given = False
        # innate
        inn = [c for c in self.draw if c.name in sim.innate]
        for c in inn:
            self.draw.remove(c); self.draw.append(c)

    # ---------- 工具
    def alive(self):
        return [e for e in self.enemies if e.alive]

    def in_live(self):
        return self.live >= 1

    def deal(self, dmg, target, powered=True, hits=1):
        if target is None or not target.alive:
            return
        for _ in range(hits):
            d = dmg + (self.str if powered else 0)
            if powered:
                d *= majoka_mult(self.majoka)
            if target.vuln > 0:
                d *= 1.5
            d = max(0, int(d))
            self.dealt += d
            if d > 0 and self.in_live() and not target.fan:
                target.fan = True; self.sim.total_fans += 1; self.sim.fans_made += 1

    def deal_all(self, dmg, powered=True):
        for e in self.alive():
            self.deal(dmg, e, powered)

    def gain_block(self, b, powered=True):
        self.block += max(0, b + (self.dex if powered else 0))

    def target(self):
        a = self.alive()
        return a[0] if a else None

    def draw_n(self, n):
        out = []
        for _ in range(n):
            if not self.draw:
                if not self.discard:
                    break
                self.draw = self.discard; self.discard = []; self.rng.shuffle(self.draw)
            if len(self.hand) >= 10:
                break
            c = self.draw.pop(); self.hand.append(c); out.append(c)
        return out

    def add_majoka(self, n):
        self.majoka += n
        if self.majoka >= 100 and not self.clair_given and self.sim.name != "nomajoka":
            self.clair_given = True
            if "Clairvoyance" not in self.sim.perm_added:
                self.sim.perm_added.append("Clairvoyance")
            if len(self.hand) < 10:
                self.hand.append(Card("Clairvoyance", token=True))
            self.sim.stat("_majoka100", 1)

    def incoming(self):
        tot = 0
        for e in self.alive():
            if e.intent == "attack" and not e.stun:
                d = e.dmg * (0.75 if e.weak else 1) - (1 if e.fan else 0)
                tot += max(0, d) if not self.intang else 1
        return tot

    # ---------- 裝備
    def equip(self, slot, series, score):
        cur = self.slots.get(slot)
        if cur and cur[0] != series:
            pass  # 選擇換裝：一律換
        self.slots[slot] = (series, score)
        self.after_equip(series)
        self.pocket()

    def after_equip(self, series):
        tot = sum(s for _, s in self.slots.values())
        for th, eff in ((3000, "dex"), (6000, "str"), (10000, "maj")):
            if tot >= th and th not in self.score_ms:
                self.score_ms.add(th)
                self.sim.stat("_milestone%d" % th, 1)
                if eff == "dex": self.dex += 1
                elif eff == "str": self.str += 1
                else: self.add_majoka(20)
        if len(self.slots) == 4 and len({s for s, _ in self.slots.values()}) == 1 and series not in self.set_done:
            self.set_done.add(series)
            self.sim.stat("_set_" + series, 1)
            if series == "Punk": self.str += 5
            elif series == "Cyber": self.dex += 5
            elif series == "Myst": self.maxe += 2
            elif series == "Cutie":
                for e in self.alive():
                    e.poison += 20  # 10 中毒 + 10 灼燒（近似合併）

    def pocket(self):
        st = self.powers["Pocket"]
        if st <= 0:
            return
        for series in ("Punk", "Cyber", "Myst", "Cutie"):
            have = [sl for sl, (s, _) in self.slots.items() if s == series]
            missing = 4 - len(have)
            if have and 1 <= missing <= st:
                self.powers["Pocket"] -= missing
                self.sim.stat("_pocket_fill", 1)
                self.sim.stat("_pocket_fill_pieces", missing)
                for sl in ("Head", "Top", "Skirt", "Shoes"):
                    if sl not in have:
                        self.slots[sl] = (series, EQUIP_SCORE[(series, sl)])
                self.after_equip(series)
                return

    # ---------- 回合
    def run(self):
        while self.turn < self.length and self.alive():
            self.turn += 1
            self.start_turn()
            self.play_phase()
            self.end_turn()
            self.enemy_turn()
        return self

    def start_turn(self):
        self.block = 0
        for e in self.alive():
            r = self.rng.random()
            e.intent = "attack" if r < 0.65 else ("debuff" if r < 0.8 else "buff")
            e.stun = False
        self.energy = self.maxe
        if self.hiding > 0:
            self.gain_block(self.hiding, False); self.add_majoka(self.hiding)
        if self.rl >= 4 and self.in_live():
            self.energy += 1; self.sim.stat("_lv4_energy", 1)
        if self.fs_cd > 0:
            self.fs_cd -= 1
        if self.powers["Shop"] > 0:
            for _ in range(self.powers["Shop"]):
                self.draw.insert(self.rng.randrange(len(self.draw) + 1), Card("NiceOutfit", True))
        if self.powers["Clair"] > 0:
            self.clair_tick()
        self.draw_n(5)
        if self.powers["TakeoverPending"]:
            self.powers["TakeoverPending"] = 0
            if self.majoka >= 150:
                self.takeover += 1; self.sim.stat("_takeover", 1)
        self.sim.stat("_turns", 1)
        self.sim.stat("_turns_live", 1 if self.in_live() else 0)
        self.sim.stat("_turn_live_sum", self.live)

    def clair_tick(self):
        for e in self.alive():
            if e.intent == "attack": self.gain_block(5, False)
            elif e.intent == "debuff": e.vuln += 1
            else: self.str += 1

    def end_turn(self):
        keep = []
        for c in self.hand:
            if c.name == "TreeHollow" or c.name == "NiceOutfit":
                self.exhausted += 1
            elif c.name == "HideInWardrobe":
                keep.append(c)
            else:
                self.discard.append(c)
        self.hand = keep
        if self.live > 0:
            a = self.alive()
            if a:
                self.deal(self.live, self.rng.choice(a), powered=False)
            self.live -= 1
        if self.powers["IWTPAG"] > 0:
            self.add_majoka(30)
            if self.majoka >= 150:
                self.powers["TakeoverPending"] = 1
        if self.powers["Clair"] > 0:
            self.powers["Clair"] -= 1

    def enemy_turn(self):
        for e in self.enemies:
            if e.alive and e.poison > 0:
                self.dealt += e.poison; e.poison -= 1
        inc_total = 0
        for e in self.alive():
            if e.intent == "attack" and not e.stun:
                d = e.dmg * (0.75 if e.weak else 1) - (1 if e.fan else 0)
                d = max(0, d)
                if self.intang: d = min(d, 1)
                inc_total += d
            e.weak = max(0, e.weak - 1); e.vuln = max(0, e.vuln - 1)
        self.intang = max(0, self.intang - 1)
        lost = max(0, inc_total - self.block)
        self.hp_lost += lost
        for e in self.enemies:
            if e.alive and e.dies_at <= self.turn:
                e.alive = False

    # ---------- 出牌
    def play_phase(self):
        guard = 0
        if self.powers["FanSvc"] > 0 and self.fs_cd <= 0 and self.alive():
            use_fanservice(self)
        while guard < 40:
            guard += 1
            best = None; bs = 0.0
            for c in self.hand:
                d = CARDS[c.name]
                cost = self.cost(c)
                if cost > self.energy or not d["ok"](self):
                    continue
                s = d["prio"](self)
                if s > bs:
                    bs = s; best = c
            if best is None:
                break
            self.play(best)

    def cost(self, c):
        d = CARDS[c.name]
        return d["cost"]

    def play(self, c, free=False):
        d = CARDS[c.name]
        self.hand.remove(c)
        if not free:
            self.energy -= d["cost"]
        times = 1 + c.replay
        self.sim.on_play(self, c.name)
        for _ in range(times):
            d["fx"](self)
        if d["type"] == "power" or d.get("exhaust") or c.token:
            self.exhausted += 1
        else:
            self.discard.append(c)


# ---------------- 卡牌定義 ----------------
def need_block(s):
    return max(0, s.incoming() - s.block)


def atk_prio(base):
    return lambda s: base if s.alive() else 0


def blk_prio(base):
    return lambda s: base * (1.6 if need_block(s) > 0 else 0.35)


def F(cost, typ, fx, prio, ok=lambda s: True, **kw):
    d = dict(cost=cost, type=typ, fx=fx, prio=prio, ok=ok); d.update(kw); return d


def fx_lucid(s):
    if not s.discard:
        s.sim.stat("LucidDream:empty", 1); return
    cands = [c for c in s.discard if c.name != "LucidDream" and CARDS[c.name]["ok"](s)]
    if not cands:
        s.sim.stat("LucidDream:empty", 1); return
    c = max(cands, key=lambda c: STATIC_V[c.name])
    s.sim.stat("LucidDream:picked_v", STATIC_V[c.name]); s.sim.stat("LucidDream:picked_cost", CARDS[c.name]["cost"])
    s.discard.remove(c); s.hand.append(c); s.play(c, free=True)


def fx_mirror(s):
    s.sim.stat("InfiniteMirror:added", s.live); s.live *= 2


def fx_simul(s):
    for c in s.draw_n(1):
        c.replay += 1
        s.sim.stat("SimultaneousViewing:replay_v", STATIC_V[c.name])


def fx_tdo(s):
    h = s.hiding
    s.sim.stat("TurnDefenseToOffense:hiding", h)
    s.sim.stat("TurnDefenseToOffense:str", h // 2)
    s.sim.stat("TurnDefenseToOffense:turns_left", s.length - s.turn)
    s.hiding = 0; s.str += h // 2


def fx_fanservice(s):
    if s.powers["FanSvc"] > 0:
        s.fs_dup = max(1, s.fs_dup - 1); s.fs_cd = max(0, s.fs_cd - 1)
    s.powers["FanSvc"] = 1


def use_fanservice(s):
    # 技能：全敵粉絲化 + (TotalFanCount 格擋 或 全敵 TotalFanCount 傷害)
    for e in s.alive():
        if not e.fan:
            e.fan = True; s.sim.total_fans += 1; s.sim.fans_made += 1; s.sim.stat("FanService:fans_by_skill", 1)
    f = s.sim.total_fans
    s.sim.stat("FanService:skill_uses", 1); s.sim.stat("FanService:total_fans_at_use", f)
    nb = need_block(s)
    blk_v = min(f, nb) / HP_PER_E
    dmg_v = f * len(s.alive()) / DMG_PER_E
    if blk_v > dmg_v:
        s.gain_block(f, False); s.sim.stat("FanService:skill_v", blk_v)
    else:
        s.deal_all(f, False); s.sim.stat("FanService:skill_v", dmg_v)
    s.fs_cd = s.fs_dup


def fx_iwtpag(s):
    s.powers["IWTPAG"] = 1
    for _ in range(3):
        s.draw.insert(s.rng.randrange(len(s.draw) + 1), Card("PlayWhatGame", True))


def fx_pwg(s):
    s.powers["IWTPAG"] = 0; s.draw_n(1)


def fx_hideseek(s):
    s.add_majoka(20)
    # 選擇：若魔女化可望達 100 選找到你，否則衣櫃
    pick = "FoundYou" if s.majoka >= 60 else "HideInWardrobe"
    s.sim.stat("LetsPlayHideAndSeek:pick_found", 1 if pick == "FoundYou" else 0)
    s.draw.insert(s.rng.randrange(len(s.draw) + 1), Card(pick, True))


def fx_tree(s):
    s.gain_block(25)
    s.sim.stat("TreeHollow:majoka_before", s.majoka)
    s.sim.stat("TreeHollow:mult_gain", majoka_mult(100) - majoka_mult(s.majoka))
    s.sim.stat("TreeHollow:turns_left", s.length - s.turn)
    if s.majoka < 100: s.add_majoka(100 - s.majoka)


def fx_clair(s):
    st = min(10, s.majoka // 10)
    s.sim.stat("Clairvoyance:stacks", st)
    if st > 0:
        s.powers["Clair"] += st; s.clair_tick()


def fx_mpants(s):
    f = sum(1 for e in s.alive() if e.fan)
    s.sim.stat("MysteriousCatPants:fans", f)
    for _ in range(f):
        s.draw_n(1); s.energy += 1


def fx_mshoes(s):
    if s.in_live(): s.energy += 1; s.sim.stat("MysteriousCatShoes:live", 1)


def fx_cshoes(s):
    t = next((e for e in s.alive() if e.intent == "attack" and not e.stun), None)
    s.sim.stat("CutieCatsShoes:hit", 1 if t else 0)
    if t:
        s.sim.stat("CutieCatsShoes:prevent", max(0, t.dmg * (0.75 if t.weak else 1) - (1 if t.fan else 0)))
        t.stun = True


def fx_pchead(s):
    f = s.sim.total_fans
    s.sim.stat("PunkCatHeadwear:fans", f); s.sim.stat("PunkCatHeadwear:enemies", len(s.alive()))
    s.deal_all(f)


def fx_cchead(s):
    f = s.sim.total_fans
    s.sim.stat("CybercatHeadwear:fans", f); s.gain_block(f)


def fx_stsii(s):
    pool = s.hand + s.draw + s.discard
    s.sim.stat("SlayTheSpireIi:pile", len(pool))
    keep = sorted(pool, key=lambda c: -STATIC_V[c.name])[:8]
    s.exhausted += len(pool) - len(keep)
    s.hand = keep[:10]; s.draw = []; s.discard = []


def eq(series, slot, fx):
    def f(s):
        s.equip(slot, series, EQUIP_SCORE[(series, slot)])
        fx(s)
    return f


EQUIP_SCORE = {
    ("Punk", "Head"): 2000, ("Punk", "Top"): 5000, ("Punk", "Skirt"): 2000, ("Punk", "Shoes"): 1000,
    ("Cyber", "Head"): 5000, ("Cyber", "Top"): 2000, ("Cyber", "Skirt"): 1000, ("Cyber", "Shoes"): 2000,
    ("Myst", "Head"): 1000, ("Myst", "Top"): 2000, ("Myst", "Skirt"): 5000, ("Myst", "Shoes"): 2000,
    ("Cutie", "Head"): 2000, ("Cutie", "Top"): 1000, ("Cutie", "Skirt"): 2000, ("Cutie", "Shoes"): 5000,
}


def live_stat(name):
    def f(s):
        s.sim.stat(name + ":live", 1 if s.in_live() else 0); s.sim.stat(name + ":layers", s.live)
    return f


def chain(*fs):
    def f(s):
        for g in fs: g(s)
    return f


def punk_top(s):
    s.sim.stat("PunkCatTop:hits", 1 + s.live); s.deal(5, s.target(), hits=1 + s.live)


def weaken(s, n):
    t = s.target()
    if t: t.weak += n


def vuln(s, n):
    t = s.target()
    if t: t.vuln += n


def trash(s):
    s.hiding += 1
    # 本回合敵人 -6 力量：近似為本回合每攻擊敵 -6
    for e in s.alive():
        if e.intent == "attack": s.block += 6 if not e.fan else 5


CARDS = {
    "Strike": F(1, "attack", lambda s: s.deal(6, s.target()), atk_prio(1.0)),
    "Defend": F(1, "skill", lambda s: s.gain_block(5), blk_prio(1.0)),
    "Trauma": F(0, "skill", lambda s: s.add_majoka(10), lambda s: 0.3),
    "PhoneLiveStream": F(1, "skill", lambda s: (setattr(s, "hiding", s.hiding + 1), setattr(s, "live", s.live + 2)), lambda s: 2.2),
    "ForMyOshi": F(1, "attack", lambda s: (s.deal(8, s.target()), s.hand.append(Card("OnAir", True)) if len(s.hand) < 10 else None), atk_prio(2.6)),
    "OnAir": F(1, "power", lambda s: setattr(s, "live", s.live + 3), lambda s: 2.0 + (0.8 if not s.in_live() else 0)),
    "FlameBait": F(1, "attack", chain(live_stat("FlameBait"), lambda s: (setattr(s, "hp_lost", s.hp_lost + 2), s.deal(10 + (20 if s.in_live() else 0), s.target()))),
                   lambda s: (5 if s.in_live() else 1.2) if s.alive() else 0),
    "ShowerStream": F(1, "skill", chain(live_stat("ShowerStream"), lambda s: (s.gain_block(15), setattr(s, "hiding", s.hiding + 1))),
                      blk_prio(3.0), ok=lambda s: s.in_live()),
    "SharpTongue": F(2, "attack", lambda s: [s.deal_all(5) for _ in range(2)] and [setattr(e, "weak", e.weak + 2) for e in s.alive()], atk_prio(3.0)),
    "TalkTrash": F(1, "skill", trash, blk_prio(2.0), exhaust=True),
    "InmuKing": F(3, "skill", lambda s: (setattr(s, "hiding", s.hiding + 1), setattr(s, "live", s.live + 5), s.add_majoka(14)), lambda s: 5.0, exhaust=True),
    "TurnDefenseToOffense": F(2, "skill", fx_tdo, lambda s: 2.5 if s.hiding >= 4 and s.length - s.turn >= 2 else 0.05, exhaust=True),
    "LucidDream": F(1, "skill", fx_lucid, lambda s: 2.0 if s.discard else 0.01),
    "InfiniteMirror": F(2, "power", fx_mirror, lambda s: 0.6 * s.live, ok=lambda s: s.in_live()),
    "SimultaneousViewing": F(1, "skill", chain(live_stat("SimultaneousViewing"), fx_simul), lambda s: 1.5, ok=lambda s: s.in_live()),
    "FanService": F(3, "power", fx_fanservice, lambda s: 3.5),
    "IGuessIHaveNoChoice": F(2, "power", lambda s: (s.powers.__setitem__("Pocket", min(2, s.powers["Pocket"] + 1)), s.pocket()), lambda s: 1.5),
    "OnlineShopping": F(2, "power", lambda s: s.powers.__setitem__("Shop", s.powers["Shop"] + 1), lambda s: 3.0),
    "NiceOutfit": F(0, "skill", lambda s: (s.sim.stat("OnlineShopping:bought", 1), s.sim.stat("OnlineShopping:turns_left", s.length - s.turn), setattr(s, "str", s.str + 1), setattr(s, "dex", s.dex + 1)), lambda s: 4.0, exhaust=True),
    "IWantToPlayAGame": F(1, "power", fx_iwtpag, lambda s: 3.0),
    "PlayWhatGame": F(0, "power", fx_pwg, lambda s: 0.5 if s.majoka >= 130 else 0.02),
    "LetsPlayHideAndSeek": F(1, "skill", fx_hideseek, lambda s: 2.5),
    "FoundYou": F(1, "attack", lambda s: (s.sim.stat("FoundYou:played", 1), s.deal(44, s.target())), atk_prio(8), ok=lambda s: s.majoka >= 100, exhaust=True),
    "HideInWardrobe": F(1, "skill", lambda s: (s.sim.stat("HideInWardrobe:played", 1), setattr(s, "hiding", s.hiding + 1), setattr(s, "intang", s.intang + 1)),
                        lambda s: 6 if s.incoming() >= 12 else 0.02, ok=lambda s: s.majoka < 100, exhaust=True),
    "TreeHollow": F(3, "power", fx_tree, lambda s: 6.0, ok=lambda s: s.majoka < 100),
    "Clairvoyance": F(2, "power", fx_clair, lambda s: 0.6 * min(10, s.majoka // 10)),
    # 裝備
    "PunkCatHeadwear": F(3, "attack", eq("Punk", "Head", fx_pchead), lambda s: 0.5 + s.sim.total_fans * len(s.alive()) / 10),
    "PunkCatTop": F(1, "attack", eq("Punk", "Top", punk_top), lambda s: 1.2 * (1 + s.live) if s.alive() else 0),
    "PunkCatSkirt": F(1, "attack", eq("Punk", "Skirt", chain(live_stat("PunkCatSkirt"), lambda s: (s.deal(8 + (4 if s.in_live() else 0), s.target()), vuln(s, 1)))), atk_prio(2.4)),
    "PunkCatShoes": F(1, "attack", eq("Punk", "Shoes", chain(live_stat("PunkCatShoes"), lambda s: s.deal(9 + (4 if s.in_live() else 0), s.target()))), atk_prio(2.1)),
    "CybercatHeadwear": F(2, "skill", eq("Cyber", "Head", fx_cchead), lambda s: 0.5 + min(s.sim.total_fans, need_block(s)) / 5),
    "CybercatTop": F(1, "skill", eq("Cyber", "Top", lambda s: (s.gain_block(10), setattr(s, "hiding", s.hiding + 2))), blk_prio(3.5)),
    "CybercatSkirt": F(1, "skill", eq("Cyber", "Skirt", lambda s: (s.sim.stat("CybercatSkirt:hiding", s.hiding), s.gain_block(5 + s.hiding), s.draw_n(1))), blk_prio(2.2)),
    "CybercatShoes": F(1, "skill", eq("Cyber", "Shoes", chain(live_stat("CybercatShoes"), lambda s: s.gain_block(8 + (4 if s.in_live() else 0)))), blk_prio(2.2)),
    "MysteriousCatHeadwear": F(1, "skill", eq("Myst", "Head", lambda s: (setattr(s, "hiding", s.hiding + 1), s.draw_n(1))), lambda s: 1.6),
    "MysteriousCatTop": F(1, "skill", eq("Myst", "Top", lambda s: (setattr(s, "hiding", s.hiding + 1), s.draw_n(1))), lambda s: 1.5),
    "MysteriousCatPants": F(1, "skill", eq("Myst", "Skirt", fx_mpants), lambda s: 1.0 + 1.5 * sum(1 for e in s.alive() if e.fan)),
    "MysteriousCatShoes": F(0, "skill", eq("Myst", "Shoes", fx_mshoes), lambda s: 3 if s.in_live() else 0.3, exhaust=True),
    "CutieCatsHeadwear": F(1, "skill", eq("Cutie", "Head", chain(live_stat("CutieCatsHeadwear"), lambda s: (weaken(s, 1), vuln(s, 1) if s.in_live() else None))), lambda s: 1.0),
    "CutieCatsTop": F(0, "skill", eq("Cutie", "Top", lambda s: (vuln(s, 1), weaken(s, 1))), lambda s: 1.0, exhaust=True),
    "CutieCatsPants": F(2, "skill", eq("Cutie", "Skirt", lambda s: [setattr(e, "poison", e.poison + 6) for e in s.alive()]), lambda s: 2.0),
    "CutieCatsShoes": F(2, "skill", eq("Cutie", "Shoes", fx_cshoes), lambda s: 3.0 if any(e.intent == "attack" for e in s.alive()) else 0.2, exhaust=True),
    "SlayTheSpireIi": F(3, "skill", fx_stsii, lambda s: 0.5, ok=lambda s: len(s.hand) + len(s.draw) + len(s.discard) > 8, exhaust=True),
    "EscapeWitchIsland": F(1, "skill", lambda s: None, lambda s: 0, ok=lambda s: False, exhaust=True),
}

STARTER = ["Strike"] * 3 + ["Defend"] * 4 + ["Trauma", "PhoneLiveStream", "ForMyOshi"]

ARCH = {
    "live": STARTER + ["OnAir", "FlameBait", "FlameBait", "ShowerStream", "InfiniteMirror", "SimultaneousViewing",
                       "PunkCatTop", "PunkCatShoes", "MysteriousCatShoes", "LucidDream", "SharpTongue", "MysteriousCatPants"],
    "hide": STARTER + ["CybercatTop", "CybercatSkirt", "MysteriousCatHeadwear", "TurnDefenseToOffense", "TreeHollow",
                       "IWantToPlayAGame", "LetsPlayHideAndSeek", "TalkTrash", "InmuKing", "ShowerStream", "CybercatShoes", "LucidDream"],
    "equip": STARTER + ["PunkCatHeadwear", "PunkCatTop", "PunkCatSkirt", "PunkCatShoes", "MysteriousCatPants", "CybercatHeadwear",
                        "IGuessIHaveNoChoice", "FanService", "OnlineShopping", "CutieCatsShoes", "SlayTheSpireIi", "MysteriousCatTop"],
}


class Sim:
    def __init__(self, deck, seed, name="", innate=()):
        self.deck = deck; self.rng = random.Random(seed); self.name = name; self.innate = set(innate)
        self.st = defaultdict(float); self.wsum = 0.0
        self.total_fans = 0; self.fans_made = 0; self.perm_added = []
        self.out = 0.0; self.w = 1.0

    def stat(self, k, v):
        self.st[k] += v * self.w
        self.st[k + "#n"] += self.w

    def on_play(self, s, name):
        self.stat(name + ":plays", 1)
        self.stat(name + ":at_live", 1 if s.in_live() else 0)
        self.stat(name + ":at_layers", s.live)
        self.stat(name + ":at_fans_combat", sum(1 for e in s.alive() if e.fan))
        self.stat(name + ":at_total_fans", self.total_fans)
        self.stat(name + ":at_majoka", s.majoka)
        self.stat(name + ":at_hiding", s.hiding)
        self.stat(name + ":at_turn", s.turn)
        self.stat(name + ":rl", s.rl)

    def run_one(self):
        self.total_fans = 0; self.perm_added = []
        exp = 0
        for act, kind in RUN:
            rl = relic_level(exp)
            deck = [Card(n) for n in self.deck + self.perm_added]
            self.w = ENC[kind][2]
            f0 = self.total_fans
            c = Combat(self, deck, kind, act, rl, self.rng).run()
            self.stat("_fight", 1)
            self.stat("_fans_per_fight", self.total_fans - f0)
            self.stat("_fight_len", c.length)
            self.out += self.w * (c.dealt / DMG_PER_E - c.hp_lost / HP_PER_E)
            self.wsum += self.w
            exp += EXP_GAIN[kind]


def run(deck, n, seed=1, innate=()):
    s = Sim(deck, seed, innate=innate)
    for _ in range(n):
        s.run_one()
    return s


def summarize(s, keys=None):
    res = {}
    for k, v in s.st.items():
        if k.endswith("#n"):
            continue
        n = s.st[k + "#n"]
        res[k] = (v / n if n else 0, v / s.st["_fight"])
    return res


if __name__ == "__main__":
    N = int(sys.argv[1]) if len(sys.argv) > 1 else 2000
    out = {}
    for name, deck in ARCH.items():
        s = run(deck, N * 3, seed=11)
        out[name] = {"stats": {k: [round(a, 3), round(b, 1)] for k, (a, b) in sorted(summarize(s).items())},
                     "out_per_fight": s.out / s.wsum}
    # 邊際價值：將目標卡換成空白卡（不可打出）比較每場加權輸出
    CARDS["Blank"] = F(9, "skill", lambda s: None, lambda s: 0, ok=lambda s: False)
    marg = {}
    for name, deck in ARCH.items():
        base_cards = sorted(set(deck) - {"Strike", "Defend", "Trauma"})
        for card in base_cards:
            d2 = list(deck); d2[d2.index(card)] = "Blank"
            a = run(deck, N, seed=101); b = run(d2, N, seed=101)
            plays = a.st[card + ":plays"] / max(1.0, a.st["_fight"])  # 每場加權平均打出
            per_fight = (a.out - b.out) / a.wsum
            marg[f"{name}:{card}"] = {"delta_per_fight": round(per_fight, 3), "plays_per_fight": round(plays, 3),
                                      "value_per_play_raw": round(per_fight / plays, 3) if plays > 0.01 else None}
    out["marginal"] = marg
    json.dump(out, open(sys.argv[2] if len(sys.argv) > 2 else "result.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print("done")
