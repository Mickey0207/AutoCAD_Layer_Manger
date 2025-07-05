using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCAD_Layer_Manger.Services
{
    /// <summary>
    /// 圖層服務接口
    /// </summary>
    public interface ILayerService
    {
        Task<List<LayerInfo>> GetLayersAsync();
        Task<bool> CreateLayerAsync(string layerName, LayerProperties? properties = null);
        Task<bool> UnlockLayerAsync(string layerName);
        Task<ConversionResult> ConvertEntitiesToLayerAsync(IEnumerable<ObjectId> entityIds, string targetLayer, ConversionOptions options, IProgress<ConversionProgress>? progress = null);
        bool LayerExists(string layerName);
    }

    /// <summary>
    /// 圖層服務實現
    /// </summary>
    public class LayerService : ILayerService
    {
        private readonly IEntityConverter _entityConverter;

        public LayerService(IEntityConverter entityConverter)
        {
            _entityConverter = entityConverter ?? throw new ArgumentNullException(nameof(entityConverter));
        }

        public async Task<List<LayerInfo>> GetLayersAsync()
        {
            return await Task.Run(() => GetLayers());
        }

        private List<LayerInfo> GetLayers()
        {
            var layers = new List<LayerInfo>();
            
            try
            {
                Document doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc == null) return layers;

                Database db = doc.Database;
                
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
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
                                IsOff = ltr.IsOff,
                                Color = ltr.Color.ColorIndex.ToString(),
                                LineWeight = ltr.LineWeight,
                                PlotStyleName = ltr.PlotStyleName,
                                IsPlottable = ltr.IsPlottable
                            });
                        }
                    }
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                // 記錄錯誤但不拋出異常
                System.Diagnostics.Debug.WriteLine($"獲取圖層失敗: {ex.Message}");
            }

            return layers.OrderBy(l => l.Name).ToList();
        }

        public async Task<bool> CreateLayerAsync(string layerName, LayerProperties? properties = null)
        {
            return await Task.Run(() => CreateLayer(layerName, properties));
        }

        private bool CreateLayer(string layerName, LayerProperties? properties)
        {
            if (string.IsNullOrWhiteSpace(layerName)) return false;

            try
            {
                Document doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc == null) return false;

                Database db = doc.Database;

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForWrite) as LayerTable;
                    if (layerTable == null) return false;

                    if (layerTable.Has(layerName)) return true; // 已存在

                    var ltr = new LayerTableRecord
                    {
                        Name = layerName
                    };

                    // 應用屬性
                    if (properties != null)
                    {
                        if (properties.Color.HasValue)
                            ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByColor, properties.Color.Value);
                        
                        if (properties.LineWeight.HasValue)
                            ltr.LineWeight = properties.LineWeight.Value;
                        
                        if (!string.IsNullOrEmpty(properties.PlotStyleName))
                            ltr.PlotStyleName = properties.PlotStyleName;
                        
                        ltr.IsPlottable = properties.IsPlottable;
                    }

                    layerTable.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
                    tr.Commit();
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"創建圖層失敗: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UnlockLayerAsync(string layerName)
        {
            return await Task.Run(() => UnlockLayer(layerName));
        }

        private bool UnlockLayer(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName)) return false;

            try
            {
                Document doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc == null) return false;

                Database db = doc.Database;

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (layerTable?.Has(layerName) == true)
                    {
                        var ltr = tr.GetObject(layerTable[layerName], OpenMode.ForWrite) as LayerTableRecord;
                        if (ltr != null && ltr.IsLocked)
                        {
                            ltr.IsLocked = false;
                        }
                    }
                    tr.Commit();
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解鎖圖層失敗: {ex.Message}");
                return false;
            }
        }

        public bool LayerExists(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName)) return false;

            try
            {
                Document doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc == null) return false;

                Database db = doc.Database;

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    bool exists = layerTable?.Has(layerName) == true;
                    tr.Commit();
                    return exists;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<ConversionResult> ConvertEntitiesToLayerAsync(
            IEnumerable<ObjectId> entityIds, 
            string targetLayer, 
            ConversionOptions options, 
            IProgress<ConversionProgress>? progress = null)
        {
            return await Task.Run(() => ConvertEntitiesToLayer(entityIds, targetLayer, options, progress));
        }

        private ConversionResult ConvertEntitiesToLayer(
            IEnumerable<ObjectId> entityIds, 
            string targetLayer, 
            ConversionOptions options, 
            IProgress<ConversionProgress>? progress)
        {
            var result = new ConversionResult();
            var entityList = entityIds.ToList();
            
            try
            {
                Document doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc == null) return result;

                Database db = doc.Database;

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    // 如果需要，創建目標圖層
                    if (options.CreateTargetLayer && !LayerExists(targetLayer))
                    {
                        CreateLayer(targetLayer, null);
                    }

                    // 如果需要，解鎖目標圖層
                    if (options.UnlockTargetLayer)
                    {
                        UnlockLayer(targetLayer);
                    }

                    int totalCount = entityList.Count;
                    int processedCount = 0;

                    foreach (var objId in entityList)
                    {
                        try
                        {
                            if (tr.GetObject(objId, OpenMode.ForRead) is Entity entity)
                            {
                                var entityResult = _entityConverter.ConvertEntityToLayer(
                                    tr, entity, targetLayer, Matrix3d.Identity, options);
                                
                                result.ConvertedCount += entityResult.ConvertedCount;
                                result.SkippedCount += entityResult.SkippedCount;
                                result.ErrorCount += entityResult.ErrorCount;
                                result.Errors.AddRange(entityResult.Errors);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            result.ErrorCount++;
                            result.Errors.Add($"物件 {objId}: {ex.Message}");
                        }

                        processedCount++;
                        progress?.Report(new ConversionProgress
                        {
                            ProcessedCount = processedCount,
                            TotalCount = totalCount,
                            CurrentOperation = $"處理物件 {processedCount}/{totalCount}"
                        });
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"轉換過程發生錯誤: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// 圖層屬性
    /// </summary>
    public class LayerProperties
    {
        public short? Color { get; set; }
        public LineWeight? LineWeight { get; set; }
        public string? PlotStyleName { get; set; }
        public bool IsPlottable { get; set; } = true;
    }

    /// <summary>
    /// 轉換選項 - 物件類型過濾版本
    /// </summary>
    public class ConversionOptions
    {
        public bool CreateTargetLayer { get; set; } = true;
        public bool UnlockTargetLayer { get; set; } = true;
        public bool ForceConvertLockedObjects { get; set; } = true;
        public bool ProcessAllEntities { get; set; } = true;
        
        // 物件類型過濾選項
        public bool ProcessPolylines { get; set; } = true;
        public bool ProcessLines { get; set; } = true;
        public bool ProcessCircles { get; set; } = true;
        public bool ProcessArcs { get; set; } = true;
        public bool ProcessBlocks { get; set; } = true;
        public bool ProcessDynamicBlocks { get; set; } = true;
        public bool ProcessDimensions { get; set; } = true;
        
        // 圖塊處理選項（保留現有功能）
        public bool UseBlockExplodeMethod { get; set; } = true;
        public bool UseReferenceEditMethod { get; set; } = false;
        public bool UseBlockEditorMethod { get; set; } = false;
        public bool EnableAutoRetry { get; set; } = true;
        public int MaxDepth { get; set; } = 50;
        
        public BlockProcessingMethod PreferredBlockMethod { get; set; } = BlockProcessingMethod.ExplodeRecombine;
        
        /// <summary>
        /// 獲取啟用的處理方法列表（按優先順序）
        /// </summary>
        public List<BlockProcessingMethod> GetEnabledMethods()
        {
            var methods = new List<BlockProcessingMethod>();
            
            // 始終包含傳統方法作為基礎
            methods.Add(BlockProcessingMethod.Traditional);
            
            // 根據設定添加其他方法
            if (UseBlockExplodeMethod)
                methods.Add(BlockProcessingMethod.ExplodeRecombine);
                
            if (UseReferenceEditMethod)
                methods.Add(BlockProcessingMethod.ReferenceEdit);
                
            if (UseBlockEditorMethod)
                methods.Add(BlockProcessingMethod.BlockEditor);
            
            return methods;
        }
        
        /// <summary>
        /// 檢查是否有任何物件類型被啟用
        /// </summary>
        public bool HasAnyEntityTypeEnabled()
        {
            return ProcessPolylines || ProcessLines || ProcessCircles || ProcessArcs ||
                   ProcessBlocks || ProcessDynamicBlocks || ProcessDimensions;
        }
    }

    /// <summary>
    /// 圖塊處理方法枚舉
    /// </summary>
    public enum BlockProcessingMethod
    {
        /// <summary>傳統方法（暫時解鎖）</summary>
        Traditional = 0,
        /// <summary>分解重組法</summary>
        ExplodeRecombine = 1,
        /// <summary>Reference Edit（現地編輯）</summary>
        ReferenceEdit = 2,
        /// <summary>圖塊編輯器</summary>
        BlockEditor = 3
    }

    /// <summary>
    /// 轉換進度
    /// </summary>
    public class ConversionProgress
    {
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public string CurrentOperation { get; set; } = string.Empty;
        public double PercentComplete => TotalCount > 0 ? (double)ProcessedCount / TotalCount * 100 : 0;
    }
}