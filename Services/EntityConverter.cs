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
    /// 增強版實體轉換器 - 支援圖塊分解重組法
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
                    // 使用新的圖塊處理方法
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

        /// <summary>
        /// 使用分解重組法處理鎖定圖層上的圖塊
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
                // 步驟1: 記錄原始圖塊資訊
                var originalBlockInfo = new BlockInfo
                {
                    Name = blockRef.Name,
                    Position = blockRef.Position,
                    Rotation = blockRef.Rotation,
                    ScaleFactors = blockRef.ScaleFactors,
                    Layer = targetLayer, // 新圖塊的圖層
                    Attributes = ExtractAttributes(tr, blockRef)
                };

                // 步驟2: 分解圖塊到基礎元素
                var explodedEntities = ExplodeBlockToBasicElements(tr, blockRef, transform);
                if (explodedEntities.Count == 0)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"無法分解圖塊 {blockRef.Name}");
                    return result;
                }

                // 步驟3: 刪除原始圖塊
                blockRef.UpgradeOpen();
                blockRef.Erase();

                // 步驟4: 轉換基礎元素的圖層
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
                        result.Errors.Add($"轉換基礎元素失敗: {ex.Message}");
                    }
                }

                // 步驟5: 重新組合成圖塊
                if (convertedEntities.Count > 0)
                {
                    var newBlockRef = RecreateBlockFromEntities(tr, convertedEntities, originalBlockInfo);
                    if (newBlockRef != null)
                    {
                        result.ConvertedCount++; // 圖塊本身也算轉換成功
                    }
                    else
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"重新創建圖塊 {originalBlockInfo.Name} 失敗");
                    }
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"圖塊分解重組法處理失敗: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 分解圖塊到最基礎的元素
        /// </summary>
        private List<Entity> ExplodeBlockToBasicElements(Transaction tr, BlockReference blockRef, Matrix3d transform)
        {
            var basicElements = new List<Entity>();
            
            try
            {
                // 創建分解後的實體集合
                var explodedEntities = new DBObjectCollection();
                blockRef.Explode(explodedEntities);

                foreach (Entity explodedEntity in explodedEntities)
                {
                    if (explodedEntity is BlockReference nestedBlockRef)
                    {
                        // 遞迴分解嵌套圖塊
                        var nestedElements = ExplodeBlockToBasicElements(tr, nestedBlockRef, transform);
                        basicElements.AddRange(nestedElements);
                    }
                    else
                    {
                        // 基礎幾何元素
                        explodedEntity.TransformBy(transform);
                        basicElements.Add(explodedEntity);
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"分解圖塊失敗: {ex.Message}");
            }

            return basicElements;
        }

        /// <summary>
        /// 重新創建圖塊
        /// </summary>
        private BlockReference? RecreateBlockFromEntities(Transaction tr, List<Entity> entities, BlockInfo blockInfo)
        {
            try
            {
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc?.Database == null) return null;

                var db = doc.Database;
                
                // 創建唯一的圖塊名稱（如果需要）
                string newBlockName = GetUniqueBlockName(tr, blockInfo.Name);

                // 創建新的圖塊定義
                using (var newBlockRecord = new BlockTableRecord())
                {
                    newBlockRecord.Name = newBlockName;
                    newBlockRecord.Origin = Point3d.Origin;

                    // 獲取圖塊表
                    var blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                    if (blockTable == null) return null;

                    // 添加圖塊定義到圖塊表
                    var blockId = blockTable.Add(newBlockRecord);
                    tr.AddNewlyCreatedDBObject(newBlockRecord, true);

                    // 將實體添加到圖塊定義
                    foreach (var entity in entities)
                    {
                        newBlockRecord.AppendEntity(entity);
                        tr.AddNewlyCreatedDBObject(entity, true);
                    }

                    // 創建圖塊參考
                    var newBlockRef = new BlockReference(blockInfo.Position, blockId)
                    {
                        Rotation = blockInfo.Rotation,
                        ScaleFactors = blockInfo.ScaleFactors,
                        Layer = blockInfo.Layer
                    };

                    // 添加到模型空間
                    var modelSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                    if (modelSpace != null)
                    {
                        modelSpace.AppendEntity(newBlockRef);
                        tr.AddNewlyCreatedDBObject(newBlockRef, true);

                        // 恢復屬性
                        RestoreAttributes(tr, newBlockRef, blockInfo.Attributes);

                        return newBlockRef;
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重新創建圖塊失敗: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 獲取唯一的圖塊名稱
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
        /// 提取圖塊屬性
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
                System.Diagnostics.Debug.WriteLine($"提取屬性失敗: {ex.Message}");
            }

            return attributes;
        }

        /// <summary>
        /// 恢復圖塊屬性
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
                System.Diagnostics.Debug.WriteLine($"恢復屬性失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 傳統圖塊處理方法（向下兼容）
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
                if (TryConvertEntityLayerWithUnlock(tr, blockRef, targetLayer, options))
                {
                    result.ConvertedCount++;
                }
                else
                {
                    result.SkippedCount++;
                    result.Errors.Add($"無法轉換圖塊 {blockRef.Name} 到圖層 {targetLayer}");
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
                if (TryConvertEntityLayerWithUnlock(tr, entity, targetLayer, options))
                {
                    result.ConvertedCount = 1;
                }
                else
                {
                    result.SkippedCount = 1;
                    result.Errors.Add($"無法轉換實體 {entity.GetType().Name} 到圖層 {targetLayer}");
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"轉換幾何實體時發生錯誤: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 增強的圖層轉換方法，支援強制處理鎖定圖層上的物件
        /// </summary>
        private bool TryConvertEntityLayerWithUnlock(Transaction tr, Entity entity, string targetLayer, ConversionOptions options)
        {
            try
            {
                bool wasLayerLocked = false;
                LayerTableRecord? sourceLayerRecord = null;

                // 檢查當前圖層是否被鎖定
                if (IsEntityOnLockedLayer(tr, entity, out sourceLayerRecord))
                {
                    if (!options.ForceConvertLockedObjects)
                    {
                        // 如果不強制轉換鎖定物件，則跳過
                        return false;
                    }

                    // 暫時解鎖源圖層以允許修改
                    if (sourceLayerRecord != null && sourceLayerRecord.IsLocked)
                    {
                        sourceLayerRecord.UpgradeOpen();
                        sourceLayerRecord.IsLocked = false;
                        wasLayerLocked = true;
                    }
                }

                try
                {
                    // 升級為寫入模式並變更圖層
                    entity.UpgradeOpen();
                    entity.Layer = targetLayer;
                    entity.DowngradeOpen();
                    return true;
                }
                finally
                {
                    // 恢復源圖層的鎖定狀態
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
                // 即使暫時解鎖也無法修改，可能是其他原因
                return false;
            }
            catch (System.Exception ex)
            {
                // 記錄詳細錯誤但不拋出異常
                System.Diagnostics.Debug.WriteLine($"轉換實體圖層時發生錯誤: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 檢查實體是否在鎖定圖層上，並返回圖層記錄
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
        /// 檢查實體是否在鎖定圖層上（簡化版本）
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
    /// 圖塊資訊類別
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
    /// 屬性資訊類別
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