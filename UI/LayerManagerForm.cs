using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AutoCAD_Layer_Manger.Services;

namespace AutoCAD_Layer_Manger.UI
{
    /// <summary>
    /// �Τ@���ϼh�޲z����� - �i�γ]�p�u��s��
    /// </summary>
    public partial class LayerManagerForm : Form
    {
        private readonly ObjectId[] _entityIds;
        private readonly ILayerService _layerService;
        private readonly IEntityConverter _entityConverter;
        
        public ConversionResult? Result { get; private set; }

        public LayerManagerForm()
        {
            _entityIds = Array.Empty<ObjectId>();
            _entityConverter = new EntityConverter();
            _layerService = new LayerService(_entityConverter);
            InitializeComponent();
        }

        public LayerManagerForm(ObjectId[] entityIds)
        {
            _entityIds = entityIds;
            _entityConverter = new EntityConverter();
            _layerService = new LayerService(_entityConverter);
            InitializeComponent();
            InitializeFormData();
        }

        private void InitializeFormData()
        {
            // �]�w����ƶq��T
            this.infoLabel.Text = $"�w��� {_entityIds.Length} �Ӫ���";
            
            // ���J�ϼh�ƾ�
            LoadLayers();
        }

        private void LoadLayers()
        {
            try
            {
                this.statusLabel.Text = "���b���J�ϼh...";
                this.layerComboBox.Items.Clear();
                
                // �ϥΦP�B��k���קK�u�{���D
                var layers = GetLayersSync();
                
                foreach (var layer in layers)
                {
                    string displayName = layer.Name;
                    if (layer.IsLocked) displayName += " (��w)";
                    if (layer.IsFrozen) displayName += " (�ᵲ)";
                    
                    this.layerComboBox.Items.Add(displayName);
                }
                
                if (this.layerComboBox.Items.Count > 0)
                {
                    this.layerComboBox.SelectedIndex = 0;
                }

                this.statusLabel.Text = $"���J�����A��� {this.layerComboBox.Items.Count} �ӹϼh";
            }
            catch (System.Exception ex)
            {
                this.statusLabel.Text = "���J�ϼh����";
                MessageBox.Show($"���J�ϼh����: {ex.Message}", "���~", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// �P�B����ϼh�C��
        /// </summary>
        private List<LayerInfo> GetLayersSync()
        {
            var layers = new List<LayerInfo>();
            
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
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
                System.Diagnostics.Debug.WriteLine($"����ϼh����: {ex.Message}");
            }

            return layers.OrderBy(l => l.Name).ToList();
        }

        private void previewButton_Click(object sender, EventArgs e)
        {
            if (this.layerComboBox.SelectedItem is string selectedItem)
            {
                string layerName = selectedItem.Split(' ')[0];
                string message = "�ഫ�w��:\n\n";
                message += $"����ƶq: {_entityIds.Length}\n";
                message += $"�ؼйϼh: {layerName}\n\n";
                message += "�ഫ�]�w:\n";
                message += $"? �۰ʸ���ؼйϼh: {(this.unlockTargetLayerCheckBox.Checked ? "�O" : "�_")}\n";
                message += $"? �۰ʳЫعϼh: {(this.createLayerCheckBox.Checked ? "�O" : "�_")}\n";
                message += $"? �j���ഫ��w����: {(this.handleLockedObjectsCheckBox.Checked ? "�O�A�]�A��w�ϼh�W���϶�" : "�_�A���L��w�ϼh�W������")}\n";
                message += $"? �϶����ѭ��ժk: {(this.blockExplodeMethodCheckBox.Checked ? "�O�A����w�϶��ϥΤ��ѭ���" : "�_�A�ϥζǲΤ�k")}\n";
                
                if (this.handleLockedObjectsCheckBox.Checked && this.blockExplodeMethodCheckBox.Checked)
                {
                    message += "\n?? �`�N�G���ѭ��ժk�|�Ȯɤ�����w�϶����¦�����A�ഫ�ϼh�᭫�s�զX���ۦP���϶��C";
                    message += "\n�o�ؤ�k��w���i�a�A���|���s�Ыع϶��w�q�C";
                }
                else if (this.handleLockedObjectsCheckBox.Checked)
                {
                    message += "\n?? �`�N�G�ҥαj���ഫ�|�Ȯɸ��귽�ϼh�A�ഫ������۰ʫ�_��w���A�C";
                }
                
                MessageBox.Show(message, "�ഫ�w��", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void convertButton_Click(object sender, EventArgs e)
        {
            if (this.layerComboBox.SelectedItem is string selectedItem)
            {
                try
                {
                    this.convertButton.Enabled = false;
                    this.cancelButton.Enabled = false;
                    this.statusLabel.Text = "���b�����ഫ...";
                    
                    string layerName = selectedItem.Split(' ')[0];
                    
                    // �Ы��ഫ�ﶵ
                    var options = new ConversionOptions
                    {
                        CreateTargetLayer = this.createLayerCheckBox.Checked,
                        UnlockTargetLayer = this.unlockTargetLayerCheckBox.Checked,
                        ForceConvertLockedObjects = this.handleLockedObjectsCheckBox.Checked,
                        UseBlockExplodeMethod = this.blockExplodeMethodCheckBox.Checked,
                        ProcessBlocks = true,
                        MaxDepth = 50
                    };
                    
                    // �ϥΦP�B��k�����ഫ
                    Result = ExecuteConversionSync(layerName, options);
                    
                    this.statusLabel.Text = "�ഫ�����I";
                    
                    string resultMessage = "�ഫ�����I\n\n";
                    resultMessage += $"���\�ഫ: {Result.ConvertedCount} �Ӫ���\n";
                    if (Result.SkippedCount > 0)
                    {
                        resultMessage += $"���L: {Result.SkippedCount} �Ӫ���\n";
                    }
                    resultMessage += $"���~: {Result.ErrorCount} ��\n";
                    
                    if (Result.Errors.Any())
                    {
                        resultMessage += $"\n���~�Ա�:\n{string.Join("\n", Result.Errors.Take(3))}";
                        if (Result.Errors.Count > 3)
                        {
                            resultMessage += $"\n... �٦� {Result.Errors.Count - 3} �ӿ��~";
                        }
                    }
                    
                    if (options.UseBlockExplodeMethod && this.handleLockedObjectsCheckBox.Checked)
                    {
                        resultMessage += "\n\n? �w�ϥι϶����ѭ��է޳N�B�z��w�϶�";
                    }
                    
                    MessageBox.Show(resultMessage, "�ഫ���G", 
                        MessageBoxButtons.OK, 
                        Result.ErrorCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                    
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (System.Exception ex)
                {
                    this.statusLabel.Text = "�ഫ����";
                    MessageBox.Show($"�ഫ����: {ex.Message}", "���~", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                    this.convertButton.Enabled = true;
                    this.cancelButton.Enabled = true;
                }
            }
            else
            {
                MessageBox.Show("�п�ܥؼйϼh", "����", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// �P�B�����ഫ
        /// </summary>
        private ConversionResult ExecuteConversionSync(string targetLayer, ConversionOptions options)
        {
            var result = new ConversionResult();
            
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc?.Database == null) return result;

                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    // �p�G�ݭn�A�Ыعϼh
                    if (options.CreateTargetLayer)
                    {
                        var layerTable = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                        if (layerTable != null && !layerTable.Has(targetLayer))
                        {
                            layerTable.UpgradeOpen();
                            var newLayer = new LayerTableRecord { Name = targetLayer };
                            layerTable.Add(newLayer);
                            tr.AddNewlyCreatedDBObject(newLayer, true);
                        }
                    }

                    // �p�G�ݭn�A����ؼйϼh
                    if (options.UnlockTargetLayer)
                    {
                        var layerTable = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                        if (layerTable != null && layerTable.Has(targetLayer))
                        {
                            var layerRecord = tr.GetObject(layerTable[targetLayer], OpenMode.ForRead) as LayerTableRecord;
                            if (layerRecord != null && layerRecord.IsLocked)
                            {
                                layerRecord.UpgradeOpen();
                                layerRecord.IsLocked = false;
                            }
                        }
                    }

                    // �ϥ�EntityConverter�i���ഫ
                    foreach (var objId in _entityIds)
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
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"�ഫ����: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ExecuteConversionSync error: {ex}");
            }

            return result;
        }
    }
}