using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace AlotGUI
{
    public partial class formMain : Form
    {
        const string IMG_DIR = @"C:\dev\scripts\Alot of Knowledge\dat knowledge\!IMAGES"; //top-level directory for images


        Bitmap mosaic = null;
        string[] imgs, multipleChoiceImages;
        int imgIndex, correctAnswer;


        Bitmap combineImages(string[] files)
        {
            Image[] visuals = new Bitmap[files.Length];
            for (int i = 0; i < files.Length; i++)
                visuals[i] = Image.FromFile(files[i]);

            Graphics gfx;
            Bitmap visMosaic = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
            int w = this.ClientSize.Width / 2;
            int h = this.ClientSize.Height / 3;
            int ind = 0;

            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 2; x++)
                {
                    if (ind == visuals.Length)
                        break;

                    visMosaic.SetResolution(visuals[ind].HorizontalResolution, visuals[ind].VerticalResolution);
                    gfx = Graphics.FromImage(visMosaic);

                    int resizedW, resizedH;
                    if (visuals[ind].Width > visuals[ind].Height)
                    {
                        resizedW = w;
                        resizedH = (int)((float)visuals[ind].Height / visuals[ind].Width * w);
                    }
                    else
                    {
                        resizedW = (int)((float)visuals[ind].Width / visuals[ind].Height * h);
                        resizedH = h;
                    }

                    gfx.DrawImage(visuals[ind], x * w + (w - resizedW) / 2, y * h + (h - resizedH) / 2, resizedW, resizedH);

                    ind++;
                }

            foreach (Image vis in visuals)
                vis.Dispose();

            //write labels
            visMosaic.SetResolution(250, 250);

            gfx = Graphics.FromImage(visMosaic);
            SolidBrush blackBrush = new SolidBrush(Color.Black);
            SolidBrush whiteBrush = new SolidBrush(Color.White);
            Font backgroundFont = new Font(SystemFonts.DefaultFont, FontStyle.Bold);
            ind = 1;

            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 2; x++, ind++)
                {
                    gfx.DrawString(ind.ToString(), backgroundFont, whiteBrush, x * w, y * h);
                    gfx.DrawString(ind.ToString(), SystemFonts.DefaultFont, blackBrush, x * w + 1, y * h + 1);
                }

            return visMosaic;
        }


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

            //check if this is a "/choose" question type
            if (args.Length == 4 && args[2] == "/choose")
            {
                correctAnswer = int.Parse(args[3]) - 1;

                //find 5 more images (select the ones closest to the correct one)
                multipleChoiceImages = new string[6];
                int i = 0;
                string folder = Path.GetDirectoryName(imgs[0]), prevFolder = folder;

                if (imgs.Length > 1)
                {
                    //if there are multiple correct images choose a random one to display
                    imgs[0] = imgs[new Random((int)DateTime.Now.Ticks).Next(imgs.Length)];

                    //and don't use the other ones as alternatives, so move to the parent folder
                    prevFolder = folder;
                    folder = Directory.GetParent(folder).FullName;
                }

                while (i < 5 && folder.Contains(IMG_DIR)) //don't search beyond the top-most images directory
                {
                    string[] files = Directory.GetFiles(folder);

                    for (int j = 0; j < files.Length && i < 5; j++)
                        if (files[j] != imgs[0])
                            multipleChoiceImages[i++] = files[j];

                    if (i < 5) //check subfolders
                        foreach (string dir in Directory.GetDirectories(folder))
                            if (dir != prevFolder)
                            {
                                files = Directory.GetFiles(dir);

                                for (int j = 0; j < files.Length && i < 5; j++, i++)
                                    multipleChoiceImages[i] = files[j];

                                if (i == 5)
                                    break;
                            }

                    prevFolder = folder;
                    folder = Directory.GetParent(folder).FullName;
                }

                //insert correct image
                if (correctAnswer != 5)
                    multipleChoiceImages[5] = multipleChoiceImages[correctAnswer];

                multipleChoiceImages[correctAnswer] = imgs[0];

                //generate composite image
                mosaic = combineImages(multipleChoiceImages);
            }

            if (mosaic == null)
                this.BackgroundImage = Image.FromFile(imgs[imgIndex]);
            else
                this.BackgroundImage = mosaic;
        }

        private void formMain_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    if (mosaic == null)
                    {
                        imgIndex = imgIndex == 0 ? imgs.Length - 1 : imgIndex - 1;
                        this.BackgroundImage = Image.FromFile(imgs[imgIndex]);
                    }
                    break;
                case Keys.Right:
                    if (mosaic == null)
                    {
                        imgIndex = imgIndex == imgs.Length - 1 ? 0 : imgIndex + 1;
                        this.BackgroundImage = Image.FromFile(imgs[imgIndex]);
                    }
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
