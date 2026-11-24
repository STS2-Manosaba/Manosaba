"""共用卡池（Common）簡化戰鬥蒙地卡羅模型（v5）。規則依 Manosaba/Characters/Common 原始碼：
- 處刑卡＝帶 ManosabaKeywords.Execution 的卡（ExecutionCardHelper）；共用池 13 張（含典獄長命令本身）。
- 典獄長命令 WardenCommandPower(Counter)：自己打出處刑卡時 playCount += Amount（每層多打 1 次）。升級＝固有。
- 魔女語精通 WitchLanguageMasteryPower：擁有者受到傷害（TotalDamage＝被格擋+未格擋）後，
  對所有存活敵人造成 TotalDamage×2×層數 的 nonCardHpLoss（無視敵方格擋）。升級＝0 費。
- 魔女島監獄 PrisonPower：每個己方回合開始 +10/15 魔女化。魔女化：攻擊傷害倍率
  1+3·s^1.8365/(s^1.8365+166.6^1.8365)；≥100 時給殺人衝動（單人無隊友→無害）與角色魔法卡（不計值）。
- 高牆 WallPower：受到 Move 攻擊的未格擋傷害 → 等量活力（下一張攻擊加傷）。升級降 1 費。
- 鑄矛 ForgeSpear：4 張零件（不可打出、保留）隨機洗入抽牌堆；零件全在手時合成 0 費 50 傷簡易長矛。
  升級＝不消耗（可再打出、再洗入一組零件）。
- 斷頭台 Guillotine：14 傷後若目標生命 <10%（升級 18 傷/15%）即 DoomKill。
- 死亡鐘擺 DeathPendulum：3(+2) 傷 × 手中技能數段，全體。
- 魔女島遠征（Beta）：敵人 Move 攻擊對自己 ×3，QTE 成功則 ×0；成功率 p 為參數。
- 宿主：雪莉（起始 1 力量、回合開始清空魔女化）、可可、艾瑪 的起始牌組簡化，加 6 張通用中期卡。
- 每回合抽 5、能量 3、手牌上限 10；貪婪出牌（即時價值/費用）。敵人固定血量/傷害曲線。
"""
from __future__ import annotations
import random, math, json, sys
from collections import defaultdict

ENC = {  # 比例, 總血量, 敵人數, 每回合總傷害基礎, 每回合成長, 敵力量成長/回合, 重要性
    "hallway": (0.72, (50, 72), (1, 3), 7.0, 0.8, 0.0, 1.0),
    "elite":   (0.20, (105, 130), (1, 2), 10.0, 1.0, 0.2, 2.0),
    "boss":    (0.08, (220, 260), (1, 1), 12.0, 1.0, 0.3, 3.0),
}

# name: (cost, type)
CARDS = {
    "Strike": (1, "A"), "Defend": (1, "S"), "Punch": (1, "A"), "MurderCase": (1, "S"),
    "Trauma": (1, "S"), "Live": (1, "P"), "Oshi": (1, "S"), "Search": (0, "S"), "Present": (1, "S"), "Pen": (1, "A"),
    "GAtk": (1, "A"), "GBig": (2, "A"), "GBlk": (1, "S"), "GDraw": (1, "S"), "GStr": (1, "P"),
    # 處刑卡
    "Hotpot": (1, "A"), "Bullseye": (1, "A"), "Boulders": (2, "A"), "PiercingDeathSpear": (1, "A"),
    "IronMaiden": (2, "A"), "Guillotine": (2, "A"), "Immolation": (1, "A"), "DeathPendulum": (1, "A"),
    "Suicide": (1, "S"), "Gallows": (3, "A"), "ConcentratedFire": (1, "A"), "Dissolve": (1, "S"),
    "WardenCommand": (2, "P"),
    # 其他探針
    "WitchLanguageMastery": (1, "P"), "Prison": (1, "P"), "Wall": (2, "P"), "ForgeSpear": (1, "S"),
    "WitchIslandExpedition": (0, "P"), "SimpleSpear": (0, "A"), "Part": (99, "X"),
}
EXEC = {"Hotpot", "Bullseye", "Boulders", "PiercingDeathSpear", "IronMaiden", "Guillotine", "Immolation",
        "DeathPendulum", "Suicide", "Gallows", "ConcentratedFire", "Dissolve", "WardenCommand"}
EXEC_POOL = ["Hotpot", "Bullseye", "Boulders", "PiercingDeathSpear", "IronMaiden", "Guillotine", "Immolation",
             "DeathPendulum", "Suicide", "Gallows", "ConcentratedFire", "Dissolve"]
# 原 v2 單次打出價值（重複打出時的期望增益用）
EXEC_V = {"Hotpot": 1.63, "Bullseye": 1.87, "Boulders": 3.33, "PiercingDeathSpear": 2.33, "IronMaiden": 2.8,
          "Guillotine": 2.9, "Immolation": 3.6, "DeathPendulum": 1.4, "Suicide": 1.1, "Gallows": 4.17,
          "ConcentratedFire": 1.75, "Dissolve": 2.3}
EXHAUST = {"ForgeSpear", "SimpleSpear"}
HOSTS = {
    "sherry": ["Strike"] * 4 + ["Defend"] * 3 + ["Punch", "Punch", "MurderCase"],
    "coco": ["Strike"] * 3 + ["Defend"] * 4 + ["Trauma", "Live", "Oshi"],
    "ema": ["Strike"] * 3 + ["Defend"] * 3 + ["Trauma", "Search", "Search", "Present", "Pen"],
}
MID = ["GAtk", "GAtk", "GBig", "GBlk", "GDraw", "GStr"]


def majoka_mult(s):
    if s <= 0:
        return 1.0
    a = s ** 1.8365; k = 166.6 ** 1.8365
    return 1 + 3 * a / (a + k)


class E:
    __slots__ = ("hp", "mx", "vuln", "weak", "burn", "tstr")

    def __init__(self, hp):
        self.hp = hp; self.mx = hp; self.vuln = 0; self.weak = 0; self.burn = 0; self.tstr = 0


class Combat:
    def __init__(self, host, deck, kind, rng, st, opt):
        self.rng = rng; self.st = st; self.opt = opt; self.host = host
        _, hpr, nr, self.base, self.grow, self.sgrow, _ = ENC[kind]
        n = rng.randint(*nr); tot = rng.uniform(*hpr)
        self.n0 = n
        self.en = [E(tot / n) for _ in range(n)]
        self.draw = list(deck); rng.shuffle(self.draw)
        for c in list(self.draw):  # 固有
            if c.endswith("+") and c[:-1] in ("WardenCommand", "WitchIslandExpedition"):
                self.draw.remove(c); self.draw.append(c)
        self.hand = []; self.disc = []
        self.str = 1.0 if host == "sherry" else 0.0
        self.block = 0.0; self.vigor = 0.0; self.majoka = 0.0
        self.warden = 0; self.wlm = 0; self.prison = 0; self.wall = False; self.wie = False
        self.turn = 0; self.energy = 0; self.estr = 0.0; self.live = 0
        self.parts_drawn_pre = 0; self._attack_ctx = False

    def alive(self):
        return [e for e in self.en if e.hp > 0]

    # ---------- 傷害 ----------
    def hit(self, e, dmg, src=None, powered=True):
        if e.hp <= 0:
            return 0.0
        if powered:
            base = dmg + self.str + self.vigor_take()
            m = majoka_mult(self.majoka)
            d = max(0.0, base) * m
            if e.vuln > 0:
                d *= 1.5
            d = math.floor(d)
            nomaj = max(0.0, base) * (1.5 if e.vuln > 0 else 1)
            real = min(e.hp, d)
            if m > 1:
                self.st["majoka_extra"] += max(0.0, real - min(e.hp, nomaj))
        else:
            real = min(e.hp, dmg)
        e.hp -= real
        self.st["dmg"] += real
        if src:
            self.st["dmg_" + src] += real
        return real

    def vigor_take(self):
        v = self.vigor
        if v > 0 and self._attack_ctx:
            self.st["vigor_used"] += v
            self.vigor = 0
            self._attack_ctx = False
            return v
        return 0.0

    def attack(self, e, dmg, hits=1, src=None):
        tot = 0.0
        for _ in range(hits):
            if e.hp <= 0:
                break
            self._attack_ctx = True if _ == 0 else False
            tot += self.hit(e, dmg, src)
        self._attack_ctx = False
        return tot

    def attack_all(self, dmg, hits=1, src=None):
        first = True
        for _ in range(hits):
            for e in self.alive():
                self._attack_ctx = first; first = False
                self.hit(e, dmg, src)
        self._attack_ctx = False

    def target(self):
        al = self.alive()
        return min(al, key=lambda e: e.hp) if al else None

    # ---------- 抽牌 ----------
    def draw_n(self, n):
        for _ in range(n):
            if len(self.hand) >= 10:
                return
            if not self.draw:
                if not self.disc:
                    return
                self.draw = self.disc; self.disc = []; self.rng.shuffle(self.draw)
            c = self.draw.pop()
            self.hand.append(c)
            if c == "Part":
                self.parts_drawn_pre += 1
                self.st["part_draws"] += 1
        self.try_craft()

    def try_craft(self):
        if self.hand.count("Part") >= 4:
            for _ in range(4):
                self.hand.remove("Part")
            self.hand.append("SimpleSpear")
            self.st["crafts"] += 1
            if self.st.get("_craft_turn") is None:
                self.st["_craft_turn"] = self.turn

    # ---------- 打牌 ----------
    def incoming(self):
        t = self.turn
        tot = 0.0
        for e in self.alive():
            per = (self.base + self.grow * (t - 1)) / self.n0 + self.estr * 2 - e.tstr * 2
            if e.weak > 0:
                per *= 0.75
            tot += max(0.0, per)
        return tot

    def score(self, c):
        nm = c.rstrip("+"); up = c.endswith("+")
        cost, ty = CARDS[nm]
        if nm == "Part":
            return None
        if nm == "ForgeSpear" and up:
            cost = 1
        if nm in ("WitchLanguageMastery", "Wall") and up:
            cost -= 1
        if cost > self.energy:
            return None
        need = max(0.0, self.incoming() - self.block)
        tg = self.target()
        if ty == "P":
            return 99.0
        if nm == "SimpleSpear":
            return 98.0
        if nm in ("Defend",):
            return min(5, need) / 5 * 1.1
        if nm == "GBlk":
            return min(8, need) / 5 * 1.1
        if nm == "Trauma":
            return min(6, need) / 5 * 1.1
        if nm in ("GDraw", "Oshi"):
            return 1.0
        if nm == "Search":
            return 0.6
        if nm == "Present":
            return 0.5 + min(3, need) / 5
        if nm == "MurderCase":
            return 0.5 + min(4, need) / 5
        if nm == "Suicide":
            return 0.3
        if nm == "Dissolve":
            return min(need, 8 * 0.5 * len(self.alive())) / 5 + 0.2
        if nm == "ForgeSpear":
            return 2.0
        if tg is None:
            return None
        dmg = {"Strike": 6, "Punch": 8, "Pen": 9, "GAtk": 9, "GBig": 18, "Hotpot": 7 * len(self.alive()),
               "Bullseye": 8, "Boulders": 20, "PiercingDeathSpear": 13, "IronMaiden": 12, "Guillotine": 14,
               "Immolation": 10 * len(self.alive()), "Gallows": 25, "ConcentratedFire": 10,
               "DeathPendulum": 3 * len(self.alive()) * sum(1 for h in self.hand if CARDS[h.rstrip('+')][1] == "S")}[nm]
        return min(dmg, sum(e.hp for e in self.alive())) / 6 + 0.05

    def cost_of(self, c):
        nm = c.rstrip("+"); up = c.endswith("+")
        cost = CARDS[nm][0]
        if nm in ("WitchLanguageMastery", "Wall") and up:
            cost -= 1
        return cost

    def play(self, c):
        nm = c.rstrip("+"); up = c.endswith("+")
        self.energy -= self.cost_of(c)
        self.hand.remove(c)
        times = 1
        if nm in EXEC and self.warden > 0 and nm != "WardenCommand":
            times += self.warden
            self.st["warden_extra"] += self.warden
            self.st["warden_extra_v"] += self.warden * EXEC_V[nm] * (1.4 if up else 1.0)
        for _ in range(times):
            self.effect(nm, up)
            if not self.alive():
                break
        if nm in EXEC:
            self.st["exec_plays"] += 1
            if self.st["probe_live"]:
                self.st["exec_after_probe"] += 1
        if CARDS[nm][1] == "P" or (nm in EXHAUST and not (nm == "ForgeSpear" and up)):
            pass
        else:
            self.disc.append(c)

    def effect(self, nm, up):
        tg = self.target(); st = self.st
        if nm in ("Strike",): self.attack(tg, 6)
        elif nm == "Punch": self.attack(tg, 8)
        elif nm in ("Pen", "GAtk"): self.attack(tg, 9)
        elif nm == "GBig": self.attack(tg, 18)
        elif nm == "Defend": self.block += 5
        elif nm == "GBlk": self.block += 8
        elif nm == "Trauma": self.block += 6
        elif nm in ("GDraw", "Oshi"): self.draw_n(2)
        elif nm == "Search": self.draw_n(1)
        elif nm == "Present": self.block += 3; self.draw_n(1)
        elif nm == "MurderCase": self.block += 4; self.draw_n(1)
        elif nm == "Live": self.str += 1
        elif nm == "GStr": self.str += 2
        elif nm == "Hotpot": self.attack_all(11 if up else 7)
        elif nm == "Bullseye":
            self.attack(tg, 10 if up else 7); tg.vuln += 3 if up else 2
        elif nm == "Boulders": self.attack(tg, 30 if up else 20)
        elif nm == "PiercingDeathSpear": self.attack(tg, 17 if up else 11); self.draw_n(1)
        elif nm == "IronMaiden":
            self.attack(tg, 13 if up else 9); tg.weak += 2; tg.vuln += 2
        elif nm == "Guillotine":
            al = self.alive(); d = 18 if up else 14; th = 0.15 if up else 0.10
            cand = [e for e in al if 0 < e.hp - d - self.str < e.mx * th]
            t = cand[0] if cand else tg
            self.attack(t, d)
            st["guil_plays"] += 1
            if t.hp > 0 and t.hp < t.mx * th:
                st["guil_kills"] += 1; st["guil_hp"] += t.hp; t.hp = 0
        elif nm == "Immolation":
            self.attack_all(12 if up else 8)
            for e in self.alive(): e.burn += 5
        elif nm == "DeathPendulum":
            k = sum(1 for h in self.hand if CARDS[h.rstrip('+')][1] == "S")
            st["pend_plays"] += 1; st["pend_skills"] += k
            if k: self.attack_all(5 if up else 3, hits=k)
        elif nm == "Suicide":
            self.take(3, move=False)
            self.majoka += 30 if up else 20
        elif nm == "Gallows": self.attack(tg, 25)
        elif nm == "ConcentratedFire": self.attack(tg, 2, hits=7 if up else 5)
        elif nm == "Dissolve":
            for e in self.alive(): e.tstr += 8
        elif nm == "WardenCommand":
            self.warden += 1; st["probe_live"] = 1; st["probe_turn"] += self.turn
        elif nm == "WitchLanguageMastery":
            self.wlm += 1; st["probe_live"] = 1; st["probe_turn"] += self.turn
        elif nm == "Prison":
            self.prison += 15 if up else 10; st["probe_live"] = 1; st["probe_turn"] += self.turn
        elif nm == "Wall":
            self.wall = True; st["probe_live"] = 1; st["probe_turn"] += self.turn
        elif nm == "WitchIslandExpedition":
            self.wie = True; st["probe_live"] = 1; st["probe_turn"] += self.turn
        elif nm == "ForgeSpear":
            st["forge_plays"] += 1
            if st.get("_forge_turn") is None:
                st["_forge_turn"] = self.turn
            for _ in range(4):
                self.draw.insert(self.rng.randint(0, len(self.draw)), "Part")
        elif nm == "SimpleSpear":
            st["spear_plays"] += 1
            self.attack(tg, 50, src="spear")

    # ---------- 受傷 ----------
    def take(self, dmg, move=True):
        if move and self.wie and self.opt.get("wie_on", True):
            if self.rng.random() < self.opt.get("parry", 0.7):
                self.st["wie_parried"] += dmg
                dmg = 0.0
            else:
                self.st["wie_extra"] += dmg * 2
                dmg *= 3
        blocked = min(self.block, dmg) if move else 0.0
        self.block -= blocked
        unb = dmg - blocked
        self.st["hp_loss"] += unb
        self.st["dmg_taken_total"] += dmg
        if self.wall and move and unb > 0:
            self.vigor += unb; self.st["vigor_gain"] += unb
        if self.wlm and dmg > 0:
            loss = dmg * 2 * self.wlm
            for e in self.alive():
                r = min(e.hp, loss); e.hp -= r; self.st["wlm_dmg"] += r; self.st["dmg"] += r

    def run(self):
        while self.alive() and self.turn < 25:
            self.turn += 1
            if self.host == "sherry":
                self.majoka = 0.0
            if self.prison:
                self.majoka += self.prison
                if self.majoka >= 100 and not self.st.get("_m100"):
                    self.st["_m100"] = 1; self.st["m100"] += 1
            self.block = 0.0; self.energy = 3
            self.draw_n(5)
            while True:
                best = None; bs = 0.0
                for c in set(self.hand):
                    s = self.score(c)
                    if s is None:
                        continue
                    s = s / max(self.cost_of(c), 0.5)
                    if s > bs:
                        bs = s; best = c
                if best is None:
                    break
                self.play(best)
                if not self.alive():
                    break
            if self.majoka > 0:
                self.st["majoka_turn_sum"] += self.majoka; self.st["majoka_turns"] += 1
            if not self.alive():
                break
            # 棄牌（零件保留）
            keep = [c for c in self.hand if c in ("Part", "SimpleSpear")]
            self.disc += [c for c in self.hand if c not in ("Part", "SimpleSpear")]
            self.hand = keep
            # 敵方回合
            for e in self.alive():
                if e.burn > 0:
                    r = min(e.hp, e.burn); e.hp -= r; self.st["dmg"] += r; e.burn -= 1
            n_al = len(self.alive())
            for e in self.alive():
                per = (self.base + self.grow * (self.turn - 1)) / self.n0 + self.estr * 2 - e.tstr * 2
                if e.weak > 0:
                    per *= 0.75
                per = max(0.0, per)
                for _ in range(2):
                    d = per / 2 * (1 if True else 0)
                    if d > 0:
                        self.take(d)
                e.tstr = 0
                e.weak = max(0, e.weak - 1); e.vuln = max(0, e.vuln - 1)
            self.estr += self.sgrow
        self.st["turns"] += self.turn


def build_deck(host, rng, k_exec, probe):
    deck = HOSTS[host] + MID + rng.sample(EXEC_POOL, k_exec)
    if probe:
        deck = deck + [probe]
    return deck


def run(host, probe, k_exec=2, N=6000, seed=1, opt=None, extra=None):
    opt = opt or {}
    rng = random.Random(seed)
    agg = defaultdict(float); wsum = 0.0
    kinds = list(ENC); ws = [ENC[k][0] for k in kinds]
    by = {k: defaultdict(float) for k in kinds}; byn = defaultdict(float)
    for _ in range(N):
        kind = rng.choices(kinds, ws)[0]; imp = ENC[kind][6]
        deck = build_deck(host, rng, k_exec, probe)
        if extra:
            deck += extra
        st = defaultdict(float)
        Combat(host, deck, kind, rng, st, opt).run()
        st["craft_turn"] = st.get("_craft_turn") or 0
        st["any_craft"] = 1.0 if st.get("_craft_turn") else 0.0
        st["forge_turn"] = st.get("_forge_turn") or 0
        for k in [k for k in st if k.startswith("_")]:
            del st[k]
        for k, v in st.items():
            agg[k] += v * imp; by[kind][k] += v
        wsum += imp; byn[kind] += 1
    res = {k: v / wsum for k, v in agg.items()}
    res["turns_by"] = {k: round(by[k]["turns"] / max(byn[k], 1), 2) for k in kinds}
    return res


if __name__ == "__main__":
    out = {}
    HOSTL = list(HOSTS)
    for h in HOSTL:
        out[h] = {}
        base = run(h, None)
        out[h]["base"] = base
        for p in ["WitchLanguageMastery", "WitchLanguageMastery+", "Prison", "Prison+", "Wall", "ForgeSpear",
                  "ForgeSpear+", "Guillotine", "Guillotine+", "DeathPendulum", "Suicide", "WitchIslandExpedition",
                  "WitchIslandExpedition+"]:
            out[h][p] = run(h, p)
        for k in (1, 2, 3, 4):
            out[h][f"Warden_k{k}"] = run(h, "WardenCommand", k_exec=k)
            out[h][f"Warden+_k{k}"] = run(h, "WardenCommand+", k_exec=k)
        for p in (0.5, 0.7, 0.9):
            out[h][f"WIE_p{p}"] = run(h, "WitchIslandExpedition", opt={"parry": p})
            out[h][f"WIE_off_p{p}"] = run(h, "WitchIslandExpedition", opt={"parry": p, "wie_on": False})
        print(h, "done", file=sys.stderr)
    json.dump(out, open(sys.argv[1] if len(sys.argv) > 1 else "results.json", "w", encoding="utf-8"),
              ensure_ascii=False, indent=1, default=float)
