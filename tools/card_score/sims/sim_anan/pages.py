"""古神頁卡子模擬：在第 tb 回合開始前以理智 X 把牌堆換成古神牌組（24 張），量測頁卡打出次數、
克圖格亞自動打出次數、阿撒托斯勝利率。召喚書本身是 X 費，不在此評分。"""
import json, random, sys
from collections import defaultdict
from sim import Combat, C, ARCH, RUN, ENC, IMP, majoka_mult, agg, CARDS

PAGE = {  # energy
    "PageAzathoth": 1, "PageNyarlathotep": 1, "PageShubNiggurath": 1, "PageYogSothoth": 3,
    "PageHastur": 2, "PageCthugha": 3, "PageCthulhu": 2,
}
DECK = (["PageAzathoth"] * 2 + ["PageNyarlathotep"] * 6 + ["PageShubNiggurath"] * 4 + ["PageYogSothoth"] * 2
        + ["PageHastur"] * 4 + ["PageCthugha"] * 2 + ["PageCthulhu"] * 4)


CARDS.update({p: (e, 0, "S") for p, e in PAGE.items()})


class PageCombat(Combat):
    def __init__(self, deck, idx, rng, tb, X, stats):
        super().__init__(deck, idx, rng, None, stats)
        self.tb = tb; self.X = X; self.sanity = None; self.str = 0; self.cthulhu = False
        self.cth_ex = 0; self.won = False; self.dmg_taken_turn = 0.0; self.shadow = {}

    def spend_e(self, e):
        super().spend_e(e)
        if self.sanity is not None and e > 0:
            self.sanity -= e

    def deal(self, d, e, powered=True):
        if self.sanity is not None and e is not None:
            d = (d + self.str) * (1 + 0.5 * self.shadow.get(id(e), 0))
        super().deal(d, e, powered)

    def priority(self, c, reserve):
        if c.n not in PAGE:
            return super().priority(c, reserve)
        e = PAGE[c.n]
        if e > self.e or (self.sanity - e) <= 0:
            return None
        al = self.alive()
        inc = self.incoming_now()
        return {"PageAzathoth": 9, "PageCthulhu": 8.5, "PageYogSothoth": 8 if inc >= 12 else 1,
                "PageCthugha": 7.5, "PageNyarlathotep": 7, "PageHastur": 7.4 if len(al) >= 2 else 6.5,
                "PageShubNiggurath": 6.8 if inc - self.block > 6 else 4}[c.n]

    def resolve(self, c, auto):
        n = c.n
        if n not in PAGE:
            return super().resolve(c, auto)
        al = self.alive()
        if n == "PageAzathoth":
            self.draw_n(2)
        elif n == "PageNyarlathotep":
            if al:
                t = al[-1]
                self.deal(12, t); self.shadow[id(t)] = self.shadow.get(id(t), 0) + 1
        elif n == "PageShubNiggurath":
            self.heal += 5; self.S("page_heal", 5); self.gain_block(10)
        elif n == "PageYogSothoth":
            for e in al:
                if e.atk > 0 and e.dies > self.turn:
                    self.S("yog_stunned_dmg", e.atk)
                e.atk = 0
        elif n == "PageHastur":
            for e in al:
                for _ in range(5):
                    self.deal(4, e)
        elif n == "PageCthugha":
            if al:
                self.deal(25, al[0])
        elif n == "PageCthulhu":
            self.cthulhu = True
        if not auto:
            self.exhp.append(c)

    def turn_start(self):
        if self.turn == self.tb and self.sanity is None:
            self.sanity = self.X
            self.draw = [C(x) for x in DECK]; self.rng.shuffle(self.draw); self.hand = []; self.disc = []
            self.S("book_fights")
        if self.sanity is not None:
            self.shadow = {}
            if self.cthulhu:
                self.str += 1
            for c in list(self.exhp):
                if c.n == "PageCthugha":
                    al = self.alive()
                    if al:
                        self.deal(25, al[0]); self.S("cthugha_auto")
        super().turn_start()

    def play_turn(self):
        super().play_turn()
        if self.sanity is not None and not self.won:
            types = {c.n for c in self.exhp if c.n in PAGE}
            if len(types) == 7:
                self.won = True; self.S("aza_win"); self.S("aza_win_turn", self.turn)
                # 強制勝利：剩餘回合敵人不再攻擊（以剩餘回合數記錄）
                self.S("aza_turns_saved", self.length - self.turn)

    def enemy_turn(self):
        if self.won:
            return
        if self.cthulhu:
            before = self.hp_lost
            super().enemy_turn()
            taken = self.hp_lost - before
            if taken > 15:
                self.S("cthulhu_cap_saved", taken - 15); self.hp_lost = before + 15
        else:
            super().enemy_turn()

    def run(self):
        for t in range(1, self.length + 1):
            self.turn = t
            self.enemy_intents()
            self.play_turn()
            if self.won:
                break
            if t < self.length:
                self.enemy_turn()
        self.S("len", self.length)
        return 0.0


if __name__ == "__main__":
    N = int(sys.argv[1]) if len(sys.argv) > 1 else 20000
    out = {}
    for tb, X in [(3, 8), (3, 14), (4, 20), (2, 6)]:
        by = {k: defaultdict(float) for k in ENC}
        for i in range(N):
            rng = random.Random(7_000_003 + i)
            idx = rng.randrange(len(RUN))
            st = by[RUN[idx][1]]
            cb = PageCombat(ARCH["engine"], idx, rng, tb, X, st)
            if cb.length < tb:
                st["fights"] += 1; st["too_short"] += 1
                continue
            cb.run(); st["fights"] += 1
        r = {"p_fight_reaches_tb": round(1 - agg(by, "too_short"), 3)}
        bf = max(1e-9, agg(by, "book_fights"))
        for p in PAGE:
            r["plays/" + p] = round(agg(by, "played:" + p) / bf, 3)
        for k in ["cthugha_auto", "aza_win", "aza_win_turn", "aza_turns_saved", "yog_stunned_dmg",
                  "cthulhu_cap_saved", "page_heal"]:
            r[k] = round(agg(by, k) / bf, 3)
        r["aza_win_turn"] = round(agg(by, "aza_win_turn") / max(1e-9, agg(by, "aza_win")), 2)
        r["aza_turns_saved"] = round(agg(by, "aza_turns_saved") / max(1e-9, agg(by, "aza_win")), 2)
        out[f"tb{tb}_X{X}"] = r
        print(tb, X, r, flush=True)
    json.dump(out, open("pages_results.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
