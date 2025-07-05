using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AutoCAD_Layer_Manger.UI;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: CommandClass(typeof(AutoCAD_Layer_Manger.Commands.LayerCommands))]
[assembly: ExtensionApplication(typeof(AutoCAD_Layer_Manger.PluginExtension))]

namespace AutoCAD_Layer_Manger.Commands
{
    /// <summary>
    /// 簡化的圖層管理指令 - 使用統一UI
    /// </summary>
    public class LayerCommands
    {
        /// <summary>
        /// 圖層管理指令 - 選擇物件後直接進入統一UI
        /// </summary>
        [CommandMethod("LAYERMANAGER", CommandFlags.Modal)]
        public void LayerManagerCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== AutoCAD 圖層管理器 ===");
                
                // 直接選取物件
                var entityIds = SelectEntities(ed);
                if (entityIds.Length == 0) 
                {
                    ed.WriteMessage("\n未選取任何物件，指令結束。");
                    return;
                }

                // 顯示統一UI對話框
                try
                {
                    using (var dialog = new LayerManagerForm(entityIds))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            ed.WriteMessage("\n圖層轉換完成！");
                            if (dialog.Result != null)
                            {
                                ShowResult(ed, dialog.Result);
                            }
                        }
                        else
                        {
                            ed.WriteMessage("\n操作已取消。");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n對話框錯誤: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Dialog error: {ex}");
                }
            }
            catch (System.Exception ex)
            {
                ed?.WriteMessage($"\n錯誤: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerManagerCommand error: {ex}");
            }
        }

        /// <summary>
        /// 測試功能指令 - 獨立的測試指令
        /// </summary>
        [CommandMethod("LAYERTEST", CommandFlags.Modal)]
        public void LayerTestCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== 圖層管理器功能測試 ===");
                
                // 測試圖層讀取
                var layers = GetLayers();
                ed.WriteMessage($"\n? 圖層讀取: 找到 {layers.Count} 個圖層");
                
                if (layers.Count > 0)
                {
                    ed.WriteMessage($"\n前5個圖層: {string.Join(", ", layers.Take(5))}");
                    
                    // 顯示圖層狀態
                    var layerInfo = GetLayerInfo();
                    var lockedLayers = layerInfo.Where(l => l.IsLocked).Count();
                    var frozenLayers = layerInfo.Where(l => l.IsFrozen).Count();
                    
                    ed.WriteMessage($"\n? 鎖定圖層: {lockedLayers} 個");
                    ed.WriteMessage($"\n? 凍結圖層: {frozenLayers} 個");
                }
                
                ed.WriteMessage("\n? 所有功能測試完成！");
                ed.WriteMessage("\n使用 LAYERMANAGER 指令進行圖層轉換");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n測試失敗: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerTestCommand error: {ex}");
            }
        }

        /// <summary>
        /// 快速轉換指令 - 轉換到當前圖層
        /// </summary>
        [CommandMethod("LAYERQUICK", CommandFlags.Modal)]
        public void LayerQuickCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== 快速圖層轉換 ===");
                
                // 獲取當前圖層
                string currentLayer = GetCurrentLayer();
                ed.WriteMessage($"\n目標圖層: {currentLayer}");
                
                // 選取物件
                var entityIds = SelectEntities(ed);
                if (entityIds.Length == 0) return;

                // 確認轉換
                var confirmOpts = new PromptKeywordOptions($"\n將 {entityIds.Length} 個物件轉換到圖層 '{currentLayer}'? ");
                confirmOpts.Keywords.Add("Yes");
                confirmOpts.Keywords.Add("No");
                confirmOpts.Keywords.Default = "Yes";

                var confirmResult = ed.GetKeywords(confirmOpts);
                if (confirmResult.Status == PromptStatus.OK && confirmResult.StringResult == "Yes")
                {
                    var result = ConvertEntities(entityIds, currentLayer);
                    ShowResult(ed, result);
                }
                else
                {
                    ed.WriteMessage("\n操作已取消。");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n快速轉換錯誤: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerQuickCommand error: {ex}");
            }
        }

        /// <summary>
        /// 選取實體
        /// </summary>
        private ObjectId[] SelectEntities(Editor ed)
        {
            var selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\n選取要轉換圖層的物件: ",
                AllowDuplicates = false
            };

            var selResult = ed.GetSelection(selOpts);
            if (selResult.Status == PromptStatus.OK)
            {
                var entityIds = selResult.Value.GetObjectIds();
                ed.WriteMessage($"\n已選取 {entityIds.Length} 個物件");
                return entityIds;
            }

            return Array.Empty<ObjectId>();
        }

        /// <summary>
        /// 獲取當前圖層
        /// </summary>
        private string GetCurrentLayer()
        {
            try
            {
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc?.Database == null) return "0";

                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var layerTable = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (layerTable != null)
                    {
                        var currentLayerRecord = tr.GetObject(doc.Database.Clayer, OpenMode.ForRead) as LayerTableRecord;
                        return currentLayerRecord?.Name ?? "0";
                    }
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCurrentLayer error: {ex}");
            }

            return "0";
        }

        /// <summary>
        /// 獲取圖層列表
        /// </summary>
        private List<string> GetLayers()
        {
            var layers = new List<string>();
            
            try
            {
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc?.Database == null) return layers;

                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var layerTable = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (layerTable == null) return layers;

                    foreach (ObjectId layerId in layerTable)
                    {
                        if (tr.GetObject(layerId, OpenMode.ForRead) is LayerTableRecord ltr)
                        {
                            layers.Add(ltr.Name);
                        }
                    }
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetLayers error: {ex}");
            }

            return layers.OrderBy(l => l).ToList();
        }

        /// <summary>
        /// 獲取圖層詳細資訊
        /// </summary>
        private List<LayerInfo> GetLayerInfo()
        {
            var layers = new List<LayerInfo>();
            
            try
            {
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc?.Database == null) return layers;

                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var layerTable = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (layerTable == null) return layers;

                    foreach (ObjectId layerId in layerTable)
                    {
                        if (tr.GetObject(layerId, OpenMode.ForRead) is LayerTableRecord ltr)
                        {
                            layers.Add(new LayerInfo
                            {
                                Name = ltr.Name,
                                IsLocked = ltr.IsLocked,
                                IsFrozen = ltr.IsFrozen,
                                IsOff = ltr.IsOff
                            });
                        }
                    }
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetLayerInfo error: {ex}");
            }

            return layers.OrderBy(l => l.Name).ToList();
        }

        /// <summary>
        /// 轉換實體到圖層
        /// </summary>
        private LayerManagerForm.ConversionResult ConvertEntities(ObjectId[] entityIds, string targetLayer)
        {
            var result = new LayerManagerForm.ConversionResult();
            
            try
            {
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc?.Database == null) return result;

                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    // 確保目標圖層存在
                    EnsureLayerExists(tr, doc.Database, targetLayer);

                    foreach (var objId in entityIds)
                    {
                        try
                        {
                            if (tr.GetObject(objId, OpenMode.ForWrite) is Entity entity)
                            {
                                entity.Layer = targetLayer;
                                result.ConvertedCount++;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            result.ErrorCount++;
                            result.Errors.Add($"物件 {objId}: {ex.Message}");
                        }
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"轉換失敗: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ConvertEntities error: {ex}");
            }

            return result;
        }

        /// <summary>
        /// 確保圖層存在
        /// </summary>
        private void EnsureLayerExists(Transaction tr, Database db, string layerName)
        {
            try
            {
                var layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (layerTable != null && !layerTable.Has(layerName))
                {
                    layerTable.UpgradeOpen();
                    var newLayer = new LayerTableRecord
                    {
                        Name = layerName
                    };
                    layerTable.Add(newLayer);
                    tr.AddNewlyCreatedDBObject(newLayer, true);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureLayerExists error: {ex}");
            }
        }

        /// <summary>
        /// 顯示轉換結果
        /// </summary>
        private void ShowResult(Editor ed, LayerManagerForm.ConversionResult result)
        {
            ed.WriteMessage($"\n=== 轉換結果 ===");
            ed.WriteMessage($"\n成功轉換: {result.ConvertedCount} 個物件");
            ed.WriteMessage($"\n錯誤物件: {result.ErrorCount} 個");
            
            if (result.Errors.Any())
            {
                ed.WriteMessage($"\n錯誤詳情:");
                foreach (var error in result.Errors.Take(3))
                {
                    ed.WriteMessage($"\n  - {error}");
                }
                if (result.Errors.Count > 3)
                {
                    ed.WriteMessage($"\n  ... 還有 {result.Errors.Count - 3} 個錯誤");
                }
            }
            
            ed.WriteMessage("\n轉換完成！");
        }

        #region 數據類別

        /// <summary>
        /// 圖層資訊
        /// </summary>
        public class LayerInfo
        {
            public string Name { get; set; } = string.Empty;
            public bool IsLocked { get; set; }
            public bool IsFrozen { get; set; }
            public bool IsOff { get; set; }
            public bool IsAvailable => !IsLocked && !IsFrozen && !IsOff;
        }

        #endregion
    }
}