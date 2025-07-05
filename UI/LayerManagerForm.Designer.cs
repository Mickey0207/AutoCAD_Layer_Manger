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
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            this.infoLabel = new System.Windows.Forms.Label();
            this.layerGroupBox = new System.Windows.Forms.GroupBox();
            this.previewButton = new System.Windows.Forms.Button();
            this.layerComboBox = new System.Windows.Forms.ComboBox();
            this.settingsGroupBox = new System.Windows.Forms.GroupBox();
            this.handleLockedObjectsCheckBox = new System.Windows.Forms.CheckBox();
            this.createLayerCheckBox = new System.Windows.Forms.CheckBox();
            this.unlockTargetLayerCheckBox = new System.Windows.Forms.CheckBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.convertButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.layerGroupBox.SuspendLayout();
            this.settingsGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // infoLabel
            // 
            this.infoLabel.AutoSize = true;
            this.infoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.infoLabel.ForeColor = System.Drawing.Color.DarkBlue;
            this.infoLabel.Location = new System.Drawing.Point(12, 15);
            this.infoLabel.Name = "infoLabel";
            this.infoLabel.Size = new System.Drawing.Size(102, 20);
            this.infoLabel.TabIndex = 0;
            this.infoLabel.Text = "已選取物件";
            // 
            // layerGroupBox
            // 
            this.layerGroupBox.Controls.Add(this.previewButton);
            this.layerGroupBox.Controls.Add(this.layerComboBox);
            this.layerGroupBox.Location = new System.Drawing.Point(12, 50);
            this.layerGroupBox.Name = "layerGroupBox";
            this.layerGroupBox.Size = new System.Drawing.Size(480, 80);
            this.layerGroupBox.TabIndex = 1;
            this.layerGroupBox.TabStop = false;
            this.layerGroupBox.Text = "目標圖層";
            // 
            // previewButton
            // 
            this.previewButton.Location = new System.Drawing.Point(380, 29);
            this.previewButton.Name = "previewButton";
            this.previewButton.Size = new System.Drawing.Size(75, 25);
            this.previewButton.TabIndex = 1;
            this.previewButton.Text = "預覽";
            this.previewButton.UseVisualStyleBackColor = true;
            this.previewButton.Click += new System.EventHandler(this.previewButton_Click);
            // 
            // layerComboBox
            // 
            this.layerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.layerComboBox.FormattingEnabled = true;
            this.layerComboBox.Location = new System.Drawing.Point(15, 30);
            this.layerComboBox.Name = "layerComboBox";
            this.layerComboBox.Size = new System.Drawing.Size(350, 20);
            this.layerComboBox.TabIndex = 0;
            // 
            // settingsGroupBox
            // 
            this.settingsGroupBox.Controls.Add(this.handleLockedObjectsCheckBox);
            this.settingsGroupBox.Controls.Add(this.createLayerCheckBox);
            this.settingsGroupBox.Controls.Add(this.unlockTargetLayerCheckBox);
            this.settingsGroupBox.Location = new System.Drawing.Point(12, 150);
            this.settingsGroupBox.Name = "settingsGroupBox";
            this.settingsGroupBox.Size = new System.Drawing.Size(480, 150);
            this.settingsGroupBox.TabIndex = 2;
            this.settingsGroupBox.TabStop = false;
            this.settingsGroupBox.Text = "轉換設定";
            // 
            // handleLockedObjectsCheckBox
            // 
            this.handleLockedObjectsCheckBox.AutoSize = true;
            this.handleLockedObjectsCheckBox.Checked = true;
            this.handleLockedObjectsCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.handleLockedObjectsCheckBox.Location = new System.Drawing.Point(15, 90);
            this.handleLockedObjectsCheckBox.Name = "handleLockedObjectsCheckBox";
            this.handleLockedObjectsCheckBox.Size = new System.Drawing.Size(147, 16);
            this.handleLockedObjectsCheckBox.TabIndex = 2;
            this.handleLockedObjectsCheckBox.Text = "處理鎖定圖層上的物件";
            this.handleLockedObjectsCheckBox.UseVisualStyleBackColor = true;
            // 
            // createLayerCheckBox
            // 
            this.createLayerCheckBox.AutoSize = true;
            this.createLayerCheckBox.Checked = true;
            this.createLayerCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.createLayerCheckBox.Location = new System.Drawing.Point(15, 60);
            this.createLayerCheckBox.Name = "createLayerCheckBox";
            this.createLayerCheckBox.Size = new System.Drawing.Size(159, 16);
            this.createLayerCheckBox.TabIndex = 1;
            this.createLayerCheckBox.Text = "自動創建不存在的圖層";
            this.createLayerCheckBox.UseVisualStyleBackColor = true;
            // 
            // unlockTargetLayerCheckBox
            // 
            this.unlockTargetLayerCheckBox.AutoSize = true;
            this.unlockTargetLayerCheckBox.Checked = true;
            this.unlockTargetLayerCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.unlockTargetLayerCheckBox.Location = new System.Drawing.Point(15, 30);
            this.unlockTargetLayerCheckBox.Name = "unlockTargetLayerCheckBox";
            this.unlockTargetLayerCheckBox.Size = new System.Drawing.Size(135, 16);
            this.unlockTargetLayerCheckBox.TabIndex = 0;
            this.unlockTargetLayerCheckBox.Text = "自動解鎖目標圖層";
            this.unlockTargetLayerCheckBox.UseVisualStyleBackColor = true;
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.ForeColor = System.Drawing.Color.Gray;
            this.statusLabel.Location = new System.Drawing.Point(12, 320);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(53, 12);
            this.statusLabel.TabIndex = 3;
            this.statusLabel.Text = "準備就緒";
            // 
            // convertButton
            // 
            this.convertButton.BackColor = System.Drawing.Color.LightGreen;
            this.convertButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.convertButton.Location = new System.Drawing.Point(330, 360);
            this.convertButton.Name = "convertButton";
            this.convertButton.Size = new System.Drawing.Size(80, 35);
            this.convertButton.TabIndex = 4;
            this.convertButton.Text = "開始轉換";
            this.convertButton.UseVisualStyleBackColor = false;
            this.convertButton.Click += new System.EventHandler(this.convertButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(420, 360);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 35);
            this.cancelButton.TabIndex = 5;
            this.cancelButton.Text = "取消";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // LayerManagerForm
            // 
            this.AcceptButton = this.convertButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(520, 420);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.convertButton);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.settingsGroupBox);
            this.Controls.Add(this.layerGroupBox);
            this.Controls.Add(this.infoLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LayerManagerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AutoCAD 圖層管理器";
            this.layerGroupBox.ResumeLayout(false);
            this.settingsGroupBox.ResumeLayout(false);
            this.settingsGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label infoLabel;
        private System.Windows.Forms.GroupBox layerGroupBox;
        private System.Windows.Forms.Button previewButton;
        private System.Windows.Forms.ComboBox layerComboBox;
        private System.Windows.Forms.GroupBox settingsGroupBox;
        private System.Windows.Forms.CheckBox handleLockedObjectsCheckBox;
        private System.Windows.Forms.CheckBox createLayerCheckBox;
        private System.Windows.Forms.CheckBox unlockTargetLayerCheckBox;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Button convertButton;
        private System.Windows.Forms.Button cancelButton;
    }
}