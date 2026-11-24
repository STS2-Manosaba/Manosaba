"""產生交叉審核抽樣清單：每個卡池抽 8 張 agent 估值卡（類型、稀有度分散），分給審核員 A／B 各 4 張。

用法：python tools/card_score/make_review_sample.py（需先執行 card_score.py）
輸出：tools/card_score/reviews/sample_A.md、sample_B.md
"""
import hashlib
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
data = json.loads((HERE / "output" / "data.json").read_text(encoding="utf-8"))
PER_POOL = 8

def stable(s: str) -> str:
    return hashlib.md5(s.encode()).hexdigest()

def code_path(c):
    if c["source"] == "mod":
        return f"e:\\slay_the_spire\\Manosaba\\Manosaba\\Characters\\{c['pool']}\\（搜尋 class {c['name']}）"
    base = "decompile_beta1119" if c["source"] == "original-beta" else "decompile1118"
    return f"e:\\slay_the_spire\\{base}\\src\\Core\\Models\\Cards\\{c['name']}.cs"

pools = {}
for c in data["cards"]:
    if c["confidence"].startswith("agent") and c["bucket"] != "Ancient" and c["pool"] != "Deprecated" and c["raw"]:
        pools.setdefault((c["source"], c["pool"]), []).append(c)

sheets = {"A": [], "B": []}
for (src, pool), cs in sorted(pools.items()):
    # 依「類型-稀有度」輪流挑，讓樣本分散
    strata = {}
    for c in sorted(cs, key=lambda c: stable(c["name"])):
        strata.setdefault((c["type"], c["bucket"]), []).append(c)
    keys = sorted(strata, key=lambda k: stable(str(k)))
    picks = []
    while len(picks) < PER_POOL and any(strata.values()):
        for k in keys:
            if strata[k] and len(picks) < PER_POOL:
                picks.append(strata[k].pop(0))
    for i, c in enumerate(picks):
        sheets["A" if i % 2 == 0 else "B"].append(c)

out = HERE / "reviews"
out.mkdir(exist_ok=True)
for who, cs in sheets.items():
    L = [f"# 審核清單 {who}（{len(cs)} 張）", "",
         "key 欄請原樣作為輸出 JSON 的 key。", "",
         "| key | 卡牌 | 類型 | 稀有度 | 費用 | 程式碼位置 |", "|---|---|---|---|---|---|"]
    for c in cs:
        key = f"{c['source']}|{c['pool']}|{c['name']}"
        L.append(f"| {key} | {c['title'] or c['name']} | {c['type']} | {c['bucket']} | {c['cost']} | {code_path(c)} |")
    (out / f"sample_{who}.md").write_text("\n".join(L) + "\n", encoding="utf-8")
print({k: len(v) for k, v in sheets.items()}, "→", out)
