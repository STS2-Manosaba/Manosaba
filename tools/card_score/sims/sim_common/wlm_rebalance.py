"""只重跑魔女語精通（平衡調整：2 費／升級 1 費、反傷 1 倍）的探針，輸出 wlm_rebalance.json。"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import sim  # noqa: E402

out = {}
for h in sim.HOSTS:
    out[h] = {"base": sim.run(h, None)}
    for p in ("WitchLanguageMastery", "WitchLanguageMastery+"):
        out[h][p] = sim.run(h, p)
    print(h, "done", file=sys.stderr)
Path(__file__).with_name("wlm_rebalance.json").write_text(
    json.dumps(out, ensure_ascii=False, indent=1, default=float), encoding="utf-8")
