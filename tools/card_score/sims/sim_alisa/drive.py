import json, os, sys
from multiprocessing import Pool
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sim import run, ARCH, ENC, IMP

NB = int(sys.argv[1]) if len(sys.argv) > 1 else 30000
NP = int(sys.argv[2]) if len(sys.argv) > 2 else 8000


def agg(by, key, per="fights"):
    num = den = 0.0
    for k, s in by.items():
        if s.get("fights", 0) == 0:
            continue
        num += IMP[k] * s.get(key, 0.0); den += IMP[k] * s.get(per, 0.0)
    return num / den if den else 0.0


def summarize(by):
    keys = set()
    for s in by.values():
        keys |= set(s)
    r = {k: round(agg(by, k), 4) for k in sorted(keys)}
    r["len_by"] = {k: round(by[k]["len"] / max(1, by[k]["fights"]), 2) for k in ENC}
    r["death_by"] = {k: round(by[k]["deaths"] / max(1, by[k]["fights"]), 3) for k in ENC}
    r["burn_per_stack"] = round(agg(by, "burn_dmg", "burn_applied"), 3)
    r["burn_per_stack_by"] = {k: round(by[k]["burn_dmg"] / max(1, by[k]["burn_applied"]), 2) for k in ENC}
    srcs = {k.split(":", 1)[1] for k in keys if k.startswith("bapp:")}
    r["burn_per_stack_src"] = {s: round(agg(by, "bdmg:" + s, "bapp:" + s), 3) for s in srcs}
    r["orb_absorb_per_gain"] = round(agg(by, "orb_absorbed", "orb_gained"), 3)
    r["lv4heal_per_turn_orb"] = round(agg(by, "lv4_heal", "orb_turn_end"), 3)
    return r


PROBES = ["DisasterGirl", "DisasterGirl+", "FinalCountdown", "RestrictionRelease", "RestrictionRelease+",
          "ScaldingTouch", "ScaldingTouch+", "SpreadBurn", "SpreadBurn+", "ThisIsFine", "ThisIsFine+", "Scorching",
          "Scorching+", "EmberSpark", "EmberSpark+", "SnakeFire", "SnakeFire+", "Lighter", "CounterViolence",
          "FireOnFire", "Flaming", "Flaming+", "LoveFlame", "MagicianRed", "YouAreWeakEma", "BurnCap", "FireSword",
          "FireJudgementCourt", "SweepUp"]


def job(args):
    kind, name, a = args
    deck = ARCH[a]
    if kind == "base":
        return kind, name, a, summarize(run(deck, NB, 1))
    p = name.rstrip("+")
    d = list(deck) + [name]
    # 探針名稱若與牌組內同名卡重疊，統計會包含原有複本；以 per-play 比率為主
    return kind, name, a, summarize(run(d, NP, 2, probe=p))


if __name__ == "__main__":
    jobs = [("base", "base", a) for a in ARCH] + [("probe", p, a) for p in PROBES for a in ARCH]
    out = {"baseline": {}, "probe": {}}
    with Pool(max(1, os.cpu_count() - 1)) as pool:
        for kind, name, a, r in pool.imap_unordered(job, jobs):
            if kind == "base":
                out["baseline"][a] = r
            else:
                out["probe"].setdefault(name, {})[a] = r
            print(kind, name, a, flush=True)
    json.dump(out, open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "results.json"), "w",
                        encoding="utf-8"), ensure_ascii=False, indent=1)
