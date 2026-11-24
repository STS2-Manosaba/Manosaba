"""從原版評分結果挑出錨點卡，給估值 agent 當共同的價值參考。

用法：python tools/card_score/make_anchors.py（需先執行 card_score.py 產生 output/data.json）
輸出：tools/card_score/anchors.md
"""
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
data = json.loads((HERE / "output" / "data.json").read_text(encoding="utf-8"))
cards = [c for c in data["cards"] if c["source"] == "original" and c["pool"] != "Colorless"
         and c["relative"] is not None and c["raw"] is not None and c["bucket"] in ("Basic", "Common", "Uncommon", "Rare")
         and c["confidence"] in ("auto", "agent-high", "agent-medium")]
pref = {"auto": 0, "agent-high": 1, "agent-medium": 2}

L = ["# 原版錨點卡（估值共同尺標）", "",
     "value 欄是「打出一次的效果價值」（能量單位，不扣費用、不計消耗等關鍵字），與你要輸出的 value 同一尺度。",
     "這些卡的相對強度接近原版同稀有度中位數（≈1.0），代表「設計上剛好合理」的強度；另附偏強與偏弱的例子。",
     "卡牌程式碼在 e:\\slay_the_spire\\decompile1118\\src\\Core\\Models\\Cards\\<名稱>.cs，可對照效果。", ""]
for rarity in ("Basic", "Common", "Uncommon", "Rare"):
    L += [f"## {rarity}", "", "| 卡 | 角色 | 類型 | 費用 | value | 升級後 | 相對強度 |", "|---|---|---|---|---|---|---|"]
    picks = []
    for t in ("Attack", "Skill", "Power"):
        pool = [c for c in cards if c["bucket"] == rarity and c["type"] == t]
        pool.sort(key=lambda c: (abs(c["relative"] - 1.0), pref[c["confidence"]]))
        picks += pool[:3 if rarity != "Basic" else 2]
    if rarity != "Basic":
        rest = [c for c in cards if c["bucket"] == rarity and c["confidence"] in ("auto", "agent-high")]
        picks += sorted(rest, key=lambda c: -c["relative"])[:2]   # 偏強例
        picks += sorted(rest, key=lambda c: c["relative"])[:2]    # 偏弱例
    seen = set()
    for c in picks:
        if c["name"] in seen:
            continue
        seen.add(c["name"])
        L.append(f"| {c['name']} | {c['pool']} | {c['type']} | {c['cost']} | {c['raw']:.2f} | "
                 f"{(c['raw_upg'] or c['raw']):.2f} | {c['relative']:.2f} |")
    L.append("")
L += ["## 校準結論（請在估值時留意）", "",
      f"- 類型係數（腳本會自動套用，**不要**自己再修正）：{ {k: round(v, 2) for k, v in data['calibration']['type_factor'].items()} }",
      "- 原因：以同一套方法估原版時，技能牌的過牌、檢索、保留等操作價值常被低估，能力牌的整場價值常被高估。",
      "- 估值時請照實估計，只要和上方錨點卡保持一致的尺度即可。"]
(HERE / "anchors.md").write_text("\n".join(L) + "\n", encoding="utf-8")
sys.stdout.reconfigure(encoding="utf-8")
print(f"錨點卡 {sum(1 for l in L if l.startswith('| ') and not l.startswith('| 卡'))} 張 → {HERE / 'anchors.md'}")
