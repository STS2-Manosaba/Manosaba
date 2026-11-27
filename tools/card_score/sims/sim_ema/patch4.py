"""補模擬：魔女島監獄藍圖→持家（暫時力量4，只作用於非裁判攻擊；持家卡進棄牌堆累積）、
控制面板（隨機處刑卡自動打出）、殺死魔女的魔法→特別的魔法（每回合2張去死吧+1張大家都去死吧，
你做得很好≥13層時全牌轉為去死吧+）、念寫相片（魔女化補到100並結束回合）。"""
import os
p = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'ema_sim.py')
s = open(p, encoding='utf-8-sig').read()

def rep(a, b, cnt=1):
    global s
    assert s.count(a) == cnt, (a[:70], s.count(a))
    s = s.replace(a, b)

rep('"PrisonBlueprint": 0.70,', '"PrisonBlueprint": float(os.environ.get("BP_VAL", "0.70")),')
rep('"ControlPanel": 1.00,', '"ControlPanel": 2.30,')   # 12 種可生成處刑卡 v5 估值平均 2.69 ×0.85 隨機目標折扣
rep('G_VAL = float(', 'import os\nG_VAL = float(')
rep('''"MagicKill": (1, False, 2.0),''', '''"MagicKill": (1, False, 2.0),
    "HouseKeep": (1, False, 0.6), "Die": (0, False, 3.0), "ESJD": (1, False, 1.6), "Psychic": (3, False, 5.0),''')
rep('''EXHAUST = {"Deliberation", "RedTape", "JudgmentHour", "DArime", "LivingPage", "Statement"}''',
    '''EXHAUST = {"Deliberation", "RedTape", "JudgmentHour", "DArime", "LivingPage", "Statement", "Die", "ESJD"}''')
rep('''POWERS = {"RawCard", "Endless", "LaVI", "RawTellOwk", "MagicKill"}''', '''POWERS = {"RawCard", "Endless", "LaVI", "RawTellOwk", "MagicKill", "Psychic"}''')
rep('''        self.uses = {}; self.tracers = []; self.tstr = 0''', '''        self.uses = {}; self.tracers = []; self.tstr = 0; self.tstr_bp = 0
        self.stacks = stacks; self.special = False; self.transformed = False; self.end_turn = False
        self.has_magic = stacks >= 5''')
rep('''                       arg10=None, guilt_hits=0.0, trial_hits=0, maj100=None)''', '''                       arg10=None, guilt_hits=0.0, trial_hits=0, maj100=None,
                       hk_dmg=0.0, hk_plays=0, bp_presents=0, special_turn=None, special_val=0.0, magic_drawn=0,
                       psychic_play=None, special_turns=0)''')
rep('''        if powered: d += self.strength + self.tstr + self.vigor; self.vigor = 0
        self.dmg += d * (1.5 if self.vuln > 0 else 1.0)''', '''        m = 1.5 if self.vuln > 0 else 1.0
        if powered:
            d += self.strength + self.tstr + self.vigor; self.vigor = 0
            self.st["hk_dmg"] += self.tstr_bp * m
        self.dmg += d * m''')
rep('''        elif e == "ControlPanel": self.dmg += 8''', '''        elif e == "ControlPanel": self.dmg += 13
        elif e == "PrisonBlueprint":   # 自動打出持家：暫時力量4；持家不消耗，進棄牌堆
            self.tstr += 4; self.tstr_bp += 4; self.disc.append("HouseKeep"); self.st["bp_presents"] += 1''')
rep('''        elif c == "Fever": self.maj += max(1, int(20 * self.majmult))''', '''        elif c == "Fever": self.maj += max(1, int(20 * self.majmult))
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
                if self.stacks >= 6: self.st["special_val"] += 1.5   # 衍生 CUrOi/ShiDes''')
rep('''        if c == "DArime" and self.prog < 20: return False''', '''        if c == "DArime" and self.prog < 20: return False
        if c == "MagicKill" and self.maj < 100 and self.turn < 6: return False
        if c == "Psychic" and (self.maj >= 100 or not self.has_magic or self.special): return False
        if c == "HouseKeep" and not any(x in ("Strike", "Strangle") for x in self.hand): return False''')
rep('''            if t == 1 and self.pending_raw: self.add_hand("RawTellOwk")
            self.drawn(5)
            while True:''', '''            if t == 1 and self.pending_raw: self.add_hand("RawTellOwk")
            self.end_turn = False
            if self.special:
                self.st["special_turns"] += 1
                if self.transformed:   # 全牌轉為去死吧+：每回合約5張 0 費
                    self.st["special_val"] += 5 * 3.1; self.dmg += 5 * 7.5
                    continue
                self.add_hand("Die"); self.add_hand("Die"); self.add_hand("ESJD")
            self.drawn(5)
            if "MagicKill" in self.hand: self.st["magic_drawn"] += 1
            while not self.end_turn:''')
open(p, 'w', encoding='utf-8', newline='\n').write(s)
print("patched")
