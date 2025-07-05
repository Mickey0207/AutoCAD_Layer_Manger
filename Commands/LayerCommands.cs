using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AutoCAD_Layer_Manger.UI;
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
        private LayerManagerForm.ConversionResult ConvertEntities(ObjectId[] entityIds, string targetLayer)
        {
            var result = new LayerManagerForm.ConversionResult();
            
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
        private void ShowResult(Editor ed, LayerManagerForm.ConversionResult result)
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