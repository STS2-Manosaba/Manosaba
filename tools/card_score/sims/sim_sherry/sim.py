"""橘雪莉簡化戰鬥蒙地卡羅模型（v5）。規則依 Manosaba 原始碼：
- 放大鏡 Lv1：戰鬥開始 1 力量；調查時刻每回合開始 +1 進度，每 5 層疑點額外 +1 進度並 +1 力量；
  進度滿 5：依疑點數對隨機敵人造成傷害、清空疑點、神探當賞 +2（每回合結束 -1）。
  回合開始清空魔女化；力量每點受擊減傷 5%（上限 30%）。
- 九：神探當賞時 0 費，否則 9 費；六：本場打出過三後 0 費；三：3 - 本回合獲得疑點(上限 3)。
- 每回合 5 抽、3 能量；敵人用固定血量／傷害曲線。
"""
from __future__ import annotations
import random, math
from collections import defaultdict

ENC = {  # 出現比例, 總血量範圍, 敵人數範圍, 基礎傷害, 每回合成長, 每敵每回合攻擊段數, 敵力量成長(每回合)
    "hallway": (0.72, (55, 80), (1, 3), 8.0, 1.0, 1.0, 0.0),
    "elite":   (0.20, (140, 175), (1, 2), 11.0, 1.2, 1.5, 0.25),
    "boss":    (0.08, (440, 520), (1, 1), 10.0, 1.1, 2.0, 0.5),
}
IMP = {"hallway": 1.0, "elite": 2.0, "boss": 3.0}

ATTACKS = {"Strike", "LeftPunch", "RightPunch", "MurderCase", "PrisonBreak", "GoodMorningSherry", "Ignite",
           "KyuMiraclePartners", "RokuBraveFace", "SanFindsClue", "DoublePunch", "BurnTheHouseDown", "HangingKill",
           "IceBall", "SuperPunch", "UltimateStrike"}
POWERS = {"HorsebackRiding", "LittleFairy", "QuickWit", "SearchAnalyzeSolve", "GiveItATry", "DirtyEhe", "YouAreThis"}
BASE_COST = {"Strike": 1, "Defend": 1, "LeftPunch": 1, "RightPunch": 1, "MurderCase": 1, "PrisonBreak": 1, "Cotton": 0,
             "BigInvestigation": 1, "MasterKey": 1, "BrokenLock": 0, "Crossbow": 2, "BrokenCrossbow": 0, "DontRush": 1,
             "GoodMorningSherry": 1, "HorsebackRiding": 1, "IceCube": 1, "IceBall": 0, "Ignite": 2,
             "KyuMiraclePartners": 9, "LittleFairy": 2, "RokuBraveFace": 6, "SanFindsClue": 3, "SixSeven": 0,
             "TruthIsOnlyOne": 1, "UnsteadyLegs": 1, "WhatDoYouMean": 1, "YouAreThis": 2, "DoublePunch": 1,
             "Achuba": 2, "BerserkerSoul": 2, "QuickWit": 2, "SearchAnalyzeSolve": 1, "GiveItATry": 3,
             "SuperPunch": 0, "DirtyEhe": 3, "DateTime": 3, "FryFryFry": 1, "UltimateStrike": 1}
EXHAUST = {"Cotton", "BrokenLock", "Crossbow", "BrokenCrossbow", "IceBall", "SixSeven", "BerserkerSoul",
           "DateTime", "FryFryFry", "UltimateStrike"}
ETHEREAL = {"DontRush"}
RETAIN = {"BrokenLock", "BrokenCrossbow"}

STARTER = ["Strike"] * 4 + ["Defend"] * 3 + ["LeftPunch", "RightPunch", "MurderCase"]
ARCH = {
    # 疑點／神探當賞型
    "clue": STARTER + ["PrisonBreak", "PrisonBreak", "Cotton", "BigInvestigation", "TruthIsOnlyOne",
                       "HorsebackRiding", "GoodMorningSherry", "IceCube", "MasterKey", "DontRush"],
    # 力量型
    "strength": STARTER + ["UnsteadyLegs", "YouAreThis", "Ignite", "DoublePunch", "SixSeven", "Crossbow",
                           "PrisonBreak", "Defend", "GoodMorningSherry"],
    # 三六九連段型
    "combo": STARTER + ["PrisonBreak", "PrisonBreak", "Cotton", "SanFindsClue", "RokuBraveFace",
                        "TruthIsOnlyOne", "MasterKey", "IceCube", "UnsteadyLegs"],
}


class Enemy:
    __slots__ = ("hp", "str", "vuln", "weak", "dontrush", "achuba")

    def __init__(self, hp):
        self.hp = hp; self.str = 0.0; self.vuln = 0; self.weak = 0; self.dontrush = 0; self.achuba = False


class Combat:
    def __init__(self, deck, kind, rng, stats, probe=None):
        self.rng = rng; self.kind = kind; self.stats = stats; self.probe = probe
        _, hpr, nr, self.bdmg, self.grow, self.hits, self.sgrow = ENC[kind]
        n = rng.randint(*nr); tot = rng.uniform(*hpr)
        self.enemies = [Enemy(tot / n) for _ in range(n)]
        self.draw = list(deck); rng.shuffle(self.draw)
        if self.probe in ("QuickWit+", "SearchAnalyzeSolve+"):
            nm = self.probe[:-1]
            if nm in self.draw:
                self.draw.remove(nm); self.draw.append(nm)  # 固有：置頂
        self.hand = []; self.disc = []; self.exh = []
        self.hp = 66.0; self.block = 0.0; self.str = 1.0; self.dex = 0.0; self.clue = 0; self.inv = 0
        self.reward = 0; self.majoka = 0; self.majoka_turn = False
        self.cluesturn = 0; self.san_played = False; self.fairy = 0; self.fairy_credit = 0
        self.quickwit = 0; self.horse = 0; self.thorns = 0; self.selfvuln = 0; self.plating = 0
        self.sas = 0; self.sas_cd = 0; self.sas_cdlen = 3; self.tempstr = 0
        self.turn = 0; self.energy = 0; self.dmg_dealt = 0.0; self.dmg_taken = 0.0
        self.str_gained = 0.0; self.ice_exh = 0

    # ---------- 資源 ----------
    def alive(self):
        return [e for e in self.enemies if e.hp > 0]

    def gain_str(self, n):
        if n == 0:
            return
        self.str += n
        if n > 0:
            self.str_gained += n
            if self.reward > 0:
                self.block += 1
            if self.fairy:
                self.fairy_credit += int(n)
                while self.fairy_credit >= 5:
                    self.fairy_credit -= 5; self.energy += self.fairy
                    self.stats["fairy_energy"] += self.fairy if self.probe == "LittleFairy" else 0

    def gain_clue(self, n):
        if n <= 0:
            return
        old = self.clue; self.clue += n; self.cluesturn += n
        for _ in range(self.clue // 5 - old // 5):
            self.gain_str(1); self.advance_inv()

    def advance_inv(self):
        self.inv += 1
        while self.inv >= 5:
            self.inv -= 5
            al = self.alive()
            if al and self.clue > 0:
                t = self.rng.choice(al); d = min(t.hp, self.clue); t.hp -= self.clue; self.dmg_dealt += d
                self.stats["inv_dmg"] += d
            self.clue = 0
            self.reward += 2
            self.stats["reward_triggers"] += 1
            if self.stats.get("_first") is None:
                self.stats["_first"] = 1
                self.stats["first_reward_turn_sum"] += self.turn; self.stats["first_reward_n"] += 1

    def str_now(self):
        return self.str + self.tempstr

    def majoka_mult(self):
        s = self.majoka
        if s <= 0:
            return 1.0
        r = s ** 1.8365 / (s ** 1.8365 + 166.6 ** 1.8365)
        return 1 + 3 * r

    def hit(self, e, base, times=1, powered=True):
        dealt = 0.0
        for _ in range(times):
            if e.hp <= 0:
                break
            d = base + (self.str_now() if powered else 0)
            d = max(0.0, d)
            if powered:
                d *= self.majoka_mult()
                if e.achuba and self.str_now() > e.str:
                    d *= 1.2
                    if self.probe == "Achuba":
                        self.stats["achuba_bonus"] += d - d / 1.2
            if e.vuln > 0:
                d *= 1.5
            d = math.floor(d)
            a = min(d, e.hp); e.hp -= d; dealt += a
        self.dmg_dealt += dealt
        return dealt

    def target(self):
        al = self.alive()
        for e in al:
            if e.achuba:
                return e
        return min(al, key=lambda e: e.hp) if al else None

    def aoe(self, base, powered=True):
        return sum(self.hit(e, base, 1, powered) for e in list(self.alive()))

    def incoming(self):
        t = self.turn
        tot = 0.0
        for e in self.alive():
            per = (self.bdmg + self.grow * (t - 1)) / len(self.enemies)
            per = max(0, per + e.str - e.dontrush)
            if e.weak > 0:
                per *= 0.75
            tot += per
        mult = 1 - min(self.str * 0.05, 0.3)
        if self.selfvuln > 0:
            mult *= 1.5
        return tot * mult

    # ---------- 費用 ----------
    def cost(self, c):
        b = BASE_COST[c]
        if c == "SanFindsClue":
            return max(0, b - min(3, self.cluesturn))
        if c == "RokuBraveFace" and self.san_played:
            return 0
        if c == "KyuMiraclePartners" and self.reward > 0:
            return 0
        if c == "BerserkerSoul" and self.str_now() >= 16:
            return 99
        if self.slither.get(id(c)) is not None:
            return b
        return b

    slither = {}

    # ---------- 啟發式價值 ----------
    def h(self, c):
        al = self.alive()
        if not al:
            return -1
        inc = max(0.0, self.incoming() - self.block)
        s = self.str_now(); rw = self.reward > 0
        blk = lambda b: min(b, inc) / 5 + 0.05
        dm = lambda d: min(d, self.target().hp) / 6
        if c == "Strike": return dm(6 + s)
        if c == "Defend": return blk(5 + self.dex)
        if c == "LeftPunch": return dm(6 + s) + 1.2
        if c == "RightPunch": return sum(min(4 + s, e.hp) for e in al) / 6 + 1.2
        if c == "MurderCase": return dm(6 + s) + 0.35
        if c == "PrisonBreak": return dm(6 + s) + 0.9
        if c == "Cotton": return 0.6
        if c == "BigInvestigation": return 0.6 + (2.6 if rw else 0)
        if c == "MasterKey": return blk(5 + self.dex) + 1.0 + (1.2 if rw else 0)
        if c == "BrokenLock": return 1.2
        if c == "Crossbow": return 2.6
        if c == "BrokenCrossbow": return 2.0 if self.energy <= 1 and len(self.hand) > 1 else 0.3
        if c == "DontRush": return min(7, inc) / 5 * 0.8 + (2.6 if rw else 0) + 0.1
        if c == "GoodMorningSherry": return dm(15 + s + (5 if rw else 0)) - 0.2
        if c in ("HorsebackRiding",): return 1.5 + 0.1 * max(0, 8 - self.turn)
        if c == "IceCube": return 0.9 + (2.6 if rw else 0)
        if c == "IceBall": return dm(6 + 2 * self.ice_exh + s)
        if c == "Ignite": return sum(min(11 + s, e.hp) for e in al) / 6 + 1.6
        if c == "KyuMiraclePartners": return sum(min(14 + s, e.hp) for e in al) / 6
        if c == "LittleFairy": return 1.8 + 0.1 * max(0, 8 - self.turn)
        if c == "RokuBraveFace": return dm(24 + s)
        if c == "SanFindsClue": return sum(min(12 + s, e.hp) for e in al) / 6 + 1.2 + (1.5 if not self.san_played else 0)
        if c == "SixSeven": return 1.1
        if c == "TruthIsOnlyOne": return blk(self.clue + 4) + 0.9
        if c == "UnsteadyLegs": return 2.2
        if c == "WhatDoYouMean": return 0.6 + (2.6 if rw else 0)
        if c == "YouAreThis": return 4.0
        if c == "DoublePunch": return dm((5 + s) * (1 + min(5, max(0, int(s)))))
        if c == "Achuba": return 1.6 if not any(e.achuba for e in al) else 0.9
        if c == "BerserkerSoul":
            return 2.0 if len(self.hand) <= 2 else 0.8
        if c == "QuickWit": return 3.0 + 0.1 * max(0, 8 - self.turn)
        if c == "SearchAnalyzeSolve": return 2.5
        if c == "GiveItATry": return 4.0 if self.energy == BASE_COST["GiveItATry"] or self.energy >= 3 else 1.0
        if c == "SuperPunch": return sum(min((8 + s + (15 if self.majoka >= 100 else 0)) * self.majoka_mult(), e.hp) for e in al) / 6
        if c == "DirtyEhe": return 2.5 if self.turn <= 4 else 1.0
        if c == "DateTime": return 0.6
        if c == "FryFryFry": return 0.5
        if c == "UltimateStrike": return dm(14 + s)
        return 0.1

    # ---------- 打出 ----------
    def play(self, c):
        st = self.stats; rw = self.reward > 0; s = self.str_now()
        if self.probe and c == self.probe.rstrip("+"):
            st["probe_plays"] += 1; st["probe_reward"] += rw; st["probe_str"] += s; st["probe_clue"] += self.clue
            st["probe_turn"] += self.turn; st["probe_cost_paid"] += self.cost(c)
        if c in ATTACKS and c != "SuperPunch":
            pass
        t = self.target()
        if c == "Strike": self.hit(t, 6)
        elif c == "Defend": self.block += 5 + self.dex
        elif c == "LeftPunch": self.hit(t, 6); self.gain_str(1)
        elif c == "RightPunch": self.aoe(4); self.gain_str(1)
        elif c == "MurderCase": self.hit(t, 6); self.gain_clue(1)
        elif c == "PrisonBreak": self.hit(t, 6); self.draw_n(1); self.gain_clue(1)
        elif c == "Cotton":
            self.exhaust_worst(); self.gain_clue(2)
        elif c == "BigInvestigation":
            t.vuln += 1; self.gain_clue(1)
            if self.reward > 0: self.gain_str(2)
        elif c == "MasterKey":
            self.hand.append("BrokenLock"); self.block += 5 + self.dex
            if self.reward > 0: self.gain_str(1)
        elif c == "BrokenLock": self.draw_n(2)
        elif c == "Crossbow": self.hand.append("BrokenCrossbow"); self.gain_str(1)
        elif c == "BrokenCrossbow": self.energy += 2
        elif c == "DontRush":
            al = self.alive(); max(al, key=lambda e: self.bdmg).dontrush += 7
            if self.reward > 0: self.gain_str(2)
        elif c == "GoodMorningSherry":
            self.hp -= 2; self.hit(t, 15 + (5 if self.reward > 0 else 0))
        elif c == "HorsebackRiding": self.horse += 1
        elif c == "IceCube":
            self.hand.append("IceBall")
            if self.reward > 0: self.gain_str(2)
        elif c == "IceBall": self.hit(t, 6 + 2 * self.ice_exh); self.ice_exh += 1
        elif c == "Ignite": self.aoe(11); self.gain_str(1)
        elif c == "KyuMiraclePartners":
            d = self.aoe(14)
            if self.probe == "KyuMiraclePartners": st["kyu_dmg"] += d
        elif c == "LittleFairy": self.fairy += 1
        elif c == "RokuBraveFace":
            d = self.hit(t, 24)
            if self.probe == "RokuBraveFace": st["roku_dmg"] += d
        elif c == "SanFindsClue": self.aoe(12); self.gain_str(1); self.san_played = True
        elif c == "SixSeven":
            r = self.rng.randrange(100)
            if r < 67: self.gain_str(1)
            elif r < 80: self.gain_clue(1)
            elif r < 93: self.dex += 1
            elif r < 99: self.plating += 7
            else: self.block += 67
        elif c == "TruthIsOnlyOne": self.gain_clue(4); self.block += self.clue
        elif c == "UnsteadyLegs": self.hp -= 2; self.gain_str(2)
        elif c == "WhatDoYouMean":
            t.weak += 1
            if self.reward > 0: self.gain_str(2)
            self.exhaust_worst()
        elif c == "YouAreThis": self.gain_str(3); self.gain_clue(2)
        elif c == "DoublePunch":
            n = 1 + min(5, max(0, int(self.str_now())))
            d = self.hit(t, 5, n)
            if self.probe == "DoublePunch": st["dp_hits"] += n; st["dp_dmg"] += d
        elif c == "Achuba":
            tgt = max(self.alive(), key=lambda e: e.hp)
            if not any(e.achuba for e in self.alive()):
                tgt.achuba = True
            self.gain_str(1)
        elif c == "BerserkerSoul":
            for x in self.hand: self.disc.append(x)
            self.hand = []
            n = 0; d0 = self.dmg_dealt
            for _ in range(7):
                if not self.alive(): break
                x = self.draw_one()
                if x is None: break
                if x not in ATTACKS:
                    self.hand.append(x); break
                n += 1
                self.hand.append(x); self.hand.remove(x); self.resolve(x, free=True)
            if self.probe == "BerserkerSoul":
                st["bs_chain"] += n; st["bs_dmg"] += self.dmg_dealt - d0
        elif c == "QuickWit": self.quickwit += 1
        elif c == "SearchAnalyzeSolve":
            if self.sas: self.sas_cdlen = max(1, self.sas_cdlen - 1); self.sas_cd = max(0, self.sas_cd - 1)
            self.sas = 1
        elif c == "GiveItATry":
            if self.majoka < 100:
                self.majoka = 100
                if self.probe == "GiveItATry": st["gat_majoka100"] += 1
                if "SuperPunch" not in self.deck_all():
                    self.hand.append("SuperPunch")
        elif c == "SuperPunch":
            big = self.majoka >= 100
            d = self.aoe(8 + (15 if big else 0))
            if big: self.gain_str(5)
            if self.probe in ("SuperPunch", "GiveItATry"):
                st["sp_plays"] += 1; st["sp_big"] += big; st["sp_dmg"] += d
        elif c == "DirtyEhe":
            self.hp -= 2; self.thorns += 10; self.selfvuln += 1
        elif c == "DateTime":
            if self.majoka <= 0:
                h = 5 + max(0, self.str_now()); self.hp += h
                if self.probe == "DateTime": st["heal"] += h
        elif c == "FryFryFry":
            h = 2 + self.clue; self.hp += h
            if self.probe == "FryFryFry": st["heal"] += h
        elif c == "UltimateStrike": self.hit(t, 14)

    def deck_all(self):
        return self.hand + self.draw + self.disc + self.exh

    def resolve(self, c, free=False):
        self.play(c)
        if c in POWERS:
            return
        (self.exh if c in EXHAUST else self.disc).append(c)

    def exhaust_worst(self):
        if not self.hand:
            return
        prio = ["Defend", "Strike", "DontRush", "FryFryFry", "MurderCase"]
        for p in prio:
            if p in self.hand:
                self.hand.remove(p); self.exh.append(p); return

    def draw_one(self):
        if not self.draw:
            self.draw = self.disc; self.disc = []; self.rng.shuffle(self.draw)
        if not self.draw:
            return None
        return self.draw.pop()

    def draw_n(self, n):
        for _ in range(n):
            if len(self.hand) >= 10: return
            x = self.draw_one()
            if x is None: return
            self.hand.append(x)

    # ---------- 事件牌（難道說？） ----------
    TOKENS = [("PrisonFood", 6, -1.2), ("Meruru", 8, 2.5), ("EqualTreat", 6, -2.0), ("HandErase", 6, -0.6),
              ("Emma", 8, 0.6), ("CombatFeelsGood", 8, 1.6), ("BigFists", 8, None), ("Slither", 7, 0.3),
              ("Leia", 8, 0.15), ("EquivExchange", 1, -6.0), ("TruthRevealed", 1, 3.0), ("Fridge", 8, 1.0),
              ("Warden", 7, -0.8), ("CrimeScene", 8, 0.05)]

    def use_sas(self):
        ws = [w for _, w, _ in self.TOKENS]
        opts = self.rng.choices(self.TOKENS, ws, k=2)
        natt = sum(1 for x in self.hand if x in ATTACKS)

        def val(o):
            if o[0] == "BigFists":
                return 5 * min(natt, 3) / 6
            return o[2]
        best = max(opts, key=val)
        v = val(best)
        self.stats["sas_uses"] += 1; self.stats["sas_val"] += v
        n = best[0]
        if n == "Fridge": self.energy += 1
        elif n == "Emma": self.draw_n(1)
        elif n == "BigFists": self.tempstr += 5
        elif n == "CombatFeelsGood": self.hand.append("UltimateStrike")
        elif n == "Meruru": self.hp += 10
        elif n == "EqualTreat": self.hp -= 10
        elif n == "TruthRevealed":
            for e in self.alive(): e.vuln += 1
        elif n == "CrimeScene": self.majoka += 10
        self.sas_cd = self.sas_cdlen

    # ---------- 回合 ----------
    def run(self):
        st = self.stats
        while self.turn < 30:
            self.turn += 1
            t = self.turn
            self.block = 0.0 + self.plating; self.energy = 3; self.cluesturn = 0; self.tempstr = 0
            self.majoka = 0  # 放大鏡：回合開始清空魔女化
            self.draw_n(5)
            self.advance_inv()
            if self.horse: self.gain_clue(self.horse)
            for _ in range(self.quickwit):
                rw = self.reward > 0
                sg = 2 if rw else self.rng.randint(-2, 2)
                dx = 2 if rw else self.rng.randint(-2, 2)
                hp = 2 if rw else self.rng.randint(-2, 2)
                cl = 2 if rw else self.rng.randint(0, 2)
                if sg >= 0: self.gain_str(sg)
                else: self.str += sg
                self.dex += dx; self.hp += hp; self.gain_clue(cl)
                if self.probe and self.probe.startswith("QuickWit"):
                    st["qw_str"] += sg; st["qw_dex"] += dx; st["qw_hp"] += hp; st["qw_clue"] += cl; st["qw_turns"] += 1
                    st["qw_maxroll"] += rw
            if self.sas:
                if self.sas_cd > 0:
                    self.sas_cd -= 1
                if self.sas_cd <= 0 and self.alive():
                    self.use_sas()
            # 統計：神探當賞啟用回合
            st["turns"] += 1; st["reward_turns"] += self.reward > 0
            # 出牌
            while self.alive():
                best = None; bv = 0.0
                for c in set(self.hand):
                    k = self.cost(c)
                    if k > self.energy: continue
                    v = self.h(c)
                    sc = v / max(k, 0.6)
                    if v > 0.15 and sc > bv: bv, best = sc, c
                if best is None: break
                self.energy -= self.cost(best); self.hand.remove(best)
                if self.probe and best == "KyuMiraclePartners": pass
                self.resolve(best)
            if self.probe in ("KyuMiraclePartners", "RokuBraveFace"):
                if self.probe in self.hand: st["probe_stuck"] += 1
            if not self.alive():
                break
            # 回合結束
            if self.reward > 0: self.reward -= 1
            keep = []
            for c in self.hand:
                if c in RETAIN: keep.append(c)
                elif c in ETHEREAL: self.exh.append(c)
                else: self.disc.append(c)
            self.hand = keep
            self.str_end = None
            # 敵人回合
            inc = self.incoming()
            if self.selfvuln > 0: self.selfvuln -= 1
            dmg = max(0.0, inc - self.block)
            self.hp -= dmg; self.dmg_taken += dmg
            if self.thorns:
                for e in self.alive():
                    if self.rng.random() < 0.75:
                        nh = max(1, round(self.hits))
                        d = min(e.hp, self.thorns * nh); e.hp -= d
                        if self.probe == "DirtyEhe": st["thorns_dmg"] += d
            for e in self.alive():
                e.vuln = max(0, e.vuln - 1); e.weak = max(0, e.weak - 1); e.dontrush = 0; e.str += self.sgrow
            if self.hp <= 0:
                st["deaths"] += 1; break
        st["len"] += self.turn; st["fights"] += 1
        st["str_end"] += self.str; st["str_gained"] += self.str_gained
        st["dmg_taken"] += self.dmg_taken
        st.pop("_first", None)
        st["str_ge5_turn"] += 0


def run(deck, n, seed, probe=None, kind=None):
    rng = random.Random(seed)
    stats = defaultdict(float); by = {k: defaultdict(float) for k in ENC}
    kinds = list(ENC); ws = [ENC[k][0] for k in kinds]
    for i in range(n):
        k = kind or rng.choices(kinds, ws)[0]
        s = by[k]
        Combat(deck, k, rng, s, probe).run()
    return by


