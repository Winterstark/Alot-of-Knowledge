namespace GeoDataExtractor
{
    partial class formMain
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
            this.lblDragRecipient = new System.Windows.Forms.Label();
            this.picDisplay = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.chklistShapes = new System.Windows.Forms.CheckedListBox();
            this.buttSave = new System.Windows.Forms.Button();
            this.buttClose = new System.Windows.Forms.Button();
            this.buttCheckAll = new System.Windows.Forms.Button();
            this.buttCheckNone = new System.Windows.Forms.Button();
            this.saveDialog = new System.Windows.Forms.SaveFileDialog();
            this.timerRefreshDisplay = new System.Windows.Forms.Timer(this.components);
            this.chkShowWorldCoastline = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.picDisplay)).BeginInit();
            this.SuspendLayout();
            // 
            // lblDragRecipient
            // 
            this.lblDragRecipient.AllowDrop = true;
            this.lblDragRecipient.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDragRecipient.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblDragRecipient.Location = new System.Drawing.Point(1004, 9);
            this.lblDragRecipient.Name = "lblDragRecipient";
            this.lblDragRecipient.Size = new System.Drawing.Size(982, 436);
            this.lblDragRecipient.TabIndex = 0;
            this.lblDragRecipient.Text = "Drag Shapefile (.shp) here";
            this.lblDragRecipient.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblDragRecipient.DragDrop += new System.Windows.Forms.DragEventHandler(this.lblDragRecipient_DragDrop);
            this.lblDragRecipient.DragEnter += new System.Windows.Forms.DragEventHandler(this.lblDragRecipient_DragEnter);
            this.lblDragRecipient.DragLeave += new System.EventHandler(this.lblDragRecipient_DragLeave);
            // 
            // picDisplay
            // 
            this.picDisplay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picDisplay.Location = new System.Drawing.Point(272, 25);
            this.picDisplay.Name = "picDisplay";
            this.picDisplay.Size = new System.Drawing.Size(720, 360);
            this.picDisplay.TabIndex = 1;
            this.picDisplay.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Shapes:";
            // 
            // chklistShapes
            // 
            this.chklistShapes.CheckOnClick = true;
            this.chklistShapes.FormattingEnabled = true;
            this.chklistShapes.Location = new System.Drawing.Point(12, 25);
            this.chklistShapes.Name = "chklistShapes";
            this.chklistShapes.Size = new System.Drawing.Size(254, 289);
            this.chklistShapes.TabIndex = 4;
            this.chklistShapes.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.chklistShapes_ItemCheck);
            // 
            // buttSave
            // 
            this.buttSave.Enabled = false;
            this.buttSave.Location = new System.Drawing.Point(15, 362);
            this.buttSave.Name = "buttSave";
            this.buttSave.Size = new System.Drawing.Size(251, 23);
            this.buttSave.TabIndex = 5;
            this.buttSave.Text = "Save checked shapes...";
            this.buttSave.UseVisualStyleBackColor = true;
            this.buttSave.Click += new System.EventHandler(this.buttSave_Click);
            // 
            // buttClose
            // 
            this.buttClose.Location = new System.Drawing.Point(12, 404);
            this.buttClose.Name = "buttClose";
            this.buttClose.Size = new System.Drawing.Size(254, 23);
            this.buttClose.TabIndex = 7;
            this.buttClose.Text = "Close file";
            this.buttClose.UseVisualStyleBackColor = true;
            this.buttClose.Click += new System.EventHandler(this.buttClose_Click);
            // 
            // buttCheckAll
            // 
            this.buttCheckAll.Location = new System.Drawing.Point(12, 320);
            this.buttCheckAll.Name = "buttCheckAll";
            this.buttCheckAll.Size = new System.Drawing.Size(104, 23);
            this.buttCheckAll.TabIndex = 8;
            this.buttCheckAll.Text = "Check all";
            this.buttCheckAll.UseVisualStyleBackColor = true;
            this.buttCheckAll.Click += new System.EventHandler(this.buttCheckAll_Click);
            // 
            // buttCheckNone
            // 
            this.buttCheckNone.Enabled = false;
            this.buttCheckNone.Location = new System.Drawing.Point(162, 320);
            this.buttCheckNone.Name = "buttCheckNone";
            this.buttCheckNone.Size = new System.Drawing.Size(104, 23);
            this.buttCheckNone.TabIndex = 9;
            this.buttCheckNone.Text = "Check none";
            this.buttCheckNone.UseVisualStyleBackColor = true;
            this.buttCheckNone.Click += new System.EventHandler(this.buttCheckNone_Click);
            // 
            // saveDialog
            // 
            this.saveDialog.Filter = "Text files|*.txt";
            this.saveDialog.OverwritePrompt = false;
            // 
            // timerRefreshDisplay
            // 
            this.timerRefreshDisplay.Interval = 50;
            this.timerRefreshDisplay.Tick += new System.EventHandler(this.timerRefreshDisplay_Tick);
            // 
            // chkShowWorldCoastline
            // 
            this.chkShowWorldCoastline.AutoSize = true;
            this.chkShowWorldCoastline.Checked = true;
            this.chkShowWorldCoastline.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowWorldCoastline.Location = new System.Drawing.Point(866, 393);
            this.chkShowWorldCoastline.Name = "chkShowWorldCoastline";
            this.chkShowWorldCoastline.Size = new System.Drawing.Size(126, 17);
            this.chkShowWorldCoastline.TabIndex = 10;
            this.chkShowWorldCoastline.Text = "Show world coastline";
            this.chkShowWorldCoastline.UseVisualStyleBackColor = true;
            this.chkShowWorldCoastline.CheckedChanged += new System.EventHandler(this.chkShowWorldCoastline_CheckedChanged);
            // 
            // formMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1006, 449);
            this.Controls.Add(this.lblDragRecipient);
            this.Controls.Add(this.chkShowWorldCoastline);
            this.Controls.Add(this.buttCheckNone);
            this.Controls.Add(this.buttCheckAll);
            this.Controls.Add(this.buttClose);
            this.Controls.Add(this.buttSave);
            this.Controls.Add(this.chklistShapes);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.picDisplay);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MinimizeBox = false;
            this.Name = "formMain";
            this.Text = "GeoDataExtractor";
            this.Load += new System.EventHandler(this.formMain_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picDisplay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblDragRecipient;
        private System.Windows.Forms.PictureBox picDisplay;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckedListBox chklistShapes;
        private System.Windows.Forms.Button buttSave;
        private System.Windows.Forms.Button buttClose;
        private System.Windows.Forms.Button buttCheckAll;
        private System.Windows.Forms.Button buttCheckNone;
        private System.Windows.Forms.SaveFileDialog saveDialog;
        private System.Windows.Forms.Timer timerRefreshDisplay;
        private System.Windows.Forms.CheckBox chkShowWorldCoastline;
    }
}

