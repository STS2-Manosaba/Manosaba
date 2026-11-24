import json, sys
from sim import run, ARCH, ENC, IMP

N = int(sys.argv[1]) if len(sys.argv) > 1 else 20000


def agg(by, key, per="fights", weighted=True):
    num = den = 0.0
    for k, s in by.items():
        if s["fights"] == 0: continue
        w = IMP[k] if weighted else 1.0
        # 依出現比例加權（run 已按比例抽樣），再乘重要性
        num += w * s[key]; den += w * s[per]
    return num / den if den else 0.0


out = {"baseline": {}, "probe": {}}
for a, deck in ARCH.items():
    by = run(deck, N, 1)
    out["baseline"][a] = {
        "len_by": {k: round(by[k]["len"] / max(1, by[k]["fights"]), 2) for k in ENC},
        "reward_turn_frac": round(agg(by, "reward_turns", "turns"), 3),
        "reward_triggers_per_fight": round(agg(by, "reward_triggers"), 2),
        "first_reward_turn": round(agg(by, "first_reward_turn_sum", "first_reward_n"), 2),
        "p_any_reward": round(agg(by, "first_reward_n"), 3),
        "str_end": round(agg(by, "str_end"), 2),
        "str_gained": round(agg(by, "str_gained"), 2),
        "inv_dmg": round(agg(by, "inv_dmg"), 2),
        "death": round(agg(by, "deaths", weighted=False), 4),
    }

PROBES = ["KyuMiraclePartners", "RokuBraveFace", "LittleFairy", "QuickWit", "QuickWit+", "SearchAnalyzeSolve",
          "SearchAnalyzeSolve+", "GiveItATry", "BerserkerSoul", "Achuba", "DirtyEhe", "DoublePunch", "DateTime",
          "FryFryFry", "BigInvestigation", "DontRush", "IceCube", "WhatDoYouMean", "MasterKey", "GoodMorningSherry",
          "SanFindsClue", "TruthIsOnlyOne"]
for p in PROBES:
    out["probe"][p] = {}
    for a, deck in ARCH.items():
        d = list(deck) + [p.rstrip("+")]
        if p == "RokuBraveFace" and "SanFindsClue" not in deck:
            pass
        by = run(d, N, 2, probe=p)
        r = {"plays_per_fight": round(agg(by, "probe_plays"), 3)}
        pl = max(1e-9, agg(by, "probe_plays"))
        for k in ["probe_reward", "probe_str", "probe_clue", "probe_turn", "probe_cost_paid"]:
            r[k.replace("probe_", "avg_")] = round(agg(by, k) / pl, 3)
        for k in ["fairy_energy", "kyu_dmg", "roku_dmg", "qw_str", "qw_dex", "qw_hp", "qw_clue", "qw_turns",
                  "qw_maxroll", "sas_uses", "sas_val", "gat_majoka100", "sp_plays", "sp_big", "sp_dmg", "bs_chain",
                  "bs_dmg", "achuba_bonus", "thorns_dmg", "dp_hits", "dp_dmg", "heal", "probe_stuck", "len"]:
            v = agg(by, k)
            if v: r[k] = round(v, 3)
        r["reward_turn_frac"] = round(agg(by, "reward_turns", "turns"), 3)
        out["probe"][p][a] = r
json.dump(out, open("results.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(json.dumps(out["baseline"], ensure_ascii=False, indent=1))
