"""驅動：基準統計 + 共同隨機數邊際測試（卡換成同費空白卡，或 +1 言靈/+1 能量擾動）。"""
import json, sys
from multiprocessing import Pool
from sim import run, agg, ARCH, ENC, RUN_K, IMP

N = int(sys.argv[1]) if len(sys.argv) > 1 else 20000
SEED = 11


def job(args):
    arch, variant, deck, perturb = args
    by = run(deck, N, SEED, perturb)
    return arch, variant, {k: dict(v) for k, v in by.items()}


def tasks():
    out = []
    for a, deck in ARCH.items():
        out.append((a, "base", deck, None))
        out.append((a, "+1q_t1", deck, {"q_t1": 1}))
        out.append((a, "+1q_t3", deck, {"q_t3": 1}))
        out.append((a, "+1e_t1", deck, {"e_t1": 1}))
        for c in sorted(set(deck)):
            i = deck.index(c)
            d2 = list(deck); d2[i] = "BLANK:" + c
            out.append((a, "blank:" + c, d2, None))
        if a == "kokoro":
            out.append((a, "bw_cap15", deck, {"bw_cap": 15}))
            d3 = [x for x in deck if x != "Brainwash"]
            out.append((a, "no_bw_in_deck", d3, None))
            d4 = list(d3); d4[d4.index("Kokoro")] = "BLANK:Kokoro"
            out.append((a, "no_bw_blank:Kokoro", d4, None))
    for a in ("engine", "balanced"):
        d = ARCH[a] + ["Kokoro"]
        out.append((a, "add:Kokoro", d, None))
        out.append((a, "add:BLANK:Kokoro", ARCH[a] + ["BLANK:Kokoro"], None))
        d = ARCH[a] + ["Brainwash"]
        out.append((a, "add:Brainwash", d, None))
        out.append((a, "add:BLANK:Brainwash", ARCH[a] + ["BLANK:Brainwash"], None))
    return out


if __name__ == "__main__":
    res = {}
    with Pool() as p:
        for a, v, by in p.imap_unordered(job, tasks()):
            res.setdefault(a, {})[v] = by
            print(a, v, round(agg(by, "value"), 3), flush=True)
    json.dump({"N": N, "run_k": RUN_K, "raw": res}, open("raw.json", "w", encoding="utf-8"))
