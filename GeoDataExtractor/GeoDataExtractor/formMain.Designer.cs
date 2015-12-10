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
            this.lblShapesCount = new System.Windows.Forms.Label();
            this.chklistShapes = new System.Windows.Forms.CheckedListBox();
            this.buttSave = new System.Windows.Forms.Button();
            this.buttClose = new System.Windows.Forms.Button();
            this.buttCheckAll = new System.Windows.Forms.Button();
            this.buttCheckNone = new System.Windows.Forms.Button();
            this.saveDialog = new System.Windows.Forms.SaveFileDialog();
            this.timerRefreshDisplay = new System.Windows.Forms.Timer(this.components);
            this.chkShowWorldCoastline = new System.Windows.Forms.CheckBox();
            this.numDefaultColor = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.chkSaveCountry = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.picDisplay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDefaultColor)).BeginInit();
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
            // lblShapesCount
            // 
            this.lblShapesCount.AutoSize = true;
            this.lblShapesCount.Location = new System.Drawing.Point(12, 9);
            this.lblShapesCount.Name = "lblShapesCount";
            this.lblShapesCount.Size = new System.Drawing.Size(46, 13);
            this.lblShapesCount.TabIndex = 3;
            this.lblShapesCount.Text = "Shapes:";
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
            this.chkShowWorldCoastline.Location = new System.Drawing.Point(866, 391);
            this.chkShowWorldCoastline.Name = "chkShowWorldCoastline";
            this.chkShowWorldCoastline.Size = new System.Drawing.Size(126, 17);
            this.chkShowWorldCoastline.TabIndex = 10;
            this.chkShowWorldCoastline.Text = "Show world coastline";
            this.chkShowWorldCoastline.UseVisualStyleBackColor = true;
            this.chkShowWorldCoastline.CheckedChanged += new System.EventHandler(this.chkShowWorldCoastline_CheckedChanged);
            // 
            // numDefaultColor
            // 
            this.numDefaultColor.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numDefaultColor.Location = new System.Drawing.Point(406, 391);
            this.numDefaultColor.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numDefaultColor.Name = "numDefaultColor";
            this.numDefaultColor.Size = new System.Drawing.Size(98, 20);
            this.numDefaultColor.TabIndex = 11;
            this.numDefaultColor.ValueChanged += new System.EventHandler(this.numDefaultColor_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(298, 393);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Default shape color:";
            // 
            // chkSaveCountry
            // 
            this.chkSaveCountry.AutoSize = true;
            this.chkSaveCountry.Location = new System.Drawing.Point(598, 391);
            this.chkSaveCountry.Name = "chkSaveCountry";
            this.chkSaveCountry.Size = new System.Drawing.Size(128, 17);
            this.chkSaveCountry.TabIndex = 13;
            this.chkSaveCountry.Text = "Save shape\'s country";
            this.chkSaveCountry.UseVisualStyleBackColor = true;
            // 
            // formMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1006, 449);
            this.Controls.Add(this.lblDragRecipient);
            this.Controls.Add(this.chkSaveCountry);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.numDefaultColor);
            this.Controls.Add(this.chkShowWorldCoastline);
            this.Controls.Add(this.buttCheckNone);
            this.Controls.Add(this.buttCheckAll);
            this.Controls.Add(this.buttClose);
            this.Controls.Add(this.buttSave);
            this.Controls.Add(this.chklistShapes);
            this.Controls.Add(this.lblShapesCount);
            this.Controls.Add(this.picDisplay);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "formMain";
            this.Text = "GeoDataExtractor";
            this.Load += new System.EventHandler(this.formMain_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picDisplay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDefaultColor)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblDragRecipient;
        private System.Windows.Forms.PictureBox picDisplay;
        private System.Windows.Forms.Label lblShapesCount;
        private System.Windows.Forms.CheckedListBox chklistShapes;
        private System.Windows.Forms.Button buttSave;
        private System.Windows.Forms.Button buttClose;
        private System.Windows.Forms.Button buttCheckAll;
        private System.Windows.Forms.Button buttCheckNone;
        private System.Windows.Forms.SaveFileDialog saveDialog;
        private System.Windows.Forms.Timer timerRefreshDisplay;
        private System.Windows.Forms.CheckBox chkShowWorldCoastline;
        private System.Windows.Forms.NumericUpDown numDefaultColor;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkSaveCountry;
    }
}

