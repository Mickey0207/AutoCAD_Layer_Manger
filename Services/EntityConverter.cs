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
    /// �����ഫ�����f
    /// </summary>
    public interface IEntityConverter
    {
        ConversionResult ConvertEntityToLayer(Transaction tr, Entity entity, string targetLayer, Matrix3d transform, ConversionOptions options, int depth = 0);
        bool IsGeometricEntity(Entity entity);
    }

    /// <summary>
    /// �����ഫ����{
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

            // ����L�����j
            if (depth > options.MaxDepth)
            {
                result.ErrorCount++;
                result.Errors.Add($"�F��̤j���j�`�� {options.MaxDepth}");
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
                    // ���䴩����������
                    result.SkippedCount = 1;
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"�ഫ����ɵo�Ϳ��~: {ex.Message}");
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

                // �p��զX�ܴ��x�}
                var blockTransform = blockRef.BlockTransform;
                var combinedTransform = blockTransform.PreMultiplyBy(transform);

                // ���j�B�z�϶���������
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

                // �ഫ�϶��Ѧҥ�����ؼйϼh
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
                result.Errors.Add($"�B�z�϶��ѦҮɵo�Ϳ��~: {ex.Message}");
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
                result.Errors.Add($"�ഫ�X�����ɵo�Ϳ��~: {ex.Message}");
            }

            return result;
        }

        private bool TryConvertEntityLayer(Transaction tr, Entity entity, string targetLayer, ConversionOptions options)
        {
            try
            {
                // �ˬd��e�ϼh�O�_�Q��w
                if (options.SkipLockedObjects && IsEntityOnLockedLayer(tr, entity))
                {
                    return false;
                }

                // �ɯŬ��g�J�Ҧ����ܧ�ϼh
                entity.UpgradeOpen();
                entity.Layer = targetLayer;
                entity.DowngradeOpen();
                return true;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
            {
                // ����b��w�ϼh�W
                return false;
            }
            catch
            {
                // ��L���~�A���s�ߥX
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