# 審問開始入場演出

`InterrogationStartPower.ApplyToField()` 在場地由未持有變成持有審問開始時播放，並等待演出完成。場地已持有時不重播；`RawTellOwk` 原本增加審問進度的行為保留。

素材來源為使用者提供的 `G:/assets_manosaba_WitchTrials/Unity_Dump_Texture2D`，動畫資料由遊戲的 `naninovel-spawn_assets_all.bundle` 中 `BeginDebateText` Prefab、`Start` Timeline、`Start_Present`、`Start_Dismiss` 與背景動畫取出。原作檔案只讀。轉換工具：

```powershell
python Tools/ExtractInterrogationStart.py --bundle "E:/SteamLibrary/steamapps/common/manosaba_game/manosaba_Data/StreamingAssets/aa/StandaloneWindows64/naninovel-spawn_assets_all.bundle" --textures "G:/assets_manosaba_WitchTrials/Unity_Dump_Texture2D" --unitypy-path "E:/manosaba/build/unitypy"
```

轉換包含 42 個 UI 節點、23 個圖像實例、261 個原作 60 FPS 取樣影格、位置、縮放、轉角、RGBA、CanvasGroup 透明度與天秤旋轉約束。執行時不需要 Python 或 UnityPy。原作設計尺寸為 2560×1440；演出保持比例置中，底下透出目前戰鬥畫面。參考影片的起點相當於原作 Timeline 第 1 秒，因此從該處起播，總長約 3.33 秒。發光層使用 Godot 加算混合，與 Unity 的 HDR/Bloom 仍可能略有差異。

左右 `LibraContainer` 的 RotationConstraint 分別跟隨同側 `LibraRoot`，保留原作約 ±25 度的秤盤搖擺。所有節點的動畫取樣完成後再套用來源的全域轉角，確保來源位於目標之後時也能在同一影格正確跟隨，包含本體與發光圖層。驗證工具檢查兩側擺幅、來源轉角、子圖像繼承與非整格／反向取樣；加上 `-- --record` 可匯出 30 FPS 動態預覽影格。

音樂為 `res://Manosaba/audio/bgm/rAwTell Owk....mp3`（檔名空格為 U+00A0）。模組配置「審 問 開 始」只控制這首 BGM，選項為每次／每場冒險一次／不播放，預設每次；每場冒險一次以共用場地計次，新冒險重置。入場畫面始終播放。音量及戰鬥結束、離場、放棄冒險後的音樂還原沿用現有 `GodotSfxRouter`。

畫面附屬於戰鬥房間，完成或離開房間時自動清理。使用實際影格時間播放，不受出牌快速模式壓縮。無場地或測試模式下跳過音畫。

驗證工具為 `Tools/InterrogationStartVerify.gd`，檢查資源載入、構圖節點、等比例縮放、實際播放結束和離場清理。`-- --capture` 匯出演出影格，`-- --pack` 檢查 `_mods_out/Manosaba.pck`。可在不載入模組 C# 的獨立 Godot 專案中複製同一路徑的素材與驗證腳本執行。

修改 C# 後需重新編譯；修改素材或動畫後還需重新匯出並更新遊戲 mods/Manosaba/Manosaba.pck。
