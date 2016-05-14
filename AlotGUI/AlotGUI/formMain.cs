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
        class TimelineEvent
        {
            public static readonly int TIMELINE_IMAGE_HEIGHT = 100;

            public string Name, EntryKey;
            public float Date1, Date2;
            public Image Img;
            public bool IsPeriod;

            public TimelineEvent(string line)
            {
                string[] keyValuePair = line.Split(new string[] { " :: " }, StringSplitOptions.RemoveEmptyEntries);
                string[] values = keyValuePair[1].Split(new string[] { " // " }, StringSplitOptions.None);
                string[] dates = values[0].Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);

                Name = keyValuePair[0];
                EntryKey = values[1];

                if (Directory.Exists(values[2]))
                    values[2] = Directory.GetFiles(values[2])[0];
                if (File.Exists(values[2]))
                {
                    Image fullSizeImg = Bitmap.FromFile(values[2]);
                    Img = new Bitmap(fullSizeImg, fullSizeImg.Width * TIMELINE_IMAGE_HEIGHT / fullSizeImg.Height, TIMELINE_IMAGE_HEIGHT);
                    fullSizeImg.Dispose();
                }
                
                Date1 = float.Parse(dates[0].Replace('.', ','));
                if (dates.Length == 1)
                {
                    Date2 = 0;
                    IsPeriod = false;
                }
                else
                {
                    Date2 = float.Parse(dates[1].Replace('.', ','));
                    IsPeriod = true;
                }
            }

            public bool IsVisibleOnScreen(float timelineLB, float timelineUB)
            {
                if (Date2 == 0)
                    return Date1 >= timelineLB && Date1 <= timelineUB;
                else
                {
                    if (Date1 < timelineLB)
                        return Date2 > timelineLB;
                    else
                        return Date1 < timelineUB;
                }
            }
        }


        const string IMG_DIR = @"C:\dev\scripts\Alot of Knowledge\dat knowledge\!IMAGES"; //top-level directory for images
        const string GEO_DIR = @"C:\dev\scripts\Alot of Knowledge\dat knowledge\!GEODATA";
        const string LOGO_PATH = @"C:\dev\scripts\Alot of Knowledge\alot.png";
        const string TIMELINE_PATH = @"C:\dev\scripts\Alot of Knowledge\timeline.txt";
        readonly string[] MONTHS = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        enum DisplayMode { Logo, Image, Mosaic, Timeline, FamilyTree, Map };
        DisplayMode mode;

        BackgroundWorker pipeWorker;
        delegate void SetTextCallback(string text);

        //image globals
        Image logo;
        string[] imgs, multipleChoiceImages;
        int imgIndex, correctAnswer;

        //timeline globals
        Font labelFont, labelMonthFont;
        Point initialMousePoint, previousMousePoint;
        List<TimelineEvent> timeline;
        int[,] reservedLabelRows, reservedPeriodRows; //used to prevent overlapping labels/periods
        string questionEvent;
        float timelineLB, timelineUB, prevTimelineLB; //lower and upper date bounds of the timeline
        float timelineNotchPeriod;
        int timelinePenWidth, timelineNotchWidth, timelineLabelFrequency, timelineMonthNotchFrequency, timelineMonthLabelFrequency;
        int timelineImageHeightInRows;
        bool mouseDown, concealQuestionEvent;

        //family tree globals
        const int NODE_IMAGE_HEIGHT = 150;

        Dictionary<int, int[]> reservedNodeRows, reservedLineRows; //used to prevent overlapping nodes/lines
        Dictionary<int, string[]> reservedLineRowsBorders; //tracks to which parent a line leads
        Dictionary<string, Image> nodeImages;
        Font nodeFont;
        string[,] tree;
        string questionNode;
        int treeX, treeY;
        bool concealQuestionNode;

        //map globals
        Visualizer viz;
        Point mouseClickPoint;
        string[] questionEntities;
        string mapMouseOverRegion, selectedArea;
        int mapQType, nRemainingFeedbackTicks;
        bool isAnswerHighlighted, mapFrozen;


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
            else if (msg.Contains("map"))
                processMapMsg(msg);
            else if (msg.Contains("ftree"))
                processFamilyTreeMsg(msg);
            else if (msg.Contains("timeline"))
                processTimelineMsg(msg);
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
                    processMosaicMsg(msg);
            }
        }

        void sendFeedbackToAlot(string feedback)
        {
            try
            {
                var server = new NamedPipeServerStream("alotPipeFeedback");
                server.WaitForConnection();

                var bw = new BinaryWriter(server);
                if (feedback == "True")
                    bw.Write(true);
                else
                    bw.Write(feedback);

                server.Close();
                server.Dispose();
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error while sending feedback!" + Environment.NewLine + exc.Message + Environment.NewLine + Environment.NewLine + "Trying again...");
                sendFeedbackToAlot(feedback);
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

        bool arePointsCloseEnough(Point pt1, Point pt2)
        {
            return Math.Abs(pt1.X - pt2.X) <= SystemInformation.DoubleClickSize.Width && Math.Abs(pt1.Y - pt2.Y) <= SystemInformation.DoubleClickSize.Height;
        }

        #region Mosaic
        void processMosaicMsg(string msg)
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
        #endregion

        #region Timeline
        void loadTimelineData()
        {
            StreamReader file = new StreamReader(TIMELINE_PATH);
            timeline = new List<TimelineEvent>();

            while (!file.EndOfStream)
                timeline.Add(new TimelineEvent(file.ReadLine()));

            timeline.Sort(delegate (TimelineEvent x, TimelineEvent y)
            {
                if (x.Date1 != y.Date1)
                    return x.Date1.CompareTo(y.Date1);
                else
                    return x.Date2.CompareTo(y.Date2);
            });
        }

        void processTimelineMsg(string msg)
        {
            if (msg.Length > 8)
            {
                questionEvent = msg.Substring(9);

                if (questionEvent.Contains(" ?"))
                {
                    concealQuestionEvent = true;
                    questionEvent = questionEvent.Replace(" ?", "");
                }
                else
                    concealQuestionEvent = false;
                float questionEventDate = 0;

                foreach (var entry in timeline)
                    if (entry.Name == questionEvent)
                    {
                        if (!entry.IsPeriod)
                        {
                            questionEventDate = entry.Date1;
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
                            questionEventDate = (entry.Date1 + entry.Date2) / 2;

                            //zoom in enough to show the whole period
                            for (timelinePenWidth = 9; timelinePenWidth >= 1; timelinePenWidth--)
                            {
                                setTimelineScale();
                                if (questionEventDate - convertPixelsToDate(this.ClientSize.Width / 2) < entry.Date1)
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
            nodeFont = new Font(FontFamily.GenericSansSerif, 14);

            //calculate how many rows of labels there is room for
            int rowH = (int)this.CreateGraphics().MeasureString("A", labelFont).Height;
            int nRows = (this.ClientSize.Height / 2 - timelinePenWidth - rowH) / rowH;

            reservedLabelRows = new int[nRows, 2];
            reservedPeriodRows = new int[(int)((float)nRows * 0.75f), 2];

            timelineImageHeightInRows = (int)Math.Ceiling((float)TimelineEvent.TIMELINE_IMAGE_HEIGHT / rowH);
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

        void drawTimeline(Graphics gfx)
        {
            gfx.DrawLine(new Pen(Color.Black, timelinePenWidth), 0, this.ClientSize.Height / 2, this.ClientSize.Width, this.ClientSize.Height / 2);

            //draw notches and date labels
            float dateOffset = mod(timelineLB, timelineNotchPeriod);
            if (dateOffset != 0)
                dateOffset = timelineNotchPeriod - dateOffset;
            float labelDate = timelineLB + dateOffset;
            int notchesSincePrevLabel = (int)Math.Ceiling((double)mod(timelineLB, timelineNotchPeriod * timelineLabelFrequency) / timelineNotchPeriod) - 1;

            drawMonthNotches(gfx, dateOffset * timelineNotchWidth / timelineNotchPeriod - timelineNotchWidth);

            for (float x = dateOffset * timelineNotchWidth / timelineNotchPeriod; x < this.ClientSize.Width; x += timelineNotchWidth)
            {
                if (notchesSincePrevLabel == timelineLabelFrequency - 1)
                {
                    gfx.DrawLine(new Pen(Color.Black, timelinePenWidth), x, this.ClientSize.Height / 2 - timelinePenWidth * 2, x, this.ClientSize.Height / 2 + timelinePenWidth * 2);
                    gfx.DrawString(labelDate.ToString(), labelFont, Brushes.Black, x - gfx.MeasureString(labelDate.ToString(), labelFont).Width / 2, this.ClientSize.Height / 2 + 10 + timelinePenWidth);
                    notchesSincePrevLabel = 0;
                }
                else
                {
                    gfx.DrawLine(new Pen(Color.Black, timelinePenWidth), x, this.ClientSize.Height / 2 - timelinePenWidth, x, this.ClientSize.Height / 2 + timelinePenWidth);
                    notchesSincePrevLabel++;
                }

                drawMonthNotches(gfx, x);

                labelDate += timelineNotchPeriod;
            }

            //draw dates
            for (int i = 0; i < reservedLabelRows.Length / 2; i++)
            {
                reservedLabelRows[i, 0] = this.ClientSize.Width + 100;
                reservedLabelRows[i, 1] = -100;
            }
            for (int i = 0; i < reservedPeriodRows.Length / 2; i++)
            {
                reservedPeriodRows[i, 0] = this.ClientSize.Width + 100;
                reservedPeriodRows[i, 1] = -100;
            }

            List<Tuple<int, int, int, Image, bool>> potentialEventImages = new List<Tuple<int, int, int, Image, bool>>(); //the three ints indicate the row and coordinates of the event label

            foreach (var entry in timeline)
            {
                if (entry.IsVisibleOnScreen(timelineLB, timelineUB))
                {
                    if (!entry.IsPeriod)
                    {
                        Brush brush = Brushes.RoyalBlue;
                        string label = entry.Name;
                        if (entry.Name == questionEvent || entry.EntryKey == questionEvent)
                        {
                            if (entry.Name == questionEvent)
                            {
                                if (concealQuestionEvent)
                                    label = "???";
                            }
                            else
                                continue; //hide sub events

                            brush = Brushes.Purple;
                        }

                        int x = convertDateToPixels(entry.Date1);
                        float dotSize = 5 + (float)timelinePenWidth;
                        gfx.FillEllipse(brush, x - dotSize / 2, this.ClientSize.Height / 2 - dotSize / 2, dotSize, dotSize);
                        SizeF labelSize = gfx.MeasureString(label, labelFont);

                        //locate the first available label row
                        int row = 0;
                        for (; row < reservedLabelRows.Length / 2; row++)
                            if (reservedLabelRows[row, 1] + 10 + timelinePenWidth <= x - labelSize.Width / 2)
                                break;

                        if (row < reservedLabelRows.Length / 2)
                        {
                            float y = this.ClientSize.Height / 2 - 10 - timelinePenWidth - labelSize.Height * (1 + row);
                            gfx.DrawString(label, labelFont, brush, x - labelSize.Width / 2, y);

                            reservedLabelRows[row, 0] = Math.Min(x - (int)(labelSize.Width / 2), reservedLabelRows[row, 0]);
                            reservedLabelRows[row, 1] = Math.Max(x + (int)(labelSize.Width / 2), reservedLabelRows[row, 1]);

                            if (entry.Img != null)
                                potentialEventImages.Add(new Tuple<int, int, int, Image, bool>(row + 1, x, (int)y, entry.Img, false));
                        }
                    }
                    else
                    {
                        Brush brush = Brushes.SeaGreen;
                        Color color = Color.SeaGreen;
                        string label = entry.Name;
                        if (entry.Name == questionEvent || entry.EntryKey == questionEvent)
                        {
                            if (entry.Name == questionEvent)
                            {
                                if (concealQuestionEvent)
                                    label = "???";
                            }
                            else
                                continue; //hide sub events

                            brush = Brushes.Purple;
                            color = Color.Purple;
                        }

                        int x1 = convertDateToPixels(entry.Date1), x2 = convertDateToPixels(entry.Date2);
                        SizeF labelSize = gfx.MeasureString(label, labelFont);
                        int labelX = (int)((x1 + x2) / 2 - labelSize.Width / 2);

                        //locate the first available timeline row
                        int row = 0;
                        for (; row < reservedPeriodRows.Length / 2; row++)
                            if (reservedPeriodRows[row, 1] + 10 + timelinePenWidth <= Math.Min(x1, labelX))
                                break;

                        if (row < reservedPeriodRows.Length / 2)
                        {
                            int y = (int)(this.ClientSize.Height / 2 + 10 + timelinePenWidth + labelSize.Height * 1.5f * (1 + row));

                            gfx.DrawLine(new Pen(color, timelinePenWidth / 2), x1, y, x2, y);
                            gfx.DrawString(label, labelFont, brush, labelX, y);

                            reservedPeriodRows[row, 0] = Math.Min(Math.Min(x1, labelX), reservedPeriodRows[row, 0]);
                            reservedPeriodRows[row, 1] = Math.Max(Math.Max(x2, labelX + (int)labelSize.Width), reservedPeriodRows[row, 1]);

                            if (entry.Img != null)
                                potentialEventImages.Add(new Tuple<int, int, int, Image, bool>(row + 1, labelX + (int)labelSize.Width / 2, y + (int)labelSize.Height, entry.Img, true));
                        }
                    }
                }
            }

            //draw event images (those that have unused space above their labels)
            foreach (var eventImage in potentialEventImages)
            {
                if (!eventImage.Item5)
                {
                    //single event
                    bool enoughSpace = true;
                    for (int row = eventImage.Item1; row < eventImage.Item1 + timelineImageHeightInRows && row < reservedLabelRows.Length / 2; row++)
                        if (reservedLabelRows[row, 1] == -100)
                            break;
                        else if (anyOverlap(eventImage.Item2 - eventImage.Item4.Width / 2, eventImage.Item2 + eventImage.Item4.Width / 2, reservedLabelRows[row, 0], reservedLabelRows[row, 1]))
                        {
                            enoughSpace = false;
                            break;
                        }

                    if (enoughSpace)
                    {
                        gfx.DrawImage(eventImage.Item4, eventImage.Item2 - eventImage.Item4.Width / 2, eventImage.Item3 - eventImage.Item4.Height);

                        for (int row = eventImage.Item1; row < eventImage.Item1 + timelineImageHeightInRows && row < reservedLabelRows.Length / 2; row++)
                        {
                            reservedLabelRows[row, 0] = Math.Min(reservedLabelRows[row, 0], eventImage.Item2 - eventImage.Item4.Width / 2);
                            reservedLabelRows[row, 1] = Math.Max(reservedLabelRows[row, 1], eventImage.Item2 + eventImage.Item4.Width / 2);
                        }
                    }
                }
                else
                {
                    //period
                    bool enoughSpace = true;
                    for (int row = eventImage.Item1; row < eventImage.Item1 + timelineImageHeightInRows && row < reservedPeriodRows.Length / 2; row++)
                        if (reservedPeriodRows[row, 1] == -100)
                            break;
                        else if (anyOverlap(eventImage.Item2 - eventImage.Item4.Width / 2, eventImage.Item2 + eventImage.Item4.Width / 2, reservedPeriodRows[row, 0], reservedPeriodRows[row, 1]))
                        {
                            enoughSpace = false;
                            break;
                        }

                    if (enoughSpace)
                    {
                        gfx.DrawImage(eventImage.Item4, eventImage.Item2 - eventImage.Item4.Width / 2, eventImage.Item3);

                        for (int row = eventImage.Item1; row < eventImage.Item1 + timelineImageHeightInRows && row < reservedPeriodRows.Length / 2; row++)
                        {
                            reservedPeriodRows[row, 0] = Math.Min(reservedPeriodRows[row, 0], eventImage.Item2 - eventImage.Item4.Width / 2);
                            reservedPeriodRows[row, 1] = Math.Max(reservedPeriodRows[row, 1], eventImage.Item2 + eventImage.Item4.Width / 2);
                        }
                    }
                }
            }
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

        bool anyOverlap(int a1, int a2, int b1, int b2)
        {
            return a1 >= b1 && a1 <= b2 || a2 >= b1 && a2 <= b2;
        }
        #endregion

        #region FamilyTree
        void processFamilyTreeMsg(string msg)
        {
            string args = msg.Substring(6);
            if (args.Contains(" ?"))
            {
                concealQuestionNode = true;
                args = args.Replace(" ?", "");
            }

            questionNode = args.Substring(0, args.IndexOf('\n'));
            List<string> treeLines = new List<string>(args.Substring(args.IndexOf('\n') + 1).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries));

            nodeImages = new Dictionary<string, Image>();
            foreach (string line in treeLines)
                if (line.Contains(", Appearance: "))
                {
                    string node = line.Substring(0, line.IndexOf(", "));
                    string imgPath = line.Substring(line.IndexOf(": ") + 2);

                    Image img = Bitmap.FromFile(imgPath);
                    nodeImages.Add(node, new Bitmap(img, img.Width * NODE_IMAGE_HEIGHT / img.Height, NODE_IMAGE_HEIGHT));
                    img.Dispose();
                }

            treeLines.RemoveAll(l => l.Contains(", Appearance: "));
            tree = new string[treeLines.Count, 3];
            for (int i = 0; i < treeLines.Count; i++)
            {
                int sep1 = treeLines[i].IndexOf(", ");
                int sep2 = treeLines[i].IndexOf(": ");

                tree[i, 0] = treeLines[i].Substring(0, sep1);
                tree[i, 1] = treeLines[i].Substring(sep1 + 2, sep2 - sep1 - 2);
                tree[i, 2] = treeLines[i].Substring(sep2 + 2);
            }

            treeX = this.ClientSize.Width / 2;
            treeY = this.ClientSize.Height / 2;

            mode = DisplayMode.FamilyTree;
            this.BackgroundImage = null;
            this.Refresh();
        }

        SizeF drawFamilyTreeNode(Graphics gfx, string node, int x, int y, out int nodeX, List<string> visitedNodes, Dictionary<string, Point> parentNodes)
        {
            visitedNodes.Add(node);

            //draw node
            SizeF nodeLabelSize;
            if (node != questionNode || !concealQuestionNode)
            {
                nodeLabelSize = drawCenteredNodeLabel(gfx, node, ref x, y);

                if (nodeImages.ContainsKey(node))
                    gfx.DrawImageUnscaled(nodeImages[node], x - nodeImages[node].Width / 2, y + (int)nodeLabelSize.Height / 2 + 10);
            }
            else
                nodeLabelSize = drawCenteredNodeLabel(gfx, "???", ref x, y);
            nodeX = x;

            //check for consort
            string[] consorts = getNodeRelatives(node, "Consort");
            string nodeConsortCouple;
            if (consorts.Length == 0)
            {
                nodeConsortCouple = node;
                if (!parentNodes.ContainsKey(nodeConsortCouple))
                    parentNodes.Add(nodeConsortCouple, new Point(x, y + (int)nodeLabelSize.Height / 2 + 10));
            }
            else
            {
                string consort = consorts[0];
                nodeConsortCouple = createCouple(new string[] { node, consort });
                if (!parentNodes.ContainsKey(nodeConsortCouple))
                    parentNodes.Add(nodeConsortCouple, new Point(x + 75, y));

                if (!visitedNodes.Contains(consort))
                {
                    int consortNodeX;
                    SizeF consortLabelSize = drawFamilyTreeNode(gfx, consort, x + 150, y, out consortNodeX, visitedNodes, parentNodes);
                    gfx.DrawLine(new Pen(getCoupleColor(nodeConsortCouple, 255), 2), x + nodeLabelSize.Width / 2 + 10, y, x + 150 - 10 - consortLabelSize.Width / 2, y);
                }
            }

            //check for parents
            string[] parents = getNodeRelatives(node, "Parents");
            if (parents.Length > 0)
            {
                string parentCouple = createCouple(parents);
                bool visitedAnyParent = false;
                foreach (string parent in parents)
                    visitedAnyParent |= visitedNodes.Contains(parent);

                if (!visitedAnyParent || !parentNodes.ContainsKey(parentCouple))
                {
                    int parentNodeX;

                    if (parents.Length == 1)
                    {
                        SizeF parentLabelSize = drawFamilyTreeNode(gfx, parents[0], x, y - 300, out parentNodeX, visitedNodes, parentNodes);
                        if (!parentNodes.ContainsKey(parentCouple))
                            parentNodes.Add(parentCouple, new Point(parentNodeX, y - 300 + (int)parentLabelSize.Height / 2 + 10));
                    }
                    else
                    {
                        drawFamilyTreeNode(gfx, parents[0], x - 75, y - 300, out parentNodeX, visitedNodes, parentNodes);
                        if (!parentNodes.ContainsKey(parentCouple))
                            parentNodes.Add(parentCouple, new Point(parentNodeX + 75, y - 300));
                    }
                }

                connectParentToChild(gfx, parentNodes[parentCouple], new Point(x, y - (int)nodeLabelSize.Height / 2 - 10), parentCouple, node, nodeLabelSize);
            }

            //check for children
            string[] children = getNodeRelatives(node, "Children");
            foreach (string child in children)
                if (!visitedNodes.Contains(child))
                {
                    int tmp;
                    SizeF childLabelSize = drawFamilyTreeNode(gfx, child, x, y + 300, out tmp, visitedNodes, parentNodes);
                }

            return nodeLabelSize;
        }

        SizeF drawCenteredNodeLabel(Graphics gfx, string label, ref int x, int y)
        {
            SizeF labelSize = gfx.MeasureString(label, nodeFont);

            int x1 = x - (int)labelSize.Width / 2;
            int x2 = x + (int)labelSize.Width / 2;
            reserveRow(ref y, ref x1, ref x2, 100, true, "");
            x = (x1 + x2) / 2;

            if (label == "???")
                gfx.DrawString(label, nodeFont, Brushes.Purple, x - labelSize.Width / 2, y - labelSize.Height / 2);
            else
                gfx.DrawString(label, nodeFont, Brushes.Black, x - labelSize.Width / 2, y - labelSize.Height / 2);

            return labelSize;
        }

        void connectParentToChild(Graphics gfx, Point p1, Point p2, string parentCouple, string childNode, SizeF childNodeLabelSize)
        {
            Color lineColor = getCoupleColor(parentCouple, 128);
            Pen linePen = new Pen(lineColor, 2);

            if (p1.Y < p2.Y)
            {
                //the parents are above the child
                int midY = p1.Y + NODE_IMAGE_HEIGHT + 40;
                int x1 = p1.X, x2 = p2.X;
                reserveRow(ref midY, ref x1, ref x2, 10, false, parentCouple);

                Point mid1 = new Point(p1.X, midY);
                Point mid2 = new Point(p2.X, midY);

                gfx.DrawLine(linePen, p1, mid1);
                gfx.DrawLine(linePen, mid1, mid2);
                gfx.DrawLine(linePen, mid2, p2);
            }
            else
            {
                //the parents are on the same level as the child
                int extraW;
                if (nodeImages.ContainsKey(childNode))
                    extraW = nodeImages[childNode].Width / 2 + 10;
                else
                    extraW = (int)childNodeLabelSize.Width / 2 + 10;

                int midY = p1.Y + NODE_IMAGE_HEIGHT + 30;
                int x1 = p1.X, x2 = p2.X + extraW;
                reserveRow(ref midY, ref x1, ref x2, 10, false, parentCouple);

                Point mid1 = new Point(p1.X, midY);
                Point mid2 = new Point(p2.X + extraW, midY);
                Point mid3 = new Point(p2.X + extraW, p2.Y - 10);
                Point mid4 = new Point(p2.X, p2.Y - 10);

                gfx.DrawLine(linePen, p1, mid1);
                gfx.DrawLine(linePen, mid1, mid2);
                gfx.DrawLine(linePen, mid2, mid3);
                gfx.DrawLine(linePen, mid3, mid4);
                gfx.DrawLine(linePen, mid4, p2);
            }
        }

        string[] getNodeRelatives(string node, string relation)
        {
            List<string> results = new List<string>();
            string relationQuery = relation;
            if (relation == "Children")
                relationQuery = "Parents";

            bool breakForLoop = false;
            for (int i = 0; i < tree.Length / 3 && !breakForLoop; i++)
                if (tree[i, 1] == relationQuery)
                {
                    switch (relation)
                    {
                        case "Parents":
                            if (tree[i, 0] == node)
                            {
                                foreach (string parent in tree[i, 2].Split('/'))
                                    results.Add(parent);
                                breakForLoop = true;
                            }
                            break;
                        case "Consort":
                            if (tree[i, 0] == node)
                            {
                                results.Add(tree[i, 2]);
                                breakForLoop = true;
                            }
                            if (tree[i, 2] == node)
                            {
                                results.Add(tree[i, 0]);
                                breakForLoop = true;
                            }
                            break;
                        case "Children":
                            if (new List<string>(tree[i, 2].Split('/')).Contains(node))
                                results.Add(tree[i, 0]);
                            break;
                    }
                }

            return results.ToArray();
        }

        string createCouple(string[] nodes)
        {
            if (nodes.Length == 0)
                return "";
            else if (nodes.Length == 1)
                return nodes[0];
            else if (nodes[0].CompareTo(nodes[1]) < 0)
                return nodes[0] + "/" + nodes[1];
            else
                return nodes[1] + "/" + nodes[0];
        }

        Color getCoupleColor(string couple, int alpha)
        {
            return Color.FromArgb(alpha, Color.FromArgb(couple.GetHashCode()));
        }

        void reserveRow(ref int y, ref int x1, ref int x2, int margin, bool node, string parentCouple)
        {
            if (x1 > x2)
            {
                int tmp = x1;
                x1 = x2;
                x2 = tmp;
            }

            int dx = x2 - x1;

            Dictionary<int, int[]> reserved;
            if (node)
                reserved = reservedNodeRows;
            else
                reserved = reservedLineRows;

            if (!reserved.ContainsKey(y))
            {
                reserved.Add(y, new int[] { x1, x2 });
                if (!node)
                    reservedLineRowsBorders.Add(y, new string[] { parentCouple, parentCouple });
            }
            else
            {
                //if this NODE overlaps with reserved space, move it to the left or right
                //if this LINE overlaps with reserved space, move it down
                if ((x1 + x2) / 2 < (reserved[y][0] + reserved[y][1]) / 2)
                {
                    if (x2 + margin > reserved[y][0] && (node || reservedLineRowsBorders[y][0] != parentCouple)) //disregard any overlaps if the reserved line leads to the same parent
                    {
                        if (node)
                        {
                            x2 = reserved[y][0] - margin;
                            x1 = x2 - dx;
                            reserved[y][0] = x1;
                        }
                        else
                        {
                            do
                                y += margin;
                            while (reserved.ContainsKey(y) && reservedLineRowsBorders[y][0] != parentCouple);
                            
                            if (reserved.ContainsKey(y))
                                reservedLineRowsBorders[y][0] = parentCouple;
                            else
                            {
                                reserved.Add(y, new int[] { x1, x2 });
                                reservedLineRowsBorders.Add(y, new string[] { parentCouple, parentCouple });
                            }
                        }
                    }
                    else
                    {
                        reserved[y][0] = x1;
                        if (!node)
                            reservedLineRowsBorders[y][0] = parentCouple;
                    }
                }
                else
                {
                    if (x1 - margin < reserved[y][1] && (node || reservedLineRowsBorders[y][1] != parentCouple)) //disregard any overlaps if the reserved line leads to the same parent
                    {
                        if (node)
                        {
                            x1 = reserved[y][1] + margin;
                            x2 = x1 + dx;
                            reserved[y][1] = x2;
                        }
                        else
                        {
                            do
                                y += margin;
                            while (reserved.ContainsKey(y) && reservedLineRowsBorders[y][1] != parentCouple);
                            
                            if (reserved.ContainsKey(y))
                                reservedLineRowsBorders[y][1] = parentCouple;
                            else
                            {
                                reserved.Add(y, new int[] { x1, x2 });
                                reservedLineRowsBorders.Add(y, new string[] { parentCouple, parentCouple });
                            }
                        }
                    }
                    else
                    {
                        reserved[y][1] = x2;
                        if (!node)
                            reservedLineRowsBorders[y][1] = parentCouple;
                    }
                }
            }
        }
        #endregion

        #region Map
        void processMapMsg(string msg)
        {
            if (msg.Length > 6)
            {
                mapQType = int.Parse(msg.Substring(4, 1));
                msg = msg.Substring(6);

                if (msg.Contains(".."))
                {
                    //expand shortened expression, for example: "Nile..3" -> "Nile+Nile 2+Nile 3"
                    int lb = msg.IndexOf("..");
                    int ub = msg.IndexOf('+', lb);
                    if (ub == -1)
                        ub = msg.Length;

                    int n = int.Parse(msg.Substring(lb + 2, ub - (lb + 2)));
                    
                    string msgRemainder = msg.Substring(ub);
                    msg = msg.Substring(0, lb);
                    string baseName = msg;

                    for (int i = 2; i <= n; i++)
                        msg += "+" + baseName + " " + i.ToString();

                    msg += msgRemainder;
                }

                questionEntities = msg.Split('+');
                viz.Highlight(questionEntities, mapQType);
            }

            mode = DisplayMode.Map;
            mapFrozen = false;
            this.BackgroundImage = null;
            this.Refresh();
        }

        public void ForceDraw()
        {
            this.Invalidate();
        }
        #endregion


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

            logo = Bitmap.FromFile(LOGO_PATH);
            this.BackgroundImage = logo;

            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.formMain_MouseWheel);

            //init variables
            mode = DisplayMode.Image;

            timelinePenWidth = 1;
            timelineLB = 0;

            setTimelineScale();

            labelMonthFont = new Font(FontFamily.GenericSansSerif, 7);
            questionEvent = "";
            mapMouseOverRegion = "";

            timerDoubleClick.Interval = SystemInformation.DoubleClickTime;

            //init worker
            pipeWorker = new BackgroundWorker();
            pipeWorker.DoWork += new DoWorkEventHandler(pipeWorker_DoWork);
            pipeWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(pipeWorker_RunWorkerCompleted);

            pipeWorker.RunWorkerAsync();

            //load data
            loadTimelineData();
            viz = new Visualizer(this.ClientSize, GEO_DIR, ForceDraw);

            //processMsg("map 1 Kagera..3");
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            switch (mode)
            {
                case DisplayMode.Timeline:
                    drawTimeline(e.Graphics);
                    break;
                case DisplayMode.FamilyTree:
                    reservedNodeRows = new Dictionary<int, int[]>();
                    reservedLineRows = new Dictionary<int, int[]>();
                    reservedLineRowsBorders = new Dictionary<int, string[]>();
                    int temp;

                    drawFamilyTreeNode(e.Graphics, questionNode, treeX, treeY, out temp, new List<string>(), new Dictionary<string, Point>());
                    break;
                case DisplayMode.Map:
                    viz.Draw(e.Graphics);
                    break;
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
                case Keys.F1:
                    this.Invalidate();
                    break;
            }
        }

        private void formMain_MouseDown(object sender, MouseEventArgs e)
        {
            initialMousePoint = e.Location;
            previousMousePoint = e.Location;
            prevTimelineLB = timelineLB;
            mouseDown = true;
        }

        private void formMain_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (mouseDown)
                    switch (mode)
                    {
                        case DisplayMode.Timeline:
                            timelineLB = prevTimelineLB - convertPixelsToDate(e.X - previousMousePoint.X);
                            calcTimelineUB();
                            this.Invalidate();
                            break;
                        case DisplayMode.FamilyTree:
                            treeX += e.X - previousMousePoint.X;
                            treeY += e.Y - previousMousePoint.Y;
                            previousMousePoint.X = e.X;
                            previousMousePoint.Y = e.Y;
                            this.Invalidate();
                            break;
                        case DisplayMode.Map:
                            viz.MoveViewport(e.X - previousMousePoint.X, e.Y - previousMousePoint.Y);
                            previousMousePoint.X = e.X;
                            previousMousePoint.Y = e.Y;

                            this.Invalidate();
                            break;
                    }
                else if (mode == DisplayMode.Map && (mapQType == 2 || mapQType == 3) && !timerFeedback.Enabled && !mapFrozen)
                {
                    string mouseOverRegion = viz.GetSelectedArea(e.X, e.Y);
                    if (mouseOverRegion != mapMouseOverRegion)
                    {
                        viz.Highlight(mouseOverRegion.Split('+'), -mapQType);
                        this.Invalidate();

                        mapMouseOverRegion = mouseOverRegion;
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Exception in MouseMove: " + exc.Message);
            }
        }

        private void formMain_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                mouseDown = false;
                
                if (mode == DisplayMode.Map && (mapQType == 2 || mapQType == 3) &&
                    arePointsCloseEnough(e.Location, initialMousePoint)) //check if the user actually clicked or was just dragging
                {
                    if (!timerDoubleClick.Enabled)
                    {
                        mapFrozen = true;
                        mouseClickPoint = e.Location;
                        timerDoubleClick.Enabled = true;
                    }
                    else if (arePointsCloseEnough(e.Location, mouseClickPoint))
                    {
                        //user performed a doubleclick
                        mapFrozen = false;
                        timerDoubleClick.Enabled = false;

                        if (mode == DisplayMode.Map)
                            viz.FastZoomIn(mouseClickPoint.X, mouseClickPoint.Y);
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Exception in MouseUp: " + exc.Message);
            }
        }

        private void formMain_MouseWheel(object sender, MouseEventArgs e)
        {
            switch (mode)
            {
                case DisplayMode.Timeline:
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
                    break;
                case DisplayMode.Map:
                    viz.Zoom(e.Delta > 0);
                    this.Invalidate();
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

        private void timerFeedback_Tick(object sender, EventArgs e)
        {
            if (!isAnswerHighlighted)
                viz.Highlight(questionEntities, -mapQType);
            else
                viz.Highlight(null, -mapQType);
            isAnswerHighlighted = !isAnswerHighlighted;

            this.Invalidate();

            nRemainingFeedbackTicks--;
            if (nRemainingFeedbackTicks == 0)
            {
                timerFeedback.Enabled = false;
                sendFeedbackToAlot(selectedArea);
            }
        }

        private void timerDoubleClick_Tick(object sender, EventArgs e)
        {
            //double click period expired -> execute a single click
            timerDoubleClick.Enabled = false;
            
            selectedArea = viz.GetSelectedArea(mouseClickPoint.X, mouseClickPoint.Y);
            bool correct = viz.ArrayContainsString(questionEntities, selectedArea);

            if (correct)
                sendFeedbackToAlot("True");
            else
            {
                //display the correct answer on the map
                isAnswerHighlighted = false;
                nRemainingFeedbackTicks = 6;
                timerFeedback.Enabled = true;
            }
        }
    }
}
