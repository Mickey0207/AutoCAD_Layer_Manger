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
    /// 統一的圖層管理器表單 - 可用設計工具編輯
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
            // 設定物件數量資訊
            this.infoLabel.Text = $"已選取 {_entityIds.Length} 個物件";
            
            // 載入圖層數據
            LoadLayers();
        }

        private void LoadLayers()
        {
            try
            {
                this.statusLabel.Text = "正在載入圖層...";
                
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
                        if (layer.IsLocked) displayName += " (鎖定)";
                        if (layer.IsFrozen) displayName += " (凍結)";
                        
                        this.layerComboBox.Items.Add(displayName);
                    }
                }
                
                if (this.layerComboBox.Items.Count > 0)
                {
                    this.layerComboBox.SelectedIndex = 0;
                }

                this.statusLabel.Text = $"載入完成，找到 {this.layerComboBox.Items.Count} 個圖層";
            }
            catch (System.Exception ex)
            {
                this.statusLabel.Text = "載入圖層失敗";
                MessageBox.Show($"載入圖層失敗: {ex.Message}", "錯誤", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void previewButton_Click(object sender, EventArgs e)
        {
            if (this.layerComboBox.SelectedItem is string selectedItem)
            {
                string layerName = selectedItem.Split(' ')[0];
                string message = "預覽轉換操作:\n\n";
                message += $"物件數量: {_entityIds.Length}\n";
                message += $"目標圖層: {layerName}\n\n";
                message += "轉換設定:\n";
                message += $"? 自動解鎖目標圖層: {(this.unlockTargetLayerCheckBox.Checked ? "是" : "否")}\n";
                message += $"? 自動創建圖層: {(this.createLayerCheckBox.Checked ? "是" : "否")}\n";
                message += $"? 處理鎖定物件: {(this.handleLockedObjectsCheckBox.Checked ? "是" : "否")}\n";
                
                MessageBox.Show(message, "轉換預覽", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    this.statusLabel.Text = "正在執行轉換...";
                    
                    string layerName = selectedItem.Split(' ')[0];
                    
                    Result = ExecuteConversion(layerName);
                    
                    this.statusLabel.Text = "轉換完成！";
                    
                    string resultMessage = "轉換完成！\n\n";
                    resultMessage += $"成功轉換: {Result.ConvertedCount} 個物件\n";
                    resultMessage += $"錯誤: {Result.ErrorCount} 個\n";
                    
                    if (Result.Errors.Any())
                    {
                        resultMessage += $"\n錯誤詳情:\n{string.Join("\n", Result.Errors.Take(3))}";
                        if (Result.Errors.Count > 3)
                        {
                            resultMessage += $"\n... 還有 {Result.Errors.Count - 3} 個錯誤";
                        }
                    }
                    
                    MessageBox.Show(resultMessage, "轉換結果", 
                        MessageBoxButtons.OK, 
                        Result.ErrorCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                    
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (System.Exception ex)
                {
                    this.statusLabel.Text = "轉換失敗";
                    MessageBox.Show($"轉換失敗: {ex.Message}", "錯誤", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                    this.convertButton.Enabled = true;
                    this.cancelButton.Enabled = true;
                }
            }
            else
            {
                MessageBox.Show("請選擇目標圖層", "提示", 
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
                    // 如果需要，創建圖層
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

                    // 如果需要，解鎖目標圖層
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

                    // 轉換物件
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
                                    result.Errors.Add("物件在鎖定圖層上，已跳過");
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            result.ErrorCount++;
                            result.Errors.Add($"物件 {objId}: {ex.Message}");
                        }
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add($"轉換失敗: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ExecuteConversion error: {ex}");
            }

            return result;
        }

        /// <summary>
        /// 轉換結果類別
        /// </summary>
        public class ConversionResult
        {
            public int ConvertedCount { get; set; }
            public int ErrorCount { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }
    }
}