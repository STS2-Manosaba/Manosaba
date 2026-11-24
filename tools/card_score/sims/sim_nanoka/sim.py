"""黑部奈葉香簡化戰鬥蒙地卡羅模型（v5）。規則依 Manosaba 原始碼：
- 魔法槍 MagicalGun：戰鬥開始子彈 1（Ribbon Lv2+ +1、Lv4+ +2），每回合開始 +1（魔女化>=100 時 +2），上限 6。
  槍擊牌（GunBase）需有子彈才可打出。
- 緞帶 Ribbon：本場首次「未格擋傷害」時召喚看守（Lv1 10HP，每級 +5，Lv5 30HP）並 +30 魔女化，該擊由看守承受；
  看守存活時所有未格擋傷害整擊轉給看守（溢出不回流）；每次轉移且造成傷害 +10 魔女化；
  敵方回合中本人與看守都沒受未格擋傷害 → 下回合開始看守回復 5（Lv1，Lv5 為 10），看守死亡時會以此復活。
- 魔女化：所有非 Unpowered 傷害 ×(1+3·s^1.8365/(s^1.8365+166.6^1.8365))；>=100 時給幻覺魔法(手牌)、
  殺人衝動 floor(s/100) 層：攻擊牌造成總傷 D 後對看守造成 D×層×10%（大廳預設）。
- 精準：槍擊攻擊牌傷害 ×(1+精準/100)，打出後移除（露營中不移除）。
- 每回合 5 抽、3 能量；敵人用固定血量／傷害曲線；死亡不中止戰鬥（僅記錄）。
"""
from __future__ import annotations
import random, math

ENC = {  # 出現比例, 總血量範圍, 敵人數範圍, 每回合總傷害基底, 每回合成長, 每敵段數
    "hallway": (0.72, (50, 70), (1, 3), 8.0, 1.0, 1),
    "elite":   (0.20, (110, 135), (1, 2), 11.0, 1.0, 2),
    "boss":    (0.08, (250, 290), (1, 1), 12.0, 0.8, 2),
}
IMP = {"hallway": 1.0, "elite": 2.0, "boss": 3.0}

# name: (cost, type, bullets)
CARDS = {
    "Strike": (1, "A", 0), "Defend": (1, "S", 0), "Trauma": (0, "S", 0), "GunShot": (1, "A", 1),
    "Aim": (1, "S", 0), "WarningShot": (0, "S", 1), "Snipe": (1, "A", 1), "RecoilShot": (2, "A", 1),
    "TakeCover": (1, "S", 0), "SorryIMissed": (1, "S", 1), "SteadyShot": (1, "P", 0),
    "WeakeningShot": (1, "A", 1), "WoundShot": (1, "A", 1), "TrapSetting": (2, "S", 0),
    "SurppressiveShooting": (3, "A", 3), "DarkSight": (2, "S", 0), "ThreeSixtyNoScope": (0, "A", 1),
    "BouncingShot": (1, "A", 1), "HallucinationMagic": (1, "S", 0), "MysteriousJungler": (1, "P", 0),
    "JailerRegeneration": (2, "P", 0), "EscapeRoute": (1, "S", 0), "EnvironmentExplore": (1, "S", 0),
    "Reload": (0, "S", 0), "Buttstroke": (1, "A", 0), "Camping": (2, "S", 0), "DeckShot": (0, "S", 1),
    "EmergencyBlock": (1, "S", 0), "MyTurnToProtect": (1, "S", 0), "NanokaSearch": (1, "P", 0),
    "BurningWound": (3, "P", 0), "ActingCool": (2, "P", 0), "WardenAttack": (2, "P", 0),
    "RelicSearch": (3, "P", 0), "BestInfoMagic": (1, "P", 0),
}
GUN_ATTACK = {"GunShot", "Snipe", "RecoilShot", "WeakeningShot", "WoundShot", "SurppressiveShooting",
              "ThreeSixtyNoScope", "BouncingShot"}
EXHAUST = {"WarningShot", "SurppressiveShooting", "DeckShot"}
INNATE = {"Snipe", "BestInfoMagic"}
POWERS = {k for k, v in CARDS.items() if v[1] == "P"}

STARTER = ["Strike"] * 3 + ["Defend"] * 4 + ["Trauma", "GunShot", "GunShot"]
ARCH = {
    "gun": STARTER + ["Aim", "TakeCover", "WoundShot", "WeakeningShot", "ThreeSixtyNoScope", "SteadyShot",
                      "RecoilShot", "EscapeRoute", "BouncingShot", "Snipe"],
    "guard": STARTER + ["WardenAttack", "ActingCool", "JailerRegeneration", "MyTurnToProtect", "EscapeRoute",
                        "RecoilShot", "TakeCover", "WoundShot", "Buttstroke", "GunShot"],
    "control": STARTER + ["WarningShot", "SurppressiveShooting", "TrapSetting", "Buttstroke", "EnvironmentExplore",
                          "NanokaSearch", "MysteriousJungler", "Reload", "WeakeningShot", "EscapeRoute"],
}


def majoka_mult(s):
    if s <= 0:
        return 1.0
    a = s ** 1.8365; b = 166.6 ** 1.8365
    return 1 + 3 * a / (a + b)


class Enemy:
    __slots__ = ("hp", "vuln", "weak", "strloss", "stun", "trap")

    def __init__(self, hp):
        self.hp = hp; self.vuln = 0; self.weak = 0; self.strloss = 0; self.stun = False; self.trap = 0


class Combat:
    def __init__(self, deck, kind, rng, st, level=1, probe=None):
        self.rng = rng; self.kind = kind; self.st = st; self.probe = probe; self.level = level
        _, hpr, nr, self.bdmg, self.grow, self.hits = ENC[kind]
        n = rng.randint(*nr); tot = rng.uniform(*hpr)
        self.n0 = n
        self.enemies = [Enemy(tot / n) for _ in range(n)]
        self.draw = list(deck); rng.shuffle(self.draw)
        for c in list(self.draw):
            if c in INNATE:
                self.draw.remove(c); self.draw.append(c)
        self.hand = []; self.disc = []; self.exh = []
        self.hp = 60.0; self.block = 0.0; self.dex = 0; self.str = 0
        self.bullets = 1 + (2 if level >= 4 else 1 if level >= 2 else 0)
        self.majoka = 0.0; self.acc = 0.0
        self.guard_hp = None  # None=未召喚, 0=死亡
        self.guard_max = 10 + 5 * (level - 1)
        self.safe_heal = 5 + 5 * (level - 1) / 4
        self.summoned = False
        self.hm_given = False
        self.powers = {}
        self.steady_bonus = 0.0
        self.next_energy = 0; self.next_draw = 0; self.next_block = 0.0
        self.camping_round = None; self.darksight = False
        self.turn = 0; self.energy = 0; self.maxe = 3
        self.unsafe = True
        self.d = {}

    def add(self, k, v=1.0):
        self.d[k] = self.d.get(k, 0.0) + v

    def alive(self):
        return [e for e in self.enemies if e.hp > 0]

    def draw_n(self, n):
        for _ in range(n):
            if not self.draw:
                if not self.disc:
                    return
                self.draw = self.disc; self.disc = []; self.rng.shuffle(self.draw)
            if len(self.hand) >= 10:
                return
            self.hand.append(self.draw.pop())

    def gain_majoka(self, x, src=""):
        before = self.majoka
        self.majoka += x
        self.add("majoka_" + src, x)
        if before < 100 <= self.majoka:
            self.d["m100_turn"] = self.turn
            if not self.hm_given:
                self.hm_given = True
                if len(self.hand) < 10:
                    self.hand.append("HallucinationMagic")

    def deal(self, e, base, gun=False, unpowered=False):
        if e is None or e.hp <= 0:
            return 0.0
        dmg = base
        if not unpowered:
            dmg += self.str
            if gun:
                dmg += self.steady_bonus
            mm = majoka_mult(self.majoka)
            mult = mm
            if gun:
                mult *= 1 + self.acc / 100
                if e.trap and e.trap == self.turn:
                    mult *= 2
                    self.add("trap_extra", dmg * mult / 2)
            if e.vuln:
                mult *= 1.5
            dmg *= mult
            self.add("dmg_majoka_extra", dmg * (1 - 1 / mm))
        dmg = max(0.0, dmg)
        real = min(dmg, e.hp)
        e.hp -= dmg
        self.add("dmg", real)
        if gun and "BurningWound" in self.powers and real > 0:
            burn = math.floor(real * 0.3)
            self.add("burn_applied", burn)
            if e.hp > 0:
                self.add("burn_dmg", min(burn, e.hp))
            e.hp -= burn  # 近似：灼燒一次性兌現
        return real

    def murderous(self, total):
        stacks = int(self.majoka // 100)
        if stacks >= 1 and total > 0 and self.guard_hp and self.guard_hp > 0:
            x = total * stacks * 0.10
            self.guard_hp = max(0.0, self.guard_hp - x)
            self.add("mi_guard_dmg", x)
            if self.guard_hp <= 0:
                self.add("mi_guard_kill")

    def target(self):
        al = self.alive()
        return min(al, key=lambda e: e.hp) if al else None

    def incoming_hits(self):
        t = self.turn
        hits = []
        per = (self.bdmg + self.grow * (t - 1)) / self.n0
        for e in self.alive():
            if e.stun:
                continue
            for _ in range(self.hits):
                h = per / self.hits - e.strloss
                if e.weak:
                    h *= 0.75
                if h > 0:
                    hits.append(h)
        return hits

    def resolve_hits(self, hits, block, guard_hp, summoned, apply=False):
        pl = gd = 0.0; red = 0
        for h in hits:
            if self.darksight:
                continue
            ub = max(0.0, h - block); block = max(0.0, block - h)
            if ub <= 0:
                continue
            if guard_hp is not None and guard_hp > 0:
                gd += min(ub, guard_hp); guard_hp = max(0.0, guard_hp - ub); red += 1
                continue
            if not summoned:
                summoned = True; guard_hp = self.guard_max
                if apply:
                    self.summoned = True; self.d["summon_turn"] = self.turn
                    self.gain_majoka(30, "summon")
                gd += min(ub, guard_hp); guard_hp = max(0.0, guard_hp - ub); red += 1
                continue
            pl += ub
        if apply:
            self.guard_hp = guard_hp
        return pl, gd, red

    def pred_loss(self, block):
        pl, gd, _ = self.resolve_hits(self.incoming_hits(), block, self.guard_hp, self.summoned)
        return pl + 0.25 * gd

    def guns_in_hand(self, exclude=None):
        return [c for c in self.hand if c in GUN_ATTACK and c != exclude]

    def gun_base(self, c):
        return {"GunShot": 10, "Snipe": 30 if self.turn == 1 else 15, "RecoilShot": 6, "WeakeningShot": 10,
                "WoundShot": 10, "SurppressiveShooting": 12, "ThreeSixtyNoScope": 10, "BouncingShot": 10}[c]

    def dmg_value(self, base, gun=False, n=1):
        t = self.target()
        if t is None:
            return 0.0
        dmg = (base + self.str + (self.steady_bonus if gun else 0)) * majoka_mult(self.majoka)
        if gun:
            dmg *= 1 + self.acc / 100
            if t.trap == self.turn:
                dmg *= 2
        if t.vuln:
            dmg *= 1.5
        return min(dmg * n, sum(e.hp for e in self.alive()))

    def score(self, c):
        cost, typ, bul = CARDS[c]
        if cost > self.energy or bul > self.bullets:
            return -1
        if c in GUN_ATTACK:
            base = self.gun_base(c)
            v = self.dmg_value(base, True) / 6
            if c == "RecoilShot":
                v += (self.pred_loss(self.block) - self.pred_loss(self.block + 15 + self.dex)) / 5
            if c in ("WeakeningShot", "WoundShot"):
                v += 0.6
            if c == "BouncingShot" and len(self.alive()) > 1:
                v += self.dmg_value(8, True) / 6 * 0.8
            if c == "ThreeSixtyNoScope":
                v += 0.5
            if c == "SurppressiveShooting":
                v += 1.5
            if self.acc >= 100 and base < 10:
                v -= 0.5
            return v / max(cost, 0.5)
        if c == "Strike":
            return self.dmg_value(6) / 6
        if c == "Buttstroke":
            return self.dmg_value(3) / 6 + 0.9
        if c in ("Defend", "TakeCover", "MyTurnToProtect", "EmergencyBlock"):
            b = {"Defend": 5, "TakeCover": 7, "MyTurnToProtect": 6, "EmergencyBlock": 5}[c] + self.dex
            if c == "MyTurnToProtect" and self.guard_hp and 0 < self.guard_hp < self.guard_max * 0.2:
                b *= 2
            if self.camping_round == self.turn:
                b = 0
            v = (self.pred_loss(self.block) - self.pred_loss(self.block + max(0, b))) / 5
            if c == "TakeCover" and self.guns_in_hand():
                v += 0.5
            return v
        if c == "EscapeRoute":
            return 1.6
        if c == "Trauma":
            return 0.3
        if c in ("Aim", "SorryIMissed", "WarningShot"):
            gs = self.guns_in_hand()
            avail = self.bullets - (1 if c in ("SorryIMissed", "WarningShot") else 0)
            add = {"Aim": 60, "SorryIMissed": 150, "WarningShot": 30}[c]
            ec = self.energy - cost
            if gs and avail >= 1 and ec >= min(CARDS[g][0] for g in gs):
                b = max(self.gun_base(g) for g in gs)
                v = b * majoka_mult(self.majoka) * add / 100 / 6
            else:
                v = 0.3 * add / 100
            if c == "WarningShot":
                v += (self.pred_loss(self.block) - self.pred_loss_strloss(8)) / 5
            return v / max(cost, 0.5)
        if c in POWERS:
            if c in ("RelicSearch", "BestInfoMagic"):
                return 0.4
            return 2.5
        if c == "TrapSetting":
            return 0.9 if self.guns_in_hand() or self.bullets >= 2 else 0.3
        if c == "DarkSight":
            loss = self.pred_loss(self.block)
            return loss / 5 / 2 if loss > 12 else -1
        if c == "EnvironmentExplore":
            return 1.0 + 0.6 * len(self.guns_in_hand())
        if c == "Reload":
            return 0.4 if len(self.hand) >= 3 and self.energy >= 1 else -1
        if c == "HallucinationMagic":
            return 1.8
        if c == "Camping":
            return 0.2
        if c == "DeckShot":
            return -1
        return 0.1

    def pred_loss_strloss(self, x):
        saved = [e.strloss for e in self.enemies]
        for e in self.alive():
            e.strloss += x
        v = self.pred_loss(self.block)
        for e, s in zip(self.enemies, saved):
            e.strloss = s
        return v

    def play(self, c):
        cost, typ, bul = CARDS[c]
        self.energy -= cost
        self.hand.remove(c)
        if self.probe and c == self.probe:
            self.add("probe_plays"); self.add("probe_turn", self.turn)
        self.add("play_" + c)
        spent = False
        if bul:
            self.bullets -= bul; spent = True
            self.add("bullets_used", bul)
        tgt = self.target()
        total = 0.0
        if c in GUN_ATTACK:
            base = self.gun_base(c)
            self.add("gun_attacks")
            if self.acc > 0:
                self.add("acc_consumed", self.acc); self.add("acc_shot_base", (base + self.steady_bonus) * self.acc)
                self.add("acc_extra_dmg", (base + self.steady_bonus) * majoka_mult(self.majoka) * self.acc / 100)
            if self.steady_bonus:
                self.add("steady_extra", self.steady_bonus * majoka_mult(self.majoka) * (1 + self.acc / 100))
            if c == "SurppressiveShooting":
                for _ in range(3):
                    total += self.deal(tgt, 4, gun=True)
                if tgt:
                    tgt.stun = True
            elif c == "BouncingShot":
                d = 10.0
                order = [tgt] + [e for e in self.alive() if e is not tgt]
                for e in order:
                    total += self.deal(e, d, gun=True); d *= 0.8
            else:
                total += self.deal(tgt, base, gun=True)
            if c == "RecoilShot":
                self.block += 15 + self.dex
            if c == "WeakeningShot" and tgt:
                tgt.weak = 2
            if c == "WoundShot" and tgt:
                tgt.vuln = 2
            if c == "ThreeSixtyNoScope":
                self.draw_n(1)
            if "SteadyShot" in self.powers:
                self.steady_bonus += self.powers["SteadyShot"]
            if spent and self.camping_round != self.turn:
                self.acc = 0
            self.murderous(total)
        elif c == "Strike":
            total = self.deal(tgt, 6); self.murderous(total)
        elif c == "Buttstroke":
            total = self.deal(tgt, 3)
            if tgt:
                tgt.weak = max(tgt.weak, 1); tgt.vuln = max(tgt.vuln, 1)
            self.draw_n(1); self.murderous(total)
        elif c in ("Defend", "TakeCover", "MyTurnToProtect", "EmergencyBlock"):
            b = {"Defend": 5, "TakeCover": 7, "MyTurnToProtect": 6, "EmergencyBlock": 5}[c] + self.dex
            if c == "MyTurnToProtect":
                if self.guard_hp and 0 < self.guard_hp < self.guard_max * 0.2:
                    b *= 2; self.add("mttp_double")
            if self.camping_round != self.turn:
                self.block += max(0, b)
            if c == "TakeCover":
                self.acc += 30; self.add("acc_applied", 30)
        elif c == "EscapeRoute":
            self.next_block += 11 + self.dex
        elif c == "Trauma":
            self.gain_majoka(10, "trauma")
        elif c == "Aim":
            self.acc += 60; self.add("acc_applied", 60)
        elif c == "SorryIMissed":
            self.acc += 150; self.add("acc_applied", 150)
        elif c == "WarningShot":
            for e in self.alive():
                e.strloss += 8
            self.acc += 30; self.add("acc_applied", 30)
        elif c in POWERS:
            self.powers[c] = self.powers.get(c, 0) + 1
            if c == "MysteriousJungler":
                self.maxe += 1; self.dex -= 2
            if c == "NanokaSearch":
                self.next_energy += 3; self.next_draw += 2
            if c == "SteadyShot":
                self.d["steady_turn"] = self.turn
        elif c == "TrapSetting":
            if tgt:
                tgt.trap = self.turn + 1
        elif c == "DarkSight":
            self.add("darksight_saved", self.pred_loss_raw())
            self.darksight = True
            self.disc += self.hand; self.hand = []
            self.energy = 0
        elif c == "EnvironmentExplore":
            gs = self.guns_in_hand()
            for g in gs:
                self.hand.remove(g); self.disc.append(g)
            self.draw_n(2)
            self.next_draw += len(gs); self.next_energy += len(gs)
            self.add("ee_guns", len(gs))
        elif c == "Reload":
            n = len(self.hand)
            self.disc += self.hand; self.hand = []
            self.draw_n(n)
        elif c == "HallucinationMagic":
            k = int(min(self.majoka / 100, 1) * 10)
            pool = self.draw + self.disc
            good = sorted(pool, key=self.static_rank, reverse=True)[:min(k, 6)]
            for g in good:
                if g in self.draw:
                    self.draw.remove(g)
                else:
                    self.disc.remove(g)
            self.draw = self.draw + good[::-1]
            self.draw_n(1)
            self.add("hm_k", k)
        elif c == "Camping":
            self.camping_round = self.turn + 1
        if c in EXHAUST or c in POWERS:
            self.exh.append(c)
        else:
            self.disc.append(c)

    def pred_loss_raw(self):
        return self.pred_loss(self.block)

    @staticmethod
    def static_rank(c):
        r = {"SorryIMissed": 3, "Aim": 2.5, "Snipe": 2.6, "WoundShot": 2.4, "RecoilShot": 2.5,
             "WeakeningShot": 2.3, "GunShot": 2.0, "ThreeSixtyNoScope": 2.2, "BouncingShot": 2.1,
             "EscapeRoute": 2.0, "TakeCover": 1.9}
        return r.get(c, 1.0 if c not in POWERS else 3.0)

    def player_turn(self):
        self.turn += 1
        self.block = self.next_block; self.next_block = 0.0
        self.darksight = False
        for e in self.enemies:
            e.strloss = 0; e.stun = False
        gain = 2 if self.majoka >= 100 else 1
        before = self.bullets
        self.bullets = min(6, self.bullets + gain)
        self.add("bullet_waste", before + gain - self.bullets)
        if self.summoned and not self.unsafe:
            if self.guard_hp is not None and self.guard_hp < self.guard_max:
                if self.guard_hp <= 0:
                    self.add("guard_revive")
                h = min(self.safe_heal, self.guard_max - self.guard_hp)
                self.guard_hp += h; self.add("ribbon_heal", h)
        if "JailerRegeneration" in self.powers and self.guard_hp and self.guard_hp > 0:
            h = min(10 * self.powers["JailerRegeneration"], self.guard_max - self.guard_hp)
            self.guard_hp += h; self.add("jr_heal", h)
        if "ActingCool" in self.powers and self.guard_hp and self.guard_hp > 0:
            self.acc += 40 * self.powers["ActingCool"]; self.add("ac_acc", 40 * self.powers["ActingCool"])
            self.add("acc_applied", 40)
        self.energy = self.maxe + self.next_energy; self.next_energy = 0
        nd = 5 + self.next_draw; self.next_draw = 0
        self.draw_n(nd)
        self.add("bullets_start", self.bullets); self.add("turns")
        if self.guard_hp and self.guard_hp > 0:
            self.add("guard_alive_turns"); self.add("guard_hp_sum", self.guard_hp)
        self.add("majoka_sum", self.majoka)
        for _ in range(30):
            if not self.alive():
                break
            best = None; bs = 0.05
            for c in set(self.hand):
                s = self.score(c)
                if s > bs:
                    bs = s; best = c
            if best is None:
                break
            self.play(best)
        stuck = [c for c in self.hand if c in GUN_ATTACK and CARDS[c][2] > self.bullets]
        if stuck:
            self.add("gun_stuck_turns"); self.add("gun_stuck_cards", len(stuck))
        if self.acc > 0:
            self.add("acc_left_end_turn", self.acc)
        if "WardenAttack" in self.powers and self.guard_hp and self.guard_hp > 0:
            for e in self.alive():
                d = min(10 * self.powers["WardenAttack"], e.hp)
                e.hp -= 10 * self.powers["WardenAttack"]; self.add("wa_dmg", d); self.add("dmg", d)
        self.disc += self.hand; self.hand = []

    def enemy_turn(self):
        hits = self.incoming_hits()
        pl, gd, red = self.resolve_hits(hits, self.block, self.guard_hp, self.summoned, apply=True)
        for _ in range(red):
            self.gain_majoka(10, "intercept")
        self.add("redirects", red); self.add("guard_dmg", gd)
        self.unsafe = (pl > 0) or (gd > 0)
        self.hp -= pl; self.add("hp_lost", pl)
        for e in self.alive():
            if e.weak: e.weak -= 1
            if e.vuln: e.vuln -= 1

    def run(self):
        while self.alive() and self.turn < 30:
            self.player_turn()
            if not self.alive():
                break
            self.enemy_turn()
        self.d["len"] = self.turn
        self.d["dead"] = 1.0 if self.hp <= 0 else 0.0
        self.d["majoka_end"] = self.majoka
        if "m100_turn" in self.d:
            self.d["m100_n"] = 1.0
        if "summon_turn" in self.d:
            self.d["summon_n"] = 1.0
        if "steady_turn" in self.d:
            self.d["steady_n"] = 1.0
        return self.d


def run(deck, n, seed=1, level=1, probe=None):
    rng = random.Random(seed)
    kinds = list(ENC); w = [ENC[k][0] for k in kinds]
    by = {k: {"fights": 0} for k in kinds}
    for _ in range(n):
        k = rng.choices(kinds, w)[0]
        d = Combat(deck, k, rng, None, level, probe).run()
        s = by[k]; s["fights"] += 1
        for kk, v in d.items():
            s[kk] = s.get(kk, 0.0) + v
    return by
