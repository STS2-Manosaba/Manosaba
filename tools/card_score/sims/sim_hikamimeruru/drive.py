import sys, json, time
sys.path.insert(0, ".")
from sim import *

ALL = set(COST)
PROBES = ["Trauma", "PotionSelect", "AddFirewood", "CollectPotions", "AutomatedPotionMix", "PotionFactory",
          "PotionThrow", "MeruruInfirmary", "Exaid", "PrimalPotionPower", "WitchesCauldron", "QuickHeal"]
N = int(sys.argv[1]) if len(sys.argv) > 1 else 1500
out = {}
t0 = time.time()
for arch, deck in ARCH.items():
    for tag, upg in (("base", frozenset()), ("upg", frozenset(ALL))):
        by = run_set(deck, N, 11, upg)
        r = {}
        for c in PROBES:
            if c in deck:
                r[c] = {"per_play": round(wavg(by, "cr_" + c, "play_" + c), 3), "per_play_global": round(sum(s["cr_" + c] for s in by.values()) / max(1, sum(s["play_" + c] for s in by.values())), 3), "gen_global_per_play": round(sum(s["gen_" + c] for s in by.values()) / max(1, sum(s["play_" + c] for s in by.values())), 3), "ovf_global_per_play": round(sum(s["overflow_" + c] for s in by.values()) / max(1, sum(s["play_" + c] for s in by.values())), 3),
                        "plays_per_fight": round(wavg(by, "play_" + c), 3),
                        "gen_per_fight": round(wavg(by, "gen_" + c), 3),
                        "overflow_per_fight": round(wavg(by, "overflow_" + c), 3)}
        for key in ("deaths", "len", "dmg_taken", "cat_procs", "craft_pair", "craft_cat", "pot_use", "pot_discard",
                    "throw_dmg", "throw_disc_nom", "pressure_discard", "slots", "pots_end", "heal_pot",
                    "primal_rocks", "primal_lost_nom", "cr_relic", "inf_pots_LPK", "inf_pots_PK", "inf_pots_GPK"):
            r[key] = round(wavg(by, key), 3)
        r["len_by"] = {k: round(s["len"] / max(1, s["fights"]), 2) for k, s in by.items()}
        out[f"{arch}_{tag}"] = r
        print(arch, tag, round(time.time() - t0, 1), flush=True)
# 奈葉香拼圖：首領戰邊際
for tag, upg in (("base", frozenset()), ("upg", frozenset({"NanokaPuzzle"}))):
    for with_n in (False, True):
        deck = ARCH["heal"] + (["NanokaPuzzle"] if with_n else [])
        rng = random.Random(5); s = defaultdict(float)
        for _ in range(N * 4):
            Combat(deck, "boss", rng, s, [], 7, 70.0, upg).run()
        out[f"nanoka_boss_{tag}_{with_n}"] = {k: round(s[k] / s["fights"], 3) for k in ("deaths", "len", "dmg_taken", "nanoka_win", "nanoka_played")}
    for with_n in (False, True):
        deck = ARCH["heal"] + (["NanokaPuzzle"] if with_n else [])
        rng = random.Random(6); s = defaultdict(float)
        for _ in range(N * 4):
            Combat(deck, "elite", rng, s, [], 7, 70.0, upg).run()
        out[f"nanoka_elite_{tag}_{with_n}"] = {k: round(s[k] / s["fights"], 3) for k in ("deaths", "len", "dmg_taken", "nanoka_win", "nanoka_played")}
json.dump(out, open("result.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(json.dumps(out, ensure_ascii=False, indent=1))

