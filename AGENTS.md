# Manosaba 卡牌與能力製作指南

本指南適用於在 Manosaba 專案中新增或修改卡牌、能力（Power）、關鍵字及其本地化內容。依照使用者當前提供的需求工作；描述可以是自然語言、表格、清單或類似下方的卡牌規格。只處理使用者目前要求的項目。

## 如何判讀卡牌製作需求

- 將卡名、角色／卡池、效果、升級效果、費用、類型、稀有度、目標與備註視為可能提供的規格欄位。欄位順序和標籤不必完全一致，也可能以段落、表格或簡短敘述提供。
- 識別新增卡牌、修改既有卡牌、修改效果、本地化修正、能力或關鍵字等意圖；同一則訊息可以包含多個項目。
- 以使用者提供的效果文字為行為依據。遇到中文效果與其他語言描述衝突時，優先遵循使用者明確指定的語意，並保持四語文案一致；無法合理判定的衝突應在交付摘要中指出。
- 未提供的技術欄位，先參考同角色或同卡池的相似卡牌及專案既有慣例。只有在缺少資訊會導致不同玩法或不可逆選擇時，才詢問使用者；否則採用最貼近既有模式的實作並簡要說明假設。
- 不要因為輸入不是固定模板或缺少某個欄位，就忽略其中可辨識的卡牌製作要求。

例如，規格可能長這樣，但不要求使用者遵循固定格式：

```text
角色：Sherry
新增卡牌：1. 某張卡
(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
中文效果：……
升級：……
備註：……
```

## 專案路徑與卡池

專案 C# 原始碼位於 `Manosaba/`。卡牌與能力通常放在 `Manosaba/Characters/<Character>/Cards/` 和 `Manosaba/Characters/<Character>/Powers/`；共用內容放在 `Manosaba/Characters/Common/Cards/`、`Powers/` 等目錄。以該角色現有檔案和 `[Pool]` 宣告判斷實際歸屬。

| 角色／常見別名 | CardPool | 原始碼目錄 |
|---|---|---|
| 共用、common | `CommonCardPool` | `Manosaba/Characters/Common/` |
| 二階堂希羅、Hiro | `NikaidoHiroCardPool` | `Manosaba/Characters/NikaidoHiro/` |
| 遠野漢娜、Hanna | `TonoHannaCardPool` | `Manosaba/Characters/TonoHanna/` |
| 橘雪莉、Sherry | `TachibanaSherryCardPool` | `Manosaba/Characters/TachibanaSherry/` |
| 澤渡可可、Coco | `SawatariCocoCardPool` | `Manosaba/Characters/SawatariCoco/` |
| 紫藤亞里莎、Alisa | `ShitoAlisaCardPool` | `Manosaba/Characters/ShitoAlisa/` |
| 蓮見蕾亞、Leia | `HasumiLeiaCardPool` | `Manosaba/Characters/HasumiLeia/` |
| 冰上梅露露、Meruru | `HikamiMeruruCardPool` | `Manosaba/Characters/HikamiMeruru/` |
| 寶生瑪格、Mago | `HoshoMagoCardPool` | `Manosaba/Characters/HoshoMago/` |
| 城崎諾亞、Noah | `JogasakiNoahCardPool` | `Manosaba/Characters/JogasakiNoah/` |
| 黑部奈葉香、Nanoka | `KurobeNanokaCardPool` | `Manosaba/Characters/KurobeNanoka/` |
| 夏目安安、Anan | `NatsumeAnanCardPool` | `Manosaba/Characters/NatsumeAnan/` |
| 佐伯米莉亞、Miria | `SaekiMiriaCardPool` | `Manosaba/Characters/SaekiMiria/` |
| 櫻羽艾瑪、Ema | `SakurabaEmaCardPool` | `Manosaba/Characters/SakurabaEma/` |

其他常用路徑：

- 本地化：`Manosaba/Manosaba/localization/{zhs,eng,jpn,kor}/`
- 卡牌圖片：`Manosaba/Manosaba/images/cards/`；`PathCustomCardModel.PortraitPath` 依卡牌 ID 推導檔名
- 卡牌大圖／肖像：`Manosaba/Manosaba/images/card_portraits/`，大圖在 `big/`
- 能力圖片：`Manosaba/Manosaba/images/powers/`，大圖在 `big/`
- 角色專屬圖片：`Manosaba/Manosaba/images/characters/<character_id>/`
- 擴充 helper：`Manosaba/Extensions/`；共用處刑卡判定：`Manosaba/Characters/Common/ExecutionCardHelper.cs`；人偶收藏邏輯：`Manosaba/Characters/TonoHanna/PuppetCollectionHelper.cs`；遊戲行為 patch：`Manosaba/Patches/`

目前各角色 `*CardPool.GenerateAllCards()` 都從 `CommonCardPool` 取得共用卡牌清單。新增卡牌仍須依實際用途選擇 `[Pool(typeof(...CardPool))]`；不要因為共用卡牌會出現在角色卡池，就把角色專屬卡放進 `Common/`。修改前先比對同角色相似卡牌、能力、圖片與本地化實作。

卡牌類別沿用 `PathCustomCardModel` 和 `[Pool]`；卡牌 ID 使用專案既有的 `MANOSABA-<SCREAMING_SNAKE_CASE>` 慣例。`PathCustomCardModel` 的預設肖像檔名由 ID 去除 `MANOSABA-` 前綴後轉成小寫，放在 `Manosaba/Manosaba/images/cards/`。能力使用 `PathCustomPowerModel` 等既有模式。

## 卡牌與能力實作

- 讓 C# 行為與本地化描述相符；可調整數值優先使用 `DynamicVars`，避免同一數值在多處硬編碼。數值本地化沿用 `{Damage:diff()}`、`{Block:diff()}` 等既有格式，並確保變數與 `CanonicalVars` 一致。
- 依專案既有模式處理 `CanonicalVars`、`CanonicalKeywords`、`OnUpgrade`、`AddKeyword`、`ExtraHoverTips`、`PathCustomCardModel` 和 `PathCustomPowerModel`。提到具有 hover 支援的能力、關鍵字、卡牌或機制時，加入相符的 `ExtraHoverTips`。
- 分清楚遊戲關鍵字與卡牌分類標籤：關鍵字用 `CanonicalKeywords`；Strike、Defend 等分類用 `CanonicalTags`（例如 `CardTag.Strike`、`CardTag.Defend`）。依相似卡牌及遊戲機制選擇，不要把分類標籤寫成描述文字來代替程式設定。
- 固有、保留、消耗等關鍵字通常由程式碼中的 `CanonicalKeywords` 或升級邏輯設定，不要把它們當成一般效果字串硬塞進 localization JSON；先查相似實作確認。
- 若卡牌涉及人偶收藏或處刑機制，先查並沿用 `Manosaba/Characters/TonoHanna/PuppetCollectionHelper.cs`、`Manosaba/Characters/Common/ExecutionCardHelper.cs` 等既有 helper。需要確認遊戲模型或 API 時，可參考當前環境可取得的反編譯原始碼；若無法取得，依本專案現有實作與可用 API 資訊處理。
- 升級數值增加可參考 `DynamicVars.X.UpgradeValueBy(N)`；升級費用可參考 `EnergyCost.UpgradeBy(...)`；升級新增固有等關鍵字由程式碼處理；移除消耗則在升級邏輯中移除 `Exhaust`。以現有 API 與同類卡牌為準。
- 若新卡沒有實際升級效果，提醒使用者確認，除非規格明確指出這是刻意設計。
- 若卡牌會指定或影響其他玩家、隊友或盟友，預設檢查並設定適當的 `MultiplayerConstraint`；若設計意圖是單人模式也可使用，於摘要說明。
- 對多人可見的選擇、重播或收藏邏輯採用確定性順序。

## 本地化

卡牌文字位於 `Manosaba/Manosaba/localization/{zhs,eng,jpn,kor}/cards.json`；能力文字也要更新相應語系的 `powers.json`。新增卡牌、能力或關鍵字時，更新所有支援語言：`eng`、`zhs`、`jpn`、`kor`。若新增的是獨立 hover tip，檢查是否應更新 `static_hover_tips.json`；卡牌關鍵字資料則在 `card_keywords.json`，先比照同類功能確認是否需要新增。

- 以使用者提供的效果語意為基準，讓四語表達一致。翻譯時沿用各語系既有術語，不要自行重譯專案既有的卡牌或能力名稱。
- JSON 使用 UTF-8 無 BOM；保留檔案原本的換行方式，並把新項目放在同角色或同功能的相關項目附近。使用可讀文字，不使用 Unicode escape。
- 引用其他卡牌或能力名稱時，查該語系對應 `*.title` 的既有譯名，使用完全一致的名稱。
- `cards.json` 放卡牌文案。`powers.json` 的變數放在 `smartDescription`，不要放在 `description`。依使用者備註指定的插入位置修改。
- 卡牌關鍵字通常不需在描述內重複敘述。除非使用者要求，升級效果通常不需額外本地化文字。

## `[gold]` 標籤

新增或修改描述前，先在各語系的現有 `cards.json`／`powers.json` 搜尋同一術語、卡名或相似效果，參考現行標記方式，再依本節的四語一致性原則處理。不要只憑詞彙猜測標記方式；若使用者提供的文案已包含 `[gold]`，保留其意圖，再與相鄰條目核對。

以下是搜尋方向，不是取代實際查證的硬性清單：

- 原版機制，例如力量、格檔、敏捷、虛弱、易傷、脆弱、荊棘、再生、灼燒、翱翔、暈眩。
- Manosaba 機制，例如魔女化、嫌疑、疑點、調查時刻、神探當賞、抑制力、召喚、人偶收藏、處刑卡，以及劇情專有名詞。
- 牌堆名稱與關鍵字，例如手牌、棄牌堆、抽牌堆、消耗牌堆、消耗、變化、狀態牌等；依相鄰卡片確認專案是否將該詞標記。
- 被引用的卡牌、角色、衍伸卡與物件名稱：查 `MANOSABA-XXX.title`，沿用該語系的原文和標記方式。升級衍伸卡文字可參考 `{IfUpgraded:show:名稱+|名稱}` 的既有用法。
- Alisa 燃燒相關用語，例如燃燒、點火、火球環繞，依 Alisa 既有卡片及四語術語核對。

純數字、量詞及一般動詞通常不標記。若無法確定，搜尋同角色近期相似卡牌並依專案現行用法處理。

### 專案中實際使用的文字標籤

- `[gold]...[/gold]`：關鍵字、能力／卡牌名稱、牌堆名稱及有 hover 支援的機制詞。標記完整術語，不要把整句效果一律標金色。
- `[blue]...[/blue]`：主要用於明確呈現的數值與數值變數，尤其是 Power `smartDescription`，例如 `[blue]{Amount}[/blue]`、`[blue]{DamageDecrease}%[/blue]`。部分卡牌也用藍色標示固定效果數值。
- `[blue]` 偶爾也用在指定的角色、召喚物或特殊物件名稱；這是個別文案用法，不代表所有角色或卡牌名稱都應標藍色。具 hover／關鍵字語意的詞仍優先依 `[gold]` 規則處理。若遇到既有例子互相衝突，參考同類機制並採用此處的通則；不要照搬孤立例外（例如 `MANOSABA-COMEDIAN_POWER.smartDescription` 將「格檔」標藍色的用法）。
- 卡牌 `{Var:diff()}` 在現有文案中多半不包藍色；不要為了讓所有檔案外觀一致而任意替卡牌變數加色。新增時依相同欄位、相似卡牌的用法；Power `smartDescription` 則沿用藍色數值標記。
- `[red]...[/red]`：負面數值或失去生命等內容；僅在相同類型文案使用時採用。
- `[jitter]...[/jitter]`：抖動／強調文字效果，專案目前用於少數敘事文字；一般卡牌描述不要自行加入。

### 四語標記一致性

- 以 `zhs` 的效果語意和標記作為起點，讓 `eng`、`jpn`、`kor` 中對應的關鍵字、機制、卡名及數值也有相同語意的標記；沿用各語系既有術語和自然語序。
- 對齊的是「哪些語意片段要標記」，不是每種語言的標籤數量或字面位置。不同語言可能需要包住不同長度的詞組，不要為了湊相同標籤數而破壞翻譯。
- 若同一個 localization key 的現有翻譯標記不一致，新增或修改該條目時依上面的通則一併校正四語同一 key；不要把某個語言既有的漏標或孤立特殊用法複製到其他語言。
- 確認每個標籤都有成對的開閉標記、巢狀順序正確。一般讓 `[blue]` 數值與 `[gold]` 術語各自標記，不要互相包住；只有複製既有且確有需要的格式時才使用巢狀標籤，例如敘事文字中的 `[jitter][red]文字[/red][/jitter]`。
- `{Var:diff()}`、`{Var:energyIcons()}`、`{IfUpgraded:show:升級文字|一般文字}` 是本地化格式器，不是方括號顯示標籤；沿用同類欄位格式及對應 `CanonicalVars`。

## 完成時

完成規格涵蓋的程式碼、必要能力、四語本地化與相關 helper／衍伸卡更新。最後簡要列出修改內容、採用的重要假設，以及尚未處理或需要使用者決定的事項。除非使用者明確要求，不要建立 git commit。

## 音效與 BGM

- 新增或修改音效／BGM 檔案及播放程式碼時，必須先依照 `Manosaba/Audio/README.md` 接入，並檢查資源路徑、音量設定、播放頻率設定、循環設定與播放結束清理。
- 自訂音訊應經由專案既有的 `SfxCmd.Play`／`GodotSfxRouter` 路由，確保套用 Manosaba 音量設定及 BGM 暫停／恢復行為。
