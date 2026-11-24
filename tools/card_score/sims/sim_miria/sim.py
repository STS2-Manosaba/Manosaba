"""佐伯米莉亞 簡化戰鬥蒙地卡羅模擬。
規則依 Manosaba/Characters/SaekiMiria 原始碼；敵人為抽象化（turn_model 戰鬥長度、排程死亡、攻擊意圖）。
量測各卡打出時的資源狀態與觸發次數，價值換算在 write_v5.py 手算。
用法: python sim.py [每原型局數]
"""
from __future__ import annotations
import random, json, sys
from collections import defaultdict

ENC = {  # 平均回合, gamma 形狀, 重要性, 敵人數分佈, 每敵攻擊基礎傷害
    "hallway": (4.0, 4.0, 1.0, [(1, .35), (2, .40), (3, .25)], 7.0),
    "elite":   (6.5, 4.0, 2.0, [(1, .6), (2, .3), (3, .1)], 13.0),
    "boss":    (10.0, 5.0, 3.0, [(1, .8), (2, .2)], 17.0),
}
ACT_SCALE = [1.0, 1.35, 1.7]
RUN = []
for act in range(3):
    seq = ["hallway"] * 10
    seq.insert(4, "elite"); seq.insert(9, "elite")
    seq.append("boss")
    RUN += [(act, k) for k in seq]
EXP_GAIN = {"hallway": 1, "elite": 2, "boss": 3}
LV_TABLE = [0, 2, 7, 15, 30]  # CabinetKey MaxRelicLevel=5
MOVIES = ["Horror", "Comedy", "Fantasy", "Action", "Romantic", "Spy"]


def relic_level(exp):
    return sum(1 for t in LV_TABLE if exp >= t)


def majoka_mult(s):
    if s <= 0:
        return 1.0
    p = s ** 1.8365
    return 1 + 3 * p / (p + 166.6 ** 1.8365)


# name: (cost, type)
CARDS = {
    "Strike": (1, "A"), "Defend": (1, "S"), "MindShard": (1, "S"), "Trauma": (0, "S"),
    "MovieNoise": (1, "P"), "MovieViewing": (1, "S"), "AbsoluteCinema": (1, "S"), "CabinetExpansion": (2, "P"),
    "EvidenceLooting": (1, "S"), "UncleWriting": (0, "S"), "CutterKnife": (1, "A"), "Sabotage": (0, "A"),
    "Clumsy": (0, "S"), "Objection": (1, "S"), "HeartStopper": (0, "P"), "WalkingMisfortune": (1, "P"),
    "Makeup": (1, "P"), "NarehateFeeding": (0, "S"), "LuckTransfer": (1, "S"), "Slimification": (2, "P"),
    "PrisonMeal": (0, "S"), "PepperRamen": (1, "S"), "BodySlam": (1, "A"), "TraumaSharing": (1, "S"),
    "SNSExchange": (1, "S"), "HitTweet": (3, "P"), "MindOverload": (5, "S"), "BalancedNutrition": (2, "P"),
    "Peacemaker": (2, "P"), "Friendly": (1, "P"), "LawKnower": (1, "P"), "MemorySharing": (1, "P"),
    "FreeAttack": (0, "A"),  # 心之碎片取得的他角色攻擊牌（本回合 0 費，約 9 傷）
    "MemCard": (1, "S"),     # 記憶共享取得的牌（虛無消耗，約 1 能量價值，簡化為 1 費 6 傷或 5 格）
    "Blank": (99, "X"),
    "Movie": (0, "M"), "Cassette": (0, "M"), "SmallPaper": (0, "M"),
}

ARCH = {
    "movie": dict(
        deck=["Strike"] * 4 + ["Defend"] * 4 + ["MindShard", "Trauma", "MovieNoise", "MovieViewing", "MovieViewing",
              "AbsoluteCinema", "CabinetExpansion", "EvidenceLooting", "UncleWriting", "CutterKnife", "Sabotage",
              "Clumsy", "Objection", "HeartStopper", "Strike"],
        prio=["UncleWriting", "HeartStopper", "MovieNoise", "CabinetExpansion", "AbsoluteCinema", "MovieViewing",
              "EvidenceLooting", "Sabotage", "Trauma", "MindShard", "FreeAttack", "CutterKnife", "Clumsy",
              "Objection", "Strike", "Defend"]),
    "misfortune": dict(
        deck=["Strike"] * 4 + ["Defend"] * 4 + ["MindShard", "Trauma", "WalkingMisfortune", "Makeup", "NarehateFeeding",
              "NarehateFeeding", "Clumsy", "Clumsy", "LuckTransfer", "LuckTransfer", "Slimification", "PrisonMeal",
              "PepperRamen", "CutterKnife", "CutterKnife", "BodySlam", "TraumaSharing", "Sabotage", "LawKnower"],
        prio=["WalkingMisfortune", "Slimification", "LawKnower", "Makeup", "PepperRamen", "PrisonMeal", "Sabotage",
              "NarehateFeeding", "Clumsy", "LuckTransfer", "TraumaSharing", "Trauma", "MindShard", "FreeAttack",
              "BodySlam", "CutterKnife", "Strike", "Defend"]),
    "majoka": dict(
        deck=["Strike"] * 4 + ["Defend"] * 4 + ["MindShard", "Trauma", "Trauma", "PrisonMeal", "PrisonMeal",
              "SNSExchange", "SNSExchange", "HitTweet", "MovieViewing", "MindOverload", "WalkingMisfortune",
              "UncleWriting", "EvidenceLooting", "Peacemaker", "Friendly", "BalancedNutrition", "CutterKnife",
              "Strike", "Defend"],
        prio=["UncleWriting", "WalkingMisfortune", "MemorySharing", "MindOverload", "HitTweet", "Friendly",
              "Peacemaker", "BalancedNutrition", "SNSExchange", "MovieViewing", "EvidenceLooting", "Trauma",
              "PrisonMeal", "MindShard", "FreeAttack", "MemCard", "CutterKnife", "Strike", "Defend"]),
}


class Card:
    __slots__ = ("name", "kind", "up", "replay", "free", "ac")
    def __init__(self, name, kind=None, up=False):
        self.name = name; self.kind = kind; self.up = up; self.replay = 0; self.free = False; self.ac = False


class Enemy:
    __slots__ = ("alive", "dies_at", "dmg", "weak", "vuln", "unlucky", "poison", "doom", "strgain", "str")
    def __init__(self, dies_at, dmg, strgain):
        self.alive = True; self.dies_at = dies_at; self.dmg = dmg
        self.weak = self.vuln = self.unlucky = self.poison = self.doom = 0
        self.strgain = strgain; self.str = 0


class Stats:
    def __init__(self):
        self.s = defaultdict(float)  # 加權總和
        self.w = defaultdict(float)  # 加權次數

    def add(self, key, val, imp):
        self.s[key] += val * imp; self.w[key] += imp

    def mean(self, key):
        return self.s[key] / self.w[key] if self.w[key] else 0.0


class Combat:
    def __init__(self, sim, deck, kind, act, rl, rng, hp):
        self.sim = sim; self.rng = rng; self.rl = rl; self.st = sim.st
        mean, shape, imp, ndist, base = ENC[kind]
        self.imp = imp; self.kind = kind
        self.length = max(2, round(rng.gammavariate(shape, mean / shape)))
        n = rng.choices([a for a, _ in ndist], [w for _, w in ndist])[0]
        dmg = base * ACT_SCALE[act] / (1 + 0.35 * (n - 1))
        # 約 30% 敵人會成長力量（每 2 回合 +2~3）
        self.enemies = [Enemy(max(1, round(self.length * (i + 1) / n)), dmg,
                              rng.choice([2, 3]) if rng.random() < 0.3 else 0) for i in range(n)]
        self.draw = [Card(c) for c in deck]; rng.shuffle(self.draw)
        if "MemorySharing" in sim.deck_extra:
            self.draw.append(Card("MemorySharing"))  # 固有
        self.hand = []; self.discard = []
        self.hp = hp; self.block = 0; self.str = 0; self.dex = 0; self.majoka = 0; self.weak = 0
        self.poison = self.burn = self.heal = self.doom = self.unlucky = self.plating = 0
        self.energy = 0; self.maxe = 3; self.turn = 0
        self.p = defaultdict(int)  # powers
        self.ac_pending = 0  # 絕對電影：下一張生成的電影 +重播
        self.ht_bonus = 0     # 熱門推文提供的魔女化
        self.lost = 0; self.healed = 0
        self.tag = {}  # 打出後追蹤的計數器: key -> [累計值]
        self.reached100 = False

    # ---------------- 工具
    def alive(self):
        return [e for e in self.enemies if e.alive]

    def target(self):
        a = self.alive(); return a[0] if a else None

    def track(self, key, v):
        for k in list(self.tag):
            if k.startswith(key + "|"):
                self.tag[k] += v

    def hit(self, base, target, powered=True):
        if target is None:
            return 0
        d = base + (self.str if powered else 0)
        if powered:
            m = majoka_mult(self.majoka)
            if self.ht_bonus:
                self.track("HitTweet", max(0, d) * (m - majoka_mult(self.majoka - self.ht_bonus)))
            d *= m
            if self.weak > 0:
                d *= 0.75
        if target.vuln > 0:
            d *= 1.5
        d = max(0, d)
        self.sim.dealt += d * self.imp
        return d

    def gain_block(self, b, powered=True):
        self.block += max(0, b + (self.dex if powered else 0))

    def gain_power(self, amt):
        # 法律通（已棄用）：自身能力層數增加 → 等量格檔
        if amt > 0 and self.p["LawKnower"]:
            self.block += amt * self.p["LawKnower"]
            self.track("LawKnower", amt)

    def add_majoka(self, n):
        self.majoka += n; self.gain_power(n)
        if self.majoka >= 100 and not self.reached100:
            self.reached100 = True
            self.st.add("reach100_turn", self.turn, self.imp)
            self.sim.got_memory = True

    def lose_hp(self, d):
        if d <= 0:
            return
        self.hp -= d; self.lost += d
        if self.p["Slimification"]:
            self.heal += self.p["Slimification"]; self.gain_power(self.p["Slimification"])
            self.track("Slimification", self.p["Slimification"])

    def draw_n(self, n):
        for _ in range(n):
            if len(self.hand) >= 10:
                return
            if not self.draw:
                if not self.discard:
                    return
                self.draw = self.discard; self.discard = []; self.rng.shuffle(self.draw)
            c = self.draw.pop()
            self.hand.append(c)

    def make_movie(self, up):
        if self.rng.randrange(100) == 0:
            c = Card("SmallPaper", up=up)
        else:
            c = Card("Movie", self.rng.choice(MOVIES), up)
        if self.ac_pending:
            c.replay = self.ac_pending; c.ac = True; self.ac_pending = 0
        return c

    # ---------------- 卡牌效果
    def movie_effect(self, c):
        up = c.up
        if c.name == "Cassette":
            self.draw_n(1); self.hit(10, self.target()); return
        if c.name == "SmallPaper":
            self.draw_n(1)
            r = self.rng.randrange(10)
            if r == 0:
                self.sim.wayout += self.imp
            return
        k = c.kind
        self.draw_n(2 + up if k == "Fantasy" else 1)
        if k == "Horror":
            self.add_majoka(15 if up else 10)
        elif k == "Comedy":
            self.energy += 2 if up else 1
        elif k == "Action":
            self.str += 2 if up else 1; self.gain_power(2 if up else 1)
        elif k == "Romantic":
            self.heal += 2 if up else 1; self.gain_power(2 if up else 1)
        elif k == "Spy":
            self.dex += 2 if up else 1; self.gain_power(2 if up else 1)

    def play_movie(self, c):
        times = 1 + c.replay
        for _ in range(times):
            self.movie_effect(c)
            self.sim.movie_execs += self.imp
            self.track("movie", 1)
            if self.p["MovieNoise"]:
                t = self.rng.choice(self.alive()) if self.alive() else None
                if t:
                    self.hit(5, t, powered=False)
                    self.track("MovieNoise", 1)
        if c.ac:
            self.st.add("AC_replays_played", c.replay, self.imp)
            self.st.add("AC_kind_" + (c.kind or c.name), 1, self.imp)

    def start_track(self, name):
        key = f"{name}|{id(object())}"
        self.tag[key] = 0.0
        self.sim.open_tags.append((key, self))
        return key

    def play(self, c):
        n = c.name; up = False; st = self.st; imp = self.imp
        if n in ("Movie", "Cassette", "SmallPaper"):
            self.play_movie(c); return
        if n == "Strike":
            self.hit(6, self.target())
        elif n == "Defend":
            self.gain_block(5)
        elif n == "MindShard":
            fa = Card("FreeAttack"); fa.free = True; self.hand.append(fa)
        elif n == "FreeAttack":
            self.hit(9, self.target())
        elif n == "MemCard":
            self.hit(6, self.target()) if self.rng.random() < 0.5 else self.gain_block(5)
        elif n == "Trauma":
            self.add_majoka(10)
        elif n == "MovieNoise":
            self.p["MovieNoise"] += 1; self.tag[self.start_track("MovieNoise")] = 0
        elif n == "MovieViewing":
            for _ in range(2):
                self.hand.append(self.make_movie(False))
        elif n == "AbsoluteCinema":
            self.ac_pending = 2; st.add("AC_played", 1, imp)
        elif n == "CabinetExpansion":
            self.p["CabinetExpansion"] += 1; self.start_track("CabExp_movie")
        elif n == "EvidenceLooting":
            self.draw_n(3)
        elif n == "UncleWriting":
            self.energy += 1; self.draw_n(2)
        elif n == "CutterKnife":
            st.add("Cutter_heal", self.heal, imp); self.hit(6 + self.heal, self.target())
        elif n == "Sabotage":
            t = self.target(); self.hit(4, t); t.vuln += 1; t.weak += 1
        elif n == "Clumsy":
            self.gain_block(8); self.weak += 2
        elif n == "Objection":
            self.gain_block(8)
        elif n == "HeartStopper":
            self.p["HeartStopper"] += 1; self.burn += 2; self.gain_power(1)
            self.start_track("HS_burn")
        elif n == "WalkingMisfortune":
            self.p["WM"] += 1; self.maxe += 1; self.unlucky += 2; self.start_track("WM_energy_used")
            st.add("WM_turn", self.turn, imp)
        elif n == "Makeup":
            self.plating += 7; self.unlucky += 2; self.gain_power(7)
            self.start_track("Makeup_plating")
        elif n == "NarehateFeeding":
            self.gain_block(9); self.doom += 6
        elif n == "LuckTransfer":
            t = self.target()
            st.add("LT_unlucky", self.unlucky, imp); st.add("LT_weak", self.weak, imp)
            st.add("LT_doom", self.doom, imp); st.add("LT_poison", self.poison, imp)
            st.add("LT_burn", self.burn, imp); st.add("LT_any", 1 if (self.unlucky + self.weak + self.doom + self.poison + self.burn) else 0, imp)
            st.add("LT_remaining", self.length - self.turn + 1, imp)
            t.unlucky += 4 * self.unlucky; t.weak += 4 * self.weak; t.doom += 4 * self.doom
            t.poison += 4 * self.poison
            self.sim.lt_unlucky_dmg_key = self.start_track("LT_dmg")
        elif n == "Slimification":
            self.p["Slimification"] += 1; self.start_track("Slimification")
            st.add("Slim_turn", self.turn, imp)
        elif n == "PrisonMeal":
            self.heal += 3; self.poison += 3; self.add_majoka(5); self.gain_power(3)
        elif n == "PepperRamen":
            self.heal += 3; self.poison += 1; self.gain_power(3)
            st.add("Pepper_remaining", self.length - self.turn + 1, imp)
        elif n == "BodySlam":
            t = self.target(); self.hit(self.block, t); t.vuln += 1
        elif n == "TraumaSharing":
            t = self.target(); others = [e for e in self.alive() if e is not t]
            st.add("TS_others", len(others), imp)
            st.add("TS_tdebuff", (t.vuln > 0) + (t.weak > 0) + (t.unlucky > 0) + (t.poison > 0) + (t.doom > 0), imp)
            st.add("TS_useful", 1 if (others and (t.vuln or t.weak or t.unlucky or t.poison or t.doom)) else 0, imp)
            for e in others:
                e.vuln += t.vuln; e.weak += t.weak; e.unlucky += t.unlucky; e.poison += t.poison; e.doom += t.doom
        elif n == "SNSExchange":
            self.add_majoka(10); self.draw_n(2)
        elif n == "HitTweet":
            add = max(0, 100 - self.majoka)
            st.add("HT_majoka_before", self.majoka, imp); st.add("HT_add", add, imp)
            st.add("HT_remaining", self.length - self.turn + 1, imp)
            self.ht_bonus += add; self.add_majoka(add); self.start_track("HitTweet")
        elif n == "MindOverload":
            st.add("MO_played", 1, imp); st.add("MO_majoka", self.majoka, imp)
            st.add("MO_remaining", self.length - self.turn + 1, imp)
        elif n == "BalancedNutrition":
            self.poison += 3; st.add("BN_played", 1, imp)
        elif n == "Peacemaker":
            self.p["Peacemaker"] += 2; self.start_track("PM_str_prevented")
        elif n == "Friendly":
            self.p["Friendly"] += 1; self.start_track("Friendly_saved")
        elif n == "LawKnower":
            self.p["LawKnower"] += 1; self.start_track("LawKnower")
        elif n == "MemorySharing":
            self.p["MemorySharing"] += 1

    # ---------------- 回合
    def player_turn(self):
        self.turn += 1; st = self.st; imp = self.imp
        self.block = 0
        # 回合開始：中毒、灼燒、回復
        if self.poison:
            self.lose_hp(self.poison); self.poison -= 1
        if self.burn:
            self.lose_hp(self.burn); self.track("HS_burn", self.burn)
        if self.heal:
            h = self.heal; self.healed += h; self.heal -= 1
        # 小鑰匙
        nmov = (2 if self.rl >= 5 else 1) + self.p["CabinetExpansion"]
        for i in range(nmov):
            c = self.make_movie(self.rl >= 4)
            self.draw.insert(self.rng.randint(0, len(self.draw)), c)
            if i >= (2 if self.rl >= 5 else 1):
                c.kind = c.kind; setattr(c, "free", True)  # 擴充產生的電影（標記）
        if self.rl >= 3 and self.rng.randrange(100) < 50:
            c = Card("Cassette")
            self.draw.insert(self.rng.randint(0, len(self.draw)), c)
        if self.p["MemorySharing"]:
            mc = Card("MemCard"); self.hand.append(mc)
        self.energy = self.maxe
        self.draw_n(5 + self.p["HeartStopper"])
        st.add("hand_movies_turn", sum(1 for c in self.hand if c.name in ("Movie", "Cassette", "SmallPaper")), imp)
        e0 = self.energy
        played_movies = 0
        # 出牌
        prio = self.sim.prio
        guard = 0
        while guard < 60:
            guard += 1
            mv = [c for c in self.hand if CARDS[c.name][1] == "M"]
            if mv:
                c = mv[0]; self.hand.remove(c)
                if c.free and c.name == "Movie":
                    self.track("CabExp_movie", 1)
                self.play(c); played_movies += 1
                if not self.alive():
                    break
                continue
            best = None; bi = 999
            for c in self.hand:
                cost = 0 if c.free else CARDS[c.name][0]
                if cost > self.energy:
                    continue
                if c.name == "HitTweet" and self.majoka >= 100:
                    continue
                if c.name in ("BodySlam",) and self.block < 8:
                    continue
                if c.name == "LuckTransfer" and (self.unlucky + self.weak + self.doom + self.poison) == 0:
                    continue
                if c.name == "TraumaSharing" and len(self.alive()) < 2:
                    continue
                i = prio.index(c.name) if c.name in prio else 500
                if i < bi:
                    bi = i; best = c
            if best is None:
                break
            cost = 0 if best.free else CARDS[best.name][0]
            if best.name == "MindOverload":
                st.add("MO_turns_castable", 1, imp)
            self.energy -= cost; self.hand.remove(best)
            self.play(best)
            if CARDS[best.name][1] != "P" and best.name not in ("FreeAttack", "MemCard", "MovieViewing", "MindOverload", "UncleWriting"):
                self.discard.append(best)
            if not self.alive():
                break
        for c in self.hand:
            if c.name == "MindOverload":
                st.add("MO_stuck_in_hand", 1, imp)
        st.add("movies_played_turn", played_movies, imp)
        st.add(f"movies_played_turn_lv{self.rl}", played_movies, imp)
        st.add("energy_left", self.energy, imp)
        if self.p["WM"]:
            used = min(1, max(0, (e0 - self.energy) - (self.maxe - 1)))
            self.track("WM_energy_used", used)
        st.add("majoka_turn", self.majoka, imp)
        # 保留：無；其餘棄置（電影/卡帶非虛無，留在棄牌堆）
        keep = []
        for c in self.hand:
            if c.name in ("FreeAttack", "MemCard"):
                continue
            keep.append(c)
        self.discard += keep; self.hand = []
        # 回合結束：鍍層、厄運自傷、末日
        if self.plating:
            self.block += self.plating; self.track("Makeup_plating", self.plating)
        if self.unlucky:
            self.lose_hp(self.unlucky)
        if self.weak:
            self.weak -= 1

    def enemy_turn(self):
        st = self.st
        for e in self.alive():
            if e.unlucky:
                self.track("LT_dmg", e.unlucky); self.sim.dealt += e.unlucky * self.imp
            if e.poison:
                self.track("LT_dmg", e.poison); e.poison -= 1
        incoming_total = 0
        for e in self.alive():
            if self.rng.random() < 0.65:
                d = e.dmg + e.str
                if e.weak > 0:
                    d *= 0.75
                if self.p["Friendly"]:
                    saved = d * (1 - 0.8 ** self.p["Friendly"]); self.track("Friendly_saved", min(saved, max(0, d - self.block)))
                    d *= 0.8 ** self.p["Friendly"]
                b = min(self.block, d); self.block -= b
                self.lose_hp(d - b)
            if e.strgain and self.turn % 2 == 0:
                g = max(0, e.strgain - self.p["Peacemaker"])
                self.track("PM_str_prevented", (e.strgain - g) * 0.65 * max(0, self.length - self.turn) / 2)
                e.str += g
            if e.weak:
                e.weak -= 1
            if e.vuln:
                e.vuln -= 1

    def run(self):
        while self.turn < self.length and self.alive():
            self.player_turn()
            for e in self.enemies:
                if e.alive and e.dies_at <= self.turn:
                    e.alive = False
            if not self.alive():
                break
            self.enemy_turn()
        self.st.add("fight_len", self.turn, self.imp)
        self.st.add("fight_lost", self.lost, self.imp)
        self.st.add("fight_healed", self.healed, self.imp)
        self.st.add("fight_reach100", 1 if self.reached100 else 0, self.imp)
        for key, cb in list(self.sim.open_tags):
            if cb is self:
                name = key.split("|")[0]
                self.st.add("T_" + name, self.tag[key], self.imp)
        self.sim.open_tags = [(k, c) for k, c in self.sim.open_tags if c is not self]


class Sim:
    def __init__(self, arch, seed, remove=None):
        self.remove = remove; self.arch = arch; self.rng = random.Random(seed); self.st = Stats()
        self.prio = ARCH[arch]["prio"]; self.dealt = 0.0; self.movie_execs = 0.0; self.wayout = 0.0
        self.open_tags = []; self.deck_extra = set(); self.got_memory = False

    def run_game(self):
        exp = 0; deck = list(ARCH[self.arch]["deck"])
        if self.remove:
            deck[deck.index(self.remove)] = "Blank"
        self.deck_extra = set(); self.got_memory = False
        hp = 70
        for act, kind in RUN:
            rl = relic_level(exp)
            c = Combat(self, deck, kind, act, rl, self.rng, hp)
            c.run()
            hp = min(80, max(30, c.hp + c.healed * 0.0 + 6))
            self.st.add("relic_lv", rl, ENC[kind][2])
            if self.got_memory:
                self.deck_extra.add("MemorySharing")
            exp += EXP_GAIN[kind]


MARGINAL = {
    "movie": ["MovieNoise", "AbsoluteCinema", "CabinetExpansion", "MovieViewing", "HeartStopper"],
    "misfortune": ["LuckTransfer", "Slimification", "PepperRamen", "WalkingMisfortune", "Makeup", "TraumaSharing", "PrisonMeal"],
    "majoka": ["HitTweet", "Friendly", "Peacemaker", "BalancedNutrition", "SNSExchange", "PrisonMeal"],
}


def main():
    n = int(sys.argv[1]) if len(sys.argv) > 1 else 2000
    out = {}
    for i, a in enumerate(ARCH):
        s = Sim(a, 1000 + i)
        for _ in range(n):
            s.run_game()
        out[a] = {k: round(s.st.mean(k), 3) for k in sorted(s.st.s)}
        fw = s.st.w["fight_len"]
        out[a].update({"rate_" + k: round(v / fw, 4) for k, v in sorted(s.st.w.items())})
        out[a]["_dealt_per_fight"] = round(s.dealt / fw, 2)
        for rem in MARGINAL.get(a, []):
            s2 = Sim(a, 1000 + i, rem)
            for _ in range(n):
                s2.run_game()
            fw2 = s2.st.w["fight_len"]
            out[a]["M_" + rem] = dict(d_dealt=round(out[a]["_dealt_per_fight"] - s2.dealt / fw2, 2),
                                      d_lost=round(out[a]["fight_lost"] - s2.st.mean("fight_lost"), 2),
                                      d_healed=round(out[a]["fight_healed"] - s2.st.mean("fight_healed"), 2))
    json.dump(out, open(__file__.replace("sim.py", "result.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    for a, d in out.items():
        print("==", a)
        for k, v in d.items():
            print(f"  {k}: {v}")


if __name__ == "__main__":
    main()
