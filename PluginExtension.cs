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
                    doc.Editor.WriteMessage("\n=== AutoCAD 圖層管理器 - 物件類型選擇版 ===");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n主要指令: LAYERMANAGER");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n功能特色:");
                    doc.Editor.WriteMessage("\n   選擇性轉換物件類型");
                    doc.Editor.WriteMessage("\n   轉換聚合線、線、圓、弧");
                    doc.Editor.WriteMessage("\n   轉換圖塊和動態圖塊");
                    doc.Editor.WriteMessage("\n   轉換標註和尺寸");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n圖層設定:");
                    doc.Editor.WriteMessage("\n   自動解鎖目標圖層");
                    doc.Editor.WriteMessage("\n   轉換鎖定圖層上的物件");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n使用流程:");
                    doc.Editor.WriteMessage("\n   1. 輸入 LAYERMANAGER");
                    doc.Editor.WriteMessage("\n   2. 選取要轉換的物件");
                    doc.Editor.WriteMessage("\n   3. 選擇圖層設定選項");
                    doc.Editor.WriteMessage("\n   4. 選擇要轉換的物件類型");
                    doc.Editor.WriteMessage("\n   5. 選擇目標圖層並轉換");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n輔助指令:");
                    doc.Editor.WriteMessage("\n   LAYERQUICK      - 快速轉換到當前圖層");
                    doc.Editor.WriteMessage("\n   LAYERDIAGNOSE   - 診斷物件轉換問題");
                    doc.Editor.WriteMessage("\n   LAYERTEST       - 基本功能測試");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n立即開始: 輸入 LAYERMANAGER");
                    doc.Editor.WriteMessage("\n");
                }
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
            // 插件終止時的清理工作
        }
    }
}
