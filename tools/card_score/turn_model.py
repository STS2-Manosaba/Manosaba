"""戰鬥回合數模型：以簡單模擬求「持續型效果（每回合觸發的能力）」的期望剩餘回合數。

舊估值多以固定約 4.5 回合計算持續能力。實際戰鬥長度依遭遇類型差異大（菁英、首領常超過 10 回合），
這裡模擬戰鬥長度與打出時機，求期望剩餘回合數，換算成縮放係數 = E[剩餘回合] / 舊假設回合數。
"""
from __future__ import annotations

import random

# 遭遇類型：(出現比例, 平均回合數, 形狀參數, 重要性權重)；長度 ~ Gamma(k, mean/k)，至少 2 回合
# 重要性：菁英、首領戰失血多、決定一局成敗，持續型卡的價值也主要在這些長戰兌現
ENCOUNTERS = {
    "hallway": (0.72, 4.0, 4.0, 1.0),   # 一般戰鬥
    "elite":   (0.20, 6.5, 4.0, 2.0),   # 菁英
    "boss":    (0.08, 10.0, 5.0, 3.0),  # 首領（常 8~15 回合）
}
DECK_SIZE = 20     # 平均牌組大小
HAND = 5           # 每回合抽牌
OLD_TURNS = 4.5    # 舊估值假設的剩餘回合數
N = 200_000


def simulate(innate: bool = False, seed: int = 7) -> dict:
    rng = random.Random(seed)
    first_cycle = max(1, round(DECK_SIZE / HAND))
    kinds = list(ENCOUNTERS)
    weights = [ENCOUNTERS[k][0] for k in kinds]
    tot = wsum = raw = 0.0
    by = {k: [0.0, 0] for k in kinds}
    long10 = 0
    for _ in range(N):
        k = rng.choices(kinds, weights)[0]
        _, mean, shape, imp = ENCOUNTERS[k]
        length = max(2, round(rng.gammavariate(shape, mean / shape)))
        t = 1 if innate else rng.randint(1, first_cycle)   # 第一輪抽到時打出
        rem = max(0, length - t + 1)
        raw += rem
        tot += rem * imp
        wsum += imp
        by[k][0] += rem
        by[k][1] += 1
        long10 += length > 10
    exp = tot / wsum
    return {"expected_turns": round(exp, 2), "unweighted_turns": round(raw / N, 2), "factor": round(exp / OLD_TURNS, 3),
            "by_encounter": {k: round(v[0] / max(v[1], 1), 2) for k, v in by.items()},
            "p_fight_over_10_turns": round(long10 / N, 3)}


_CACHE: dict = {}


def factors() -> dict:
    if not _CACHE:
        _CACHE["normal"] = simulate(False)
        _CACHE["innate"] = simulate(True)
    return _CACHE


if __name__ == "__main__":
    import json
    print(json.dumps(factors(), ensure_ascii=False, indent=2))
