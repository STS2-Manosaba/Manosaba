"""量測持家、特別的魔法、念寫相片的實際價值（EARLY_END=0、G_VAL=0.33）。"""
import os, sys, json
os.environ.setdefault('EARLY_END', '0'); os.environ.setdefault('G_VAL', '0.33')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ema_sim as m

N = int(sys.argv[1]) if len(sys.argv) > 1 else 20000
W = {"A_early": .3, "B_trial": .4, "C_evid": .3}
out = {}
for extra_name, extra in [("base", ()), ("psychic", ("Psychic",))]:
    agg = {}
    for a in W:
        res = m.simulate(a, N, seed=sum(map(ord, a)), extra=extra)
        tw = sum(r[4] for r in res)
        mag = [r for r in res if r[2] >= 5]
        mw = sum(r[4] for r in mag)
        st = lambda k, rows=res, ww=tw: sum((r[5][k] or 0) * r[4] for r in rows) / ww
        bp = sum(r[5]["bp_presents"] * r[4] for r in res)
        agg[a] = dict(
            bp_presents=st("bp_presents"), hk_plays=st("hk_plays"),
            bp_value=(sum(r[5]["hk_dmg"] * r[4] for r in res) / 6) / max(bp, 1e-9),
            P_special=sum((r[5]["special_turn"] is not None) * r[4] for r in mag) / mw,
            special_turn=sum((r[5]["special_turn"] or 0) * r[4] for r in mag if r[5]["special_turn"]) / max(1e-9, sum(r[4] for r in mag if r[5]["special_turn"])),
            special_turns=sum(r[5]["special_turns"] * r[4] for r in mag) / mw,
            special_val=sum(r[5]["special_val"] * r[4] for r in mag) / mw,
            P_magic_drawn=sum((r[5]["magic_drawn"] > 0) * r[4] for r in mag) / mw,
            P_psychic=sum((r[5]["psychic_play"] is not None) * r[4] for r in mag) / mw,
            mag_share=mw / tw,
        )
    agg["W"] = {k: sum(W[a] * agg[a][k] for a in W) for k in agg["A_early"]}
    out[extra_name] = agg
json.dump(out, open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'out_extra.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
for k, agg in out.items():
    print("==", k)
    for a, v in agg.items(): print(a, {x: round(y, 3) for x, y in v.items()})
