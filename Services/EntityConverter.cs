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
    /// �W�j�������ഫ�� - �䴩�϶����ѭ��ժk
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
                    // �ϥηs���϶��B�z��k
                    if (options.UseBlockExplodeMethod && IsEntityOnLockedLayer(tr, blockRef))
                    {
                        result = ProcessBlockReferenceWithExplode(tr, blockRef, targetLayer, transform, options, depth);
                    }
                    else
                    {
                        result = ProcessBlockReference(tr, blockRef, targetLayer, transform, options, depth);
                    }
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

        /// <summary>
        /// �ϥΤ��ѭ��ժk�B�z��w�ϼh�W���϶�
        /// </summary>
        private ConversionResult ProcessBlockReferenceWithExplode(
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
                // �B�J1: �O����l�϶���T
                var originalBlockInfo = new BlockInfo
                {
                    Name = blockRef.Name,
                    Position = blockRef.Position,
                    Rotation = blockRef.Rotation,
                    ScaleFactors = blockRef.ScaleFactors,
                    Layer = targetLayer, // �s�϶����ϼh
                    Attributes = ExtractAttributes(tr, blockRef)
                };

                // �B�J2: ���ѹ϶����¦����
                var explodedEntities = ExplodeBlockToBasicElements(tr, blockRef, transform);
                if (explodedEntities.Count == 0)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"�L�k���ѹ϶� {blockRef.Name}");
                    return result;
                }

                // �B�J3: �R����l�϶�
                blockRef.UpgradeOpen();
                blockRef.Erase();

                // �B�J4: �ഫ��¦�������ϼh
                var convertedEntities = new List<Entity>();
                foreach (var entity in explodedEntities)
                {
                    try
                    {
                        entity.Layer = targetLayer;
                        convertedEntities.Add(entity);
                        result.ConvertedCount++;
                    }
                    catch (System.Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"�ഫ��¦��������: {ex.Message}");
                    }
                }

                // �B�J5: ���s�զX���϶�
                if (convertedEntities.Count > 0)
                {
                    var newBlockRef = RecreateBlockFromEntities(tr, convertedEntities, originalBlockInfo);
                    if (newBlockRef != null)
                    {
                        result.ConvertedCount++; // �϶������]���ഫ���\
                    }
                    else
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"���s�Ыع϶� {originalBlockInfo.Name} ����");
                    }
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"�϶����ѭ��ժk�B�z����: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// ���ѹ϶���̰�¦������
        /// </summary>
        private List<Entity> ExplodeBlockToBasicElements(Transaction tr, BlockReference blockRef, Matrix3d transform)
        {
            var basicElements = new List<Entity>();
            
            try
            {
                // �Ыؤ��ѫ᪺���鶰�X
                var explodedEntities = new DBObjectCollection();
                blockRef.Explode(explodedEntities);

                foreach (Entity explodedEntity in explodedEntities)
                {
                    if (explodedEntity is BlockReference nestedBlockRef)
                    {
                        // ���j���ѴO�M�϶�
                        var nestedElements = ExplodeBlockToBasicElements(tr, nestedBlockRef, transform);
                        basicElements.AddRange(nestedElements);
                    }
                    else
                    {
                        // ��¦�X�󤸯�
                        explodedEntity.TransformBy(transform);
                        basicElements.Add(explodedEntity);
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"���ѹ϶�����: {ex.Message}");
            }

            return basicElements;
        }

        /// <summary>
        /// ���s�Ыع϶�
        /// </summary>
        private BlockReference? RecreateBlockFromEntities(Transaction tr, List<Entity> entities, BlockInfo blockInfo)
        {
            try
            {
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc?.Database == null) return null;

                var db = doc.Database;
                
                // �Ыذߤ@���϶��W�١]�p�G�ݭn�^
                string newBlockName = GetUniqueBlockName(tr, blockInfo.Name);

                // �Ыطs���϶��w�q
                using (var newBlockRecord = new BlockTableRecord())
                {
                    newBlockRecord.Name = newBlockName;
                    newBlockRecord.Origin = Point3d.Origin;

                    // ����϶���
                    var blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                    if (blockTable == null) return null;

                    // �K�[�϶��w�q��϶���
                    var blockId = blockTable.Add(newBlockRecord);
                    tr.AddNewlyCreatedDBObject(newBlockRecord, true);

                    // �N����K�[��϶��w�q
                    foreach (var entity in entities)
                    {
                        newBlockRecord.AppendEntity(entity);
                        tr.AddNewlyCreatedDBObject(entity, true);
                    }

                    // �Ыع϶��Ѧ�
                    var newBlockRef = new BlockReference(blockInfo.Position, blockId)
                    {
                        Rotation = blockInfo.Rotation,
                        ScaleFactors = blockInfo.ScaleFactors,
                        Layer = blockInfo.Layer
                    };

                    // �K�[��ҫ��Ŷ�
                    var modelSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                    if (modelSpace != null)
                    {
                        modelSpace.AppendEntity(newBlockRef);
                        tr.AddNewlyCreatedDBObject(newBlockRef, true);

                        // ��_�ݩ�
                        RestoreAttributes(tr, newBlockRef, blockInfo.Attributes);

                        return newBlockRef;
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"���s�Ыع϶�����: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// ����ߤ@���϶��W��
        /// </summary>
        private string GetUniqueBlockName(Transaction tr, string baseName)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc?.Database == null) return baseName;

            var blockTable = tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
            if (blockTable == null) return baseName;

            string uniqueName = baseName;
            int counter = 1;

            while (blockTable.Has(uniqueName))
            {
                uniqueName = $"{baseName}_{counter}";
                counter++;
            }

            return uniqueName;
        }

        /// <summary>
        /// �����϶��ݩ�
        /// </summary>
        private List<AttributeInfo> ExtractAttributes(Transaction tr, BlockReference blockRef)
        {
            var attributes = new List<AttributeInfo>();
            
            try
            {
                foreach (ObjectId attId in blockRef.AttributeCollection)
                {
                    if (tr.GetObject(attId, OpenMode.ForRead) is AttributeReference attRef)
                    {
                        attributes.Add(new AttributeInfo
                        {
                            Tag = attRef.Tag,
                            TextString = attRef.TextString,
                            Position = attRef.Position,
                            Height = attRef.Height,
                            Rotation = attRef.Rotation
                        });
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"�����ݩʥ���: {ex.Message}");
            }

            return attributes;
        }

        /// <summary>
        /// ��_�϶��ݩ�
        /// </summary>
        private void RestoreAttributes(Transaction tr, BlockReference blockRef, List<AttributeInfo> attributes)
        {
            try
            {
                foreach (var attrInfo in attributes)
                {
                    foreach (ObjectId attId in blockRef.AttributeCollection)
                    {
                        if (tr.GetObject(attId, OpenMode.ForWrite) is AttributeReference attRef &&
                            attRef.Tag == attrInfo.Tag)
                        {
                            attRef.TextString = attrInfo.TextString;
                            attRef.Position = attrInfo.Position;
                            attRef.Height = attrInfo.Height;
                            attRef.Rotation = attrInfo.Rotation;
                            break;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"��_�ݩʥ���: {ex.Message}");
            }
        }

        /// <summary>
        /// �ǲι϶��B�z��k�]�V�U�ݮe�^
        /// </summary>
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
                if (TryConvertEntityLayerWithUnlock(tr, blockRef, targetLayer, options))
                {
                    result.ConvertedCount++;
                }
                else
                {
                    result.SkippedCount++;
                    result.Errors.Add($"�L�k�ഫ�϶� {blockRef.Name} ��ϼh {targetLayer}");
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
                if (TryConvertEntityLayerWithUnlock(tr, entity, targetLayer, options))
                {
                    result.ConvertedCount = 1;
                }
                else
                {
                    result.SkippedCount = 1;
                    result.Errors.Add($"�L�k�ഫ���� {entity.GetType().Name} ��ϼh {targetLayer}");
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"�ഫ�X�����ɵo�Ϳ��~: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// �W�j���ϼh�ഫ��k�A�䴩�j��B�z��w�ϼh�W������
        /// </summary>
        private bool TryConvertEntityLayerWithUnlock(Transaction tr, Entity entity, string targetLayer, ConversionOptions options)
        {
            try
            {
                bool wasLayerLocked = false;
                LayerTableRecord? sourceLayerRecord = null;

                // �ˬd��e�ϼh�O�_�Q��w
                if (IsEntityOnLockedLayer(tr, entity, out sourceLayerRecord))
                {
                    if (!options.ForceConvertLockedObjects)
                    {
                        // �p�G���j���ഫ��w����A�h���L
                        return false;
                    }

                    // �Ȯɸ��귽�ϼh�H���\�ק�
                    if (sourceLayerRecord != null && sourceLayerRecord.IsLocked)
                    {
                        sourceLayerRecord.UpgradeOpen();
                        sourceLayerRecord.IsLocked = false;
                        wasLayerLocked = true;
                    }
                }

                try
                {
                    // �ɯŬ��g�J�Ҧ����ܧ�ϼh
                    entity.UpgradeOpen();
                    entity.Layer = targetLayer;
                    entity.DowngradeOpen();
                    return true;
                }
                finally
                {
                    // ��_���ϼh����w���A
                    if (wasLayerLocked && sourceLayerRecord != null)
                    {
                        if (!sourceLayerRecord.IsWriteEnabled)
                        {
                            sourceLayerRecord.UpgradeOpen();
                        }
                        sourceLayerRecord.IsLocked = true;
                        sourceLayerRecord.DowngradeOpen();
                    }
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
            {
                // �Y�ϼȮɸ���]�L�k�ק�A�i��O��L��]
                return false;
            }
            catch (System.Exception ex)
            {
                // �O���Բӿ��~�����ߥX���`
                System.Diagnostics.Debug.WriteLine($"�ഫ����ϼh�ɵo�Ϳ��~: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// �ˬd����O�_�b��w�ϼh�W�A�ê�^�ϼh�O��
        /// </summary>
        private bool IsEntityOnLockedLayer(Transaction tr, Entity entity, out LayerTableRecord? layerRecord)
        {
            layerRecord = null;
            
            try
            {
                Document doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc == null) return false;

                Database db = doc.Database;
                var layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                
                if (layerTable?.Has(entity.Layer) == true)
                {
                    layerRecord = tr.GetObject(layerTable[entity.Layer], OpenMode.ForRead) as LayerTableRecord;
                    return layerRecord?.IsLocked == true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// �ˬd����O�_�b��w�ϼh�W�]²�ƪ����^
        /// </summary>
        private bool IsEntityOnLockedLayer(Transaction tr, Entity entity)
        {
            return IsEntityOnLockedLayer(tr, entity, out _);
        }

        public bool IsGeometricEntity(Entity entity)
        {
            return entity != null && GeometricEntityTypes.Contains(entity.GetType());
        }
    }

    /// <summary>
    /// �϶���T���O
    /// </summary>
    public class BlockInfo
    {
        public string Name { get; set; } = string.Empty;
        public Point3d Position { get; set; }
        public double Rotation { get; set; }
        public Scale3d ScaleFactors { get; set; }
        public string Layer { get; set; } = string.Empty;
        public List<AttributeInfo> Attributes { get; set; } = new List<AttributeInfo>();
    }

    /// <summary>
    /// �ݩʸ�T���O
    /// </summary>
    public class AttributeInfo
    {
        public string Tag { get; set; } = string.Empty;
        public string TextString { get; set; } = string.Empty;
        public Point3d Position { get; set; }
        public double Height { get; set; }
        public double Rotation { get; set; }
    }
}