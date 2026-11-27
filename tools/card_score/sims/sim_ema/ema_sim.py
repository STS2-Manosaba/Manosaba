"""艾瑪（SakurabaEma）簡化戰鬥蒙地卡羅模擬。

規則依原始碼（Manosaba/Characters/SakurabaEma）：
- 腿環 LegRing：等級由經驗（一般1/菁英2/首領3，累積 0/2/7/15/30 → Lv1..5）。
  搜查時間需打出 max(2, 10-2*Lv) 張牌（8/6/4/2）→ 得 RawTellOwk 到手；Lv5 開場直接給。
  Lv3+：審問開始時三選一（Rebuttal / Question）加入手牌；Lv5：裁判攻擊再 +罪證（罪證加成×2）。
- RawTellOwk(0費能力)：未開審問→開審問；已開→裁判進度 +10。
- 審問開始後，每打出（含自動打出）一張帶「裁判」的牌，進度 +1。
- 證物進手牌即被收入魔女圖鑑；提出證據＝從圖鑑選一張自動打出後放回（可重複）。
- 證言（Question 自動打出）：抽1+罪證2，之後也留在圖鑑可被提出。
- 議論 ≥10：每回合開始 +1 能量。罪證：審問中裁判攻擊每段 +罪證。
- 自動打出也觸發 AfterCardPlayed → 計入搜查張數。
- 戰鬥長度依 tools/card_score/turn_model.py 的 Gamma 分佈抽樣（不模擬敵人血量）。
"""
from __future__ import annotations
import random, json, sys
from collections import defaultdict

ENCOUNTERS = {"hallway": (4.0, 4.0, 1.0), "elite": (6.5, 4.0, 2.0), "boss": (10.0, 5.0, 3.0)}
EXP = {"hallway": 1, "elite": 2, "boss": 3}
LVL_EXP = [0, 2, 7, 15, 30]
ACT = ["hallway"] * 3 + ["elite"] + ["hallway"] * 3 + ["elite", "boss"]   # 每幕 6 一般 / 2 菁英 / 1 首領
RUN = ACT * 3
KILLS_PER_FIGHT = {"hallway": 1.6, "elite": 1.3, "boss": 1.0}

def schedule():
    """每場戰鬥開始時的 (類型, 腿環等級, 你做得很好層數, 第幾場)。"""
    out, exp, stacks = [], 0, 0.0
    for i, k in enumerate(RUN):
        lv = sum(exp >= e for e in LVL_EXP)
        out.append((k, lv, min(13, int(stacks)), i))
        exp += EXP[k]
        stacks += KILLS_PER_FIGHT[k]
    return out

SCHED = schedule()

# 證物靜態價值（能量單位；1能≈6傷≈5格擋），單人
import os
G_VAL = float(__import__('os').environ.get('G_VAL', '0.30'))  # 每層罪證，後由模擬回填
EVIDENCE = {
    # 5傷+暫時力量1；4格擋+暫時敏捷1；看守所藍圖=自動打出大掃除(暫時力量4，裁判攻擊不吃力量)
    "BrokenCrossbow": 1.10, "NoahsButterfly": 1.05, "PrisonBlueprint": float(os.environ.get("BP_VAL", "1.20")), "FruitBasket": 0.50,
    "WhiteSprayCan": 0.50, "HairRibbon": 0.83, "CocoStream": 0.47, "Broom": 1.00, "TortureShow": 0.77,
    "Padlock": 0.80, "ChippedKnife": 0.50, "PunishmentRoomKey": "G2", "ScorchedMarks": 0.75,
    "BulletMarks": 1.27, "WardenStatue": 0.30, "AlisaDoll": 0.70, "GuardRecording": 0.20,
    "RedPaint": 0.35, "ElectricChair": 0.80, "ShatteredGlass": 0.75, "MeltedCap": 0.60,
    "WitchFiles": 1.10, "Note": 0.80, "Safe": 0.02, "LargerDoor": 0.90, "ControlPanel": 2.30, "Vent": 0.50,
}
PLAY_LIMIT = 3   # 證物/證言每回合限打（升級 5）
EVID_NAMES = list(EVIDENCE)
TESTIMONIES = ["Ema", "Hiro", "Anan", "Noah", "Leia", "Miria", "Mago", "Nanoka", "Alisa", "Sherry",
               "Hanna", "Coco", "Meruru", "Warden", "Guard", "GrandWitch"]

def evid_value(name):
    if name == "HirosPen":
        return 1.0
    if name.startswith("T:"):
        t = name[2:]
        v = 0.5 + 1 * G_VAL
        if t == "Ema": v += 0.6
        if t == "Warden": v -= 0.05
        if t == "Guard": v += 0.3   # 暫時力量 2，僅非裁判攻擊吃到
        return v
    v = EVIDENCE[name]
    return 2 * G_VAL if v == "G2" else v

# 牌定義：cost, trial, kind, base value（貪婪優先序用）
CARDS = {
    "Strike": (1, False, 1.0), "Defend": (1, False, 1.0), "Trauma": (0, False, 0.3), "Search": (1, False, 1.4),
    "Present": (0, True, 0.7), "HirosPen": (0, False, 0), "Rebuttal": (1, True, 2.4), "Sophistry": (1, True, 1.7),
    "EvidenceStrike": (1, True, 2.6), "Question": (0, True, 1.2), "SusInv": (1, True, 2.6),
    "Exhaustive": (2, True, 3.6), "Hammer": (2, True, 3.1), "DeafEar": (1, True, 2.2), "Deliberation": (1, True, 0.8),
    "RawCard": (0, False, 9), "Endless": (1, False, 4.5), "RedTape": (0, True, 2.0), "JointInv": (1, False, 3.0),
    "LaVI": (2, True, 3.0), "JudgmentHour": (1, True, 2.4), "AccUh": (1, True, 1.9), "DArime": (2, True, 3.0),
    "BookBite": (2, False, 2.3), "Strangle": (1, False, 2.5), "LivingPage": (0, False, 0.5),
    "Statement": (0, True, 1.0), "RawTellOwk": (0, False, 10), "Fever": (0, False, 0.4), "MagicKill": (1, False, 2.0),
    "HouseKeep": (1, False, 0.6), "Die": (0, False, 3.0), "ESJD": (1, False, 1.6), "Psychic": (3, False, 5.0),
}
EXHAUST = {"Deliberation", "RedTape", "JudgmentHour", "DArime", "LivingPage", "Statement", "Die", "ESJD"}
POWERS = {"RawCard", "Endless", "LaVI", "RawTellOwk", "MagicKill", "Psychic"}
TRIAL_ATTACKS = {"Rebuttal", "Sophistry", "EvidenceStrike", "Exhaustive", "Hammer", "AccUh", "DArime"}
JH_POOL = ["Rebuttal", "Sophistry", "EvidenceStrike", "Question", "SusInv", "Exhaustive", "Hammer", "DeafEar",
           "Deliberation", "RedTape", "AccUh", "Present"]

STARTER = ["Strike"] * 3 + ["Defend"] * 3 + ["Trauma", "Search", "Search", "Present", "HirosPen"]
ARCHETYPES = {
    # 早期：起始牌組＋少量艾瑪普通卡
    "A_early": STARTER + ["Rebuttal", "Sophistry", "EvidenceStrike", "Question"],
    # 裁判：大量裁判牌
    "B_trial": STARTER + ["Rebuttal", "Rebuttal", "Sophistry", "EvidenceStrike", "Question", "Question", "SusInv",
                          "Exhaustive", "Hammer", "DeafEar", "Deliberation"],
    # 證物：搜證／提出證據＋牌組內的 RawTellOwk
    "C_evid": STARTER + ["RawCard", "Endless", "RedTape", "Rebuttal", "Rebuttal", "Question", "AccUh", "JointInv",
                         "Strangle", "DeafEar", "Search"],
}
ARCH_W = {"A_early": 0.3, "B_trial": 0.35, "C_evid": 0.35}


class Fight:
    def __init__(self, rng, deck, kind, lv, stacks, extra=()):
        self.r = rng; self.kind = kind; self.lv = lv
        d = list(deck) + list(extra)
        if stacks >= 5:
            d = ["TraumaTrue" if c == "Trauma" else c for c in d] + ["MagicKill"]
        self.draw = d[:]; rng.shuffle(self.draw)
        if "HirosPen" in self.draw:   # 固有
            self.draw.remove("HirosPen"); self.draw.append("HirosPen")
        self.uses = {}; self.tracers = []; self.tstr = 0; self.tstr_bp = 0
        self.stacks = stacks; self.special = False; self.transformed = False; self.end_turn = False
        self.has_magic = stacks >= 5
        self.disc = []; self.hand = []
        self.ency = []; self.majmult = (50 + 10 * stacks) / 100
        self.search_req = max(2, 10 - 2 * max(1, lv)); self.search = 0; self.search_done = False
        self.active = False; self.prog = 0; self.guilt = 0; self.arg = 0; self.sus = 0; self.maj = 0
        self.lawdevil = False; self.endless = 0; self.vuln = 0; self.weak = 0; self.burn = False; self.bleed = False
        self.ev = []   # 事件紀錄
        self.st = dict(start_turn=None, p10=None, p20=None, presents=0, present_val=0.0, present_empty=0,
                       arg10=None, guilt_hits=0.0, trial_hits=0, maj100=None,
                       hk_dmg=0.0, hk_plays=0, bp_presents=0, special_turn=None, special_val=0.0, magic_drawn=0,
                       psychic_play=None, special_turns=0)
        self.turn = 0
        self.pending_raw = lv >= 5
        if lv >= 5: self.search_done = True
        self.dmg = 0.0; self.hp = None; self.strength = 0; self.vigor = 0
        self.done_tracers = []

    def drawn(self, n):
        for _ in range(n):
            if not self.draw:
                if not self.disc: return
                self.draw = self.disc; self.disc = []; self.r.shuffle(self.draw)
            c = self.draw.pop()
            if c == "HirosPen":
                self.add_ency("HirosPen")    # 證物進手即收入圖鑑
            elif len(self.hand) < 10:
                self.hand.append(c)
            else:
                self.disc.append(c)

    def add_hand(self, c):
        if len(self.hand) < 10: self.hand.append(c)

    def after_play(self, trial):
        if self.active and trial:
            self.prog += 1
            if self.prog >= 10 and self.st["p10"] is None: self.st["p10"] = self.turn
            if self.prog >= 20 and self.st["p20"] is None: self.st["p20"] = self.turn
        if not self.search_done:
            self.search += 1
            if self.search >= self.search_req:
                self.search_done = True; self.add_hand("RawTellOwk")

    def gain_guilt(self, g):
        self.guilt += g; self.tracers.append([float(g), 0.0, g])   # [剩餘權重, 已貢獻段數, 原始層數]

    def add_ency(self, name):
        if name not in self.ency: self.ency.append(name)   # 同名合併

    def gen_evidence(self):
        self.add_ency(self.r.choice(EVID_NAMES))

    def present(self):
        avail = [x for x in self.ency if self.uses.get(x, 0) < PLAY_LIMIT]
        if not avail:
            self.st["present_empty"] += 1; self.st["presents"] += 1; return 0.0
        best = max(avail, key=evid_value)
        self.uses[best] = self.uses.get(best, 0) + 1
        v = evid_value(best)
        self.st["presents"] += 1; self.st["present_val"] += v
        self.resolve_evidence(best)
        self.after_play(False)   # 自動打出也算打出一張牌
        return v

    def resolve_evidence(self, e):
        if e == "HirosPen": self.drawn(1); self.vigor += 3
        elif e.startswith("T:"): self.testimony_effect(e[2:])
        elif e == "WitchFiles": self.drawn(2); self.arg += 1
        elif e in ("WhiteSprayCan", "MeltedCap", "ShatteredGlass"): self.drawn(1)
        elif e == "PunishmentRoomKey": self.gain_guilt(2)
        elif e == "Padlock": self.strength += 1
        elif e == "BrokenCrossbow": self.hit(5); self.tstr += 1
        elif e == "HairRibbon": self.hit(5)
        elif e == "BulletMarks": self.hit(4)
        elif e == "CocoStream": self.hit(2)
        elif e == "ElectricChair":
            for _ in range(5): self.hit(1)
        elif e == "ControlPanel": self.dmg += 13
        elif e == "PrisonBlueprint":   # 自動打出持家：暫時力量4；持家不消耗，進棄牌堆
            self.tstr += 4; self.tstr_bp += 4; self.disc.append("HouseKeep"); self.st["bp_presents"] += 1
        elif e == "TortureShow": self.sus += 1
        elif e in ("RedPaint",): self.vuln = max(self.vuln, 1)
        elif e == "WardenStatue": self.weak = max(self.weak, 1)
        elif e in ("ScorchedMarks", "AlisaDoll"): self.burn = True
        elif e == "ChippedKnife": self.bleed = True

    def testimony_effect(self, t):
        self.drawn(1); self.gain_guilt(1)
        if t == "Ema": self.add_hand("Statement")
        if t == "Guard": self.tstr += 2
        if t == "Warden": self.sus += 1

    BASE_DMG = {"Rebuttal": 9, "Sophistry": 9, "EvidenceStrike": 6, "Exhaustive": 20, "Hammer": 13, "AccUh": 5, "DArime": 13}

    def hit(self, d, powered=True):
        m = 1.5 if self.vuln > 0 else 1.0
        if powered:
            d += self.strength + self.tstr + self.vigor; self.vigor = 0
            self.st["hk_dmg"] += self.tstr_bp * m
        self.dmg += d * m

    def trial_attack(self, name, hits=1):
        bonus = (self.guilt * (2 if self.lv >= 5 else 1)) if self.active else 0
        base = self.BASE_DMG.get(name, 6)
        for i in range(hits):
            d = base + bonus
            if name == "Rebuttal" and self.prog >= 10: d += 1 * len(self.ency)
            if name == "Exhaustive" and self.prog >= 10: d += self.prog
            self.hit(d, powered=self.lawdevil)
        mult = (2 if self.lv >= 5 else 1) if self.active else 0
        for tr in self.tracers:
            tr[1] += tr[0] * hits * mult
            tr[0] /= 2   # 打出裁判攻擊後罪證減半（1 層時移除）
        self.tracers = [tr for tr in self.tracers if tr[0] >= 0.25 or self._log_tracer(tr)]
        self.guilt = 0 if self.guilt <= 1 else self.guilt // 2
        self.st["trial_hits"] += hits; self.st["guilt_hits"] += bonus * hits
        self.ev.append((name, self.turn, self.active, self.prog, len(self.ency), self.guilt * (2 if self.lv >= 5 else 1) if self.active else 0,
                        self.sus, (self.vuln > 0) + (self.weak > 0) + self.burn + self.bleed + (self.maj > 0 and False), hits))

    def _log_tracer(self, tr):
        self.done_tracers.append(tr); return False

    def play(self, c):
        cost, trial, _ = CARDS.get(c, (0, False, 0))
        self.hand.remove(c)
        # 先記錄打出時狀態（給非攻擊卡也用）
        self.ev.append(("@" + c, self.turn, self.active, self.prog, len(self.ency), self.guilt, self.sus, 0, 0))
        if c == "Strike": self.hit(6)
        elif c == "Strangle": self.hit(9)
        elif c == "LivingPage": self.hit(2)
        if c == "Trauma": self.maj += max(1, int(10 * self.majmult))
        elif c == "TraumaTrue": self.maj += max(1, int(20 * self.majmult))
        elif c == "Search": self.drawn(1); self.gen_evidence()
        elif c == "Present": self.present()
        elif c == "Rebuttal": self.present(); self.trial_attack(c)
        elif c == "EvidenceStrike": self.present(); self.trial_attack(c); self.add_hand("LivingPage")
        elif c == "AccUh":
            self.present()
            hits = min(5, 1 + len(self.ency)) if self.prog >= 10 else 1
            self.trial_attack(c, hits)
        elif c == "Hammer":
            self.present()
            deb = (self.vuln > 0) + (self.weak > 0) + self.burn + self.bleed
            self.trial_attack(c, 1 + (deb if self.prog >= 10 else 0))
        elif c == "Sophistry": self.trial_attack(c, 2 if (self.prog >= 10 and self.sus > 0) else 1)
        elif c == "Exhaustive": self.trial_attack(c)
        elif c == "Question":
            self.drawn(1); t = self.r.choice(TESTIMONIES)
            self.add_ency("T:" + t)   # 生成證言進手牌即收入圖鑑，不自動打出
        elif c == "Statement": self.drawn(1); self.arg += 1
        elif c == "SusInv": self.drawn(1); self.sus += 1
        elif c == "DeafEar": self.weak = max(self.weak, 2)
        elif c == "Deliberation": n = min(2, len(self.hand)); self.disc += self.hand[:n]; del self.hand[:n]; self.drawn(n)
        elif c in ("RawTellOwk", "RawCard"):
            if self.active: self.prog += 10
            else:
                self.active = True; self.st["start_turn"] = self.turn
                if self.lv >= 3: self.add_hand("Rebuttal")
            if self.prog >= 10 and self.st["p10"] is None: self.st["p10"] = self.turn
            if self.prog >= 20 and self.st["p20"] is None: self.st["p20"] = self.turn
        elif c == "Endless": self.endless += 1
        elif c == "RedTape":
            for _ in range(3): self.present()
        elif c == "JointInv": self.drawn(2); self.gen_evidence(); self.gen_evidence()
        elif c == "LaVI": self.lawdevil = True
        elif c == "JudgmentHour":
            self.add_hand(self.r.choice(JH_POOL))
        elif c == "DArime":
            hits = 0
            while self.draw and CARDS.get(self.draw[-1], (0, False))[1] and hits < 10:
                self.draw.pop(); hits += 1
            self.trial_attack(c, hits)
        elif c == "BookBite": self.vuln = max(self.vuln, 3); self.add_hand("LivingPage")
        elif c == "LivingPage": self.vuln = max(self.vuln, 1)
        elif c == "Fever": self.maj += max(1, int(20 * self.majmult))
        elif c == "HouseKeep": self.tstr += 4; self.tstr_bp += 4; self.st["hk_plays"] += 1
        elif c == "Die":
            up = self.stacks >= 10
            self.dmg += (5 if up else 3) * 1.5
            self.st["special_val"] += 3.1 if up else 2.2   # 全體5/3傷＋末日13/10層×0.15
        elif c == "ESJD":
            self.drawn(1); self.st["special_val"] += (2.3 if self.stacks >= 10 else 1.6) - 1.0
        elif c == "Psychic":
            self.maj = max(self.maj, 100); self.end_turn = True
            if self.st["psychic_play"] is None: self.st["psychic_play"] = self.turn
        elif c == "MagicKill":
            if self.maj >= 100:
                self.special = True; self.st["special_turn"] = self.turn; self.end_turn = True
                if self.stacks >= 13: self.transformed = True
                if self.stacks >= 6: self.st["special_val"] += 1.5   # 衍生 CUrOi/ShiDes
        if self.maj >= 100 and self.st["maj100"] is None: self.st["maj100"] = self.turn
        if c in EXHAUST or c in POWERS: pass
        else: self.disc.append(c)
        self.after_play(trial)

    def playable(self, c, e):
        cost = CARDS[c][0] if c in CARDS else 0
        if c == "DArime" and self.prog < 20: return False
        if c == "MagicKill" and self.maj < 100 and self.turn < 6: return False
        if c == "Psychic" and (self.maj >= 100 or not self.has_magic or self.special): return False
        if c == "HouseKeep" and not any(x in ("Strike", "Strangle") for x in self.hand): return False
        return cost <= e

    def prio(self, c):
        cost, _, v = CARDS.get(c, (0, False, 0.3))
        if c in ("RawTellOwk", "RawCard"): return 100
        if c == "Present" or c == "RedTape": return 50 if self.ency else 0.1
        if cost == 0: return 40 + v
        return v / cost

    def run(self, length, hp):
        self.hp = hp; self.st["len"] = length
        for t in range(1, length + 1):
            if self.dmg >= hp:
                self.st["len"] = t - 1; break
            self.turn = t
            e = 3 + (1 if self.arg >= 10 else 0)
            if self.arg >= 10 and self.st["arg10"] is None: self.st["arg10"] = t
            if self.lawdevil: self.gain_guilt(1); self.arg += 1
            for _ in range(self.endless): self.gen_evidence()
            self.uses = {}
            if t == 1 and self.pending_raw: self.add_hand("RawTellOwk")
            self.end_turn = False
            if self.special:
                self.st["special_turns"] += 1
                if self.transformed:   # 全牌轉為去死吧+：每回合約5張 0 費
                    self.st["special_val"] += 5 * 3.1; self.dmg += 5 * 7.5
                    continue
                self.add_hand("Die"); self.add_hand("Die"); self.add_hand("ESJD")
            self.drawn(5)
            if "MagicKill" in self.hand: self.st["magic_drawn"] += 1
            while not self.end_turn:
                opts = [c for c in self.hand if self.playable(c, e)]
                if not opts: break
                c = max(opts, key=self.prio)
                e -= CARDS.get(c, (0,))[0]
                self.play(c)
            self.disc += self.hand; self.hand = []
            self.vuln = max(0, self.vuln - 1); self.weak = max(0, self.weak - 1)
            self.tstr = 0; self.uses = {}
        allt = self.done_tracers + self.tracers
        self.st["g_marg_num"] = sum(tr[1] for tr in allt)
        self.st["g_marg_den"] = sum(tr[2] for tr in allt)
        self.st["guilt_end"] = self.guilt
        return self


# 敵人血量 = 抽樣回合數 × 基準每回合輸出；強牌組會提早結束，上限 L+3 回合
DPT = {"hallway": 24, "elite": 30, "boss": 36}
ACT_HP = (1.0, 1.6, 2.2)   # 敵人血量隨幕數提高（固定牌組下代表後期敵人）
EARLY_END = __import__('os').environ.get('EARLY_END', '1') == '1'


def sample_fight(rng):
    k, lv, stacks, idx = rng.choice(SCHED)
    mean, shape, imp = ENCOUNTERS[k]
    L = max(2, round(rng.gammavariate(shape, mean / shape)))
    return k, lv, stacks, L, imp, idx // 9


def simulate(arch, N=30000, seed=1, extra=()):
    rng = random.Random(seed)
    res = []
    for _ in range(N):
        k, lv, stacks, L, imp, act = sample_fight(rng)
        hp = L * DPT[k] * ACT_HP[act] if EARLY_END else 1e9
        f = Fight(rng, ARCHETYPES[arch], k, lv, stacks, extra).run(L + 3 if EARLY_END else L, hp)
        res.append((k, lv, stacks, L, imp, f.st, f.ev))
    return res


def wavg(pairs):
    s = sum(w for _, w in pairs); return sum(v * w for v, w in pairs) / s if s else 0.0


def summarize(res):
    out = {}
    W = [(r[4]) for r in res]
    out["P_start"] = wavg([(r[5]["start_turn"] is not None, r[4]) for r in res])
    out["P_p10"] = wavg([(r[5]["p10"] is not None, r[4]) for r in res])
    out["P_p20"] = wavg([(r[5]["p20"] is not None, r[4]) for r in res])
    out["P_arg10"] = wavg([(r[5]["arg10"] is not None, r[4]) for r in res])
    out["P_maj100"] = wavg([(r[5]["maj100"] is not None, r[4]) for r in res])
    hist = defaultdict(float); hs = defaultdict(float)
    for r in res:
        if r[5]["start_turn"] is not None: hs[r[5]["start_turn"]] += r[4]
        if r[5]["p10"] is not None: hist[r[5]["p10"]] += r[4]
    tw = sum(W)
    out["start_turn_dist"] = {t: round(v / tw, 3) for t, v in sorted(hs.items()) if t <= 10}
    out["p10_turn_dist"] = {t: round(v / tw, 3) for t, v in sorted(hist.items()) if t <= 14}
    out["presents_per_fight"] = wavg([(r[5]["presents"], r[4]) for r in res])
    out["present_avg_value"] = sum(r[5]["present_val"] * r[4] for r in res) / max(1e-9, sum((r[5]["presents"] - r[5]["present_empty"]) * r[4] for r in res))
    out["present_empty_rate"] = sum(r[5]["present_empty"] * r[4] for r in res) / max(1e-9, sum(r[5]["presents"] * r[4] for r in res))
    out["mean_len"] = wavg([(r[5]["len"], r[4]) for r in res])
    out["mean_len_by"] = {k: round(sum(r[5]["len"] for r in res if r[0]==k)/max(1,sum(1 for r in res if r[0]==k)),2) for k in ENCOUNTERS}
    out["mean_guilt_end"] = wavg([(r[5]["guilt_end"], r[4]) for r in res])
    out["guilt_marginal_hits"] = sum(r[5]["g_marg_num"] * r[4] for r in res) / max(1e-9, sum(r[5]["g_marg_den"] * r[4] for r in res))
    out["trial_hits_per_fight"] = wavg([(r[5]["trial_hits"], r[4]) for r in res])
    out["guilt_bonus_dmg_per_fight"] = wavg([(r[5]["guilt_hits"], r[4]) for r in res])
    return out


def card_stats(res, name):
    """某張牌被打出當下的條件機率（重要性加權）。"""
    rows = [(e, r[4]) for r in res for e in r[6] if e[0] == name]
    if not rows: return None
    n = sum(w for _, w in rows)
    g = lambda f: sum(f(e) * w for e, w in rows) / n
    return dict(n=len(rows), ready=g(lambda e: e[3] >= 10), ready20=g(lambda e: e[3] >= 20), active=g(lambda e: e[2]),
                ency=g(lambda e: e[4]), ency_ready=g(lambda e: e[4] * (e[3] >= 10)), prog_ready=g(lambda e: e[3] * (e[3] >= 10)),
                guilt=g(lambda e: e[5]), sus_ready=g(lambda e: (e[3] >= 10) * (e[6] > 0)), sus_amt_ready=g(lambda e: (e[3] >= 10) * e[6]),
                deb_ready=g(lambda e: (e[3] >= 10) * e[7]), hits=g(lambda e: e[8]), empty=g(lambda e: e[4] == 0), turn=g(lambda e: e[1]))


if __name__ == "__main__":
    N = int(sys.argv[1]) if len(sys.argv) > 1 else 30000
    allres = {}
    for a in ARCHETYPES:
        allres[a] = simulate(a, N, seed=sum(map(ord, a)))
    summ = {a: summarize(allres[a]) for a in allres}
    # 罪證價值：審問中每層罪證帶來的後續裁判攻擊段數
    names = ["Rebuttal", "Sophistry", "EvidenceStrike", "Exhaustive", "Hammer", "AccUh", "SusInv", "DeafEar"]
    cs = {a: {nm: card_stats(allres[a], nm) for nm in names} for a in allres}
    cs0 = {a: {nm: card_stats(allres[a], "@" + nm) for nm in ["Present", "Question", "RedTape", "Search", "Trauma", "TraumaTrue", "MagicKill"]} for a in allres}
    json.dump({"summary": summ, "attack": cs, "plays": cs0, "sched": SCHED}, open(sys.argv[2] if len(sys.argv) > 2 else "out.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(json.dumps(summ, ensure_ascii=False, indent=1))
