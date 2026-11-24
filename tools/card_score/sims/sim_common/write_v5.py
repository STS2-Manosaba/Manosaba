import json, os
base = r'e:\slay_the_spire\Manosaba\tools\card_score'
L = lambda d: json.load(open(os.path.join(base, d, 'Common.json'), encoding='utf-8'))
v2 = L('agent_estimates_v2')
cards = {k: dict(v) for k, v in v2['cards'].items()}
for d in ('agent_estimates_v3_heal', 'agent_estimates_v3_ally', 'agent_estimates_v4'):  # 依序覆蓋，保留既有修正
    for k, v in L(d)['cards'].items():
        cards[k] = dict(v)


def C(k, v, vu, vm, vmu, conf, why):
    cards[k] = dict(value=v, value_upgraded=vu, value_multi=vm, value_multi_upgraded=vmu, confidence=conf,
                    reasoning="v5模擬:" + why)


C("WitchLanguageMastery", 8.2, 8.2, 8.2, 8.2, "medium",
  "打出率 0.96、平均第 2.2 回合；反傷（含被格擋傷害×2，扣除敵人溢出）每場 43(雪莉)/53(可可)/49(艾瑪)，約 48/6≈8.0，"
  "折算每次打出 8.2；另使戰鬥縮短約 2.5 回合、少受約 25 血（已大致由反傷計入不重複加）。升級僅 0 費，效果同。"
  "v2 的 11 高估（未計戰鬥提早結束後受擊次數減少）；relative 仍偏高屬設計強度問題")
C("Prison", 1.3, 2.0, 1.3, 2.0, "medium",
  "魔女化增傷實測每場額外傷害 可可 9.8/艾瑪 8.5，雪莉 0（回合開始清空魔女化）；升級 14.5/13.1/0；"
  "條件加權（三宿主平均、扣打出率 0.96）約 6.4/9.6 傷≈1.1/1.6；達 100 層（角色魔法卡）機率 6~9%/17~20%，另計 +0.2/+0.4。v2 3.0/4.0 高估")
C("Wall", 3.6, 3.6, 3.6, 3.6, "medium",
  "受到的未格擋攻擊傷害轉活力：每場活力 26(雪莉)/30(可可)/26(艾瑪)，扣溢出與攻擊時機約八成有效≈22 傷/6≈3.6；"
  "需先受傷的條件已由模擬量入。升級僅降 1 費，效果不變")
C("ForgeSpear", 2.6, 3.2, 2.6, 3.2, "medium",
  "零件合成率 67%(雪莉)/82%(可可)/80%(艾瑪)，平均第 3.1~3.9 回合合成；長矛實際造成傷害每場 21/25/24（扣溢出）≈3.9；"
  "抽到零件的空抽 3.1~3.5 張×0.45 ≈ −1.5 → 2.6（以合成率乘入，不以 50 傷計）。升級不消耗：再洗入一組，合成 0.81~1.05 次、"
  "長矛傷 26~32、空抽 3.8~4.7 → 3.2。哈斯米蕾亞只需 3 零件（未模擬，略高）")
C("Guillotine", 2.5, 3.2, 2.5, 3.2, "medium",
  "14 傷≈2.33；斬首觸發率每次打出 10~11%（貪婪選目標），每次斬首省下約 7 血≈+0.1；"
  "升級 18 傷、門檻 15%：觸發率 16%、省 8 血≈+0.2 → 3.2。v2 2.9/3.9 高估斬首")
C("DeathPendulum", 1.5, 2.5, 1.5, 2.5, "medium",
  "打出時手中技能數平均 2.2(雪莉)~2.5(可可/艾瑪)，全體 3 傷×2.4 段、平均存活敵人加權約 ×1.3 ≈9.4 傷 → 1.5；升級 5 傷 → 2.5")
C("Suicide", 0.5, 0.8, 0.5, 0.8, "medium",
  "附加效果核對：自傷 3（−0.6）＋魔女化 20/30 層：倍率僅 +6%/+12% 且小數取整，模擬增傷每場 <0.1；雪莉回合開始清空魔女化。"
  "主要價值是處刑卡（典獄長）、湊 100 魔女化與魔女語精通自傷反傷（3×2=全體 6）。v2 1.1/1.8 高估")
C("WardenCommand", 2.6, 3.3, 2.6, 3.3, "medium",
  "每次打出後處刑卡額外打出次數：牌組處刑卡 k=1/2/3/4 張時 0.6~0.8/1.0~1.3/1.2~1.6/1.3~1.7 次，"
  "折合價值 1.8/3.0/3.6/4.1（固有升級 2.2/3.7/4.8/5.5）；以 k 分布 0:15% 1:25% 2:30% 3:20% 4:10% 加權（k=0 計 0）→ 2.5/3.2，"
  "選此卡的牌組偏處刑略上調 → 2.6/3.3。v2 4.5/5.5 高估")
C("WitchIslandExpedition", 0.3, 0.3, 0.3, 0.3, "low",
  "敵方 Move 攻擊 ×3，QTE 成功 ×0。QTE 成功率 p=0.5/0.7/0.9 時每場淨受傷 +25~42/+0~5/−23~28 血；損益兩平約 p≈0.72。"
  "p 依玩家操作無法模擬，以 p≈0.7 估接近 0；升級固有反而更早承受風險，不加值")
C("CulpritVerdict", 0.1, 0.25, 0.1, 0.25, "low",
  "低機率可用：需第 5 回合（升級第 3 回合）後才能打出，模擬戰鬥加權平均 5.7~7 回合、一般戰約 4 回合，抽到時可打出約 40%（升級約 70%）；"
  "非 Ema 罪證體系時真兇常落在敵人（攻擊+2）。可用時價值 ~0.3，乘可用率")
# MultiplayerOnly：單人 null
for k in ("DriftApart", "Inhibition", "TrialStart", "GuiltQuestion"):
    if k in cards:
        c = cards[k]; c["value"] = None; c["value_upgraded"] = None
        if not c["reasoning"].startswith("v5"):
            c["reasoning"] = "v5模擬:MultiplayerOnly，單人 value=null；多人欄位沿用。" + c["reasoning"]

out = {
    "character": "Common",
    "assumptions": [
        "v5模擬：sims/sim_common/sim.py，每情境 6000 場；每回合抽 5、3 能量、手牌上限 10，貪婪啟發式（即時價值/費用）",
        "宿主：雪莉(4打3防+2拳+殺人事件、起始 1 力量、回合開始清空魔女化)、可可(3打4防+3技)、艾瑪(3打3防+5張)，各加 6 張通用中期卡與 k 張隨機共用處刑卡（預設 k=2）",
        "敵人：一般 72%(總血 50~72、1~3 隻)、菁英 20%(105~130)、首領 8%(220~260)；每回合總攻擊 7/10/12 起線性成長、分兩段；模擬回合 3.6~4.5/6.1~7.7/11~14；重要性 1/2/3 加權",
        "魔女化倍率依 MajokaPower Hill 曲線，傷害取整；單人無隊友，殺人衝動無害；魔法卡不計值（僅以達 100 機率小幅加值）",
        "使用者規則：條件型效果乘模擬達成率；逐卡核對附加效果；低機率可用卡乘可用機率",
        "v3_heal/v3_ally/v4 修正保留；MultiplayerOnly 卡 value=null；X 費（Electrocution）未處理沿用 v2",
    ],
    "mechanic_values": v2.get("mechanic_values", {}),
    "cards": cards,
    "source": "sims/sim_common/sim.py + results.json",
}
json.dump(out, open(os.path.join(base, 'agent_estimates_v5_sim', 'Common.json'), 'w', encoding='utf-8', newline='\n'),
          ensure_ascii=False, indent=2)
print(len(cards))
