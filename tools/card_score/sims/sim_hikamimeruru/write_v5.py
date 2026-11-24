import json
OUT = r"e:\slay_the_spire\Manosaba\tools\card_score\agent_estimates_v5_sim\HikamiMeruru.json"
V2 = json.load(open(r"e:\slay_the_spire\Manosaba\tools\card_score\agent_estimates_v2\HikamiMeruru.json", encoding="utf-8"))
V3H = json.load(open(r"e:\slay_the_spire\Manosaba\tools\card_score\agent_estimates_v3_heal\HikamiMeruru.json", encoding="utf-8"))["cards"]
V3A = json.load(open(r"e:\slay_the_spire\Manosaba\tools\card_score\agent_estimates_v3_ally\HikamiMeruru.json", encoding="utf-8"))["cards"]

cards = {k: dict(v) for k, v in V2["cards"].items()}
for src in (V3H, V3A):
    for k, v in src.items():
        cards[k].update(v)

def setc(name, v, vu, conf, why, vm=None, vmu=None):
    c = cards[name]
    c.update(value=v, value_upgraded=vu, confidence=conf, reasoning="v5模擬: " + why,
             value_multi=v if vm is None else vm, value_multi_upgraded=vu if vmu is None else vmu)

setc("TraumaHikamiMeruru", 0.3, 0.45, "medium",
     "10/15 魔女化整場保留，倍率 1.06/1.09；模擬魔女化加傷每次打出 0.10~0.16（升級牌組 0.22~0.35），梅露露攻擊少，下修自 0.5/0.75")
setc("AddFirewood", 0.2, 0.25, "medium",
     "+10/15 火力只在其後的藥水使用/丟棄（含合成丟棄）時多 10~15% 催化劑機率；模擬每次打出 0.04~0.22（升級 0.03~0.20），常在戰鬥後段打出；滿火力改給催化劑幾乎不發生（每場 <0.02）。條件已乘入")
setc("PotionSelect", 1.5, 2.2, "medium",
     "選 2/3 瓶輕效（多選輕效格檔配對合成格檔藥水 12）；模擬每次打出 0.7~2.8（樣本少，2 費貪婪少打），溢出 0.3~0.7 瓶/次；維持 v2")
setc("CollectPotions", 1.4, 1.8, "medium",
     "隨機 2/3 瓶輕效；模擬每次實際兌現 1.0~1.2（升級 1.0~1.15），每次生成 2.4~3.5 瓶（含合成產物）、溢出 0.1~1.1 瓶；欄位依照片升級平均 7.2 格。v2 2.2/3.1 下修")
setc("AutomatedPotionMix", 2.8, 3.2, "medium",
     "回合結束 2 瓶輕效；模擬每次打出兌現 1.8（與工廠搶欄位）~2.6（投擲牌組，輕效可丟換傷害），每次溢出 1~5.4 瓶；升級固有（模擬與工廠同時固有時更塞欄位 0.85~1.7）。v2 4.0/4.6 下修")
setc("PotionFactory", 4.5, 5.0, "medium",
     "回合開始 2 瓶隨機基礎藥水；模擬欄位幾乎常滿（戰末持有 6.7/7.2 格），每次打出溢出 6.9 瓶、兌現 1.85（含啟發式囤藥保守），實際玩家會更積極使用，取 4.5；v2 6.5/7.3（relative 2.65）下修")
setc("WitchesCauldron", 1.0, 1.0, "medium",
     "火力設為 100：其後每次非催化劑藥水使用/丟棄（含合成丟棄）有空欄即得催化劑；模擬每次打出 0.75（藥水牌組，貪婪打出率每場 0.15），催化劑需配對基礎藥水才兌現（+12 格檔等），單人無其他用途；消耗、獨特；升級降費")
setc("PotionThrow", 1.8, 1.8, "medium",
     "每層丟棄藥水對隨機敵 5 傷（v2 誤記 6），丟石頭改滾石 AoE 5 起每次 +5；投擲牌組每張每場約 1.8 張打出、每次打出兌現 1.77~1.83；合成丟棄目前也會觸發（PotionCraftService 未進入抑制範圍）。升級 0 費")
setc("PrimalPotionPower", 4.5, 4.5, "medium",
     "附加效果補計：藥水形石頭可直接使用造成 15 傷（原版 PotionShapedRock），把輕效（名目 0.5~1.0）換成每瓶約 2.5；模擬投擲牌組每次打出淨 5.2~8.4（約 4~7 瓶轉換），但需持有便宜藥水時才打（打出率 0.18~0.27/場），乘可用率並扣轉掉好藥水，取 4.5；v2 0.8 嚴重低估")
setc("HikamiMeruruExaid", 3.0, 3.0, "low",
     "魔女化補到 100（倍率 1.84 整場保留）後結束回合；模擬僅在菁英/首領前 3 回合打出時每次 +12.9（剩餘傷害 ×0.84），抽到時可用率約 0.3；其餘情境約 1.5；期望 0.3×10+0.7×1.5≈4 扣結束回合手牌損失 → 3.0；單人殺人衝動無隊友可傷。升級降費")
setc("NanokaPuzzle", 0.5, 0.8, "medium",
     "移除手牌與抽牌堆，塞 35/25 張無效拼圖+5 部件，部件全在手才勝利；模擬首領戰勝率 0.3%/1.3%（約需 8 回合空轉，期間無法防禦），死亡率不變，菁英戰啟發式不打；條件達成率乘入後 v2 2.5/3.5 大幅下修")
setc("DoubleSuicide", None, None, "medium",
     "MultiplayerOnly（程式碼 MultiplayerConstraint），單人不可用 value=null；多人沿用 v2",
     vm=0.6, vmu=0.6)
setc("DrugDealer", None, None, "medium",
     "MultiplayerOnly（程式碼 MultiplayerConstraint），單人不可用 value=null；多人全隊各 1 瓶輕效、升級 50% 催化劑沿用 v2",
     vm=5.2, vmu=5.55)
cards["MeruruInfirmary"]["reasoning"] = ("v5模擬: 保留 v3 補血 2.6；止痛藥為分級（(層-1)/2 <5 輕止痛 2 血、5~11 止痛 5、≥12 強止痛 12）非精確一半；"
                                         "貪婪 2 費少打（每場 0.04），樣本不足無法確認，維持 low。" + cards["MeruruInfirmary"]["reasoning"])
cards["QuickHeal"]["reasoning"] = "v5模擬: 每次打出兌現 2.1~2.9（回血 0.22/血+有效格檔），與 v3 相符保留。" + cards["QuickHeal"]["reasoning"]

out = {
    "character": V2["character"],
    "assumptions": [
        "v5模擬：sims/sim_hikamimeruru/sim.py，3 原型（藥水量產 brew／投擲 throw／治療 heal）各 1000 輪×16 場連續戰鬥（藥水、血量跨戰鬥保留），未升級與全升級各一組；每回合抽 5、3 能量、貪婪（價值/費用）出牌",
        "資源規則照程式碼：大鍋火力每場 20，非催化劑藥水使用/丟棄時 火力% 機率得催化劑（需空欄）；輕效同種×2→基礎、基礎+催化劑→強效；合成丟棄也觸發大鍋與藥水投擲",
        "照片遺物欄位 3+{1,2,4,6,10}（Lv1~5，exp 一般1/菁英2/首領3，門檻 0/2/7/15/30），模擬平均 7.2 格，藥水牌組欄位幾乎常滿",
        "牌組：起始 10 張（4 打擊 3 防禦 創傷 藥水選擇 添柴）+ 共用卡代表 3 張 11 傷/2 張 9 格檔 + 原型卡 10 張；敵血為雪莉模型 ×0.6 使回合長約 一般 3.3~4.4／菁英 5.3~7.9／首領 9~14，重要性 1/2/3",
        "藥水價值：格檔僅計擋下部分/5、回血 0.33/血、暫時力量計實際加傷/6、能量 0.85、石頭 15 傷；合成產物按名目價值分帳給素材來源；啟發式偏囤藥，量產卡兌現為保守值",
        "關鍵量測：藥水牌組每場催化劑 3.2 次、輕效配對合成 1.3~2.3 次、催化劑合成 2.0~2.2 次、藥水使用 7~7.6、丟棄 8~10；投擲牌組每場投擲傷害 52~64",
        "v3 修正保留：一次性回血 0.28/血、可重用 0.22/血（QuickHeal/GrandHeal/MeruruInfirmary）；ShotsFired 沿用 v3_ally 隊友機率 0.088；MultiplayerOnly（DoubleSuicide、DrugDealer）value=null",
        "條件型/低可用率：Exaid、NanokaPuzzle、PrimalPotionPower、AddFirewood、WitchesCauldron 已以模擬打出/觸發率乘入；X 費卡不處理"
    ],
    "mechanic_values": dict(V2["mechanic_values"], **{
        "催化劑(v5)": "每場藥水牌組 3.2 次觸發；單人只能合成（Tredecim 對自己使用會致死，單人無實用）",
        "藥水投擲(v5)": "每次丟棄 5 傷/層；石頭滾石 5 起 +5 遞增",
        "石頭(v5)": "藥水形石頭使用 15 傷 ≈2.5 能量"}),
    "cards": cards,
    "source": V2.get("source", "mod"),
}
json.dump(out, open(OUT, "w", encoding="utf-8", newline="\n"), ensure_ascii=False, indent=2)
print(len(cards))
