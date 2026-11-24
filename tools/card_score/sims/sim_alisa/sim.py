"""紫藤亞里莎簡化戰鬥蒙地卡羅模型（v5）。規則依 Manosaba 原始碼：
- 灼燒(BurnPower)：敵方回合開始時造成「層數」點無法格擋傷害，觸發 1 + 我方 ThisIsFine 層數 次，**不遞減**。
- 火球環繞：上限 12；每層吸收 3 點失血（格擋後，ceil(失血/3) 層被消耗）。
- 腳鐐(LegIrons)：戰鬥開始獲得 Lv 層火球；Lv2+ 每獲得 1 層火球 +5 魔女化；Lv4+ 回合結束回復 = 火球層數。
- 燃燒(Combust)：每次抽到 -1，點火 -1；歸零時 +1 火球、FinalCountdown -1、重設並免費自動打出。
- 魔女化：攻擊(非 Unpowered)傷害倍率 1+3·s^1.8365/(s^1.8365+166.6^1.8365)；>=100 時（本場一次）將 FireJudgementCourt 加入手牌。
- 每回合抽 5、3 能量、手牌上限 10；敵人固定血量／傷害曲線。
"""
from __future__ import annotations
import math
import random
from collections import defaultdict

ENC = {  # 比例, 總血量, 敵人數, 每回合總傷害基礎, 每回合成長, 攻擊機率
    "hallway": (0.72, (62, 92), (1, 3), 7.0, 0.7, 0.8),
    "elite": (0.20, (165, 205), (1, 2), 10.0, 0.8, 0.85),
    "boss": (0.08, (380, 440), (1, 1), 10.0, 0.9, 0.85),
}
IMP = {"hallway": 1.0, "elite": 2.0, "boss": 3.0}
RELIC_LV = ([1, 2, 3, 4, 5], [0.15, 0.30, 0.25, 0.20, 0.10])
KP = 166.6 ** 1.8365

COST = {"Strike": 1, "Defend": 1, "Trauma": 0, "EmberSpark": 1, "Lighter": 1, "BurnCap": 1,
        "CounterViolence": 1, "FireSword": 1, "SnakeFire": 2, "DisasterGirl": 1, "FinalCountdown": 3,
        "FireJudgementCourt": 3, "FireOnFire": 1, "Flaming": 3, "LoveFlame": 0, "MagicianRed": 3,
        "RestrictionRelease": 5, "ScaldingTouch": 2, "Scorching": 1, "SpreadBurn": 2, "SweepUp": 1,
        "ThisIsFine": 1, "YouAreWeakEma": 1, "Fireball": 0}
COST_UP = {"FinalCountdown": 2, "FireJudgementCourt": 2, "MagicianRed": 2}
POWER = {"DisasterGirl", "FinalCountdown", "ScaldingTouch", "Scorching", "ThisIsFine"}
IGNITE_KW = {"Lighter", "CounterViolence", "FireOnFire"}
NATIVE_COMBUST = {"Flaming": 2, "RestrictionRelease": 3}
EXHAUST = {"MagicianRed", "Fireball", "FireJudgementCourt"}  # RestrictionRelease 依升級
RETAIN = {"SnakeFire"}

STARTER = ["Strike"] * 4 + ["Defend"] * 3 + ["Trauma", "EmberSpark", "Lighter"]
ARCH = {
    # 灼燒堆疊型
    "burn": STARTER + ["FireSword", "BurnCap", "SnakeFire", "FireOnFire", "Scorching", "DisasterGirl",
                       "ThisIsFine", "CounterViolence", "Flaming", "Defend"],
    # 火球環繞型
    "orb": STARTER + ["SweepUp", "BurnCap", "LoveFlame", "YouAreWeakEma", "YouAreWeakEma", "MagicianRed",
                      "Scorching", "FireSword", "CounterViolence", "SweepUp"],
    # 燃燒／點火型
    "combust": STARTER + ["Lighter", "CounterViolence", "CounterViolence", "SnakeFire", "EmberSpark",
                          "Flaming", "FireOnFire", "FireSword", "ScaldingTouch", "BurnCap"],
}

BASE_VAL = {"Strike": 1.0, "FireSword": 2.6, "BurnCap": 2.2, "Defend": 0.9, "Trauma": 0.6, "EmberSpark": 1.2,
            "SnakeFire": 2.4, "Flaming": 3.0, "SweepUp": 1.6, "YouAreWeakEma": 2.0, "MagicianRed": 3.0,
            "SpreadBurn": 1.5, "LoveFlame": 0.5, "FinalCountdown": 0.5, "FireJudgementCourt": 5.0,
            "RestrictionRelease": 6.0, "Fireball": 0.8}


def base_val(c):
    return BASE_VAL.get(c.name, 1.0) + (0.3 if c.up else 0)


class Card:
    __slots__ = ("name", "up", "cmax", "ccur", "src")

    def __init__(self, name, up=False):
        self.name = name; self.up = up
        self.cmax = NATIVE_COMBUST.get(name, 0); self.ccur = self.cmax
        self.src = None  # 附加燃燒的來源卡名


class Enemy:
    __slots__ = ("hp", "burn", "stun", "srcs")

    def __init__(self, hp):
        self.hp = hp; self.burn = 0; self.stun = False; self.srcs = defaultdict(float)


def majoka_mult(s):
    if s <= 0:
        return 1.0
    p = s ** 1.8365
    return 1.0 + 3.0 * p / (p + KP)


class Combat:
    def __init__(self, deck, kind, rng, st, probe=None, lv=None):
        self.rng = rng; self.kind = kind; self.st = st; self.probe = probe
        _, hpr, nr, self.bdmg, self.grow, self.patk = ENC[kind]
        n = rng.randint(*nr); tot = rng.uniform(*hpr)
        self.n0 = n
        self.enemies = [Enemy(tot / n) for _ in range(n)]
        self.draw = [Card(nm.rstrip("+"), nm.endswith("+")) for nm in deck]; rng.shuffle(self.draw)
        self.hand = []; self.disc = []
        self.lv = lv if lv else rng.choices(*RELIC_LV)[0]
        self.hp = 60.0; self.block = 0.0; self.orb = 0; self.majoka = 0.0; self.fjc_given = False
        self.fine = 0; self.scorch = 0; self.dg_layers = []; self.dg_credit = 0
        self.scald = 0; self.scald_majoka = 0.0; self.countdown = 0; self.cd_win = False
        self.turn = 0; self.energy = 0; self.done = False
        self.ignite_cur = None

    def alive(self):
        return [e for e in self.enemies if e.hp > 0]

    def pst(self, key, v=1.0):
        self.st[key] += v

    def gain_orb(self, n, src=None):
        if n <= 0:
            return
        self.orb = min(12, self.orb + n)
        self.pst("orb_gained", n)
        if src is not None and src == self.probe:
            self.pst("probe_orb", n)
        if self.lv >= 2:
            self.gain_majoka(5 * n)

    def gain_majoka(self, n):
        self.majoka += n
        if self.majoka >= 100 and not self.fjc_given:
            self.fjc_given = True
            self.pst("majoka100"); self.pst("majoka100_turn", self.turn)
            if self.turn > 0 and len(self.hand) < 10:
                self.hand.append(Card("FireJudgementCourt"))

    def apply_burn(self, e, n, src):
        if n <= 0 or e.hp <= 0:
            return
        e.burn += n; e.srcs[src] += n; self.st["bapp:" + src] += n
        self.pst("burn_applied", n)
        self.st["bapp_t%d" % min(self.turn, 8)] += n
        if self.dg_layers:
            self.dg_credit += n
            while self.dg_credit >= 5:
                self.dg_credit -= 5
                al = self.alive()
                if not al:
                    break
                t = self.rng.choice(al); d = sum(self.dg_layers)
                self.deal(t, d, unpowered=True, src="DisasterGirl")
                self.pst("dg_triggers")

    def burn_tick(self, e, tag=None):
        if e.hp <= 0 or e.burn <= 0:
            return
        d = min(e.burn, e.hp)
        frac = d / e.burn
        for s, amt in e.srcs.items():
            self.st["bdmg:" + s] += amt * frac
        e.hp -= e.burn
        self.pst("burn_dmg", d)
        if tag:
            self.pst(tag, d)

    def deal(self, e, dmg, unpowered=False, src=None, attack=False):
        if e.hp <= 0:
            return 0.0
        if not unpowered:
            dmg = dmg * majoka_mult(self.majoka)
        d = min(dmg, e.hp); e.hp -= dmg
        self.pst("dmg_direct", d)
        if src is not None and src == self.probe:
            self.pst("probe_dmg", d)
        if src == "DisasterGirl":
            self.pst("dg_dmg", d)
        if attack and self.scald and self.rng.random() < 0.85:  # 約 15% 被敵方格檔吸收
            self.pst("scald_hits")
            if e.hp > 0:
                self.apply_burn(e, self.scald, "ScaldingTouch")
            self.gain_majoka(self.scald_majoka)
            self.pst("scald_majoka", self.scald_majoka)
        return d

    def target_low(self):
        al = self.alive()
        return min(al, key=lambda e: e.hp) if al else None

    def target_burn(self):
        al = self.alive()
        return max(al, key=lambda e: e.hp - 3 * e.burn) if al else None

    def lose_hp(self, amt):
        if amt <= 0:
            return
        if self.orb > 0:
            need = min(self.orb, math.ceil(amt / 3))
            ab = min(amt, need * 3)
            self.orb -= need; amt -= ab
            self.pst("orb_absorbed", ab); self.pst("orb_used", need)
        self.hp -= amt
        self.pst("hp_lost", amt)

    # ---------- 抽牌與燃燒 ----------
    def draw_n(self, n):
        for _ in range(n):
            if self.done:
                return
            if not self.draw:
                if not self.disc:
                    return
                self.draw = self.disc; self.disc = []; self.rng.shuffle(self.draw)
            c = self.draw.pop()
            if len(self.hand) >= 10:
                self.disc.append(c); continue
            self.hand.append(c)
            if self.probe and c.name == self.probe:
                self.pst("probe_drawn")
            if c.cmax > 0:
                c.ccur -= 1
                if c.ccur <= 0:
                    self.combust_zero(c, "draw")

    def combust_zero(self, c, how):
        self.pst("combust_zero"); self.pst("combust_zero_" + how)
        if c.src is not None:
            self.st["attach_auto:" + c.src] += 1
            self.st["attach_auto_val:" + c.src] += base_val(c)
        if c.name == self.probe:
            self.pst("probe_auto")
        if self.ignite_cur is not None and self.ignite_cur == self.probe:
            self.pst("probe_ignite_auto")
            self.pst("probe_ignite_auto_val", base_val(c))
        if self.countdown > 0:
            self.countdown -= 1; self.pst("cd_combust")
            if self.countdown <= 0:
                self.cd_win = True; self.done = True; return
        self.gain_orb(1)
        c.ccur = c.cmax
        if c in self.hand:
            self.hand.remove(c)
        self.play(c, free=True)

    def ignite(self, src):
        cands = [c for c in self.hand if c.cmax > 0 and c.ccur > 0]
        if src == self.probe:
            self.pst("probe_ignite_try")
        if not cands:
            return
        c = min(cands, key=lambda c: (c.ccur, -base_val(c)))
        if src == self.probe:
            self.pst("probe_ignite_ok")
        c.ccur -= 1
        if c.ccur <= 0:
            self.ignite_cur = src
            self.combust_zero(c, "ignite")
            self.ignite_cur = None

    def attach(self, src, picks):
        cands = [c for c in self.hand if c.cmax == 0 and c.name not in IGNITE_KW and c.name not in POWER
                 and c.name != "Fireball"]
        cands.sort(key=lambda c: -base_val(c))
        if src == self.probe:
            self.pst("probe_attach_try")
        for c in cands[:picks]:
            c.cmax = 3; c.ccur = 3; c.src = src
            if src == self.probe:
                self.pst("probe_attached")

    # ---------- 卡牌效果 ----------
    def cost(self, c):
        if c.up and c.name in COST_UP:
            return COST_UP[c.name]
        return COST[c.name]

    def playable(self, c):
        if c.name == "LoveFlame" and self.orb < 2:
            return False
        if c.name == "MagicianRed" and self.orb < 4:
            return False
        if c.name == "FinalCountdown" and self.countdown > 0:
            return False
        return True

    def play(self, c, free=False):
        if self.done:
            return
        al = self.alive()
        if not al:
            self.done = True; return
        if free and not self.playable(c):  # 自動打出但條件不符：視為無效果
            self.disc.append(c); return
        nm, up = c.name, c.up
        isprobe = nm == self.probe
        if isprobe:
            self.pst("probe_plays")
            if free:
                self.pst("probe_free_plays")
            self.pst("probe_turn", self.turn)
            self.pst("probe_orb_at_play", self.orb)
            self.pst("probe_majoka_at_play", self.majoka)
        tl = self.target_low(); tb = self.target_burn()
        if free:
            tl = tb = self.rng.choice(al)
        if nm == "Strike":
            self.deal(tl, 9 if up else 6, src=nm, attack=True)
        elif nm == "Defend":
            self.block += 8 if up else 5
        elif nm == "Trauma":
            self.gain_majoka(15 if up else 10)
        elif nm == "EmberSpark":
            self.block += 5; self.attach(nm, 2 if up else 1)
        elif nm == "Lighter":
            self.apply_burn(tb, 5 if up else 3, nm); self.ignite(nm)
        elif nm == "BurnCap":
            self.apply_burn(tb, 7 if up else 5, nm); self.gain_orb(1, nm)
        elif nm == "CounterViolence":
            self.block += 12 if up else 8; self.ignite(nm)
        elif nm == "FireSword":
            self.deal(tl, 8 if up else 6, src=nm, attack=True)
            if tl.hp > 0:
                self.apply_burn(tl, 6 if up else 5, nm)
        elif nm == "SnakeFire":
            self.apply_burn(tb, 7, nm); self.attach(nm, 2 if up else 1)
        elif nm == "DisasterGirl":
            self.dg_layers.append(11 if up else 7)
            if isprobe:
                self.st["dg_play_turn"] += self.turn
        elif nm == "FinalCountdown":
            hp0 = self.hp; self.lose_hp(20); self.countdown = 20
            if isprobe:
                self.pst("cd_hp_cost", hp0 - self.hp)
        elif nm == "FireJudgementCourt":
            for e in al:
                self.apply_burn(e, 10, nm)
            trig = int(min(self.majoka / 100.0, 1.0) * 3)
            self.pst("fjc_trig", trig)
            for e in al:
                for _ in range(trig):
                    self.burn_tick(e, "fjc_dmg")
        elif nm == "FireOnFire":
            ts = [e for e in al if e.burn > 0]
            if isprobe:
                self.pst("fof_try")
            if ts:
                if isprobe:
                    self.pst("fof_ok")
                t = tb if (tb in ts) else max(ts, key=lambda e: e.hp)
                self.apply_burn(t, 12 if up else 9, nm); self.ignite(nm)
        elif nm == "Flaming":
            for e in al:
                self.apply_burn(e, 6 if up else 4, nm)
            self.gain_orb(1, nm)
        elif nm == "LoveFlame":
            self.orb -= 2; self.energy += 2 if up else 1
            if isprobe:
                self.pst("lf_energy", 2 if up else 1)
        elif nm == "MagicianRed":
            self.orb -= 4; self.deal(tl, 20, src=nm, attack=True)
            if tl.hp > 0:
                tl.stun = True
                if isprobe:
                    self.pst("mr_stun")
        elif nm == "RestrictionRelease":
            ex = len(self.hand)
            for h in list(self.hand):
                self.hand.remove(h)
            if isprobe:
                self.pst("rr_exhausted", ex)
            for _ in range(8):
                self.hand.append(Card("Fireball"))
            if not up:
                return
        elif nm == "ScaldingTouch":
            self.scald += 2 if up else 1; self.scald_majoka += 5
        elif nm == "Scorching":
            self.scorch += 2 if up else 1
        elif nm == "SpreadBurn":
            s = max(al, key=lambda e: e.burn)
            if s.burn > 0:
                k = 0
                for e in al:
                    if e is not s:
                        self.apply_burn(e, s.burn, nm); k += 1
                if k:
                    self.gain_orb(k * (2 if up else 1), nm)
                    if isprobe:
                        self.pst("sb_spread", k)
        elif nm == "SweepUp":
            self.draw_n(3 if up else 2); self.gain_orb(1, nm)
        elif nm == "ThisIsFine":
            self.fine += 2 if up else 1
        elif nm == "YouAreWeakEma":
            self.deal(tl, 5 + (5 if up else 3) * self.orb, src=nm, attack=True)
        elif nm == "Fireball":
            self.deal(tl, 5, src="RestrictionRelease" if self.probe == "RestrictionRelease" else nm, attack=True)
        if nm in POWER or nm in EXHAUST:
            pass
        else:
            self.disc.append(c)
        if not self.alive():
            self.done = True

    # ---------- 出牌啟發式 ----------
    def incoming(self):
        al = self.alive()
        if not al:
            return 0.0
        per = (self.bdmg + self.grow * (self.turn - 1)) / self.n0
        return per * sum(1 for e in al if not e.stun)

    def ign_v(self):
        return 0.6 if any(h.cmax > 0 and h.ccur > 0 for h in self.hand) else 0.0

    def score(self, c):
        nm, up = c.name, c.up
        al = self.alive()
        if not al:
            return -1
        need_block = max(0.0, self.incoming() - self.block - 3 * self.orb)
        bval = lambda b: min(b, need_block) / 5.0 + 0.03 * b
        burnv = lambda s: s * 0.35
        mm = majoka_mult(self.majoka)
        if nm == "Strike": return (9 if up else 6) * mm / 6
        if nm == "Defend": return bval(8 if up else 5)
        if nm == "Trauma": return 0.8
        if nm == "EmberSpark":
            return bval(5) + (0.5 if any(h.cmax == 0 and h.name not in IGNITE_KW and h is not c for h in self.hand) else 0)
        if nm == "Lighter": return burnv(5 if up else 3) + self.ign_v()
        if nm == "BurnCap": return burnv(7 if up else 5) + 0.5
        if nm == "CounterViolence": return bval(12 if up else 8) + self.ign_v()
        if nm == "FireSword": return (8 if up else 6) * mm / 6 + burnv(6 if up else 5)
        if nm == "SnakeFire": return burnv(7) + 0.4
        if nm in ("DisasterGirl", "Scorching", "ScaldingTouch"): return 3.0
        if nm == "ThisIsFine": return 3.0 if (sum(e.burn for e in al) > 0 or self.turn >= 2) else 1.0
        if nm == "FinalCountdown": return 2.0 if (self.kind == "boss" and self.hp > 45) else -1
        if nm == "FireJudgementCourt": return 9.0
        if nm == "FireOnFire": return (burnv(12 if up else 9) + self.ign_v()) if any(e.burn > 0 for e in al) else -1
        if nm == "Flaming": return burnv(6 if up else 4) * len(al) + 0.5
        if nm == "LoveFlame":
            rest = [h for h in self.hand if h is not c and self.playable(h) and self.cost(h) > self.energy]
            return 1.2 if rest else -1
        if nm == "MagicianRed": return 5.0
        if nm == "RestrictionRelease": return 8.0
        if nm == "SpreadBurn":
            m = max(e.burn for e in al)
            return burnv(m) * (len(al) - 1) + 0.5 * (len(al) - 1) if (len(al) > 1 and m > 0) else -1
        if nm == "SweepUp": return 1.5
        if nm == "YouAreWeakEma": return (5 + (5 if up else 3) * self.orb) * mm / 6
        if nm == "Fireball": return 5 * mm / 6
        return 0.5

    def player_turn(self):
        self.turn += 1
        self.block = 0.0; self.energy = 3
        if self.scorch:
            for e in self.alive():
                self.apply_burn(e, self.scorch, "Scorching")
            self.gain_orb(self.scorch, "Scorching")
        self.draw_n(5)
        if self.done:
            return
        for nm, need in (("LoveFlame", 2), ("MagicianRed", 4)):
            if self.probe == nm and any(h.name == nm for h in self.hand):
                self.pst("inhand_turns"); self.pst("inhand_ok", 1 if self.orb >= need else 0)
        guard = 0
        while not self.done and guard < 40:
            guard += 1
            best = None; bs = 0.15
            for c in self.hand:
                k = self.cost(c)
                if k > self.energy or not self.playable(c):
                    continue
                s = self.score(c)
                if c.name in ("EmberSpark", "SnakeFire"):
                    s += 0.05
                s = s / max(k, 0.6)
                if s > bs:
                    best, bs = c, s
            if best is None:
                break
            self.hand.remove(best); self.energy -= self.cost(best)
            self.play(best)
        if self.done:
            return
        keep = [c for c in self.hand if c.name in RETAIN]
        for c in self.hand:
            if c.name not in RETAIN and c.name != "Fireball":
                self.disc.append(c)
        self.hand = keep
        if self.lv >= 4 and self.orb > 0 and self.hp < 60:
            h = min(self.orb, 60 - self.hp)
            self.hp += h; self.pst("lv4_heal", h)
        self.pst("orb_turn_end", self.orb); self.pst("turns")
        if self.countdown > 0:
            self.countdown -= 1
            if self.countdown <= 0:
                self.cd_win = True; self.done = True

    def enemy_turn(self):
        for e in self.alive():
            for i in range(1 + self.fine):
                self.burn_tick(e, "fine_dmg" if i > 0 else None)
        if not self.alive():
            self.done = True; return
        dmg = 0.0
        per = (self.bdmg + self.grow * (self.turn - 1)) / self.n0
        for e in self.alive():
            if e.stun:
                e.stun = False; continue
            if self.rng.random() < self.patk:
                dmg += per / self.patk
        b = min(self.block, dmg); dmg -= b
        self.lose_hp(dmg)
        if self.hp <= 0:
            self.done = True

    def run(self):
        self.gain_orb(self.lv)
        while not self.done and self.turn < 30:
            self.player_turn()
            if self.done:
                break
            self.enemy_turn()
        st = self.st
        st["fights"] += 1; st["len"] += self.turn
        st["deaths"] += 1 if self.hp <= 0 else 0
        st["cd_win"] += 1 if self.cd_win else 0
        st["majoka_end"] += self.majoka
        st["orb_end"] += self.orb


def run(deck, n, seed, probe=None, lv=None):
    rng = random.Random(seed)
    kinds = list(ENC); w = [ENC[k][0] for k in kinds]
    by = {k: defaultdict(float) for k in kinds}
    for _ in range(n):
        k = rng.choices(kinds, w)[0]
        Combat(deck, k, rng, by[k], probe=probe, lv=lv).run()
    return by
