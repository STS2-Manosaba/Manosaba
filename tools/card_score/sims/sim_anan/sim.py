"""夏目安安 簡化戰鬥蒙地卡羅模擬。
規則依 Manosaba/Characters/NatsumeAnan 原始碼：
- 言靈：每場從 0 開始、不在回合間清空（上限 999）；Clipboard 每回合開始 +1/+2/+3（Lv1 / Lv2~3 / Lv4~5），
  以卡牌費用花費言靈時每點得 3 格檔（自動打出不花費、不給格檔）；開戰獲得體弱多病。
- 遺物經驗 0/2/7/15/30（一般 1、菁英 2、首領 3），整局 39 場（每幕 10 一般+2 菁英+1 首領）依序抽樣。
- 體弱多病：狀態牌進入戰鬥牌堆時複製一張（虛無+消耗）；抽到狀態牌補抽 1 並回 1 血。
- 魔女化：Hill 曲線傷害倍率；>=100 時若牌組沒有洗腦魔法則加入牌組與手牌。
- 敵人抽象化：戰鬥長度 Gamma（turn_model：4/6.5/10 回合），敵人依序排程死亡，每敵每回合 65% 攻擊。
價值單位：6 傷 = 1 能量、5 點少失血 = 1 能量、狀態牌回血 0.22/點。
"""
from __future__ import annotations
import random
from collections import defaultdict

DMG_PER_E = 6.0
HP_PER_E = 5.0
HEAL_E = 0.22

ENC = {  # 平均回合, gamma 形狀, 重要性, 敵人數分佈, 每敵攻擊基礎傷害
    "hallway": (4.0, 4.0, 1.0, [(1, .35), (2, .40), (3, .25)], 7.0),
    "elite":   (6.5, 4.0, 2.0, [(1, .6), (2, .3), (3, .1)], 13.0),
    "boss":    (10.0, 5.0, 3.0, [(1, .8), (2, .2)], 17.0),
}
IMP = {k: v[2] for k, v in ENC.items()}
ACT_SCALE = [1.0, 1.35, 1.7]
RUN = []
for _act in range(3):
    _seq = ["hallway"] * 10
    _seq.insert(4, "elite"); _seq.insert(9, "elite")
    _seq.append("boss")
    RUN += [(_act, k) for k in _seq]
EXP_GAIN = {"hallway": 1, "elite": 2, "boss": 3}
LV_TABLE = [0, 2, 7, 15, 30]
_exp = 0
RUN_K = []  # 每場戰鬥的 Clipboard 言靈/回合
for _act, _k in RUN:
    lv = sum(1 for t in LV_TABLE if _exp >= t)
    RUN_K.append(3 if lv >= 4 else 2 if lv >= 2 else 1)
    _exp += EXP_GAIN[_k]


def majoka_mult(s):
    if s <= 0:
        return 1.0
    k = 166.6 ** 1.8365
    p = s ** 1.8365
    return 1 + 3 * p / (p + k)


# name: (energy, kotodama cost, type)
CARDS = {
    "Strike": (1, 0, "A"), "Defend": (1, 0, "S"), "Trauma": (0, 0, "S"), "Flash": (1, 0, "S"),
    "Jigaishiro": (0, 1, "S"), "Instigate": (0, 1, "S"), "Reading": (1, 0, "S"), "Bocchan": (1, 0, "P"),
    "Endurance": (1, 0, "S"), "GeniusIdea": (0, 2, "S"), "Nandato": (0, 2, "S"), "Nanigaiitai": (0, 2, "A"),
    "NovelCreation": (0, 5, "S"), "PaperMaking": (1, 0, "S"), "Kokoro": (3, 0, "P"),
    "MovableTypePrinting": (2, 0, "P"), "Wagahaiwanekodearu": (2, 0, "P"), "Brainwash": (0, 20, "P"),
    "KokoroSuicide": (0, 0, "S"), "Dazed": (None, 0, "Status"),
}
EXHAUST = {"KokoroSuicide"}
POWERS = {"Bocchan", "Kokoro", "MovableTypePrinting", "Wagahaiwanekodearu", "Brainwash"}
QSPEND_ORDER = ["NovelCreation", "GeniusIdea", "Nanigaiitai", "Nandato", "Jigaishiro", "Instigate"]

START = ["Strike"] * 3 + ["Defend"] * 3 + ["Trauma", "Flash", "Jigaishiro", "Instigate"]
ARCH = {
    "engine": START + ["Bocchan", "MovableTypePrinting", "Reading", "Reading", "Endurance", "PaperMaking",
                       "GeniusIdea", "NovelCreation", "Nanigaiitai", "Nandato"],
    "kokoro": START + ["Kokoro", "Brainwash", "Bocchan", "Reading", "Endurance", "NovelCreation", "GeniusIdea",
                       "Nanigaiitai", "Nandato", "Wagahaiwanekodearu"],
    "balanced": START + ["Nanigaiitai", "Nanigaiitai", "Nandato", "GeniusIdea", "Jigaishiro",
                         "Wagahaiwanekodearu", "Reading", "Strike", "Defend"],
}


class C:
    __slots__ = ("n", "eth", "exh", "blank")

    def __init__(self, n, eth=False, exh=False, blank=None):
        self.n = n; self.eth = eth; self.exh = exh; self.blank = blank  # blank: 原卡名（空白替代卡）

    @property
    def key(self):
        return self.blank or self.n


def cost(c):
    if c.blank:
        e, q, _ = CARDS[c.blank]
        return (e, 0)
    e, q, _ = CARDS[c.n]
    return (e, q)


class Enemy:
    __slots__ = ("dies", "dmg", "atk", "hits", "weak", "vuln", "jigai")

    def __init__(self, dies, dmg):
        self.dies = dies; self.dmg = dmg; self.atk = 0.0; self.hits = 1
        self.weak = 0; self.vuln = 0; self.jigai = 0


class Combat:
    def __init__(self, deck, idx, rng, perturb=None, stats=None):
        self.rng = rng
        act, kind = RUN[idx]
        self.kind = kind; self.K = RUN_K[idx]
        mean, shape, imp, ndist, base = ENC[kind]
        self.length = max(2, round(rng.gammavariate(shape, mean / shape)))
        n = rng.choices([a for a, _ in ndist], [w for _, w in ndist])[0]
        dmg = base * ACT_SCALE[act] / (1 + 0.35 * (n - 1))
        self.enemies = [Enemy(max(1, round(self.length * (i + 1) / n)), dmg) for i in range(n)]
        self.perturb = perturb or {}
        self.st = stats
        self.draw = [C(x) if not x.startswith("BLANK:") else C("Blank", blank=x[6:]) for x in deck]
        rng.shuffle(self.draw)
        self.hand, self.disc, self.exhp = [], [], []
        self.deck_has_brainwash = any(c.n == "Brainwash" for c in self.draw)
        self.q = 0; self.e = 0; self.block = 0; self.majoka = 0; self.kokoro = False
        self.boc = 0; self.mtp = 0; self.mtp_spent = 0; self.cat = False
        self.extra = 0; self.turn = 0; self.next_draw = 0
        self.dealt = 0.0; self.hp_lost = 0.0; self.heal = 0.0
        self.brainwash_played = False; self.reached100 = None
        self.incoming = 0.0

    # ---------- 基本工具
    def S(self, k, v=1.0):
        if self.st is not None:
            self.st[k] += v

    def alive(self):
        return [e for e in self.enemies if e.dies >= self.turn]

    def deal(self, d, e, powered=True):
        if e is None:
            return
        if powered:
            d *= majoka_mult(self.majoka)
            if e.vuln > 0:
                d *= 1.5
        self.dealt += int(d)
        self.S("dmg_from_cards", int(d))

    def gain_block(self, b):
        self.block += b

    def add_status(self, name, pile):
        cards = [C(name), C(name, eth=True, exh=True)]  # 體弱多病複製（虛無+消耗）
        for c in cards:
            if pile == "discard":
                self.disc.append(c)
            else:
                self.draw.insert(self.rng.randrange(len(self.draw) + 1), c)

    def draw_n(self, k):
        for _ in range(k):
            if not self.draw:
                if not self.disc:
                    return
                self.draw = self.disc; self.disc = []
                self.rng.shuffle(self.draw)
            c = self.draw.pop()
            self.hand.append(c)
            self.S("drawn:" + c.key)
            if c.n == "Dazed":
                self.heal += 1; self.S("sickly_heal")
                self.draw_n(1)

    def spend_q(self, q):
        if q <= 0:
            return
        self.q -= q
        self.gain_block(3 * q)  # Clipboard
        self.S("q_spent", q)

    def spend_e(self, e):
        if e <= 0:
            return
        self.e -= e
        if self.mtp:
            before = self.mtp_spent // 3
            self.mtp_spent += e
            g = (self.mtp_spent // 3 - before) * self.mtp
            if g:
                self.q += g; self.S("q_gain_mtp", g)

    def gain_q(self, g, src):
        self.q += g
        self.S("q_gain_" + src, g)

    # ---------- 出牌
    def incoming_now(self):
        tot = 0.0
        for e in self.alive():
            if e.dies == self.turn:
                continue
            if e.atk > 0:
                per = max(0.0, e.atk / e.hits - e.jigai) * (0.75 if e.weak else 1.0)
                tot += per * e.hits * (0.75 if self.cat else 1.0)
        return tot

    def priority(self, c, reserve):
        n = c.key
        e, q = cost(c)
        if c.n == "Dazed":
            return None
        if e is not None and e > self.e:
            return None
        if q > self.q:
            return None
        if reserve and q > 0 and c.n != "Brainwash":
            return None
        need = self.incoming_now() - self.block
        if n == "Brainwash":
            return 20
        if n in POWERS:
            return 10
        if n == "NovelCreation":
            return 9.5
        if n in ("Trauma", "KokoroSuicide"):
            return 9
        if n == "GeniusIdea":
            costly = sum(1 for x in self.hand if x is not c and (cost(x)[0] or 0) > 0 and x.n != "Dazed")
            return 8.5 if costly > self.e or self.e == 0 and costly else 0.5
        if n == "Nandato":
            return 8
        if n == "Nanigaiitai":
            return 7.8
        if n == "Instigate":
            return 7.5 if self.draw or self.disc else None
        if n == "Jigaishiro":
            return 7.2
        if n == "Reading":
            return 7
        if n in ("Flash", "Endurance", "PaperMaking"):
            return 6
        if n == "Defend":
            return 6.5 if need > 0 else 2
        if n == "Strike":
            return 5
        return 1

    def play_turn(self):
        self.block = 0
        self.turn_start()
        guard = 0
        while guard < 60:
            guard += 1
            reserve = any(x.n == "Brainwash" for x in self.hand) and not self.brainwash_played \
                and self.q < self.brainwash_cost()
            best, bp = None, -1
            for c in self.hand:
                p = self.priority(c, reserve)
                if p is not None and p > bp:
                    best, bp = c, p
            if best is None:
                break
            self.hand.remove(best)
            e, q = cost(best)
            if best.n == "Brainwash":
                q = self.brainwash_cost()
            self.spend_e(e or 0)
            self.spend_q(q)
            self.S("played:" + best.key)
            self.resolve(best, auto=False)
        # 回合結束：虛無消耗，其餘棄掉
        for c in self.hand:
            if c.eth or c.n == "Dazed":
                self.exhp.append(c)
            else:
                self.disc.append(c)
        self.hand = []

    def brainwash_cost(self):
        cap = self.perturb.get("bw_cap", 10)
        return max(0, 20 - min(cap, int(self.majoka // 10)))

    def after_play_pile(self, c):
        if c.key in POWERS:
            return
        if c.n in EXHAUST or c.exh:
            self.exhp.append(c)
        else:
            self.disc.append(c)

    def resolve(self, c, auto):
        n = c.n
        al = self.alive()
        tgt = max(al, key=lambda x: x.atk) if al else None
        if n == "Blank":
            pass
        elif n == "Strike":
            self.deal(6, al[0] if al else None)
        elif n == "Defend":
            self.gain_block(5)
        elif n == "Trauma":
            self.add_majoka(10)
        elif n == "Flash":
            self.gain_q(2, "flash")
        elif n == "Reading":
            self.gain_q(1, "reading"); self.draw_n(1)
        elif n == "Endurance":
            self.gain_q(4, "endurance"); self.add_status("Dazed", "discard")
        elif n == "PaperMaking":
            junk = [x for x in self.hand if x.n in ("Dazed", "Trauma", "Strike", "Defend", "Blank")]
            if junk:
                pick = min(junk, key=lambda x: ["Dazed", "Trauma", "Blank", "Strike", "Defend"].index(x.n))
                self.hand.remove(pick); self.exhp.append(pick); self.S("paper_exhaust:" + pick.n)
            self.gain_q(2, "paper")
        elif n == "Bocchan":
            self.boc += 2
        elif n == "MovableTypePrinting":
            self.mtp += 1
        elif n == "Wagahaiwanekodearu":
            self.cat = True
        elif n == "Kokoro":
            self.kokoro = True
            for _ in range(5):
                self.draw.insert(self.rng.randrange(len(self.draw) + 1), C("KokoroSuicide"))
        elif n == "KokoroSuicide":
            self.draw_n(1)
            if not self.kokoro:
                self.hp_lost += 3
            self.add_majoka(20)
        elif n == "GeniusIdea":
            self.e += 2; self.S("genius_energy", 2)
        elif n == "Nandato":
            before = len(self.hand)
            self.draw_n(2)
            new = self.hand[before:]
            tc = sum((cost(x)[0] or 0) for x in new if x.n != "Dazed")
            self.gain_block(5 * tc); self.S("nandato_block", 5 * tc)
        elif n == "Nanigaiitai":
            for e in al:
                self.deal(8, e)
            for e in al:
                e.weak = max(e.weak, 1); e.vuln = max(e.vuln, 1)
        elif n == "NovelCreation":
            self.gain_block(5); self.e += 3; self.S("novel_energy", 3)
        elif n == "Jigaishiro":
            if tgt is not None:
                tgt.jigai += 5
                self.S("jigai_on_attacker", 1 if tgt.atk > 0 and tgt.dies > self.turn else 0)
        elif n == "Instigate":
            self.instigate()
        elif n == "Brainwash":
            self.brainwash_played = True
            self.extra += 1
            self.S("bw_turn", self.turn); self.S("bw_majoka", self.majoka)
        self.after_play_pile(c)

    def instigate(self):
        if not self.draw:
            if not self.disc:
                return
            self.draw = self.disc; self.disc = []
            self.rng.shuffle(self.draw)
        top = self.draw.pop()
        self.S("inst_auto:" + top.key)
        if top.n == "Dazed":
            self.exhp.append(top) if top.exh else self.disc.append(top)
            return
        # 自動打出：不花能量與言靈，也不觸發 Clipboard 格檔
        if top.n == "Brainwash":
            self.brainwash_played = True; self.extra += 1
            return
        self.resolve(top, auto=True)

    def add_majoka(self, m):
        self.majoka += m
        if self.majoka >= 100 and self.reached100 is None:
            self.reached100 = self.turn
            self.S("majoka100", 1); self.S("majoka100_turn", self.turn)
            if not self.deck_has_brainwash:
                self.deck_has_brainwash = True
                self.hand.append(C("Brainwash")); self.S("bw_granted", 1)

    def turn_start(self):
        self.q += self.K + self.boc
        if self.turn == 1:
            self.q += self.perturb.get("q_t1", 0)
        if self.turn == 3:
            self.q += self.perturb.get("q_t3", 0)
        self.e = 3 + (self.perturb.get("e_t1", 0) if self.turn == 1 else 0)
        self.S("q_bank_start", self.q); self.S("turns")
        self.draw_n(5 + self.next_draw)
        self.next_draw = 0

    def enemy_intents(self):
        for e in self.enemies:
            e.jigai = 0
            if self.rng.random() < 0.65:
                e.atk = e.dmg * self.rng.uniform(0.7, 1.3)
                e.hits = 1 if self.rng.random() < 0.7 else 2
            else:
                e.atk = 0.0

    def enemy_turn(self):
        for e in self.alive():
            if e.dies == self.turn:
                continue
            if e.jigai:
                self.dealt += e.jigai; self.S("jigai_dmg", e.jigai)
            if e.atk > 0:
                per = max(0.0, e.atk / e.hits - e.jigai) * (0.75 if e.weak else 1.0)
                saved_j = min(e.atk / e.hits, e.jigai) * e.hits
                self.S("jigai_str_prevented_raw", saved_j)
                for _ in range(e.hits):
                    if self.cat and self.rng.random() < 0.25:
                        self.S("cat_dodged", per)
                        continue
                    b = min(self.block, per)
                    self.block -= b
                    self.S("block_used", b)
                    self.hp_lost += per - b
            e.weak = max(0, e.weak - 1); e.vuln = max(0, e.vuln - 1)
        self.S("block_wasted", self.block)

    def run(self):
        for t in range(1, self.length + 1):
            self.turn = t
            self.enemy_intents()
            self.play_turn()
            while self.extra > 0:
                self.extra -= 1
                self.S("extra_turns")
                d0, h0 = self.dealt, self.hp_lost
                self.play_turn()
                self.S("extra_turn_dmg", self.dealt - d0)
            if t < self.length:
                self.enemy_turn()
        self.S("q_left_end", self.q)
        self.S("len", self.length)
        return self.dealt / DMG_PER_E - self.hp_lost / HP_PER_E + self.heal * HEAL_E


def run(deck, n, seed, perturb=None):
    """回傳 {encounter: stats}；第 i 場使用固定種子（共同隨機數）。"""
    by = {k: defaultdict(float) for k in ENC}
    for i in range(n):
        rng = random.Random(seed * 1_000_003 + i)
        idx = rng.randrange(len(RUN))
        kind = RUN[idx][1]
        st = by[kind]
        cb = Combat(deck, idx, rng, perturb, st)
        v = cb.run()
        st["value"] += v
        st["fights"] += 1
        st["dealt"] += cb.dealt; st["hp_lost"] += cb.hp_lost
    return by


def agg(by, key, per="fights"):
    num = den = 0.0
    for k, s in by.items():
        if s["fights"] == 0:
            continue
        w = IMP[k]
        num += w * s.get(key, 0.0); den += w * s.get(per, 0.0)
    return num / den if den else 0.0
