namespace AlotWriter
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
            this.menuMain = new System.Windows.Forms.MenuStrip();
            this.menuAddClass = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuAddClassSaveClass = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAddAttribute = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSaveTo = new System.Windows.Forms.ToolStripMenuItem();
            this.saveDialog = new System.Windows.Forms.SaveFileDialog();
            this.menuAddAttributeSaveAttribute = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.menuMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuMain
            // 
            this.menuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuAddClass,
            this.menuAddAttribute,
            this.menuSaveTo});
            this.menuMain.Location = new System.Drawing.Point(0, 0);
            this.menuMain.Name = "menuMain";
            this.menuMain.Size = new System.Drawing.Size(708, 24);
            this.menuMain.TabIndex = 1;
            this.menuMain.Text = "menuStrip1";
            // 
            // menuAddClass
            // 
            this.menuAddClass.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator1,
            this.menuAddClassSaveClass});
            this.menuAddClass.Name = "menuAddClass";
            this.menuAddClass.Size = new System.Drawing.Size(67, 20);
            this.menuAddClass.Text = "Add class";
            this.menuAddClass.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuAddClass_DropDownItemClicked);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
            // 
            // menuAddClassSaveClass
            // 
            this.menuAddClassSaveClass.Name = "menuAddClassSaveClass";
            this.menuAddClassSaveClass.Size = new System.Drawing.Size(152, 22);
            this.menuAddClassSaveClass.Text = "Save class...";
            // 
            // menuAddAttribute
            // 
            this.menuAddAttribute.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator2,
            this.menuAddAttributeSaveAttribute});
            this.menuAddAttribute.Name = "menuAddAttribute";
            this.menuAddAttribute.Size = new System.Drawing.Size(88, 20);
            this.menuAddAttribute.Text = "Add attribute";
            this.menuAddAttribute.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuAddAttribute_DropDownItemClicked);
            // 
            // menuSaveTo
            // 
            this.menuSaveTo.Name = "menuSaveTo";
            this.menuSaveTo.Size = new System.Drawing.Size(56, 20);
            this.menuSaveTo.Text = "Save to";
            this.menuSaveTo.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuSaveTo_DropDownItemClicked);
            // 
            // menuAddAttributeSaveAttribute
            // 
            this.menuAddAttributeSaveAttribute.Name = "menuAddAttributeSaveAttribute";
            this.menuAddAttributeSaveAttribute.Size = new System.Drawing.Size(154, 22);
            this.menuAddAttributeSaveAttribute.Text = "Save attribute...";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(151, 6);
            // 
            // formMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(708, 388);
            this.Controls.Add(this.menuMain);
            this.MainMenuStrip = this.menuMain;
            this.Name = "formMain";
            this.Text = "AlotWriter";
            this.Load += new System.EventHandler(this.formMain_Load);
            this.menuMain.ResumeLayout(false);
            this.menuMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menuMain;
        private System.Windows.Forms.ToolStripMenuItem menuAddClass;
        private System.Windows.Forms.ToolStripMenuItem menuSaveTo;
        private System.Windows.Forms.ToolStripMenuItem menuAddClassSaveClass;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.SaveFileDialog saveDialog;
        private System.Windows.Forms.ToolStripMenuItem menuAddAttribute;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem menuAddAttributeSaveAttribute;
    }
}

