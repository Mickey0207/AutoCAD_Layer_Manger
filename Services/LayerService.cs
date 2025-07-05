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
    /// �ϼh�A�ȱ��f
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
    /// �ϼh�A�ȹ�{
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
                // �O�����~�����ߥX���`
                System.Diagnostics.Debug.WriteLine($"����ϼh����: {ex.Message}");
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

                    if (layerTable.Has(layerName)) return true; // �w�s�b

                    var ltr = new LayerTableRecord
                    {
                        Name = layerName
                    };

                    // �����ݩ�
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
                System.Diagnostics.Debug.WriteLine($"�Ыعϼh����: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"����ϼh����: {ex.Message}");
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
                    // �p�G�ݭn�A�Ыإؼйϼh
                    if (options.CreateTargetLayer && !LayerExists(targetLayer))
                    {
                        CreateLayer(targetLayer, null);
                    }

                    // �p�G�ݭn�A����ؼйϼh
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
                            result.Errors.Add($"���� {objId}: {ex.Message}");
                        }

                        processedCount++;
                        progress?.Report(new ConversionProgress
                        {
                            ProcessedCount = processedCount,
                            TotalCount = totalCount,
                            CurrentOperation = $"�B�z���� {processedCount}/{totalCount}"
                        });
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"�ഫ�L�{�o�Ϳ��~: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// �ϼh�ݩ�
    /// </summary>
    public class LayerProperties
    {
        public short? Color { get; set; }
        public LineWeight? LineWeight { get; set; }
        public string? PlotStyleName { get; set; }
        public bool IsPlottable { get; set; } = true;
    }

    /// <summary>
    /// �ഫ�ﶵ - ���������L�o����
    /// </summary>
    public class ConversionOptions
    {
        public bool CreateTargetLayer { get; set; } = true;
        public bool UnlockTargetLayer { get; set; } = true;
        public bool ForceConvertLockedObjects { get; set; } = true;
        public bool ProcessAllEntities { get; set; } = true;
        
        // ���������L�o�ﶵ
        public bool ProcessPolylines { get; set; } = true;
        public bool ProcessLines { get; set; } = true;
        public bool ProcessCircles { get; set; } = true;
        public bool ProcessArcs { get; set; } = true;
        public bool ProcessBlocks { get; set; } = true;
        public bool ProcessDynamicBlocks { get; set; } = true;
        public bool ProcessDimensions { get; set; } = true;
        
        // �϶��B�z�ﶵ�]�O�d�{���\��^
        public bool UseBlockExplodeMethod { get; set; } = true;
        public bool UseReferenceEditMethod { get; set; } = false;
        public bool UseBlockEditorMethod { get; set; } = false;
        public bool EnableAutoRetry { get; set; } = true;
        public int MaxDepth { get; set; } = 50;
        
        public BlockProcessingMethod PreferredBlockMethod { get; set; } = BlockProcessingMethod.ExplodeRecombine;
        
        /// <summary>
        /// ����ҥΪ��B�z��k�C��]���u�����ǡ^
        /// </summary>
        public List<BlockProcessingMethod> GetEnabledMethods()
        {
            var methods = new List<BlockProcessingMethod>();
            
            // �l�ץ]�t�ǲΤ�k�@����¦
            methods.Add(BlockProcessingMethod.Traditional);
            
            // �ھڳ]�w�K�[��L��k
            if (UseBlockExplodeMethod)
                methods.Add(BlockProcessingMethod.ExplodeRecombine);
                
            if (UseReferenceEditMethod)
                methods.Add(BlockProcessingMethod.ReferenceEdit);
                
            if (UseBlockEditorMethod)
                methods.Add(BlockProcessingMethod.BlockEditor);
            
            return methods;
        }
        
        /// <summary>
        /// �ˬd�O�_�����󪫥������Q�ҥ�
        /// </summary>
        public bool HasAnyEntityTypeEnabled()
        {
            return ProcessPolylines || ProcessLines || ProcessCircles || ProcessArcs ||
                   ProcessBlocks || ProcessDynamicBlocks || ProcessDimensions;
        }
    }

    /// <summary>
    /// �϶��B�z��k�T�|
    /// </summary>
    public enum BlockProcessingMethod
    {
        /// <summary>�ǲΤ�k�]�Ȯɸ���^</summary>
        Traditional = 0,
        /// <summary>���ѭ��ժk</summary>
        ExplodeRecombine = 1,
        /// <summary>Reference Edit�]�{�a�s��^</summary>
        ReferenceEdit = 2,
        /// <summary>�϶��s�边</summary>
        BlockEditor = 3
    }

    /// <summary>
    /// �ഫ�i��
    /// </summary>
    public class ConversionProgress
    {
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public string CurrentOperation { get; set; } = string.Empty;
        public double PercentComplete => TotalCount > 0 ? (double)ProcessedCount / TotalCount * 100 : 0;
    }
}