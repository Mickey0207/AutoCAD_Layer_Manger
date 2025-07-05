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
<<<<<<< HEAD
        private readonly ILayerService _layerService;
        private readonly IEntityConverter _entityConverter;

=======
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
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
<<<<<<< HEAD
                this.statusLabel.Text = "正在載入圖層...";
                this.layerComboBox.Items.Clear();

                // 使用同步方法來避免線程問題
                var layers = GetLayersSync();

                foreach (var layer in layers)
                {
                    string displayName = layer.Name;
                    if (layer.IsLocked) displayName += " (鎖定)";
                    if (layer.IsFrozen) displayName += " (凍結)";

                    this.layerComboBox.Items.Add(displayName);
=======
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
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
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

<<<<<<< HEAD
        /// <summary>
        /// 同步獲取圖層列表
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
                System.Diagnostics.Debug.WriteLine($"獲取圖層失敗: {ex.Message}");
            }

            return layers.OrderBy(l => l.Name).ToList();
        }

        /// <summary>
        /// 自動選擇方法CheckBox變更事件
        /// </summary>
        private void autoSelectMethodCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // 當啟用自動選擇時，啟用所有方法
            if (this.autoSelectMethodCheckBox.Checked)
            {
                this.blockExplodeMethodCheckBox.Checked = true;
                this.referenceEditMethodCheckBox.Checked = true;
                this.blockEditorMethodCheckBox.Checked = false; // 圖塊編輯器需要手動，預設不啟用

                // 可以選擇性禁用手動選擇
                this.blockExplodeMethodCheckBox.Enabled = true;
                this.referenceEditMethodCheckBox.Enabled = true;
                this.blockEditorMethodCheckBox.Enabled = true;
            }
            else
            {
                // 手動模式，用戶可以自由選擇
                this.blockExplodeMethodCheckBox.Enabled = true;
                this.referenceEditMethodCheckBox.Enabled = true;
                this.blockEditorMethodCheckBox.Enabled = true;
            }
        }

=======
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
        private void previewButton_Click(object sender, EventArgs e)
        {
            if (this.layerComboBox.SelectedItem is string selectedItem)
            {
                string layerName = selectedItem.Split(' ')[0];
<<<<<<< HEAD
                string message = "轉換預覽:\n\n";
                message += $"物件數量: {_entityIds.Length}\n";
                message += $"目標圖層: {layerName}\n\n";

                message += "基本設定:\n";
                message += $"  自動解鎖目標圖層: {(this.unlockTargetLayerCheckBox.Checked ? "是" : "否")}\n";
                message += $"  自動創建圖層: {(this.createLayerCheckBox.Checked ? "是" : "否")}\n";
                message += $"  強制轉換鎖定物件: {(this.handleLockedObjectsCheckBox.Checked ? "是" : "否")}\n";
                message += $"  處理鎖定圖層的標註和動態圖塊: {(this.processAnnotationsCheckBox.Checked ? "是" : "否")}\n\n";

                message += "處理方法:\n";
                if (this.autoSelectMethodCheckBox.Checked)
                {
                    message += "  智能自動選擇: 是 (推薦)\n";
                    message += "     失敗時自動嘗試其他方法\n";
                    message += "     優先順序: 傳統方法 → 分解重組 → 現地編輯\n";
                }
                else
                {
                    message += "  手動選擇方法:\n";
                }

                var enabledMethods = new List<string>();
                if (this.blockExplodeMethodCheckBox.Checked) enabledMethods.Add("分解重組法");
                if (this.referenceEditMethodCheckBox.Checked) enabledMethods.Add("現地編輯法");
                if (this.blockEditorMethodCheckBox.Checked) enabledMethods.Add("圖塊編輯器法");

                if (enabledMethods.Count > 0)
                {
                    message += $"     啟用方法: {string.Join(", ", enabledMethods)}\n";
                }
                else if (!this.autoSelectMethodCheckBox.Checked)
                {
                    message += "     僅使用傳統方法\n";
                }

                if (this.blockEditorMethodCheckBox.Checked)
                {
                    message += "\n注意: 圖塊編輯器法需要手動操作";
                }

                if (this.processAnnotationsCheckBox.Checked)
                {
                    message += "\n特別處理: 鎖定圖層的標註、尺寸和動態圖塊會被自動解鎖並轉換";
                }

                MessageBox.Show(message, "轉換預覽", MessageBoxButtons.OK, MessageBoxIcon.Information);
=======
                string message = "�w���ഫ�ާ@:\n\n";
                message += $"����ƶq: {_entityIds.Length}\n";
                message += $"�ؼйϼh: {layerName}\n\n";
                message += "�ഫ�]�w:\n";
                message += $"? �۰ʸ���ؼйϼh: {(this.unlockTargetLayerCheckBox.Checked ? "�O" : "�_")}\n";
                message += $"? �۰ʳЫعϼh: {(this.createLayerCheckBox.Checked ? "�O" : "�_")}\n";
                message += $"? �B�z��w����: {(this.handleLockedObjectsCheckBox.Checked ? "�O" : "�_")}\n";
                
                MessageBox.Show(message, "�ഫ�w��", MessageBoxButtons.OK, MessageBoxIcon.Information);
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
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
<<<<<<< HEAD

                    // 創建轉換選項 - 智能配置
                    var options = new ConversionOptions
                    {
                        CreateTargetLayer = this.createLayerCheckBox.Checked,
                        UnlockTargetLayer = this.unlockTargetLayerCheckBox.Checked,
                        ForceConvertLockedObjects = this.handleLockedObjectsCheckBox.Checked,
                        ProcessAnnotationsOnLockedLayers = this.processAnnotationsCheckBox.Checked, // 新增

                        // 智能方法選擇
                        UseBlockExplodeMethod = this.autoSelectMethodCheckBox.Checked || this.blockExplodeMethodCheckBox.Checked,
                        UseReferenceEditMethod = this.autoSelectMethodCheckBox.Checked || this.referenceEditMethodCheckBox.Checked,
                        UseBlockEditorMethod = this.blockEditorMethodCheckBox.Checked, // 只有手動選擇才啟用

                        ProcessBlocks = true,
                        MaxDepth = 50,
                        PreferredBlockMethod = GetPreferredBlockMethod(),

                        // 新增：啟用智能自動重試
                        EnableAutoRetry = this.autoSelectMethodCheckBox.Checked
                    };

                    // 使用同步方法執行轉換
                    Result = ExecuteConversionSync(layerName, options);

                    this.statusLabel.Text = "轉換完成！";

                    string resultMessage = "轉換完成！\n\n";
                    resultMessage += $"成功轉換: {Result.ConvertedCount} 個物件\n";
                    if (Result.SkippedCount > 0)
                    {
                        resultMessage += $"跳過: {Result.SkippedCount} 個物件\n";
                    }
                    if (Result.ErrorCount > 0)
                    {
                        resultMessage += $"錯誤: {Result.ErrorCount} 個\n";
                    }

=======
                    
                    Result = ExecuteConversion(layerName);
                    
                    this.statusLabel.Text = "�ഫ�����I";
                    
                    string resultMessage = "�ഫ�����I\n\n";
                    resultMessage += $"���\�ഫ: {Result.ConvertedCount} �Ӫ���\n";
                    resultMessage += $"���~: {Result.ErrorCount} ��\n";
                    
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
                    if (Result.Errors.Any())
                    {
                        resultMessage += $"\n錯誤詳情:\n";
                        foreach (var error in Result.Errors.Take(3))
                        {
                            resultMessage += $"  {error}\n";
                        }
                        if (Result.Errors.Count > 3)
                        {
                            resultMessage += $"  還有 {Result.Errors.Count - 3} 個錯誤\n";
                        }
                    }
<<<<<<< HEAD

                    // 顯示使用的方法統計
                    if (options.EnableAutoRetry)
                    {
                        resultMessage += "\n使用了智能自動選擇模式";
                    }

                    if (options.ProcessAnnotationsOnLockedLayers)
                    {
                        resultMessage += "\n已處理鎖定圖層的標註和動態圖塊";
                    }

                    var usedMethods = new List<string>();
                    if (options.UseBlockExplodeMethod) usedMethods.Add("分解重組法");
                    if (options.UseReferenceEditMethod) usedMethods.Add("現地編輯法");
                    if (options.UseBlockEditorMethod) usedMethods.Add("圖塊編輯器法");

                    if (usedMethods.Count > 0)
                    {
                        resultMessage += $"\n啟用的處理方法: {string.Join(", ", usedMethods)}";
                    }

                    MessageBox.Show(resultMessage, "轉換結果",
                        MessageBoxButtons.OK,
=======
                    
                    MessageBox.Show(resultMessage, "�ഫ���G", 
                        MessageBoxButtons.OK, 
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
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

<<<<<<< HEAD
        /// <summary>
        /// 根據UI設定獲取首選的圖塊處理方法
        /// </summary>
        private BlockProcessingMethod GetPreferredBlockMethod()
        {
            // 根據CheckBox的選擇確定優先順序
            if (this.blockEditorMethodCheckBox.Checked)
            {
                return BlockProcessingMethod.BlockEditor;
            }
            else if (this.referenceEditMethodCheckBox.Checked)
            {
                return BlockProcessingMethod.ReferenceEdit;
            }
            else if (this.blockExplodeMethodCheckBox.Checked)
            {
                return BlockProcessingMethod.ExplodeRecombine;
            }
            else
            {
                return BlockProcessingMethod.Traditional;
            }
        }

        /// <summary>
        /// 同步執行轉換
        /// </summary>
        private ConversionResult ExecuteConversionSync(string targetLayer, ConversionOptions options)
=======
        private ConversionResult ExecuteConversion(string targetLayer)
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
        {
            var result = new ConversionResult();

            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc?.Database == null) return result;

                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
<<<<<<< HEAD
                    // 如果需要，創建圖層
                    if (options.CreateTargetLayer)
=======
                    // �p�G�ݭn�A�Ыعϼh
                    if (this.createLayerCheckBox.Checked)
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
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

<<<<<<< HEAD
                    // 如果需要，解鎖目標圖層
                    if (options.UnlockTargetLayer)
=======
                    // �p�G�ݭn�A����ؼйϼh
                    if (this.unlockTargetLayerCheckBox.Checked)
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
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

<<<<<<< HEAD
                    // 使用EntityConverter進行轉換
=======
                    // �ഫ����
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
                    foreach (var objId in _entityIds)
                    {
                        try
                        {
                            if (tr.GetObject(objId, OpenMode.ForRead) is Entity entity)
                            {
<<<<<<< HEAD
                                var entityResult = _entityConverter.ConvertEntityToLayer(
                                    tr, entity, targetLayer, Matrix3d.Identity, options);

                                result.ConvertedCount += entityResult.ConvertedCount;
                                result.SkippedCount += entityResult.SkippedCount;
                                result.ErrorCount += entityResult.ErrorCount;
                                result.Errors.AddRange(entityResult.Errors);
=======
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
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
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
<<<<<<< HEAD
                result.Errors.Add($"轉換失敗: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ExecuteConversionSync error: {ex}");
=======
                result.Errors.Add($"�ഫ����: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ExecuteConversion error: {ex}");
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
            }

            return result;
        }

<<<<<<< HEAD
        private void blockEditorMethodCheckBox_CheckedChanged(object sender, EventArgs e)
        {

=======
        /// <summary>
        /// �ഫ���G���O
        /// </summary>
        public class ConversionResult
        {
            public int ConvertedCount { get; set; }
            public int ErrorCount { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
        }
    }
}