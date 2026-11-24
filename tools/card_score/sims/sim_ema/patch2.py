p = r'C:\Users\a2211\AppData\Local\Temp\claude\bddbd92c538df783\c381d8a3-a740-49e0-8d36-a2f77a091d23\scratchpad\sim_ema\ema_sim.py'
s = open(p, encoding='utf-8-sig').read()
def rep(a, b, cnt=1):
    global s
    assert s.count(a) == cnt, (a, s.count(a))
    s = s.replace(a, b)
rep("self.dmg = 0.0; self.hp = None; self.strength = 0; self.vigor = 0\n",
    "self.dmg = 0.0; self.hp = None; self.strength = 0; self.vigor = 0\n        self.whits = 0.0; self.ggain = []\n")
rep("self.guilt += 2", "self.gain_guilt(2)")
rep("self.guilt += 1", "self.gain_guilt(1)", 2)
rep("    def gen_evidence(self):", """    def gain_guilt(self, g):
        self.guilt += g; self.ggain.append((g, self.whits))

    def gen_evidence(self):""")
rep("        self.st[\"trial_hits\"] += hits;", "        if self.active: self.whits += hits * (2 if self.lv >= 5 else 1)\n        self.st[\"trial_hits\"] += hits;")
rep("""            self.vuln = max(0, self.vuln - 1); self.weak = max(0, self.weak - 1)
        return self""", """            self.vuln = max(0, self.vuln - 1); self.weak = max(0, self.weak - 1)
        self.st["g_marg_num"] = sum(g * (self.whits - w) for g, w in self.ggain)
        self.st["g_marg_den"] = sum(g for g, _ in self.ggain)
        self.st["guilt_end"] = self.guilt
        return self""")
rep("""    out["mean_guilt_end"] = 0""", """    out["mean_guilt_end"] = wavg([(r[5]["guilt_end"], r[4]) for r in res])
    out["guilt_marginal_hits"] = sum(r[5]["g_marg_num"] * r[4] for r in res) / max(1e-9, sum(r[5]["g_marg_den"] * r[4] for r in res))""")
rep("G_VAL = 0.30", "G_VAL = float(__import__('os').environ.get('G_VAL', '0.30'))")
rep("EARLY_END = True", "EARLY_END = __import__('os').environ.get('EARLY_END', '1') == '1'")
open(p, 'w', encoding='utf-8').write(s)
