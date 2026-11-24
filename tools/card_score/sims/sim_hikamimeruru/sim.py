"""冰上梅露露簡化戰鬥蒙地卡羅模型（v5）。規則依 Manosaba 原始碼：
- 魔女的大鍋（遺物）：每場火力重設 20；使用／丟棄非催化劑藥水時有 火力% 機率得催化劑（需有空欄）。
- 大魔女的照片（升級遺物）：藥水欄 3 + {Lv1:1, Lv2:2, Lv3:4, Lv4:6, Lv5:10}；exp 一般 1／菁英 2／首領 3，
  門檻 0/2/7/15/30。藥水跨戰鬥保留（模擬以 16 場連續戰鬥為一輪）。
- 合成：同種輕效×2→基礎；基礎(止痛/格檔/肌肉/能量/虛弱/甲蟲)+催化劑→強效。合成的丟棄不會觸發大鍋與藥水投擲
  （BeginCraftDiscardScope 抑制）。
- 藥水投擲：丟棄藥水時每層對隨機敵 5 傷；丟石頭改為滾石 AoE 5，每次 +5。
- 原初藥水力量：所有藥水變成藥水形石頭（使用 15 傷）。
- 梅露露醫務室：未格擋傷害累積層數，回合開始 (層-1)/2 →<5 輕止痛(2)、5~11 止痛(5)、≥12 強止痛(12)。
- 魔女化倍率 1+3·s^1.8365/(s^1.8365+166.6^1.8365)，整場保留；單人殺人衝動無隊友可波及。
"""
from __future__ import annotations
import random, math
from collections import defaultdict

ENC = {  # 比例, 總血, 敵數, 基礎傷害, 每回合成長
    "hallway": (0.72, (55, 80), (1, 3), 8.0, 1.0),
    "elite":   (0.20, (140, 175), (1, 2), 11.0, 1.2),
    "boss":    (0.08, (440, 520), (1, 1), 10.0, 1.1),
}
HPSCALE = 0.6  # 梅露露牌組輸出低：敵血縮放使回合長度接近 turn_model
IMP = {"hallway": 1.0, "elite": 2.0, "boss": 3.0}
EXP = {"hallway": 1, "elite": 2, "boss": 3}
LV_EXP = [0, 2, 7, 15, 30]
LV_SLOTS = {1: 1, 2: 2, 3: 4, 4: 6, 5: 10}

# 藥水：名稱 -> 名目價值（能量），用於合成分帳與丟棄判斷
NOM = {"LBlock": 1.0, "LPK": 0.55, "LFlex": 0.5, "Block": 2.4, "PK": 1.4, "Flex": 1.3, "Beetle": 2.0,
       "Energy": 2.0, "Weak": 1.3, "Vuln": 1.5, "GBlock": 4.8, "GPK": 3.3, "GFlex": 2.6, "GEnergy": 4.0,
       "GWeak": 2.2, "GBeetle": 3.0, "Cat": 1.0, "Rock": 2.5, "Tred": 0.0}
FACTORY = ["Block", "PK", "Flex", "Beetle", "Energy", "Weak", "Vuln"]
LESSER = ["LBlock", "LPK", "LFlex"]
PAIR = {"LBlock": "Block", "LPK": "PK", "LFlex": "Flex"}
CATUP = {"Block": "GBlock", "PK": "GPK", "Flex": "GFlex", "Energy": "GEnergy", "Weak": "GWeak", "Beetle": "GBeetle"}

COST = {"Strike": 1, "Defend": 1, "Trauma": 0, "PotionSelect": 2, "AddFirewood": 1, "CollectPotions": 1,
        "Incitement": 1, "ShotsFired": 1, "CollectData": 1, "QuickHeal": 1, "AutomatedPotionMix": 1,
        "PotionThrow": 1, "MeruruInfirmary": 2, "PotionFactory": 1, "Exaid": 3, "NanokaPuzzle": 3,
        "PrimalPotionPower": 3, "WitchesCauldron": 2, "GrandHeal": 3, "Piece": 0, "Slash": 1, "Guard": 1}
POWERS = {"AutomatedPotionMix", "PotionThrow", "MeruruInfirmary", "PotionFactory", "Exaid", "GrandHeal"}
EXHAUST = {"WitchesCauldron", "NanokaPuzzle", "Piece"}
ATTACKS = {"Strike", "ShotsFired", "Slash"}
FILLER = ["Slash"] * 3 + ["Guard"] * 2  # 共用卡池代表：1 費 11 傷 / 1 費 9 格檔

STARTER = ["Strike"] * 4 + ["Defend"] * 3 + ["Trauma", "PotionSelect", "AddFirewood"]
ARCH = {
    "brew": STARTER + FILLER + ["CollectPotions", "CollectPotions", "AutomatedPotionMix", "PotionFactory", "CollectData",
                       "ShotsFired", "ShotsFired", "QuickHeal", "WitchesCauldron", "Defend"],
    "throw": STARTER + FILLER + ["PotionThrow", "PotionThrow", "CollectPotions", "CollectPotions", "AutomatedPotionMix",
                        "PrimalPotionPower", "ShotsFired", "ShotsFired", "CollectData", "Defend"],
    "heal": STARTER + FILLER + ["ShotsFired", "ShotsFired", "ShotsFired", "QuickHeal", "QuickHeal", "MeruruInfirmary",
                       "CollectData", "Incitement", "Exaid", "Defend"],
}


class Pot:
    __slots__ = ("k", "src")

    def __init__(self, k, src):
        self.k = k; self.src = src  # src: dict 來源->權重


class Enemy:
    __slots__ = ("hp", "weak", "vuln", "shrink")

    def __init__(self, hp):
        self.hp = hp; self.weak = 0; self.vuln = 0; self.shrink = 0


def mult(s):
    if s <= 0: return 1.0
    a = s ** 1.8365
    return 1 + 3 * a / (a + 166.6 ** 1.8365)


class Combat:
    def __init__(self, deck, kind, rng, st, pots, slots, hp, upg=frozenset()):
        self.rng = rng; self.kind = kind; self.st = st; self.pots = pots; self.slots = slots; self.upg = upg
        _, hpr, nr, self.bdmg, self.grow = ENC[kind]
        n = rng.randint(*nr); tot = rng.uniform(*hpr) * HPSCALE
        self.enemies = [Enemy(tot / n) for _ in range(n)]
        self.draw = list(deck); rng.shuffle(self.draw)
        for c in ("AutomatedPotionMix", "PotionFactory"):
            if c in upg and c in self.draw:
                self.draw.remove(c); self.draw.append(c)
        self.hand = []; self.disc = []; self.maxhp = 80.0; self.hp = hp
        self.block = 0.0; self.energy = 0; self.turn = 0
        self.fire = 20; self.firesrc = {"relic": 20.0}
        self.majoka = 0.0; self.majsrc = {}
        self.tempstr = 0.0; self.apm = 0; self.fac = 0; self.throw = 0; self.throwsrc = []; self.boulder = 5
        self.inf = 0; self.infstack = 0; self.ended = False; self.attacks_left = 0
        self.dmg_taken = 0.0; self.won_nanoka = False

    # ---- 共用 ----
    def credit(self, src, v, w=1.0):
        for s, ww in src.items():
            self.st["cr_" + s] += v * ww * w

    def alive(self):
        return [e for e in self.enemies if e.hp > 0]

    def tgt(self):
        al = self.alive(); return min(al, key=lambda e: e.hp) if al else None

    def hit(self, e, base, powered=True, src=None):
        if e is None or e.hp <= 0: return 0.0
        d = base + (self.tempstr if powered else 0)
        m = mult(self.majoka) if powered else 1.0
        d *= m
        if e.vuln > 0: d *= 1.5
        d = math.floor(d)
        a = min(d, e.hp); e.hp -= d
        if powered and self.majoka > 0 and a > 0:
            bonus = a * (1 - 1 / m)
            for s, w in self.majsrc.items():
                self.st["cr_" + s] += bonus / 6 * w / self.majoka
        if powered and self.tempstr > 0 and a > 0 and self.flexsrc:
            fb = min(a, self.tempstr * m)
            for s, w in self.flexsrc.items():
                self.st["cr_" + s] += fb / 6 * w
        if src: self.st["cr_" + src] += a / 6
        return a

    flexsrc = None

    def incoming(self):
        tot = 0.0
        for e in self.alive():
            per = (self.bdmg + self.grow * (self.turn - 1)) / len(self.enemies)
            if e.weak > 0: per *= 0.75
            if e.shrink > 0: per *= 0.7
            tot += per
        return tot

    def add_majoka(self, n, src):
        n = max(0.0, n)
        if n <= 0: return
        self.majoka += n; self.majsrc[src] = self.majsrc.get(src, 0) + n

    # ---- 藥水 ----
    def procure(self, k, src):
        if len(self.pots) >= self.slots:
            self.st["overflow_" + next(iter(src))] += 1; return False
        self.pots.append(Pot(k, src)); self.st["gen_" + next(iter(src))] += 1; return True

    def cauldron(self, p):
        if p.k == "Cat" or len(self.pots) >= self.slots: return
        if self.rng.randrange(100) < min(self.fire, 100):
            tot = sum(self.firesrc.values())
            src = {s: v / tot for s, v in self.firesrc.items()}
            self.st["cat_procs"] += 1
            self.procure("Cat", src)

    def remove(self, p, discard, craft=False):
        self.pots.remove(p)
        if craft:
            self.st["craft_discard"] += 1; return  # BeginCraftDiscardScope：不觸發投擲與大鍋
        self.st["pot_discard" if discard else "pot_use"] += 1
        if discard and self.throw:
            for _ in range(self.throw):
                if not self.alive(): break
                if p.k == "Rock":
                    d = sum(self.hit(e, self.boulder, powered=False) for e in list(self.alive())); self.boulder += 5
                else:
                    d = self.hit(self.rng.choice(self.alive()), 5, powered=False)
                self.st["throw_dmg"] += d
                for s, w in self.throwsrc_w().items():
                    self.st["cr_" + s] += d / 6 * w
        self.cauldron(p)

    def throwsrc_w(self):
        n = len(self.throwsrc); return {s: 1 / n for s in self.throwsrc} if n else {}

    def find(self, k):
        for p in self.pots:
            if p.k == k: return p
        return None

    def craft_all(self):
        changed = True
        while changed:
            changed = False
            for a, b in PAIR.items():
                ps = [p for p in self.pots if p.k == a]
                if len(ps) >= 2:
                    x, y = ps[0], ps[1]; src = self.merge(x, y)
                    self.remove(x, True, True); self.remove(y, True, True)
                    self.st["craft_pair"] += 1; self.procure(b, src); changed = True; break
            if changed: continue
            cat = self.find("Cat")
            if cat:
                for b, g in CATUP.items():
                    p = self.find(b)
                    if p:
                        src = self.merge(p, cat)
                        self.remove(p, True, True); self.remove(cat, True, True)
                        self.st["craft_cat"] += 1; self.procure(g, src); changed = True; break

    def merge(self, x, y):
        wx, wy = NOM[x.k], NOM[y.k]; t = wx + wy; out = defaultdict(float)
        for s, w in x.src.items(): out[s] += w * wx / t
        for s, w in y.src.items(): out[s] += w * wy / t
        return dict(out)

    def use(self, p):
        k = p.k; v = 0.0
        if k in ("LBlock", "Block", "GBlock"):
            b = {"LBlock": 5, "Block": 12, "GBlock": 24}[k]
            un = max(0.0, self.incoming() - self.block); v = min(b, un) / 5; self.block += b
        elif k in ("LPK", "PK", "GPK"):
            h = {"LPK": 2, "PK": 5, "GPK": 12}[k]; a = min(h, self.maxhp - self.hp); self.hp += a; v = a * 0.33
            self.st["heal_pot"] += a
        elif k in ("LFlex", "Flex", "GFlex"):
            s = {"LFlex": 2, "Flex": 5, "GFlex": 10}[k]; self.tempstr += s
            self.flexsrc = dict(p.src) if self.flexsrc is None else self.flexsrc
            self.remove(p, False); return  # 傷害於 hit() 中記帳
        elif k in ("Energy", "GEnergy"):
            e = 2 if k == "Energy" else 5; self.energy += e; v = e * 0.85
        elif k in ("Weak", "GWeak"):
            n = 3 if k == "Weak" else 6; t = max(self.alive(), key=lambda e: e.hp); t.weak += n
            v = min(n, 4) * self.incoming() / max(1, len(self.alive())) * 0.25 / 5
        elif k == "Vuln":
            t = self.tgt(); t.vuln += 3; v = 1.2
        elif k in ("Beetle", "GBeetle"):
            n = 4 if k == "Beetle" else 8; t = max(self.alive(), key=lambda e: e.hp); t.shrink += n
            v = min(n, 4) * self.incoming() / max(1, len(self.alive())) * 0.3 / 5
        elif k == "Rock":
            v = self.hit(self.tgt(), 15, powered=False) / 6
        self.credit(p.src, v)
        self.remove(p, False)

    def potion_phase(self, start):
        if not self.alive(): return
        self.craft_all()
        for p in list(self.pots):
            if not self.alive(): return
            k = p.k; un = max(0.0, self.incoming() - self.block)
            if k in ("Energy", "GEnergy") and start and self.hand_cost() > self.energy: self.use(p)
            elif k in ("LFlex", "Flex", "GFlex") and start and sum(1 for c in self.hand if c in ATTACKS) >= 2: self.use(p)
            elif k in ("Weak", "GWeak", "Beetle", "GBeetle", "Vuln") and start and (self.kind != "hallway" or self.turn <= 2): self.use(p)
            elif k == "Rock" and not start: self.use(p)
            elif k in ("LBlock", "Block", "GBlock") and not start:
                b = {"LBlock": 5, "Block": 12, "GBlock": 24}[k]
                if un >= b * 0.6 or (un >= 4 and len(self.pots) >= self.slots - 1): self.use(p)
            elif k in ("LPK", "PK", "GPK") and not start:
                h = {"LPK": 2, "PK": 5, "GPK": 12}[k]
                if self.maxhp - self.hp >= h: self.use(p)
        # 藥水投擲：丟棄輕效與石頭換傷害
        if self.throw and not start:
            for p in list(self.pots):
                if not self.alive(): break
                if p.k in ("LPK", "LFlex", "Rock", "Tred") or (p.k == "LBlock" and len(self.pots) >= self.slots - 1):
                    self.remove(p, True)
        # 欄位壓力：準備接收回合結束／開始的藥水
        gen = 2 * self.apm + 2 * self.fac
        while self.pots and len(self.pots) > self.slots - gen and not start:
            p = min(self.pots, key=lambda q: NOM[q.k])
            if NOM[p.k] > 1.2: break
            if p.k == "Cat": break
            self.remove(p, True); self.st["pressure_discard"] += 1

    def hand_cost(self):
        return sum(COST[c] for c in self.hand if c != "Piece")

    # ---- 牌 ----
    def draw_one(self):
        if not self.draw:
            self.draw = self.disc; self.disc = []; self.rng.shuffle(self.draw)
        return self.draw.pop() if self.draw else None

    def draw_n(self, n):
        for _ in range(n):
            if len(self.hand) >= 10: return
            x = self.draw_one()
            if x is None: return
            self.hand.append(x)
            if x.startswith("Part"):
                self.check_nanoka()

    def check_nanoka(self):
        if sum(1 for c in self.hand if c.startswith("Part")) >= 5:
            self.won_nanoka = True
            for e in self.enemies: e.hp = 0

    def h(self, c):
        al = self.alive()
        if not al: return -1
        un = max(0.0, self.incoming() - self.block); t = self.tgt()
        dm = lambda d: max(0.3, min((d + self.tempstr) * mult(self.majoka), t.hp) / 6)
        bl = lambda b: min(b, un) / 5 + 0.05
        free = self.slots - len(self.pots); U = c in self.upg
        if c == "Strike": return dm(9 if U else 6)
        if c == "ShotsFired": return dm(15 if U else 10)
        if c == "Slash": return dm(11)
        if c == "Guard": return bl(9)
        if c == "Defend": return bl(8 if U else 5)
        if c == "Trauma": return 0.3 if self.turn <= 3 else 0.12
        if c == "PotionSelect": return 0.6 * min(3 if U else 2, free) + 0.2
        if c == "CollectPotions": return 0.7 * min(3 if U else 2, free) + 0.1
        if c == "AddFirewood": return 0.35 if self.fire < 100 else 0.8
        if c in ("CollectData", "Incitement"): return 1.2
        if c == "QuickHeal": return bl(10 if U else 7) + 0.33 * min(7 if U else 5, self.maxhp - self.hp)
        if c == "AutomatedPotionMix": return 2.5
        if c == "PotionFactory": return 3.5
        if c == "PotionThrow": return 2.2
        if c == "MeruruInfirmary": return 1.8 if self.turn <= 3 else 0.6
        if c == "Exaid": return 2.5 if self.majoka < 60 and self.turn <= 3 and len(al) and sum(e.hp for e in al) > 60 else 0.1
        if c == "WitchesCauldron": return 1.5 if self.fire < 60 else 0.1
        if c == "PrimalPotionPower":
            n = sum(1 for p in self.pots if NOM[p.k] < 1.6)
            return 0.9 * n - 0.4 * (len(self.pots) - n) if self.throw else 0.5 * n - 0.4 * (len(self.pots) - n)
        if c == "NanokaPuzzle": return 4.0 if self.kind == "boss" and self.turn <= 2 else -1
        if c == "Piece": return 0.01
        if c == "GrandHeal": return 0.33 * min(20, self.maxhp - self.hp)
        return 0.1

    def play(self, c):
        st = self.st; U = c in self.upg; t = self.tgt()
        st["play_" + c] += 1
        if c == "Strike": self.hit(t, 9 if U else 6, src=None)
        elif c == "ShotsFired": self.hit(t, 15 if U else 10)
        elif c == "Defend": self.block += 8 if U else 5
        elif c == "Slash": self.hit(t, 11)
        elif c == "Guard": self.block += 9
        elif c == "Trauma": self.add_majoka(15 if U else 10, "Trauma")
        elif c == "PotionSelect":
            for _ in range(3 if U else 2):
                un = max(0.0, self.incoming() - self.block)
                k = "LBlock" if un > 3 or self.hp > 70 else "LPK"
                if sum(1 for p in self.pots if p.k == "LBlock") % 2 == 1: k = "LBlock"
                self.procure(k, {"PotionSelect": 1.0})
        elif c == "CollectPotions":
            for _ in range(3 if U else 2): self.procure(self.rng.choice(LESSER), {"CollectPotions": 1.0})
        elif c == "AddFirewood":
            if self.fire >= 100: self.procure("Cat", {"AddFirewood": 1.0})
            else:
                a = min(15 if U else 10, 100 - self.fire); self.fire += a
                self.firesrc["AddFirewood"] = self.firesrc.get("AddFirewood", 0) + a
        elif c == "WitchesCauldron":
            a = 100 - self.fire; self.fire = 100
            self.firesrc["WitchesCauldron"] = self.firesrc.get("WitchesCauldron", 0) + a
        elif c in ("CollectData", "Incitement"):
            self.draw_n((4 if U else 3) if c == "CollectData" else (3 if U else 2))
        elif c == "QuickHeal":
            un = max(0.0, self.incoming() - self.block); b = 10 if U else 7
            a = min(7 if U else 5, self.maxhp - self.hp); self.hp += a; self.block += b
            st["cr_QuickHeal"] += a * 0.22 + min(b, un) / 5
        elif c == "AutomatedPotionMix": self.apm += 1
        elif c == "PotionFactory": self.fac += 1
        elif c == "PotionThrow": self.throw += 1; self.throwsrc.append("PotionThrow")
        elif c == "MeruruInfirmary":
            if self.inf == 0: self.infstack = 1
            else: self.infstack += 1
            self.inf += 1
        elif c == "Exaid":
            self.add_majoka(max(0.0, 100 - self.majoka), "Exaid"); self.ended = True
        elif c == "PrimalPotionPower":
            lost = sum(NOM[p.k] for p in self.pots)
            st["primal_lost_nom"] += lost; st["primal_rocks"] += len(self.pots)
            for p in self.pots: p.k = "Rock"; p.src = {"PrimalPotionPower": 1.0}
            st["cr_PrimalPotionPower"] -= lost * 0.7
        elif c == "NanokaPuzzle":
            st["nanoka_played"] += 1
            self.hand = []
            pieces = ["Piece"] * (25 if U else 35) + ["Part"] * 5
            self.draw = pieces; self.rng.shuffle(self.draw); self.nanoka_turn = self.turn
        elif c == "Piece": pass

    # ---- 回合 ----
    def run(self):
        st = self.st
        while self.turn < 30:
            self.turn += 1
            self.block = 0.0; self.energy = 3; self.tempstr = 0.0; self.flexsrc = None; self.ended = False
            # 回合開始：工廠、醫務室
            for _ in range(2 * self.fac): self.procure(self.rng.choice(FACTORY), {"PotionFactory": 1.0})
            if self.inf:
                stacks = max(0, self.infstack - 1); h = stacks // 2
                if h > 0:
                    k = "GPK" if h >= 12 else ("PK" if h >= 5 else "LPK")
                    self.procure(k, {"MeruruInfirmary": 1.0}); st["inf_pots_" + k] += 1
                self.infstack = 1
            self.draw_n(5)
            if not self.alive(): break
            self.potion_phase(True)
            while self.alive() and not self.ended:
                best = None; bv = 0.0
                for c in set(self.hand):
                    if c.startswith("Part"): continue
                    k = COST[c] - (1 if c in self.upg and c in ("Exaid", "PrimalPotionPower", "WitchesCauldron", "GrandHeal", "PotionThrow") else 0)
                    if k > self.energy: continue
                    v = self.h(c); sc = v / max(k, 0.6)
                    if v > 0.15 and sc > bv: bv, best = sc, c
                if best is None: break
                k = COST[best] - (1 if best in self.upg and best in ("Exaid", "PrimalPotionPower", "WitchesCauldron", "GrandHeal", "PotionThrow") else 0)
                self.energy -= k; self.hand.remove(best)
                self.play(best)
                if best in POWERS: pass
                elif best in EXHAUST: pass
                else: self.disc.append(best)
                if not self.alive(): break
                if best in ("CollectPotions", "PotionSelect"): self.craft_all()
            self.potion_phase(False)
            if not self.alive(): break
            # 回合結束：自動調藥
            for _ in range(2 * self.apm): self.procure(self.rng.choice(LESSER), {"AutomatedPotionMix": 1.0})
            keep = [c for c in self.hand if c.startswith("Part")]
            self.disc += [c for c in self.hand if not c.startswith("Part")]
            self.hand = keep
            inc = self.incoming(); d = max(0.0, inc - self.block)
            self.hp -= d; self.dmg_taken += d
            if self.inf and d > 0: self.infstack += int(d)
            for e in self.alive():
                e.weak = max(0, e.weak - 1); e.vuln = max(0, e.vuln - 1); e.shrink = max(0, e.shrink - 1)
            if self.hp <= 0:
                st["deaths"] += 1; break
        st["len"] += self.turn; st["fights"] += 1; st["dmg_taken"] += self.dmg_taken
        st["nanoka_win"] += self.won_nanoka
        st["slots"] += self.slots; st["pots_end"] += len(self.pots)
        return self.hp


def run_set(deck, nruns, seed, upg=frozenset(), fights_per_run=16):
    rng = random.Random(seed)
    by = {k: defaultdict(float) for k in ENC}
    kinds = list(ENC); ws = [ENC[k][0] for k in kinds]
    for _ in range(nruns):
        pots = []; exp = 0; hp = 80.0
        for i in range(fights_per_run):
            kind = "boss" if i == fights_per_run - 1 else rng.choices(kinds, ws)[0]
            lv = sum(1 for x in LV_EXP if exp >= x)
            slots = 3 + LV_SLOTS[lv]
            hp = max(hp, 50.0)
            if len(pots) > slots: del pots[slots:]
            c = Combat(deck, kind, rng, by[kind], pots, slots, hp, upg)
            hp = c.run(); exp += EXP[kind]
            if hp <= 0: hp = 60.0
            hp = min(80.0, hp + 8)  # 休息／事件小幅回血
    return by


def wavg(by, key, per=None):
    num = den = 0.0
    for k, s in by.items():
        if s["fights"] == 0: continue
        if per:
            if s[per] == 0: continue
            v = s[key] / s[per]
        else:
            v = s[key] / s["fights"]
        num += v * IMP[k]; den += IMP[k]
    return num / den if den else 0.0
