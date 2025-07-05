namespace AutoCAD_Layer_Manger.UI
{
    partial class LayerManagerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            infoLabel = new Label();
            layerGroupBox = new GroupBox();
            previewButton = new Button();
            layerComboBox = new ComboBox();
            settingsGroupBox = new GroupBox();
            unlockTargetLayerCheckBox = new CheckBox();
            convertLockedLayersCheckBox = new CheckBox();
            entityTypesGroupBox = new GroupBox();
            convertPolylinesCheckBox = new CheckBox();
            convertLinesCheckBox = new CheckBox();
            convertCirclesCheckBox = new CheckBox();
            convertArcsCheckBox = new CheckBox();
            convertBlocksCheckBox = new CheckBox();
            convertDynamicBlocksCheckBox = new CheckBox();
            convertDimensionsCheckBox = new CheckBox();
            statusLabel = new Label();
            convertButton = new Button();
            cancelButton = new Button();
            layerGroupBox.SuspendLayout();
            settingsGroupBox.SuspendLayout();
            entityTypesGroupBox.SuspendLayout();
            SuspendLayout();
            // 
            // infoLabel
            // 
            infoLabel.AutoSize = true;
            infoLabel.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point, 136);
            infoLabel.ForeColor = Color.DarkBlue;
            infoLabel.Location = new Point(22, 29);
            infoLabel.Margin = new Padding(6, 0, 6, 0);
            infoLabel.Name = "infoLabel";
            infoLabel.Size = new Size(138, 29);
            infoLabel.TabIndex = 0;
            infoLabel.Text = "已選取物件";
            // 
            // layerGroupBox
            // 
            layerGroupBox.Controls.Add(previewButton);
            layerGroupBox.Controls.Add(layerComboBox);
            layerGroupBox.Location = new Point(22, 96);
            layerGroupBox.Margin = new Padding(6, 6, 6, 6);
            layerGroupBox.Name = "layerGroupBox";
            layerGroupBox.Padding = new Padding(6, 6, 6, 6);
            layerGroupBox.Size = new Size(880, 153);
            layerGroupBox.TabIndex = 1;
            layerGroupBox.TabStop = false;
            layerGroupBox.Text = "目標圖層";
            // 
            // previewButton
            // 
            previewButton.Location = new Point(697, 56);
            previewButton.Margin = new Padding(6, 6, 6, 6);
            previewButton.Name = "previewButton";
            previewButton.Size = new Size(138, 48);
            previewButton.TabIndex = 1;
            previewButton.Text = "預覽";
            previewButton.UseVisualStyleBackColor = true;
            previewButton.Click += previewButton_Click;
            // 
            // layerComboBox
            // 
            layerComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            layerComboBox.FormattingEnabled = true;
            layerComboBox.Location = new Point(28, 58);
            layerComboBox.Margin = new Padding(6, 6, 6, 6);
            layerComboBox.Name = "layerComboBox";
            layerComboBox.Size = new Size(638, 31);
            layerComboBox.TabIndex = 0;
            // 
            // settingsGroupBox
            // 
            settingsGroupBox.Controls.Add(convertLockedLayersCheckBox);
            settingsGroupBox.Controls.Add(unlockTargetLayerCheckBox);
            settingsGroupBox.Location = new Point(22, 288);
            settingsGroupBox.Margin = new Padding(6, 6, 6, 6);
            settingsGroupBox.Name = "settingsGroupBox";
            settingsGroupBox.Padding = new Padding(6, 6, 6, 6);
            settingsGroupBox.Size = new Size(880, 120);
            settingsGroupBox.TabIndex = 2;
            settingsGroupBox.TabStop = false;
            settingsGroupBox.Text = "圖層設定";
            // 
            // unlockTargetLayerCheckBox
            // 
            unlockTargetLayerCheckBox.AutoSize = true;
            unlockTargetLayerCheckBox.Checked = true;
            unlockTargetLayerCheckBox.CheckState = CheckState.Checked;
            unlockTargetLayerCheckBox.Location = new Point(28, 35);
            unlockTargetLayerCheckBox.Margin = new Padding(6, 6, 6, 6);
            unlockTargetLayerCheckBox.Name = "unlockTargetLayerCheckBox";
            unlockTargetLayerCheckBox.Size = new Size(180, 27);
            unlockTargetLayerCheckBox.TabIndex = 0;
            unlockTargetLayerCheckBox.Text = "自動解鎖目標圖層";
            unlockTargetLayerCheckBox.UseVisualStyleBackColor = true;
            // 
            // convertLockedLayersCheckBox
            // 
            convertLockedLayersCheckBox.AutoSize = true;
            convertLockedLayersCheckBox.Checked = true;
            convertLockedLayersCheckBox.CheckState = CheckState.Checked;
            convertLockedLayersCheckBox.Location = new Point(28, 72);
            convertLockedLayersCheckBox.Margin = new Padding(6, 6, 6, 6);
            convertLockedLayersCheckBox.Name = "convertLockedLayersCheckBox";
            convertLockedLayersCheckBox.Size = new Size(156, 27);
            convertLockedLayersCheckBox.TabIndex = 1;
            convertLockedLayersCheckBox.Text = "轉換鎖定的圖層";
            convertLockedLayersCheckBox.UseVisualStyleBackColor = true;
            // 
            // entityTypesGroupBox
            // 
            entityTypesGroupBox.Controls.Add(convertPolylinesCheckBox);
            entityTypesGroupBox.Controls.Add(convertLinesCheckBox);
            entityTypesGroupBox.Controls.Add(convertCirclesCheckBox);
            entityTypesGroupBox.Controls.Add(convertArcsCheckBox);
            entityTypesGroupBox.Controls.Add(convertBlocksCheckBox);
            entityTypesGroupBox.Controls.Add(convertDynamicBlocksCheckBox);
            entityTypesGroupBox.Controls.Add(convertDimensionsCheckBox);
            entityTypesGroupBox.Location = new Point(22, 428);
            entityTypesGroupBox.Margin = new Padding(6, 6, 6, 6);
            entityTypesGroupBox.Name = "entityTypesGroupBox";
            entityTypesGroupBox.Padding = new Padding(6, 6, 6, 6);
            entityTypesGroupBox.Size = new Size(880, 240);
            entityTypesGroupBox.TabIndex = 3;
            entityTypesGroupBox.TabStop = false;
            entityTypesGroupBox.Text = "物件類型選擇";
            // 
            // convertPolylinesCheckBox
            // 
            convertPolylinesCheckBox.AutoSize = true;
            convertPolylinesCheckBox.Checked = true;
            convertPolylinesCheckBox.CheckState = CheckState.Checked;
            convertPolylinesCheckBox.Location = new Point(28, 35);
            convertPolylinesCheckBox.Margin = new Padding(6, 6, 6, 6);
            convertPolylinesCheckBox.Name = "convertPolylinesCheckBox";
            convertPolylinesCheckBox.Size = new Size(120, 27);
            convertPolylinesCheckBox.TabIndex = 0;
            convertPolylinesCheckBox.Text = "轉換聚合線";
            convertPolylinesCheckBox.UseVisualStyleBackColor = true;
            // 
            // convertLinesCheckBox
            // 
            convertLinesCheckBox.AutoSize = true;
            convertLinesCheckBox.Checked = true;
            convertLinesCheckBox.CheckState = CheckState.Checked;
            convertLinesCheckBox.Location = new Point(200, 35);
            convertLinesCheckBox.Margin = new Padding(6, 6, 6, 6);
            convertLinesCheckBox.Name = "convertLinesCheckBox";
            convertLinesCheckBox.Size = new Size(84, 27);
            convertLinesCheckBox.TabIndex = 1;
            convertLinesCheckBox.Text = "轉換線";
            convertLinesCheckBox.UseVisualStyleBackColor = true;
            // 
            // convertCirclesCheckBox
            // 
            convertCirclesCheckBox.AutoSize = true;
            convertCirclesCheckBox.Checked = true;
            convertCirclesCheckBox.CheckState = CheckState.Checked;
            convertCirclesCheckBox.Location = new Point(340, 35);
            convertCirclesCheckBox.Margin = new Padding(6, 6, 6, 6);
            convertCirclesCheckBox.Name = "convertCirclesCheckBox";
            convertCirclesCheckBox.Size = new Size(84, 27);
            convertCirclesCheckBox.TabIndex = 2;
            convertCirclesCheckBox.Text = "轉換圓";
            convertCirclesCheckBox.UseVisualStyleBackColor = true;
            // 
            // convertArcsCheckBox
            // 
            convertArcsCheckBox.AutoSize = true;
            convertArcsCheckBox.Checked = true;
            convertArcsCheckBox.CheckState = CheckState.Checked;
            convertArcsCheckBox.Location = new Point(480, 35);
            convertArcsCheckBox.Margin = new Padding(6, 6, 6, 6);
            convertArcsCheckBox.Name = "convertArcsCheckBox";
            convertArcsCheckBox.Size = new Size(84, 27);
            convertArcsCheckBox.TabIndex = 3;
            convertArcsCheckBox.Text = "轉換弧";
            convertArcsCheckBox.UseVisualStyleBackColor = true;
            // 
            // convertBlocksCheckBox
            // 
            convertBlocksCheckBox.AutoSize = true;
            convertBlocksCheckBox.Checked = true;
            convertBlocksCheckBox.CheckState = CheckState.Checked;
            convertBlocksCheckBox.Location = new Point(28, 85);
            convertBlocksCheckBox.Margin = new Padding(6, 6, 6, 6);
            convertBlocksCheckBox.Name = "convertBlocksCheckBox";
            convertBlocksCheckBox.Size = new Size(102, 27);
            convertBlocksCheckBox.TabIndex = 4;
            convertBlocksCheckBox.Text = "轉換圖塊";
            convertBlocksCheckBox.UseVisualStyleBackColor = true;
            // 
            // convertDynamicBlocksCheckBox
            // 
            convertDynamicBlocksCheckBox.AutoSize = true;
            convertDynamicBlocksCheckBox.Checked = true;
            convertDynamicBlocksCheckBox.CheckState = CheckState.Checked;
            convertDynamicBlocksCheckBox.Location = new Point(200, 85);
            convertDynamicBlocksCheckBox.Margin = new Padding(6, 6, 6, 6);
            convertDynamicBlocksCheckBox.Name = "convertDynamicBlocksCheckBox";
            convertDynamicBlocksCheckBox.Size = new Size(138, 27);
            convertDynamicBlocksCheckBox.TabIndex = 5;
            convertDynamicBlocksCheckBox.Text = "轉換動態圖塊";
            convertDynamicBlocksCheckBox.UseVisualStyleBackColor = true;
            // 
            // convertDimensionsCheckBox
            // 
            convertDimensionsCheckBox.AutoSize = true;
            convertDimensionsCheckBox.Checked = true;
            convertDimensionsCheckBox.CheckState = CheckState.Checked;
            convertDimensionsCheckBox.Location = new Point(400, 85);
            convertDimensionsCheckBox.Margin = new Padding(6, 6, 6, 6);
            convertDimensionsCheckBox.Name = "convertDimensionsCheckBox";
            convertDimensionsCheckBox.Size = new Size(102, 27);
            convertDimensionsCheckBox.TabIndex = 6;
            convertDimensionsCheckBox.Text = "轉換標註";
            convertDimensionsCheckBox.UseVisualStyleBackColor = true;
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.ForeColor = Color.Gray;
            statusLabel.Location = new Point(22, 690);
            statusLabel.Margin = new Padding(6, 0, 6, 0);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(82, 23);
            statusLabel.TabIndex = 4;
            statusLabel.Text = "等待操作";
            // 
            // convertButton
            // 
            convertButton.BackColor = Color.LightGreen;
            convertButton.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, 136);
            convertButton.Location = new Point(605, 740);
            convertButton.Margin = new Padding(6, 6, 6, 6);
            convertButton.Name = "convertButton";
            convertButton.Size = new Size(147, 67);
            convertButton.TabIndex = 5;
            convertButton.Text = "開始轉換";
            convertButton.UseVisualStyleBackColor = false;
            convertButton.Click += convertButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(770, 740);
            cancelButton.Margin = new Padding(6, 6, 6, 6);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(138, 67);
            cancelButton.TabIndex = 6;
            cancelButton.Text = "取消";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // LayerManagerForm
            // 
            AcceptButton = convertButton;
            AutoScaleDimensions = new SizeF(11F, 23F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(953, 850);
            Controls.Add(cancelButton);
            Controls.Add(convertButton);
            Controls.Add(statusLabel);
            Controls.Add(entityTypesGroupBox);
            Controls.Add(settingsGroupBox);
            Controls.Add(layerGroupBox);
            Controls.Add(infoLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(6, 6, 6, 6);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LayerManagerForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AutoCAD 圖層管理器 - 物件類型選擇";
            layerGroupBox.ResumeLayout(false);
            settingsGroupBox.ResumeLayout(false);
            settingsGroupBox.PerformLayout();
            entityTypesGroupBox.ResumeLayout(false);
            entityTypesGroupBox.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label infoLabel;
        private System.Windows.Forms.GroupBox layerGroupBox;
        private System.Windows.Forms.Button previewButton;
        private System.Windows.Forms.ComboBox layerComboBox;
        private System.Windows.Forms.GroupBox settingsGroupBox;
        private System.Windows.Forms.CheckBox unlockTargetLayerCheckBox;
        private System.Windows.Forms.CheckBox convertLockedLayersCheckBox;
        private System.Windows.Forms.GroupBox entityTypesGroupBox;
        private System.Windows.Forms.CheckBox convertPolylinesCheckBox;
        private System.Windows.Forms.CheckBox convertLinesCheckBox;
        private System.Windows.Forms.CheckBox convertCirclesCheckBox;
        private System.Windows.Forms.CheckBox convertArcsCheckBox;
        private System.Windows.Forms.CheckBox convertBlocksCheckBox;
        private System.Windows.Forms.CheckBox convertDynamicBlocksCheckBox;
        private System.Windows.Forms.CheckBox convertDimensionsCheckBox;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Button convertButton;
        private System.Windows.Forms.Button cancelButton;
    }
}