using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Collections;
using System.Collections.Generic;

namespace AlotGUI
{
    public partial class formMain : Form
    {
        const string IMG_DIR = @"C:\dev\scripts\Alot of Knowledge\dat knowledge\!IMAGES"; //top-level directory for images
        const string LOGO_PATH = @"C:\dev\scripts\Alot of Knowledge\alot.png";
        const string TIMELINE_PATH = @"C:\dev\scripts\Alot of Knowledge\timeline.txt";
        readonly string[] MONTHS = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        enum DisplayMode {Logo, Image, Mosaic, Timeline};
        DisplayMode mode;

        BackgroundWorker pipeWorker;
        delegate void SetTextCallback(string text);

        Image logo;
        string[] imgs, multipleChoiceImages;
        int imgIndex, correctAnswer;

        Font labelFont, labelMonthFont;
        List<Tuple<string, float, float>> timeline;
        int[] reservedLabelRows, reservedPeriodRows; //used to prevent overlapping labels/periods
        string questionEvent;
        float timelineLB, timelineUB, prevTimelineLB; //lower and upper date bounds of the timeline
        float timelineNotchPeriod;
        int timelinePenWidth, timelineNotchWidth, timelineLabelFrequency, timelineMonthNotchFrequency, timelineMonthLabelFrequency;
        int prevMX, prevMY;
        bool mouseDown, concealEventName;


        void pipeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var server = new NamedPipeServerStream("alotPipe");

            updateStatus("Waiting for connection from alot.py...");
            server.WaitForConnection();

            updateStatus("");
            var br = new BinaryReader(server);
            var bw = new BinaryWriter(server);

            while (true)
            {
                try
                {
                    var len = (int)br.ReadUInt32();
                    var msg = new string(br.ReadChars(len));

                    processMsg(msg);
                }
                catch (EndOfStreamException)
                {
                    break;
                }
            }
            
            server.Close();
            server.Dispose();
        }

        void processMsg(string msg)
        {
            if (msg == "logo")
            {
                this.BackgroundImage = logo;
                mode = DisplayMode.Logo;
            }
            else if (msg.Contains("timeline"))
            {
                if (msg.Length > 8)
                {
                    questionEvent = msg.Substring(9);

                    if (questionEvent.Contains(" ?"))
                    {
                        concealEventName = true;
                        questionEvent = questionEvent.Replace(" ?", "");
                    }
                    else
                        concealEventName = false;
                    float questionEventDate = 0;

                    foreach (var entry in timeline)
                        if (entry.Item1 == questionEvent)
                        {
                            if (entry.Item3 == 0)
                            {
                                questionEventDate = entry.Item2;
                                if (questionEventDate % 1 > 0.01)
                                    timelinePenWidth = 8; //if the date specifies the month then zoom in enough to show the month labels
                                else
                                {
                                    //otherwise, zoom in more for more recent events
                                    if (questionEventDate < -1000)
                                        timelinePenWidth = 1;
                                    else if (questionEventDate < 0)
                                        timelinePenWidth = 2;
                                    else if (questionEventDate < 1000)
                                        timelinePenWidth = 3;
                                    else if (questionEventDate < 1400)
                                        timelinePenWidth = 4;
                                    else if (questionEventDate < 1700)
                                        timelinePenWidth = 5;
                                    else if (questionEventDate < 1900)
                                        timelinePenWidth = 6;
                                    else
                                        timelinePenWidth = 7;
                                }
                            }
                            else
                            {
                                questionEventDate = (entry.Item2 + entry.Item3) / 2;

                                //zoom in enough to show the whole period
                                for (timelinePenWidth = 9; timelinePenWidth >= 1; timelinePenWidth--)
                                {
                                    setTimelineScale();
                                    if (questionEventDate - convertPixelsToDate(this.ClientSize.Width / 2) < entry.Item2)
                                        break;
                                }
                            }
                            break;
                        }

                    if (questionEventDate == 0)
                    {
                        System.Media.SystemSounds.Beep.Play();
                        updateStatus("Could not find entry: " + questionEventDate);
                        return;
                    }
                    
                    setTimelineScale();
                    timelineLB = questionEventDate - convertPixelsToDate(this.ClientSize.Width / 2);
                }
                
                calcTimelineUB();
                mode = DisplayMode.Timeline;
                this.BackgroundImage = null;
                this.Refresh();
            }
            else
            {
                string path = msg.Substring(msg.IndexOf(' ') + 1);

                if (File.Exists(path))
                {
                    imgs = new string[1];
                    imgs[0] = path;
                }
                else if (Directory.Exists(path))
                {
                    imgs = Directory.GetFiles(path);
                    imgIndex = 0;
                }
                else
                {
                    System.Media.SystemSounds.Beep.Play();
                    updateStatus("Invalid path!");
                    return;
                }

                if (msg[0] == 'I')
                {
                    //question type: identify
                    this.BackgroundImage = Image.FromFile(imgs[imgIndex]);
                    mode = DisplayMode.Image;
                }
                else if (msg[0] == 'C')
                {
                    //question type: choose
                    correctAnswer = int.Parse(msg.Substring(1, 1)) - 1;

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

                    Queue<string> unvisitedFolders = new Queue<string>();

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
                                    unvisitedFolders.Enqueue(dir);
                                    files = Directory.GetFiles(dir);

                                    for (int j = 0; j < files.Length && i < 5; j++, i++)
                                        multipleChoiceImages[i] = files[j];

                                    if (i == 5)
                                        break;
                                }

                        prevFolder = folder;
                        folder = Directory.GetParent(folder).FullName;
                    }

                    //need more images?
                    while (i < 5 && unvisitedFolders.Count > 0)
                    {
                        string dir = unvisitedFolders.Dequeue();

                        foreach (string file in Directory.GetFiles(dir))
                            if (!arrayContainsString(multipleChoiceImages, file))
                                multipleChoiceImages[i++] = file;

                        foreach (string subDir in Directory.GetDirectories(dir))
                            unvisitedFolders.Enqueue(subDir);
                    }

                    //insert correct image
                    if (correctAnswer != 5)
                        multipleChoiceImages[5] = multipleChoiceImages[correctAnswer];

                    multipleChoiceImages[correctAnswer] = imgs[0];

                    //generate composite image
                    this.BackgroundImage = combineImages(multipleChoiceImages);
                    mode = DisplayMode.Mosaic;
                }
            }
        }

        void pipeWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Application.Exit();
        }

        void updateStatus(string status)
        {
            if (lblStatus.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(updateStatus);
                this.Invoke(d, new object[] { status });
            }
            else
                lblStatus.Text = status;
        }

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
                    if ((float)visuals[ind].Width / visuals[ind].Height > (float)w / h)
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

        bool arrayContainsString(string[] array, string s)
        {
            for (int i = 0; i < array.Length; i++)
                if (array[i] == s)
                    return true;

            return false;
        }
        
        bool dateVisibleOnScreen(float date1, float date2)
        {
            if (date2 == 0)
                return date1 >= timelineLB && date1 <= timelineUB;
            else
            {
                if (date1 < timelineLB)
                    return date2 > timelineLB;
                else
                    return date1 < timelineUB;
            }
        }
        
        void setTimelineScale()
        {
            switch (timelinePenWidth)
            {
                case 1:
                    timelineNotchPeriod = 200;
                    timelineNotchWidth = 80;
                    timelineLabelFrequency = 5;
                    timelineMonthNotchFrequency = -1;
                    timelineMonthLabelFrequency = -1;
                    break;
                case 2:
                    timelineNotchPeriod = 100;
                    timelineNotchWidth = 100;
                    timelineLabelFrequency = 2;
                    timelineMonthNotchFrequency = -1;
                    timelineMonthLabelFrequency = -1;
                    break;
                case 3:
                    timelineNotchPeriod = 20;
                    timelineNotchWidth = 40;
                    timelineLabelFrequency = 5;
                    timelineMonthNotchFrequency = -1;
                    timelineMonthLabelFrequency = -1;
                    break;
                case 4:
                    timelineNotchPeriod = 5;
                    timelineNotchWidth = 25;
                    timelineLabelFrequency = 4;
                    timelineMonthNotchFrequency = -1;
                    timelineMonthLabelFrequency = -1;
                    break;
                case 5:
                    timelineNotchPeriod = 1;
                    timelineNotchWidth = 20;
                    timelineLabelFrequency = 5;
                    timelineMonthNotchFrequency = -1;
                    timelineMonthLabelFrequency = -1;
                    break;
                case 6:
                    timelineNotchPeriod = 1;
                    timelineNotchWidth = 75;
                    timelineLabelFrequency = 1;
                    timelineMonthNotchFrequency = 6;
                    timelineMonthLabelFrequency = -1;
                    break;
                case 7:
                    timelineNotchPeriod = 1;
                    timelineNotchWidth = 200;
                    timelineLabelFrequency = 1;
                    timelineMonthNotchFrequency = 3;
                    timelineMonthLabelFrequency = 1;
                    break;
                case 8:
                    timelineNotchPeriod = 1;
                    timelineNotchWidth = 600;
                    timelineLabelFrequency = 1;
                    timelineMonthNotchFrequency = 2;
                    timelineMonthLabelFrequency = 1;
                    break;
                case 9:
                    timelineNotchPeriod = 1;
                    timelineNotchWidth = 3650;
                    timelineLabelFrequency = 1;
                    timelineMonthNotchFrequency = 1;
                    timelineMonthLabelFrequency = 1;
                    break;
            }

            labelFont = new Font(FontFamily.GenericSansSerif, 7 + timelinePenWidth);

            //calculate how many rows of labels there is room for
            int rowH = (int)this.CreateGraphics().MeasureString("A", labelFont).Height;
            int nRows = (this.ClientSize.Height / 2 - timelinePenWidth - rowH) / rowH;

            reservedLabelRows = new int[nRows];
            reservedPeriodRows = new int[(int)((float)nRows * 0.75f)];
        }

        void calcTimelineUB()
        {
            timelineUB = timelineLB + convertPixelsToDate(this.ClientSize.Width);
        }

        int convertDateToDays(int[] date)
        {
            return date[0] * 365 + date[1] * 12 + date[2];
        }

        int convertDateToPixels(float date)
        {
            if (date < timelineLB)
                return -1;
            else if (date <= timelineUB)
                return (int)((date - timelineLB) * (float)timelineNotchWidth / timelineNotchPeriod);
            else
                return this.ClientSize.Width + 1;
        }

        float convertPixelsToDate(int x)
        {
            return x * (float)timelineNotchPeriod / timelineNotchWidth;
        }
        
        float mod(float x, float m)
        {
            x %= m;
            if (x < 0)
                x += m;

            return x;
        }
        
        void drawMonthNotches(Graphics gfx, float x)
        {
            if (timelineMonthNotchFrequency != -1)
            {
                int monthInd = timelineMonthNotchFrequency;
                int monthNotchesSincePrevLabel = 0;
                float monthNotchWidth = timelineMonthNotchFrequency * (float)timelineNotchWidth / 12;

                for (float monthX = x + monthNotchWidth; monthX < x + timelineNotchWidth; monthX += monthNotchWidth, monthInd += timelineMonthNotchFrequency)
                    if (monthX < 0 || monthInd >= 12)
                        continue;
                    else if (monthNotchesSincePrevLabel == timelineMonthLabelFrequency - 1)
                    {
                        gfx.DrawLine(new Pen(Color.Black, timelinePenWidth), monthX, this.ClientSize.Height / 2 - timelinePenWidth, monthX, this.ClientSize.Height / 2 + timelinePenWidth);
                        
                        float labelX = monthX - gfx.MeasureString(MONTHS[monthInd], labelMonthFont).Width / 2;
                        if (labelX >= 0)
                            gfx.DrawString(MONTHS[monthInd], labelMonthFont, Brushes.Black, labelX, this.ClientSize.Height / 2 + 10 + timelinePenWidth);

                        monthNotchesSincePrevLabel = 0;
                    }
                    else
                    {
                        gfx.DrawLine(new Pen(Color.Black, timelinePenWidth), monthX, this.ClientSize.Height / 2 - timelinePenWidth, monthX, this.ClientSize.Height / 2 + timelinePenWidth);
                        monthNotchesSincePrevLabel++;
                    }
            }
        }


        public formMain()
        {
            InitializeComponent();
        }

        private void formMain_Load(object sender, EventArgs e)
        {
            //init variables
            mode = DisplayMode.Image;
            
            timelinePenWidth = 1;
            timelineLB = 0;

            setTimelineScale();

            labelMonthFont = new Font(FontFamily.GenericSansSerif, 7);
            questionEvent = "";

            //setup window
            StreamReader file = new StreamReader(Application.StartupPath + "\\config.txt");
            this.Left = int.Parse(file.ReadLine());
            this.Top = int.Parse(file.ReadLine());
            this.Width = int.Parse(file.ReadLine());
            this.Height = int.Parse(file.ReadLine());
            file.Close();

            logo = Bitmap.FromFile(LOGO_PATH);
            this.BackgroundImage = logo;

            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.formMain_MouseWheel);

            //init worker
            pipeWorker = new BackgroundWorker();
            pipeWorker.DoWork += new DoWorkEventHandler(pipeWorker_DoWork);
            pipeWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(pipeWorker_RunWorkerCompleted);

            pipeWorker.RunWorkerAsync();

            //load timeline data
            file = new StreamReader(TIMELINE_PATH);
            timeline = new List<Tuple<string, float, float>>();

            while (!file.EndOfStream)
            {
                string line = file.ReadLine();
                string[] parts = line.Split(new string[] { " :: " }, StringSplitOptions.RemoveEmptyEntries);
                string[] dates = parts[1].Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);

                float date1 = float.Parse(dates[0].Replace('.', ',')), date2;
                if (dates.Length == 1)
                    date2 = 0; 
                else
                    date2 = float.Parse(dates[1].Replace('.', ','));

                timeline.Add(new Tuple<string, float, float>(parts[0], date1, date2));
            }

            timeline.Sort(delegate (Tuple<string, float, float> x, Tuple<string, float, float> y)
            {
                if (x.Item2 != y.Item2)
                    return x.Item2.CompareTo(y.Item2);
                else
                    return x.Item3.CompareTo(y.Item3);
            });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (mode == DisplayMode.Timeline)
            {
                e.Graphics.DrawLine(new Pen(Color.Black, timelinePenWidth), 0, this.ClientSize.Height / 2, this.ClientSize.Width, this.ClientSize.Height / 2);

                //draw notches and date labels
                float dateOffset = mod(timelineLB, timelineNotchPeriod);
                if (dateOffset != 0)
                    dateOffset = timelineNotchPeriod - dateOffset;
                float labelDate = timelineLB + dateOffset;
                int notchesSincePrevLabel = (int)Math.Ceiling((double)mod(timelineLB, timelineNotchPeriod * timelineLabelFrequency) / timelineNotchPeriod) - 1;
                
                drawMonthNotches(e.Graphics, dateOffset * timelineNotchWidth / timelineNotchPeriod - timelineNotchWidth);

                for (float x = dateOffset * timelineNotchWidth / timelineNotchPeriod; x < this.ClientSize.Width; x += timelineNotchWidth)
                {
                    if (notchesSincePrevLabel == timelineLabelFrequency - 1)
                    {
                        e.Graphics.DrawLine(new Pen(Color.Black, timelinePenWidth), x, this.ClientSize.Height / 2 - timelinePenWidth * 2, x, this.ClientSize.Height / 2 + timelinePenWidth * 2);
                        e.Graphics.DrawString(labelDate.ToString(), labelFont, Brushes.Black, x - e.Graphics.MeasureString(labelDate.ToString(), labelFont).Width / 2, this.ClientSize.Height / 2 + 10 + timelinePenWidth);
                        notchesSincePrevLabel = 0;
                    }
                    else
                    {
                        e.Graphics.DrawLine(new Pen(Color.Black, timelinePenWidth), x, this.ClientSize.Height / 2 - timelinePenWidth, x, this.ClientSize.Height / 2 + timelinePenWidth);
                        notchesSincePrevLabel++;
                    }

                    drawMonthNotches(e.Graphics, x);

                    labelDate += timelineNotchPeriod;
                }

                //draw dates
                for (int i = 0; i < reservedLabelRows.Length; i++)
                    reservedLabelRows[i] = -100;
                for (int i = 0; i < reservedPeriodRows.Length; i++)
                    reservedPeriodRows[i] = -100;

                foreach (var entry in timeline)
                {
                    if (dateVisibleOnScreen(entry.Item2, entry.Item3))
                    {
                        if (entry.Item3 == 0)
                        {
                            Brush brush = Brushes.RoyalBlue;
                            string label = entry.Item1;
                            if (entry.Item1 == questionEvent)
                            {
                                if (concealEventName)
                                    label = "???";

                                brush = Brushes.Purple;
                            }

                            int x = convertDateToPixels(entry.Item2);
                            float dotSize = 5 + (float)timelinePenWidth;
                            e.Graphics.FillEllipse(brush, x - dotSize / 2, this.ClientSize.Height / 2 - dotSize / 2, dotSize, dotSize);
                            SizeF labelSize = e.Graphics.MeasureString(label, labelFont);

                            //locate the first available label row
                            int row = 0;
                            for (; row < reservedLabelRows.Length; row++)
                                if (reservedLabelRows[row] + 10 + timelinePenWidth <= x - labelSize.Width / 2)
                                    break;

                            if (row < reservedLabelRows.Length)
                            {
                                e.Graphics.DrawString(label, labelFont, brush, x - labelSize.Width / 2, this.ClientSize.Height / 2 - 10 - timelinePenWidth - labelSize.Height * (1 + row));
                                reservedLabelRows[row] = x + (int)(labelSize.Width / 2);
                            }
                        }
                        else
                        {
                            Brush brush = Brushes.SeaGreen;
                            Color color = Color.SeaGreen;
                            string label = entry.Item1;
                            if (entry.Item1 == questionEvent)
                            {
                                if (concealEventName)
                                    label = "???";

                                brush = Brushes.Purple;
                                color = Color.Purple;
                            }

                            int x1 = convertDateToPixels(entry.Item2), x2 = convertDateToPixels(entry.Item3);
                            SizeF labelSize = e.Graphics.MeasureString(label, labelFont);
                            int labelX = (int)((x1 + x2) / 2 - labelSize.Width / 2);

                            //locate the first available timeline row
                            int row = 0;
                            for (; row < reservedPeriodRows.Length; row++)
                                if (reservedPeriodRows[row] + 10 + timelinePenWidth <= Math.Min(x1, labelX))
                                    break;

                            if (row < reservedPeriodRows.Length)
                            {
                                int y = (int)(this.ClientSize.Height / 2 + 10 + timelinePenWidth + labelSize.Height * 1.5f * (1 + row));

                                e.Graphics.DrawLine(new Pen(color, timelinePenWidth / 2), x1, y, x2, y);
                                e.Graphics.DrawString(label, labelFont, brush, labelX, y);

                                reservedPeriodRows[row] = Math.Max(x2, labelX + (int)labelSize.Width);
                            }
                        }
                    }
                }
            }
        }

        private void formMain_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    if (mode == DisplayMode.Image)
                    {
                        imgIndex = imgIndex == 0 ? imgs.Length - 1 : imgIndex - 1;
                        this.BackgroundImage = Image.FromFile(imgs[imgIndex]);
                    }
                    break;
                case Keys.Right:
                    if (mode == DisplayMode.Image)
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

        private void formMain_MouseDown(object sender, MouseEventArgs e)
        {
            prevMX = e.X;
            prevMY = e.Y;
            prevTimelineLB = timelineLB;
            mouseDown = true;
        }

        private void formMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                if (mode == DisplayMode.Timeline)
                {
                    timelineLB = prevTimelineLB - convertPixelsToDate(e.X - prevMX);
                    calcTimelineUB();
                    this.Text = timelineLB.ToString();
                    this.Invalidate();
                }
            }
        }

        private void formMain_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        private void formMain_MouseWheel(object sender, MouseEventArgs e)
        {
            //calculate current year in the center of the (visible) timeline
            float timelineMid = timelineLB + convertPixelsToDate(this.ClientSize.Width / 2);
            
            //change scale
            timelinePenWidth = Math.Min(Math.Max(timelinePenWidth + e.Delta / 120, 1), 9);
            setTimelineScale();

            //calculate new timelineLB
            timelineLB = timelineMid - convertPixelsToDate(this.ClientSize.Width / 2);
            calcTimelineUB();

            //refresh
            this.Invalidate();
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
