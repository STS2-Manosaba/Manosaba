"""寶生瑪格 簡化戰鬥＋蒙地卡羅模型。

規則依 Manosaba/Characters/HoshoMago 原始碼：
- 起始牌組 3 打擊 3 防禦 創傷 解夢 + TarotDeck 取得 1 張隨機塔羅；遺物 Lv2~5 各給 1 張未擁有塔羅（經驗 0/2/7/15/30，一般1菁英2首領3）
- 卡牌獎勵全換成 3 張不重複塔羅；集齊 21 種得 TheWorld（固有、保留，抽牌堆空時 0 費直接勝利）
- 魔女化：MajokaPower Hill 曲線加傷；>=100 取得回音（牌組+手牌）
敵人為抽象模型。價值換算：6 傷=1 能量、有效格擋 5=1、回血 0.28（一次）/0.22（可重用）。
用法：python sim.py [runs_per_archetype]
"""
from __future__ import annotations
import random, json, sys, math
from collections import defaultdict

DMG_E, BLK_E, HEAL_E, HEAL_RE = 6.0, 5.0, 0.28, 0.22

TAROTS = ["TheFool", "TheMagician", "TheHighPriestess", "TheEmpress", "TheEmperor", "TheHierophant",
          "TheLovers", "TheChariot", "Strength", "TheHermit", "WheelOfFortune", "Justice", "TheHangedMan",
          "Death", "Temperance", "TheDevil", "TheTower", "TheStar", "TheMoon", "TheSun", "Judgement"]
assert len(TAROTS) == 21
# 選牌用靜態值（v2）
STATIC = {"TheFool": 1.9, "TheMagician": 2.8, "TheHighPriestess": 1.7, "TheEmpress": 2.0, "TheEmperor": 3.8,
          "TheHierophant": 1.6, "TheLovers": 1.0, "TheChariot": 3.1, "Strength": 3.6, "TheHermit": 2.5,
          "WheelOfFortune": 2.2, "Justice": 0.5, "TheHangedMan": 1.5, "Death": 3.3, "Temperance": 0.3,
          "TheDevil": 1.8, "TheTower": 2.5, "TheStar": 3.4, "TheMoon": 1.3, "TheSun": 4.5, "Judgement": 0.7}
COST = {"StrikeHoshoMago": 1, "DefendHoshoMago": 1, "TraumaHoshoMago": 0, "DreamInterpretation": 0, "Echoes": 2,
        "TheFool": 0, "TheMagician": 1, "TheHighPriestess": 1, "TheEmpress": 1, "TheEmperor": 2, "TheHierophant": 1,
        "TheLovers": 1, "TheChariot": 2, "Strength": 1, "TheHermit": 2, "WheelOfFortune": 1, "Justice": 1,
        "TheHangedMan": 0, "Death": 1, "Temperance": 3, "TheDevil": 1, "TheTower": 2, "TheStar": 2, "TheMoon": 1,
        "TheSun": 2, "Judgement": 1, "TheWorld": 2, "Dazed": 99}
UPCOST = {"Echoes": 1, "TheLovers": 0, "Justice": 0, "Temperance": 2, "Judgement": 0, "TheWorld": 1}
TYPE = defaultdict(lambda: "skill", {"StrikeHoshoMago": "attack", "TheEmpress": "attack", "TheEmperor": "attack",
       "TheChariot": "attack", "TheTower": "attack", "TheMoon": "attack", "Strength": "power", "TheHermit": "power",
       "Temperance": "power", "TheSun": "power", "Echoes": "power", "Dazed": "status"})
EXHAUST = {"TheMagician", "Justice", "TheHangedMan", "Death_never", "TheDevil", "Judgement", "Dazed_no"}
POWERS = {"Strength", "TheHermit", "Temperance", "TheSun", "Echoes"}

ENC = {  # 平均回合, gamma形狀, 重要性, 敵人數分佈, 每敵基礎傷害, 總血量
    "hallway": (4.0, 4.0, 1.0, [(1, .35), (2, .40), (3, .25)], 4.5, 66.0),
    "elite": (6.5, 4.0, 2.0, [(1, .6), (2, .3), (3, .1)], 6.5, 185.0),
    "boss": (10.0, 5.0, 3.0, [(1, .8), (2, .2)], 8.0, 520.0),
}
ACT_SCALE = [1.0, 1.5, 2.1]
RUN = []
for act in range(3):
    seq = ["hallway"] * 7
    seq.insert(3, "elite"); seq.insert(7, "elite"); seq.append("boss")
    RUN += [(act, k) for k in seq]
EXP = {"hallway": 1, "elite": 2, "boss": 3}
LV = [0, 2, 7, 15, 30]


def relic_level(exp):
    return sum(1 for t in LV if exp >= t)


def mmult(s):
    if s <= 0:
        return 1.0
    p = s ** 1.8365
    return 1 + 3 * p / (p + 166.6 ** 1.8365)


class C:
    __slots__ = ("n", "up", "replay", "gen", "exh")
    def __init__(self, n, up=False, gen=False):
        self.n, self.up, self.replay, self.gen, self.exh = n, up, 0, gen, False
    def cost(self):
        return UPCOST.get(self.n, COST[self.n]) if self.up else COST[self.n]


# ---------------- 統計 ----------------
class Stats:
    def __init__(self):
        self.v = defaultdict(float)       # 卡 -> 總價值(能量)
        self.plays = defaultdict(float)
        self.inDeckW = defaultdict(float)  # 擁有權重
        self.fightsW = 0.0
        self.k = defaultdict(float)       # 其他觀測
        self.kn = defaultdict(float)
    def obs(self, key, x, w=1.0):
        self.k[key] += x * w; self.kn[key] += w
    def mean(self, key):
        return self.k[key] / self.kn[key] if self.kn[key] else None


# ---------------- 戰鬥 ----------------
class Fight:
    def __init__(self, deck, kind, act, rng, st, w, hp, maxhp, ctx):
        self.rng, self.st, self.w, self.kind, self.ctx = rng, st, w, kind, ctx
        _, _, _, ndist, base, tot = ENC[kind]
        n = rng.choices([a for a, _ in ndist], [b for _, b in ndist])[0]
        sc = ACT_SCALE[act]
        self.en = []
        for i in range(n):
            h = tot * sc / n * rng.uniform(0.8, 1.2)
            self.en.append(dict(hp=h, mx=h, dmg=base * sc * (1.3 if n == 1 else 1.0) / (1 + 0.25 * (n - 1)), str=0.0,
                                weak=0, vuln=0, doom=0.0, doomD=0.0, mad=0.0, stun=False, lov=0.0, tower=0,
                                eye=0, awak=0))
        self.hp, self.mx = hp, maxhp
        self.blk = 0.0; self.str = 0.0; self.maj = 0.0; self.intang = 0; self.star = 0.0
        self.weak = self.frail = self.vuln = 0; self.doom = 0.0; self.mad = 0.0; self.eye = 0; self.awak = 0
        self.sun = 0; self.fool = None; self.majsrc = defaultdict(float); self.strsrc = defaultdict(float)
        self.deckset = set(c.n for c in deck)
        self.statuser = rng.random() < 0.25
        cards = [C(c.n, c.up) for c in deck]
        rng.shuffle(cards)
        inn = [c for c in cards if c.n == "TheWorld" or (c.n == "TheSun" and c.up)]
        rest = [c for c in cards if c not in inn]
        self.draw = rest + inn  # pop() 取尾端 → 固有先抽
        self.hand, self.disc, self.exh = [], [], []
        self.won_world = False
        self.turn = 0

    has = lambda s, n: n in s.deckset
    def alive(self):
        return [e for e in self.en if e["hp"] > 0]
    def credit(self, card, val):
        self.st.v[card] += val * self.w

    def drawn(self, k):
        for _ in range(k):
            if not self.draw:
                if not self.disc:
                    return
                self.draw = self.disc; self.disc = []; self.rng.shuffle(self.draw)
            if len(self.hand) >= 10:
                return
            self.hand.append(self.draw.pop())

    def add_majoka(self, src, amt):
        before = self.maj
        self.maj += amt; self.majsrc[src] += amt
        if before < 100 <= self.maj and not self.ctx["echo_in_deck"]:
            self.ctx["echo_in_deck"] = True
            self.ctx["echo_gain"] = True
            self.hand.append(C("Echoes", gen=True))
            self.st.obs("echo_gained_in_fight", 1, self.w)

    # 玩家攻擊單擊
    def hit(self, card, e, base, kind="move"):
        if e["hp"] <= 0:
            return 0.0
        s = self.str
        m = mmult(self.maj)
        raw = base + s
        d = raw * m * (0.75 if self.weak else 1.0) * (1.5 if e["vuln"] else 1.0)
        d = max(0.0, d)
        dealt = min(d, e["hp"])
        frac = dealt / d if d > 0 else 0
        # 歸屬：基礎/力量/魔女化/易傷
        basepart = base * (0.75 if self.weak else 1.0)
        strpart = s * (0.75 if self.weak else 1.0)
        majpart = raw * (m - 1) * (0.75 if self.weak else 1.0)
        vpart = d - d / 1.5 if e["vuln"] else 0.0
        tot = basepart + strpart + majpart + vpart
        if tot > 0:
            self.credit(card, dealt * basepart / tot / DMG_E)
            if strpart and self.strsrc:
                ss = sum(self.strsrc.values())
                for k, v in self.strsrc.items():
                    self.credit(k, dealt * strpart / tot / DMG_E * v / ss)
            if majpart > 0 and self.majsrc:
                ms = sum(self.majsrc.values())
                for k, v in self.majsrc.items():
                    self.credit(k, dealt * majpart / tot / DMG_E * v / ms)
            if vpart:
                self.credit("TheHierophant", dealt * vpart / tot / DMG_E)
        e["hp"] -= dealt
        if e["lov"] and dealt > 0:
            h = min(dealt * e["lov"] / 100, self.mx - self.hp)
            self.hp += h
            self.credit("TheLovers", h * HEAL_RE); self.st.obs("lovers_heal", h, self.w)
        if self.awak and dealt > 0 and e["hp"] > 0:
            e["mad"] += dealt
            self.madcheck(e, "TheDevil")
        return dealt

    def madcheck(self, e, src):
        if e["mad"] >= 100 and e["hp"] > 0:
            loss = min(e["mx"] * 0.05, e["hp"])
            e["hp"] -= loss; e["mad"] = 0
            prevented = e["dmg"] * ACT_SCALE[0] * 0.65 if not e["stun"] else 0
            e["stun"] = True
            self.credit(src, loss / DMG_E + prevented / BLK_E * 0.7)
            self.st.obs("devil_enemy_madness_trigger", 1, self.w)

    def gain_block(self, card, b):
        b *= 0.75 if self.frail else 1.0
        self.blk += b
        self.pending_block.append((card, b))

    def incoming(self):
        tot = 0.0
        for e in self.alive():
            if e["stun"] or e["intent"] is None:
                continue
            tot += e["intent"]
        return tot

    def play(self, c):
        n, up = c.n, c.up
        reps = 1 + c.replay
        for r in range(reps):
            who = n if r == 0 else "Echoes"
            mult_credit = 1.0
            self.effect(c, who)
        self.st.plays[n] += self.w
        if c.replay:
            self.st.obs("echo_target_replays_used", c.replay, self.w)

    def effect(self, c, who):
        n, up, rng = c.n, c.up, self.rng
        al = self.alive()
        tgt = min(al, key=lambda e: e["hp"]) if al else None
        cr = who  # 歸屬（重放歸回音）
        if n == "StrikeHoshoMago":
            if tgt: self.hit(cr, tgt, 9 if up else 6)
        elif n == "DefendHoshoMago":
            self.gain_block(cr, 8 if up else 5)
        elif n == "TraumaHoshoMago":
            self.add_majoka(cr, 15 if up else 10)
            self.credit(cr, 0.0)
        elif n == "DreamInterpretation":
            opts = rng.sample(TAROTS, 3)
            pick = max(opts, key=lambda x: STATIC[x] if COST[x] <= self.energy else STATIC[x] * 0.6)
            g = C(pick, False, gen=True); g.exh = not up
            self.hand.append(g)
            self.st.obs("dream_pick_static", STATIC[pick], self.w)
        elif n == "Echoes":
            pool = [x for x in self.draw if TYPE[x.n] != "status" and x.replay < 1]
            if pool:
                t = rng.choice(pool)
                f = min(self.maj / 100, 1)
                add = int(1 + 4 * f)
                t.replay += add
                self.st.obs("echo_majoka_at_play", self.maj, self.w)
                self.st.obs("echo_replay_add", add, self.w)
        elif n == "TheFool":
            if self.has("TheWorld"):
                self.fool = (10, 5); self.st.obs("fool_world", 1, self.w)
            else:
                self.fool = (3 if up else 2, 0); self.st.obs("fool_world", 0, self.w)
            self.fool_src = cr
        elif n == "TheMagician":
            kinds = len([k for k in self.deckset if k in TAROTS or k == "TheWorld"])
            amt = kinds * (7 if up else 5)
            self.st.obs("magician_kinds", kinds, self.w); self.st.obs("magician_majoka", amt, self.w)
            self.add_majoka(cr, amt)
        elif n == "TheHighPriestess":
            sts = [x for x in self.hand if TYPE[x.n] == "status"]
            for x in sts:
                self.hand.remove(x); self.exh.append(x)
            self.st.obs("priestess_status", len(sts), self.w)
            self.gain_block(cr, (9 if up else 7) + 4 * len(sts))
            if sts: self.credit(cr, 0.3 * len(sts))
        elif n == "TheEmpress":
            if tgt:
                syn = self.has("TheEmperor"); self.st.obs("empress_syn", syn, self.w)
                base = (9 if up else 6) + ((4.5 if up else 3) if syn else 0)
                d = self.hit(cr, tgt, base)
                h = min(d * 0.5, self.mx - self.hp); self.hp += h
                self.credit(cr, h * HEAL_E); self.st.obs("empress_heal", h, self.w)
        elif n == "TheEmperor":
            if tgt:
                syn = self.has("TheEmpress"); self.st.obs("emperor_syn", syn, self.w)
                self.hit(cr, max(al, key=lambda e: e["hp"]), (30 if up else 20) + ((15 if up else 10) if syn else 0))
        elif n == "TheHierophant":
            if self.hand:
                worst = min(self.hand, key=lambda x: -10 if TYPE[x.n] == "status" else STATIC.get(x.n, 1.0))
                self.hand.remove(worst); self.exh.append(worst)
                self.credit(cr, 0.3 if TYPE[worst.n] == "status" else -0.25)
            for e in al:
                e["weak"] += 3 if up else 2; e["vuln"] += 3 if up else 2
                e["wsrc"] = cr
        elif n == "TheLovers":
            if tgt:
                amt = 25 + (10 if self.has("TheDevil") else 0)
                tgt["lov"] = amt
        elif n == "TheChariot":
            hits = (5 if up else 3) + (1 if self.has("Strength") else 0)
            self.st.obs("chariot_syn", self.has("Strength"), self.w)
            for _ in range(hits):
                for e in self.alive():
                    self.hit(cr, e, 4)
        elif n == "Strength":
            a = 5 if up else 3
            self.str += a; self.strsrc[cr] += a
        elif n == "TheHermit":
            self.intang += 1; self.hermit_src = cr
        elif n == "WheelOfFortune":
            k = (6 if up else 4) + (1 if self.has("TheHangedMan") else 0)
            back = [x for x in self.hand if x.cost() > self.energy or TYPE[x.n] == "status"][:k]
            for x in back:
                self.hand.remove(x); self.draw.insert(rng.randrange(len(self.draw) + 1), x)
            before = len(self.hand)
            self.drawn(k)
            net = len(self.hand) - before
            self.st.obs("wheel_net_draw", net, self.w); self.st.obs("wheel_back", len(back), self.w)
            self.credit(cr, 0.45 * len(back) + 0.25 * max(0, net - len(back)))  # 換手/淨抽 價值
        elif n == "Justice":
            cnt = len(self.hand)
            sts = sum(1 for x in self.hand if TYPE[x.n] == "status")
            for x in self.hand:
                self.exh.append(x)
            self.hand = []
            self.st.obs("justice_exhausted", cnt, self.w)
            if self.has("Temperance") and cnt:
                self.gain_block(cr, 3 * cnt); self.st.obs("justice_temp", 1, self.w)
            else:
                self.st.obs("justice_temp", 0, self.w)
            self.credit(cr, 0.3 * sts - 0.15 * (cnt - sts))
        elif n == "TheHangedMan":
            if al:
                e = max(al, key=lambda e: e["dmg"] + e["str"])
                e["str"] -= 3 if up else 2; e["hsrc"] = cr
        elif n == "Death":
            mag = self.has("TheMagician"); self.st.obs("death_magician", mag, self.w)
            for e in al:
                d = 30 if up else 20
                e["doom"] += d; e["doomD"] += d
                if mag:
                    extra = e["doom"] * 0.5; e["doom"] += extra; e["doomD"] += extra
                e["dsrc"] = cr
            self.doom += 5
            self.credit(cr, -0.1)
        elif n == "Temperance":
            if self.has("Justice"): self.gain_block(cr, 10)
            self.credit(cr, 0.3)
        elif n == "TheDevil":
            boss = self.kind == "boss"
            gainE = sum(max(0, e["hp"] - e["mx"] / 2) for e in al)
            selfloss = max(0, self.hp - self.mx // 2)
            net = gainE / DMG_E - selfloss * HEAL_E * 1.0
            if not boss and net > 0.8:
                for e in al:
                    if e["hp"] > e["mx"] / 2: e["hp"] = e["mx"] / 2
                self.hp = min(self.hp, self.mx // 2)
                self.credit(cr, net); self.st.obs("devil_choice_bestow", 1, self.w)
                self.st.obs("devil_bestow_net", net, self.w)
                self.st.obs("devil_bestow_enemyhp", gainE, self.w); self.st.obs("devil_bestow_selfloss", selfloss, self.w)
            else:
                self.awak = 3
                for e in al: e["awak"] = 3
                self.st.obs("devil_choice_bestow", 0, self.w)
        elif n == "TheTower":
            for e in self.alive():
                self.hit(cr, e, 12 if up else 9)
            dd = self.has("Death"); self.st.obs("tower_death", dd, self.w)
            for e in self.alive():
                e["tower"] = 2 if up else 1
                if dd: e["doom"] += 10; e["dsrc"] = e.get("dsrc", cr)
        elif n == "TheStar":
            sms = all(self.has(x) for x in ("TheSun", "TheMoon", "TheStar"))
            self.st.obs("star_sms", sms, self.w)
            self.gain_block(cr, (20 if up else 16) * (2 if sms else 1))
            self.star = 1.5 if up else 1.0; self.star_src = cr
        elif n == "TheMoon":
            if al:
                t = max(al, key=lambda e: e["doom"])
                sms = all(self.has(x) for x in ("TheSun", "TheMoon", "TheStar"))
                stacks = t["doom"]
                self.st.obs("moon_stacks", stacks, self.w)
                self.hit(cr, t, 5 + (40 if up else 25) * stacks * (2 if sms else 1) / 100)
        elif n == "TheSun":
            sms = all(self.has(x) for x in ("TheSun", "TheMoon", "TheStar"))
            self.sun += 1; self.sun_src = cr
            self.st.obs("sun_turns_left", max(0, self.L - self.turn), self.w)
        elif n == "Judgement":
            debs = sum(1 for x in (self.weak, self.frail, self.vuln, self.doom, self.mad, self.eye) if x)
            trip = all(self.has(x) for x in ("TheHierophant", "TheHighPriestess", "Justice"))
            g = debs * (2 if trip else 1)
            self.energy += g
            self.weak = self.frail = self.vuln = 0; self.doom = 0; self.mad = 0; self.eye = 0
            self.st.obs("judgement_energy", g, self.w)
            self.credit(cr, g * 0.9 + 0.2 * debs)
        elif n == "TheWorld":
            if not self.draw:
                self.won_world = True
                rem = sum(e["hp"] for e in self.alive())
                self.credit(cr, rem / DMG_E)
                self.st.obs("world_win_remhp_e", rem / DMG_E, self.w)
                for e in self.en: e["hp"] = 0
            else:
                ups = [x for x in self.draw + self.disc + self.hand if not x.up]
                for x in ups: x.up = True
                self.credit(cr, 0.15 * len(ups))

    # 出牌優先度
    def score(self, c):
        n = c.n; inc = self.incoming() - self.blk
        if n == "TheWorld":
            return 999 if not self.draw else -1
        if TYPE[n] == "status":
            return -1
        if n == "Justice":
            return -1  # 回合尾特殊處理
        if n in ("TraumaHoshoMago", "DreamInterpretation"):
            return 50
        if n == "TheFool":
            return 40 if self.fool is None and self.turn < self.L else -1
        if n == "TheLovers":
            return 8 if self.mx - self.hp > 8 and self.energy - c.cost() >= 2 else 0.3
        if n in ("DefendHoshoMago", "TheHighPriestess"):
            b = (8 if c.up else 5) if n == "DefendHoshoMago" else 7
            return 0.2 + min(b, max(inc, 0)) / BLK_E
        if n == "TheStar":
            return 0.5 + (min(16, max(inc, 0)) + self.incoming()) / BLK_E
        if n == "Judgement":
            debs = sum(1 for x in (self.weak, self.frail, self.vuln, self.doom, self.mad, self.eye) if x)
            return 30 if debs >= (1 if c.up else 2) else -1
        if n == "Temperance":
            return 0.6
        if n == "TheDevil":
            return 6 if self.turn <= 2 else 1.5
        if n in POWERS:
            return 5 + STATIC.get(n, 2)
        if n == "TheMagician":
            return 6
        if n == "Death":
            return 7 if self.turn <= 2 else 2.5
        return STATIC.get(n, 1.0) + (0.5 if TYPE[n] == "attack" else 0)

    def player_turn(self):
        self.turn += 1
        base_e = 3 + self.sun
        self.energy = base_e
        extra_draw = 0
        if self.fool:
            fe, fd = self.fool
            self.energy += fe; extra_draw = fd
            self.credit(self.fool_src, fd * 0.45)
            self.fool_bonus = fe
            self.fool = None
        else:
            self.fool_bonus = 0
        self.blk = 0.0
        self.pending_block = []
        self.drawn(5 + extra_draw)
        # 敵人意圖
        for e in self.alive():
            e["intent"] = None
            if self.rng.random() < 0.65:
                dm = (e["dmg"] + e["str"]) * (1 + 0.05 * (self.turn - 1))
                e["intent"] = max(0.0, dm * (0.75 if e["weak"] else 1.0) * (1.5 if self.vuln else 1.0))
        guard = 0
        while guard < 30 and self.alive():
            guard += 1
            cand = [c for c in self.hand if c.cost() <= self.energy]
            cand = [(self.score(c), c) for c in cand]
            cand = [x for x in cand if x[0] > 0]
            if not cand:
                break
            cand.sort(key=lambda x: -x[0] / max(x[1].cost(), 0.6))
            c = cand[0][1]
            self.energy -= c.cost()
            self.hand.remove(c)
            self.play(c)
            if self.won_world:
                return
            if c.n in EXHAUST or c.exh or c.n in POWERS:
                if c.n == "Echoes":
                    self.disc.append(c)  # Eternal 能力：此處視為回到棄牌（打出後即生效，實際能力牌移出）
                    self.disc.remove(c)
                self.exh.append(c) if c.n != "Echoes" else None
            elif c.n == "TheDevil" and c.up:
                self.disc.append(c)
            else:
                self.disc.append(c)
        # 額外能量的實際使用量
        spent = base_e + self.fool_bonus - self.energy
        over = max(0.0, spent - 3)
        if self.sun:
            u = min(self.sun, over); self.credit(self.sun_src, u); over -= u
        if self.fool_bonus:
            self.credit(self.fool_src, min(self.fool_bonus, over)); self.st.obs("fool_energy_used", min(self.fool_bonus, over), self.w)
        # 正義：手上剩牌時（狀態或配節制）
        js = [c for c in self.hand if c.n == "Justice" and c.cost() <= self.energy]
        if js:
            j = js[0]
            rest = [x for x in self.hand if x is not j]
            sts = sum(1 for x in rest if TYPE[x.n] == "status")
            if sts >= 1 or (self.has("Temperance") and len(rest) >= 2 and self.incoming() > self.blk):
                self.energy -= j.cost(); self.hand.remove(j); self.play(j); self.exh.append(j)
        # 回合結束：保留世界，其餘棄
        keep = [c for c in self.hand if c.n == "TheWorld"]
        for c in self.hand:
            if TYPE[c.n] == "status":
                self.exh.append(c)  # 暈眩：虛無
            elif c.n != "TheWorld" and not (c.n == "TheHermit" and c.up):
                self.disc.append(c)
            elif c.n == "TheHermit":
                keep.append(c)
        self.hand = keep
        # 末日（玩家回合結束後）
        for e in self.alive():
            if e["doom"] and e["hp"] <= e["doom"]:
                self.credit(e.get("dsrc", "Death"), e["hp"] / DMG_E)
                self.st.obs("doom_kill", 1, self.w)
                e["hp"] = 0
        # 狀態牌
        if self.statuser and self.turn % 2 == 1:
            self.disc.append(C("Dazed"))

    def enemy_turn(self):
        al = self.alive()
        # 格擋歸屬：依加入順序吸收
        dmg_total = 0.0
        hits = []
        for e in al:
            if e["stun"]:
                e["stun"] = False; continue
            if e["intent"] is None:
                r = self.rng.random()
                if r < 0.43 and not self.weak:
                    pass
                if r < 0.35:
                    if e["tower"]:
                        self.credit("TheTower", 0.4)
                    else:
                        e["str"] += 2
                elif r < 0.75:
                    which = self.rng.choice(["weak", "frail", "vuln"])
                    setattr(self, which, 2)
                continue
            d = (e["dmg"] + e["str"]) * (1 + 0.05 * (self.turn - 1)) * (0.75 if e["weak"] else 1.0) * (1.5 if self.vuln else 1.0)
            d = max(0.0, d)
            if e["weak"] and e.get("wsrc"):
                self.credit(e["wsrc"], d / 3 / BLK_E * 0.7)
            if e.get("hsrc") and e["str"] < 0:
                self.credit(e["hsrc"], min(-e["str"], d + 3) / BLK_E * 0.7)
            hits.append((e, d))
        for e, d in hits:
            if self.star:
                r = math.floor(d * self.star)
                if e["hp"] > 0:
                    dealt = min(r, e["hp"]); e["hp"] -= dealt
                    self.credit(self.star_src, dealt / DMG_E); self.st.obs("star_reflect", dealt, self.w)
            if self.intang:
                self.credit(self.hermit_src, max(0, min(d, max(0, d - self.blk)) - 1) / BLK_E)
                d = min(d, 1)
            ab = min(self.blk, d)
            self.blk -= ab
            # 格擋歸屬
            left = ab
            for i, (card, b) in enumerate(self.pending_block):
                if left <= 0: break
                u = min(b, left); left -= u
                self.pending_block[i] = (card, b - u)
                self.credit(card, u / BLK_E)
            un = d - ab
            if un > 0:
                self.hp -= un
                if self.awak:
                    self.mad += un
                    if self.mad >= 100:
                        self.hp -= self.mx * 0.05; self.mad = 0
                        self.credit("TheDevil", -self.mx * 0.05 * HEAL_E)
                        self.st.obs("devil_self_madness", 1, self.w)
        self.star = 0.0
        if self.intang: self.intang -= 1
        for e in al:
            if e["weak"]: e["weak"] -= 1
            if e["vuln"]: e["vuln"] -= 1
            e["lov"] = 0.0
            if e["tower"]: e["tower"] -= 1
        for a in ("weak", "frail", "vuln"):
            if getattr(self, a): setattr(self, a, getattr(self, a) - 1)
        if self.awak:
            self.awak -= 1
        if self.mad: self.mad = max(0, self.mad - 5)

    def run(self):
        mean, shape = ENC[self.kind][0], ENC[self.kind][1]
        self.L = max(2, round(self.rng.gammavariate(shape, mean / shape)))  # 參考長度（用於太陽/愚者估計）
        t = 0
        while self.alive() and self.hp > 0 and t < 25:
            self.player_turn(); t += 1
            if self.won_world or not self.alive():
                break
            self.enemy_turn()
        self.st.obs("fight_turns_" + self.kind, t, 1)
        self.st.obs("death_" + self.kind, self.hp <= 0, 1)
        self.st.obs("maj_end", self.maj, self.w)
        self.st.obs("maj_ge100", self.maj >= 100, self.w)
        if self.won_world:
            self.st.obs("world_win", 1, self.w)
            self.st.obs("world_win_turn", t, self.w)
        elif self.has("TheWorld"):
            self.st.obs("world_win", 0, self.w)
        return max(1, self.hp), t


# ---------------- 冒險 ----------------
def run_once(arch, rng, st):
    deck = [C("StrikeHoshoMago") for _ in range(3)] + [C("DefendHoshoMago") for _ in range(3)] + \
           [C("TraumaHoshoMago"), C("DreamInterpretation")]
    owned = lambda: set(c.n for c in deck)
    deck.append(C(rng.choice(TAROTS)))
    exp = 0; hp = 65; mx = 65
    ctx = {"echo_in_deck": False}
    def kinds():
        return len(set(c.n for c in deck if c.n in TAROTS))
    def world_check():
        if kinds() >= 21 and "TheWorld" not in owned():
            deck.append(C("TheWorld")); st.obs("world_floor", fi, 1)
    for fi, (act, kind) in enumerate(RUN):
        imp = ENC[kind][2]
        st.fightsW += imp
        for n in owned():
            st.inDeckW[n] += imp
        if all(x in owned() for x in ("TheSun", "TheMoon", "TheStar")):
            st.obs("own_sms", 1, imp)
        st.obs("deck_size", len(deck), imp); st.obs("kinds", kinds(), imp)
        f = Fight(deck, kind, act, rng, st, imp, hp, mx, ctx)
        hp, _ = f.run()
        if ctx.pop("echo_gain", False):
            deck.append(C("Echoes"))
        # 獎勵
        lv0 = relic_level(exp); exp += EXP[kind]; lv1 = relic_level(exp)
        for _ in range(lv1 - lv0):
            un = [t for t in TAROTS if t not in owned()]
            if un: deck.append(C(rng.choice(un)))
        world_check()
        offer = [t for t in TAROTS if not (t == "TheMagician" and "TheMagician" in owned())]
        opts = rng.sample(offer, 3)
        own = owned()
        if arch == "collect":
            un = [o for o in opts if o not in own]
            if un: deck.append(C(max(un, key=lambda x: STATIC[x])))
        elif arch == "lean":
            un = [o for o in opts if o not in own and STATIC[o] >= 2.4]
            if un and rng.random() < 0.8: deck.append(C(max(un, key=lambda x: STATIC[x])))
        else:  # greedy 全拿最強
            deck.append(C(max(opts, key=lambda x: STATIC[x])))
        world_check()
        # 火堆/升級
        if rng.random() < 0.3:
            ups = [c for c in deck if not c.up and c.n not in ("DreamInterpretation_x",)]
            if ups:
                c = max(ups, key=lambda c: (c.n in TAROTS) + rng.random())
                c.up = True
        # 商店：每幕第 7 場後
        if fi % 10 == 5:
            un = [t for t in TAROTS if t not in owned()]
            if arch != "lean" and un: deck.append(C(rng.choice(un)))
            for _ in range(2 if arch == "lean" else 1):
                rm = [c for c in deck if c.n == "StrikeHoshoMago"] or [c for c in deck if c.n == "DefendHoshoMago"]
                if rm: deck.remove(rm[0])
            world_check()
        mx = 65 + 2 * fi // 5
        hp = min(mx, hp + (0.3 * mx if fi % 4 == 3 else 0.0) + 4)
        if hp <= 0: hp = mx * 0.5
    st.obs("final_kinds", kinds(), 1)
    st.obs("final_world", "TheWorld" in owned(), 1)


def main():
    N = int(sys.argv[1]) if len(sys.argv) > 1 else 800
    out = {}
    for arch in ("collect", "lean", "greedy"):
        rng = random.Random(hash(arch) & 0xffff)
        st = Stats()
        for _ in range(N):
            run_once(arch, rng, st)
        per = {}
        for n in sorted(set(st.plays) | set(st.v)):
            p = st.plays[n]
            per[n] = dict(per_play=round(st.v[n] / p, 3) if p else None,
                          plays_per_owned_fight=round(p / st.inDeckW[n], 2) if st.inDeckW[n] else None,
                          own_rate=round(st.inDeckW[n] / st.fightsW, 3))
        obs = {k: round(st.mean(k), 3) for k in sorted(st.k) if st.mean(k) is not None}
        obs.update({k: round(st.mean(k), 3) for k in st.kn if k not in st.k and st.mean(k) is not None})
        out[arch] = dict(cards=per, obs=obs, fights=N * len(RUN))
    json.dump(out, open(__file__.replace("sim.py", "result.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    for a, r in out.items():
        print("==", a, r["fights"])
        for n, d in r["cards"].items():
            print(f"  {n:22s} {d}")
        for k, v in r["obs"].items():
            print(f"  . {k} = {v}")


if __name__ == "__main__":
    main()
