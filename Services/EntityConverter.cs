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
            typeof(Ellipse), typeof(DBPoint), typeof(Ray), typeof(Xline),
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
                // �ˬd����򥻪��A
                if (entity == null)
                {
                    result.ErrorCount++;
                    result.Errors.Add("���鬰null");
                    return result;
                }

                if (entity.IsErased)
                {
                    result.SkippedCount++;
                    result.Errors.Add("����w�Q�R��");
                    return result;
                }

                // �ˬd�O�_����w�ϼh�W���е��ΰʺA�϶�
                if (options.ProcessAnnotationsOnLockedLayers && IsEntityOnLockedLayer(tr, entity))
                {
                    if (IsAnnotationOrDynamicBlock(entity))
                    {
                        return ProcessAnnotationOnLockedLayer(tr, entity, targetLayer, options);
                    }
                }

                if (entity is BlockReference blockRef && options.ProcessBlocks)
                {
                    // �϶��B�z�޿� - �ϥΦh������
                    result = ProcessBlockReferenceWithStrategy(tr, blockRef, targetLayer, transform, options, depth);
                }
                else if (IsGeometricEntity(entity))
                {
                    result = ProcessGeometricEntity(tr, entity, targetLayer, options);
                }
                else
                {
                    // ���䴩�����������A���ѸԲӤ��R
                    result.SkippedCount = 1;
                    string analysis = AnalyzeConversionFailure(tr, entity, options);
                    result.Errors.Add($"�L�k�B�z���� {entity.GetType().Name}: {analysis}");
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
            {
                result.ErrorCount++;
                result.Errors.Add($"�϶��B�z����: eOnLockedLayer - �N���ըϥι϶��s�边��k");
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"�ഫ����ɵo�Ϳ��~: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ConvertEntityToLayer exception: {ex}");
            }

            return result;
        }

        /// <summary>
        /// �ϥδ���h�������B�z�϶��Ѧ�
        /// </summary>
        private ConversionResult ProcessBlockReferenceWithStrategy(
            Transaction tr, 
            BlockReference blockRef, 
            string targetLayer, 
            Matrix3d transform, 
            ConversionOptions options, 
            int depth)
        {
            var result = new ConversionResult();
            var attemptHistory = new List<string>();

            try
            {
                bool isOnLockedLayer = IsEntityOnLockedLayer(tr, blockRef);
                
                // ����n���ժ���k�C��
                var methodsToTry = options.GetEnabledMethods();
                
                // �p�G�O��w�ϼh�B���j���ഫ�A�u���հ��Ť�k
                if (isOnLockedLayer && !options.ForceConvertLockedObjects)
                {
                    methodsToTry.RemoveAll(m => m == BlockProcessingMethod.Traditional);
                }
                
                foreach (var method in methodsToTry)
                {
                    try
                    {
                        ConversionResult methodResult = method switch
                        {
                            BlockProcessingMethod.Traditional => ProcessBlockReference(tr, blockRef, targetLayer, transform, options, depth),
                            BlockProcessingMethod.ExplodeRecombine => ProcessBlockReferenceWithExplode(tr, blockRef, targetLayer, transform, options, depth),
                            BlockProcessingMethod.ReferenceEdit => CreateNotImplementedResult("�{�a�s��k�ȥ�������{"),
                            BlockProcessingMethod.BlockEditor => CreateNotImplementedResult("�϶��s�边�k�ȥ�������{"),
                            _ => new ConversionResult { ErrorCount = 1, Errors = { "�������B�z��k" } }
                        };

                        attemptHistory.Add($"{GetMethodDisplayName(method)}: {methodResult.ConvertedCount}���\/{methodResult.ErrorCount}���~");

                        // �p�G���\�ഫ�F����A��^���G
                        if (methodResult.ConvertedCount > 0)
                        {
                            methodResult.Errors.Insert(0, $"? �ϥ� {GetMethodDisplayName(method)} ���\�B�z");
                            return methodResult;
                        }

                        // �p�G�S�����~���]�S���ഫ�]�i����L�^�A�]�������\
                        if (methodResult.ErrorCount == 0)
                        {
                            methodResult.Errors.Add($"�ϥ� {GetMethodDisplayName(method)} �B�z�]�L���~�^");
                            return methodResult;
                        }

                        // �p�G�ҥΦ۰ʭ��աA�~����դU�@�Ӥ�k
                        if (!options.EnableAutoRetry)
                        {
                            // ���ҥΦ۰ʭ��աA��^�Ĥ@�Ӥ�k�����G
                            methodResult.Errors.Add($"�ϥ� {GetMethodDisplayName(method)} �B�z�A���ҥΦ۰ʭ���");
                            return methodResult;
                        }

                        // �ֿn���~�H��
                        result.Errors.AddRange(methodResult.Errors);
                    }
                    catch (System.Exception ex)
                    {
                        attemptHistory.Add($"{GetMethodDisplayName(method)}: ���` - {ex.Message}");
                        result.Errors.Add($"{GetMethodDisplayName(method)} ���`: {ex.Message}");
                        
                        // �p�G���ҥΦ۰ʭ��աA�ߥX���`
                        if (!options.EnableAutoRetry)
                        {
                            break;
                        }
                    }
                }

                // �Ҧ���k�����չL�F�A��^���ѵ��G
                result.ErrorCount++;
                result.Errors.Add($"? �Ҧ��B�z��k�����ѤF");
                result.Errors.Add($"���հO��: {string.Join(" �� ", attemptHistory)}");
                
                return result;
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"�϶������B�z���`: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// �����k����ܦW��
        /// </summary>
        private string GetMethodDisplayName(BlockProcessingMethod method)
        {
            return method switch
            {
                BlockProcessingMethod.Traditional => "�ǲΤ�k",
                BlockProcessingMethod.ExplodeRecombine => "���ѭ��ժk",
                BlockProcessingMethod.ReferenceEdit => "�{�a�s��k",
                BlockProcessingMethod.BlockEditor => "�϶��s�边�k",
                _ => "������k"
            };
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
                // �ˬd�϶��O�_�i�H����
                if (!CanExplodeBlock(tr, blockRef))
                {
                    result.ErrorCount++;
                    result.Errors.Add($"�϶� {blockRef.Name} �L�k���ѡ]�i��O�ʺA�϶��Ψ��O�@�϶��^");
                    return result;
                }

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
                var explodedEntities = ExplodeBlockToBasicElements(tr, blockRef, transform, depth);
                if (explodedEntities.Count == 0)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"�L�k���ѹ϶� {blockRef.Name}");
                    return result;
                }

                // �B�J3: ���ഫ��¦�������ϼh�]�b�R����϶����e�^
                var convertedEntities = new List<Entity>();
                foreach (var entity in explodedEntities)
                {
                    try
                    {
                        // �ˬd����O�_�b��w�ϼh�W
                        if (IsEntityOnLockedLayer(tr, entity))
                        {
                            // ���ռȮɸ�����ഫ
                            if (TryConvertEntityLayerWithUnlock(tr, entity, targetLayer, options))
                            {
                                convertedEntities.Add(entity);
                                result.ConvertedCount++;
                            }
                            else
                            {
                                // �p�G���M�L�k�ഫ�A�O�����~���~��B�z��L����
                                result.ErrorCount++;
                                result.Errors.Add($"��¦�����ഫ����: {entity.GetType().Name}");
                                convertedEntities.Add(entity); // ���M�[�J�A�O�����c����
                            }
                        }
                        else
                        {
                            entity.Layer = targetLayer;
                            convertedEntities.Add(entity);
                            result.ConvertedCount++;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"�ഫ��¦��������: {ex.Message}");
                        convertedEntities.Add(entity); // ���M�[�J�A�O�����c����
                    }
                }

                // �B�J4: ���խ��s�զX���϶�
                if (convertedEntities.Count > 0)
                {
                    try
                    {
                        var newBlockRef = RecreateBlockFromEntities(tr, convertedEntities, originalBlockInfo);
                        if (newBlockRef != null)
                        {
                            // �B�J5: ���\��~�R����l�϶�
                            blockRef.UpgradeOpen();
                            blockRef.Erase();
                            result.ConvertedCount++; // �϶������]���ഫ���\
                        }
                        else
                        {
                            result.ErrorCount++;
                            result.Errors.Add($"���s�Ыع϶� {originalBlockInfo.Name} ����");
                            
                            // �p�G���ե��ѡA�N���Ѫ������[�J��ҫ��Ŷ�
                            AddEntitiesToModelSpace(tr, convertedEntities);
                            result.ConvertedCount += convertedEntities.Count;
                            
                            // ���M�R����϶�
                            blockRef.UpgradeOpen();
                            blockRef.Erase();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"�϶����չL�{����: {ex.Message}");
                        
                        // �Y�ϭ��ե��ѡA�]���իO�s���Ѫ�����
                        try
                        {
                            AddEntitiesToModelSpace(tr, convertedEntities);
                            result.ConvertedCount += convertedEntities.Count;
                            
                            blockRef.UpgradeOpen();
                            blockRef.Erase();
                        }
                        catch (System.Exception ex2)
                        {
                            result.Errors.Add($"�O�s���Ѥ�������: {ex2.Message}");
                        }
                    }
                }
                else
                {
                    result.ErrorCount++;
                    result.Errors.Add($"�S���i�ഫ����¦����");
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
        /// �ˬd�϶��O�_�i�H����
        /// </summary>
        private bool CanExplodeBlock(Transaction tr, BlockReference blockRef)
        {
            try
            {
                // �ˬd�϶��w�q�O�_�s�b
                var btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null) return false;

                // �ˬd�O�_�O�ʺA�϶�
                if (blockRef.IsDynamicBlock) 
                {
                    return true; // �ʺA�϶��]�i�H���դ���
                }

                // �ˬd�O�_�����i���Ѫ�����
                if (btr.Explodable == false) return false;

                // ���ճЫؤ@�Ӵ��ժ����Ѷ��X
                var testExplode = new DBObjectCollection();
                blockRef.Explode(testExplode);
                
                return testExplode.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ���ѹ϶���̰�¦�������]��i���^
        /// </summary>
        private List<Entity> ExplodeBlockToBasicElements(Transaction tr, BlockReference blockRef, Matrix3d transform, int depth)
        {
            var basicElements = new List<Entity>();
            
            try
            {
                // ����L�����j
                if (depth > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"���Ѳ`�׹L�`�A����j");
                    return basicElements;
                }

                // �Ыؤ��ѫ᪺���鶰�X
                var explodedEntities = new DBObjectCollection();
                blockRef.Explode(explodedEntities);

                foreach (Entity explodedEntity in explodedEntities)
                {
                    if (explodedEntity is BlockReference nestedBlockRef)
                    {
                        // ���j���ѴO�M�϶�
                        var nestedElements = ExplodeBlockToBasicElements(tr, nestedBlockRef, transform, depth + 1);
                        basicElements.AddRange(nestedElements);
                        
                        // ����O�M�϶��Ѧ�
                        nestedBlockRef.Dispose();
                    }
                    else
                    {
                        // ��¦�X�󤸯�
                        try
                        {
                            if (!transform.IsEqualTo(Matrix3d.Identity))
                            {
                                explodedEntity.TransformBy(transform);
                            }
                            basicElements.Add(explodedEntity);
                        }
                        catch (System.Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"�ഫ��������: {ex.Message}");
                            // �Y���ഫ���Ѥ]�K�[����
                            basicElements.Add(explodedEntity);
                        }
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
        /// �N����K�[��ҫ��Ŷ��]�@���ƥΤ�ס^
        /// </summary>
        private void AddEntitiesToModelSpace(Transaction tr, List<Entity> entities)
        {
            try
            {
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc?.Database == null) return;

                var modelSpace = tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                if (modelSpace == null) return;

                foreach (var entity in entities)
                {
                    try
                    {
                        modelSpace.AppendEntity(entity);
                        tr.AddNewlyCreatedDBObject(entity, true);
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"�K�[�����ҫ��Ŷ�����: {ex.Message}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddEntitiesToModelSpace����: {ex.Message}");
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

        /// <summary>
        /// �W�j���ϼh�ഫ��k�A�䴩�j��B�z��w�ϼh�W������
        /// </summary>
        private bool TryConvertEntityLayerWithUnlock(Transaction tr, Entity entity, string targetLayer, ConversionOptions options)
        {
            try
            {
                bool wasLayerLocked = false;
                LayerTableRecord? sourceLayerRecord = null;

                // �ˬd����O�_�i�H�ק�
                if (entity.IsReadEnabled && !entity.IsWriteEnabled)
                {
                    try
                    {
                        entity.UpgradeOpen();
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
                    {
                        // ����b��w�ϼh�W�A�ݭn�S��B�z
                    }
                }

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
                        try
                        {
                            sourceLayerRecord.UpgradeOpen();
                            sourceLayerRecord.IsLocked = false;
                            wasLayerLocked = true;
                        }
                        catch (System.Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"�L�k����ϼh {sourceLayerRecord.Name}: {ex.Message}");
                            return false;
                        }
                    }
                }

                try
                {
                    // �T�O����i�g
                    if (!entity.IsWriteEnabled)
                    {
                        entity.UpgradeOpen();
                    }
                    
                    // �ܧ�ϼh
                    string originalLayer = entity.Layer;
                    entity.Layer = targetLayer;
                    
                    // �����ഫ�O�_���\
                    if (entity.Layer != targetLayer)
                    {
                        System.Diagnostics.Debug.WriteLine($"�ϼh�ഫ���ҥ���: ���� {targetLayer}, ��� {entity.Layer}");
                        return false;
                    }
                    
                    entity.DowngradeOpen();
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
                {
                    System.Diagnostics.Debug.WriteLine($"���餴�b��w�ϼh�W: {ex.Message}");
                    return false;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == ErrorStatus.NotOpenForWrite)
                {
                    System.Diagnostics.Debug.WriteLine($"���饼�}�Ҽg�J�v��: {ex.Message}");
                    return false;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AutoCAD���~ {ex.ErrorStatus}: {ex.Message}");
                    return false;
                }
                finally
                {
                    // ��_���ϼh����w���A
                    if (wasLayerLocked && sourceLayerRecord != null)
                    {
                        try
                        {
                            if (!sourceLayerRecord.IsWriteEnabled)
                            {
                                sourceLayerRecord.UpgradeOpen();
                            }
                            sourceLayerRecord.IsLocked = true;
                            sourceLayerRecord.DowngradeOpen();
                        }
                        catch (System.Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"��_�ϼh��w���A����: {ex.Message}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                // �O���Բӿ��~�����ߥX���`
                System.Diagnostics.Debug.WriteLine($"�ഫ����ϼh�ɵo�Ϳ��~: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// �B�z�X����骺�ഫ�]��i���^
        /// </summary>
        private ConversionResult ProcessGeometricEntity(
            Transaction tr, 
            Entity entity, 
            string targetLayer, 
            ConversionOptions options)
        {
            var result = new ConversionResult();

            try
            {
                // �ˬd���������O�_�䴩
                if (!IsGeometricEntity(entity))
                {
                    result.SkippedCount = 1;
                    result.Errors.Add($"���䴩����������: {entity.GetType().Name}");
                    return result;
                }

                // �ˬd����O�_�w�Q�R��
                if (entity.IsErased)
                {
                    result.SkippedCount = 1;
                    result.Errors.Add($"����w�Q�R��: {entity.GetType().Name}");
                    return result;
                }

                // �����ഫ
                if (TryConvertEntityLayerWithUnlock(tr, entity, targetLayer, options))
                {
                    result.ConvertedCount = 1;
                }
                else
                {
                    result.SkippedCount = 1;
                    
                    // ���ѧ�ԲӪ����~�H��
                    string errorDetail = AnalyzeConversionFailure(tr, entity, options);
                    
                    result.Errors.Add($"�L�k�ഫ���� {entity.GetType().Name}: {errorDetail}");
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
        /// �ˬd����O�_�b��w�ϼh�W�A�ê�^�ϼh�O���]�W�j���^
        /// </summary>
        private bool IsEntityOnLockedLayer(Transaction tr, Entity entity, out LayerTableRecord? layerRecord)
        {
            layerRecord = null;
            
            try
            {
                Document doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc == null) return false;

                Database db = entity.Database;
                var layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                
                if (layerTable?.Has(entity.Layer) == true)
                {
                    layerRecord = tr.GetObject(layerTable[entity.Layer], OpenMode.ForRead) as LayerTableRecord;
                    return layerRecord?.IsLocked == true;
                }

                return false;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"�ˬd�ϼh��w���A����: {ex.Message}");
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
        /// �ԲӪ����~���R��k
        /// </summary>
        private string AnalyzeConversionFailure(Transaction tr, Entity entity, ConversionOptions options)
        {
            try
            {
                var reasons = new List<string>();

                // �ˬd���骬�A
                if (entity.IsErased)
                {
                    reasons.Add("����w�Q�R��");
                }

                if (entity.IsReadEnabled && !entity.IsWriteEnabled)
                {
                    reasons.Add("���鬰��Ū���A");
                }

                // �ˬd�ϼh���A
                if (IsEntityOnLockedLayer(tr, entity, out var layerRecord))
                {
                    if (!options.ForceConvertLockedObjects)
                    {
                        reasons.Add("�����w�ϼh�B���ҥαj���ഫ");
                    }
                    else
                    {
                        reasons.Add("�����w�ϼh�A�j���ഫ����");
                    }
                }

                // �ˬd��������
                if (!IsGeometricEntity(entity) && !(entity is BlockReference))
                {
                    reasons.Add($"���䴩����������: {entity.GetType().Name}");
                }

                // �S������ˬd
                if (entity is BlockReference blockRef)
                {
                    var btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    if (btr?.Explodable == false)
                    {
                        reasons.Add("�϶����i����");
                    }
                }

                return reasons.Count > 0 ? string.Join(", ", reasons) : "������]";
            }
            catch (System.Exception ex)
            {
                return $"���R����: {ex.Message}";
            }
        }

        /// <summary>
        /// �ˬd����O�_���X�����
        /// </summary>
        public bool IsGeometricEntity(Entity entity)
        {
            return entity is Curve ||
                   entity is Autodesk.AutoCAD.DatabaseServices.Region ||
                   entity is Body ||
                   entity is Face ||
                   entity is Autodesk.AutoCAD.DatabaseServices.Surface ||
                   entity is Solid3d ||
                   entity is Hatch ||
                   entity is MText ||
                   entity is DBText ||
                   entity is Dimension ||  // �s�W�G�ؤo�е�
                   entity is Leader ||     // �s�W�G�޽u
                   entity is MLeader ||    // �s�W�G�h���޽u
                   entity is Table ||      // �s�W�G���
                   entity is Polyline ||
                   entity is Polyline2d ||
                   entity is Polyline3d ||
                   entity is Line ||
                   entity is Arc ||
                   entity is Circle ||
                   entity is Ellipse ||
                   entity is DBPoint ||    // �ץ��G�ϥ�DBPoint�Ӥ��OPoint
                   entity is Spline ||
                   entity is Ray ||
                   entity is Xline ||
                   entity is Shape;
        }

        /// <summary>
        /// �Ыإ���{��k�����G
        /// </summary>
        private ConversionResult CreateNotImplementedResult(string methodName)
        {
            var result = new ConversionResult();
            result.ErrorCount++;
            result.Errors.Add($"{methodName}�A��ĳ�ϥΤ��ѭ��ժk�ζǲΤ�k");
            return result;
        }

        /// <summary>
        /// �ˬd�O�_���е��ΰʺA�϶�
        /// </summary>
        private bool IsAnnotationOrDynamicBlock(Entity entity)
        {
            return entity is Dimension ||
                   entity is Leader ||
                   entity is MLeader ||
                   (entity is BlockReference blockRef && IsDynamicBlock(blockRef));
        }

        /// <summary>
        /// �ˬd�϶��O�_���ʺA�϶�
        /// </summary>
        private bool IsDynamicBlock(BlockReference blockRef)
        {
            try
            {
                return blockRef.IsDynamicBlock || 
                       blockRef.DynamicBlockTableRecord != ObjectId.Null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// �B�z��w�ϼh�W���е��ΰʺA�϶�
        /// </summary>
        private ConversionResult ProcessAnnotationOnLockedLayer(
            Transaction tr, 
            Entity entity, 
            string targetLayer, 
            ConversionOptions options)
        {
            var result = new ConversionResult();

            try
            {
                // �������Ҧb���ϼh
                string sourceLayerName = entity.Layer;
                
                // �Ȯɸ��귽�ϼh
                var layerTable = tr.GetObject(entity.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (layerTable != null && layerTable.Has(sourceLayerName))
                {
                    var sourceLayerRecord = tr.GetObject(layerTable[sourceLayerName], OpenMode.ForRead) as LayerTableRecord;
                    bool wasLocked = false;
                    
                    if (sourceLayerRecord != null && sourceLayerRecord.IsLocked)
                    {
                        wasLocked = true;
                        sourceLayerRecord.UpgradeOpen();
                        sourceLayerRecord.IsLocked = false;
                        sourceLayerRecord.DowngradeOpen();
                    }

                    try
                    {
                        // �ഫ����ϼh
                        if (TryConvertEntityLayer(tr, entity, targetLayer, options))
                        {
                            result.ConvertedCount++;
                            result.Errors.Add($"�w�B�z��w�ϼh�W��{GetEntityTypeName(entity)}: {entity.GetType().Name}");
                        }
                        else
                        {
                            result.ErrorCount++;
                            result.Errors.Add($"�L�k�ഫ��w�ϼh�W��{GetEntityTypeName(entity)}");
                        }
                    }
                    finally
                    {
                        // ��_�ϼh��w���A
                        if (wasLocked && options.RestoreLayerLockState)
                        {
                            if (sourceLayerRecord.IsWriteEnabled)
                            {
                                sourceLayerRecord.IsLocked = true;
                            }
                            else
                            {
                                sourceLayerRecord.UpgradeOpen();
                                sourceLayerRecord.IsLocked = true;
                                sourceLayerRecord.DowngradeOpen();
                            }
                        }
                    }
                }
                else
                {
                    result.ErrorCount++;
                    result.Errors.Add($"�䤣�췽�ϼh: {sourceLayerName}");
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"�B�z��w�ϼh�W���е�����: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// ��������������͵��W��
        /// </summary>
        private string GetEntityTypeName(Entity entity)
        {
            return entity switch
            {
                Dimension => "�ؤo�е�",
                Leader => "�޽u",
                MLeader => "�h���޽u",
                BlockReference blockRef when IsDynamicBlock(blockRef) => "�ʺA�϶�",
                BlockReference => "�϶�",
                _ => "����"
            };
        }

        /// <summary>
        /// �����ഫ����ϼh�]�򥻤�k�^
        /// </summary>
        private bool TryConvertEntityLayer(Transaction tr, Entity entity, string targetLayer, ConversionOptions options)
        {
            try
            {
                entity.UpgradeOpen();
                entity.Layer = targetLayer;
                entity.DowngradeOpen();
                return true;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"�ഫ����ϼh����: {ex.Message}");
                return false;
            }
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