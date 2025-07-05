using System;
using Autodesk.AutoCAD.Runtime;

namespace AutoCAD_Layer_Manger
{
    /// <summary>
    /// 插件擴展應用程式 - 增強版圖層管理器
    /// </summary>
    public class PluginExtension : IExtensionApplication
    {
        private const string AppName = "AutoCAD Layer Manager";
        private const string Version = "4.0.0";

        public void Initialize()
        {
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                
                if (doc?.Editor != null)
                {
                    doc.Editor.WriteMessage($"\n{AppName} v{Version} 載入成功！");
                    doc.Editor.WriteMessage("\n=== 增強版圖層管理器 - 圖塊分解重組技術 ===");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n📋 可用指令:");
                    doc.Editor.WriteMessage("\n  🎯 LAYERMANAGER    - 圖層轉換工具 (主要指令)");
                    doc.Editor.WriteMessage("\n  ⚡ LAYERQUICK      - 快速轉換到當前圖層");
                    doc.Editor.WriteMessage("\n  🔧 LAYERTEST       - 功能測試");
                    doc.Editor.WriteMessage("\n  🧪 LAYERBLOCKTEST  - 圖塊分解重組測試");
                    doc.Editor.WriteMessage("\n  📊 LAYERLOADTEST   - 圖層載入測試");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n🚀 使用方式:");
                    doc.Editor.WriteMessage("\n  1. 輸入 LAYERMANAGER");
                    doc.Editor.WriteMessage("\n  2. 選取要轉換的物件(包括圖塊)");
                    doc.Editor.WriteMessage("\n  3. 按 Enter 進入圖層選擇界面");
                    doc.Editor.WriteMessage("\n  4. 選擇目標圖層並確認轉換");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n✨ 特色功能:");
                    doc.Editor.WriteMessage("\n  ✓ 直接進入UI，無命令列問答");
                    doc.Editor.WriteMessage("\n  ✓ 智能處理鎖定圖層和圖塊");
                    doc.Editor.WriteMessage("\n  ✓ 革命性圖塊分解重組技術");
                    doc.Editor.WriteMessage("\n  ✓ 自動創建不存在的圖層");
                    doc.Editor.WriteMessage("\n  ✓ 保持圖塊結構和屬性完整");
                    doc.Editor.WriteMessage("\n  ✓ 增強的設定選項和預覽功能");
                    doc.Editor.WriteMessage("\n  ✓ 即時狀態顯示和詳細錯誤報告");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n🔐 鎖定圖層處理新技術:");
                    doc.Editor.WriteMessage("\n  🔧 方法一：圖塊分解重組法 (推薦)");
                    doc.Editor.WriteMessage("\n    • 將鎖定圖塊分解到基礎元素");
                    doc.Editor.WriteMessage("\n    • 轉換基礎元素圖層");
                    doc.Editor.WriteMessage("\n    • 重新組合成相同名稱的圖塊");
                    doc.Editor.WriteMessage("\n    • 保持所有屬性和變換");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n  ⚡ 方法二：暫時解鎖法 (傳統)");
                    doc.Editor.WriteMessage("\n    • 暫時解鎖源圖層");
                    doc.Editor.WriteMessage("\n    • 執行圖層轉換");
                    doc.Editor.WriteMessage("\n    • 恢復原鎖定狀態");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n🛠️ 問題排解:");
                    doc.Editor.WriteMessage("\n  • 如果圖層選擇器空白，請執行 LAYERLOADTEST");
                    doc.Editor.WriteMessage("\n  • 如果圖塊轉換有問題，請執行 LAYERBLOCKTEST");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n💡 快速開始: 輸入 LAYERMANAGER 體驗圖塊分解重組技術！");
                    doc.Editor.WriteMessage("\n");
                }

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] {AppName} v{Version} initialized successfully.");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] Plugin initialization failed: {ex.Message}");
                
                try
                {
                    var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                    doc?.Editor?.WriteMessage($"\n{AppName} 載入失敗: {ex.Message}");
                }
                catch
                {
                    // 忽略二次錯誤
                }
            }
        }

        public void Terminate()
        {
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                doc?.Editor?.WriteMessage($"\n{AppName} 已卸載，感謝使用！");

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] {AppName} terminated successfully.");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] Plugin termination failed: {ex.Message}");
            }
        }
    }
}
