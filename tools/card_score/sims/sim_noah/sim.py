"""城崎諾亞 簡化戰鬥蒙地卡羅模擬（顏料球 / 血球）。
規則依 Manosaba/Characters/JogasakiNoah 原始碼；敵人抽象化（turn_model 戰鬥長度、排程死亡、攻擊意圖）。
價值量測：在戰鬥第 t 回合（t~U(1..4)）出牌階段開始時複製戰鬥狀態，
  A 分支「免費」執行某卡效果、B 分支不做事，兩支共用亂數種子跑到戰鬥結束，
  差值 = Δ傷害/6 + Δ避免失血/5 + Δ回血×0.28（能量、抽牌、魔女化會轉成後續出牌與數值，自動計入）。
"""
from __future__ import annotations
import random, math, copy, json, sys
from collections import defaultdict

DMG_PER_E = 6.0
HP_PER_E = 5.0
HEAL_PER_HP = 0.28

ENC = {  # 平均回合, gamma 形狀, 重要性, 敵人數分佈, 每敵攻擊基礎傷害
    "hallway": (4.0, 4.0, 1.0, [(1, .35), (2, .40), (3, .25)], 7.0),
    "elite":   (6.5, 4.0, 2.0, [(1, .6), (2, .3), (3, .1)], 13.0),
    "boss":    (10.0, 5.0, 3.0, [(1, .8), (2, .2)], 17.0),
}
ACT_SCALE = [1.0, 1.35, 1.7]
RUN = []
for act in range(3):
    seq = ["hallway"] * 10
    seq.insert(4, "elite"); seq.insert(9, "elite"); seq.append("boss")
    RUN += [(act, k) for k in seq]
EXP_GAIN = {"hallway": 1, "elite": 2, "boss": 3}
LV_TABLE = [0, 2, 7, 15, 30]
RUN_EXP = []
_e = 0
for _a, _k in RUN:
    RUN_EXP.append(_e); _e += EXP_GAIN[_k]


def relic_level(exp):
    lv = 1
    for i, t in enumerate(LV_TABLE):
        if exp >= t:
            lv = i + 1
    return min(lv, 5)


def majoka_mult(s):
    if s <= 0:
        return 1.0
    k = 166.6 ** 1.8365
    p = s ** 1.8365
    return 1 + 3 * p / (p + k)


PAINT = ["R", "Y", "B", "O", "G", "P", "K", "W"]
ZUMA_PRIMARY = ["R", "B", "Y"]
COMPLEMENT = {"R": "G", "G": "R", "B": "O", "O": "B", "Y": "P", "P": "Y", "K": "W", "W": "K", "X": "X"}

COST = {"Strike": 1, "Defend": 1, "Trauma": 0, "Palette": 1, "ColorMix": 1, "Confusion": 2, "Complementary": 1,
        "InkPainting": 1, "PaintRefill": 2, "ColorBlast": 3, "SpaceOut": 3, "Zuma": 2, "PaletteGap": 1,
        "Karibarai": 1, "Kesseiseki": 2, "Sekibaku": 2, "Hyakuren": 1, "PiercingBlood": 1, "SuperNova": 2,
        "BloodDraw": 2, "Ketsujin": 2, "Shiou": 1, "Sekirin": 3, "GAtk": 1, "GBlk": 1}
BLOOD_KW = {"Karibarai", "Kesseiseki", "Sekibaku", "Hyakuren", "BloodDraw", "Ketsujin", "Shiou", "Sekirin"}
POWERS = {"PaintRefill", "SpaceOut", "Zuma", "Sekirin"}
EXHAUST = {"PiercingBlood", "SuperNova"}


class Orb:
    __slots__ = ("c", "L")
    def __init__(self, c, L=3):
        self.c = c; self.L = L


class Enemy:
    __slots__ = ("alive", "dies_at", "hp0", "dmg", "intent", "poison", "burn", "doom", "strdown")
    def __init__(self, dies_at, hp0, dmg):
        self.alive = True; self.dies_at = dies_at; self.hp0 = hp0; self.dmg = dmg
        self.intent = True; self.poison = 0; self.burn = 0; self.doom = 0; self.strdown = 0


class Combat:
    def __init__(self, deck, kind, act, rl, rng, stats=None, upg=frozenset()):
        self.rng = rng; self.kind = kind; self.act = act; self.rl = rl; self.upg = upg
        mean, shape, imp, ndist, base = ENC[kind]
        self.length = max(2, round(rng.gammavariate(shape, mean / shape)))
        n = rng.choices([a for a, _ in ndist], [w for _, w in ndist])[0]
        dmg = base * ACT_SCALE[act] / (1 + 0.35 * (n - 1))
        hp_tot = 17.0 * self.length * ACT_SCALE[act]
        self.enemies = []
        for i in range(n):
            da = max(1, round(self.length * (i + 1) / n))
            self.enemies.append(Enemy(da, hp_tot / n, dmg))
        self.imp = imp
        self.draw = list(deck); rng.shuffle(self.draw)
        self.hand = []; self.discard = []
        self.has_blood_deck = any(c in BLOOD_KW for c in deck)
        self.cap = 6 + (rl - 1) // 2
        self.orbs: list[Orb] = []
        self.block = 0; self.majoka = 0; self.energy = 0; self.turn = 0
        self.zuma = False; self.spaceout = False; self.refill = False; self.sekirin = False
        self.zuma_mult = 1.0; self.zdepth = 0
        self.dealt = 0.0; self.hp_lost = 0.0; self.heal = 0.0; self.won_spaceout = False
        self.stats = stats  # None in forks
        self.evokes = 0

    # ---------- 基本
    def st(self, k, v=1):
        if self.stats is not None:
            self.stats[k] += v

    def alive(self):
        return [e for e in self.enemies if e.alive]

    def mm(self):
        return majoka_mult(self.majoka)

    def pv(self, base):  # ModifyPaintOrbValue
        return max(1, math.floor(base * self.mm() * self.zuma_mult))

    def bv(self, base):  # blood orb value
        return max(1, math.floor(base * self.mm()))

    def hit(self, e, d):
        if e.alive and d > 0:
            self.dealt += d

    def attack(self, base, target, hits=1):
        if target is None:
            return
        d = int(base * self.mm())
        for _ in range(hits):
            self.hit(target, d)

    def target(self):
        a = self.alive()
        return a[0] if a else None

    def gain_block(self, b):
        self.block += b

    def draw_n(self, n):
        for _ in range(n):
            if not self.draw:
                if not self.discard:
                    return
                self.draw = self.discard; self.discard = []; self.rng.shuffle(self.draw)
            if len(self.hand) >= 10:
                return
            self.hand.append(self.draw.pop())

    def incoming(self):
        return sum(max(0, e.dmg - e.strdown) for e in self.alive() if e.intent)

    def colors(self):
        return len({o.c for o in self.orbs if o.c != "X"})

    # ---------- 充能球
    def roll_generated(self):
        if self.sekirin:
            return "X"
        pool = ZUMA_PRIMARY if self.zuma else PAINT
        return self.rng.choice(pool)

    def roll_rby(self):
        r = self.rng.randrange(100)
        return "R" if r < 40 else ("B" if r < 80 else "Y")

    def evoke_orb(self, o):
        self.evokes += 1
        c = o.c
        if c == "X":
            h = self.bv(3); self.heal += h
            return
        v = self.pv(5)
        a = self.alive()
        if c == "R":
            for e in a: self.hit(e, v)
        elif c == "B":
            self.gain_block(v)
        elif c == "Y":
            self.energy += v
        elif c == "W":
            self.draw_n(v)
        elif c == "K":
            self.majoka += v
        elif c == "G":
            for e in a: e.poison += v
        elif c == "O":
            for e in a: e.burn += v
        elif c == "P":
            for e in a: e.doom += v

    def passive(self, o):
        c = o.c
        if c == "X":
            o.L += 1; return
        if c in ("Y", "W"):
            v = self.pv(1)
        else:
            v = self.pv(3)
        a = self.alive()
        if c == "R":
            if a: self.hit(self.rng.choice(a), v)
        elif c == "B":
            self.gain_block(v)
        elif c == "Y":
            self.energy += v
        elif c == "W":
            self.draw_n(v)
        elif c == "K":
            self.majoka += v
        elif c == "G":
            if a: self.rng.choice(a).poison += v
        elif c == "O":
            if a: self.rng.choice(a).burn += v
        elif c == "P":
            if a: self.rng.choice(a).doom += v

    def evoke_front(self):
        if not self.orbs:
            return
        o = self.orbs.pop(0)
        self.evoke_orb(o)
        self.zuma_resolve()

    def channel(self, o, index=None):
        if len(self.orbs) >= self.cap:
            self.evoke_front()
            if index is not None and index > 0:
                index -= 1
        if index is None or index >= len(self.orbs):
            self.orbs.append(o)
        else:
            self.orbs.insert(max(0, index), o)
        self.st("_channels")
        self.zuma_resolve()

    def zuma_resolve(self):
        if not self.zuma or self.zdepth > 0:
            return
        self.zdepth += 1
        combo = 1
        for _ in range(24):
            run = self.find_run()
            if not run:
                break
            self.zuma_mult = 2.0 if combo <= 1 else (4.0 if combo == 2 else 8.0)
            cnt = 0
            for o in run:
                if o in self.orbs:
                    self.orbs.remove(o); self.evoke_orb(o); cnt += 1
            self.zuma_mult = 1.0
            self.st("_zuma_runs"); self.st("_zuma_orbs", cnt); self.st("_zuma_combo%d" % min(combo, 3))
            self.draw_n(cnt)
            combo += 1
        self.zdepth -= 1

    def find_run(self):
        o = self.orbs; i = 0
        while i < len(o):
            if o[i].c == "X":
                i += 1; continue
            j = i + 1
            while j < len(o) and o[j].c == o[i].c:
                j += 1
            if j - i >= 3:
                return o[i:j]
            i = j
        return None

    def best_gap_index(self, c):
        o = self.orbs
        best = None
        for idx in range(len(o) + 1):
            l = 0; k = idx - 1
            while k >= 0 and o[k].c == c: l += 1; k -= 1
            r = 0; k = idx
            while k < len(o) and o[k].c == c: r += 1; k += 1
            s = l + r
            if best is None or s > best[0]:
                best = (s, idx)
        return best[1] if best and best[0] > 0 else None

    def blood_orbs(self):
        return [o for o in self.orbs if o.c == "X"]

    def evoke_highest_blood(self):
        bl = self.blood_orbs()
        if not bl:
            return 0, 0
        o = max(bl, key=lambda x: x.L)
        pv = self.bv(o.L); L = o.L
        self.orbs.remove(o)
        self.evoke_orb(o)
        self.zuma_resolve()
        return pv, L

    def take_damage(self, d, unblockable=False):
        if not unblockable:
            b = min(self.block, d); self.block -= b; d -= b
        if d > 0:
            self.hp_lost += d
            if self.has_blood_deck and self.rng.random() < 0.5:
                self.st("_relic_blood")
                self.channel(Orb("X", max(1, d)))

    # ---------- 卡牌效果
    def effect(self, name, played=True):
        u = name in self.upg
        mm = self.mm
        if name == "Strike":
            self.attack(9 if u else 6, self.target())
        elif name == "GAtk":
            self.attack(9, self.target())
        elif name == "Defend":
            self.gain_block(8 if u else 5)
        elif name == "GBlk":
            self.gain_block(8)
        elif name == "Trauma":
            self.majoka += 15 if u else 10
        elif name == "Palette":
            self.gain_block(6 if u else 4); self.channel(Orb(self.roll_rby()))
        elif name == "PaletteGap":
            self.gain_block(4)
            c = self.roll_rby()
            self.channel(Orb(c), self.best_gap_index(c))
        elif name == "ColorMix":
            n = self.colors()
            m = 9 if n >= 6 else 5 if n >= 5 else 2 if n >= 4 else 1 if n >= 3 else 0
            self.st("_cm_plays"); self.st("_cm_colors", n); self.st("_cm_dmgbase", 5 + 5 * m)
            self.st("_cm_maj", mm())
            self.attack(5 + 5 * m, self.target())
        elif name == "Confusion":
            for _ in range(3 if u else 2):
                self.channel(Orb(self.rng.choice(PAINT)))
        elif name == "Complementary":
            if not self.orbs:
                self.st("_comp_empty"); return
            o = Orb(COMPLEMENT[self.orbs[-1].c])
            self.channel(o)
            if o in self.orbs:
                for _ in range(2 if u else 1):
                    self.passive(o)
        elif name == "InkPainting":
            # 白：入白球+抽2(3)；黑：入黑球+10(15)魔女化。魔女化<60 且手牌已夠時選黑
            if self.majoka < 60 and len(self.hand) >= 4:
                self.channel(Orb("K")); self.majoka += 15 if u else 10; self.st("_ink_black")
            else:
                self.channel(Orb("W")); self.draw_n(3 if u else 2); self.st("_ink_white")
        elif name == "PaintRefill":
            self.refill = True
        elif name == "SpaceOut":
            self.spaceout = True; self.cap += 2
        elif name == "Zuma":
            self.zuma = True; self.cap += 1
            for _ in range(7):
                self.draw.insert(self.rng.randrange(len(self.draw) + 1), "PaletteGap")
        elif name == "ColorBlast":
            n = len(self.orbs); self.st("_cb_plays"); self.st("_cb_orbs", n)
            for _ in range(n):
                self.evoke_front(); self.channel(Orb(self.rng.choice(PAINT)))
        elif name in ("Karibarai", "Kesseiseki", "PiercingBlood"):
            base = {"Karibarai": 12 if u else 8, "Kesseiseki": 30 if u else 20,
                    "PiercingBlood": 45 if u else 30}[name]
            if name == "PiercingBlood":
                base = 45 if "PiercingBlood+" in self.upg else 30
            bonus, L = self.evoke_highest_blood()
            self.st("_bl_plays"); self.st("_bl_has", 1 if bonus else 0); self.st("_bl_bonus", bonus)
            self.attack(base + bonus, self.target())
        elif name == "SuperNova":
            base = 6 if "SuperNova+" in self.upg else 4
            bonus, L = self.evoke_highest_blood()
            for e in self.alive():
                self.attack(base + bonus, e, hits=4)
        elif name == "Sekibaku":
            bonus, L = self.evoke_highest_blood()
            self.st("_sb_bonus", bonus)
            t = self.target()
            if t and bonus:
                t.strdown += bonus
        elif name == "Shiou":
            bonus, L = self.evoke_highest_blood()
            self.st("_sh_plays"); self.st("_sh_bonus", bonus)
            self.gain_block((12 if u else 8) + bonus)
        elif name == "Hyakuren":
            add = 3 if u else 2
            tok = "PiercingBlood" if len(self.alive()) <= 1 else "SuperNova"
            if len(self.hand) < 10:
                self.hand.append(tok)
            if u:
                self.upg = self.upg | {tok + "+"}
            for o in self.blood_orbs():
                o.L += add
        elif name == "BloodDraw":
            self.take_damage(3, unblockable=True)
            for _ in range(3 if u else 2):
                self.channel(Orb("X", 3))
        elif name == "Ketsujin":
            nb = len(self.blood_orbs()); self.st("_kj_plays"); self.st("_kj_orbs", nb)
            for o in self.blood_orbs():
                o.L += 6 if u else 4
        elif name == "Sekirin":
            self.sekirin = True

    # ---------- 出牌策略（貪婪）
    def playable(self, c):
        if COST[c] > self.energy:
            return False
        if c == "Sekibaku" and not self.blood_orbs():
            return False
        return True

    def prio(self, c):
        inc = self.incoming(); need_block = self.block < inc
        if c in POWERS:
            active = {"PaintRefill": self.refill, "SpaceOut": self.spaceout, "Zuma": self.zuma, "Sekirin": self.sekirin}[c]
            return -1 if active else 100
        if c == "Trauma": return 95
        if c == "ColorBlast": return 80 if len(self.orbs) >= self.cap - 1 else 15
        if c == "ColorMix":
            n = self.colors(); return 85 if n >= 5 else (70 if n >= 4 else 30)
        if c == "BloodDraw": return 72
        if c == "Hyakuren": return 74
        if c == "Ketsujin": return 66 if len(self.blood_orbs()) >= 2 else 12
        if c in ("PiercingBlood", "SuperNova"): return 90
        if c in ("Kesseiseki", "Karibarai"):
            return 68 if self.blood_orbs() else 45
        if c == "Sekibaku": return 64 if inc > 0 else 5
        if c in ("Confusion", "Complementary", "InkPainting"): return 60
        if c in ("Palette", "PaletteGap"): return 63 if need_block else 58
        if c in ("Shiou", "Defend", "GBlk"):
            return 62 if need_block else 8
        if c in ("Strike", "GAtk"): return 50
        return 10

    def play_phase(self):
        while True:
            cands = [c for c in self.hand if self.playable(c)]
            if not cands or not self.alive():
                return
            c = max(cands, key=lambda x: (self.prio(x), -COST[x]))
            if self.prio(c) < 0:
                return
            self.hand.remove(c)
            self.energy -= COST[c]
            self.play_hook(c)
            self.effect(c)
            if c in POWERS or c in EXHAUST:
                pass
            else:
                self.discard.append(c)

    def play_hook(self, c):
        if self.stats is not None:
            self.stats["play_" + c] += 1
            if c in BLOOD_KW or c in ("PiercingBlood", "SuperNova"):
                self.stats["bloodpresent_" + c] += 1 if self.blood_orbs() else 0
            self.stats["colors_at_" + c] += self.colors()
            self.stats["orbs_at_" + c] += len(self.orbs)
            self.stats["maj_at_" + c] += self.majoka

    # ---------- 回合流程
    def start_turn(self):
        self.turn += 1
        self.block = 0
        for e in self.alive():
            e.intent = self.rng.random() < 0.65
            e.strdown = 0
        self.energy = 3
        self.draw_n(5)
        for o in list(self.orbs):  # AfterTurnStart：黃、白、黑
            if o.c in ("Y", "W", "K") and o in self.orbs:
                self.passive(o)
        k = 3 if self.rl >= 5 else (2 if self.rl >= 3 else 1)
        for _ in range(k):
            self.channel(Orb(self.roll_generated()))
        if self.spaceout:
            self.channel(Orb(self.rng.choice(PAINT)))
        self.st("_turns"); self.st("_maj_turn", self.majoka); self.st("_orbs_turn", len(self.orbs))
        self.st("_colors_turn", self.colors())

    def end_turn(self):
        self.discard.extend(self.hand); self.hand = []
        for o in list(self.orbs):  # BeforeTurnEnd：紅藍綠橘紫、血
            if o.c in ("R", "B", "G", "O", "P", "X") and o in self.orbs:
                self.passive(o)
        if self.refill:
            n = 0
            while len(self.orbs) < self.cap and n < 64:
                self.channel(Orb(self.roll_generated())); n += 1
            self.st("_refill_orbs", n)
        if self.sekirin:
            for o in self.blood_orbs():
                o.L += 1
            res = []
            for _ in range(2):
                pv, L = self.evoke_highest_blood()
                if not L:
                    break
                res.append(L)
            for L in res:
                self.channel(Orb("X", L))
        if self.spaceout:
            if self.colors() >= 8:
                self.won_spaceout = True; self.st("_spaceout_win")
                rem = self.length - self.turn
                # 直接勝利：剩餘敵人血量視為傷害
                for e in self.alive():
                    hp_left = e.hp0 * max(0.0, (e.dies_at - self.turn) / e.dies_at)
                    self.dealt += hp_left; e.alive = False
                return
        # 末日：玩家回合結束時判定
        for e in self.alive():
            hp_left = e.hp0 * max(0.0, (e.dies_at - self.turn + 0.5) / e.dies_at)
            if e.doom > 0 and e.doom >= hp_left:
                self.dealt += hp_left; e.alive = False; self.st("_doom_kill")

    def enemy_turn(self):
        for e in self.alive():
            if e.poison > 0:
                self.hit(e, e.poison); e.poison -= 1
            if e.burn > 0:
                self.hit(e, e.burn)
        for e in self.alive():
            if e.intent:
                self.take_damage(max(0, e.dmg - e.strdown))
        for e in self.enemies:
            if e.alive and e.dies_at <= self.turn:
                e.alive = False

    def run_turns(self, upto=None):
        while self.turn < self.length and self.alive():
            if upto is not None and self.turn >= upto:
                return
            self.start_turn()
            self.play_phase()
            self.end_turn()
            if not self.alive():
                break
            self.enemy_turn()

    def finish_from_play(self):
        """從出牌階段中段繼續到戰鬥結束。"""
        self.play_phase()
        self.end_turn()
        if self.alive():
            self.enemy_turn()
        self.run_turns()

    def utility(self):
        return self.dealt / DMG_PER_E - self.hp_lost / HP_PER_E + self.heal * HEAL_PER_HP


START = ["Strike"] * 3 + ["Defend"] * 3 + ["Trauma", "Palette", "Palette", "ColorMix"]
DECKS = {
    "paint": START + ["Confusion", "Confusion", "Complementary", "Complementary", "InkPainting", "PaintRefill",
                      "ColorMix", "ColorBlast", "SpaceOut", "GAtk", "GAtk", "GBlk"],
    "zuma": START + ["Zuma", "Confusion", "Complementary", "Complementary", "PaintRefill", "ColorMix",
                     "InkPainting", "GAtk", "GAtk", "GAtk", "GBlk", "GBlk"],
    "blood": START + ["BloodDraw", "BloodDraw", "Karibarai", "Karibarai", "Kesseiseki", "Shiou", "Shiou",
                      "Ketsujin", "Sekibaku", "Hyakuren", "Sekirin", "GAtk", "GBlk"],
}

# 量測的效果（卡名, 是否升級）
PROBES = {
    "paint": ["Palette", "ColorMix", "Confusion", "Complementary", "InkPainting", "PaintRefill", "ColorBlast",
              "SpaceOut", "Zuma", "Trauma", "Strike", "Defend", "_orb_rand", "_orb_rby"],
    "zuma": ["Palette", "ColorMix", "Confusion", "Complementary", "InkPainting", "PaintRefill", "ColorBlast",
             "SpaceOut", "Zuma", "Trauma", "Strike", "_orb_rand", "PaletteGap"],
    "blood": ["Karibarai", "Kesseiseki", "Sekibaku", "Hyakuren", "BloodDraw", "Ketsujin", "Shiou", "Sekirin",
              "Palette", "ColorMix", "Confusion", "Complementary", "PaintRefill", "ColorBlast", "Strike", "_orb_rand"],
}
UPG_PROBES = {"Palette", "Confusion", "Complementary", "InkPainting", "Karibarai", "Kesseiseki", "Hyakuren",
              "BloodDraw", "Ketsujin", "Shiou", "Trauma", "Strike", "Defend"}


def apply_probe(cb: Combat, p, up):
    if up:
        cb.upg = cb.upg | {p}
    if p == "_orb_rand":
        cb.channel(Orb(cb.rng.choice(PAINT)))
    elif p == "_orb_rby":
        cb.channel(Orb(cb.roll_rby()))
    else:
        cb.effect(p)


def sample_fight(rng):
    i = rng.randrange(len(RUN))
    act, kind = RUN[i]
    return act, kind, relic_level(RUN_EXP[i])


def run_arch(arch, n_base=20000, n_probe=4000, seed=1):
    deck = DECKS[arch]
    rng = random.Random(seed)
    stats = defaultdict(float)
    wsum = 0.0
    lv_hist = defaultdict(float)
    for _ in range(n_base):
        act, kind, rl = sample_fight(rng)
        cb = Combat(deck, kind, act, rl, random.Random(rng.random()), stats)
        cb.run_turns()
        stats["_fights"] += 1
        stats["_len"] += cb.length
        stats["_hp_lost"] += cb.hp_lost
        lv_hist[rl] += 1
    # 探針
    probe_res = defaultdict(lambda: [0.0, 0.0, 0.0])  # sum_w*val, sum_w, sum_w*devokes
    for _ in range(n_probe):
        act, kind, rl = sample_fight(rng)
        seed2 = rng.random()
        cb = Combat(deck, kind, act, rl, random.Random(seed2))
        t = rng.randint(1, min(4, cb.length))
        cb.run_turns(upto=t - 1)
        if not cb.alive() or cb.turn >= cb.length:
            continue
        cb.start_turn()
        w = cb.imp
        snap = copy.deepcopy(cb)
        fork_seed = rng.random()
        base = copy.deepcopy(snap); base.rng = random.Random(fork_seed)
        base.finish_from_play()
        ub = base.utility(); eb = base.evokes
        for p in PROBES[arch]:
            for up in ((False, True) if p in UPG_PROBES else (False,)):
                a = copy.deepcopy(snap); a.rng = random.Random(fork_seed)
                apply_probe(a, p, up)
                a.finish_from_play()
                key = p + ("+" if up else "")
                r = probe_res[key]
                r[0] += w * (a.utility() - ub); r[1] += w; r[2] += w * (a.evokes - eb)
    out = {"arch": arch, "fights": stats["_fights"],
           "avg_len": stats["_len"] / stats["_fights"],
           "relic_lv_hist": {k: v / n_base for k, v in sorted(lv_hist.items())},
           "stats": {}, "probes": {}}
    S = stats
    def ratio(a, b):
        return round(S[a] / S[b], 3) if S[b] else None
    out["stats"] = {
        "avg_orbs_turnstart": ratio("_orbs_turn", "_turns"),
        "avg_colors_turnstart": ratio("_colors_turn", "_turns"),
        "avg_majoka_turnstart": ratio("_maj_turn", "_turns"),
        "colormix_colors": ratio("_cm_colors", "_cm_plays"),
        "colormix_basedmg": ratio("_cm_dmgbase", "_cm_plays"),
        "colormix_majmult": ratio("_cm_maj", "_cm_plays"),
        "colorblast_orbs": ratio("_cb_orbs", "_cb_plays"),
        "blood_attack_has_orb": ratio("_bl_has", "_bl_plays"),
        "blood_attack_bonus": ratio("_bl_bonus", "_bl_plays"),
        "shiou_bonus": ratio("_sh_bonus", "_sh_plays"),
        "sekibaku_bonus": ratio("_sb_bonus", "play_Sekibaku"),
        "ketsujin_orbs": ratio("_kj_orbs", "_kj_plays"),
        "relic_blood_per_fight": ratio("_relic_blood", "_fights"),
        "zuma_runs_per_fight": ratio("_zuma_runs", "_fights"),
        "zuma_orbs_per_run": ratio("_zuma_orbs", "_zuma_runs"),
        "zuma_combo2_per_fight": ratio("_zuma_combo2", "_fights"),
        "spaceout_win_per_fight": ratio("_spaceout_win", "_fights"),
        "spaceout_win_given_played": ratio("_spaceout_win", "play_SpaceOut"),
        "refill_orbs_per_turn": ratio("_refill_orbs", "_turns"),
        "doom_kills_per_fight": ratio("_doom_kill", "_fights"),
        "ink_black_share": ratio("_ink_black", "play_InkPainting"),
        "hp_lost_per_fight": ratio("_hp_lost", "_fights"),
        "complementary_empty": ratio("_comp_empty", "play_Complementary"),
    }
    for k in list(S):
        if k.startswith("play_"):
            c = k[5:]
            if not S[k]:
                continue
            out["stats"]["plays_per_fight_" + c] = round(S[k] / S["_fights"], 3)
            if S.get("bloodpresent_" + c) is not None and ("bloodpresent_" + c) in S:
                out["stats"]["bloodpresent_" + c] = round(S["bloodpresent_" + c] / S[k], 3)
            out["stats"]["colors_at_" + c] = round(S["colors_at_" + c] / S[k], 2)
            out["stats"]["maj_at_" + c] = round(S["maj_at_" + c] / S[k], 1)
    for k, (sv, sw, se) in probe_res.items():
        out["probes"][k] = {"value": round(sv / sw, 3), "d_evokes": round(se / sw, 3)}
    return out


if __name__ == "__main__":
    nb = int(sys.argv[1]) if len(sys.argv) > 1 else 20000
    npb = int(sys.argv[2]) if len(sys.argv) > 2 else 4000
    res = {a: run_arch(a, nb, npb, seed=11 + i) for i, a in enumerate(DECKS)}
    with open(__file__.replace("sim.py", "results.json"), "w", encoding="utf-8") as f:
        json.dump(res, f, ensure_ascii=False, indent=1)
    print(json.dumps(res, ensure_ascii=False, indent=1))
