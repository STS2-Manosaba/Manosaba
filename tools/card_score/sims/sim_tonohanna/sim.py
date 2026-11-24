"""遠野漢娜簡化戰鬥蒙地卡羅模型（v5）。規則依 Manosaba 原始碼：
- 羽扇（FeatherFan）Lv2 起：擁有 SoarPower / FloatingPower / SkyIslandPower 時，自身 powered 攻擊 x1.5。
  注意：漢娜人偶與 High Sense 給的是 HannaPuppetPower（外觀為翱翔），程式碼不在羽扇判定清單中，預設不吃加傷
  （FAN_COUNTS_HANNA_POWER 可切換比較）。經驗表 [0,2,7,15,30,50]，一般戰 1 經驗 → 第 3 場起即 Lv2。
- 人偶收藏層數 = 本場打出過的不同人偶種類數；冰擊 25+5x、晚安 2+x 回血。
- 魔女化 Hill 曲線（100 層 x1.84，只影響 powered 攻擊），戰鬥內不衰減；>=100 層時給一張空島到手牌（並加入牌組）。
- 漂浮：受 powered 攻擊 -20%，回合開始 -1；空島：受所有傷害 -50%，回合開始 -1；HannaPuppetPower：受攻擊 -50%，下回合開始移除。
- 每回合抽 5、3 能量；敵人固定血量／傷害曲線；貪婪出牌（分數/費用最高者）。
"""
from __future__ import annotations
import random
from collections import defaultdict

ENC = {  # 出現比例, 總血量範圍, 敵人數範圍, 基礎傷害, 每回合成長, 每敵每回合攻擊段數
    "hallway": (0.72, (55, 80), (1, 3), 8.0, 1.0, 1.0),
    "elite":   (0.20, (140, 175), (1, 2), 11.0, 1.2, 1.5),
    "boss":    (0.08, (440, 520), (1, 1), 10.0, 1.1, 2.0),
}
IMP = {"hallway": 1.0, "elite": 2.0, "boss": 3.0}
FAN_LV2 = True
FAN_COUNTS_HANNA_POWER = False

COST = {"Strike": 1, "Defend": 1, "ShyGlance": 1, "Thrifty": 1, "Suspicion": 0, "Trauma": 0,
        "HiroPuppet": 0, "NanokaPuppet": 2, "NoahPuppet": 1, "AnAnPuppet": 1, "AlisaPuppet": 1, "CocoPuppet": 1,
        "EmaPuppet": 1, "HannaPuppet": 1, "LeiaPuppet": 1, "MargoPuppet": 1, "MeruruPuppet": 1, "SherryPuppet": 1,
        "Sewing": 1, "IceStrike": 3, "GoodnightYoungLady": 3, "EmergencySuture": 2, "ToughItOut": 1, "SlipFall": 0,
        "MealWithReward": 0, "DreamsInjuredWings": 1, "WitchIslandCanFly": 2, "GiveHannaABeer": 1,
        "AngelsDice": 1, "DevilsDice": 1, "LittleMatchGirl": 1, "RefuseMajo": 3, "SteelBallRun": 3, "SkyIsland": 3,
        "HighSense": 3, "Boulder": 0, "Toxic": 1, "Dazed": 9, "Wound": 9}
PUPPETS = {"HiroPuppet", "NanokaPuppet", "NoahPuppet", "AnAnPuppet", "AlisaPuppet", "CocoPuppet", "EmaPuppet",
           "HannaPuppet", "LeiaPuppet", "MargoPuppet", "MeruruPuppet", "SherryPuppet"}
ATTACKS = {"Strike", "ShyGlance", "HiroPuppet", "NanokaPuppet", "IceStrike", "EmergencySuture", "SlipFall",
           "GiveHannaABeer", "Boulder"}
POWERS = {"CocoPuppet", "EmaPuppet", "LeiaPuppet", "MargoPuppet", "LittleMatchGirl", "SteelBallRun", "SkyIsland",
          "HighSense"}
STATUS = {"Dazed", "Wound", "Toxic"}
EXHAUST = {"AnAnPuppet", "HannaPuppet", "MeruruPuppet", "GoodnightYoungLady", "Boulder", "Toxic"}
DICE = {"AngelsDice", "DevilsDice", "GiveHannaABeer"}

STARTER = ["Strike"] * 3 + ["Defend"] * 3 + ["ShyGlance", "Thrifty", "Suspicion", "Trauma"]
ARCH = {
    # 人偶收藏型（含羽扇升級送的人偶）
    "puppet": STARTER + ["HiroPuppet", "NanokaPuppet", "NoahPuppet", "AnAnPuppet", "AlisaPuppet", "CocoPuppet",
                         "HannaPuppet", "Sewing", "NoahPuppet", "WitchIslandCanFly"],
    # 狀態牌型
    "status": STARTER + ["ToughItOut", "SlipFall", "SlipFall", "MealWithReward", "EmergencySuture", "Thrifty",
                         "NoahPuppet", "HiroPuppet", "WitchIslandCanFly", "Suspicion"],
    # 飛行／骰子型
    "fly": STARTER + ["DreamsInjuredWings", "WitchIslandCanFly", "WitchIslandCanFly", "GiveHannaABeer",
                      "AngelsDice", "DevilsDice", "HannaPuppet", "HiroPuppet", "NanokaPuppet", "Trauma"],
}


class Enemy:
    __slots__ = ("hp", "weak", "vuln", "burn", "tmpstr")

    def __init__(self, hp):
        self.hp = hp; self.weak = 0; self.vuln = 0; self.burn = 0; self.tmpstr = 0.0


class Combat:
    def __init__(self, deck, kind, rng, st, probe=None):
        self.rng = rng; self.kind = kind; self.st = st; self.probe = probe
        _, hpr, nr, self.bdmg, self.grow, self.hits = ENC[kind]
        n = rng.randint(*nr); tot = rng.uniform(*hpr)
        self.enemies = [Enemy(tot / n) for _ in range(n)]
        self.draw = [[c, False] for c in deck]; rng.shuffle(self.draw)   # [name, upgraded]
        for inn in ("HighSense+", "MargoPuppet+", "CocoPuppet+"):
            if probe == inn:
                for i, c in enumerate(self.draw):
                    if c[0] == inn[:-1]:
                        c[1] = True; self.draw.append(self.draw.pop(i)); break
        self.up_probe = probe.endswith("+") if probe else False
        if self.up_probe:
            for c in self.draw:
                if c[0] == probe[:-1]:
                    c[1] = True
        self.hand = []; self.disc = []; self.exh = []
        self.hp = 66.0; self.block = 0.0; self.str = 0.0; self.majoka = 0
        self.floating = 0; self.sky = 0; self.hannapow = False; self.weak_self = 0
        self.collection = set(); self.coco = 0; self.ema = 0; self.margo = 0; self.match = 0
        self.sherry = 0; self.hs = False; self.hs_cd = 0; self.dice_bonus = 0; self.skyisland_given = False
        self.turn = 0; self.energy = 0; self.sbr_turn = None; self.dmg_dealt = 0.0
        if any(c[0] == "SkyIsland" for c in self.draw): self.skyisland_given = True

    # ---------- 基本 ----------
    def alive(self):
        return [e for e in self.enemies if e.hp > 0]

    def flying(self):
        return self.floating > 0 or self.sky > 0 or (FAN_COUNTS_HANNA_POWER and self.hannapow)

    def mult(self):
        s = self.majoka; m = 1.0
        if s > 0:
            r = s ** 1.8365 / (s ** 1.8365 + 166.6 ** 1.8365); m = 1 + 3 * r
        if FAN_LV2 and self.flying():
            m *= 1.5
        return m

    def hit(self, e, base, powered=True, tag=None):
        if e.hp <= 0:
            return 0.0
        d = base + (self.str if powered else 0)
        if powered:
            d *= self.mult()
            if e.vuln > 0:
                d *= 1.5
        d = max(0.0, d); real = min(e.hp, d); e.hp -= d; self.dmg_dealt += real
        if powered and FAN_COUNTS_HANNA_POWER and self.hannapow and not (self.floating > 0 or self.sky > 0):
            self.st["hanna_fan"] += real / 3
        if tag:
            self.st[tag] += real
        return real

    def target(self):
        al = self.alive()
        return min(al, key=lambda e: e.hp) if al else None

    def attack(self, base, tag=None, aoe=False):
        if aoe:
            return sum(self.hit(e, base, tag=tag) for e in self.alive())
        t = self.target()
        return self.hit(t, base, tag=tag) if t else 0.0

    def all_cards(self):
        return self.draw + self.hand + self.disc   # 不含消耗堆

    def add_status(self, name, pile="disc"):
        (self.disc if pile == "disc" else self.draw).append([name, False])

    def draw_n(self, n):
        for _ in range(n):
            if not self.draw:
                if not self.disc:
                    return
                self.draw = self.disc; self.disc = []; self.rng.shuffle(self.draw)
            c = self.draw.pop()
            self.hand.append(c)
            if c[0] in STATUS and self.match:
                heal = min(self.match, 66 - self.hp); self.hp += heal
                self.st["match_heal"] += self.match; self.st["match_heal_eff"] += heal; self.st["match_procs"] += 1

    def incoming(self):
        tot = 0.0
        for e in self.alive():
            d = max(0.0, self.bdmg + self.grow * (self.turn - 1) - e.tmpstr) * self.hits
            if e.weak > 0:
                d *= 0.75
            tot += d
        return tot * self.reduction()

    def reduction(self):
        r = 1.0
        if self.floating > 0: r *= 0.8
        if self.hannapow: r *= 0.5
        if self.sky > 0: r *= 0.5
        return r

    # ---------- 出牌評分 ----------
    def est_dmg(self, base):
        t = self.target()
        if not t:
            return 0
        d = (base + self.str) * self.mult() * (1.5 if t.vuln else 1)
        return min(d, t.hp + 3)

    def need_block(self):
        return max(0.0, self.incoming() - self.block)

    def score(self, c):
        n, up = c
        nb = self.need_block(); turnleft = max(1.0, 6 - self.turn)
        puppet_bonus = 0.25 + (self.coco + 0) / 5 + (self.ema * len(self.alive()) / 6) if n in PUPPETS else 0
        if n in STATUS:
            return -1
        if n == "Strike": return self.est_dmg(9 if up else 6) / 6
        if n == "Defend": return min(nb, 8 if up else 5) / 5 + 0.05
        if n == "ShyGlance": return sum(min(e.hp, (4 + self.str) * self.mult()) for e in self.alive()) / 6 + 0.3
        if n == "Thrifty":
            k = sum(1 for x in self.hand if x[0] in STATUS)
            return min(nb, 6 + k * (3 if up else 2)) / 5 + k * 0.2 + 0.05
        if n == "Suspicion": return 0.9 if any(COST[x[0]] > 0 and x[0] not in STATUS and x is not c for x in self.hand) else 0.1
        if n == "Trauma": return 0.3
        if n == "Boulder": return self.est_dmg(20) / 6
        if n == "HiroPuppet": return self.est_dmg(8 if up else 6) / 6 + puppet_bonus + 0.3
        if n == "NanokaPuppet":
            k = sum(1 for x in self.all_cards() + self.exh if x[0] in PUPPETS)
            return self.est_dmg((9 if up else 6) + 2 * k) / 6 + puppet_bonus
        if n == "NoahPuppet": return min(nb, 9 if up else 7) / 5 + puppet_bonus + 0.1
        if n == "AnAnPuppet":
            k = len(self.alive()) * self.hits * (8 if up else 6) * self.reduction()
            return min(nb, k) / 5 + puppet_bonus
        if n == "AlisaPuppet": return (15 if up else 10) * 0.8 / 6 + puppet_bonus
        if n == "CocoPuppet": return 1.5 * turnleft / 5 + puppet_bonus
        if n == "EmaPuppet": return 2.5 * turnleft / 5 + puppet_bonus
        if n == "LeiaPuppet": return 0.1 + puppet_bonus
        if n == "MargoPuppet": return 0.8 * turnleft / 5 + puppet_bonus
        if n == "MeruruPuppet":
            return (0.9 if self.exh else -0.5) + puppet_bonus
        if n == "HannaPuppet": return min(nb, self.incoming()) / 5 * 0.5 + puppet_bonus
        if n == "SherryPuppet":
            atk = [x for x in self.hand if x[0] in ATTACKS and x is not c and COST[x[0]] <= self.energy - 1]
            b = max((self.score(x) for x in atk), default=0)
            return b + puppet_bonus + (3 if "HannaPuppet" in self.collection else 0)
        if n == "Sewing":
            return min(nb, 6) / 5 + (0.3 if any(x[0] in PUPPETS and not x[1] for x in self.hand) else 0)
        if n == "IceStrike": return self.est_dmg((30 if up else 25) + (6 if up else 5) * len(self.collection)) / 6
        if n == "GoodnightYoungLady": return -1   # 另外處理
        if n == "EmergencySuture":
            k = sum(1 for x in self.all_cards() if x[0] in STATUS)
            return k * (7 if up else 4) * len(self.alive()) * self.mult() / 6 * 0.9
        if n == "ToughItOut": return min(nb, 17 if up else 13) / 5 - 0.2
        if n == "SlipFall": return self.est_dmg(5 if up else 3) / 6 + 0.6
        if n == "MealWithReward": return 1.0
        if n in ("DreamsInjuredWings", "WitchIslandCanFly"):
            atk = sum(self.est_dmg(6) for x in self.hand if x[0] in ATTACKS) if not self.flying() else 0
            fly = 0 if self.floating else (self.incoming() * 0.2 / 5 + atk * 0.5 / 6 * 0.6)
            return fly + (min(nb, 10 if up else 7) / 5 if n == "WitchIslandCanFly" else 0)
        if n == "GiveHannaABeer": return self.est_dmg(10.5 + 3 * self.dice_bonus) / 6
        if n == "AngelsDice": return min(nb, (3.5 + self.dice_bonus) * (3 if up else 2)) / 5
        if n == "DevilsDice": return min(nb, (3.5 + self.dice_bonus) * self.hits * len(self.alive())) / 5
        if n == "LittleMatchGirl": return 0.6 if self.hp < 60 else 0.3
        if n == "RefuseMajo":
            others = [x for x in self.hand if x is not c]
            if self.majoka >= 100 or len(others) < 2:
                return -1
            gain = sum(min(20 * self.mult(), 40) for _ in others) / 6 * 0.75
            lost = sum(0.6 for x in others if x[0] not in STATUS)
            return gain - lost
        if n == "SteelBallRun": return 3.0 if self.majoka < 100 else -1
        if n == "SkyIsland":
            lay = int(5 * min(self.majoka / 100, 1))
            return lay * 1.6 - 0.5
        if n == "HighSense": return 0.7 * turnleft
        return 0

    # ---------- 打出 ----------
    def play(self, c):
        n, up = c
        self.hand.remove(c)
        cost = COST[n]
        if n == "Boulder":
            cost = 0
        if n in ("RefuseMajo", "SteelBallRun", "SkyIsland", "GoodnightYoungLady") and up:
            cost -= 1
        self.energy -= cost
        st = self.st; pr = self.probe.rstrip("+") if self.probe else None
        if n == pr:
            st["probe_plays"] += 1; st["probe_turn"] += self.turn
        dest = "exh" if n in EXHAUST else ("none" if n in POWERS else "disc")
        mt = 2 if (self.sherry > 0 and n in ATTACKS) else 1
        if mt == 2:
            self.sherry -= 1
        for rep in range(mt):
            tag = "sherry_extra" if rep == 1 else None
            if n == "Strike": self.attack(9 if up else 6, tag)
            elif n == "Defend": self.block += 8 if up else 5
            elif n == "ShyGlance":
                self.attack(7 if up else 4, tag, aoe=True)
                for e in self.alive(): e.weak += 1
            elif n == "Thrifty":
                k = [x for x in self.hand if x[0] in STATUS]
                for x in k: self.hand.remove(x); self.exh.append(x)
                self.block += 6 + len(k) * (3 if up else 2)
                if pr == "Thrifty": st["thrifty_status"] += len(k)
            elif n == "Suspicion": self.energy += 2 if up else 1; self.add_status("Dazed")
            elif n == "Trauma": self.add_majoka(15 if up else 10)
            elif n == "Boulder":
                d = self.attack(30 if up else 20, tag)
                if self.probe and self.probe.rstrip("+") in ("RefuseMajo", "SherryPuppet"): st["boulder_dmg"] += d
            elif n == "HiroPuppet":
                self.attack(8 if up else 6, tag); self.disc.append([n, up])
            elif n == "NanokaPuppet":
                k = sum(1 for x in self.all_cards() + self.exh if x[0] in PUPPETS) + 1
                d = self.attack((9 if up else 6) + 2 * k, tag)
                if pr == "NanokaPuppet": st["nanoka_k"] += k
                if "EmaPuppet" in self.collection: self.block += 0.2 * d
            elif n == "NoahPuppet":
                if "AnAnPuppet" in self.collection:
                    for e in self.alive(): e.weak += 2 if up else 1
                self.block += 9 if up else 7
                if self.hand:
                    if up:
                        s = [x for x in self.hand if x[0] in STATUS]
                        x = s[0] if s else None
                    else:
                        x = self.rng.choice(self.hand)
                    if x is not None:
                        self.hand.remove(x); self.exh.append(x)
                        st["noah_exh_status" if x[0] in STATUS else "noah_exh_good"] += 1
            elif n == "AnAnPuppet":
                if "NoahPuppet" in self.collection: self.block += 3
                for e in self.alive(): e.tmpstr += 8 if up else 6
            elif n == "AlisaPuppet":
                t = self.target()
                if t: t.burn += (15 if up else 10) + (3 if "EmaPuppet" in self.collection else 0)
            elif n == "CocoPuppet": self.coco += 3 if up else 2
            elif n == "EmaPuppet": self.ema += 12 if up else 9
            elif n == "MargoPuppet": self.margo += 1
            elif n == "MeruruPuppet":
                cand = [x for x in self.exh if x[0] not in STATUS]
                if pr == "MeruruPuppet":
                    st["meruru_has_target"] += 1 if cand else 0
                if cand:
                    best = max(cand, key=lambda x: self.score(x))
                    self.exh.remove(best); self.hand.append(best)
                    if pr == "MeruruPuppet": st["meruru_fetch_score"] += max(0, self.score(best)); st["meruru_fetch_" + best[0]] += 1
                    if best[0] == "AnAnPuppet": self.heal(3)
                if up: dest = "disc"
            elif n == "HannaPuppet":
                if "SherryPuppet" in self.collection: self.str += 2; st["hanna_str"] += 2
                self.hannapow = True
                if up: dest = "disc"
            elif n == "SherryPuppet":
                if "HannaPuppet" in self.collection:
                    self.hand.append(["Boulder", False]); st["sherry_boulder"] += 1
                self.sherry += 1
            elif n == "Sewing":
                self.block += 6
                ps = [x for x in self.hand if x[0] in PUPPETS and not x[1]]
                for x in (ps if up else ps[:1]): x[1] = True
            elif n == "IceStrike":
                if pr == "IceStrike": st["ice_coll"] += len(self.collection)
                self.attack((30 if up else 25) + (6 if up else 5) * len(self.collection), tag)
            elif n == "GoodnightYoungLady":
                h = 2 + len(self.collection)
                if pr == "GoodnightYoungLady": st["gn_heal"] += h; st["gn_coll"] += len(self.collection); st["gn_energy_lost"] += self.energy
                self.heal(h); self.endturn_flag = True
            elif n == "EmergencySuture":
                ss = [x for x in self.all_cards() if x[0] in STATUS]
                if pr == "EmergencySuture": st["es_status"] += len(ss)
                for pile in (self.draw, self.hand, self.disc):
                    for x in [x for x in pile if x[0] in STATUS]: pile.remove(x); self.exh.append(x)
                for _ in range(len(ss)): self.attack(7 if up else 4, tag, aoe=True)
            elif n == "ToughItOut": self.block += 17 if up else 13; self.add_status("Wound"); self.add_status("Wound")
            elif n == "SlipFall":
                t = self.target()
                if t: self.hit(t, 5 if up else 3, tag=tag); t.vuln += 2
                self.add_status("Wound")
            elif n == "MealWithReward": self.draw_n(3 if up else 2); self.add_status("Toxic")
            elif n == "DreamsInjuredWings": self.floating += 2 if up else 1
            elif n == "WitchIslandCanFly": self.block += 10 if up else 7; self.floating = max(self.floating, 1)
            elif n == "GiveHannaABeer":
                db = self.dice_bonus
                dmg = sum(min(6, self.rng.randint(4 if self.majoka > 50 else 1, 6) + db) for _ in range(3))
                self.attack(dmg, tag)
                if db: st["dice_bonus_used"] += 1
            elif n == "AngelsDice":
                self.block += min(6, self.rng.randint(1, 6) + self.dice_bonus) * (3 if up else 2)
                if self.dice_bonus: st["dice_bonus_used"] += 1
            elif n == "DevilsDice":
                v = min(6, self.rng.randint(1, 6) + self.dice_bonus)
                for e in self.alive(): e.tmpstr += v
                if self.dice_bonus: st["dice_bonus_used"] += 1
            elif n == "LittleMatchGirl": self.match += 3 if up else 2
            elif n == "RefuseMajo":
                others = list(self.hand)
                if pr == "RefuseMajo":
                    st["rm_converted"] += len(others); st["rm_status_conv"] += sum(1 for x in others if x[0] in STATUS)
                    st["rm_majoka_lost"] += self.majoka
                self.hand = [["Boulder", False] for _ in others]
                self.majoka = 0
            elif n == "SteelBallRun":
                self.sbr_turn = self.turn
                if pr == "SteelBallRun": st["sbr_turn"] += self.turn
                self.add_majoka(100 - self.majoka)
            elif n == "SkyIsland":
                lay = int(5 * min(self.majoka / 100, 1))
                if pr in ("SkyIsland", "SteelBallRun"): st["sky_layers"] += lay; st["sky_plays"] += 1
                self.sky += lay
            elif n == "HighSense":
                if self.hs: self.hs_cd = max(0, self.hs_cd - 1)
                self.hs = True
            if mt == 2 and rep == 0 and n == "Strike" and False:
                pass
        if n in PUPPETS:
            self.collection.add(n)
            if self.coco: self.block += self.coco
            if self.ema:
                for e in self.alive(): self.hit(e, self.ema, powered=False, tag="ema_dmg" if pr == "EmaPuppet" else None)
                if n == "HiroPuppet":
                    for e in self.alive(): self.hit(e, 2 * self.ema / 9, powered=False)
        if dest == "exh": self.exh.append([n, up])
        elif dest == "disc": self.disc.append([n, up])

    def heal(self, h):
        self.hp = min(66.0, self.hp + h)

    def add_majoka(self, n):
        self.majoka += n
        if self.majoka >= 100 and not self.skyisland_given:
            self.skyisland_given = True; self.hand.append(["SkyIsland", False])
            self.st["sky_given"] += 1

    # ---------- 回合 ----------
    def start_turn(self):
        self.turn += 1
        self.block = 0.0
        if self.floating: self.floating -= 1
        if self.sky: self.sky -= 1
        self.hannapow = False
        self.energy = 3
        if self.hs and self.hs_cd > 0: self.hs_cd -= 1
        pr = self.probe.rstrip("+") if self.probe else None
        for _ in range(self.margo):
            ps = [x for x in self.disc if x[0] in PUPPETS]
            if not ps:
                if pr == "MargoPuppet": self.st["margo_miss"] += 1
                break
            x = self.rng.choice(ps); self.disc.remove(x); self.hand.append(x)
            if pr == "MargoPuppet": self.st["margo_fetch"] += 1
            if x[0] == "MeruruPuppet": self.add_majoka(10)
        self.draw_n(5)
        # High Sense：冷卻好且有傷害來時使用
        if self.hs and self.hs_cd <= 0 and self.incoming() > 8:
            self.hs_cd = 3; self.hannapow = True; self.dice_bonus = 2
            if pr == "HighSense":
                self.st["hs_uses"] += 1
                self.st["hs_prevent"] += self.incoming()  # 已含 0.5 → 等於少受量的上界
        else:
            self.dice_bonus = 0

    def player_turn(self):
        self.endturn_flag = False
        for _ in range(30):
            if self.endturn_flag or not self.alive():
                break
            best = None; bs = 0.0
            for c in self.hand:
                cost = COST[c[0]]
                if c[0] == "Boulder": cost = 0
                if c[1] and c[0] in ("RefuseMajo", "SteelBallRun", "SkyIsland", "GoodnightYoungLady"): cost -= 1
                if cost > self.energy or c[0] in STATUS:
                    continue
                s = self.score(c)
                # RefuseMajo 優先在花能量前打（需 3 費）
                r = s / max(cost, 0.5)
                if c[0] == "RefuseMajo" and s > 0: r = 99
                if r > bs: bs = r; best = c
            if best is None or bs <= 0.05:
                break
            self.play(best)
        # 晚安小姐：剩餘能量足且缺血時收尾打出
        if not self.endturn_flag:
            for c in list(self.hand):
                if c[0] == "GoodnightYoungLady" and self.energy >= (2 if c[1] else 3) and 66 - self.hp >= 2 + len(self.collection):
                    self.play(c); break

    def end_turn(self):
        nh = []
        for c in self.hand:
            if c[0] == "Toxic":
                self.take(5, attack=False); nh.append(c)
            elif c[0] == "Dazed":
                self.exh.append(c)
            elif c[0] == "SherryPuppet" and c[1]:
                nh.append(c)   # 保留
            elif c[0] == "Boulder":
                self.disc.append(c)
            else:
                nh.append(c)
        self.disc.extend(x for x in nh if not (x[0] == "SherryPuppet" and x[1]))
        self.hand = [x for x in nh if x[0] == "SherryPuppet" and x[1]]
        self.sherry = 0
        # 敵方回合開始：灼燒
        for e in self.alive():
            if e.burn > 0:
                d = min(e.hp, e.burn); e.hp -= e.burn; self.dmg_dealt += d; e.burn -= 1
        inc = self.incoming()
        raw = inc / self.reduction() if self.reduction() else inc
        if self.hannapow and self.probe and self.probe.rstrip("+") == "HighSense":
            self.st["hs_prevent_real"] += raw - inc
        if self.sky and self.probe and self.probe.rstrip("+") in ("SkyIsland", "SteelBallRun"):
            self.st["sky_prevent"] += raw * (1 - 0.5) * (0.8 if self.floating else 1) * (0.5 if self.hannapow else 1)
        self.take(inc)
        for e in self.enemies:
            e.tmpstr = 0.0
            if e.weak: e.weak -= 1
            if e.vuln: e.vuln -= 1

    def take(self, d, attack=True):
        b = min(self.block, d); self.block -= b; d -= b
        self.hp -= d; self.st["dmg_taken"] += d

    def run(self):
        while self.turn < 25:
            self.start_turn()
            self.player_turn()
            if not self.alive():
                break
            self.end_turn()
            if self.hp <= 0:
                self.st["deaths"] += 1; break
        st = self.st
        st["len"] += self.turn
        st["coll_end"] += len(self.collection)
        st["majoka_end"] += self.majoka
        st["hp_end"] += max(0, self.hp)
        if self.sbr_turn is not None:
            st["sbr_active_turns"] += self.turn - self.sbr_turn + 1


def run(deck, n, seed, probe=None):
    rng = random.Random(seed)
    kinds = list(ENC); w = [ENC[k][0] for k in kinds]
    by = {k: defaultdict(float) for k in kinds}
    for _ in range(n):
        k = rng.choices(kinds, w)[0]
        s = by[k]; s["fights"] += 1
        Combat(deck, k, rng, s, probe).run()
    return by
