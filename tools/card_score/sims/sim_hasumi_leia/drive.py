import json, sys
from multiprocessing import Pool
from sim import run, ARCH, ENC, IMP

N = int(sys.argv[1]) if len(sys.argv) > 1 else 20000


def agg(by, key, per="fights", weighted=True):
    num = den = 0.0
    for k, s in by.items():
        if s["fights"] == 0:
            continue
        w = IMP[k] if weighted else 1.0
        num += w * s.get(key, 0.0); den += w * s.get(per, 0.0)
    return num / den if den else 0.0


KEYS = ["reflect", "mastery_reflect", "spears", "spear_dmg", "spear_plays", "forges", "smart_pull", "pfs_bonus",
        "first_spear_turn", "spear_fights", "fa_auto", "fa_manual", "fa_dmg", "branch_extra", "ss_extra",
        "sword_plays", "smooth_draw", "sake_energy", "sdm_k", "sdm_dmg", "sb_dmg", "sb_k", "gate_n", "gate_dmg",
        "reborn_turn_dmg", "crown_prevent_raw", "crown_block_before", "gaze_red", "gaze_unc", "shadow_extra",
        "showoff_str", "com_parts", "linear_gen_playable", "linear_gen_dead", "probe_plays", "probe_turn",
        "taken", "len", "deaths"]


def summarize(by):
    r = {k: round(agg(by, k), 3) for k in KEYS if agg(by, k)}
    r["death"] = round(agg(by, "deaths"), 4)
    r["len_by"] = {k: round(by[k]["len"] / max(1, by[k]["fights"]), 2) for k in ENC}
    r["taken_by"] = {k: round(by[k]["taken"] / max(1, by[k]["fights"]), 1) for k in ENC}
    return r


PROBES = ["Projection", "AdvancedProjection", "FinalAttack", "TreeBranch", "PortableFletchingStation",
          "SmartSpear", "SmartSpear+", "SpearMastery", "GateOfBabylon", "UnlimitedSpearWork", "UnlimitedSpearWork+",
          "IAmReborn", "Pride", "Crowning", "GazeGuiding", "RapierMastery", "RapierMastery+", "SmoothMove",
          "SmoothMove+", "StrongSake", "StardustMirage", "Linear", "Comedian", "CeremonialSword", "DefensiveStance",
          "ShowOff", "RealisticActing", "Strike", "Defend"]


def job(args):
    a, p, extra = args
    deck = list(ARCH[a]) + [p.rstrip("+")] + extra
    return a, p, summarize(run(deck, N, 2, probe=p))


if __name__ == "__main__":
    out = {"baseline": {}, "probe": {}, "level": {}, "starburst": {}}
    jobs = [(a, p, []) for p in PROBES for a in ARCH]
    # 星爆氣流：需二刀流 -> 配儀禮之劍 / 樹枝 / 劍類遺物
    sbj = [(a, "StarburstStream", ["CeremonialSword"]) for a in ARCH] + \
          [(a, "StarburstStream", ["TreeBranch"]) for a in ARCH]
    with Pool() as pool:
        res = pool.map(job, jobs + sbj)
    for a, p, r in res[:len(jobs)]:
        out["probe"].setdefault(p, {})[a] = r
    for (a, p, ex), (_, _, r) in zip(sbj, res[len(jobs):]):
        out["starburst"][f"{a}+{ex[0]}"] = r
    for a, deck in ARCH.items():
        out["baseline"][a] = summarize(run(deck, N, 1))
        out["level"][a] = {lv: summarize(run(deck, N // 2, 3, level=lv)) for lv in (1, 3, 5)}
    out["starburst"]["relic"] = {a: summarize(run(list(d) + ["StarburstStream"], N, 4, probe="StarburstStream",
                                                   second_sword=True)) for a, d in ARCH.items()}
    json.dump(out, open("results.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(json.dumps(out["baseline"], ensure_ascii=False, indent=1))
