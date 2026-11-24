import json, os
R = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "results.json"), encoding="utf-8"))
B = R["baseline"]
for a, r in B.items():
    print("BASE", a, "len", r["len_by"], "death", r["death_by"], "bps", r["burn_per_stack"], r["burn_per_stack_by"])
    print("   src", r["burn_per_stack_src"])
    print("   orb absorb/gain", r["orb_absorb_per_gain"], "orb_end/turn", round(r.get("orb_turn_end", 0) / max(1e-9, r.get("turns", 1)), 2),
          "lv4heal/fight", r.get("lv4_heal"), "majoka_end", r.get("majoka_end"), "m100", r.get("majoka100"),
          "combust_zero/fight", r.get("combust_zero"), "draw", r.get("combust_zero_draw"), "ign", r.get("combust_zero_ignite"),
          "attachauto ES", r.get("attach_auto:EmberSpark"), "SF", r.get("attach_auto:SnakeFire"))
K = ["probe_plays", "probe_free_plays", "probe_drawn", "probe_turn", "probe_orb_at_play", "probe_majoka_at_play", "probe_dmg",
     "probe_orb", "probe_auto", "probe_ignite_try", "probe_ignite_ok", "probe_ignite_auto", "probe_ignite_auto_val",
     "probe_attach_try", "probe_attached", "dg_triggers", "dg_dmg", "fine_dmg", "scald_hits", "scald_majoka", "sb_spread",
     "fof_try", "fof_ok", "inhand_turns", "inhand_ok", "lf_energy", "mr_stun", "rr_exhausted", "cd_win", "cd_hp_cost",
     "cd_combust", "fjc_trig", "fjc_dmg", "len", "deaths", "dg_play_turn"]
for p, by in R["probe"].items():
    nm = p.rstrip("+")
    for a, r in by.items():
        pl = max(1e-9, r.get("probe_plays", 0))
        d = {k: r[k] for k in K if r.get(k)}
        per = {k: round(r[k] / pl, 3) for k in ["probe_turn", "probe_orb_at_play", "probe_majoka_at_play", "probe_dmg", "probe_orb"] if r.get(k)}
        bsrc = r["burn_per_stack_src"].get(nm)
        bapp = r.get("bapp:" + nm, 0)
        bd = r.get("bdmg:" + nm, 0)
        print(p, a, "plays", round(r.get("probe_plays", 0), 3), "drawn", round(r.get("probe_drawn", 0), 3), "per", per,
              "burn app/fight", round(bapp, 2), "bdmg", round(bd, 2), "bps", bsrc, "len", r["len_by"],
              {k: round(v, 3) for k, v in d.items() if k not in ("probe_plays", "probe_drawn", "probe_turn", "probe_orb_at_play", "probe_majoka_at_play", "probe_dmg", "probe_orb", "len")},
              "AA", round(r.get("attach_auto:" + nm, 0), 3), "AAval", round(r.get("attach_auto_val:" + nm, 0), 3))
