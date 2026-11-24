import json, sys
import sim
from sim import run, ARCH, ENC, IMP

N = int(sys.argv[1]) if len(sys.argv) > 1 else 20000


def agg(by, key, per="fights", weighted=True):
    num = den = 0.0
    for k, s in by.items():
        if s["fights"] == 0: continue
        w = IMP[k] if weighted else 1.0
        num += w * s.get(key, 0); den += w * s.get(per, 0)
    return num / den if den else 0.0


KEYS = ["len", "dmg_taken", "deaths", "coll_end", "majoka_end", "match_heal", "match_heal_eff", "match_procs",
        "margo_fetch", "margo_miss", "meruru_has_target", "meruru_fetch_score", "sherry_extra", "sherry_boulder",
        "boulder_dmg", "rm_converted", "rm_status_conv", "rm_majoka_lost", "sbr_turn", "sbr_active_turns",
        "sky_layers", "sky_plays", "sky_given", "sky_prevent", "hs_uses", "hs_prevent_real", "dice_bonus_used",
        "ice_coll", "gn_heal", "gn_coll", "gn_energy_lost", "nanoka_k", "ema_dmg", "es_status", "thrifty_status",
        "noah_exh_status", "noah_exh_good", "hanna_str", "probe_turn",
        "meruru_fetch_AnAnPuppet", "meruru_fetch_HannaPuppet", "meruru_fetch_Boulder", "meruru_fetch_GoodnightYoungLady"]


def summarize(by, base=None):
    r = {"plays": round(agg(by, "probe_plays"), 3)}
    for k in KEYS:
        v = agg(by, k)
        if v: r[k] = round(v, 3)
    if base:
        r["d_len"] = round(r.get("len", 0) - base["len"], 3)
        r["d_taken"] = round(r.get("dmg_taken", 0) - base["dmg_taken"], 3)
    return r


PROBES = ["RefuseMajo", "RefuseMajo+", "SherryPuppet", "SherryPuppet+", "MeruruPuppet", "MeruruPuppet+",
          "MargoPuppet", "MargoPuppet+", "LittleMatchGirl", "LittleMatchGirl+", "HighSense", "HighSense+",
          "SteelBallRun", "SteelBallRun+", "IceStrike", "IceStrike+", "GoodnightYoungLady", "GoodnightYoungLady+",
          "EmaPuppet", "CocoPuppet", "NanokaPuppet", "EmergencySuture", "Thrifty", "HannaPuppet", "NoahPuppet",
          "Strike"]
if __name__ == "__main__":
    out = {"baseline": {}, "probe": {}, "extra": {}}
    base = {}
    for a, deck in ARCH.items():
        base[a] = summarize(run(deck, N, 1))
        out["baseline"][a] = base[a]
    for p in PROBES:
        out["probe"][p] = {}
        for a, deck in ARCH.items():
            out["probe"][p][a] = summarize(run(list(deck) + [p.rstrip("+")], N, 1, probe=p), base[a])
        print(p, json.dumps(out["probe"][p], ensure_ascii=False))
    # 空島：已在牌組（前一場鋼球後）＋鋼球
    for a, deck in ARCH.items():
        out["extra"]["SkyIsland_withSBR_" + a] = summarize(run(list(deck) + ["SteelBallRun", "SkyIsland"], N, 1, probe="SkyIsland"), base[a])
        out["extra"]["SkyIsland_alone_" + a] = summarize(run(list(deck) + ["SkyIsland"], N, 1, probe="SkyIsland"), base[a])
        out["extra"]["SherryHanna_" + a] = summarize(run(list(deck) + ["SherryPuppet", "HannaPuppet"], N, 1, probe="SherryPuppet"), base[a])
        out["extra"]["MargoMeruru_" + a] = summarize(run(list(deck) + ["MargoPuppet", "MeruruPuppet"], N, 1, probe="MargoPuppet"), base[a])
    sim.FAN_COUNTS_HANNA_POWER = True
    for a, deck in ARCH.items():
        out["extra"]["HighSense_fanHanna_" + a] = summarize(run(list(deck) + ["HighSense"], N, 1, probe="HighSense"), base[a])
    sim.FAN_COUNTS_HANNA_POWER = False
    json.dump(out, open("results.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(json.dumps(out["baseline"], ensure_ascii=False, indent=1))
    print(json.dumps(out["extra"], ensure_ascii=False, indent=1))
