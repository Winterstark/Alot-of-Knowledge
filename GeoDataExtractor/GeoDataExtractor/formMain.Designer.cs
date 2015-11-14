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
            this.lblDragRecipient = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblDragRecipient
            // 
            this.lblDragRecipient.AllowDrop = true;
            this.lblDragRecipient.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDragRecipient.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblDragRecipient.Location = new System.Drawing.Point(12, 9);
            this.lblDragRecipient.Name = "lblDragRecipient";
            this.lblDragRecipient.Size = new System.Drawing.Size(342, 404);
            this.lblDragRecipient.TabIndex = 0;
            this.lblDragRecipient.Text = "Drag Shapefile (.shp) here";
            this.lblDragRecipient.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblDragRecipient.DragDrop += new System.Windows.Forms.DragEventHandler(this.lblDragRecipient_DragDrop);
            this.lblDragRecipient.DragEnter += new System.Windows.Forms.DragEventHandler(this.lblDragRecipient_DragEnter);
            this.lblDragRecipient.DragLeave += new System.EventHandler(this.lblDragRecipient_DragLeave);
            // 
            // formMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(755, 422);
            this.Controls.Add(this.lblDragRecipient);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "formMain";
            this.Text = "GeoDataExtractor";
            this.Load += new System.EventHandler(this.formMain_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblDragRecipient;
    }
}

