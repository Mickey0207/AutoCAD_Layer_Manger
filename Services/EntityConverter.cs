using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCAD_Layer_Manger.Services
{
    /// <summary>
    /// 實體轉換器接口
    /// </summary>
    public interface IEntityConverter
    {
        ConversionResult ConvertEntityToLayer(Transaction tr, Entity entity, string targetLayer, Matrix3d transform, ConversionOptions options, int depth = 0);
        bool IsGeometricEntity(Entity entity);
    }

    /// <summary>
    /// 實體轉換器實現
    /// </summary>
    public class EntityConverter : IEntityConverter
    {
        private static readonly HashSet<Type> GeometricEntityTypes = new()
        {
            typeof(Line), typeof(Arc), typeof(Circle), typeof(Polyline),
            typeof(Polyline2d), typeof(Polyline3d), typeof(Spline),
            typeof(Ellipse), typeof(Point), typeof(Ray), typeof(Xline),
            typeof(Hatch), typeof(Solid), typeof(Face), typeof(Trace),
            typeof(MText), typeof(DBText), typeof(Dimension), typeof(Leader),
            typeof(MLeader), typeof(Wipeout), typeof(Autodesk.AutoCAD.DatabaseServices.Image),
            typeof(Ole2Frame), typeof(Solid3d), typeof(Autodesk.AutoCAD.DatabaseServices.Surface),
            typeof(SubDMesh), typeof(Autodesk.AutoCAD.DatabaseServices.Region)
        };

        public ConversionResult ConvertEntityToLayer(
            Transaction tr, 
            Entity entity, 
            string targetLayer, 
            Matrix3d transform, 
            ConversionOptions options, 
            int depth = 0)
        {
            var result = new ConversionResult();

            // 防止無限遞迴
            if (depth > options.MaxDepth)
            {
                result.ErrorCount++;
                result.Errors.Add($"達到最大遞迴深度 {options.MaxDepth}");
                return result;
            }

            try
            {
                if (entity is BlockReference blockRef && options.ProcessBlocks)
                {
                    result = ProcessBlockReference(tr, blockRef, targetLayer, transform, options, depth);
                }
                else if (IsGeometricEntity(entity))
                {
                    result = ProcessGeometricEntity(tr, entity, targetLayer, options);
                }
                else
                {
                    // 不支援的實體類型
                    result.SkippedCount = 1;
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"轉換實體時發生錯誤: {ex.Message}");
            }

            return result;
        }

        private ConversionResult ProcessBlockReference(
            Transaction tr, 
            BlockReference blockRef, 
            string targetLayer, 
            Matrix3d transform, 
            ConversionOptions options, 
            int depth)
        {
            var result = new ConversionResult();

            try
            {
                var btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null) return result;

                // 計算組合變換矩陣
                var blockTransform = blockRef.BlockTransform;
                var combinedTransform = blockTransform.PreMultiplyBy(transform);

                // 遞迴處理圖塊內的物件
                foreach (ObjectId objId in btr)
                {
                    if (tr.GetObject(objId, OpenMode.ForRead) is Entity blockEntity)
                    {
                        var subResult = ConvertEntityToLayer(
                            tr, blockEntity, targetLayer, combinedTransform, options, depth + 1);
                        
                        result.ConvertedCount += subResult.ConvertedCount;
                        result.SkippedCount += subResult.SkippedCount;
                        result.ErrorCount += subResult.ErrorCount;
                        result.Errors.AddRange(subResult.Errors);
                    }
                }

                // 轉換圖塊參考本身到目標圖層
                if (TryConvertEntityLayer(tr, blockRef, targetLayer, options))
                {
                    result.ConvertedCount++;
                }
                else
                {
                    result.SkippedCount++;
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"處理圖塊參考時發生錯誤: {ex.Message}");
            }

            return result;
        }

        private ConversionResult ProcessGeometricEntity(
            Transaction tr, 
            Entity entity, 
            string targetLayer, 
            ConversionOptions options)
        {
            var result = new ConversionResult();

            try
            {
                if (TryConvertEntityLayer(tr, entity, targetLayer, options))
                {
                    result.ConvertedCount = 1;
                }
                else
                {
                    result.SkippedCount = 1;
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"轉換幾何實體時發生錯誤: {ex.Message}");
            }

            return result;
        }

        private bool TryConvertEntityLayer(Transaction tr, Entity entity, string targetLayer, ConversionOptions options)
        {
            try
            {
                // 檢查當前圖層是否被鎖定
                if (options.SkipLockedObjects && IsEntityOnLockedLayer(tr, entity))
                {
                    return false;
                }

                // 升級為寫入模式並變更圖層
                entity.UpgradeOpen();
                entity.Layer = targetLayer;
                entity.DowngradeOpen();
                return true;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
            {
                // 物件在鎖定圖層上
                return false;
            }
            catch
            {
                // 其他錯誤，重新拋出
                throw;
            }
        }

        private bool IsEntityOnLockedLayer(Transaction tr, Entity entity)
        {
            try
            {
                Document doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc == null) return false;

                Database db = doc.Database;
                var layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                
                if (layerTable?.Has(entity.Layer) == true)
                {
                    var currentLayer = tr.GetObject(layerTable[entity.Layer], OpenMode.ForRead) as LayerTableRecord;
                    return currentLayer?.IsLocked == true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsGeometricEntity(Entity entity)
        {
            return entity != null && GeometricEntityTypes.Contains(entity.GetType());
        }
    }
}