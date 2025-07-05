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
    /// ²�ƪ��ϼh�޲z���O - �ϥβΤ@UI
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
        /// �ֳt�ഫ���O - �ഫ���e�ϼh
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

                // �T�{�ഫ
                var confirmOpts = new PromptKeywordOptions($"\n�N {entityIds.Length} �Ӫ����ഫ��ϼh '{currentLayer}'? ");
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

        /// <summary>
        /// �E�_�����ഫ���D�����O
        /// </summary>
        [CommandMethod("LAYERDIAGNOSE", CommandFlags.Modal)]
        public void LayerDiagnoseCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== �ϼh�ഫ�E�_�u�� ===");
                
                // ����n�E�_������
                var selOpts = new PromptSelectionOptions
                {
                    MessageForAdding = "\n����n�E�_������: ",
                    AllowDuplicates = false
                };

                var selResult = ed.GetSelection(selOpts);
                if (selResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n��������󪫥�C");
                    return;
                }

                var entityIds = selResult.Value.GetObjectIds();
                ed.WriteMessage($"\n���b�E�_ {entityIds.Length} �Ӫ���...");

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
                                ed.WriteMessage($"\n\n��������: {entity.GetType().Name}");
                                ed.WriteMessage($"\n��e�ϼh: {entity.Layer}");

                                // �ˬd�O�_�w�R��
                                if (entity.IsErased)
                                {
                                    ed.WriteMessage("\n���A: ? �w�R��");
                                    erasedCount++;
                                    continue;
                                }

                                // �ˬd�ϼh���A
                                var layerTable = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                                if (layerTable?.Has(entity.Layer) == true)
                                {
                                    var layerRecord = tr.GetObject(layerTable[entity.Layer], OpenMode.ForRead) as LayerTableRecord;
                                    if (layerRecord != null)
                                    {
                                        ed.WriteMessage($"\n�ϼh���A: {(layerRecord.IsLocked ? "?? ��w" : "?? ����w")} | {(layerRecord.IsFrozen ? "?? �ᵲ" : "??? ���ᵲ")} | {(layerRecord.IsOff ? "?? ����" : "?? �}��")}");
                                        
                                        if (layerRecord.IsLocked)
                                        {
                                            lockedCount++;
                                        }
                                    }
                                }

                                // �ˬd��������
                                if (entity is BlockReference blockRef)
                                {
                                    ed.WriteMessage($"\n�϶���T: {blockRef.Name}");
                                    ed.WriteMessage($"\n�ʺA�϶�: {(blockRef.IsDynamicBlock ? "�O" : "�_")}");
                                    
                                    // �ˬd�϶��w�q
                                    var btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                                    if (btr != null)
                                    {
                                        ed.WriteMessage($"\n�i����: {(btr.Explodable ? "�O" : "�_")}");
                                    }
                                    blockCount++;
                                }
                                else
                                {
                                    var entityConverter = new EntityConverter();
                                    if (entityConverter.IsGeometricEntity(entity))
                                    {
                                        ed.WriteMessage("\n����: ? �䴩���X�����");
                                        normalCount++;
                                    }
                                    else
                                    {
                                        ed.WriteMessage("\n����: ? ���䴩����������");
                                        unsupportedCount++;
                                    }
                                }

                                // �ˬd�g�J�v��
                                try
                                {
                                    entity.UpgradeOpen();
                                    ed.WriteMessage("\n�v��: ? �i�g�J");
                                    entity.DowngradeOpen();
                                }
                                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                                {
                                    ed.WriteMessage($"\n�v��: ? �L�k�g�J ({ex.ErrorStatus})");
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage($"\n�E�_���� {objId} �ɵo�Ϳ��~: {ex.Message}");
                        }
                    }

                    tr.Commit();

                    // ��ܲέp�H��
                    ed.WriteMessage("\n\n=== �E�_���G�έp ===");
                    ed.WriteMessage($"\n?? ��w�ϼh����: {lockedCount} ��");
                    ed.WriteMessage($"\n?? �϶�����: {blockCount} ��");
                    ed.WriteMessage($"\n?? ���q�X�󪫥�: {normalCount} ��");
                    ed.WriteMessage($"\n? ���䴩����: {unsupportedCount} ��");
                    ed.WriteMessage($"\n??? �w�R������: {erasedCount} ��");

                    // ���ѫ�ĳ
                    ed.WriteMessage("\n\n=== ��ĳ ===");
                    if (lockedCount > 0)
                    {
                        ed.WriteMessage("\n? �ҥΡu�j���ഫ��w����v�ﶵ");
                        ed.WriteMessage("\n? ��϶��ҥΡu�϶����ѭ��ժk�v");
                    }
                    if (unsupportedCount > 0)
                    {
                        ed.WriteMessage("\n? �Y�ǯS����������i��ݭn��ʳB�z");
                    }
                    if (blockCount > 0)
                    {
                        ed.WriteMessage("\n? �϶���ĳ�ϥΡu�϶����ѭ��ժk�v�H��o�̨ε��G");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n�E�_�L�{�o�Ϳ��~: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerDiagnoseCommand error: {ex}");
            }
        }

        /// <summary>
        /// �϶��s�边��k���ի��O
        /// </summary>
        [CommandMethod("LAYEREDITBLOCK", CommandFlags.Modal)]
        public void LayerEditBlockCommand()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            var ed = doc?.Editor;
            if (ed == null) return;

            try
            {
                ed.WriteMessage("\n=== �϶��s�边�ഫ��k���� ===");
                ed.WriteMessage("\n����k�ϥ�AutoCAD���϶��s�边���ഫ�ϼh");
                ed.WriteMessage("\n�A�Ω�L�k�q�L��L��k�ഫ���x�T�϶�");
                
                // ����϶�
                var opts = new PromptSelectionOptions
                {
                    MessageForAdding = "\n����n�ϥνs�边��k�ഫ���϶�: "
                };
                
                var filter = new SelectionFilter(new[]
                {
                    new TypedValue((int)DxfCode.Start, "INSERT")
                });
                
                var selResult = ed.GetSelection(opts, filter);
                if (selResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n���������϶��C");
                    return;
                }

                var entityIds = selResult.Value.GetObjectIds();
                ed.WriteMessage($"\n�w��� {entityIds.Length} �ӹ϶�");

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

                // �߰ݨϥέ��ؽs�边��k
                var methodOpts = new PromptKeywordOptions("\n��ܽs�边��k [Reference�s��(R)/�϶��s�边(B)]: ");
                methodOpts.Keywords.Add("Reference");
                methodOpts.Keywords.Add("Block");
                methodOpts.Keywords.Default = "Reference";

                var methodResult = ed.GetKeywords(methodOpts);
                bool useReferenceEdit = methodResult.Status == PromptStatus.OK && 
                    (methodResult.StringResult == "Reference" || methodResult.StringResult == "R");

                // �����ഫ
                ed.WriteMessage($"\n�}�l�ϥ�{(useReferenceEdit ? "Reference�s��" : "�϶��s�边")}��k�ഫ...");
                
                var entityConverter = new EntityConverter();
                var layerService = new LayerService(entityConverter);
                
                var options = new ConversionOptions
                {
                    CreateTargetLayer = true,
                    UnlockTargetLayer = true,
                    ForceConvertLockedObjects = true,
                    UseBlockExplodeMethod = false,  // ���ϥΤ��ѭ���
                    UseReferenceEditMethod = useReferenceEdit,
                    UseBlockEditorMethod = !useReferenceEdit,
                    ProcessBlocks = true,
                    PreferredBlockMethod = useReferenceEdit ? 
                        BlockProcessingMethod.ReferenceEdit : 
                        BlockProcessingMethod.BlockEditor
                };

                var conversionTask = layerService.ConvertEntitiesToLayerAsync(entityIds, targetLayer, options);
                var result = conversionTask.Result;

                // ��ܵ��G
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
                    if (result.Errors.Count > 5)
                    {
                        ed.WriteMessage($"\n  ... �٦� {result.Errors.Count - 5} �ӿ��~");
                    }
                }
                
                if (result.ConvertedCount > 0)
                {
                    ed.WriteMessage($"\n? {(useReferenceEdit ? "Reference�s��" : "�϶��s�边")}��k���զ��\�I");
                }
                else
                {
                    ed.WriteMessage($"\n? {(useReferenceEdit ? "Reference�s��" : "�϶��s�边")}��k�i��ݭn�վ�C");
                }
                
                ed.WriteMessage("\n\n?? ���ܡG");
                ed.WriteMessage("\n? Reference�s��A�Ω�j�h�ƹ϶�");
                ed.WriteMessage("\n? �϶��s�边�A�Ω�������O�M�϶�");
                ed.WriteMessage("\n? �o�Ǥ�k�|�۰ʳB�z��w�ϼh���D");
                
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n���չL�{�o�Ϳ��~: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LayerEditBlockCommand error: {ex}");
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
        /// �ഫ�����ϼh
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
                    // �T�O�ؼйϼh�s�b
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
                System.Diagnostics.Debug.WriteLine($"ConvertEntities error: {ex}");
            }

            return result;
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

        /// <summary>
        /// �ˬd����O�_�b��w�ϼh�W
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
        /// �ϥθ����k�ഫ����
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
                        // �Ȯɸ���
                        layerRecord.UpgradeOpen();
                        layerRecord.IsLocked = false;
                        
                        // �ഫ�ϼh
                        entity.UpgradeOpen();
                        entity.Layer = targetLayer;
                        
                        // ��_��w
                        layerRecord.IsLocked = true;
                        
                        return true;
                    }
                    else
                    {
                        // �ϼh����w�A�����ഫ
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

        #endregion
    }
}