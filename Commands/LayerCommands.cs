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
    /// �ϼh�޲z�R�O���O
    /// </summary>
    public class LayerCommands
    {
        /// <summary>
        /// �ϼh�޲z���O - ��ܪ���᪽���i�J�Τ@UI
        /// </summary>
        [CommandMethod("LAYERMANAGER", CommandFlags.Modal)]
        public void LayerManagerCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== AutoCAD �ϼh�޲z�� ===");
                
                // �����������
                var entityIds = SelectEntities(ed);
                if (entityIds.Length == 0) 
                {
                    ed.WriteMessage("\n��������󪫥�A���O�����C");
                    return;
                }

                // ��ܲΤ@UI��ܮ�
                try
                {
                    using (var dialog = new LayerManagerForm(entityIds))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            ed.WriteMessage("\n�ϼh�ഫ�����I");
                            if (dialog.Result != null)
                            {
                                ShowResult(ed, dialog.Result);
                            }
                        }
                        else
                        {
                            ed.WriteMessage("\n�ާ@�w�����C");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n��ܮؿ��~: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Dialog error: {ex}");
                }
            }
            catch (System.Exception ex)
            {
                ed?.WriteMessage($"\n���~: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerManagerCommand error: {ex}");
            }
        }

        /// <summary>
        /// ���ե\����O - �W�ߪ����ի��O
        /// </summary>
        [CommandMethod("LAYERTEST", CommandFlags.Modal)]
        public void LayerTestCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== �ϼh�޲z���\����� ===");
                
                // ���չϼhŪ��
                var layers = GetLayers();
                ed.WriteMessage($"\n? �ϼhŪ��: ��� {layers.Count} �ӹϼh");
                
                if (layers.Count > 0)
                {
                    ed.WriteMessage($"\n�e5�ӹϼh: {string.Join(", ", layers.Take(5))}");
                    
                    // ��ܹϼh���A
                    var layerInfo = GetLayerInfo();
                    var lockedLayers = layerInfo.Where(l => l.IsLocked).Count();
                    var frozenLayers = layerInfo.Where(l => l.IsFrozen).Count();
                    
                    ed.WriteMessage($"\n? ��w�ϼh: {lockedLayers} ��");
                    ed.WriteMessage($"\n? �ᵲ�ϼh: {frozenLayers} ��");
                }
                
                ed.WriteMessage("\n? �Ҧ��\����է����I");
                ed.WriteMessage("\n�ϥ� LAYERMANAGER ���O�i��ϼh�ഫ");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n���ե���: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerTestCommand error: {ex}");
            }
        }

        /// <summary>
        /// �ֳt�ഫ���O - �ഫ���e�ϼh�A�䴩��w�ϼh
        /// </summary>
        [CommandMethod("LAYERQUICK", CommandFlags.Modal)]
        public void LayerQuickCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== �ֳt�ϼh�ഫ ===");
                
                // �����e�ϼh
                string currentLayer = GetCurrentLayer();
                ed.WriteMessage($"\n�ؼйϼh: {currentLayer}");
                
                // �������
                var entityIds = SelectEntities(ed);
                if (entityIds.Length == 0) return;

                // �߰ݬO�_�j���ഫ��w����
                var forceOpts = new PromptKeywordOptions($"\n�O�_�j���ഫ��w�ϼh�W������(�]�A�϶�)? ");
                forceOpts.Keywords.Add("Yes");
                forceOpts.Keywords.Add("No");
                forceOpts.Keywords.Default = "Yes";

                var forceResult = ed.GetKeywords(forceOpts);
                bool forceConvert = forceResult.Status == PromptStatus.OK && forceResult.StringResult == "Yes";

                // �T�{�ഫ
                var confirmOpts = new PromptKeywordOptions($"\n�N {entityIds.Length} �Ӫ����ഫ��ϼh '{currentLayer}'? ");
                confirmOpts.Keywords.Add("Yes");
                confirmOpts.Keywords.Add("No");
                confirmOpts.Keywords.Default = "Yes";

                var confirmResult = ed.GetKeywords(confirmOpts);
                if (confirmResult.Status == PromptStatus.OK && confirmResult.StringResult == "Yes")
                {
                    var result = ConvertEntitiesWithOptions(entityIds, currentLayer, forceConvert);
                    ShowResult(ed, result);
                }
                else
                {
                    ed.WriteMessage("\n�ާ@�w�����C");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n�ֳt�ഫ���~: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerQuickCommand error: {ex}");
            }
        }

        /// <summary>
        /// ���չ϶����ѭ��ե\����O
        /// </summary>
        [CommandMethod("LAYERBLOCKTEST", CommandFlags.Modal)]
        public void LayerBlockTestCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== �϶����ѭ��ե\����� ===");
                
                // ����϶�
                var opts = new PromptSelectionOptions
                {
                    MessageForAdding = "\n����n���ժ��϶�: "
                };
                
                var filter = new SelectionFilter(new[]
                {
                    new TypedValue((int)DxfCode.Start, "INSERT")
                });
                
                var selResult = ed.GetSelection(opts, filter);
                if (selResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n���������϶��A���յ����C");
                    return;
                }

                var entityIds = selResult.Value.GetObjectIds();
                ed.WriteMessage($"\n�w��� {entityIds.Length} �ӹ϶�");

                // �ˬd�϶��O�_�b��w�ϼh�W
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
                                ed.WriteMessage($"\n�����w�ϼh�W���϶�: {blockRef.Name} (�ϼh: {blockRef.Layer})");
                            }
                        }
                    }
                    tr.Commit();
                    
                    if (lockedBlockCount == 0)
                    {
                        ed.WriteMessage("\n�ҿ�϶������b��w�ϼh�W�A�L�k���դ��ѭ��ե\��C");
                        ed.WriteMessage("\n�Х��N�@�ǹ϶�������w�ϼh�W�A���աC");
                        return;
                    }
                    
                    ed.WriteMessage($"\n�@��� {lockedBlockCount} �Ӧb��w�ϼh�W���϶�");
                }

                // �߰ݥؼйϼh
                var layerOpts = new PromptStringOptions("\n��J�ؼйϼh�W��: ");
                layerOpts.AllowSpaces = false;
                layerOpts.DefaultValue = "0";
                
                var layerResult = ed.GetString(layerOpts);
                if (layerResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n�ާ@�w�����C");
                    return;
                }

                string targetLayer = layerResult.StringResult;
                ed.WriteMessage($"\n�ؼйϼh: {targetLayer}");

                // �����ഫ�]�ϥΤ��ѭ��ժk�^
                ed.WriteMessage("\n�}�l����϶����ѭ����ഫ...");
                
                var entityConverter = new EntityConverter();
                var layerService = new LayerService(entityConverter);
                
                var options = new ConversionOptions
                {
                    CreateTargetLayer = true,
                    UnlockTargetLayer = true,
                    ForceConvertLockedObjects = true,
                    UseBlockExplodeMethod = true,  // ����G�ҥΤ��ѭ��ժk
                    ProcessBlocks = true,
                    MaxDepth = 10
                };

                var conversionTask = layerService.ConvertEntitiesToLayerAsync(entityIds, targetLayer, options);
                var result = conversionTask.Result; // �P�B���ݵ��G

                // ��ܸԲӵ��G
                ed.WriteMessage("\n=== �ഫ���G ===");
                ed.WriteMessage($"\n���\�ഫ: {result.ConvertedCount} �Ӫ���");
                ed.WriteMessage($"\n���L����: {result.SkippedCount} ��");
                ed.WriteMessage($"\n���~����: {result.ErrorCount} ��");
                
                if (result.Errors.Any())
                {
                    ed.WriteMessage($"\n���~�Ա�:");
                    foreach (var error in result.Errors.Take(5))
                    {
                        ed.WriteMessage($"\n  - {error}");
                    }
                }
                
                if (result.ConvertedCount > 0)
                {
                    ed.WriteMessage("\n? �϶����ѭ��ե\����զ��\�I");
                }
                else
                {
                    ed.WriteMessage("\n? �϶����ѭ��ե\��i��s�b���D�C");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n���չL�{�o�Ϳ��~: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerBlockTestCommand error: {ex}");
            }
        }

        /// <summary>
        /// ���չϼh���J�\����O
        /// </summary>
        [CommandMethod("LAYERLOADTEST", CommandFlags.Modal)]
        public void LayerLoadTestCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== �ϼh���J�\����� ===");
                
                // ���ժ���Ū���ϼh
                var layers = GetLayers();
                ed.WriteMessage($"\n? ����Ū����� {layers.Count} �ӹϼh");
                
                if (layers.Count > 0)
                {
                    ed.WriteMessage($"\n�e10�ӹϼh:");
                    foreach (var layer in layers.Take(10))
                    {
                        ed.WriteMessage($"\n  - {layer}");
                    }
                }

                // ����LayerService
                try
                {
                    var entityConverter = new EntityConverter();
                    var layerService = new LayerService(entityConverter);
                    var layerInfoTask = layerService.GetLayersAsync();
                    var layerInfos = layerInfoTask.Result;
                    
                    ed.WriteMessage($"\n? LayerService��� {layerInfos.Count} �ӹϼh");
                    
                    if (layerInfos.Count > 0)
                    {
                        ed.WriteMessage($"\n�e5�ӹϼh�Ա�:");
                        foreach (var layerInfo in layerInfos.Take(5))
                        {
                            string status = "";
                            if (layerInfo.IsLocked) status += "��w ";
                            if (layerInfo.IsFrozen) status += "�ᵲ ";
                            if (layerInfo.IsOff) status += "���� ";
                            
                            ed.WriteMessage($"\n  - {layerInfo.Name} ({(string.IsNullOrEmpty(status) ? "���`" : status.Trim())})");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n? LayerService���ե���: {ex.Message}");
                }

                ed.WriteMessage("\n? �ϼh���J���է����I");
                ed.WriteMessage("\n�p�G�ݨ�ϼh�C��A��ܥ\�ॿ�`�C");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n���ե���: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerLoadTestCommand error: {ex}");
            }
        }

        #region �p�����U��k

        /// <summary>
        /// �ഫ�����ϼh�]�䴩�j���ഫ�ﶵ�^
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
                    // �T�O�ؼйϼh�s�b
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
                                    // �j���ഫ�Ҧ�
                                    converted = ConvertEntityWithUnlock(tr, entity, targetLayer);
                                }
                                else
                                {
                                    // �ǲμҦ��G���L��w�ϼh
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
                                    result.Errors.Add($"���L��w�ϼh�W������ {objId}");
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            result.ErrorCount++;
                            result.Errors.Add($"���� {objId}: {ex.Message}");
                        }
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"�ഫ����: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ConvertEntitiesWithOptions error: {ex}");
            }

            return result;
        }

        /// <summary>
        /// �j���ഫ����A�]�A��w�ϼh�W������
        /// </summary>
        private bool ConvertEntityWithUnlock(Transaction tr, Entity entity, string targetLayer)
        {
            try
            {
                bool wasLayerLocked = false;
                LayerTableRecord? sourceLayerRecord = null;

                // �ˬd��e�ϼh�O�_�Q��w
                if (IsEntityOnLockedLayer(tr, entity, out sourceLayerRecord))
                {
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
                    }
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex) when (ex.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.OnLockedLayer)
            {
                // �Y�ϼȮɸ���]�L�k�ק�
                System.Diagnostics.Debug.WriteLine($"�L�k�ഫ��w�ϼh�W������: {ex.Message}");
                return false;
            }
            catch (System.Exception ex)
            {
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
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc?.Database == null) return false;

                var layerTable = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                
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

        /// <summary>
        /// �������
        /// </summary>
        private ObjectId[] SelectEntities(Editor ed)
        {
            var selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\n����n�ഫ�ϼh������: ",
                AllowDuplicates = false
            };

            var selResult = ed.GetSelection(selOpts);
            if (selResult.Status == PromptStatus.OK)
            {
                var entityIds = selResult.Value.GetObjectIds();
                ed.WriteMessage($"\n�w��� {entityIds.Length} �Ӫ���");
                return entityIds;
            }

            return Array.Empty<ObjectId>();
        }

        /// <summary>
        /// �����e�ϼh
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
        /// ����ϼh�C��
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
        /// ����ϼh�ԲӸ�T
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
        /// �T�O�ϼh�s�b
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
        /// ����ഫ���G
        /// </summary>
        private void ShowResult(Editor ed, ConversionResult result)
        {
            ed.WriteMessage($"\n=== �ഫ���G ===");
            ed.WriteMessage($"\n���\�ഫ: {result.ConvertedCount} �Ӫ���");
            ed.WriteMessage($"\n���L����: {result.SkippedCount} ��");
            ed.WriteMessage($"\n���~����: {result.ErrorCount} ��");
            
            if (result.Errors.Any())
            {
                ed.WriteMessage($"\n���~�Ա�:");
                foreach (var error in result.Errors.Take(3))
                {
                    ed.WriteMessage($"\n  - {error}");
                }
                if (result.Errors.Count > 3)
                {
                    ed.WriteMessage($"\n  ... �٦� {result.Errors.Count - 3} �ӿ��~");
                }
            }
            
            ed.WriteMessage("\n�ഫ�����I");
        }

        #endregion

        #region �ƾ����O

        /// <summary>
        /// �ϼh��T
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