p = r'C:\Users\a2211\AppData\Local\Temp\claude\bddbd92c538df783\c381d8a3-a740-49e0-8d36-a2f77a091d23\scratchpad\sim_ema\ema_sim.py'
s = open(p, encoding='utf-8').read()
def rep(a, b):
    global s
    assert a in s, a[:60]
    s = s.replace(a, b)
rep("""        self.pending_raw = lv >= 5
""", """        self.pending_raw = lv >= 5
        if lv >= 5: self.search_done = True
        self.dmg = 0.0; self.hp = None; self.strength = 0; self.vigor = 0
""")
rep("""    def trial_attack(self, name, hits=1):
        bonus = (self.guilt * (2 if self.lv >= 5 else 1)) if self.active else 0
""", """    BASE_DMG = {"Rebuttal": 9, "Sophistry": 9, "EvidenceStrike": 6, "Exhaustive": 20, "Hammer": 13, "AccUh": 5, "DArime": 13}

    def hit(self, d, powered=True):
        if powered: d += self.strength + self.vigor; self.vigor = 0
        self.dmg += d * (1.5 if self.vuln > 0 else 1.0)

    def trial_attack(self, name, hits=1):
        bonus = (self.guilt * (2 if self.lv >= 5 else 1)) if self.active else 0
        base = self.BASE_DMG.get(name, 6)
        for i in range(hits):
            d = base + bonus
            if name == "Rebuttal" and self.prog >= 10: d += 2 * len(self.ency)
            if name == "Exhaustive" and self.prog >= 10: d += self.prog
            self.hit(d, powered=self.lawdevil)
""")
rep("""        if c == "Trauma": self.maj""", """        if c == "Strike": self.hit(6)
        elif c == "Strangle": self.hit(9)
        elif c == "LivingPage": self.hit(2)
        if c == "Trauma": self.maj""")
rep("""        elif e == "PunishmentRoomKey": self.guilt += 1""", """        elif e == "PunishmentRoomKey": self.guilt += 1
        elif e == "Padlock": self.strength += 1
        elif e == "BrokenCrossbow": self.hit(4)
        elif e == "HairRibbon": self.hit(3)
        elif e == "ElectricChair": self.hit(1); self.hit(1); self.hit(1)
        elif e == "ControlPanel": self.dmg += 8""")
rep("""        if e == "HirosPen": self.drawn(1)""", """        if e == "HirosPen": self.drawn(1); self.vigor += 3""")
rep("""    def run(self, length):
        for t in range(1, length + 1):""", """    def run(self, length, hp):
        self.hp = hp; self.st["len"] = length
        for t in range(1, length + 1):
            if self.dmg >= hp:
                self.st["len"] = t - 1; break""")
rep("""        f = Fight(rng, ARCHETYPES[arch], k, lv, stacks, extra).run(L)""", """        f = Fight(rng, ARCHETYPES[arch], k, lv, stacks, extra).run(L + 3, L * DPT[k])""")
rep("""def sample_fight(rng):""", """# 敵人血量 = 抽樣回合數 × 基準每回合輸出；強牌組會提早結束，上限 L+3 回合
DPT = {"hallway": 14, "elite": 16, "boss": 18}


def sample_fight(rng):""")
rep("""    out["trial_hits_per_fight"]""", """    out["mean_len"] = wavg([(r[5]["len"], r[4]) for r in res])
    out["mean_len_by"] = {k: round(sum(r[5]["len"] for r in res if r[0]==k)/max(1,sum(1 for r in res if r[0]==k)),2) for k in ENCOUNTERS}
    out["mean_guilt_end"] = 0
    out["trial_hits_per_fight"]""")
open(p, 'w', encoding='utf-8').write(s)
