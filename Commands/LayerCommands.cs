using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AutoCAD_Layer_Manger.UI;
using AutoCAD_Layer_Manger.Services;
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
        /// 測試圖塊分解重組功能指令
        /// </summary>
        [CommandMethod("LAYERBLOCKTEST", CommandFlags.Modal)]
        public void LayerBlockTestCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== 圖塊分解重組功能測試 ===");
                
                // 選取圖塊
                var opts = new PromptSelectionOptions
                {
                    MessageForAdding = "\n選取要測試的圖塊: "
                };
                
                var filter = new SelectionFilter(new[]
                {
                    new TypedValue((int)DxfCode.Start, "INSERT")
                });
                
                var selResult = ed.GetSelection(opts, filter);
                if (selResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n未選取任何圖塊，測試結束。");
                    return;
                }

                var entityIds = selResult.Value.GetObjectIds();
                ed.WriteMessage($"\n已選取 {entityIds.Length} 個圖塊");

                // 檢查圖塊是否在鎖定圖層上
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    int lockedBlockCount = 0;
                    foreach (var objId in entityIds)
                    {
                        if (tr.GetObject(objId, OpenMode.ForRead) is BlockReference blockRef)
                        {
                            if (IsEntityOnLockedLayer(tr, blockRef))
                            {
                                lockedBlockCount++;
                                ed.WriteMessage($"\n找到鎖定圖層上的圖塊: {blockRef.Name} (圖層: {blockRef.Layer})");
                            }
                        }
                    }
                    tr.Commit();
                    
                    if (lockedBlockCount == 0)
                    {
                        ed.WriteMessage("\n所選圖塊都不在鎖定圖層上，無法測試分解重組功能。");
                        ed.WriteMessage("\n請先將一些圖塊移到鎖定圖層上再測試。");
                        return;
                    }
                    
                    ed.WriteMessage($"\n共找到 {lockedBlockCount} 個在鎖定圖層上的圖塊");
                }

                // 詢問目標圖層
                var layerOpts = new PromptStringOptions("\n輸入目標圖層名稱: ");
                layerOpts.AllowSpaces = false;
                layerOpts.DefaultValue = "0";
                
                var layerResult = ed.GetString(layerOpts);
                if (layerResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n操作已取消。");
                    return;
                }

                string targetLayer = layerResult.StringResult;
                ed.WriteMessage($"\n目標圖層: {targetLayer}");

                // 執行轉換（使用分解重組法）
                ed.WriteMessage("\n開始執行圖塊分解重組轉換...");
                
                var entityConverter = new EntityConverter();
                var layerService = new LayerService(entityConverter);
                
                var options = new ConversionOptions
                {
                    CreateTargetLayer = true,
                    UnlockTargetLayer = true,
                    ForceConvertLockedObjects = true,
                    UseBlockExplodeMethod = true,  // 關鍵：啟用分解重組法
                    ProcessBlocks = true,
                    MaxDepth = 10
                };

                var conversionTask = layerService.ConvertEntitiesToLayerAsync(entityIds, targetLayer, options);
                var result = conversionTask.Result; // 同步等待結果

                // 顯示詳細結果
                ed.WriteMessage("\n=== 轉換結果 ===");
                ed.WriteMessage($"\n成功轉換: {result.ConvertedCount} 個物件");
                ed.WriteMessage($"\n跳過物件: {result.SkippedCount} 個");
                ed.WriteMessage($"\n錯誤物件: {result.ErrorCount} 個");
                
                if (result.Errors.Any())
                {
                    ed.WriteMessage($"\n錯誤詳情:");
                    foreach (var error in result.Errors.Take(5))
                    {
                        ed.WriteMessage($"\n  - {error}");
                    }
                }
                
                if (result.ConvertedCount > 0)
                {
                    ed.WriteMessage("\n? 圖塊分解重組功能測試成功！");
                }
                else
                {
                    ed.WriteMessage("\n? 圖塊分解重組功能可能存在問題。");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n測試過程發生錯誤: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerBlockTestCommand error: {ex}");
            }
        }

        /// <summary>
        /// 測試圖層載入功能指令
        /// </summary>
        [CommandMethod("LAYERLOADTEST", CommandFlags.Modal)]
        public void LayerLoadTestCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== 圖層載入功能測試 ===");
                
                // 測試直接讀取圖層
                var layers = GetLayers();
                ed.WriteMessage($"\n? 直接讀取找到 {layers.Count} 個圖層");
                
                if (layers.Count > 0)
                {
                    ed.WriteMessage($"\n前10個圖層:");
                    foreach (var layer in layers.Take(10))
                    {
                        ed.WriteMessage($"\n  - {layer}");
                    }
                }

                // 測試LayerService
                try
                {
                    var entityConverter = new EntityConverter();
                    var layerService = new LayerService(entityConverter);
                    var layerInfoTask = layerService.GetLayersAsync();
                    var layerInfos = layerInfoTask.Result;
                    
                    ed.WriteMessage($"\n? LayerService找到 {layerInfos.Count} 個圖層");
                    
                    if (layerInfos.Count > 0)
                    {
                        ed.WriteMessage($"\n前5個圖層詳情:");
                        foreach (var layerInfo in layerInfos.Take(5))
                        {
                            string status = "";
                            if (layerInfo.IsLocked) status += "鎖定 ";
                            if (layerInfo.IsFrozen) status += "凍結 ";
                            if (layerInfo.IsOff) status += "關閉 ";
                            
                            ed.WriteMessage($"\n  - {layerInfo.Name} ({(string.IsNullOrEmpty(status) ? "正常" : status.Trim())})");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n? LayerService測試失敗: {ex.Message}");
                }

                ed.WriteMessage("\n? 圖層載入測試完成！");
                ed.WriteMessage("\n如果看到圖層列表，表示功能正常。");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n測試失敗: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerLoadTestCommand error: {ex}");
            }
        }

        /// <summary>
        /// 診斷實體轉換問題的指令
        /// </summary>
        [CommandMethod("LAYERDIAGNOSE", CommandFlags.Modal)]
        public void LayerDiagnoseCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== 圖層轉換診斷工具 ===");
                
                // 選取要診斷的物件
                var selOpts = new PromptSelectionOptions
                {
                    MessageForAdding = "\n選取要診斷的物件: ",
                    AllowDuplicates = false
                };

                var selResult = ed.GetSelection(selOpts);
                if (selResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n未選取任何物件。");
                    return;
                }

                var entityIds = selResult.Value.GetObjectIds();
                ed.WriteMessage($"\n正在診斷 {entityIds.Length} 個物件...");

                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var lockedCount = 0;
                    var erasedCount = 0;
                    var unsupportedCount = 0;
                    var blockCount = 0;
                    var normalCount = 0;

                    foreach (var objId in entityIds)
                    {
                        try
                        {
                            if (tr.GetObject(objId, OpenMode.ForRead) is Entity entity)
                            {
                                ed.WriteMessage($"\n\n物件類型: {entity.GetType().Name}");
                                ed.WriteMessage($"\n當前圖層: {entity.Layer}");

                                // 檢查是否已刪除
                                if (entity.IsErased)
                                {
                                    ed.WriteMessage("\n狀態: ? 已刪除");
                                    erasedCount++;
                                    continue;
                                }

                                // 檢查圖層狀態
                                var layerTable = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                                if (layerTable?.Has(entity.Layer) == true)
                                {
                                    var layerRecord = tr.GetObject(layerTable[entity.Layer], OpenMode.ForRead) as LayerTableRecord;
                                    if (layerRecord != null)
                                    {
                                        ed.WriteMessage($"\n圖層狀態: {(layerRecord.IsLocked ? "?? 鎖定" : "?? 未鎖定")} | {(layerRecord.IsFrozen ? "?? 凍結" : "??? 未凍結")} | {(layerRecord.IsOff ? "?? 關閉" : "?? 開啟")}");
                                        
                                        if (layerRecord.IsLocked)
                                        {
                                            lockedCount++;
                                        }
                                    }
                                }

                                // 檢查實體類型
                                if (entity is BlockReference blockRef)
                                {
                                    ed.WriteMessage($"\n圖塊資訊: {blockRef.Name}");
                                    ed.WriteMessage($"\n動態圖塊: {(blockRef.IsDynamicBlock ? "是" : "否")}");
                                    
                                    // 檢查圖塊定義
                                    var btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                                    if (btr != null)
                                    {
                                        ed.WriteMessage($"\n可分解: {(btr.Explodable ? "是" : "否")}");
                                    }
                                    blockCount++;
                                }
                                else
                                {
                                    var entityConverter = new EntityConverter();
                                    if (entityConverter.IsGeometricEntity(entity))
                                    {
                                        ed.WriteMessage("\n類型: ? 支援的幾何實體");
                                        normalCount++;
                                    }
                                    else
                                    {
                                        ed.WriteMessage("\n類型: ? 不支援的實體類型");
                                        unsupportedCount++;
                                    }
                                }

                                // 檢查寫入權限
                                try
                                {
                                    entity.UpgradeOpen();
                                    ed.WriteMessage("\n權限: ? 可寫入");
                                    entity.DowngradeOpen();
                                }
                                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                                {
                                    ed.WriteMessage($"\n權限: ? 無法寫入 ({ex.ErrorStatus})");
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage($"\n診斷物件 {objId} 時發生錯誤: {ex.Message}");
                        }
                    }

                    tr.Commit();

                    // 顯示統計信息
                    ed.WriteMessage("\n\n=== 診斷結果統計 ===");
                    ed.WriteMessage($"\n?? 鎖定圖層物件: {lockedCount} 個");
                    ed.WriteMessage($"\n?? 圖塊物件: {blockCount} 個");
                    ed.WriteMessage($"\n?? 普通幾何物件: {normalCount} 個");
                    ed.WriteMessage($"\n? 不支援類型: {unsupportedCount} 個");
                    ed.WriteMessage($"\n??? 已刪除物件: {erasedCount} 個");

                    // 提供建議
                    ed.WriteMessage("\n\n=== 建議 ===");
                    if (lockedCount > 0)
                    {
                        ed.WriteMessage("\n? 啟用「強制轉換鎖定物件」選項");
                        ed.WriteMessage("\n? 對圖塊啟用「圖塊分解重組法」");
                    }
                    if (unsupportedCount > 0)
                    {
                        ed.WriteMessage("\n? 某些特殊實體類型可能需要手動處理");
                    }
                    if (blockCount > 0)
                    {
                        ed.WriteMessage("\n? 圖塊建議使用「圖塊分解重組法」以獲得最佳結果");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n診斷過程發生錯誤: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerDiagnoseCommand error: {ex}");
            }
        }

        /// <summary>
        /// 圖塊編輯器方法測試指令
        /// </summary>
        [CommandMethod("LAYEREDITBLOCK", CommandFlags.Modal)]
        public void LayerEditBlockCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== 圖塊編輯器轉換方法測試 ===");
                ed.WriteMessage("\n此方法使用AutoCAD的圖塊編輯器來轉換圖層");
                ed.WriteMessage("\n適用於無法通過其他方法轉換的頑固圖塊");
                
                // 選取圖塊
                var opts = new PromptSelectionOptions
                {
                    MessageForAdding = "\n選取要使用編輯器方法轉換的圖塊: "
                };
                
                var filter = new SelectionFilter(new[]
                {
                    new TypedValue((int)DxfCode.Start, "INSERT")
                });
                
                var selResult = ed.GetSelection(opts, filter);
                if (selResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n未選取任何圖塊。");
                    return;
                }

                var entityIds = selResult.Value.GetObjectIds();
                ed.WriteMessage($"\n已選取 {entityIds.Length} 個圖塊");

                // 詢問目標圖層
                var layerOpts = new PromptStringOptions("\n輸入目標圖層名稱: ");
                layerOpts.AllowSpaces = false;
                layerOpts.DefaultValue = "0";
                
                var layerResult = ed.GetString(layerOpts);
                if (layerResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n操作已取消。");
                    return;
                }

                string targetLayer = layerResult.StringResult;
                ed.WriteMessage($"\n目標圖層: {targetLayer}");

                // 詢問使用哪種編輯器方法
                var methodOpts = new PromptKeywordOptions("\n選擇編輯器方法 [Reference編輯(R)/圖塊編輯器(B)]: ");
                methodOpts.Keywords.Add("Reference");
                methodOpts.Keywords.Add("Block");
                methodOpts.Keywords.Default = "Reference";

                var methodResult = ed.GetKeywords(methodOpts);
                bool useReferenceEdit = methodResult.Status == PromptStatus.OK && 
                    (methodResult.StringResult == "Reference" || methodResult.StringResult == "R");

                // 執行轉換
                ed.WriteMessage($"\n開始使用{(useReferenceEdit ? "Reference編輯" : "圖塊編輯器")}方法轉換...");
                
                var entityConverter = new EntityConverter();
                var layerService = new LayerService(entityConverter);
                
                var options = new ConversionOptions
                {
                    CreateTargetLayer = true,
                    UnlockTargetLayer = true,
                    ForceConvertLockedObjects = true,
                    UseBlockExplodeMethod = false,  // 不使用分解重組
                    UseReferenceEditMethod = useReferenceEdit,
                    UseBlockEditorMethod = !useReferenceEdit,
                    ProcessBlocks = true,
                    PreferredBlockMethod = useReferenceEdit ? 
                        BlockProcessingMethod.ReferenceEdit : 
                        BlockProcessingMethod.BlockEditor
                };

                var conversionTask = layerService.ConvertEntitiesToLayerAsync(entityIds, targetLayer, options);
                var result = conversionTask.Result;

                // 顯示結果
                ed.WriteMessage("\n=== 轉換結果 ===");
                ed.WriteMessage($"\n成功轉換: {result.ConvertedCount} 個物件");
                ed.WriteMessage($"\n跳過物件: {result.SkippedCount} 個");
                ed.WriteMessage($"\n錯誤物件: {result.ErrorCount} 個");
                
                if (result.Errors.Any())
                {
                    ed.WriteMessage($"\n錯誤詳情:");
                    foreach (var error in result.Errors.Take(5))
                    {
                        ed.WriteMessage($"\n  - {error}");
                    }
                    if (result.Errors.Count > 5)
                    {
                        ed.WriteMessage($"\n  ... 還有 {result.Errors.Count - 5} 個錯誤");
                    }
                }
                
                if (result.ConvertedCount > 0)
                {
                    ed.WriteMessage($"\n? {(useReferenceEdit ? "Reference編輯" : "圖塊編輯器")}方法測試成功！");
                }
                else
                {
                    ed.WriteMessage($"\n? {(useReferenceEdit ? "Reference編輯" : "圖塊編輯器")}方法可能需要調整。");
                }
                
                ed.WriteMessage("\n\n?? 提示：");
                ed.WriteMessage("\n? Reference編輯適用於大多數圖塊");
                ed.WriteMessage("\n? 圖塊編輯器適用於複雜的嵌套圖塊");
                ed.WriteMessage("\n? 這些方法會自動處理鎖定圖層問題");
                
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n測試過程發生錯誤: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerEditBlockCommand error: {ex}");
            }
        }

        #region 私有輔助方法

        /// <summary>
        /// 轉換實體到圖層（支援強制轉換選項）
        /// </summary>
        private ConversionResult ConvertEntitiesWithOptions(ObjectId[] entityIds, string targetLayer, bool forceConvert)
        {
            var result = new ConversionResult();
            
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
                            if (tr.GetObject(objId, OpenMode.ForRead) is Entity entity)
                            {
                                bool converted = false;

                                if (forceConvert)
                                {
                                    // 強制轉換模式
                                    converted = ConvertEntityWithUnlock(tr, entity, targetLayer);
                                }
                                else
                                {
                                    // 傳統模式：跳過鎖定圖層
                                    if (!IsEntityOnLockedLayer(tr, entity))
                                    {
                                        entity.UpgradeOpen();
                                        entity.Layer = targetLayer;
                                        converted = true;
                                    }
                                }

                                if (converted)
                                {
                                    result.ConvertedCount++;
                                }
                                else
                                {
                                    result.SkippedCount++;
                                    result.Errors.Add($"跳過鎖定圖層上的物件 {objId}");
                                }
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
                System.Diagnostics.Debug.WriteLine($"ConvertEntitiesWithOptions error: {ex}");
            }

            return result;
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
        private ConversionResult ConvertEntities(ObjectId[] entityIds, string targetLayer)
        {
            var result = new ConversionResult();
            
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
        private void ShowResult(Editor ed, ConversionResult result)
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

        /// <summary>
        /// 檢查實體是否在鎖定圖層上
        /// </summary>
        private bool IsEntityOnLockedLayer(Transaction tr, Entity entity)
        {
            try
            {
                var db = entity.Database;
                var layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                
                if (layerTable?.Has(entity.Layer) == true)
                {
                    var layerRecord = tr.GetObject(layerTable[entity.Layer], OpenMode.ForRead) as LayerTableRecord;
                    return layerRecord?.IsLocked == true;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsEntityOnLockedLayer error: {ex}");
            }
            
            return false;
        }

        /// <summary>
        /// 使用解鎖方法轉換實體
        /// </summary>
        private bool ConvertEntityWithUnlock(Transaction tr, Entity entity, string targetLayer)
        {
            try
            {
                var db = entity.Database;
                var layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                
                if (layerTable?.Has(entity.Layer) == true)
                {
                    var layerRecord = tr.GetObject(layerTable[entity.Layer], OpenMode.ForRead) as LayerTableRecord;
                    
                    if (layerRecord?.IsLocked == true)
                    {
                        // 暫時解鎖
                        layerRecord.UpgradeOpen();
                        layerRecord.IsLocked = false;
                        
                        // 轉換圖層
                        entity.UpgradeOpen();
                        entity.Layer = targetLayer;
                        
                        // 恢復鎖定
                        layerRecord.IsLocked = true;
                        
                        return true;
                    }
                    else
                    {
                        // 圖層未鎖定，直接轉換
                        entity.UpgradeOpen();
                        entity.Layer = targetLayer;
                        return true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConvertEntityWithUnlock error: {ex}");
            }
            
            return false;
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

        #endregion
    }
}