# 論破 Cut-in 原型

## 九種可重用版本與測試卡

其他卡牌在 `OnPlay` 中引用 `Manosaba.Characters.Common.Commands`，直接呼叫：

```csharp
await RefuteCutInCmd.Play(RefuteCutInVariant.Hiro3);
await RefuteCutInCmd.Play(RefuteCutInVariant.WitchHiro2);
```

每次呼叫會自行建立並清理演出，等待播放完成，套用模組音效設定；不必複製場景、手動載入貼圖或管理圖層。省略參數時仍播放 `Ema3`。

在戰鬥控制台輸入以下命令：

| 演出 | 共用介面列舉值 | 測試卡命令 |
| --- | --- | --- |
| 艾瑪立繪 1 | `Ema1` | `card MANOSABA-TEST_REFUTE_CUT_IN_EMA1` |
| 艾瑪立繪 2 | `Ema2` | `card MANOSABA-TEST_REFUTE_CUT_IN_EMA2` |
| 艾瑪立繪 3 | `Ema3` | `card MANOSABA-TEST_REFUTE_CUT_IN_EMA3` |
| 希羅立繪 1 | `Hiro1` | `card MANOSABA-TEST_REFUTE_CUT_IN_HIRO1` |
| 希羅立繪 2 | `Hiro2` | `card MANOSABA-TEST_REFUTE_CUT_IN_HIRO2` |
| 希羅立繪 3 | `Hiro3` | `card MANOSABA-TEST_REFUTE_CUT_IN_HIRO3` |
| 魔女化希羅立繪 1 | `WitchHiro1` | `card MANOSABA-TEST_REFUTE_CUT_IN_WITCH_HIRO1` |
| 魔女化希羅立繪 2 | `WitchHiro2` | `card MANOSABA-TEST_REFUTE_CUT_IN_WITCH_HIRO2` |
| 魔女化希羅立繪 3 | `WitchHiro3` | `card MANOSABA-TEST_REFUTE_CUT_IN_WITCH_HIRO3` |

九張測試卡均為 0 費、無戰鬥效果，不進入獎勵、隨機生成及圖鑑；刻意沒有升級效果。卡圖取自各版本的實際演出渲染，四種語系均已補齊。

三組原始 Prefab 分別為 `ObjectionCutIn_Ema`、`ObjectionCutIn_Hiro`、`ObjectionCutIn_CreatureHiro`。原作表情槽的順序對應列舉中的 1～3。希羅使用其原本的紅色玻璃、裂紋、碎片與剪影；魔女化版另外依原作 Control Track 播放蝴蝶、火星與紅色光暈粒子。未啟用的原作備用特效不播放。

重新取出某一角色資料時，執行轉換工具並加上 `--character Ema`、`--character Hiro` 或 `--character CreatureHiro`。分別輸出 `original_timeline.json`、`hiro_timeline.json`、`creature_hiro_timeline.json`。執行端只依賴這些已轉換資源，無需 UnityPy。

`Tools/RefuteCutInVerify.gd` 現在會檢查全部九種版本的立繪互不混用、專用音效、魔女化粒子與實際播放清理。`--capture` 可產出九種版本的渲染預覽。粒子為 Unity 資料在 Godot 中的重建，保留發射時段、素材、大小、壽命、顏色與蝴蝶動畫格，3D 粒子投影與亂數軌跡仍有引擎差異。

測試卡：`TestRefuteCutInEma3`（0 費技能，僅供開發者測試，無戰鬥效果）。
進入戰鬥後，在開發者控制台輸入：

```text
card MANOSABA-TEST_REFUTE_CUT_IN_EMA3
```

打出後等待約 3.70 秒。可再次生成卡牌重播。測試卡不會出現在獎勵、隨機生成或圖鑑中，且刻意不提供升級效果。

`RefuteCutInCmd.Play()` 可供正式卡牌重用。場景附屬戰鬥房間，離開房間時會連同音效一併清理。動畫以實際畫面時間播放，不受快速模式壓縮；出牌流程等待演出結束。獨立 CanvasLayer 不接收滑鼠輸入，1920×1080 設計座標等比例縮放。

第二版直接解析遊戲本體的以下檔案（唯讀，不修改原作）：

```text
E:\SteamLibrary\steamapps\common\manosaba_game\manosaba_Data\StreamingAssets\aa\StandaloneWindows64\naninovel-spawn_assets_all.bundle
```

其中 `ObjectionCutIn_Ema` Prefab（path ID `943287031862258280`）引用 `Ema_CutIn_Timeline`（`-6978372974781963798`）。已取出 10 個 AnimationClip 的串流立方曲線、Timeline 綁定、Sprite Pixels Per Unit、圖層順序、啟用與透明度、原始兩段音效，以及粒子參數。`original_timeline.json` 為原始曲線每秒 60 格取樣，遊戲端不需要 UnityPy。

可重建資料：安裝 UnityPy 後，執行 `Tools/ExtractOriginalRefute.py --bundle <上述檔案>`。若套件裝在獨立目錄，加上 `--unitypy-path <套件目錄>`。轉換工具會寫入本專案資源，不會寫入原作。

重要修正：

- 艾瑪三張素材屬於 `_objectLibrary` 的三種表情選項，並非依序換圖。此場景預設 `expression=2`，對應參考影片。
- 原作 Timeline 長度為 200/60 = 3.3333 秒。依影片節奏校準 `PLAYBACK_RATE=0.9`，播放約 3.70 秒；75 pixels/unit 的投影已對齊影片中的臉部與帽沿。
- 已依原作 Sprite 排序與尺寸還原白色發光剪影、僅在玻璃內顯示的深色剪影、玻璃三階段、三張依序播放的碎片圖層與全畫面閃白。
- 原始專用音效為 `Crack_01`（0.75 秒）與 `Crack_02`（1.25 秒），已取代第一版的替代音效。兩段音效跟隨模組音效音量設定。
- `Glasses_Fix_Ema` 的 `_kirakira` 實際引用檔名結尾 `kirakira2_0`，`_kirakira2` 則引用 `kirakira_0`。資料貼圖另存 RGB，避免 Godot 的透明邊緣修補改變原作 shader 所讀取的 RGB。
- 已根據編譯後 DXBC 的材質運算重建裂紋閃光、固定雜訊消散、粉紫邊緣與背景扭曲雜訊。Shader Graph 的可編輯圖本身未隨遊戲發佈。

仍有渲染差異：原作 Unity 的後處理 Bloom 未整套移植；粒子數量、壽命、尺寸、速度與種子取自原作，但 Godot 與 Unity 的亂數序列和粒子投影不同，因此小星光位置不保證逐幀相同。背景會透出目前戰鬥畫面。原始 Prefab 的兩條音軌只有上述碎裂音效；正式卡牌另行指定角色語音。

驗證：`Tools/RefuteCutInVerify.gd` 可檢查場景、素材、原作角色構圖、表情、等比例縮放、連播與離場清理。加上 `-- --capture --record` 可輸出實際渲染影格；加上 `-- --pack` 可驗證 `_mods_out/Manosaba.pck` 內資源。已分別使用 OpenGL Compatibility 與 Vulkan Forward Mobile 渲染確認。

修改場景、腳本或圖片後，除了 `dotnet build -nologo`，還需重新匯出 PCK。直接呼叫 Godot 匯出時需設定 `IsInnerGodotExport=true`，避免現有 MSBuild 發佈目標再次呼叫 Godot 匯出。

## 艾瑪正式卡牌演出

舊測試卡已統一改名為 `MANOSABA-TEST_REFUTE_CUT_IN_EMA3`，卡圖改為艾瑪立繪 3 的演出截圖。

正式卡牌由 `EmaTrialPresentation` 統一管理，僅持有者角色為櫻羽艾瑪時播放。下列 cut-in 另要求未持有「特別的魔法」。提問、發言只播放語音，不受裁判進度與特別的魔法限制。

| 卡牌 | 裁判進度 | Cut-in | 原始語音 | 專案語音檔名 |
| --- | --- | --- | --- | --- |
| 提出證據 | ≥10 | Ema2 | CommonTrial_Ema05119.ogg | present_evidence.ogg |
| 反駁 | ≥10 | Ema2 | CommonTrial_Ema05117.ogg | rebuttal.ogg |
| 贊同 | ≥10 | Ema2 | CommonTrial_Ema05121.ogg | agreement.ogg |
| 詭辯 | ≥10 | Ema1 | CommonTrial_Ema06197.ogg | sophistry.ogg |
| 提問 | 無 | 無 | CommonTrial_Ema05123.ogg | question.ogg |
| 窮舉法 | ≥10 | Ema2 | CommonTrial_Ema06196.ogg | exhaustive_method.ogg |
| 發言 | 無 | 無 | CommonTrial_Ema05124.ogg | statement.ogg |
| AccUhEarts-告- | ≥10 | Ema3 | CommonTrial_Ema06192.ogg | acc_uh_earts_indictment.ogg |
| AccUhEarts-発- | ≥10 | Ema3 | CommonTrial_Ema06195.ogg | acc_uh_earts_revelation.ogg |
| DArime | ≥20 | Ema3 | CommonTrial_Ema06197.ogg | d_arime.ogg |

語音放在 `Manosaba/audio/SFX/ema/`，由使用者提供的來源複製並依卡牌 ID 命名。兩種 AccUhEarts 與 DArime 直接使用專案 `audio/bgm/` 內相應 MP3；音樂沿用現有路由，暫停原戰鬥音樂並於戰鬥結束／離場時還原。

提出證據及反駁會先完成證據選擇，再播放各自的演出，之後才結算所選證據。共用的提出證據機制不會自動觸發「提出證據」卡的演出，因此反駁及其他呼叫者不會誤播該卡語音。沒有證據可選時直接進入該卡演出。

模組配置「艾瑪的魔女審判」控制上述語音、cut-in 碎裂音效與 BGM，選項為每次／每場冒險一次／停用；「一次」以每位玩家、每種來源卡分別計算，新冒險重置。靜音或次數用盡時仍播放符合條件的畫面。音量沿用模組音效音量。角色語音與畫面同步開始，較長台詞不會被動畫結束截斷，離開戰鬥房間會清理語音。
