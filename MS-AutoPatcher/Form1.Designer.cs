namespace MS_AutoPatcher
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.locales = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkRemovePatchAfterInstall = new System.Windows.Forms.CheckBox();
            this.chkBackupMaple = new System.Windows.Forms.CheckBox();
            this.nudFinalVersion = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.nudVersion = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.btnPathSelector = new System.Windows.Forms.Button();
            this.txtPath = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.cbProxies = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.createPatcherinfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudFinalVersion)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVersion)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Variant";
            // 
            // locales
            // 
            this.locales.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.locales.FormattingEnabled = true;
            this.locales.Location = new System.Drawing.Point(87, 19);
            this.locales.Name = "locales";
            this.locales.Size = new System.Drawing.Size(172, 21);
            this.locales.TabIndex = 1;
            this.locales.SelectedIndexChanged += new System.EventHandler(this.locales_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Current version";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cbProxies);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.chkRemovePatchAfterInstall);
            this.groupBox1.Controls.Add(this.chkBackupMaple);
            this.groupBox1.Controls.Add(this.nudFinalVersion);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.nudVersion);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.locales);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 51);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(274, 175);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Installation Info";
            // 
            // chkRemovePatchAfterInstall
            // 
            this.chkRemovePatchAfterInstall.AutoSize = true;
            this.chkRemovePatchAfterInstall.Location = new System.Drawing.Point(87, 121);
            this.chkRemovePatchAfterInstall.Name = "chkRemovePatchAfterInstall";
            this.chkRemovePatchAfterInstall.Size = new System.Drawing.Size(149, 17);
            this.chkRemovePatchAfterInstall.TabIndex = 7;
            this.chkRemovePatchAfterInstall.Text = "Remove patch after install";
            this.chkRemovePatchAfterInstall.UseVisualStyleBackColor = true;
            // 
            // chkBackupMaple
            // 
            this.chkBackupMaple.AutoSize = true;
            this.chkBackupMaple.Location = new System.Drawing.Point(87, 98);
            this.chkBackupMaple.Name = "chkBackupMaple";
            this.chkBackupMaple.Size = new System.Drawing.Size(162, 17);
            this.chkBackupMaple.TabIndex = 6;
            this.chkBackupMaple.Text = "Backup Maple prior patching";
            this.chkBackupMaple.UseVisualStyleBackColor = true;
            // 
            // nudFinalVersion
            // 
            this.nudFinalVersion.Location = new System.Drawing.Point(87, 72);
            this.nudFinalVersion.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudFinalVersion.Name = "nudFinalVersion";
            this.nudFinalVersion.Size = new System.Drawing.Size(172, 20);
            this.nudFinalVersion.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 74);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Update to";
            // 
            // nudVersion
            // 
            this.nudVersion.Location = new System.Drawing.Point(87, 46);
            this.nudVersion.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudVersion.Name = "nudVersion";
            this.nudVersion.Size = new System.Drawing.Size(172, 20);
            this.nudVersion.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(80, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "MapleStory.exe";
            // 
            // btnPathSelector
            // 
            this.btnPathSelector.Location = new System.Drawing.Point(245, 23);
            this.btnPathSelector.Name = "btnPathSelector";
            this.btnPathSelector.Size = new System.Drawing.Size(41, 23);
            this.btnPathSelector.TabIndex = 5;
            this.btnPathSelector.Text = "...";
            this.btnPathSelector.UseVisualStyleBackColor = true;
            this.btnPathSelector.Click += new System.EventHandler(this.btnPathSelector_Click);
            // 
            // txtPath
            // 
            this.txtPath.Location = new System.Drawing.Point(12, 25);
            this.txtPath.Name = "txtPath";
            this.txtPath.Size = new System.Drawing.Size(227, 20);
            this.txtPath.TabIndex = 6;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(11, 232);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(274, 23);
            this.button2.TabIndex = 7;
            this.button2.Text = "Update";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(8, 258);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(50, 13);
            this.lblStatus.TabIndex = 8;
            this.lblStatus.Text = "STATUS";
            // 
            // cbProxies
            // 
            this.cbProxies.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbProxies.FormattingEnabled = true;
            this.cbProxies.Location = new System.Drawing.Point(87, 144);
            this.cbProxies.Name = "cbProxies";
            this.cbProxies.Size = new System.Drawing.Size(172, 21);
            this.cbProxies.TabIndex = 9;
            this.cbProxies.SelectedIndexChanged += new System.EventHandler(this.cbProxies_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 147);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(78, 13);
            this.label5.TabIndex = 8;
            this.label5.Text = "Download from";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.createPatcherinfoToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(176, 26);
            // 
            // createPatcherinfoToolStripMenuItem
            // 
            this.createPatcherinfoToolStripMenuItem.Name = "createPatcherinfoToolStripMenuItem";
            this.createPatcherinfoToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.createPatcherinfoToolStripMenuItem.Text = "Create Patcher.info";
            this.createPatcherinfoToolStripMenuItem.Click += new System.EventHandler(this.createPatcherinfoToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(298, 280);
            this.ContextMenuStrip = this.contextMenuStrip1;
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.txtPath);
            this.Controls.Add(this.btnPathSelector);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.groupBox1);
            this.Name = "Form1";
            this.Text = "AutoPatcher";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.Form1_HelpRequested);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Form1_KeyPress);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudFinalVersion)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVersion)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox locales;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnPathSelector;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.NumericUpDown nudVersion;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.NumericUpDown nudFinalVersion;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chkRemovePatchAfterInstall;
        private System.Windows.Forms.CheckBox chkBackupMaple;
        private System.Windows.Forms.ComboBox cbProxies;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem createPatcherinfoToolStripMenuItem;
    }
}

