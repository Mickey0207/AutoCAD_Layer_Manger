using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;

namespace AutoCAD_Layer_Manger.UI
{
    /// <summary>
    /// �Τ@���ϼh�޲z����� - �i�γ]�p�u��s��
    /// </summary>
    public partial class LayerManagerForm : Form
    {
        private readonly ObjectId[] _entityIds;
        public ConversionResult? Result { get; private set; }

        public LayerManagerForm()
        {
            _entityIds = Array.Empty<ObjectId>();
            InitializeComponent();
        }

        public LayerManagerForm(ObjectId[] entityIds)
        {
            _entityIds = entityIds;
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
                
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc?.Database == null) return;

                this.layerComboBox.Items.Clear();
                
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var layerTable = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    if (layerTable == null) return;

                    var layers = new List<(string Name, bool IsLocked, bool IsFrozen)>();
                    foreach (ObjectId layerId in layerTable)
                    {
                        if (tr.GetObject(layerId, OpenMode.ForRead) is LayerTableRecord ltr)
                        {
                            layers.Add((ltr.Name, ltr.IsLocked, ltr.IsFrozen));
                        }
                    }
                    tr.Commit();
                    
                    foreach (var layer in layers.OrderBy(l => l.Name))
                    {
                        string displayName = layer.Name;
                        if (layer.IsLocked) displayName += " (��w)";
                        if (layer.IsFrozen) displayName += " (�ᵲ)";
                        
                        this.layerComboBox.Items.Add(displayName);
                    }
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

        private void previewButton_Click(object sender, EventArgs e)
        {
            if (this.layerComboBox.SelectedItem is string selectedItem)
            {
                string layerName = selectedItem.Split(' ')[0];
                string message = "�w���ഫ�ާ@:\n\n";
                message += $"����ƶq: {_entityIds.Length}\n";
                message += $"�ؼйϼh: {layerName}\n\n";
                message += "�ഫ�]�w:\n";
                message += $"? �۰ʸ���ؼйϼh: {(this.unlockTargetLayerCheckBox.Checked ? "�O" : "�_")}\n";
                message += $"? �۰ʳЫعϼh: {(this.createLayerCheckBox.Checked ? "�O" : "�_")}\n";
                message += $"? �B�z��w����: {(this.handleLockedObjectsCheckBox.Checked ? "�O" : "�_")}\n";
                
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
                    
                    Result = ExecuteConversion(layerName);
                    
                    this.statusLabel.Text = "�ഫ�����I";
                    
                    string resultMessage = "�ഫ�����I\n\n";
                    resultMessage += $"���\�ഫ: {Result.ConvertedCount} �Ӫ���\n";
                    resultMessage += $"���~: {Result.ErrorCount} ��\n";
                    
                    if (Result.Errors.Any())
                    {
                        resultMessage += $"\n���~�Ա�:\n{string.Join("\n", Result.Errors.Take(3))}";
                        if (Result.Errors.Count > 3)
                        {
                            resultMessage += $"\n... �٦� {Result.Errors.Count - 3} �ӿ��~";
                        }
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

        private ConversionResult ExecuteConversion(string targetLayer)
        {
            var result = new ConversionResult();
            
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc?.Database == null) return result;

                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    // �p�G�ݭn�A�Ыعϼh
                    if (this.createLayerCheckBox.Checked)
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
                    if (this.unlockTargetLayerCheckBox.Checked)
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

                    // �ഫ����
                    foreach (var objId in _entityIds)
                    {
                        try
                        {
                            if (tr.GetObject(objId, OpenMode.ForRead) is Entity entity)
                            {
                                bool canConvert = true;
                                if (!this.handleLockedObjectsCheckBox.Checked && entity.Layer != null)
                                {
                                    var layerTable = tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                                    if (layerTable != null && layerTable.Has(entity.Layer))
                                    {
                                        var sourceLayerRecord = tr.GetObject(layerTable[entity.Layer], OpenMode.ForRead) as LayerTableRecord;
                                        if (sourceLayerRecord != null && sourceLayerRecord.IsLocked)
                                        {
                                            canConvert = false;
                                        }
                                    }
                                }

                                if (canConvert)
                                {
                                    entity.UpgradeOpen();
                                    entity.Layer = targetLayer;
                                    result.ConvertedCount++;
                                }
                                else
                                {
                                    result.Errors.Add("����b��w�ϼh�W�A�w���L");
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
                System.Diagnostics.Debug.WriteLine($"ExecuteConversion error: {ex}");
            }

            return result;
        }

        /// <summary>
        /// �ഫ���G���O
        /// </summary>
        public class ConversionResult
        {
            public int ConvertedCount { get; set; }
            public int ErrorCount { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }
    }
}