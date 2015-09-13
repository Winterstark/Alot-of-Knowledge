using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace AlotGUI
{
    public partial class formMain : Form
    {
        string[] imgs;
        int imgIndex;


        public formMain()
        {
            InitializeComponent();
        }

        private void formMain_Load(object sender, EventArgs e)
        {
            //setup window
            StreamReader file = new StreamReader(Application.StartupPath + "\\config.txt");
            this.Left = int.Parse(file.ReadLine());
            this.Top = int.Parse(file.ReadLine());
            this.Width = int.Parse(file.ReadLine());
            this.Height = int.Parse(file.ReadLine());
            file.Close();

            //load image
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length <= 1)
            {
                MessageBox.Show("No Image specified in arguments!");
                Application.Exit();
            }
            if (!File.Exists(args[1]))
            {
                if (!Directory.Exists(args[1]))
                {
                    MessageBox.Show("Image does not exist!");
                    Application.Exit();
                }
                else
                {
                    imgs = Directory.GetFiles(args[1]);
                    imgIndex = 0;
                }
            }
            else
            {
                imgs = new string[1];
                imgs[0] = args[1];
            }

            this.BackgroundImage = Image.FromFile(imgs[imgIndex]);
        }

        private void formMain_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    imgIndex = imgIndex == 0 ? imgs.Length - 1 : imgIndex - 1;
                    this.BackgroundImage = Image.FromFile(imgs[imgIndex]);
                    break;
                case Keys.Right:
                    imgIndex = imgIndex == imgs.Length - 1 ? 0 : imgIndex + 1;
                    this.BackgroundImage = Image.FromFile(imgs[imgIndex]);
                    break;
                case Keys.Escape:
                    Application.Exit();
                    break;
            }
        }

        private void formMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            StreamWriter file = new StreamWriter(Application.StartupPath + "\\config.txt");
            file.WriteLine(this.Left);
            file.WriteLine(this.Top);
            file.WriteLine(this.Width);
            file.WriteLine(this.Height);
            file.Close();
        }
    }
}
