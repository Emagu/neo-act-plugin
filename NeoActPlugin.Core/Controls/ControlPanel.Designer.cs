﻿using System.Windows.Forms;

namespace NeoActPlugin.Core
{
    partial class ControlPanel
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ControlPanel));
            this.tabPageMain = new System.Windows.Forms.TabPage();
            this.label_ListEmpty = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.logBox = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonClearLog = new System.Windows.Forms.Button();
            this.checkBoxFollowLog = new System.Windows.Forms.CheckBox();
            this.checkBoxShowOverlay = new System.Windows.Forms.CheckBox();
            this.btnBoss = new System.Windows.Forms.Button();
            this.tabPageMain.SuspendLayout();
            this.flowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPageMain
            // 
            this.tabPageMain.Controls.Add(this.label_ListEmpty);
            resources.ApplyResources(this.tabPageMain, "tabPageMain");
            this.tabPageMain.Name = "tabPageMain";
            this.tabPageMain.UseVisualStyleBackColor = true;
            // 
            // label_ListEmpty
            // 
            resources.ApplyResources(this.label_ListEmpty, "label_ListEmpty");
            this.label_ListEmpty.Name = "label_ListEmpty";
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // logBox
            // 
            this.logBox.BackColor = System.Drawing.SystemColors.ControlLightLight;
            resources.ApplyResources(this.logBox, "logBox");
            this.logBox.HideSelection = false;
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            // 
            // flowLayoutPanel
            // 
            resources.ApplyResources(this.flowLayoutPanel, "flowLayoutPanel");
            this.flowLayoutPanel.Controls.Add(this.buttonClearLog);
            this.flowLayoutPanel.Controls.Add(this.checkBoxFollowLog);
            this.flowLayoutPanel.Controls.Add(this.checkBoxShowOverlay);
            this.flowLayoutPanel.Controls.Add(this.btnBoss);
            this.flowLayoutPanel.Name = "flowLayoutPanel";
            // 
            // buttonClearLog
            // 
            resources.ApplyResources(this.buttonClearLog, "buttonClearLog");
            this.buttonClearLog.Name = "buttonClearLog";
            this.buttonClearLog.UseVisualStyleBackColor = true;
            this.buttonClearLog.Click += new System.EventHandler(this.ButtonClearLog_Click);
            // 
            // checkBoxFollowLog
            // 
            resources.ApplyResources(this.checkBoxFollowLog, "checkBoxFollowLog");
            this.checkBoxFollowLog.Name = "checkBoxFollowLog";
            this.checkBoxFollowLog.UseVisualStyleBackColor = true;
            // 
            // checkBoxShowOverlay
            // 
            resources.ApplyResources(this.checkBoxShowOverlay, "checkBoxShowOverlay");
            this.checkBoxShowOverlay.Name = "checkBoxShowOverlay";
            this.checkBoxShowOverlay.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            resources.ApplyResources(this.btnBoss, "button1");
            this.btnBoss.Name = "button1";
            this.btnBoss.UseVisualStyleBackColor = true;
            // 
            // ControlPanel
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.Controls.Add(this.logBox);
            this.Controls.Add(this.flowLayoutPanel);
            this.Name = "ControlPanel";
            this.tabPageMain.ResumeLayout(false);
            this.flowLayoutPanel.ResumeLayout(false);
            this.flowLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        private System.Windows.Forms.TabPage tabPageMain;
        private System.Windows.Forms.GroupBox groupBox2;
        private Label label_ListEmpty;
        private TextBox logBox;
        private FlowLayoutPanel flowLayoutPanel;
        private Button buttonClearLog;
        private CheckBox checkBoxFollowLog;
        public CheckBox checkBoxShowOverlay;
        public Button btnBoss;
    }
}