"""依 45b867f / 579746c 平衡改動更新 ema_sim.py：
罪證在打出裁判攻擊後減半、證言罪證 2→1、質問改為生成證言進圖鑑（不自動打出）、
圖鑑同名合併、證物/證言每回合限打 3 次、證物數值改動、反駁每張 +1、希羅的筆固有。"""
import os
p = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'ema_sim.py')
s = open(p, encoding='utf-8-sig').read()

def rep(a, b, cnt=1):
    global s
    assert s.count(a) == cnt, (a[:70], s.count(a))
    s = s.replace(a, b)

rep('''EVIDENCE = {
    "BrokenCrossbow": 0.67, "NoahsButterfly": 0.60, "PrisonBlueprint": 0.55, "FruitBasket": 0.50,
    "WhiteSprayCan": 0.50, "HairRibbon": 0.50, "CocoStream": 0.23, "Broom": 1.00, "TortureShow": 0.37,
    "Padlock": 0.80, "ChippedKnife": 0.50, "PunishmentRoomKey": "G1", "ScorchedMarks": 0.75,
    "BulletMarks": 0.37, "WardenStatue": 0.30, "AlisaDoll": 0.35, "GuardRecording": 0.20,
    "RedPaint": 0.35, "ElectricChair": 0.50, "ShatteredGlass": 0.75, "MeltedCap": 0.60,
    "WitchFiles": 1.00, "Note": 0.40, "Safe": 0.02, "LargerDoor": 0.90, "ControlPanel": 1.00, "Vent": 0.50,
}''', '''EVIDENCE = {
    # 5傷+暫時力量1；4格擋+暫時敏捷1；看守所藍圖=自動打出大掃除(暫時力量4，裁判攻擊不吃力量)
    "BrokenCrossbow": 1.10, "NoahsButterfly": 1.05, "PrisonBlueprint": 0.70, "FruitBasket": 0.50,
    "WhiteSprayCan": 0.50, "HairRibbon": 0.83, "CocoStream": 0.47, "Broom": 1.00, "TortureShow": 0.77,
    "Padlock": 0.80, "ChippedKnife": 0.50, "PunishmentRoomKey": "G2", "ScorchedMarks": 0.75,
    "BulletMarks": 1.27, "WardenStatue": 0.30, "AlisaDoll": 0.70, "GuardRecording": 0.20,
    "RedPaint": 0.35, "ElectricChair": 0.80, "ShatteredGlass": 0.75, "MeltedCap": 0.60,
    "WitchFiles": 1.10, "Note": 0.80, "Safe": 0.02, "LargerDoor": 0.90, "ControlPanel": 1.00, "Vent": 0.50,
}
PLAY_LIMIT = 3   # 證物/證言每回合限打（升級 5）''')
rep('''        v = 0.5 + 2 * G_VAL
        if t == "Ema": v += 0.6
        if t == "Warden": v -= 0.05
        if t == "Guard": v += 0.8
        return v
    v = EVIDENCE[name]
    return G_VAL if v == "G1" else v''', '''        v = 0.5 + 1 * G_VAL
        if t == "Ema": v += 0.6
        if t == "Warden": v -= 0.05
        if t == "Guard": v += 0.3   # 暫時力量 2，僅非裁判攻擊吃到
        return v
    v = EVIDENCE[name]
    return 2 * G_VAL if v == "G2" else v''')
rep('''        self.draw = d[:]; rng.shuffle(self.draw)''', '''        self.draw = d[:]; rng.shuffle(self.draw)
        if "HirosPen" in self.draw:   # 固有
            self.draw.remove("HirosPen"); self.draw.append("HirosPen")
        self.uses = {}; self.tracers = []; self.tstr = 0''')
rep('''            if c == "HirosPen":
                self.ency.append("HirosPen")    # 證物進手即收入圖鑑''', '''            if c == "HirosPen":
                self.add_ency("HirosPen")    # 證物進手即收入圖鑑''')
rep('''    def gain_guilt(self, g):
        self.guilt += g; self.ggain.append((g, self.whits))

    def gen_evidence(self):
        self.ency.append(self.r.choice(EVID_NAMES))''', '''    def gain_guilt(self, g):
        self.guilt += g; self.tracers.append([float(g), 0.0, g])   # [剩餘權重, 已貢獻段數, 原始層數]

    def add_ency(self, name):
        if name not in self.ency: self.ency.append(name)   # 同名合併

    def gen_evidence(self):
        self.add_ency(self.r.choice(EVID_NAMES))''')
rep('''        if not self.ency:
            self.st["present_empty"] += 1; self.st["presents"] += 1; return 0.0
        best = max(self.ency, key=evid_value)''', '''        avail = [x for x in self.ency if self.uses.get(x, 0) < PLAY_LIMIT]
        if not avail:
            self.st["present_empty"] += 1; self.st["presents"] += 1; return 0.0
        best = max(avail, key=evid_value)
        self.uses[best] = self.uses.get(best, 0) + 1''')
rep('''        elif e == "WitchFiles": self.drawn(2)''', '''        elif e == "WitchFiles": self.drawn(2); self.arg += 1''')
rep('''        elif e == "PunishmentRoomKey": self.gain_guilt(1)''', '''        elif e == "PunishmentRoomKey": self.gain_guilt(2)''')
rep('''        elif e == "BrokenCrossbow": self.hit(4)
        elif e == "HairRibbon": self.hit(3)
        elif e == "ElectricChair": self.hit(1); self.hit(1); self.hit(1)''', '''        elif e == "BrokenCrossbow": self.hit(5); self.tstr += 1
        elif e == "HairRibbon": self.hit(5)
        elif e == "BulletMarks": self.hit(4)
        elif e == "CocoStream": self.hit(2)
        elif e == "ElectricChair":
            for _ in range(5): self.hit(1)''')
rep('''        self.drawn(1); self.gain_guilt(2)
        if t == "Ema": self.add_hand("Statement")''', '''        self.drawn(1); self.gain_guilt(1)
        if t == "Ema": self.add_hand("Statement")
        if t == "Guard": self.tstr += 2''')
rep('''        if powered: d += self.strength + self.vigor; self.vigor = 0''', '''        if powered: d += self.strength + self.tstr + self.vigor; self.vigor = 0''')
rep('''            if name == "Rebuttal" and self.prog >= 10: d += 2 * len(self.ency)''', '''            if name == "Rebuttal" and self.prog >= 10: d += 1 * len(self.ency)''')
rep('''        if self.active: self.whits += hits * (2 if self.lv >= 5 else 1)''', '''        mult = (2 if self.lv >= 5 else 1) if self.active else 0
        for tr in self.tracers:
            tr[1] += tr[0] * hits * mult
            tr[0] /= 2   # 打出裁判攻擊後罪證減半（1 層時移除）
        self.tracers = [tr for tr in self.tracers if tr[0] >= 0.25 or self._log_tracer(tr)]
        self.guilt = 0 if self.guilt <= 1 else self.guilt // 2''')
rep('''    def play(self, c):''', '''    def _log_tracer(self, tr):
        self.done_tracers.append(tr); return False

    def play(self, c):''')
rep('''        self.whits = 0.0; self.ggain = []''', '''        self.done_tracers = []''')
rep('''            self.drawn(1); t = self.r.choice(TESTIMONIES)
            self.testimony_effect(t); self.after_play(False); self.ency.append("T:" + t)''', '''            self.drawn(1); t = self.r.choice(TESTIMONIES)
            self.add_ency("T:" + t)   # 生成證言進手牌即收入圖鑑，不自動打出''')
rep('''        self.st["g_marg_num"] = sum(g * (self.whits - w) for g, w in self.ggain)
        self.st["g_marg_den"] = sum(g for g, _ in self.ggain)''', '''        allt = self.done_tracers + self.tracers
        self.st["g_marg_num"] = sum(tr[1] for tr in allt)
        self.st["g_marg_den"] = sum(tr[2] for tr in allt)''')
rep('''            self.vuln = max(0, self.vuln - 1); self.weak = max(0, self.weak - 1)
        allt''', '''            self.vuln = max(0, self.vuln - 1); self.weak = max(0, self.weak - 1)
            self.tstr = 0; self.uses = {}
        allt''')
rep('''            for _ in range(self.endless): self.gen_evidence()''', '''            for _ in range(self.endless): self.gen_evidence()
            self.uses = {}''')
open(p, 'w', encoding='utf-8', newline='\n').write(s)
print("patched")
