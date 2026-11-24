"""參數不確定性蒙地卡羅與敏感度分析。

對 weights.json 的不確定權重、turn_model 的遭遇參數，以及每張卡依信心等級的乘法估值雜訊抽樣，
每組重跑完整評分流程（card_score.evaluate），統計卡牌與卡池相對強度的區間、敏感度與角色排名穩定度。

用法：python tools/card_score/uncertainty.py [-n 300] [--seed 20261123]
輸出：tools/card_score/output/uncertainty.json（build_report.py 會一併嵌入 HTML 報告）
"""
from __future__ import annotations

import argparse
import copy
import json
import math
import random
import statistics
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import card_score as cs  # noqa: E402
import turn_model  # noqa: E402

# (參數 id, 顯示名稱, 下限, 上限)；均勻分佈
WEIGHT_PARAMS = [
    ("powers.Majoka", "魔女化 Majoka 每層", 0.05, 0.10),
    ("effects.Heal", "回復 Heal 每點", 0.18, 0.32),
    ("powers.Sus", "嫌疑 Sus 每層", -0.08, -0.02),
    ("powers.Clue", "疑點 Clue 每層", 0.22, 0.38),
    ("powers.Burn", "燃燒 Burn 每層", 0.22, 0.32),
    ("effects.custom:PaintOrb", "顏料球 PaintOrb", 0.85, 1.20),
    ("keyword_fixed.Exhaust", "消耗係數 Exhaust", 0.80, 0.90),
    ("mode_weights.solo", "單人權重（多人 = 1 − 單人）", 0.50, 0.70),
]
# turn_model：以預設值為中心 ±25%
TURN_PARAMS = [
    ("turn.hallway.mean", "一般戰平均回合", "hallway", 1),
    ("turn.elite.mean", "菁英戰平均回合", "elite", 1),
    ("turn.boss.mean", "首領戰平均回合", "boss", 1),
    ("turn.elite.imp", "菁英戰重要性權重", "elite", 3),
    ("turn.boss.imp", "首領戰重要性權重", "boss", 3),
]
TURN_SPREAD = 0.25
TURN_N = 20_000          # 每組樣本的回合模擬次數（固定種子＝共同隨機數）
NOISE_SIGMA = {"agent-high": 0.10, "agent-medium": 0.20, "agent-low": 0.35,
               "auto": 0.05, "partial": 0.05, "manual-set": 0.05}
NOISE_PARAM = "noise.pool"
NOISE_LABEL = "本池卡牌估值雜訊"
MODES = {"score": "relative", "solo": "rel_solo", "multi": "rel_multi"}


def q(xs: list[float], p: float) -> float:
    s = sorted(xs)
    if len(s) == 1:
        return s[0]
    k = (len(s) - 1) * p
    f = math.floor(k)
    return s[f] + (s[min(f + 1, len(s) - 1)] - s[f]) * (k - f)


def q3(xs):
    return [round(q(xs, 0.1), 3), round(q(xs, 0.5), 3), round(q(xs, 0.9), 3)] if xs else None


def ranks(xs: list[float]) -> list[float]:
    order = sorted(range(len(xs)), key=lambda i: xs[i])
    r = [0.0] * len(xs)
    i = 0
    while i < len(order):
        j = i
        while j + 1 < len(order) and xs[order[j + 1]] == xs[order[i]]:
            j += 1
        for k in range(i, j + 1):
            r[order[k]] = (i + j) / 2
        i = j + 1
    return r


def spearman(a: list[float], b: list[float]) -> float:
    ra, rb = ranks(a), ranks(b)
    ma, mb = statistics.mean(ra), statistics.mean(rb)
    num = sum((x - ma) * (y - mb) for x, y in zip(ra, rb))
    den = math.sqrt(sum((x - ma) ** 2 for x in ra) * sum((y - mb) ** 2 for y in rb))
    return num / den if den else 0.0


def set_path(w: dict, path: str, v: float):
    sec, key = path.split(".", 1)
    w.setdefault(sec, {})[key] = v


def pool_key(c) -> str:
    return f"{c.source}|{c.pool}"


def pool_stats(cards, low_ok: bool) -> dict[str, dict[str, float]]:
    """每個卡池三種模式的相對強度中位數（不含 Ancient、已停用卡池）；low_ok=False 時排除 agent-low。"""
    groups: dict[str, dict[str, list]] = {}
    for c in cards:
        if c.pool == "Deprecated" or c.bucket == "Ancient" or c.relative is None:
            continue
        if not low_ok and c.confidence == "agent-low":
            continue
        g = groups.setdefault(pool_key(c), {m: [] for m in MODES})
        for m, attr in MODES.items():
            v = getattr(c, "_" + attr)
            if v is not None:
                g[m].append(v)
    return {k: {m: statistics.median(v) for m, v in g.items() if v} for k, g in groups.items()}


def attach_rel(res: dict):
    """補上 rel_solo／rel_multi（與 data.json 相同定義）。"""
    base = res["baseline"]
    for c in res["orig"] + res["mod"]:
        b = base.get(c.bucket)
        med = statistics.median(b) if b else None
        c._relative = c.relative
        c._rel_solo = c.score_solo / med if med and c.score_solo is not None and c.relative is not None else None
        c._rel_multi = c.score_multi / med if med and c.score_multi is not None and c.relative is not None else None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("-n", type=int, default=300)
    ap.add_argument("--seed", type=int, default=20261123)
    ap.add_argument("--out", type=Path, default=HERE / "output" / "uncertainty.json")
    args = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")
    t0 = time.time()
    w0 = json.loads((HERE / "weights.json").read_text(encoding="utf-8"))
    inp = cs.load_inputs()
    enc0 = copy.deepcopy(turn_model.ENCOUNTERS)
    n0 = turn_model.N

    # 預設參數跑一次：取得每張卡的信心等級（決定雜訊大小）
    base_res = cs.evaluate(w0, inp)
    conf = {(c.source, c.pool, c.name): c.confidence for c in base_res["orig"] + base_res["all_mod"]}
    sigma = {k: NOISE_SIGMA[v] for k, v in conf.items() if v in NOISE_SIGMA}
    card_pool = {k: f"{k[0]}|{k[1]}" for k in sigma}

    rng = random.Random(args.seed)
    params: dict[str, list[float]] = {p[0]: [] for p in WEIGHT_PARAMS + TURN_PARAMS}
    pool_noise: dict[str, list[float]] = {}
    card_vals: dict[str, dict[str, list]] = {}
    pools_all: dict[str, dict[str, list]] = {}
    pools_nolow: dict[str, dict[str, list]] = {}
    turn_model.N = TURN_N
    try:
        for it in range(args.n):
            w = copy.deepcopy(w0)
            for pid, _, lo, hi in WEIGHT_PARAMS:
                v = rng.uniform(lo, hi)
                params[pid].append(v)
                if pid == "mode_weights.solo":
                    w["mode_weights"] = {"solo": v, "multi": 1 - v}
                else:
                    set_path(w, pid, v)
            enc = copy.deepcopy(enc0)
            for pid, _, kind, idx in TURN_PARAMS:
                m = rng.uniform(1 - TURN_SPREAD, 1 + TURN_SPREAD)
                params[pid].append(enc0[kind][idx] * m)
                e = list(enc[kind]); e[idx] = enc0[kind][idx] * m; enc[kind] = tuple(e)
            turn_model.ENCOUNTERS.clear(); turn_model.ENCOUNTERS.update(enc)
            turn_model._CACHE.clear()
            # 卡牌雜訊：每卡獨立的對數常態
            logs = {k: rng.gauss(0, s) for k, s in sigma.items()}
            cs.CARD_NOISE.clear()
            cs.CARD_NOISE.update({k: math.exp(z) for k, z in logs.items()})
            per_pool: dict[str, list] = {}
            for k, z in logs.items():
                per_pool.setdefault(card_pool[k], []).append(z)
            res = cs.evaluate(w, inp)
            attach_rel(res)
            cards = res["orig"] + res["mod"]
            for c in cards:
                if c.relative is None:
                    continue
                d = card_vals.setdefault(f"{c.source}|{c.pool}|{c.name}", {m: [] for m in MODES})
                for m, attr in MODES.items():
                    v = getattr(c, "_" + attr)
                    if v is not None:
                        d[m].append(v)
            for store, low_ok in ((pools_all, True), (pools_nolow, False)):
                for pk, st in pool_stats(cards, low_ok).items():
                    d = store.setdefault(pk, {m: [None] * args.n for m in MODES})
                    for m, v in st.items():
                        d[m][it] = v
            for pk in pools_all:
                zs = per_pool.get(pk)
                pool_noise.setdefault(pk, [0.0] * args.n)[it] = statistics.median(zs) if zs else 0.0
            if (it + 1) % 25 == 0:
                print(f"{it + 1}/{args.n}　{time.time() - t0:.0f}s", flush=True)
    finally:
        turn_model.ENCOUNTERS.clear(); turn_model.ENCOUNTERS.update(enc0)
        turn_model._CACHE.clear(); turn_model.N = n0
        cs.CARD_NOISE.clear()

    labels = {p[0]: p[1] for p in WEIGHT_PARAMS + TURN_PARAMS}
    labels[NOISE_PARAM] = NOISE_LABEL
    out_pools = {}
    for pk, d in pools_all.items():
        y = d["score"]
        idx = [i for i, v in enumerate(y) if v is not None]
        ys = [y[i] for i in idx]
        sens = []
        for pid, xs in list(params.items()) + [(NOISE_PARAM, pool_noise.get(pk, [0.0] * args.n))]:
            xv = [xs[i] for i in idx]
            rho = spearman(xv, ys) if len(set(xv)) > 1 and len(set(ys)) > 1 else 0.0
            sens.append({"param": pid, "label": labels[pid], "rho": round(rho, 3)})
        sens.sort(key=lambda s: -abs(s["rho"]))
        nl = pools_nolow.get(pk, {})
        out_pools[pk] = {
            **{m: q3([v for v in d[m] if v is not None]) for m in MODES},
            "nolow": {m: q3([v for v in nl.get(m, []) if v is not None]) for m in MODES},
            "sensitivity": sens,
            "top3": sens[:3],
        }
    # 角色排名穩定度：模組角色（不含共用）依合併中位數排名
    chars = [pk for pk in pools_all if pk.startswith("mod|") and pk != "mod|Common"]
    weak4 = {pk: 0 for pk in chars}
    strong3 = {pk: 0 for pk in chars}
    rank_s = {pk: [] for pk in chars}
    valid = 0
    for it in range(args.n):
        vals = [(pools_all[pk]["score"][it], pk) for pk in chars]
        if any(v is None for v, _ in vals):
            continue
        valid += 1
        order = [pk for _, pk in sorted(vals, reverse=True)]
        for r, pk in enumerate(order):
            rank_s[pk].append(r + 1)
        for pk in order[:3]:
            strong3[pk] += 1
        for pk in order[-4:]:
            weak4[pk] += 1
    ranking = {pk: {"p_weak4": round(weak4[pk] / valid, 3), "p_strong3": round(strong3[pk] / valid, 3),
                    "rank_p50": q(rank_s[pk], 0.5), "rank_p10": q(rank_s[pk], 0.1), "rank_p90": q(rank_s[pk], 0.9)}
               for pk in chars} if valid else {}

    out = {
        "n": args.n, "seed": args.seed, "seconds": round(time.time() - t0, 1),
        "params": [{"id": p[0], "label": p[1], "lo": p[2], "hi": p[3], "dist": "uniform"} for p in WEIGHT_PARAMS]
                  + [{"id": p[0], "label": p[1], "lo": round(enc0[p[2]][p[3]] * (1 - TURN_SPREAD), 3),
                      "hi": round(enc0[p[2]][p[3]] * (1 + TURN_SPREAD), 3), "dist": "uniform"} for p in TURN_PARAMS],
        "noise_sigma": NOISE_SIGMA,
        "rho_noise_floor": round(1.96 / math.sqrt(max(args.n - 1, 1)), 3),
        "notes": "均勻抽樣權重與回合模型參數；每張卡另乘以 exp(N(0, σ))，σ 依信心等級，按卡獨立抽樣。"
                 "pool_overrides（例如個別角色的 Majoka）維持固定。排名只比較模組角色（不含共用）。",
        "pools": out_pools,
        "ranking": ranking,
        "cards": {k: {m: q3(v[m]) for m in MODES if v[m]} for k, v in card_vals.items()},
    }
    args.out.write_text(json.dumps(out, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
    print(f"{args.out}（{args.n} 組，{out['seconds']}s）")


if __name__ == "__main__":
    main()
