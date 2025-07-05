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
                // 檢查實體基本狀態
                if (entity == null)
                {
                    result.ErrorCount++;
                    result.Errors.Add("實體為null");
                    return result;
                }

                if (entity.IsErased)
                {
                    result.SkippedCount++;
                    result.Errors.Add("實體已被刪除");
                    return result;
                }

                if (entity is BlockReference blockRef && options.ProcessBlocks)
                {
                    // 圖塊處理邏輯 - 使用多重策略
                    result = ProcessBlockReferenceWithStrategy(tr, blockRef, targetLayer, transform, options, depth);
                }
                else if (IsGeometricEntity(entity))
                {
                    result = ProcessGeometricEntity(tr, entity, targetLayer, options);
                }
                else
                {
                    // 不支援的實體類型，提供詳細分析
                    result.SkippedCount = 1;
                    string analysis = AnalyzeConversionFailure(tr, entity, options);
                    result.Errors.Add($"無法處理實體 {entity.GetType().Name}: {analysis}");
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
            {
                result.ErrorCount++;
                result.Errors.Add($"圖塊處理失敗: eOnLockedLayer - 將嘗試使用圖塊編輯器方法");
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"轉換實體時發生錯誤: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ConvertEntityToLayer exception: {ex}");
            }

            return result;
        }

        /// <summary>
        /// 使用智能多重策略處理圖塊參考
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
                
                // 獲取要嘗試的方法列表
                var methodsToTry = options.GetEnabledMethods();
                
                // 如果是鎖定圖層且不強制轉換，只嘗試高級方法
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
                            BlockProcessingMethod.ReferenceEdit => CreateNotImplementedResult("現地編輯法暫未完全實現"),
                            BlockProcessingMethod.BlockEditor => CreateNotImplementedResult("圖塊編輯器法暫未完全實現"),
                            _ => new ConversionResult { ErrorCount = 1, Errors = { "未知的處理方法" } }
                        };

                        attemptHistory.Add($"{GetMethodDisplayName(method)}: {methodResult.ConvertedCount}成功/{methodResult.ErrorCount}錯誤");

                        // 如果成功轉換了物件，返回結果
                        if (methodResult.ConvertedCount > 0)
                        {
                            methodResult.Errors.Insert(0, $"✅ 使用 {GetMethodDisplayName(method)} 成功處理");
                            return methodResult;
                        }

                        // 如果沒有錯誤但也沒有轉換（可能跳過），也視為成功
                        if (methodResult.ErrorCount == 0)
                        {
                            methodResult.Errors.Add($"使用 {GetMethodDisplayName(method)} 處理（無錯誤）");
                            return methodResult;
                        }

                        // 如果啟用自動重試，繼續嘗試下一個方法
                        if (!options.EnableAutoRetry)
                        {
                            // 不啟用自動重試，返回第一個方法的結果
                            methodResult.Errors.Add($"使用 {GetMethodDisplayName(method)} 處理，未啟用自動重試");
                            return methodResult;
                        }

                        // 累積錯誤信息
                        result.Errors.AddRange(methodResult.Errors);
                    }
                    catch (System.Exception ex)
                    {
                        attemptHistory.Add($"{GetMethodDisplayName(method)}: 異常 - {ex.Message}");
                        result.Errors.Add($"{GetMethodDisplayName(method)} 異常: {ex.Message}");
                        
                        // 如果不啟用自動重試，拋出異常
                        if (!options.EnableAutoRetry)
                        {
                            break;
                        }
                    }
                }

                // 所有方法都嘗試過了，返回失敗結果
                result.ErrorCount++;
                result.Errors.Add($"❌ 所有處理方法都失敗了");
                result.Errors.Add($"嘗試記錄: {string.Join(" → ", attemptHistory)}");
                
                return result;
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"圖塊策略處理異常: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// 獲取方法的顯示名稱
        /// </summary>
        private string GetMethodDisplayName(BlockProcessingMethod method)
        {
            return method switch
            {
                BlockProcessingMethod.Traditional => "傳統方法",
                BlockProcessingMethod.ExplodeRecombine => "分解重組法",
                BlockProcessingMethod.ReferenceEdit => "現地編輯法",
                BlockProcessingMethod.BlockEditor => "圖塊編輯器法",
                _ => "未知方法"
            };
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
                // 檢查圖塊是否可以分解
                if (!CanExplodeBlock(tr, blockRef))
                {
                    result.ErrorCount++;
                    result.Errors.Add($"圖塊 {blockRef.Name} 無法分解（可能是動態圖塊或受保護圖塊）");
                    return result;
                }

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
                var explodedEntities = ExplodeBlockToBasicElements(tr, blockRef, transform, depth);
                if (explodedEntities.Count == 0)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"無法分解圖塊 {blockRef.Name}");
                    return result;
                }

                // 步驟3: 先轉換基礎元素的圖層（在刪除原圖塊之前）
                var convertedEntities = new List<Entity>();
                foreach (var entity in explodedEntities)
                {
                    try
                    {
                        // 檢查實體是否在鎖定圖層上
                        if (IsEntityOnLockedLayer(tr, entity))
                        {
                            // 嘗試暫時解鎖並轉換
                            if (TryConvertEntityLayerWithUnlock(tr, entity, targetLayer, options))
                            {
                                convertedEntities.Add(entity);
                                result.ConvertedCount++;
                            }
                            else
                            {
                                // 如果仍然無法轉換，記錄錯誤但繼續處理其他元素
                                result.ErrorCount++;
                                result.Errors.Add($"基礎元素轉換失敗: {entity.GetType().Name}");
                                convertedEntities.Add(entity); // 仍然加入，保持結構完整
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
                        result.Errors.Add($"轉換基礎元素失敗: {ex.Message}");
                        convertedEntities.Add(entity); // 仍然加入，保持結構完整
                    }
                }

                // 步驟4: 嘗試重新組合成圖塊
                if (convertedEntities.Count > 0)
                {
                    try
                    {
                        var newBlockRef = RecreateBlockFromEntities(tr, convertedEntities, originalBlockInfo);
                        if (newBlockRef != null)
                        {
                            // 步驟5: 成功後才刪除原始圖塊
                            blockRef.UpgradeOpen();
                            blockRef.Erase();
                            result.ConvertedCount++; // 圖塊本身也算轉換成功
                        }
                        else
                        {
                            result.ErrorCount++;
                            result.Errors.Add($"重新創建圖塊 {originalBlockInfo.Name} 失敗");
                            
                            // 如果重組失敗，將分解的元素加入到模型空間
                            AddEntitiesToModelSpace(tr, convertedEntities);
                            result.ConvertedCount += convertedEntities.Count;
                            
                            // 仍然刪除原圖塊
                            blockRef.UpgradeOpen();
                            blockRef.Erase();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"圖塊重組過程失敗: {ex.Message}");
                        
                        // 即使重組失敗，也嘗試保存分解的元素
                        try
                        {
                            AddEntitiesToModelSpace(tr, convertedEntities);
                            result.ConvertedCount += convertedEntities.Count;
                            
                            blockRef.UpgradeOpen();
                            blockRef.Erase();
                        }
                        catch (System.Exception ex2)
                        {
                            result.Errors.Add($"保存分解元素失敗: {ex2.Message}");
                        }
                    }
                }
                else
                {
                    result.ErrorCount++;
                    result.Errors.Add($"沒有可轉換的基礎元素");
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
        /// 檢查圖塊是否可以分解
        /// </summary>
        private bool CanExplodeBlock(Transaction tr, BlockReference blockRef)
        {
            try
            {
                // 檢查圖塊定義是否存在
                var btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                if (btr == null) return false;

                // 檢查是否是動態圖塊
                if (blockRef.IsDynamicBlock) 
                {
                    return true; // 動態圖塊也可以嘗試分解
                }

                // 檢查是否有不可分解的限制
                if (btr.Explodable == false) return false;

                // 嘗試創建一個測試的分解集合
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
        /// 分解圖塊到最基礎的元素（改進版）
        /// </summary>
        private List<Entity> ExplodeBlockToBasicElements(Transaction tr, BlockReference blockRef, Matrix3d transform, int depth)
        {
            var basicElements = new List<Entity>();
            
            try
            {
                // 防止無限遞迴
                if (depth > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"分解深度過深，停止遞迴");
                    return basicElements;
                }

                // 創建分解後的實體集合
                var explodedEntities = new DBObjectCollection();
                blockRef.Explode(explodedEntities);

                foreach (Entity explodedEntity in explodedEntities)
                {
                    if (explodedEntity is BlockReference nestedBlockRef)
                    {
                        // 遞迴分解嵌套圖塊
                        var nestedElements = ExplodeBlockToBasicElements(tr, nestedBlockRef, transform, depth + 1);
                        basicElements.AddRange(nestedElements);
                        
                        // 釋放嵌套圖塊參考
                        nestedBlockRef.Dispose();
                    }
                    else
                    {
                        // 基礎幾何元素
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
                            System.Diagnostics.Debug.WriteLine($"轉換元素失敗: {ex.Message}");
                            // 即使轉換失敗也添加元素
                            basicElements.Add(explodedEntity);
                        }
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
        /// 將實體添加到模型空間（作為備用方案）
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
                        System.Diagnostics.Debug.WriteLine($"添加實體到模型空間失敗: {ex.Message}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddEntitiesToModelSpace失敗: {ex.Message}");
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

        /// <summary>
        /// 增強的圖層轉換方法，支援強制處理鎖定圖層上的物件
        /// </summary>
        private bool TryConvertEntityLayerWithUnlock(Transaction tr, Entity entity, string targetLayer, ConversionOptions options)
        {
            try
            {
                bool wasLayerLocked = false;
                LayerTableRecord? sourceLayerRecord = null;

                // 檢查實體是否可以修改
                if (entity.IsReadEnabled && !entity.IsWriteEnabled)
                {
                    try
                    {
                        entity.UpgradeOpen();
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
                    {
                        // 實體在鎖定圖層上，需要特殊處理
                    }
                }

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
                        try
                        {
                            sourceLayerRecord.UpgradeOpen();
                            sourceLayerRecord.IsLocked = false;
                            wasLayerLocked = true;
                        }
                        catch (System.Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"無法解鎖圖層 {sourceLayerRecord.Name}: {ex.Message}");
                            return false;
                        }
                    }
                }

                try
                {
                    // 確保實體可寫
                    if (!entity.IsWriteEnabled)
                    {
                        entity.UpgradeOpen();
                    }
                    
                    // 變更圖層
                    string originalLayer = entity.Layer;
                    entity.Layer = targetLayer;
                    
                    // 驗證轉換是否成功
                    if (entity.Layer != targetLayer)
                    {
                        System.Diagnostics.Debug.WriteLine($"圖層轉換驗證失敗: 期望 {targetLayer}, 實際 {entity.Layer}");
                        return false;
                    }
                    
                    entity.DowngradeOpen();
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == ErrorStatus.OnLockedLayer)
                {
                    System.Diagnostics.Debug.WriteLine($"實體仍在鎖定圖層上: {ex.Message}");
                    return false;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == ErrorStatus.NotOpenForWrite)
                {
                    System.Diagnostics.Debug.WriteLine($"實體未開啟寫入權限: {ex.Message}");
                    return false;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AutoCAD錯誤 {ex.ErrorStatus}: {ex.Message}");
                    return false;
                }
                finally
                {
                    // 恢復源圖層的鎖定狀態
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
                            System.Diagnostics.Debug.WriteLine($"恢復圖層鎖定狀態失敗: {ex.Message}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                // 記錄詳細錯誤但不拋出異常
                System.Diagnostics.Debug.WriteLine($"轉換實體圖層時發生錯誤: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 處理幾何實體的轉換（改進版）
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
                // 檢查實體類型是否支援
                if (!IsGeometricEntity(entity))
                {
                    result.SkippedCount = 1;
                    result.Errors.Add($"不支援的實體類型: {entity.GetType().Name}");
                    return result;
                }

                // 檢查實體是否已被刪除
                if (entity.IsErased)
                {
                    result.SkippedCount = 1;
                    result.Errors.Add($"實體已被刪除: {entity.GetType().Name}");
                    return result;
                }

                // 嘗試轉換
                if (TryConvertEntityLayerWithUnlock(tr, entity, targetLayer, options))
                {
                    result.ConvertedCount = 1;
                }
                else
                {
                    result.SkippedCount = 1;
                    
                    // 提供更詳細的錯誤信息
                    string errorDetail = AnalyzeConversionFailure(tr, entity, options);
                    
                    result.Errors.Add($"無法轉換實體 {entity.GetType().Name}: {errorDetail}");
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
        /// 檢查實體是否在鎖定圖層上，並返回圖層記錄（增強版）
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
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"檢查圖層鎖定狀態失敗: {ex.Message}");
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
        /// 詳細的錯誤分析方法
        /// </summary>
        private string AnalyzeConversionFailure(Transaction tr, Entity entity, ConversionOptions options)
        {
            try
            {
                var reasons = new List<string>();

                // 檢查實體狀態
                if (entity.IsErased)
                {
                    reasons.Add("實體已被刪除");
                }

                if (entity.IsReadEnabled && !entity.IsWriteEnabled)
                {
                    reasons.Add("實體為唯讀狀態");
                }

                // 檢查圖層狀態
                if (IsEntityOnLockedLayer(tr, entity, out var layerRecord))
                {
                    if (!options.ForceConvertLockedObjects)
                    {
                        reasons.Add("位於鎖定圖層且未啟用強制轉換");
                    }
                    else
                    {
                        reasons.Add("位於鎖定圖層，強制轉換失敗");
                    }
                }

                // 檢查實體類型
                if (!IsGeometricEntity(entity) && !(entity is BlockReference))
                {
                    reasons.Add($"不支援的實體類型: {entity.GetType().Name}");
                }

                // 特殊實體檢查
                if (entity is BlockReference blockRef)
                {
                    var btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    if (btr?.Explodable == false)
                    {
                        reasons.Add("圖塊不可分解");
                    }
                }

                return reasons.Count > 0 ? string.Join(", ", reasons) : "未知原因";
            }
            catch (System.Exception ex)
            {
                return $"分析失敗: {ex.Message}";
            }
        }

        public bool IsGeometricEntity(Entity entity)
        {
            if (entity == null) return false;

            // 擴展支援的實體類型
            var extendedTypes = new HashSet<Type>(GeometricEntityTypes)
            {
                typeof(Viewport), typeof(Table), typeof(MText),
                typeof(Shape), typeof(Tolerance)
            };

            return extendedTypes.Contains(entity.GetType());
        }

        /// <summary>
        /// 創建未實現方法的結果
        /// </summary>
        private ConversionResult CreateNotImplementedResult(string methodName)
        {
            var result = new ConversionResult();
            result.ErrorCount++;
            result.Errors.Add($"{methodName}，建議使用分解重組法或傳統方法");
            return result;
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