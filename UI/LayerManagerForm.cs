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

        // UI 控制項
        private ComboBox layerComboBox;
        private CheckBox unlockTargetLayerCheckBox;
        private CheckBox createLayerCheckBox;
        private CheckBox handleLockedObjectsCheckBox;
        private Button convertButton;
        private Button cancelButton;
        private Button previewButton;
        private Label statusLabel;
        private Label infoLabel;
        private GroupBox layerGroupBox;
        private GroupBox settingsGroupBox;

        public LayerManagerForm()
        {
            _entityIds = Array.Empty<ObjectId>();
            InitializeComponents();
        }

        public LayerManagerForm(ObjectId[] entityIds)
        {
            _entityIds = entityIds;
            InitializeComponents();
            LoadLayers();
        }

        private void InitializeComponents()
        {
            this.layerComboBox = new ComboBox();
            this.unlockTargetLayerCheckBox = new CheckBox();
            this.createLayerCheckBox = new CheckBox();
            this.handleLockedObjectsCheckBox = new CheckBox();
            this.convertButton = new Button();
            this.cancelButton = new Button();
            this.previewButton = new Button();
            this.statusLabel = new Label();
            this.infoLabel = new Label();
            this.layerGroupBox = new GroupBox();
            this.settingsGroupBox = new GroupBox();
            this.layerGroupBox.SuspendLayout();
            this.settingsGroupBox.SuspendLayout();
            this.SuspendLayout();
            
            // LayerManagerForm
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(520, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LayerManagerForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "AutoCAD 圖層管理器";
            
            // infoLabel
            this.infoLabel.AutoSize = true;
            this.infoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point);
            this.infoLabel.ForeColor = Color.DarkBlue;
            this.infoLabel.Location = new Point(12, 15);
            this.infoLabel.Name = "infoLabel";
            this.infoLabel.Size = new Size(200, 20);
            this.infoLabel.TabIndex = 0;
            this.infoLabel.Text = "已選取物件";
            
            // layerGroupBox
            this.layerGroupBox.Controls.Add(this.layerComboBox);
            this.layerGroupBox.Controls.Add(this.previewButton);
            this.layerGroupBox.Location = new Point(12, 50);
            this.layerGroupBox.Name = "layerGroupBox";
            this.layerGroupBox.Size = new Size(480, 80);
            this.layerGroupBox.TabIndex = 1;
            this.layerGroupBox.TabStop = false;
            this.layerGroupBox.Text = "目標圖層";
            
            // layerComboBox
            this.layerComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this.layerComboBox.FormattingEnabled = true;
            this.layerComboBox.Location = new Point(15, 30);
            this.layerComboBox.Name = "layerComboBox";
            this.layerComboBox.Size = new Size(350, 23);
            this.layerComboBox.TabIndex = 0;
            
            // previewButton
            this.previewButton.Location = new Point(380, 29);
            this.previewButton.Name = "previewButton";
            this.previewButton.Size = new Size(75, 25);
            this.previewButton.TabIndex = 1;
            this.previewButton.Text = "預覽";
            this.previewButton.UseVisualStyleBackColor = true;
            this.previewButton.Click += this.PreviewButton_Click;
            
            // settingsGroupBox
            this.settingsGroupBox.Controls.Add(this.unlockTargetLayerCheckBox);
            this.settingsGroupBox.Controls.Add(this.createLayerCheckBox);
            this.settingsGroupBox.Controls.Add(this.handleLockedObjectsCheckBox);
            this.settingsGroupBox.Location = new Point(12, 150);
            this.settingsGroupBox.Name = "settingsGroupBox";
            this.settingsGroupBox.Size = new Size(480, 150);
            this.settingsGroupBox.TabIndex = 2;
            this.settingsGroupBox.TabStop = false;
            this.settingsGroupBox.Text = "轉換設定";
            
            // unlockTargetLayerCheckBox
            this.unlockTargetLayerCheckBox.AutoSize = true;
            this.unlockTargetLayerCheckBox.Checked = true;
            this.unlockTargetLayerCheckBox.CheckState = CheckState.Checked;
            this.unlockTargetLayerCheckBox.Location = new Point(15, 30);
            this.unlockTargetLayerCheckBox.Name = "unlockTargetLayerCheckBox";
            this.unlockTargetLayerCheckBox.Size = new Size(135, 19);
            this.unlockTargetLayerCheckBox.TabIndex = 0;
            this.unlockTargetLayerCheckBox.Text = "自動解鎖目標圖層";
            this.unlockTargetLayerCheckBox.UseVisualStyleBackColor = true;
            
            // createLayerCheckBox
            this.createLayerCheckBox.AutoSize = true;
            this.createLayerCheckBox.Checked = true;
            this.createLayerCheckBox.CheckState = CheckState.Checked;
            this.createLayerCheckBox.Location = new Point(15, 60);
            this.createLayerCheckBox.Name = "createLayerCheckBox";
            this.createLayerCheckBox.Size = new Size(159, 19);
            this.createLayerCheckBox.TabIndex = 1;
            this.createLayerCheckBox.Text = "自動創建不存在的圖層";
            this.createLayerCheckBox.UseVisualStyleBackColor = true;
            
            // handleLockedObjectsCheckBox
            this.handleLockedObjectsCheckBox.AutoSize = true;
            this.handleLockedObjectsCheckBox.Checked = true;
            this.handleLockedObjectsCheckBox.CheckState = CheckState.Checked;
            this.handleLockedObjectsCheckBox.Location = new Point(15, 90);
            this.handleLockedObjectsCheckBox.Name = "handleLockedObjectsCheckBox";
            this.handleLockedObjectsCheckBox.Size = new Size(147, 19);
            this.handleLockedObjectsCheckBox.TabIndex = 2;
            this.handleLockedObjectsCheckBox.Text = "處理鎖定圖層上的物件";
            this.handleLockedObjectsCheckBox.UseVisualStyleBackColor = true;
            
            // statusLabel
            this.statusLabel.AutoSize = true;
            this.statusLabel.ForeColor = Color.Gray;
            this.statusLabel.Location = new Point(12, 320);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new Size(55, 15);
            this.statusLabel.TabIndex = 3;
            this.statusLabel.Text = "準備就緒";
            
            // convertButton
            this.convertButton.BackColor = Color.LightGreen;
            this.convertButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point);
            this.convertButton.Location = new Point(330, 360);
            this.convertButton.Name = "convertButton";
            this.convertButton.Size = new Size(80, 35);
            this.convertButton.TabIndex = 4;
            this.convertButton.Text = "開始轉換";
            this.convertButton.UseVisualStyleBackColor = false;
            this.convertButton.Click += this.ConvertButton_Click;
            
            // cancelButton
            this.cancelButton.DialogResult = DialogResult.Cancel;
            this.cancelButton.Location = new Point(420, 360);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new Size(75, 35);
            this.cancelButton.TabIndex = 5;
            this.cancelButton.Text = "取消";
            this.cancelButton.UseVisualStyleBackColor = true;
            
            // 加入控制項到表單
            this.Controls.Add(this.infoLabel);
            this.Controls.Add(this.layerGroupBox);
            this.Controls.Add(this.settingsGroupBox);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.convertButton);
            this.Controls.Add(this.cancelButton);
            
            this.layerGroupBox.ResumeLayout(false);
            this.settingsGroupBox.ResumeLayout(false);
            this.settingsGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
            
            // 設定按鈕
            this.AcceptButton = this.convertButton;
            this.CancelButton = this.cancelButton;
        }

        private void LoadLayers()
        {
            try
            {
                this.statusLabel.Text = "正在載入圖層...";
                this.infoLabel.Text = $"已選取 {_entityIds.Length} 個物件";
                
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

        private void PreviewButton_Click(object sender, EventArgs e)
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

        private void ConvertButton_Click(object sender, EventArgs e)
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