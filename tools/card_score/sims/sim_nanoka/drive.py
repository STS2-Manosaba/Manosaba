import json, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sim import run, ARCH, ENC, IMP

N = int(sys.argv[1]) if len(sys.argv) > 1 else 30000
NP = int(sys.argv[2]) if len(sys.argv) > 2 else 10000


def agg(by, key, per="fights", weighted=True):
    num = den = 0.0
    for k, s in by.items():
        if s["fights"] == 0: continue
        w = IMP[k] if weighted else 1.0
        num += w * s.get(key, 0.0); den += w * s.get(per, 0.0)
    return num / den if den else 0.0


def summary(by):
    r = {"len_by": {k: round(by[k].get("len", 0) / max(1, by[k]["fights"]), 2) for k in ENC}}
    for key in ["len", "dead", "hp_lost", "dmg", "summon_n", "redirects", "guard_dmg", "guard_revive", "ribbon_heal",
                "majoka_end", "m100_n", "majoka_summon", "majoka_intercept", "majoka_trauma", "bullets_used",
                "bullet_waste", "gun_attacks", "gun_stuck_turns", "acc_applied", "acc_consumed", "acc_extra_dmg",
                "mi_guard_dmg", "mi_guard_kill", "dmg_majoka_extra", "play_HallucinationMagic"]:
        r[key] = round(agg(by, key), 3)
    r["summon_turn"] = round(agg(by, "summon_turn", "summon_n"), 2)
    r["m100_turn"] = round(agg(by, "m100_turn", "m100_n"), 2)
    r["guard_alive_frac"] = round(agg(by, "guard_alive_turns", "turns"), 3)
    r["guard_hp_avg_alive"] = round(agg(by, "guard_hp_sum", "guard_alive_turns"), 2)
    r["bullets_start_avg"] = round(agg(by, "bullets_start", "turns"), 2)
    r["gun_stuck_turn_frac"] = round(agg(by, "gun_stuck_turns", "turns"), 3)
    r["majoka_avg_turn"] = round(agg(by, "majoka_sum", "turns"), 1)
    r["acc_consume_ratio"] = round(agg(by, "acc_consumed") / max(1e-9, agg(by, "acc_applied")), 3)
    r["acc_shot_base"] = round(agg(by, "acc_shot_base") / max(1e-9, agg(by, "acc_consumed")) * 100, 2)
    return r


out = {"baseline": {}, "baseline_lv3": {}, "probe": {}}
for a, deck in ARCH.items():
    out["baseline"][a] = summary(run(deck, N, 1, 1))
    out["baseline_lv3"][a] = summary(run(deck, N // 2, 1, 3))
    print(a, json.dumps(out["baseline"][a], ensure_ascii=False))

PROBES = ["Aim", "SorryIMissed", "WarningShot", "TakeCover", "SteadyShot", "Snipe", "TrapSetting",
          "SurppressiveShooting", "DarkSight", "BouncingShot", "MysteriousJungler", "JailerRegeneration",
          "EnvironmentExplore", "Reload", "Camping", "MyTurnToProtect", "BurningWound", "ActingCool",
          "WardenAttack", "Trauma", "GunShot", "RelicSearch", "ThreeSixtyNoScope", "NanokaSearch"]
KEYS = ["probe_plays", "probe_turn", "wa_dmg", "ac_acc", "jr_heal", "mttp_double", "steady_extra", "burn_dmg",
        "burn_applied", "darksight_saved", "ee_guns", "acc_extra_dmg", "acc_consumed", "acc_applied",
        "gun_stuck_turns", "bullets_used", "len", "hp_lost", "m100_n", "dmg", "guard_alive_turns", "turns",
        "redirects", "majoka_end", "play_Defend"]
for p in PROBES:
    out["probe"][p] = {}
    for a, deck in ARCH.items():
        by = run(list(deck) + [p], NP, 2, 1, probe=p)
        r = {k: round(agg(by, k), 3) for k in KEYS}
        r["avg_turn"] = round(r["probe_turn"] / max(1e-9, r["probe_plays"]), 2)
        r["acc_consume_ratio"] = round(r["acc_consumed"] / max(1e-9, r["acc_applied"]), 3)
        r["guard_alive_frac"] = round(r["guard_alive_turns"] / max(1e-9, r["turns"]), 3)
        out["probe"][p][a] = r
    print(p, json.dumps(out["probe"][p], ensure_ascii=False))
json.dump(out, open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "results.json"), "w", encoding="utf-8"),
          ensure_ascii=False, indent=1)
