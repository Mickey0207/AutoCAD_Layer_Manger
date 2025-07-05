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
    /// 統一的圖層管理器表單 - 可用設計工具編輯
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
                this.layerComboBox.Items.Clear();

                // 使用同步方法來避免線程問題
                var layers = GetLayersSync();

                foreach (var layer in layers)
                {
                    string displayName = layer.Name;
                    if (layer.IsLocked) displayName += " (鎖定)";
                    if (layer.IsFrozen) displayName += " (凍結)";

                    this.layerComboBox.Items.Add(displayName);
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

        private void previewButton_Click(object sender, EventArgs e)
        {
            if (this.layerComboBox.SelectedItem is string selectedItem)
            {
                string layerName = selectedItem.Split(' ')[0];
                string message = "轉換預覽:\n\n";
                message += $"物件數量: {_entityIds.Length}\n";
                message += $"目標圖層: {layerName}\n\n";

                message += "圖層設定:\n";
                message += $"  自動解鎖目標圖層: {(this.unlockTargetLayerCheckBox.Checked ? "是" : "否")}\n";
                message += $"  轉換鎖定的圖層: {(this.convertLockedLayersCheckBox.Checked ? "是" : "否")}\n\n";

                message += "物件類型選擇:\n";
                var selectedTypes = new List<string>();
                if (this.convertPolylinesCheckBox.Checked) selectedTypes.Add("聚合線");
                if (this.convertLinesCheckBox.Checked) selectedTypes.Add("線");
                if (this.convertCirclesCheckBox.Checked) selectedTypes.Add("圓");
                if (this.convertArcsCheckBox.Checked) selectedTypes.Add("弧");
                if (this.convertBlocksCheckBox.Checked) selectedTypes.Add("圖塊");
                if (this.convertDynamicBlocksCheckBox.Checked) selectedTypes.Add("動態圖塊");
                if (this.convertDimensionsCheckBox.Checked) selectedTypes.Add("標註");

                if (selectedTypes.Count > 0)
                {
                    message += $"  將轉換: {string.Join(", ", selectedTypes)}\n";
                }
                else
                {
                    message += "  注意: 未選擇任何物件類型\n";
                }

                if (!this.convertLockedLayersCheckBox.Checked)
                {
                    message += "\n注意: 鎖定圖層上的物件將被跳過";
                }

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

                    // 創建轉換選項 - 根據新UI設定
                    var options = new ConversionOptions
                    {
                        UnlockTargetLayer = this.unlockTargetLayerCheckBox.Checked,
                        ForceConvertLockedObjects = this.convertLockedLayersCheckBox.Checked,
                        
                        // 物件類型過濾
                        ProcessPolylines = this.convertPolylinesCheckBox.Checked,
                        ProcessLines = this.convertLinesCheckBox.Checked,
                        ProcessCircles = this.convertCirclesCheckBox.Checked,
                        ProcessArcs = this.convertArcsCheckBox.Checked,
                        ProcessBlocks = this.convertBlocksCheckBox.Checked,
                        ProcessDynamicBlocks = this.convertDynamicBlocksCheckBox.Checked,
                        ProcessDimensions = this.convertDimensionsCheckBox.Checked,
                        
                        // 基本設定
                        CreateTargetLayer = true, // 總是自動創建圖層
                        ProcessAllEntities = false, // 使用類型過濾
                        MaxDepth = 50,
                        EnableAutoRetry = true
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

                    // 顯示轉換的物件類型統計
                    var processedTypes = new List<string>();
                    if (options.ProcessPolylines) processedTypes.Add("聚合線");
                    if (options.ProcessLines) processedTypes.Add("線");
                    if (options.ProcessCircles) processedTypes.Add("圓");
                    if (options.ProcessArcs) processedTypes.Add("弧");
                    if (options.ProcessBlocks) processedTypes.Add("圖塊");
                    if (options.ProcessDynamicBlocks) processedTypes.Add("動態圖塊");
                    if (options.ProcessDimensions) processedTypes.Add("標註");
                    
                    if (processedTypes.Count > 0)
                    {
                        resultMessage += $"\n處理的物件類型: {string.Join(", ", processedTypes)}";
                    }

                    if (Result.Errors.Any())
                    {
                        resultMessage += $"\n\n錯誤詳情:\n";
                        foreach (var error in Result.Errors.Take(3))
                        {
                            resultMessage += $"  {error}\n";
                        }
                        if (Result.Errors.Count > 3)
                        {
                            resultMessage += $"  ... 還有 {Result.Errors.Count - 3} 個錯誤\n";
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

        /// <summary>
        /// 檢查是否應該處理此物件類型
        /// </summary>
        private bool ShouldProcessEntity(Entity entity, ConversionOptions options)
        {
            return entity switch
            {
                Polyline => options.ProcessPolylines,
                Polyline2d => options.ProcessPolylines,
                Polyline3d => options.ProcessPolylines,
                Line => options.ProcessLines,
                Circle => options.ProcessCircles,
                Arc => options.ProcessArcs,
                BlockReference blockRef => blockRef.IsDynamicBlock ? 
                    options.ProcessDynamicBlocks : options.ProcessBlocks,
                Dimension => options.ProcessDimensions,
                _ => true // 其他類型預設處理
            };
        }

        /// <summary>
        /// 同步執行轉換
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
                    // 如果需要，創建圖層
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

                    // 如果需要，解鎖目標圖層
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

                    // 使用EntityConverter進行轉換，配合物件類型過濾
                    foreach (var objId in _entityIds)
                    {
                        try
                        {
                            if (tr.GetObject(objId, OpenMode.ForRead) is Entity entity)
                            {
                                // 檢查物件類型是否被選中
                                if (ShouldProcessEntity(entity, options))
                                {
                                    var entityResult = _entityConverter.ConvertEntityToLayer(
                                        tr, entity, targetLayer, Matrix3d.Identity, options);

                                    result.ConvertedCount += entityResult.ConvertedCount;
                                    result.SkippedCount += entityResult.SkippedCount;
                                    result.ErrorCount += entityResult.ErrorCount;
                                    result.Errors.AddRange(entityResult.Errors);
                                }
                                else
                                {
                                    result.SkippedCount++;
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
                System.Diagnostics.Debug.WriteLine($"ExecuteConversionSync error: {ex}");
            }

            return result;
        }
    }
}