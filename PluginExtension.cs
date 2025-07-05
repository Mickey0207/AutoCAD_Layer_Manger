using System;
using Autodesk.AutoCAD.Runtime;

namespace AutoCAD_Layer_Manger
{
    /// <summary>
    /// 插件擴展應用程式 - 簡化直接UI版本
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
                    doc.Editor.WriteMessage("\n=== 簡化直接UI版本 ===");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n📋 可用指令:");
                    doc.Editor.WriteMessage("\n  🎯 LAYERMANAGER  - 圖層轉換工具 (主要指令)");
                    doc.Editor.WriteMessage("\n  ⚡ LAYERQUICK    - 快速轉換到當前圖層");
                    doc.Editor.WriteMessage("\n  🔧 LAYERTEST     - 功能測試");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n🚀 使用方式:");
                    doc.Editor.WriteMessage("\n  1. 輸入 LAYERMANAGER");
                    doc.Editor.WriteMessage("\n  2. 選取要轉換的物件");
                    doc.Editor.WriteMessage("\n  3. 按 Enter 進入圖層選擇界面");
                    doc.Editor.WriteMessage("\n  4. 選擇目標圖層並確認轉換");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n✨ 特色功能:");
                    doc.Editor.WriteMessage("\n  ✓ 直接進入UI，無命令列問答");
                    doc.Editor.WriteMessage("\n  ✓ 智能處理鎖定圖層");
                    doc.Editor.WriteMessage("\n  ✓ 自動創建不存在的圖層");
                    doc.Editor.WriteMessage("\n  ✓ 增強的設定選項");
                    doc.Editor.WriteMessage("\n  ✓ 即時預覽和狀態顯示");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n💡 快速開始: 輸入 LAYERMANAGER 立即體驗！");
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
