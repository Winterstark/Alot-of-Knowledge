namespace AlotGUI
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
            this.lblStatus = new System.Windows.Forms.Label();
            this.timerFeedback = new System.Windows.Forms.Timer(this.components);
            this.timerDoubleClick = new System.Windows.Forms.Timer(this.components);
            this.buttPlay1 = new System.Windows.Forms.Button();
            this.buttPlay2 = new System.Windows.Forms.Button();
            this.buttPlay3 = new System.Windows.Forms.Button();
            this.buttPlay4 = new System.Windows.Forms.Button();
            this.buttPlay5 = new System.Windows.Forms.Button();
            this.buttPlay6 = new System.Windows.Forms.Button();
            this.timerAudioDoubleClick = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 9);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 13);
            this.lblStatus.TabIndex = 0;
            // 
            // timerFeedback
            // 
            this.timerFeedback.Interval = 500;
            this.timerFeedback.Tick += new System.EventHandler(this.timerFeedback_Tick);
            // 
            // timerDoubleClick
            // 
            this.timerDoubleClick.Tick += new System.EventHandler(this.timerDoubleClick_Tick);
            // 
            // buttPlay1
            // 
            this.buttPlay1.Location = new System.Drawing.Point(325, 35);
            this.buttPlay1.Name = "buttPlay1";
            this.buttPlay1.Size = new System.Drawing.Size(77, 59);
            this.buttPlay1.TabIndex = 1;
            this.buttPlay1.Tag = "0";
            this.buttPlay1.UseVisualStyleBackColor = true;
            this.buttPlay1.Click += new System.EventHandler(this.buttPlayPause_Click);
            // 
            // buttPlay2
            // 
            this.buttPlay2.Location = new System.Drawing.Point(408, 35);
            this.buttPlay2.Name = "buttPlay2";
            this.buttPlay2.Size = new System.Drawing.Size(77, 59);
            this.buttPlay2.TabIndex = 2;
            this.buttPlay2.Tag = "1";
            this.buttPlay2.UseVisualStyleBackColor = true;
            this.buttPlay2.Click += new System.EventHandler(this.buttPlayPause_Click);
            // 
            // buttPlay3
            // 
            this.buttPlay3.Location = new System.Drawing.Point(325, 100);
            this.buttPlay3.Name = "buttPlay3";
            this.buttPlay3.Size = new System.Drawing.Size(77, 59);
            this.buttPlay3.TabIndex = 3;
            this.buttPlay3.Tag = "2";
            this.buttPlay3.UseVisualStyleBackColor = true;
            this.buttPlay3.Click += new System.EventHandler(this.buttPlayPause_Click);
            // 
            // buttPlay4
            // 
            this.buttPlay4.Location = new System.Drawing.Point(408, 100);
            this.buttPlay4.Name = "buttPlay4";
            this.buttPlay4.Size = new System.Drawing.Size(77, 59);
            this.buttPlay4.TabIndex = 4;
            this.buttPlay4.Tag = "3";
            this.buttPlay4.UseVisualStyleBackColor = true;
            this.buttPlay4.Click += new System.EventHandler(this.buttPlayPause_Click);
            // 
            // buttPlay5
            // 
            this.buttPlay5.Location = new System.Drawing.Point(325, 165);
            this.buttPlay5.Name = "buttPlay5";
            this.buttPlay5.Size = new System.Drawing.Size(77, 59);
            this.buttPlay5.TabIndex = 5;
            this.buttPlay5.Tag = "4";
            this.buttPlay5.UseVisualStyleBackColor = true;
            this.buttPlay5.Click += new System.EventHandler(this.buttPlayPause_Click);
            // 
            // buttPlay6
            // 
            this.buttPlay6.Location = new System.Drawing.Point(408, 165);
            this.buttPlay6.Name = "buttPlay6";
            this.buttPlay6.Size = new System.Drawing.Size(77, 59);
            this.buttPlay6.TabIndex = 6;
            this.buttPlay6.Tag = "5";
            this.buttPlay6.UseVisualStyleBackColor = true;
            this.buttPlay6.Click += new System.EventHandler(this.buttPlayPause_Click);
            // 
            // timerAudioDoubleClick
            // 
            this.timerAudioDoubleClick.Tick += new System.EventHandler(this.timerAudioDoubleClick_Tick);
            // 
            // formMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.ClientSize = new System.Drawing.Size(604, 534);
            this.Controls.Add(this.buttPlay6);
            this.Controls.Add(this.buttPlay5);
            this.Controls.Add(this.buttPlay4);
            this.Controls.Add(this.buttPlay3);
            this.Controls.Add(this.buttPlay2);
            this.Controls.Add(this.buttPlay1);
            this.Controls.Add(this.lblStatus);
            this.DoubleBuffered = true;
            this.Name = "formMain";
            this.Text = "AlotGUI";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.formMain_FormClosing);
            this.Load += new System.EventHandler(this.formMain_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.formMain_KeyDown);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.formMain_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.formMain_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.formMain_MouseUp);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Timer timerFeedback;
        private System.Windows.Forms.Timer timerDoubleClick;
        private System.Windows.Forms.Button buttPlay1;
        private System.Windows.Forms.Button buttPlay2;
        private System.Windows.Forms.Button buttPlay3;
        private System.Windows.Forms.Button buttPlay4;
        private System.Windows.Forms.Button buttPlay5;
        private System.Windows.Forms.Button buttPlay6;
        private System.Windows.Forms.Timer timerAudioDoubleClick;
    }
}

