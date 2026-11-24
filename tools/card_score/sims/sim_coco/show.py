import json, sys
r = json.load(open(sys.argv[1] if len(sys.argv) > 1 else "result.json", encoding="utf-8"))
for a in ("live", "hide", "equip"):
    print("=====", a, "out/fight", round(r[a]["out_per_fight"], 2))
    for k, (mean, perfight) in r[a]["stats"].items():
        print(f"  {k:45s} mean={mean:8.3f} perFight={perfight:8.3f}")
print("===== marginal")
for k, v in r["marginal"].items():
    print(f"  {k:40s} {v}")
