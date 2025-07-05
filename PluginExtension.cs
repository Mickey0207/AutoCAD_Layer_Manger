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
                    doc.Editor.WriteMessage("\n=== 智能多方法圖層管理器 ===");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n主要指令: LAYERMANAGER");
                    doc.Editor.WriteMessage("\n   智能自動選擇最佳轉換方法");
                    doc.Editor.WriteMessage("\n   失敗時自動嘗試其他方法");
                    doc.Editor.WriteMessage("\n   使用者可選擇啟用的處理方法");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n新增功能:");
                    doc.Editor.WriteMessage("\n   自動處理鎖定圖層的標註和動態圖塊");
                    doc.Editor.WriteMessage("\n   支援尺寸標註、引線、多重引線轉換");
                    doc.Editor.WriteMessage("\n   智能解鎖圖層進行轉換");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n使用流程:");
                    doc.Editor.WriteMessage("\n   1. 輸入 LAYERMANAGER");
                    doc.Editor.WriteMessage("\n   2. 選取要轉換的物件");
                    doc.Editor.WriteMessage("\n   3. 在UI中選擇處理方法");
                    doc.Editor.WriteMessage("\n   4. 選擇目標圖層並轉換");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n可用的處理方法:");
                    doc.Editor.WriteMessage("\n   智能自動選擇 (推薦)");
                    doc.Editor.WriteMessage("\n   分解重組法 (安全可靠)");
                    doc.Editor.WriteMessage("\n   現地編輯法 (Reference Edit)");
                    doc.Editor.WriteMessage("\n   圖塊編輯器法 (終極方案)");
                    doc.Editor.WriteMessage("\n");
                    doc.Editor.WriteMessage("\n輔助指令:");
                    doc.Editor.WriteMessage("\n   LAYERQUICK     - 快速轉換到當前圖層");
                    doc.Editor.WriteMessage("\n   LAYERDIAGNOSE  - 診斷無法轉換的物件");
                    doc.Editor.WriteMessage("\n   LAYERTEST      - 功能測試");
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
