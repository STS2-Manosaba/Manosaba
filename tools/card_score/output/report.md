# 卡牌量化評分報告

分數 = 效果價值（能量單位）× 關鍵字係數 ÷ 有效費用。百分位 = 在原版同稀有度卡中的位置。
confidence：auto 純數值；partial 有額外邏輯（分數可能低估）；manual 無法自動評分。

## 從原版校準的係數

假設原版多數卡強度相近，反推費用曲線與關鍵字代價（非能力牌、無上述關鍵字的卡）。

| 費用 | 有效費用 | 樣本數 |
|---|---|---|
| 0 | 0.84 | 27 |
| 1 | 1.00 | 77 |
| 2 | 1.80 | 22 |
| 3+ | 2.70 | 7 |

| 關鍵字 | 價值係數 | 原版樣本數 |
|---|---|---|
| Exhaust | ×0.85 | 23 |
| Ethereal | ×0.85 | 4 |
| Retain | ×1.10 | 5 |
| Innate | ×1.15 | 5 |

係數 < 1 表示原版會給帶該關鍵字的卡更強的效果作為補償，因此它是代價；能力牌不套用消耗係數。

## 原版基準（auto＋partial）

| 稀有度 | 張數 | P10 | 中位數 | P90 |
|---|---|---|---|---|
| Basic | 19 | 0.96 | 1.15 | 2.36 |
| Common | 100 | 1.02 | 1.68 | 2.41 |
| Uncommon | 225 | 1.29 | 2.00 | 3.41 |
| Rare | 155 | 1.17 | 2.08 | 3.94 |
| Normal | 480 | 1.15 | 1.95 | 3.28 |
| Ancient | 10 | 1.53 | 3.76 | 6.05 |

中位數即「該稀有度下每 1 能量的期望價值」；相對強度 = 分數 ÷ 該中位數（1.00 = 原版平均）。

## 各角色／卡池統計

| 來源 | 卡池 | 起始遺物 | 卡數 | 可評分 | 中位數（合併） | 截尾平均 | 平均 | 單人 | 多人 | 平均百分位 | 過強 | 過弱 | 需手動 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| mod | Common |  | 36 | 35 | 0.90 | 0.90 | 1.01 | 0.93 | 0.90 | 40.40 | 3 | 7 | 1 |
| mod | Deprecated |  | 3 | 3 | 0.64 | 0.78 | 0.78 | 0.38 | 1.02 | 31.56 | 0 | 2 | 0 |
| mod | HasumiLeia | Rapier | 47 | 46 | 0.99 | 1.04 | 1.08 | 0.97 | 1.00 | 49.04 | 5 | 8 | 1 |
| mod | HikamiMeruru | PhotoOfTheGreatWitch, WitchesCauldron | 19 | 19 | 0.86 | 0.81 | 0.88 | 0.86 | 0.86 | 34.32 | 2 | 8 | 0 |
| mod | HoshoMago | TarotDeck | 25 | 25 | 0.99 | 0.98 | 0.98 | 0.99 | 0.99 | 45.57 | 1 | 3 | 0 |
| mod | JogasakiNoah | SprayCanOfNoah | 19 | 19 | 0.83 | 0.96 | 1.02 | 0.86 | 0.83 | 39.29 | 2 | 5 | 0 |
| mod | KurobeNanoka | Ribbon, MagicalGun | 36 | 36 | 1.08 | 1.03 | 1.05 | 1.05 | 1.08 | 53.02 | 3 | 7 | 0 |
| mod | NatsumeAnan | Clipboard | 18 | 18 | 0.98 | 1.17 | 1.20 | 0.98 | 0.98 | 52.99 | 4 | 1 | 0 |
| mod | NikaidoHiro | PenOfHiro | 22 | 22 | 1.13 | 1.16 | 1.21 | 1.13 | 1.08 | 60.38 | 4 | 0 | 0 |
| mod | SaekiMiria | CabinetKey | 42 | 41 | 1.08 | 1.13 | 1.13 | 1.05 | 1.08 | 56.58 | 5 | 5 | 1 |
| mod | SakurabaEma | LegRing | 43 | 43 | 1.27 | 1.47 | 1.54 | 1.24 | 1.34 | 66.65 | 19 | 8 | 0 |
| mod | SawatariCoco | LiveStreamingEquipment | 40 | 40 | 0.91 | 0.95 | 1.00 | 0.91 | 0.91 | 42.58 | 4 | 12 | 0 |
| mod | ShitoAlisa | LegIrons | 23 | 22 | 1.06 | 1.06 | 1.05 | 1.00 | 1.09 | 50.86 | 1 | 5 | 1 |
| mod | TachibanaSherry | MagnifyingGlass | 39 | 39 | 0.84 | 0.89 | 0.92 | 0.80 | 0.84 | 39.01 | 3 | 11 | 0 |
| mod | TonoHanna | FeatherFan | 42 | 41 | 0.91 | 0.85 | 0.87 | 0.84 | 0.91 | 37.03 | 0 | 8 | 1 |
| original | Colorless |  | 65 | 64 | 1.03 | 1.06 | 1.23 | 1.04 | 1.03 | 52.02 | 9 | 8 | 1 |
| original | Defect |  | 89 | 87 | 1.02 | 1.06 | 1.13 | 1.00 | 1.02 | 52.09 | 11 | 5 | 2 |
| original | Ironclad |  | 88 | 86 | 1.00 | 1.02 | 1.07 | 0.99 | 1.00 | 50.35 | 7 | 5 | 2 |
| original | Necrobinder |  | 89 | 87 | 1.00 | 1.03 | 1.05 | 1.01 | 1.00 | 49.96 | 7 | 11 | 2 |
| original | Regent |  | 89 | 88 | 1.16 | 1.16 | 1.24 | 1.17 | 1.16 | 57.34 | 14 | 8 | 1 |
| original | Silent |  | 89 | 87 | 0.88 | 0.89 | 0.93 | 0.86 | 0.90 | 38.69 | 3 | 12 | 2 |
| original-beta | Fool |  | 88 | 86 | 0.98 | 1.03 | 1.09 | 0.97 | 0.98 | 49.78 | 9 | 11 | 2 |

## 尚未定義權重的機制（依出現次數）

在 weights.json 的 powers 或 pool_overrides 補上權重後，這些卡就能自動評分。

| 卡池 | 機制（次數） |
|---|---|

## 隨機效果處理狀態（模組卡）

model＝專屬期望值模型（random_models.py）；branch＝自動解析機率分支；
unmodeled＝含隨機但尚未建模（分數未反映隨機部分），可在 random_models.py 補上。

| 狀態 | 卡（卡池） |
|---|---|
| model（30） | 藥水的未來？（Common）, 線性攻擊（HasumiLeia）, 星塵幻影（HasumiLeia）, 藥水蒐集（HikamiMeruru）, 藥頭（HikamiMeruru）, 解夢（HoshoMago）, 色彩迸發（JogasakiNoah）, 困惑（JogasakiNoah）, 調色（JogasakiNoah）, 祖瑪（JogasakiNoah）, 緊急防禦（KurobeNanoka）, 360 NO SCOPE（KurobeNanoka）, 交換（SaekiMiria）, 精神碎片（SaekiMiria）, 電影鑑賞（SaekiMiria）, 共同搜查（SakurabaEma）, 提問（SakurabaEma）, 搜查（SakurabaEma）, I want to play a game（SawatariCoco）, 來玩捉迷藏（SawatariCoco）, 十字弩（TachibanaSherry）, 冰塊（TachibanaSherry）, 萬能鑰匙（TachibanaSherry）, 天使的骰子（TonoHanna）, 升天電梯（TonoHanna）, 惡魔的骰子（TonoHanna）, 給漢娜一罐啤酒（TonoHanna）, 米莉亞人偶（TonoHanna）, 諾亞人偶（TonoHanna）, 風險骰子（TonoHanna） |
| branch（1） | SIX SEVEN（TachibanaSherry） |
| unmodeled（5） | 模仿魔法（HoshoMago）, 隱者（HoshoMago）, 命運之輪（HoshoMago）, 幻視魔法（KurobeNanoka）, 審判之時已至（SakurabaEma） |

## 模組卡：可能過強

| 卡池 | 卡名 | 稀有度 | 費用 | 分數 | 相對強度 | 升級後 | 百分位 | confidence | 數值 | 備註 |
|---|---|---|---|---|---|---|---|---|---|---|
| Common | 魔女語精通 | Rare | 1 | 9.87 | 4.75 | 11.76 | 99 | agent-low |  | agent：v2: 程式確認 TotalDamage=Blocked+Unblocked，被格擋的傷害也計入；反傷以 nonCardHpLoss 讓全體敵人失去生命（無視敵方格檔）。約10/回合×2×4回合≈80，受敵人血量溢出折減，較 v1 略降 / 回合模型：4→4.37 回合 |
| SakurabaEma | 情報共享 | Uncommon | 1 | 8.03 | 4.01 | 9.68 | 100 | agent-medium | TrialBlock=8, Cards=1 | agent：v5模擬:多人限定；就緒55%（v2按30%）時雙方各得1/2張虛無提出證據，+0.5/+0.8 |
| SakurabaEma | 對簿公堂 | Uncommon | 1 | 7.67 | 3.83 | 10.04 | 100 | agent-medium | Sus=1, Cards=3 | agent：多人限定；多人：提出證據、雙方各3/4張發言、嫌疑 |
| SakurabaEma | 贊同 | Common | 1 | 5.31 | 3.15 | 7.08 | 100 | agent-medium | custom:BlockPerArgument=3 | agent：多人限定；多人：發言+雙方格擋6/9，議論加成 |
| SakurabaEma | 反駁 | Common | 1 | 5.15 | 3.06 | 6.49 | 100 | agent-medium | custom:DamagePerCard=2 | agent：v5模擬:9/12傷(1.5/2.0)+提出證據1.9；就緒時+2/+3傷×圖鑑張數，E[張數×就緒]=4.36→+1.45/+2.2；罪證加成屬環境不計（進度≥10於打出時55%、整場達10為90%；≥20打出時20%、整場49%） |
| JogasakiNoah | 百斂 | Rare | 1 | 5.92 | 2.85 | 8.44 | 97 | agent-low | custom:BloodOrbPassive=2 | agent：選穿血(30傷)或超新星加入手牌，血球層數+2；升級衍伸卡與+3 |
| SawatariCoco | 來玩捉迷藏 | Rare | 1 | 5.73 | 2.76 | 7.25 | 97 | agent-medium | Majoka=20, gen:Draw:cls:HideInWardrobe|FoundYou=1 | 期望值模型：隨機躲進衣櫃／找到你了 洗入抽牌堆 / agent：v5模擬: 打出時魔女化平均 122，74% 選找到你，約 45% 的打出最終兌現 44 傷（魔女化倍率約 1.8~2.4）；+20 魔女化；邊際模擬每次約 +9（高魔女化牌組，取保守） / agent：v5模擬: 打出時魔女化平均 122，74% 選找到你，約 45% 的打出最終兌現 44 傷（魔女化倍率約 1.8~2.4）；+20 魔女化；邊際模擬每次約 +9（高魔女化牌組，取保守） |
| SakurabaEma | 炙燒藥瓶 | Uncommon | 1 | 5.43 | 2.71 | 6.97 | 98 | agent-medium | Sus=1, Burn=15, Cards=1 | agent：15/20灼燒+發言−嫌疑 |
| HasumiLeia | 無限矛製 | Rare | 1 | 5.61 | 2.70 | 11.06 | 97 | agent-low |  | agent：每回合生成虛無鑄矛，需1費打出 / 回合模型：4.5→4.37 回合 |
| HikamiMeruru | 藥水工廠 | Rare | 1 | 5.46 | 2.63 | 9.41 | 96 | agent-medium | PotionFactory=2 | agent：每回合 2 瓶隨機藥水約 4.5 回合，藥水免費、欄位限制下實用約 5～6 瓶；對照 Demesne 8/Pyre 4.5 / 回合模型：4.5→4.37 回合 |
| SakurabaEma | 證物打擊 | Common | 1 | 4.27 | 2.54 | 4.79 | 100 | agent-medium | TrialDamage=6, Cards=1 | agent：v5模擬:6/9傷+提出證據1.9+活體書頁1.3 |
| NatsumeAnan | 自殺吧！ | Basic | 0 | 2.84 | 2.48 | 4.49 | 100 | agent-medium | custom:KotodamaCost=1, Jigai=5 | 暫時性：Jigai→TempStrength / agent：1 言靈：敵 -5/-8 暫時力量減傷+敵方回合開始 5/8 傷害 |
| HasumiLeia | 保護 | Uncommon | 1 | 4.95 | 2.47 | 5.63 | 98 | agent-medium | Block=11 | agent：11格檔，替隊友擋傷單人無用；v2:MultiplayerOnly，單人null |
| SakurabaEma | 受夠繁文縟節了！ | Uncommon | 0 | 4.90 | 2.45 | 5.76 | 98 | agent-medium |  | agent：v5模擬:0費3次提出證據；打出時平均回合2、圖鑑僅2.8張（空8%），每次約1.5→4.1；升級去消耗不改單次價值 |
| SakurabaEma | 提問 | Common | 0 | 4.09 | 2.43 | 4.77 | 100 | agent-medium | Cards=1, custom:Options=3, gen:Hand:generic=1 | 期望值模型：3 選 1（以一般生成卡計） / agent：v5模擬:抽1＋自動打出證言（抽1+罪證2≈2.1，艾瑪/守衛證言額外≈+0.15）＋證言留在圖鑑使之後提出證據可重複選它（約+0.3）＋裁判進度+1；升級三選一可挑守衛（力量2）/艾瑪（發言），約+0.5 / agent：v5模擬:抽1＋自動打出證言（抽1+罪證2≈2.1，艾瑪/守衛證言額外≈+0.15）＋證言留在圖鑑使之後提出證據可重複選它（約+0.3）＋裁判進度+1；升級三選一可挑守衛（力量2）/艾瑪（發言），約+0.5 |
| KurobeNanoka | 愚者愚者 | Rare | 1 | 5.03 | 2.42 | 6.14 | 95 | agent-medium |  | agent：1力敏並串接2、3階，總計可達6力6敏 |
| SakurabaEma | 審判之時已至 | Rare | 1 | 5.02 | 2.42 | 6.22 | 95 | agent-low | Cards=5 | agent：v5模擬:三選一裁判牌≈2.5（可抽到反駁約5）；抽牌堆5/7張附加裁判：每張後續打出+1進度，附加裁判的攻擊吃罪證（打出時平均罪證≈18.6，折半計）；未直接模擬 |
| NikaidoHiro | 艾瑪出擊 | Uncommon | 1 | 4.74 | 2.36 | 5.64 | 97 | agent-low | EmaDogAttack=1 | agent：每回合末艾瑪對隨機敵造成其生命傷害，約10傷×4回合×0.9，艾瑪死亡則失效 / 回合模型：4→4.37 回合 |
| SakurabaEma | 提出證據 | Basic | 0 | 2.67 | 2.33 | 2.81 | 95 | agent-medium | Argument=0 | agent：v5模擬:提出證據每次平均1.96（圖鑑空7.4%；有證言時幾乎都選證言=抽1+罪證2，罪證每層0.8）；升級+1議論，但議論達10僅2.6%場次，約+0.1 |
| JogasakiNoah | 調色 | Basic | 1 | 2.63 | 2.30 | 3.07 | 95 | agent-medium | Block=4, custom:PaintOrb=1 | 期望值模型：權重表隨機 1 顆顏料球（以顏料球平均價值計） / agent：4格檔＋紅藍黃球(≈1.6)；升級6格檔 |
| NatsumeAnan | 少爺 | Uncommon | 1 | 4.48 | 2.23 | 6.09 | 96 | agent-low | Bocchan=2 | agent：每回合 +2/3 言靈×約 4.5 回合，每點邊際約 0.55~0.65；對照 Inflame/Loop 整場尺度略上修 / 回合模型：4.5→4.37 回合 |
| HasumiLeia | 閃避 | Basic | 1 | 2.53 | 2.21 | 3.66 | 95 | agent-high | Block=8, Vulnerable=1 | agent：8格檔+1易傷，格檔有反擊加成 |
| SakurabaEma | 共同搜查 | Rare | 1 | 4.58 | 2.21 | 5.52 | 93 | agent-medium | Cards=2, gen:Hand:filter:parent=SearchEvidenceCard=2 | 期望值模型：抽牌＋隨機證據（單人計） / agent：v5模擬:抽2/3+2證物（邊際0.6×2）；多人沿用v3（2.5人隊，隊友1.5名）結構，只下修自身證物部分 / agent：v5模擬:抽2/3+2證物（邊際0.6×2）；多人沿用v3（2.5人隊，隊友1.5名）結構，只下修自身證物部分 |
| TachibanaSherry | 一輩子的朋友 | Rare | 1 | 4.52 | 2.17 | 5.77 | 92 | agent-high | LifelongFriend=1 | agent：單人 0；多人雙方每回合得免費落石、-20 魔女化，有連帶死亡風險 |
| SakurabaEma | 不要小看我的情報網 | Rare | 1 | 4.33 | 2.08 | 5.15 | 92 | agent-medium | InformationNetwork=1 | agent：v5模擬:每次提出證據隨機敵6傷；每場提出證據11.2次，第1輪打出後剩約60%≈6.5次×6傷×0.85溢傷折扣；升級僅降費 |
| TachibanaSherry | 雙拳 | Rare | 1 | 4.32 | 2.08 | 5.77 | 92 | agent-medium | Damage=5 | agent：v5模擬: 模擬打出時力量 4.2~4.8，平均 4.2~4.6 段，基礎 5×4.5 段≈22.5 傷(3.75)+力量乘段數的增益部分 0.5；升級 7 傷/段 |
| Common | 鑄矛 | Rare | 1 | 4.29 | 2.06 | 5.60 | 92 | agent-low | Cards=1 | 隨機不影響期望值：只隨機插入位置 / agent：零件洗入抽牌堆，湊齊後合成 50 傷 0 費長槍；合成率約 65%、佔手牌；升級不消耗 |
| SawatariCoco | 炎上發言 | Common | 1 | 3.46 | 2.05 | 4.12 | 99 | agent-medium | HpLoss=2, CalculationBase=10 | agent：v5模擬: 打出時處於直播模式 85~99%（三牌組加權約 90%，舊假設 65%）；10+20×0.9=28 傷≈4.7，扣失 2 血 0.4 |
| SawatariCoco | 淋浴間直播 | Uncommon | 1 | 4.07 | 2.03 | 5.21 | 95 | auto | Block=15, Hiding=1 |  |
| SakurabaEma | 推理要在晚餐後 | Uncommon | 1 | 4.01 | 2.00 | 4.78 | 95 | agent-medium |  | agent：v3補血: 豐盛的一餐回 3 血每點 0.084→0.28，+0.6；活力、發言與壞結局代價不變 |
| SakurabaEma | 沒有邀請我很可疑 | Uncommon | 1 | 3.96 | 1.97 | 5.43 | 95 | agent-medium | Block=10, custom:AllyBlock=3, Cards=1, Sus=1 | agent：v5模擬:單人對自己：10/13格擋+抽1−嫌疑1/2；就緒55%時加 3/4×嫌疑（含本次+1/+2，E≈1.15/1.7）格擋→+0.69/+1.36；多人沿用v3+0.5 |
| NikaidoHiro | 論破 | Uncommon | 1 | 3.92 | 1.96 | 5.16 | 95 | agent-medium | CalculationBase=10, ExtraDamage=6 | agent：10傷+一半嫌疑(約3層)×6=28傷≈4.7，扣鋼筆加成損失約0.9；升級14+3×7=35傷 |
| KurobeNanoka | 幻視調查 | Rare | 1 | 4.03 | 1.94 | 5.03 | 90 | agent-high | Energy=3, Cards=2 | agent：下回合+3/4能量+抽2 |
| NatsumeAnan | 小說創作 | Uncommon | 0 | 3.86 | 1.93 | 4.76 | 94 | agent-medium | custom:KotodamaCost=5, Block=5, Energy=3 | 隨機不影響期望值：需諾亞隊友，單人無生成 / agent：v3隊友:特定隊友機率=1.5名隊友/17≈0.088（2.5人隊；單人0）；諾亞隊友時加一張設定卡（約2.2）×0.088≈+0.2 |
| SaekiMiria | 電影噪音 | Uncommon | 1 | 3.80 | 1.90 | 6.07 | 93 | agent-medium | Damage=5 | agent：能力：每打出電影牌對隨機敵5(8)傷，約1張/回合×5回合 / 回合模型：5→4.37 回合 |
| NatsumeAnan | 忍耐 | Uncommon | 1 | 3.77 | 1.88 | 4.78 | 92 | agent-medium | custom:KotodamaGain=4 | agent：+4/5 言靈；暈眩經體弱多病變兩張但抽到即補抽回血，負面極小 |
| NikaidoHiro | 打擊-天 | Common | 1 | 3.15 | 1.87 | 4.18 | 98 | agent-medium | Damage=12, Cards=1 | agent：12傷2.0+抽1 0.5+上段0.3+鋼筆0.25 |
| SaekiMiria | 替罪羊 | Uncommon | 1 | 3.67 | 1.83 | 4.52 | 92 | agent-medium | Block=11 | agent：限多人；11格檔並替隊友擋傷 |
| Common | 漸行漸遠 | Uncommon | 1 | 3.62 | 1.80 | 5.42 | 92 | agent-medium | Strength=2, Dexterity=2 | agent：多人限定；力量給隊友單人計 0，只計自身永久 2/3 敏捷；多人：隊友獲 2/3 永久力量 |
| KurobeNanoka | 逃生路線 | Common | 1 | 3.02 | 1.79 | 3.85 | 98 | agent-high | Block=11 | agent：下回合11/14格檔 |
| SaekiMiria | 笨手笨腳 | Common | 0 | 3.00 | 1.78 | 3.82 | 98 | auto | Block=8, Weak=2 |  |
| ShitoAlisa | 災厄少女 | Uncommon | 1 | 3.57 | 1.78 | 5.53 | 92 | agent-low | custom:DisasterGirlDmg=7, DisasterGirl=1 | agent：每施加5灼燒對隨機敵造成7/11傷，估約4~5次觸發；v2灼燒每層0.26對齊中毒 |
| HoshoMago | 死神 | Ancient | 1 | 3.46 | 1.78 | 5.39 | 91 | agent-medium | custom:AllyDoom=5, custom:EnemyDoom=20 | agent：全體20災厄×0.15×1.4，自身5災厄扣分，有魔術師×1.5。 |
| SawatariCoco | 電幻貓咪上衣 | Uncommon | 1 | 3.55 | 1.77 | 4.70 | 92 | auto | Block=10, Hiding=2 |  |
| HasumiLeia | 舞動 | Common | 1 | 2.98 | 1.77 | 3.67 | 98 | auto | Block=8, Energy=1 |  |
| SakurabaEma | 說服 | Uncommon | 1 | 3.54 | 1.77 | 7.08 | 92 | agent-high | Cards=1, Energy=1 | agent：多人限定；多人：雙方下回合抽1/2+1/2能量 |
| SaekiMiria | 行走的不幸 | Uncommon | 1 | 3.52 | 1.76 | 4.02 | 92 | agent-medium | Unlucky=2, WalkingMisfortune=1 | agent：每回合+1能量約4.5，厄運自傷扣分 / 回合模型：4.5→4.37 回合 |
| SakurabaEma | 奈葉香是機器人 | Uncommon | 1 | 3.45 | 1.72 | 4.16 | 92 | agent-high | Block=10, Cards=2, Sus=1, Artifact=1 | agent：v3隊友:特定隊友機率=1.5名隊友/17≈0.088（2.5人隊；單人0）；奈葉香得1人工製品約0.5×0.088≈+0.05 |
| NikaidoHiro | 打擊-人 | Common | 1 | 2.79 | 1.65 | 3.46 | 96 | agent-medium | Damage=8, Cards=2 | agent：8傷1.33+抽2 1.0+鋼筆0.25+架式切換0.1 |
| SaekiMiria | 監獄餐 | Common | 0 | 2.61 | 1.55 | 4.02 | 92 | auto | Healing=3, Poison=3, Majoka=5 |  |
| HikamiMeruru | 藥水蒐集 | Common | 1 | 2.60 | 1.54 | 3.66 | 92 | agent-medium | custom:Potion=2 | 期望值模型：隨機基礎藥水（以平均價值計） / agent：隨機生成 2/3 瓶基礎藥水，受欄位上限限制，可配合合成與催化劑 |
| SakurabaEma | 充耳不聞 | Common | 1 | 2.60 | 1.54 | 3.31 | 92 | agent-high | TrialBlock=8, Weak=2 | agent：8/11格擋+虛弱2/3 |
| SakurabaEma | 緊勒 | Common | 1 | 2.57 | 1.53 | 3.50 | 92 | agent-medium | Damage=9, Strangle=2 | agent：9/12傷+本回合後續每牌失2/3血（約3張） |
| TachibanaSherry | 萬能鑰匙 | Common | 1 | 2.52 | 1.50 | 3.09 | 92 | agent-medium | Block=5, Strength=1, gen:Hand:cls:BrokenLock=1 | 期望值模型：生成壞掉的鎖＋1 力量 / agent：v5模擬: 條件 1 力量改用啟用率 0.27(原假設 0.35 且力量 1.4)；5 格檔+壞掉的鎖抽 2 / agent：v5模擬: 條件 1 力量改用啟用率 0.27(原假設 0.35 且力量 1.4)；5 格檔+壞掉的鎖抽 2 |
| SakurabaEma | 假摔 | Common | 0 | 2.45 | 1.45 | 3.85 | 91 | agent-high | custom:HpLoss=2, Energy=2, Sus=1 | agent：2/3能量−2血−嫌疑風險；希羅隊友時額外給牌 |
| HasumiLeia | 聳肩無視 | Common | 1 | 2.41 | 1.43 | 3.09 | 90 | auto | Block=8, Cards=1 |  |

## 模組卡：可能過弱

| 卡池 | 卡名 | 稀有度 | 費用 | 分數 | 相對強度 | 升級後 | 百分位 | confidence | 數值 | 備註 |
|---|---|---|---|---|---|---|---|---|---|---|
| SaekiMiria | 胡椒拉麵 | Uncommon | 1 | 1.28 | 0.64 | 2.82 | 10 | auto | Healing=3, Poison=1 |  |
| Deprecated | 不當判決 | Uncommon | 2 | 1.27 | 0.64 | 3.21 | 10 | agent-low | Cards=1 | agent：v4: Deprecated；目標受強化攻擊傷害×2至其回合結束，不疊乘。單人只翻倍剩餘能量的攻擊（2費剩1能量≈+6傷；升級1費剩2能量≈+12傷）；多人隊友攻擊也翻倍，約+2.0 |
| SakurabaEma | 小笨狗？ | Uncommon | 1 | 1.26 | 0.63 | 1.65 | 9 | agent-low | Kiang=1 | agent：v5模擬:模擬未涵蓋此卡主要機制（受傷減免／希羅艾瑪狗；保留v3_ally特定隊友0.088），沿用前版數值 |
| HikamiMeruru | 梅露露醫護室 | Uncommon | 2 | 1.25 | 0.62 | 2.60 | 9 | agent-low | MeruruInfirmary=1 | agent：v3補血: 止痛藥回復量為受傷一半，約 4 瓶×2 血=8 血，回血部分每點 +0.12（逐次小額、溢補少）→ +1.0；升級全隊 4 人×八折 → 多人升級 +3.2 |
| TachibanaSherry | 何意味 | Uncommon | 1 | 1.24 | 0.62 | 1.59 | 9 | agent-medium | Weak=1 | agent：v5模擬: 條件 2 力量改用啟用率 0.27≈0.65；1 虛弱+消耗手牌 |
| ShitoAlisa | 大炎上 | Uncommon | 3 | 1.24 | 0.62 | 1.59 | 9 | agent-medium | Burn=4 | agent：全體4/6灼燒+1火球；自帶燃燒2可免費重複打出；v2灼燒每層0.26對齊中毒 |
| HasumiLeia | 拋接 | Uncommon | 1 | 1.24 | 0.62 | 1.24 | 9 | agent-medium |  | agent：換牌約2張，品質提升有限 |
| TachibanaSherry | 三！找到線索！ | Uncommon | 3 | 1.22 | 0.61 | 1.49 | 8 | agent-medium | Damage=12 | agent：v2: 12 全體傷 2.8+1 力量，減費不計 |
| SawatariCoco | 搗蛋貓咪褲子 | Uncommon | 2 | 1.21 | 0.60 | 1.89 | 8 | agent-medium | Poison=3, Burn=3 | agent：全體 3/5 中毒+3/5 灼燒 |
| SaekiMiria | 身體借一下 | Uncommon | 1 | 1.20 | 0.60 | 1.84 | 8 | agent-low |  | agent：限多人；借隊友力量一回合 |
| SaekiMiria | 公平 | Uncommon | 1 | 1.20 | 0.60 | 1.86 | 8 | agent-low |  | agent：限多人；抽1並平分生命 |
| ShitoAlisa | 延燒 | Uncommon | 2 | 1.19 | 0.60 | 1.39 | 8 | agent-low | custom:FireballGainPerSpread=1 | agent：複製目標灼燒到其他敵人，單體戰無效；每擴散給1/2火球；v2灼燒每層0.26對齊中毒 |
| SawatariCoco | 無限鏡像 | Uncommon | 2 | 1.18 | 0.59 | 2.12 | 8 | agent-medium |  | agent：v5模擬: 打出時平均直播 5.1 層、翻倍增加 5.2 層（直播型）；裝備/起始型約 2.5 層。剩約 4 回合截斷後回合結束傷害增加約 10~20，並延長直播連動；升級僅降費 / 回合模型：4→4.37 回合 |
| SawatariCoco | 神秘貓咪鞋子 | Uncommon | 0 | 1.15 | 0.57 | 1.57 | 7 | agent-medium | Energy=1 | agent：v5模擬: 打出時直播 90~99%；+1 能量×0.92+2000 分 0.3；升級不消耗 |
| HasumiLeia | 舞台的中心 | Uncommon | 2 | 1.15 | 0.57 | 2.29 | 7 | agent-high | TheCenter=1 | agent：僅隊友指定你時觸發，單人0；v2:MultiplayerOnly，單人null |
| ShitoAlisa | 掃除 | Uncommon | 1 | 1.15 | 0.57 | 1.72 | 7 | auto | Cards=2 |  |
| Common | 高牆 | Uncommon | 2 | 1.14 | 0.57 | 2.05 | 7 | agent-low | Wall=1 | agent：未格擋攻擊轉為等量活力，需要先受傷；升級只降費 |
| TachibanaSherry | 棉花 | Common | 0 | 0.96 | 0.57 | 1.31 | 7 | agent-medium | Clue=2 | agent：2 疑點+消耗手牌（清廢牌小正值） |
| TonoHanna | 惡魔的骰子 | Common | 1 | 0.93 | 0.56 | 1.68 | 7 | agent-medium | custom:EnemyTempStrengthDown=3.5 | 期望值模型：1d6 期望 3.5 點敵人暫時減力量（升級的額外效果未計） / agent：敵人本回合失去約3.5力量；升級額外生成虛無天使骰子。 |
| JogasakiNoah | 水墨 | Uncommon | 1 | 1.10 | 0.55 | 1.64 | 5 | agent-medium |  | agent：選白：抽2(升級3)，或黑：10/15魔女化，取較佳 |
| JogasakiNoah | 抽血 | Common | 2 | 0.91 | 0.54 | 1.52 | 6 | agent-low | Repeat=2, Damage=3 | agent：失3血，生成2顆血球(供血液攻擊加傷及回血)；升級3顆 |
| HikamiMeruru | 超級拼裝 | Rare | 3 | 1.09 | 0.53 | 1.53 | 8 | agent-low | custom:NanokaPieces=35 | 隨機不影響期望值：只隨機插入位置 / agent：消耗手牌，抽牌堆變 35/25 張垃圾加 5 部件，集齊即勝；長戰保底，但多回合空轉 |
| ShitoAlisa | 魔術師之紅 | Uncommon | 3 | 1.05 | 0.52 | 1.57 | 5 | auto | Damage=20 |  |
| TonoHanna | 吃完有獎勵 | Uncommon | 0 | 1.05 | 0.52 | 1.70 | 5 | agent-high | Cards=2 | agent：0費抽2/3，附毒素狀態。 |
| TonoHanna | 米莉亞人偶 | Uncommon | 0 | 1.05 | 0.52 | 1.70 | 5 | agent-low | custom:Transform=2 | 期望值模型：變化手牌為隨機人偶（收藏能力另計） / agent：變化手牌最多2張為人偶牌，升級後一併升級。 MultiplayerOnly，單人卡池移除。 |
| SawatariCoco | 搗蛋貓咪頭飾 | Uncommon | 1 | 1.04 | 0.52 | 1.32 | 5 | agent-medium | Weak=1 | agent：v5模擬: 打出時直播約 90%，追加易傷幾乎必定觸發；1/2 虛弱+消耗 1 手牌+易傷 |
| Common | 安眠藥 | Common | 0 | 0.87 | 0.52 | 1.02 | 5 | auto | Cards=1, Regen=1 |  |
| Common | 不在場證明 | Uncommon | 1 | 1.03 | 0.51 | 1.60 | 5 | auto | Cards=2, Sus=2 |  |
| HoshoMago | 惡魔 | Ancient | 1 | 1.00 | 0.51 | 1.40 | 5 | agent-low |  | agent：三選一：發狂系雙面效果或全場血量降至50%（非Boss），高變異。 |
| KurobeNanoka | 壓制射擊 | Uncommon | 3 | 1.02 | 0.51 | 1.53 | 4 | agent-medium | Damage=4, custom:BulletCost=3 | agent：4x3傷+暈眩約2.5，扣3子彈1.8；升級降費 |
| HasumiLeia | 智慧長矛 | Uncommon | 1 | 0.99 | 0.49 | 1.48 | 4 | agent-low | SmartSpear=1 | agent：抽零件時拉其他零件，加速組矛 |
| HasumiLeia | 樹枝 | Common | 0 | 0.80 | 0.47 | 0.94 | 3 | agent-low |  | agent：本回合二刀流，劍技追加30%傷 |
| TachibanaSherry | 約會時光 | Rare | 3 | 0.97 | 0.47 | 1.14 | 5 | agent-medium | Heal=5 | agent：v5模擬: 沿用 v3 一次性回血 0.28/血；模擬中期(第 3~4 回合)力量約 4~5，回復 5+力量≈9.5 血(2.6)；放大鏡清空魔女化幾乎恆滿足無魔女化條件；升級保留 +0.2 |
| SakurabaEma | 念寫相片 | Rare | 3 | 0.96 | 0.46 | 1.68 | 4 | agent-low | Majoka=100 | agent：v5模擬:模擬未涵蓋此卡主要機制（魔女化100與結束回合的取捨），沿用前版數值 |
| SawatariCoco | 搗蛋貓咪上衣 | Common | 0 | 0.76 | 0.45 | 0.90 | 3 | agent-medium | Vulnerable=1, Weak=1 | agent：0 費 1 易傷+1 虛弱 |
| KurobeNanoka | 最強情報魔法 | Uncommon | 1 | 0.90 | 0.45 | 1.08 | 2 | agent-low |  | agent：僅顯示牌堆頂資訊 |
| TonoHanna | 梅露露人偶 | Rare | 1 | 0.93 | 0.45 | 1.43 | 4 | agent-low | MeruruPuppetCollection=1 | agent：從消耗牌堆取回一張，早期常無好目標。 |
| TonoHanna | 可可人偶 | Uncommon | 1 | 0.89 | 0.44 | 1.39 | 2 | agent-medium | Block=2, CocoPuppetCollection=1 | agent：能力：每打人偶牌+2/3格檔，剩餘回合約2~3次觸發。 |
| HikamiMeruru | 殉情 | Common | 1 | 0.71 | 0.42 | 0.71 | 0 | agent-low | DoubleSuicide=15 | agent：敵人本回合每次受擊 +3 傷，但自身也受 +3，且雙方回復 15%/25% 生命，淨值低 |
| NatsumeAnan | 舊日支配者召喚書 | Rare | 3 | 0.87 | 0.42 | 1.63 | 3 | agent-low |  | agent：換成強力古神牌組但花能量達 X 即死；需約 14 言靈才可能集齊秒殺 |
| TachibanaSherry | 狂戰士之魂 | Uncommon | 2 | 0.84 | 0.42 | 1.10 | 2 | agent-low |  | 隨機不影響期望值：隨機目標 / agent：v5模擬: 貪婪策略極少打出無法直接量測；以模擬牌組攻擊牌比例 0.55~0.63 解析，期望連打 1.2~1.7 張(≈1.7)扣捨棄剩餘手牌約 -0.3；升級保留 +0.3 |
| JogasakiNoah | 赫鱗躍動 | Rare | 3 | 0.87 | 0.42 | 1.30 | 3 | agent-low |  | agent：每回合激發2血球回6血並重生、層數成長，但遺物不再產顏料球；升級降費 / 回合模型：4.5→4.37 回合 |
| SakurabaEma | 劍鞘 | Rare | 1 | 0.87 | 0.42 | 1.03 | 3 | agent-medium |  | agent：只影響隊友殺人衝動，單人0 |
| HasumiLeia | 攜帶式製作站 | Uncommon | 1 | 0.82 | 0.41 | 0.98 | 2 | agent-low | PortableFletchingStation=1 | agent：製作長矛時機率多一支，依賴長矛套路 |
| KurobeNanoka | 瞄準 | Common | 1 | 0.69 | 0.41 | 1.15 | 0 | auto | Accurate=60 |  |
| SakurabaEma | 深思熟慮 | Uncommon | 1 | 0.80 | 0.40 | 1.18 | 2 | agent-medium |  | agent：任意棄抽，升級去消耗 |
| KurobeNanoka | 耍帥 | Rare | 2 | 0.81 | 0.39 | 1.20 | 2 | agent-medium | ActingCool=40 | agent：看守在場時每回合40/60精準(≈0.7/1.0)，約2回合在場且需開槍 / 回合模型：4.5→4.37 回合 |
| SakurabaEma | 魔女島的藏寶圖？ | Rare | 3 | 0.80 | 0.39 | 1.44 | 2 | agent-low | BadEndSkill=1 | agent：v5模擬:模擬未涵蓋此卡主要機制（壞結局技能），沿用前版數值 |
| SawatariCoco | 不經大腦 | Common | 1 | 0.64 | 0.38 | 1.28 | 0 | agent-medium | Strength=2, custom=2 | agent：2 永久力量換 2/1 敏捷；可可格檔多靠躲藏，敏捷損失略輕 |
| TachibanaSherry | 試試看吧 | Rare | 3 | 0.77 | 0.37 | 1.44 | 2 | agent-medium | Majoka=100 | agent：v5模擬: 打出後本回合魔女化 100(傷害倍率 1.84x)；首次取得超級拳時模擬中 94% 當回合打出強化超級拳(場均約 45 全體傷+5 力量)，此後超級拳已在牌組，同回合在手機率約 0.3×(額外約 34 傷+5 力量≈10)；3 費後通常無剩餘能量吃倍率，升級剩 1 能量 +0.6；多人魔女化 100 觸發殺 |
| TonoHanna | 多餘的能量可以放冰箱嗎 | Rare | 3 | 0.75 | 0.36 | 1.16 | 2 | agent-medium | EnergyInFridge=1 | agent：-2力量，每回合+1能量並保留能量，剩約4.5回合。 / 回合模型：4.5→4.37 回合 |
| Common | 世界征服者 | Rare | 0 | 0.75 | 0.36 | 1.41 | 2 | agent-medium | Cards=1, custom:MajokaPower=250 | agent：0 費抽 1/2；全場魔女化達每玩家 250 才強制勝利，一般極難達成；多人：門檻變為 250×4 魔女化 |
| SakurabaEma | 煙滅證據 | Uncommon | 1 | 0.71 | 0.35 | 0.94 | 2 | agent-medium | Cards=2, Sus=1 | agent：消耗2/3張牌瘦身+嫌疑 |
| HoshoMago | 審判 | Ancient | 1 | 0.67 | 0.35 | 1.03 | 1 | agent-low |  | agent：清除減益並依數量回能量，典型約0.7個減益。 |
| SawatariCoco | 真拿你沒辦法 | Uncommon | 2 | 0.69 | 0.35 | 0.88 | 2 | agent-low | FourDPocket=1 | agent：v5模擬: 全龐克牌組中約 65% 的打出補齊 1 件並完成套裝（+5 力量），套裝完成 0.3 次/場；一般牌組同系列件數少，依牌組大幅變動 |
| SawatariCoco | 電幻貓咪裙子 | Common | 1 | 0.57 | 0.34 | 0.57 | 0 | auto | Cards=1 |  |
| HikamiMeruru | 冰↗上↘梅↗露↘露 | Rare | 3 | 0.71 | 0.34 | 1.06 | 1 | agent-low | Majoka=100 | agent：魔女化補到 100 但立即結束回合，損失本回合剩餘行動；梅露露輸出低，下修 |
| Deprecated | 關鍵證物 | Uncommon | 1 | 0.67 | 0.33 | 0.83 | 2 | agent-high | Damage=4 | agent：v4: Deprecated；4/5傷害 |
| TachibanaSherry | 信賴之躍 | Common | 0 | 0.56 | 0.33 | 1.69 | 0 | agent-medium | Energy=2 | agent：v2: 2 能量 − 2 永久力量 2.4，對比 Inflame 尺度近乎負收益 |
| HasumiLeia | 長矛精通 | Uncommon | 1 | 0.66 | 0.33 | 0.79 | 2 | agent-low | SpearMastery=1 | agent：簡易長矛升級，需長矛來源 |
| HikamiMeruru | 藥頭 | Uncommon | 2 | 0.64 | 0.32 | 0.89 | 2 | auto | custom:Potion=1 | 期望值模型：隨機基礎藥水 1 瓶（單人）；升級 50% 催化劑 |
| KurobeNanoka | 架槍 | Rare | 2 | 0.64 | 0.31 | 1.15 | 1 | auto | Energy=1 |  |
| JogasakiNoah | 血刃 | Common | 2 | 0.49 | 0.29 | 0.73 | 0 | agent-low | custom:BloodOrbPassive=4 | agent：血球層數+4(平均約1顆)，轉為下次血液攻擊加傷 |
| SawatariCoco | 殺戮尖塔II | Rare | 3 | 0.59 | 0.29 | 0.74 | 0 | agent-low |  | agent：v5模擬: 貪婪策略幾乎不打出（0.03 次/場），模擬無法評估取捨，沿用 v2 |
| HasumiLeia | 世界的中心是... | Rare | 3 | 0.59 | 0.28 | 0.88 | 0 | agent-medium |  | agent：需隊友選擇，單人無效 |
| Common | 罪證質疑 | Uncommon | 1 | 0.56 | 0.28 | 0.67 | 1 | agent-medium |  | agent：v4: MultiplayerOnly 單人 null。指定隊友手牌得2張罪證嫌疑（虛無消耗狀態，留手回合末+1罪證；打出需1能量再轉給別人）。罪證本身惰性，只在罪犯判決（第5/3回合後）決定真犯人（+1能量、+2攻防、受傷+2）或圖書館審判戰時有用，且與敵人罪證競爭。升級僅-1費，效果值不變 |
| KurobeNanoka | 遺物幻視 | Uncommon | 3 | 0.56 | 0.28 | 0.84 | 1 | agent-low | custom:ExtraRolls=1, custom:RelicChance=50 | agent：戰鬥結束50%得遺物(升級降費)，戰內無效果；以戰外價值粗估 |
| SaekiMiria | 營養均衡 | Rare | 2 | 0.58 | 0.28 | 1.04 | 0 | agent-low | custom=5, Poison=3 | agent：永久最大生命戰鬥內低值，中毒3扣分 |
| SaekiMiria | 和事佬 | Uncommon | 2 | 0.55 | 0.28 | 0.83 | 1 | agent-low | Peacemaker=2 | agent：v4: 敵人每次獲得力量減2(3)；僅對會疊力量的敵人有效，平均約0.8/1.2；多人保護全隊×1.5 |
| SawatariCoco | 逃離魔女島 | Rare | 1 | 0.56 | 0.27 | 0.67 | 0 | agent-low |  | agent：v5模擬: 模擬未建模監獄來源，沿用 v3補血：僅有監獄時可用，回血量=監獄層數，每點 0.28 |
| HikamiMeruru | 魔女的大鍋 | Rare | 2 | 0.56 | 0.27 | 1.00 | 0 | agent-medium |  | agent：火力提升至 100，後續每瓶藥水必得催化劑（約 0.6×3，扣時機）；升級降費 |
| TonoHanna | 晚安大小姐 | Rare | 3 | 0.55 | 0.27 | 0.83 | 0 | agent-low |  | agent：v3補血: 消耗回 2+收藏層數約 6 血，每點 +0.18 → +1.0；結束回合代價不變；升級降費 |
| TachibanaSherry | 六！勇敢面對！ | Uncommon | 6 | 0.53 | 0.27 | 0.84 | 1 | agent-medium | Damage=24 | agent：v5模擬: 六需本場打過三才 0 費；含三的連段牌組中每次抽到可打出率 44%(每場 1.2 次、場均 32 傷)，無三的牌組 0%(6 費打不出)；24 傷 4.0×0.44 − 死牌 0.17，再依約八成牌組會配三折算；升級 34 傷 |
| HikamiMeruru | 添柴 | Basic | 1 | 0.30 | 0.26 | 0.41 | 0 | agent-medium | custom:Firepower=10 | agent：+10/15 火力，需靠後續藥水使用才換成催化劑；滿火力時改得催化劑 |
| JogasakiNoah | 定格畫面 | Rare | 3 | 0.52 | 0.25 | 0.78 | 0 | agent-low |  | agent：最大能量-1代價大，換一隻普通怪友軍；精英/頭目不可用 |
| TachibanaSherry | 九！奇蹟的搭檔！ | Uncommon | 9 | 0.42 | 0.21 | 0.53 | 0 | agent-medium | Damage=14 | agent：v5模擬: 九只在神探當賞時可打出(0 費)；抽到後實際打出率 疑點型 40%/連段型 31%/力量型 15%，均約 30%；14 全體傷 3.3×0.3 − 未打出死牌 0.7×0.3；升級 18 傷 |
| TachibanaSherry | 小仙子 | Uncommon | 2 | 0.38 | 0.19 | 0.69 | 0 | agent-medium | Energy=1, LittleFairy=1 | agent：v5模擬: 每場打出後剩餘回合累積力量僅換得 0.35~0.9 能量(均約 0.67)：雪莉單場獲得力量 6.9~7.8(含放大鏡起始 1)，且多在打出前取得；升級降費不計入 value |
| TachibanaSherry | 投降 | Uncommon | 1 | 0.35 | 0.18 | 1.06 | 0 | agent-medium | Block=13 | agent：v2: 13 格檔 2.6 − 2 永久力量 2.4 |
| HikamiMeruru | 原始藥水力量 | Rare | 3 | 0.35 | 0.17 | 0.52 | 0 | agent-low |  | agent：所有藥水變石頭，單獨使用為負；配藥水投擲可打出遞增的滾石 AoE；升級降費 |
| ShitoAlisa | 終焉的倒數計時 | Rare | 3 | 0.28 | 0.14 | 0.42 | 0 | agent-low | Damage=20, FinalCountdown=20 | agent：自傷20，倒數20回合後強制勝利，燃燒歸零可加速；一般戰鬥難兌現 |
| SawatariCoco | 轉守為攻 | Uncommon | 2 | 0.27 | 0.13 | 0.48 | 0 | agent-medium |  | agent：v5模擬: 打出時躲藏平均 7.5 層→3.5 力量、剩約 5 回合；失去每回合 7.5 格檔+7.5 魔女化，淨值低；邊際模擬為負（約 -0.9/次，雜訊大） |
| SakurabaEma | 要求再審 | Rare | 3 | 0.26 | 0.12 | 0.35 | 0 | agent-medium | custom:RequiredSuspicion=10 | 隨機不影響期望值：非隨機 / agent：v5模擬:需10嫌疑：模擬中嫌疑來源（證言/證物/可疑）每次+1，打出時嫌疑平均<1.5，≥10幾乎不發生；需專門堆嫌疑牌組 |
| HasumiLeia | 巡迴演出 | Common | 1 | 0.19 | 0.12 | 0.29 | 0 | auto | Gold=10 |  |
| SakurabaEma | DArime | Rare | 2 | 0.22 | 0.11 | 0.40 | 0 | agent-medium | TrialDamage=13, custom:Progress=20 | agent：v5模擬:需進度≥20：打出時20%、整場49%；升級保留可等到20；連抽裁判牌期望約0.7段×13傷並消耗之 |
| SawatariCoco | 感謝打賞 | Uncommon | 1 | 0.21 | 0.10 | 0.35 | 0 | agent-medium |  | agent：只給金幣，戰鬥內無效 |
| Common | 罪證確鑿 | Uncommon | 1 | 0.19 | 0.10 | 0.23 | 0 | agent-medium |  | agent：罪證與嫌疑互換，只有 Ema 體系有意義；升級降為 0 費 |
| Common | 真兇判定 | Uncommon | 1 | 0.16 | 0.08 | 0.25 | 0 | agent-low | custom:RequiredRound=5 | agent：v2: 非 Ema 時敵人常並列獲真兇（攻擊+2），淨效益更低 |
| TonoHanna | 蕾雅人偶 | Uncommon | 1 | 0.16 | 0.08 | 0.16 | 0 | agent-high | Gold=10 | agent：僅戰後金幣，戰鬥價值只剩收藏層數。 |
| KurobeNanoka | 牌堆射擊 | Uncommon | 0 | 0.14 | 0.07 | 0.33 | 0 | agent-medium | custom:ExhaustedCards=3, custom:BulletCost=1 | agent：消耗抽牌堆頂3張薄牌，但耗子彈 |
| HoshoMago | 節制 | Ancient | 3 | 0.09 | 0.05 | 0.14 | 0 | agent-medium | Temperance=30, custom:JusticeBlock=10 | agent：戰鬥結束30金幣，無戰鬥效果；有正義+10格。升級降費。 |
