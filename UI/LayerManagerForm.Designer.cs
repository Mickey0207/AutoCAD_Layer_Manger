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
<<<<<<< HEAD
            infoLabel = new Label();
            layerGroupBox = new GroupBox();
            previewButton = new Button();
            layerComboBox = new ComboBox();
            settingsGroupBox = new GroupBox();
            handleLockedObjectsCheckBox = new CheckBox();
            createLayerCheckBox = new CheckBox();
            unlockTargetLayerCheckBox = new CheckBox();
            processAnnotationsCheckBox = new CheckBox();
            methodsGroupBox = new GroupBox();
            autoSelectMethodCheckBox = new CheckBox();
            blockExplodeMethodCheckBox = new CheckBox();
            referenceEditMethodCheckBox = new CheckBox();
            blockEditorMethodCheckBox = new CheckBox();
            statusLabel = new Label();
            convertButton = new Button();
            cancelButton = new Button();
            layerGroupBox.SuspendLayout();
            settingsGroupBox.SuspendLayout();
            methodsGroupBox.SuspendLayout();
            SuspendLayout();
=======
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
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
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
<<<<<<< HEAD
            settingsGroupBox.Controls.Add(handleLockedObjectsCheckBox);
            settingsGroupBox.Controls.Add(createLayerCheckBox);
            settingsGroupBox.Controls.Add(unlockTargetLayerCheckBox);
            settingsGroupBox.Controls.Add(processAnnotationsCheckBox);
            settingsGroupBox.Location = new Point(22, 288);
            settingsGroupBox.Margin = new Padding(6, 6, 6, 6);
            settingsGroupBox.Name = "settingsGroupBox";
            settingsGroupBox.Padding = new Padding(6, 6, 6, 6);
            settingsGroupBox.Size = new Size(880, 260);
            settingsGroupBox.TabIndex = 2;
            settingsGroupBox.TabStop = false;
            settingsGroupBox.Text = "基本設定";
            // 
            // handleLockedObjectsCheckBox
            // 
            handleLockedObjectsCheckBox.AutoSize = true;
            handleLockedObjectsCheckBox.Checked = true;
            handleLockedObjectsCheckBox.CheckState = CheckState.Checked;
            handleLockedObjectsCheckBox.Location = new Point(28, 144);
            handleLockedObjectsCheckBox.Margin = new Padding(6, 6, 6, 6);
            handleLockedObjectsCheckBox.Name = "handleLockedObjectsCheckBox";
            handleLockedObjectsCheckBox.Size = new Size(336, 27);
            handleLockedObjectsCheckBox.TabIndex = 2;
            handleLockedObjectsCheckBox.Text = "強制轉換鎖定圖層上的物件(包括圖塊)";
            handleLockedObjectsCheckBox.UseVisualStyleBackColor = true;
=======
            this.settingsGroupBox.Controls.Add(this.handleLockedObjectsCheckBox);
            this.settingsGroupBox.Controls.Add(this.createLayerCheckBox);
            this.settingsGroupBox.Controls.Add(this.unlockTargetLayerCheckBox);
            this.settingsGroupBox.Location = new System.Drawing.Point(12, 150);
            this.settingsGroupBox.Name = "settingsGroupBox";
            this.settingsGroupBox.Size = new System.Drawing.Size(480, 150);
            this.settingsGroupBox.TabIndex = 2;
            this.settingsGroupBox.TabStop = false;
            this.settingsGroupBox.Text = "�ഫ�]�w";
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
            this.handleLockedObjectsCheckBox.Text = "�B�z��w�ϼh�W������";
            this.handleLockedObjectsCheckBox.UseVisualStyleBackColor = true;
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
            // 
            // createLayerCheckBox
            // 
            createLayerCheckBox.AutoSize = true;
            createLayerCheckBox.Checked = true;
            createLayerCheckBox.CheckState = CheckState.Checked;
            createLayerCheckBox.Location = new Point(28, 96);
            createLayerCheckBox.Margin = new Padding(6, 6, 6, 6);
            createLayerCheckBox.Name = "createLayerCheckBox";
            createLayerCheckBox.Size = new Size(216, 27);
            createLayerCheckBox.TabIndex = 1;
            createLayerCheckBox.Text = "自動創建不存在的圖層";
            createLayerCheckBox.UseVisualStyleBackColor = true;
            // 
            // unlockTargetLayerCheckBox
            // 
            unlockTargetLayerCheckBox.AutoSize = true;
            unlockTargetLayerCheckBox.Checked = true;
            unlockTargetLayerCheckBox.CheckState = CheckState.Checked;
            unlockTargetLayerCheckBox.Location = new Point(28, 48);
            unlockTargetLayerCheckBox.Margin = new Padding(6, 6, 6, 6);
            unlockTargetLayerCheckBox.Name = "unlockTargetLayerCheckBox";
            unlockTargetLayerCheckBox.Size = new Size(180, 27);
            unlockTargetLayerCheckBox.TabIndex = 0;
            unlockTargetLayerCheckBox.Text = "自動解鎖目標圖層";
            unlockTargetLayerCheckBox.UseVisualStyleBackColor = true;
            // 
            // processAnnotationsCheckBox
            // 
            processAnnotationsCheckBox.AutoSize = true;
            processAnnotationsCheckBox.Checked = true;
            processAnnotationsCheckBox.CheckState = CheckState.Checked;
            processAnnotationsCheckBox.Location = new Point(28, 192);
            processAnnotationsCheckBox.Margin = new Padding(6, 6, 6, 6);
            processAnnotationsCheckBox.Name = "processAnnotationsCheckBox";
            processAnnotationsCheckBox.Size = new Size(420, 27);
            processAnnotationsCheckBox.TabIndex = 3;
            processAnnotationsCheckBox.Text = "處理鎖定圖層的標註和動態圖塊 (自動解鎖圖層進行轉換)";
            processAnnotationsCheckBox.UseVisualStyleBackColor = true;
            // 
            // methodsGroupBox
            // 
            methodsGroupBox.Controls.Add(autoSelectMethodCheckBox);
            methodsGroupBox.Controls.Add(blockExplodeMethodCheckBox);
            methodsGroupBox.Controls.Add(referenceEditMethodCheckBox);
            methodsGroupBox.Controls.Add(blockEditorMethodCheckBox);
            methodsGroupBox.Location = new Point(22, 567);
            methodsGroupBox.Margin = new Padding(6, 6, 6, 6);
            methodsGroupBox.Name = "methodsGroupBox";
            methodsGroupBox.Padding = new Padding(6, 6, 6, 6);
            methodsGroupBox.Size = new Size(880, 288);
            methodsGroupBox.TabIndex = 3;
            methodsGroupBox.TabStop = false;
            methodsGroupBox.Text = "處理方法選擇";
            // 
            // autoSelectMethodCheckBox
            // 
            autoSelectMethodCheckBox.AutoSize = true;
            autoSelectMethodCheckBox.Checked = true;
            autoSelectMethodCheckBox.CheckState = CheckState.Checked;
            autoSelectMethodCheckBox.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold);
            autoSelectMethodCheckBox.ForeColor = Color.DarkGreen;
            autoSelectMethodCheckBox.Location = new Point(28, 48);
            autoSelectMethodCheckBox.Margin = new Padding(6, 6, 6, 6);
            autoSelectMethodCheckBox.Name = "autoSelectMethodCheckBox";
            autoSelectMethodCheckBox.Size = new Size(380, 26);
            autoSelectMethodCheckBox.TabIndex = 0;
            autoSelectMethodCheckBox.Text = "智能自動選擇 (推薦) - 失敗時自動嘗試其他方法";
            autoSelectMethodCheckBox.UseVisualStyleBackColor = true;
            autoSelectMethodCheckBox.CheckedChanged += autoSelectMethodCheckBox_CheckedChanged;
            // 
            // blockExplodeMethodCheckBox
            // 
            blockExplodeMethodCheckBox.AutoSize = true;
            blockExplodeMethodCheckBox.Checked = true;
            blockExplodeMethodCheckBox.CheckState = CheckState.Checked;
            blockExplodeMethodCheckBox.Location = new Point(64, 96);
            blockExplodeMethodCheckBox.Margin = new Padding(6, 6, 6, 6);
            blockExplodeMethodCheckBox.Name = "blockExplodeMethodCheckBox";
            blockExplodeMethodCheckBox.Size = new Size(175, 27);
            blockExplodeMethodCheckBox.TabIndex = 1;
            blockExplodeMethodCheckBox.Text = "分解重組法 (安全可靠)";
            blockExplodeMethodCheckBox.UseVisualStyleBackColor = true;
            // 
            // referenceEditMethodCheckBox
            // 
            referenceEditMethodCheckBox.AutoSize = true;
            referenceEditMethodCheckBox.Checked = true;
            referenceEditMethodCheckBox.CheckState = CheckState.Checked;
            referenceEditMethodCheckBox.Location = new Point(64, 144);
            referenceEditMethodCheckBox.Margin = new Padding(6, 6, 6, 6);
            referenceEditMethodCheckBox.Name = "referenceEditMethodCheckBox";
            referenceEditMethodCheckBox.Size = new Size(230, 27);
            referenceEditMethodCheckBox.TabIndex = 2;
            referenceEditMethodCheckBox.Text = "現地編輯法 (Reference Edit)";
            referenceEditMethodCheckBox.UseVisualStyleBackColor = true;
            // 
            // blockEditorMethodCheckBox
            // 
            blockEditorMethodCheckBox.AutoSize = true;
            blockEditorMethodCheckBox.Location = new Point(64, 192);
            blockEditorMethodCheckBox.Margin = new Padding(6, 6, 6, 6);
            blockEditorMethodCheckBox.Name = "blockEditorMethodCheckBox";
            blockEditorMethodCheckBox.Size = new Size(260, 27);
            blockEditorMethodCheckBox.TabIndex = 3;
            blockEditorMethodCheckBox.Text = "圖塊編輯器法 (終極方案，需要手動)";
            blockEditorMethodCheckBox.UseVisualStyleBackColor = true;
            blockEditorMethodCheckBox.CheckedChanged += blockEditorMethodCheckBox_CheckedChanged;
            // 
            // statusLabel
            // 
<<<<<<< HEAD
            statusLabel.AutoSize = true;
            statusLabel.ForeColor = Color.Gray;
            statusLabel.Location = new Point(22, 892);
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
            convertButton.Location = new Point(605, 950);
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
            cancelButton.Location = new Point(770, 950);
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
            ClientSize = new Size(953, 1065);
            Controls.Add(cancelButton);
            Controls.Add(convertButton);
            Controls.Add(statusLabel);
            Controls.Add(methodsGroupBox);
            Controls.Add(settingsGroupBox);
            Controls.Add(layerGroupBox);
            Controls.Add(infoLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(6, 6, 6, 6);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LayerManagerForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AutoCAD 圖層管理器 - 智能多方法轉換";
=======
            this.statusLabel.AutoSize = true;
            this.statusLabel.ForeColor = System.Drawing.Color.Gray;
            this.statusLabel.Location = new System.Drawing.Point(12, 320);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(53, 12);
            this.statusLabel.TabIndex = 3;
            this.statusLabel.Text = "�ǳƴN��";
            // 
            // convertButton
            // 
            this.convertButton.BackColor = System.Drawing.Color.LightGreen;
            this.convertButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.convertButton.Location = new System.Drawing.Point(330, 360);
            this.convertButton.Name = "convertButton";
            this.convertButton.Size = new System.Drawing.Size(80, 35);
            this.convertButton.TabIndex = 4;
            this.convertButton.Text = "�}�l�ഫ";
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
            this.cancelButton.Text = "����";
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
            this.Text = "AutoCAD �ϼh�޲z��";
            this.layerGroupBox.ResumeLayout(false);
            this.settingsGroupBox.ResumeLayout(false);
            this.settingsGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80

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
<<<<<<< HEAD
        private System.Windows.Forms.CheckBox processAnnotationsCheckBox;
        private System.Windows.Forms.GroupBox methodsGroupBox;
        private System.Windows.Forms.CheckBox blockExplodeMethodCheckBox;
        private System.Windows.Forms.CheckBox referenceEditMethodCheckBox;
        private System.Windows.Forms.CheckBox blockEditorMethodCheckBox;
        private System.Windows.Forms.CheckBox autoSelectMethodCheckBox;
=======
>>>>>>> 0ed8e2a0214d8423838f43395ca0b7017c986a80
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Button convertButton;
        private System.Windows.Forms.Button cancelButton;
    }
}