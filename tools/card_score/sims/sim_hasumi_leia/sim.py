"""蓮見蕾雅簡化戰鬥蒙地卡羅模型（v5）。規則依 Manosaba 原始碼：
- 細劍 Rapier（等級 1~5，反擊比例 60%+10%/級）：受到強化攻擊且有格檔吸收時，對攻擊者造成 ceil(吸收量×比例) 傷害，
  RapierMastery 每層多打一次。等級依經驗表 [0,2,7,15,30]（一般+1/菁英+2/首領+3）換算整局分布。
- 鑄矛 ForgeSpear（1 費消耗）：箭/掃帚/緞帶三零件洗入抽牌堆隨機位置；零件不可打出、保留；三件同在手牌即合成
  簡易長矛（0 費 50 傷消耗，SpearMastery 時 75）。SmartSpear：抽到零件時再從抽牌堆拉 N 張零件（優先缺的種類）。
  PortableFletchingStation：合成時每層 50% 多一支長矛。GateOfBabylon：自動打出消耗堆所有簡易長矛。
  UnlimitedSpearWork：每回合開始手牌 +1 虛無鑄矛。
- 劍技：打出劍技後，手牌中的 FinalAttack 自動打出（免費）；SmoothMove 抽 N；StrongSake 下回合 +N 能量。
- 二刀流：非二刀流劍技攻擊每次命中追加 ceil(30%) 無強化傷害；TreeBranch 本回合二刀流；CeremonialSword 永久。
- 每回合抽 5、3 能量；敵人固定血量／傷害曲線；貪婪出牌（即時價值/費用最高者）。
"""
from __future__ import annotations
import math
import random
from collections import defaultdict

ENC = {  # 出現比例, 總血量範圍, 敵人數範圍, 基礎傷害(每段), 每回合成長, 每敵每回合攻擊段數, 回合攻擊機率
    "hallway": (0.72, (55, 80), (1, 3), 6.0, 0.6, 1.0, 0.7),
    "elite":   (0.20, (140, 175), (1, 2), 8.0, 0.7, 1.4, 0.75),
    "boss":    (0.08, (380, 450), (1, 1), 8.0, 0.6, 2.0, 0.75),
}
IMP = {"hallway": 1.0, "elite": 2.0, "boss": 3.0}
# 細劍等級分布：整局約 45 場戰鬥，L2 第 2 場、L3 約第 6 場、L4 約第 12 場、L5 約第 24 場
LEVEL_DIST = [(1, 0.05), (2, 0.09), (3, 0.13), (4, 0.27), (5, 0.46)]

SWORD = {"Strike", "Lunge", "ParallelSting", "SweepingEdge", "Linear", "GuardBreak", "QuadruplePain", "StarSplash",
         "StardustMirage", "FinalAttack", "StarburstStream", "ExtremeDash"}
LINEAR_POOL = sorted(["FinalAttack", "GuardBreak", "Linear", "Lunge", "ParallelSting", "QuadruplePain",
                      "StarburstStream", "StardustMirage", "StarSplash", "Strike", "SweepingEdge"])
PARTS = ("SSArrow", "SSBroom", "SSRibbon")
COST = {"Strike": 1, "Defend": 1, "Voiding": 1, "Trauma": 0, "Lunge": 1, "ParallelSting": 1, "SweepingEdge": 1,
        "Linear": 1, "GuardBreak": 1, "QuadruplePain": 1, "StarSplash": 1, "StardustMirage": 2, "FinalAttack": 2,
        "StarburstStream": 3, "Shrug": 1, "Dancing": 1, "SlidingStep": 0, "DefensiveStance": 1, "ShowOff": 1,
        "Comedian": 1, "RealisticActing": 1, "RapierMastery": 1, "CeremonialSword": 1, "Pride": 2, "Crowning": 2,
        "IAmReborn": 3, "TreeBranch": 0, "GazeGuiding": 1, "RapierTrick": 1, "GateOfBabylon": 3,
        "UnlimitedSpearWork": 1, "Projection": 1, "AdvancedProjection": 0, "ForgeSpear": 1, "ForgeSpearE": 1,
        "SmartSpear": 1, "PortableFletchingStation": 1, "SpearMastery": 1, "SmoothMove": 1, "StrongSake": 3,
        "SimpleSpear": 0, "Clusterphobia": 1}
EXHAUST = {"Linear", "Pride", "Crowning", "IAmReborn", "TreeBranch", "Projection", "AdvancedProjection", "ForgeSpear",
           "ForgeSpearE", "SimpleSpear", "ExtremeDash"}
POWERS = {"RealisticActing", "RapierMastery", "CeremonialSword", "UnlimitedSpearWork", "SmartSpear",
          "PortableFletchingStation", "SpearMastery", "SmoothMove", "StrongSake"}
RETAIN = set(PARTS) | {"GateOfBabylon"}
ETHEREAL = {"ForgeSpearE"}

STARTER = ["Strike"] * 3 + ["Defend"] * 4 + ["Voiding", "Trauma", "Lunge"]
ARCH = {
    "sword": STARTER + ["ParallelSting", "SweepingEdge", "Linear", "GuardBreak", "QuadruplePain", "StarSplash",
                        "Shrug", "Dancing"],
    "parry": STARTER + ["Shrug", "Dancing", "ShowOff", "Comedian", "RealisticActing", "SlidingStep", "GuardBreak",
                        "SweepingEdge"],
    "spear": STARTER + ["Projection", "Projection", "AdvancedProjection", "Shrug", "Dancing", "SweepingEdge",
                        "GuardBreak", "RapierTrick"],
}


class Enemy:
    __slots__ = ("hp", "weak", "vuln", "intent", "gaze", "crown")

    def __init__(self, hp):
        self.hp = hp; self.weak = 0; self.vuln = 0; self.intent = 0.0; self.gaze = 0.0; self.crown = False


class Combat:
    def __init__(self, deck, kind, rng, st, probe=None, level=None, second_sword=False):
        self.rng = rng; self.kind = kind; self.st = st; self.probe = probe
        _, hpr, nr, self.bdmg, self.grow, self.hits, self.patk = ENC[kind]
        n = rng.randint(*nr); tot = rng.uniform(*hpr)
        self.enemies = [Enemy(tot / n) for _ in range(n)]
        self.draw = list(deck); rng.shuffle(self.draw)
        if probe == "UnlimitedSpearWork+" and "UnlimitedSpearWork" in self.draw:
            self.draw.remove("UnlimitedSpearWork"); self.draw.append("UnlimitedSpearWork")
        self.hand = []; self.disc = []; self.exh = []
        if level is None:
            r = rng.random(); acc = 0
            for lv, p in LEVEL_DIST:
                acc += p
                if r <= acc:
                    level = lv; break
        self.level = level; self.pct = 0.6 + 0.1 * (level - 1)
        self.hp = 80.0; self.block = 0.0; self.str = 0.0; self.tmpstr = 0.0; self.dex = 0.0
        self.second = second_sword; self.branch = False; self.mastery = 0; self.majoka = 0
        self.usw = 0; self.smart = 0; self.pfs = 0; self.spearm = False; self.smooth = 0; self.sake = 0
        self.showoff = False; self.shadow = False; self.double = False; self.e_next = 0; self.d_next = 0
        self.turn = 0; self.energy = 0; self.dealt = 0.0; self.taken = 0.0; self.dead = False
        self.sw_played_turn = 0

    # ---------- 工具 ----------
    def alive(self):
        return [e for e in self.enemies if e.hp > 0]

    def is_probe(self, c):
        return self.probe is not None and c == self.probe.rstrip("+")

    def hit(self, e, base, times=1, card=None, powered=True):
        tot = 0.0
        for _ in range(times):
            if e.hp <= 0:
                break
            d = base + ((self.str + self.tmpstr) if powered else 0)
            d = max(0.0, d)
            if e.vuln > 0:
                d *= 1.5
            if self.double:
                d *= 2
            d = math.floor(d)
            real = min(e.hp, d); e.hp -= d; tot += real
            if card in SWORD and card != "StarburstStream" and (self.second or self.branch) and d > 0 and e.hp > -d:
                ex = math.ceil(real * 0.3) if real > 0 else 0
                if e.hp > 0 and ex > 0:
                    r2 = min(e.hp, ex); e.hp -= ex; tot += r2
                    self.st["ss_extra"] += r2
                    if self.branch and not self.second:
                        self.st["branch_extra"] += r2
        self.dealt += tot
        return tot

    def target(self):
        al = self.alive()
        return min(al, key=lambda e: e.hp) if al else None

    def gain_block(self, b, dexable=True):
        b = b + (self.dex if dexable else 0)
        if self.shadow:
            self.st["shadow_extra"] += b
            b *= 2
        self.block += b

    def incoming(self):
        return sum(e.intent for e in self.alive())

    # ---------- 抽牌 / 零件 ----------
    def draw_cards(self, n):
        for _ in range(n):
            if not self.draw:
                if not self.disc:
                    return
                self.draw = self.disc; self.disc = []; self.rng.shuffle(self.draw)
            c = self.draw.pop()
            self.hand.append(c)
            if c in PARTS:
                self.on_part_drawn()
        self.try_craft()

    def on_part_drawn(self):
        for _ in range(self.smart):
            parts = [i for i, c in enumerate(self.draw) if c in PARTS]
            if not parts:
                break
            have = {c for c in self.hand if c in PARTS}
            pref = [i for i in parts if self.draw[i] not in have] or parts
            i = self.rng.choice(pref)
            self.hand.append(self.draw.pop(i))
            self.st["smart_pull"] += 1
        self.try_craft()

    def try_craft(self):
        while all(p in self.hand for p in PARTS):
            for p in PARTS:
                self.hand.remove(p)
            self.hand.append("SimpleSpear")
            self.st["spears"] += 1
            if self.st.get("_first_spear") is None:
                self.st["_first_spear"] = 1; self.st["first_spear_turn"] += self.turn; self.st["spear_fights"] += 1
            for _ in range(self.pfs):
                if self.rng.random() < 0.5:
                    self.hand.append("SimpleSpear"); self.st["spears"] += 1; self.st["pfs_bonus"] += 1

    def forge(self):
        for p in PARTS:
            self.draw.insert(self.rng.randint(0, len(self.draw)), p)
        self.st["forges"] += 1

    # ---------- 出牌估值 ----------
    def score(self, c):
        """回傳 (即時價值, 實際費用)。價值單位≈能量。"""
        cost = COST.get(c, 1)
        if c in PARTS:
            return None
        if c == "StarburstStream" and not (self.second or self.branch):
            return None
        if cost > self.energy:
            return None
        t = self.target()
        if t is None:
            return None
        unc = max(0.0, self.incoming() - self.block)
        strn = self.str + self.tmpstr
        mult = (1.5 if t.vuln else 1.0) * (2 if self.double else 1)
        ss = 1.3 if (self.second or self.branch) and c in SWORD else 1.0

        def atk(base, times=1, aoe=False):
            if aoe:
                return sum(min(e.hp, (base + strn) * (1.5 if e.vuln else 1) * (2 if self.double else 1) * times)
                           for e in self.alive()) * ss / 6
            return min(t.hp, (base + strn) * mult * times) * ss / 6

        def blk(b):
            b = (b + self.dex) * (2 if self.shadow else 1)
            useful = min(b, unc)
            return useful / 5 + useful * self.pct * (1 + self.mastery) / 6 * 0.9 + (b - useful) * 0.02

        v = 0.0
        if c == "Strike": v = atk(6)
        elif c == "Lunge": v = atk(8) + 0.25
        elif c == "ParallelSting": v = atk(4, 2)
        elif c == "QuadruplePain": v = atk(2, 4)
        elif c == "StarSplash": v = atk(1, 8)
        elif c == "SweepingEdge": v = atk(7, aoe=True) + 0.2
        elif c == "GuardBreak": v = atk(10)
        elif c == "Linear": v = atk(8) + 0.7
        elif c == "ExtremeDash": v = atk(8)
        elif c == "FinalAttack":
            v = atk(4, 3)
            if any(x in SWORD and x != "FinalAttack" for x in self.hand):
                v *= 0.3  # 留著等自動打出
        elif c == "StardustMirage":
            k = sum(1 for x in self.draw[-5:] if x in SWORD)
            v = atk(2, 3 * k) if k else 0.05
        elif c == "StarburstStream":
            k = len(self.hand) - 1
            v = atk(3 + k, 16)
        elif c == "SimpleSpear": v = atk(75 if self.spearm else 50)
        elif c == "Defend": v = blk(5)
        elif c == "Voiding": v = blk(8) + 0.3
        elif c == "Shrug": v = blk(8) + 0.5
        elif c == "Dancing": v = blk(8) + 0.8
        elif c == "SlidingStep": v = blk(3) + 0.5
        elif c == "ShowOff": v = blk(8) + 0.2
        elif c == "Comedian": v = blk(8 + 2 * sum(1 for x in self.hand if x in PARTS))
        elif c == "Pride": v = blk(30) + 1.0
        elif c == "Crowning": v = sum(e.intent for e in self.alive()) / 5 if unc > 0 else 0
        elif c == "GazeGuiding":
            e = max(self.alive(), key=lambda e: e.intent)
            red = 0.5 + 0.5 * min(self.majoka / 100, 1)
            v = min(e.intent * red, unc) / 5
        elif c == "DefensiveStance":
            rest = sum(1 for x in self.hand if x in ("Defend", "Voiding", "Shrug", "Dancing", "ShowOff", "Comedian", "Pride"))
            v = 0.9 * min(rest, max(0, self.energy - 1)) if unc > 0 else 0
        elif c == "TreeBranch":
            if self.second or self.branch:
                return None
            sw = [x for x in self.hand if x in SWORD and x != "StarburstStream"]
            v = 0.3 * 7 * min(len(sw), self.energy) / 6 if sw else 0
            if "StarburstStream" in self.hand and self.energy >= 3:
                v = 5
        elif c in ("Projection", "AdvancedProjection", "ForgeSpear", "ForgeSpearE"):
            v = 3.0 if self.turn < 8 else 1.0
        elif c == "GateOfBabylon":
            n = sum(1 for x in self.exh if x == "SimpleSpear")
            v = atk(75 if self.spearm else 50, n) if n else None
            if v is None:
                return None
        elif c in POWERS:
            v = 3.0 if self.turn <= 6 else 0.5
            if c == "SpearMastery" and self.spearm:
                v = 0.0
        elif c == "IAmReborn":
            v = 3.2 if self.turn <= 8 else 0.5
        elif c == "RapierTrick": v = 1.4
        elif c == "Trauma": v = 0.02
        elif c == "Clusterphobia": v = 0.05
        return v, cost

    def play(self, c):
        cost = COST.get(c, 1)
        self.energy -= cost
        self.hand.remove(c)
        t = self.target()
        if self.is_probe(c):
            self.st["probe_plays"] += 1
            self.st["probe_turn"] += self.turn
        dest = "exh" if c in EXHAUST else ("none" if c in POWERS else "disc")
        if c == "Strike": self.hit(t, 6, 1, c)
        elif c == "Lunge": self.hit(t, 8, 1, c); t.weak += 1
        elif c == "ParallelSting": self.hit(t, 4, 2, c)
        elif c == "QuadruplePain": self.hit(t, 2, 4, c)
        elif c == "StarSplash": self.hit(t, 1, 8, c)
        elif c == "SweepingEdge":
            for e in self.alive():
                self.hit(e, 7, 1, c); e.weak += 1
        elif c == "GuardBreak": self.hit(t, 10, 1, c)
        elif c == "ExtremeDash": self.hit(t, 8, 1, c)
        elif c == "Linear":
            self.hit(t, 8, 1, c)
            g = self.rng.choice(LINEAR_POOL); self.hand.append(g)
            if self.probe == "Linear":
                self.st["linear_gen_" + ("playable" if g != "StarburstStream" or self.second else "dead")] += 1
        elif c == "FinalAttack":
            d = self.hit(t, 4, 3, c)
            if self.probe == "FinalAttack":
                self.st["fa_manual"] += 1; self.st["fa_dmg"] += d
        elif c == "StardustMirage":
            k = sum(1 for x in self.draw[-5:] if x in SWORD)
            d = self.hit(t, 2, 3 * k, c)
            if self.probe == "StardustMirage":
                self.st["sdm_k"] += k; self.st["sdm_dmg"] += d
            self.rng.shuffle(self.draw)
        elif c == "StarburstStream":
            k = len(self.hand); self.exh.extend(self.hand); self.hand = []
            d = self.hit(t, 3 + k, 16, c)
            if self.probe and self.probe.startswith("Starburst"):
                self.st["sb_dmg"] += d; self.st["sb_k"] += k
        elif c == "SimpleSpear":
            d = self.hit(t, 75 if self.spearm else 50, 1, c)
            self.st["spear_dmg"] += d; self.st["spear_plays"] += 1
        elif c == "Defend": self.gain_block(5)
        elif c == "Voiding": self.gain_block(8); t.vuln += 1
        elif c == "Shrug": self.gain_block(8); self.draw_cards(1)
        elif c == "Dancing": self.gain_block(8); self.e_next += 1
        elif c == "SlidingStep": self.gain_block(3); self.draw_cards(1)
        elif c == "ShowOff": self.gain_block(8); self.showoff = True
        elif c == "Comedian":
            k = sum(1 for x in self.hand if x in PARTS)
            self.gain_block(8 + 2 * k)
            if self.probe == "Comedian":
                self.st["com_parts"] += k
        elif c == "Pride":
            self.gain_block(30); self.disc.append("Crowning")
        elif c == "Crowning":
            prevented = 0
            for e in self.alive():
                e.crown = True; prevented += e.intent
            if self.probe in ("Crowning", "Pride"):
                self.st["crown_prevent_raw"] += prevented
                self.st["crown_prevent"] += min(prevented, max(0.0, prevented - 0))
                self.st["crown_block_before"] += self.block
        elif c == "GazeGuiding":
            e = max(self.alive(), key=lambda e: e.intent)
            e.gaze = 0.5 + 0.5 * min(self.majoka / 100, 1)
            if self.probe == "GazeGuiding":
                self.st["gaze_red"] += e.intent * e.gaze; self.st["gaze_unc"] += max(0, self.incoming() - self.block)
        elif c == "DefensiveStance": self.shadow = True
        elif c == "TreeBranch": self.branch = True
        elif c in ("Projection", "AdvancedProjection"):
            self.hand.append("ForgeSpear")
        elif c in ("ForgeSpear", "ForgeSpearE"):
            self.forge()
        elif c == "GateOfBabylon":
            n = sum(1 for x in self.exh if x == "SimpleSpear")
            dd = 0
            for _ in range(n):
                tt = self.target()
                if tt is None:
                    break
                dd += self.hit(tt, 75 if self.probe == "GateOfBabylon+" or self.spearm else 50, 1, "SimpleSpear")
            if self.probe and self.probe.startswith("GateOfBabylon"):
                self.st["gate_n"] += n; self.st["gate_dmg"] += dd
        elif c == "RealisticActing": self.dex += 2
        elif c == "RapierMastery": self.mastery += 2 if self.probe == "RapierMastery+" else 1
        elif c == "CeremonialSword": self.str += 2; self.second = True
        elif c == "UnlimitedSpearWork": self.usw += 1
        elif c == "SmartSpear": self.smart += 2 if self.probe == "SmartSpear+" else 1
        elif c == "PortableFletchingStation": self.pfs += 1
        elif c == "SpearMastery": self.spearm = True
        elif c == "SmoothMove": self.smooth += 2 if self.probe == "SmoothMove+" else 1
        elif c == "StrongSake": self.sake += 1
        elif c == "IAmReborn":
            self.disc.extend(self.hand); self.hand = []
            self.reborn_next = True; self.d_next += 5
        elif c == "RapierTrick":
            self.draw_cards(3)
            cand = [x for x in self.hand if x in ("Trauma", "Clusterphobia")] or \
                   [x for x in self.hand if x == "Defend"] or [x for x in self.hand if x not in PARTS and x != "SimpleSpear"]
            if cand:
                x = cand[0]; self.hand.remove(x); self.exh.append(x)
        elif c == "Trauma": self.majoka += 10
        elif c == "Clusterphobia": self.majoka += 20
        if dest == "exh":
            self.exh.append(c)
        elif dest == "disc":
            self.disc.append(c)
        # 劍技連動
        if c in SWORD:
            self.sw_played_turn += 1
            self.st["sword_plays"] += 1
            if self.smooth:
                self.st["smooth_draw"] += self.smooth; self.draw_cards(self.smooth)
            if self.sake:
                self.e_next += self.sake; self.st["sake_energy"] += self.sake
            if c != "FinalAttack" and "FinalAttack" in self.hand and self.alive():
                self.hand.remove("FinalAttack")
                d = self.hit(self.target(), 4, 3, "FinalAttack")
                self.disc.append("FinalAttack")
                if self.probe == "FinalAttack":
                    self.st["fa_auto"] += 1; self.st["fa_dmg"] += d
                    self.st["probe_plays"] += 1
                self.sw_played_turn += 1; self.st["sword_plays"] += 1
                if self.smooth:
                    self.st["smooth_draw"] += self.smooth; self.draw_cards(self.smooth)
                if self.sake:
                    self.e_next += self.sake; self.st["sake_energy"] += self.sake

    # ---------- 回合 ----------
    def player_turn(self):
        self.turn += 1
        self.block = 0.0; self.shadow = False; self.branch = False; self.sw_played_turn = 0
        self.energy = 3 + self.e_next; self.e_next = 0
        self.double = getattr(self, "reborn_next", False)
        if self.double:
            self.reborn_next = False
        t = self.turn
        for e in self.alive():
            e.crown = False; e.gaze = 0.0
            e.intent = (self.bdmg + self.grow * (t - 1)) * self.hits * (0.75 if e.weak else 1) \
                if self.rng.random() < self.patk else 0.0
        if self.usw:
            self.hand.extend(["ForgeSpearE"] * self.usw)
        n = 5 + self.d_next; self.d_next = 0
        self.draw_cards(n)
        dealt0 = self.dealt
        while self.alive():
            best = None
            for c in set(self.hand):
                s = self.score(c)
                if s is None:
                    continue
                v, cost = s
                if v <= 0.01 and cost > 0:
                    continue
                key = v / cost if cost > 0 else v * 10 + 5
                if best is None or key > best[0]:
                    best = (key, c)
            if best is None:
                break
            self.play(best[1])
        if self.double and self.probe == "IAmReborn":
            self.st["reborn_turn_dmg"] += self.dealt - dealt0
        # 回合結束
        for c in list(self.hand):
            if c in ETHEREAL:
                self.hand.remove(c); self.exh.append(c)
        keep = [c for c in self.hand if c in RETAIN]
        self.disc.extend(c for c in self.hand if c not in RETAIN)
        self.hand = keep
        if self.double:
            self.double = False

    def enemy_turn(self):
        for e in self.alive():
            if e.intent <= 0:
                e.weak = max(0, e.weak - 1); e.vuln = max(0, e.vuln - 1)
                continue
            dmg = e.intent
            if e.crown:
                dmg = 0.0
            if e.gaze:
                dmg *= (1 - e.gaze)
            nh = max(1, round(self.hits))
            per = dmg / nh
            for _ in range(nh):
                if per <= 0:
                    break
                blocked = min(self.block, per)
                self.block -= blocked
                un = per - blocked
                self.hp -= un; self.taken += un
                if blocked > 0 and e.hp > 0:
                    r = math.ceil(blocked * self.pct)
                    for _k in range(1 + self.mastery):
                        if e.hp <= 0:
                            break
                        real = min(e.hp, r); e.hp -= r; self.dealt += real
                        self.st["reflect"] += real
                        if _k > 0:
                            self.st["mastery_reflect"] += real
                if un <= 0 and self.showoff:
                    self.tmpstr += 1; self.st["showoff_str"] += 1
            e.weak = max(0, e.weak - 1); e.vuln = max(0, e.vuln - 1)
        # ShowOff 暫時力量：下回合結束移除（簡化為下回合後清除）
        if self.showoff and self.turn % 1 == 0:
            pass
        if self.hp <= 0:
            self.dead = True

    def run(self):
        while self.alive() and not self.dead and self.turn < 30:
            tmp_before = self.tmpstr
            self.player_turn()
            if not self.alive():
                break
            self.enemy_turn()
            # 炫耀的暫時力量維持到下個玩家回合結束
            self.tmpstr -= tmp_before
            self.showoff = False
        self.st["len"] += self.turn
        self.st["taken"] += self.taken
        self.st["fights"] += 1
        self.st["deaths"] += 1 if self.dead else 0


def run(deck, n, seed=1, probe=None, level=None, second_sword=False):
    rng = random.Random(seed)
    kinds = list(ENC)
    w = [ENC[k][0] for k in kinds]
    by = {k: defaultdict(float) for k in kinds}
    for _ in range(n):
        k = rng.choices(kinds, w)[0]
        st = by[k]
        st.pop("_first_spear", None)
        Combat(deck, k, rng, st, probe, level, second_sword).run()
        st.pop("_first_spear", None)
    return by
