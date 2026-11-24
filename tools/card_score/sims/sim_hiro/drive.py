import json, sys
from sim import run, ARCH, ENC, IMP

N = int(sys.argv[1]) if len(sys.argv) > 1 else 20000


def agg(by, key, per="fights", weighted=True):
    num = den = 0.0
    for k, s in by.items():
        if s["fights"] == 0: continue
        w = IMP[k] if weighted else 1.0
        num += w * s[key]; den += w * s[per]
    return num / den if den else 0.0


KEYS = ["turns", "deaths", "hp_lost", "dmg_in", "ema_absorb", "ema_deaths", "ema_revive", "summon", "ema_dmg",
        "ema_end", "ema_alive_end", "badend", "badend_dmg", "pen_bonus", "pen_hits", "majoka_end", "majoka100",
        "flux_energy", "stance_switch", "life_block", "dogatk_dmg", "musou_plays", "musou_dmg", "labours_majoka",
        "mi_ema_dmg", "deathloop_save", "deathloop_heal", "str_end", "sus_turn_sum", "sus_turns"]


def summ(by):
    r = {k: round(agg(by, k), 3) for k in KEYS}
    r["len_by"] = {k: round(by[k]["turns"] / max(1, by[k]["fights"]), 2) for k in ENC}
    r["pen_per_hit"] = round(agg(by, "pen_bonus") / max(1e-9, agg(by, "pen_hits")), 3)
    r["avg_sus_eot"] = round(agg(by, "sus_turn_sum") / max(1e-9, agg(by, "turns")), 2)
    return r


def main():
    out = {"baseline": {}, "probe": {}}
    for a, deck in ARCH.items():
        out["baseline"][a] = summ(run(deck, N, 1))

    PROBES = ["Intimidate", "WellSleep", "BathroomTalk", "Perjury", "Ronpa", "Accomplice", "LifeSentence",
              "EmaDogRush", "EmaDogBite", "EmaDogAttack", "Flux", "MusouKen", "Labours", "WitchesSabbath",
              "DeathLoop", "SeveringSpin", "StrikeTen", "StrikeJin", "StrikeChi", "Ema", "Trauma", "GoodSleep"]
    for p in PROBES:
        out["probe"][p] = {}
        for upg in (False, True):
            for a, deck in ARCH.items():
                d = list(deck) + [p]
                if p == "WitchesSabbath":
                    d = d + ["Labours"]
                by = run(d, N, 2, probe=p, upg=upg)
                pl = max(1e-9, agg(by, "probe_plays"))
                r = {"plays": round(agg(by, "probe_plays"), 3)}
                for k in ["probe_dmg", "probe_res", "probe_sus", "probe_ema", "probe_majoka", "probe_turn"]:
                    r["avg_" + k[6:]] = round(agg(by, k) / pl, 2)
                r.update({k: v for k, v in summ(by).items() if k in (
                    "hp_lost", "turns", "life_block", "dogatk_dmg", "flux_energy", "musou_dmg", "musou_plays",
                    "labours_majoka", "majoka100", "mi_ema_dmg", "deathloop_save", "deathloop_heal", "badend",
                    "ema_absorb", "pen_per_hit", "deaths", "str_end", "ema_deaths")})
                out["probe"][p][("U:" if upg else "") + a] = r
    json.dump(out, open("results.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(json.dumps(out["baseline"], ensure_ascii=False, indent=1))


if __name__ == '__main__':
    main()
