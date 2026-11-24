"""二階堂希羅簡化戰鬥蒙地卡羅模型（v5）。規則依 Manosaba 原始碼：
- 希羅的鋼筆(PenOfHiro, Lv1~5)：戰鬥開始進入中段；希羅的強化攻擊每段 + 嫌疑/4，生命 <50% 再 +Lv*10%、<20% +Lv*20%。
- 架式：上段 攻擊x1.2 格擋x0.8；下段 攻擊x0.8 格擋x1.2；中段無；從其他架式切換且有 FluxPower 時 +1 能量。
- 嫌疑(SusPower)：玩家回合開始若 >10（艾瑪存活時 >20）加入 1 張 BadEnd（不可打出，回合結束在手 5 點 Unpowered 傷害）。
- 魔女化：攻擊倍率 1+3*s^1.8365/(s^1.8365+166.6^1.8365)；每 100 層 1 層殺人衝動（攻擊牌傷害 x 層數 打隨機友方＝艾瑪）；
  首次 >=100 時把死亡循環加入手牌。
- 櫻羽艾瑪(SakurabaEmaDog)：召喚＝存活時加最大生命(同時加當前)，死亡時以該量復活；IkiteyoHirochan 把希羅未格擋的
  非 Unpowered 傷害整筆轉給艾瑪（溢出不回流，假設）；艾瑪的攻擊不吃希羅的力量/架式/鋼筆。
- 每回合抽 5、3 能量；敵人固定血量／傷害曲線；貪婪出牌（分數/費用最高者）。
"""
from __future__ import annotations
import random
from collections import defaultdict

ENC = {  # 比例, 總血量, 敵數, 基礎傷害, 每回合成長, 段數
    "hallway": (0.72, (55, 80), (1, 3), 8.0, 1.0, 1.0),
    "elite":   (0.20, (140, 175), (1, 2), 11.0, 1.2, 1.5),
    "boss":    (0.08, (440, 520), (1, 1), 10.0, 1.1, 2.0),
}
DMG_SCALE = 0.6
IMP = {"hallway": 1.0, "elite": 2.0, "boss": 3.0}
PEN_LV = [(1, 0.3), (2, 0.3), (3, 0.25), (4, 0.15)]

COST = {"Strike": 1, "Defend": 1, "Trauma": 0, "Ema": 1, "Intimidate": 1, "StrikeChi": 1, "StrikeJin": 1,
        "StrikeTen": 1, "GoodSleep": 1, "WellSleep": 1, "EmaDogRush": 1, "BathroomTalk": 1, "EmaDogBite": 2,
        "EmaDogAttack": 1, "Perjury": 0, "Ronpa": 1, "SeveringSpin": 2, "Flux": 2, "Accomplice": 2,
        "Labours": 2, "LifeSentence": 2, "MusouKen": 3, "WitchesSabbath": 0, "DeathLoop": 3, "BadEnd": 99}
ATTACK = {"Strike", "StrikeChi", "StrikeJin", "StrikeTen", "EmaDogRush", "EmaDogBite", "Ronpa", "SeveringSpin",
          "MusouKen", "WitchesSabbath"}
POWER = {"EmaDogAttack", "Flux", "Labours", "LifeSentence", "DeathLoop"}
EXHAUST = {"WitchesSabbath"}

STARTER = ["Strike"] * 4 + ["Defend"] * 2 + ["Trauma", "Ema", "Ema", "Intimidate"]
ARCH = {
    "sus": STARTER + ["WellSleep", "WellSleep", "BathroomTalk", "Ronpa", "Ronpa", "Perjury", "LifeSentence",
                      "StrikeJin", "Defend"],
    "ema": STARTER + ["BathroomTalk", "BathroomTalk", "EmaDogRush", "EmaDogRush", "EmaDogBite", "EmaDogAttack",
                      "WellSleep", "StrikeJin", "Accomplice"],
    "stance": STARTER + ["StrikeTen", "StrikeTen", "StrikeJin", "StrikeChi", "StrikeChi", "SeveringSpin", "Flux",
                         "GoodSleep", "Defend"],
}


def majoka_mult(s):
    if s <= 0:
        return 1.0
    a = s ** 1.8365; k = 166.6 ** 1.8365
    return 1 + 3 * a / (a + k)


class Combat:
    def __init__(self, deck, kind, rng, st, probe=None, upg=False):
        self.rng = rng; self.kind = kind; self.st = st; self.probe = probe; self.upg = upg
        _, hpr, nr, self.bdmg, self.grow, self.hits = ENC[kind]
        n = rng.randint(*nr); tot = rng.uniform(*hpr)
        self.n0 = n
        self.en = [tot / n for _ in range(n)]; self.weak = [0] * n
        self.draw = list(deck); rng.shuffle(self.draw)
        if upg and probe in ("Labours",) and probe in self.draw:   # 升級固有
            self.draw.remove(probe); self.draw.append(probe)
        self.hand = []; self.disc = []
        r = rng.random(); acc = 0
        for lv, p in PEN_LV:
            acc += p
            if r <= acc:
                self.pen = lv; break
        self.maxhp = 60.0; self.hp = 60.0; self.block = 0.0; self.stance = "mid"
        self.sus = 0; self.majoka = 0; self.str = 0; self.ema = 0.0; self.ema_max = 0.0; self.ema_ever = False
        self.dogatk = False; self.flux = False; self.labours = False; self.life = 0; self.deathloop = False
        self.musou = None; self.mi_given = False; self.turn = 0; self.energy = 0
        self.deathloop_card_given = False

    # ---------- 工具 ----------
    def alive(self):
        return [i for i, h in enumerate(self.en) if h > 0]

    def tgt(self):
        al = self.alive()
        return min(al, key=lambda i: self.en[i]) if al else None

    def draw_n(self, n):
        for _ in range(n):
            if not self.draw:
                if not self.disc:
                    return
                self.draw = self.disc; self.disc = []; self.rng.shuffle(self.draw)
            self.hand.append(self.draw.pop())

    def hit_mult(self):
        m = majoka_mult(self.majoka)
        if self.stance == "high": m *= 1.2
        elif self.stance == "low": m *= 0.8
        return m

    def hp_pct(self):
        f = self.hp / self.maxhp
        return self.pen * 20 if f < 0.2 else (self.pen * 10 if f < 0.5 else 0)

    def hiro_hit(self, base):
        """單段希羅強化攻擊傷害"""
        b = base + self.str
        add = self.sus / 4 + b * self.hp_pct() / 100
        self.st["pen_bonus"] += (self.sus / 4) * self.hit_mult()
        self.st["pen_hits"] += 1
        return max(0.0, (b + add) * self.hit_mult())

    def deal(self, i, d):
        if i is None or self.en[i] <= 0:
            return 0.0
        x = min(self.en[i], d); self.en[i] -= d; return x

    def attack_after(self, dealt):
        mi = self.majoka // 100
        if mi >= 1 and dealt > 0 and self.ema > 0:
            dmg = dealt * mi
            self.st["mi_ema_dmg"] += min(self.ema, dmg)
            self.ema -= dmg
            if self.ema <= 0:
                self.ema = 0; self.st["ema_deaths"] += 1
        if self.labours:
            pass

    def gain_block(self, b):
        if self.stance == "high": b *= 0.8
        elif self.stance == "low": b *= 1.2
        self.block += b

    def add_majoka(self, n):
        before = self.majoka; self.majoka += n
        if self.majoka >= 100 and before < 100:
            self.st["majoka100"] += 1
            if self.st.get("_m100") is None:
                self.st["_m100"] = 1; self.st["majoka100_turn"] += self.turn
            if not self.deathloop_card_given:
                self.deathloop_card_given = True; self.hand.append("DeathLoop")

    def set_stance(self, s):
        if self.stance != s:
            if self.flux:
                self.energy += 1; self.st["flux_energy"] += 1
            self.st["stance_switch"] += 1
            self.stance = s

    def summon(self, n):
        if self.ema > 0:
            self.ema += n; self.ema_max += n
        else:
            if self.ema_ever:
                self.st["ema_revive"] += 1
            self.ema = n; self.ema_max = n; self.ema_ever = True
        self.st["summon"] += n

    def add_sus(self, n):
        self.sus = max(0, self.sus + n)

    def sus_cap(self):
        return 20 if self.ema > 0 else 10

    def incoming(self):
        t = self.turn
        tot = 0.0
        for i in self.alive():
            d = (self.bdmg + self.grow * t) * self.hits * (1.0 if self.n0 == 1 else 0.65) * DMG_SCALE
            if self.weak[i] > 0:
                d *= 0.75
            tot += d
        return tot

    # ---------- 評分 ----------
    def dmg_val(self, d):
        al = self.alive()
        if not al:
            return 0
        t = self.tgt(); cap = self.en[t] + 4
        return min(d, cap) / 6

    def blk_val(self, b):
        need = max(0.0, self.incoming() * (0.5 if self.musou else 1) - self.block)
        if self.stance == "high": b *= 0.8
        elif self.stance == "low": b *= 1.2
        v = min(b, need) / 5 + max(0, b - need) * 0.02
        if self.ema > 0:
            v *= 0.7
        return v

    def sus_val(self, n):
        cap = self.sus_cap()
        if self.sus + n > cap:
            return -0.6
        return 0.2 * n

    def score(self, c):
        u = self.upg and c == self.probe
        if c == "BadEnd":
            return None
        if COST[c] > self.energy and not (c == "Flux" and u and self.energy >= 1) \
                and not (c in ("EmaDogAttack",) and u) and not (c == "DeathLoop" and u and self.energy >= 2):
            return None
        if c == "Strike": return self.dmg_val(self.hiro_peek(9 if u else 6))
        if c == "Defend": return self.blk_val(8 if u else 5)
        if c == "Trauma": return 0.3 if self.majoka < 90 or True else 0.3
        if c == "Ema": return 0.17 * (7 if u else 5) + 0.15
        if c == "Intimidate": return 0.35 + self.sus_val(2)
        if c == "StrikeChi": return self.dmg_val(self.hiro_peek(4, "low")) + self.blk_val(8 if u else 4) + 0.4 + (0.3 if self.flux and self.stance != "low" else 0)
        if c == "StrikeJin": return self.dmg_val(self.hiro_peek(12 if u else 8, "mid")) + 0.8 + (0.3 if self.flux and self.stance != "mid" else 0)
        if c == "StrikeTen": return self.dmg_val(self.hiro_peek(18 if u else 12, "high")) + 0.4 + 0.2 + (0.3 if self.flux and self.stance != "high" else 0)
        if c == "GoodSleep": return self.blk_val(10 if u else 7) + 0.1
        if c == "WellSleep": return self.blk_val(8 if u else 5) + self.sus_val(2)
        if c == "EmaDogRush":
            n = 6 if u else 4; return self.dmg_val(self.ema + n) + 0.15 * n
        if c == "BathroomTalk": return 0.17 * (12 if u else 8) + self.sus_val(2)
        if c == "EmaDogBite":
            if self.ema <= 0: return None
            return self.dmg_val(self.ema * (3 if u else 2)) * 1.0
        if c == "EmaDogAttack":
            if self.ema <= 0 or self.dogatk: return None
            return 3.0
        if c == "Perjury":
            if self.sus <= 0: return None
            return 0.5 * min(self.sus, 3 if u else 2)
        if c == "Ronpa":
            h = (self.sus + 1) // 2
            return self.dmg_val(self.hiro_peek((14 if u else 10) + (7 if u else 6) * h)) - 0.15 * h
        if c == "SeveringSpin":
            d = self.hiro_peek(3) * (5 if u else 4) * len(self.alive())
            return d / 6 + self.blk_val(7 if u else 5)
        if c == "Flux": return None if self.flux else 2.5
        if c == "Accomplice":
            if self.sus <= 0: return None
            return 0.17 * (10 if u else 7) * self.sus * 0.6
        if c == "Labours": return None if self.labours else 2.0
        if c == "LifeSentence": return 2.5 if self.life == 0 else 1.5
        if c == "MusouKen":
            if self.stance != "high" or not self.alive(): return None
            return 4.5 if self.energy <= 3 else 2.0
        if c == "WitchesSabbath":
            if self.majoka < 100: return None
            return 10.0
        if c == "DeathLoop":
            return None if self.deathloop else 1.5
        return 0

    def hiro_peek(self, base, stance=None):
        b = base + self.str
        m = majoka_mult(self.majoka)
        s = stance if stance in (None,) else None
        st = self.stance
        if st == "high": m *= 1.2
        elif st == "low": m *= 0.8
        return (b + self.sus / 4 + b * self.hp_pct() / 100) * m

    def cost_of(self, c):
        u = self.upg and c == self.probe
        k = COST[c]
        if u and c in ("Flux", "EmaDogAttack", "DeathLoop"):
            k -= 1
        return k

    # ---------- 出牌 ----------
    def play(self, c):
        u = self.upg and c == self.probe
        self.hand.remove(c); self.energy -= self.cost_of(c)
        if c == self.probe:
            self.st["probe_plays"] += 1; self.st["probe_turn"] += self.turn
            self.st["probe_sus"] += self.sus; self.st["probe_ema"] += self.ema; self.st["probe_majoka"] += self.majoka
        t = self.tgt(); dealt = 0.0
        if c == "Strike": dealt = self.deal(t, self.hiro_hit(9 if u else 6))
        elif c == "Defend": self.gain_block(8 if u else 5)
        elif c == "Trauma": self.add_majoka(15 if u else 10)
        elif c == "Ema": self.summon(7 if u else 5); self.add_majoka(10)
        elif c == "Intimidate":
            if t is not None: self.weak[t] += 2 if u else 1
            self.add_sus(2)
        elif c == "StrikeChi":
            dealt = self.deal(t, self.hiro_hit(4)); self.gain_block(8 if u else 4); self.draw_n(1); self.set_stance("low")
        elif c == "StrikeJin":
            dealt = self.deal(t, self.hiro_hit(12 if u else 8)); self.draw_n(2); self.set_stance("mid")
        elif c == "StrikeTen":
            dealt = self.deal(t, self.hiro_hit(18 if u else 12)); self.draw_n(1); self.set_stance("high")
        elif c == "GoodSleep": self.add_majoka(7 if u else 5); self.gain_block(10 if u else 7)
        elif c == "WellSleep": self.add_sus(2); self.gain_block(8 if u else 5)
        elif c == "EmaDogRush":
            self.summon(6 if u else 4)
            d = self.deal(t, self.ema); self.st["ema_dmg"] += d
            if c == self.probe: self.st["probe_dmg"] += d
        elif c == "BathroomTalk": self.summon(12 if u else 8); self.add_sus(2)
        elif c == "EmaDogBite":
            for _ in range(3 if u else 2):
                t = self.tgt(); d = self.deal(t, self.ema); self.st["ema_dmg"] += d
                if c == self.probe: self.st["probe_dmg"] += d
        elif c == "EmaDogAttack": self.dogatk = True
        elif c == "Perjury":
            k = min(self.sus, 3 if u else 2); self.add_sus(-k); self.str += k
            if c == self.probe: self.st["probe_res"] += k
        elif c == "Ronpa":
            h = (self.sus + 1) // 2
            d = self.hiro_hit((14 if u else 10) + (7 if u else 6) * h); dealt = self.deal(t, d)
            if c == self.probe: self.st["probe_dmg"] += dealt; self.st["probe_res"] += h
            self.add_sus(-h)
        elif c == "SeveringSpin":
            for _ in range(5 if u else 4):
                for i in self.alive():
                    dealt += self.deal(i, self.hiro_hit(3))
            self.gain_block(7 if u else 5)
            self.set_stance(self.rng.choice([s for s in ("high", "mid", "low") if s != self.stance]))
        elif c == "Flux": self.flux = True
        elif c == "Accomplice":
            k = self.sus; self.add_sus(-k)
            for _ in range(k): self.summon(10 if u else 7)
            if c == self.probe: self.st["probe_res"] += k
        elif c == "Labours": self.labours = True
        elif c == "LifeSentence": self.life += 3 if u else 2
        elif c == "MusouKen":
            self.musou = [t, 75.0 if u else 50.0]; self.st["musou_plays"] += 1
        elif c == "WitchesSabbath":
            dmg = ((70 if u else 50) + self.sus * 5 + self.majoka // 25) * (1 + 0.01 * self.majoka)
            for i in self.alive(): dealt += self.deal(i, dmg)
            if c == self.probe: self.st["probe_dmg"] += dealt
            self.majoka = 0; self.sus = 0
        elif c == "DeathLoop":
            self.deathloop = True; self.add_majoka(50)
        if c in ATTACK and c != "WitchesSabbath":
            if c in ("Strike", "StrikeChi", "StrikeJin", "StrikeTen", "Ronpa", "SeveringSpin") and c == self.probe:
                if c != "Ronpa": self.st["probe_dmg"] += dealt
            self.attack_after(dealt)
            if self.labours:
                g = 10 * self.cost_of(c); self.add_majoka(g); self.st["labours_majoka"] += g
        self.st["dmg_dealt"] += dealt
        if c in POWER or c in EXHAUST:
            return
        self.disc.append(c)

    def player_turn(self):
        self.turn += 1; self.energy = 3; self.block = 0.0
        if self.musou:
            t, amt = self.musou
            if t is not None and self.en[t] > 0:
                d = self.deal(t, amt * self.hit_mult() + self.sus / 4)
                self.st["musou_dmg"] += d
            self.musou = None
        if self.sus > self.sus_cap():
            self.hand.append("BadEnd"); self.st["badend"] += 1
        self.draw_n(5)
        while self.alive():
            best = None; bs = 0.0
            for c in set(self.hand):
                s = self.score(c)
                if s is None or s <= 0:
                    continue
                k = self.cost_of(c)
                r = s / k if k > 0 else s * 5
                if c == "MusouKen":
                    others = [x for x in self.hand if x != "MusouKen" and self.cost_of(x) == 0 and (self.score(x) or 0) > 0]
                    if others: continue
                if r > bs:
                    bs = r; best = c
            if best is None:
                break
            self.play(best)
            if best == "MusouKen":
                break
        # 回合結束
        if self.dogatk and self.ema > 0 and self.alive():
            i = self.rng.choice(self.alive()); d = self.deal(i, self.ema); self.st["dogatk_dmg"] += d
            if self.probe == "EmaDogAttack": self.st["probe_dmg"] += d
        for c in list(self.hand):
            if c == "BadEnd":
                self.hp -= 5; self.st["badend_dmg"] += 5
        self.disc.extend(self.hand); self.hand = []
        if self.life and self.sus > 0:
            self.block += self.sus * self.life; self.st["life_block"] += self.sus * self.life
        if self.sus > 0:
            self.st["sus_turn_sum"] += self.sus; self.st["sus_turns"] += 1

    def enemy_turn(self):
        inc = self.incoming()
        if self.musou:
            inc *= 0.5
        b = min(self.block, inc); un = inc - b
        self.st["dmg_in"] += inc
        if un > 0 and self.ema > 0:
            a = min(self.ema, un); self.st["ema_absorb"] += a; self.ema -= un
            if self.ema <= 0:
                self.ema = 0; self.st["ema_deaths"] += 1
            un = 0
        self.hp -= un; self.st["hp_lost"] += un
        if self.musou and un > 0:
            self.musou[1] += un
        for i in range(len(self.weak)):
            if self.weak[i] > 0: self.weak[i] -= 1

    def run(self):
        while self.turn < 30:
            self.player_turn()
            if self.hp <= 0 and not self.check_death():
                break
            if not self.alive(): break
            self.enemy_turn()
            if self.hp <= 0 and not self.check_death():
                break
        self.st["turns"] += self.turn
        self.st["deaths"] += self.hp <= 0
        self.st["majoka_end"] += self.majoka
        self.st["ema_end"] += self.ema
        self.st["ema_alive_end"] += self.ema > 0
        self.st["str_end"] += self.str

    def check_death(self):
        if self.deathloop:
            self.deathloop = False
            heal = max(1.0, self.maxhp * min(100, self.majoka) / 100)
            self.hp = heal; self.st["deathloop_save"] += 1; self.st["deathloop_heal"] += heal
            return True
        return False


def run(deck, n, seed, probe=None, upg=False):
    rng = random.Random(seed)
    kinds = list(ENC); w = [ENC[k][0] for k in kinds]
    by = {k: defaultdict(float) for k in kinds}
    for _ in range(n):
        k = rng.choices(kinds, w)[0]
        st = by[k]; st["fights"] += 1
        st.pop("_m100", None)
        Combat(deck, k, rng, st, probe, upg).run()
        st.pop("_m100", None)
    return by
