"""整理 raw.json：邊際價值（每次打出）、擾動價值、觸發率。輸出 results.json。"""
import json
from sim import agg, ARCH

R = json.load(open("raw.json", encoding="utf-8"))["raw"]
out = {}
for a, vs in R.items():
    b = vs["base"]
    base_v = agg(b, "value")
    o = {"base_value": round(base_v, 3)}
    for k in ["turns", "q_spent", "q_left_end", "q_bank_start", "extra_turns", "majoka100", "bw_granted",
              "sickly_heal", "jigai_dmg", "jigai_str_prevented_raw", "cat_dodged", "block_used", "block_wasted",
              "hp_lost", "dealt"]:
        o[k] = round(agg(b, k), 3)
    o["q_bank_avg_turnstart"] = round(agg(b, "q_bank_start", "turns"), 2)
    gains = sum(agg(b, k) for k in set().union(*[s.keys() for s in b.values()]) if k.startswith("q_gain_"))
    o["q_income_cards"] = round(gains, 2)
    o["q_spend_frac"] = round(agg(b, "q_spent") / max(1e-9, agg(b, "q_spent") + agg(b, "q_left_end")), 3)
    for p in ["+1q_t1", "+1q_t3", "+1e_t1", "bw_cap15"]:
        if p in vs:
            o["delta" + p] = round(agg(vs[p], "value") - base_v, 3)
    cards = {}
    keys = set().union(*[s.keys() for s in b.values()])
    for v, by in vs.items():
        if not v.startswith("blank:"):
            continue
        c = v[6:]
        ncopy = ARCH[a].count(c)
        plays = agg(b, "played:" + c) / ncopy; drawn = agg(b, "drawn:" + c) / ncopy
        d = base_v - agg(by, "value")
        cards[c] = {"copies": ncopy, "drawn": round(drawn, 3), "played": round(plays, 3),
                    "play_per_draw": round(plays / max(1e-9, drawn), 3),
                    "delta_per_fight": round(d, 3), "value_per_play": round(d / max(1e-9, plays), 3)}
    o["cards"] = cards
    for k in sorted(keys):
        if k.startswith(("inst_auto:", "paper_exhaust:", "nandato", "genius", "novel", "bw_", "majoka100_turn",
                         "extra_turn_dmg", "jigai_on")):
            o.setdefault("misc", {})[k] = round(agg(b, k), 3)
    out[a] = o
    for v in vs:
        if v.startswith("add:") and not v.startswith("add:BLANK"):
            c = v[4:]
            wc = vs[v]; bl = vs["add:BLANK:" + c]
            plays = agg(wc, "played:" + c)
            out[a]["add_" + c] = {"played": round(plays, 3), "drawn": round(agg(wc, "drawn:" + c), 3),
                                  "delta_per_fight": round(agg(wc, "value") - agg(bl, "value"), 3),
                                  "value_per_play": round((agg(wc, "value") - agg(bl, "value")) / max(1e-9, plays), 3),
                                  "majoka100": round(agg(wc, "majoka100"), 3), "bw_granted": round(agg(wc, "bw_granted"), 3),
                                  "extra_turns": round(agg(wc, "extra_turns"), 3),
                                  "bw_played": round(agg(wc, "played:Brainwash"), 3)}
    if a == "kokoro":
        nb = vs["no_bw_in_deck"]; nbk = vs["no_bw_blank:Kokoro"]
        pl = agg(nb, "played:Kokoro")
        out[a]["kokoro_no_bw_deck"] = {"played": round(pl, 3), "delta": round(agg(nb, "value") - agg(nbk, "value"), 3),
                                       "value_per_play": round((agg(nb, "value") - agg(nbk, "value")) / max(1e-9, pl), 3),
                                       "majoka100": round(agg(nb, "majoka100"), 3),
                                       "bw_played": round(agg(nb, "played:Brainwash"), 3)}
json.dump(out, open("results.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(json.dumps(out, ensure_ascii=False, indent=1))
