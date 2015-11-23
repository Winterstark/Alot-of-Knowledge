using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace AlotGUI
{
    public class Visualizer
    {
        public const int SHAPE_TYPE_POLYLINE = 3;
        public const int SHAPE_TYPE_POLYGON = 5;

        Shape worldLandmass;
        Dictionary<string, Shape> mapEntities;
        List<Shape> highlightedEntities;

        Brush seaBrush, highlightBrush;
        Pen pen, highlightPen;
        Size windowSize;
        float viewportX, viewportY, zoom;
        int qType;


        public Visualizer(Size windowSize, string geoDir)
        {
            this.windowSize = windowSize;

            zoom = windowSize.Width / 360;
            viewportX = 180 * zoom;
            viewportY = 90 * zoom;

            seaBrush = new SolidBrush(Color.FromArgb(222, 229, 237));
            highlightBrush = new SolidBrush(Color.Purple);
            pen = new Pen(Color.Black, 0.003f);
            highlightPen = pen;

            highlightedEntities = new List<Shape>();

            //load geo data
            mapEntities = new Dictionary<string, Shape>();

            foreach (string filePath in Directory.GetFiles(geoDir))
            {
                StreamReader file = new StreamReader(filePath);

                while (!file.EndOfStream)
                {
                    string name = file.ReadLine();
                    switch (file.ReadLine())
                    {
                        case "POLYLINE":
                            mapEntities.Add(name, new PolyLine(file, pen));
                            break;
                        case "POLYGON":
                            if (filePath.Contains("landmass"))
                                worldLandmass = new Polygon(file, pen);
                            else
                                mapEntities.Add(name, new Polygon(file, pen));
                            break;
                    }
                }

                file.Close();
            }
        }

        public void MoveViewport(int dx, int dy)
        {
            viewportX += dx;
            viewportY += dy;

            checkViewportBounds();
        }

        public void Zoom(bool zoomIn)
        {
            float diff = zoom * 0.3f;
            if (!zoomIn)
                diff *= -1;

            //calculate viewport center (in order to keep the same point in the middle after zooming)
            float viewportCenterX = (-viewportX + (float)windowSize.Width / 2) / zoom;
            float viewportCenterY = (viewportY - (float)windowSize.Height / 2) / zoom;

            //zoom
            zoomOnPoint(viewportCenterX, viewportCenterY, zoom + diff);
        }

        void zoomOnPoint(float x, float y, float newZoom)
        {
            zoom = newZoom;

            viewportX = -x * zoom + (float)windowSize.Width / 2;
            viewportY = y * zoom + (float)windowSize.Height / 2;

            checkViewportBounds();
        }

        void checkViewportBounds()
        {
            if (360 * zoom < windowSize.Width) //don't unzoom so much that the world map is smaller than the window
                zoom = (float)windowSize.Width / 360.0f;

            if (360 * zoom > windowSize.Width)
            {
                viewportX = Math.Min(viewportX, 180 * zoom);
                viewportX = Math.Max(viewportX, windowSize.Width - 180 * zoom);
            }
            else
                viewportX = windowSize.Width / 2;

            if (180 * zoom > windowSize.Height)
            {
                viewportY = Math.Min(viewportY, 90 * zoom);
                viewportY = Math.Max(viewportY, windowSize.Height - 90 * zoom);
            }
            else
                viewportY = windowSize.Height / 2;
        }

        public string GetSelectedCountry(int mx, int my)
        {
            float lon = (mx - viewportX) / zoom;
            float lat = -(my - viewportY) / zoom;

            List<Tuple<string, double>> enclosingCountries = new List<Tuple<string, double>>();
            double avgDistance;

            foreach (var country in mapEntities)
                if (country.Value.Box.Contains(lon, lat) && ((Polygon)country.Value).IsPointInPolygon(lon, lat, out avgDistance))
                    enclosingCountries.Add(new Tuple<string, double>(country.Key, avgDistance));

            //there may be more than one enclosing country if the user selected an enclave (e.g. San Marino or Lesotho), so select the one whose points are on average closest to the point of selection
            double minAvgDistance = double.MaxValue;
            string selectedCountry = "";

            foreach (var country in enclosingCountries)
                if (country.Item2 < minAvgDistance)
                {
                    selectedCountry = country.Item1;
                    minAvgDistance = country.Item2;
                }

            return selectedCountry;
        }

        public void Draw(Graphics gfx)
        {
            gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            gfx.TranslateTransform(viewportX, viewportY);
            gfx.ScaleTransform(zoom, -zoom);

            gfx.FillRectangle(seaBrush, -180, -90, 360, 180); //oceans and seas
            gfx.DrawRectangle(pen, -180, -90, 360, 180); //world frame

            if (qType == 1)
                foreach (var entity in mapEntities)
                    entity.Value.Draw(gfx);
            else
            {
                worldLandmass.Draw(gfx);

                foreach (var entity in highlightedEntities)
                    entity.Draw(gfx);
            }
        }

        public void Highlight(string entity, int qType)
        {
            this.qType = qType;
            if (qType == 3)
            {
                zoomOnPoint(0, 0, windowSize.Width / 360); //unzoom all the way
                return;
            }

            foreach (var ent in mapEntities)
                ent.Value.Highlighted = false;

            if (entity != "" && mapEntities.ContainsKey(entity))
            {
                highlightedEntities.Clear();
                
                if (qType == 2)
                {
                    //select five more random entities
                    List<string> keys = new List<string>();

                    foreach (var ent in mapEntities)
                        if (ent.Value.ShapeType == mapEntities[entity].ShapeType && ent.Key != entity)
                            highlightedEntities.Add(ent.Value);

                    Random rand = new Random((int)DateTime.Now.Ticks);
                    while (highlightedEntities.Count > 5)
                        highlightedEntities.RemoveAt(rand.Next(highlightedEntities.Count));
                }

                highlightedEntities.Add(mapEntities[entity]);

                RectangleF highlightedEntitiesBox = new RectangleF(360, 180, 0, 0);
                foreach (var ent in highlightedEntities)
                {
                    ent.Highlighted = true;

                    if (ent.Box.X < highlightedEntitiesBox.X)
                        highlightedEntitiesBox.X = ent.Box.X;
                    if (ent.Box.Y < highlightedEntitiesBox.Y)
                        highlightedEntitiesBox.Y = ent.Box.Y;
                    if (ent.Box.Right > highlightedEntitiesBox.Right)
                        highlightedEntitiesBox.Width = ent.Box.Right - highlightedEntitiesBox.X;
                    if (ent.Box.Bottom > highlightedEntitiesBox.Bottom)
                        highlightedEntitiesBox.Height = ent.Box.Bottom - highlightedEntitiesBox.Y;
                }

                if (qType != -1)
                {
                    //zoom in on the highlighted entities
                    float newZoom;
                    if (highlightedEntitiesBox.Width / highlightedEntitiesBox.Height > 1)
                        newZoom = windowSize.Width / highlightedEntitiesBox.Width * 0.75f;
                    else
                        newZoom = windowSize.Height / highlightedEntitiesBox.Height * 0.75f;

                    zoomOnPoint(highlightedEntitiesBox.X + highlightedEntitiesBox.Width / 2, highlightedEntitiesBox.Y + highlightedEntitiesBox.Height / 2, newZoom);
                }
            }
        }
    }
    

    public abstract class Shape
    {
        public RectangleF Box;
        public int ShapeType;
        public bool Highlighted;


        public abstract void Draw(Graphics gfx);
        
        public void EnlargeViewportBox(ref RectangleF viewportBox)
        {
            viewportBox.X = Math.Min(viewportBox.X, Box.X);
            viewportBox.Y = Math.Min(viewportBox.Y, Box.Y);

            float boxMaxX = Box.X + Box.Width;
            if (boxMaxX > viewportBox.X + viewportBox.Width)
                viewportBox.Width = boxMaxX - viewportBox.X;

            float boxMaxY = Box.Y + Box.Height;
            if (boxMaxY > viewportBox.Y + viewportBox.Height)
                viewportBox.Height = boxMaxY - viewportBox.Y;
        }
    }

    
    public class PolyLine : Shape
    {
        protected PointF[][] parts;
        protected Pen pen;


        public PolyLine(StreamReader file, Pen pen)
        {
            ShapeType = Visualizer.SHAPE_TYPE_POLYLINE;
            this.pen = pen;

            Box = new RectangleF(float.Parse(file.ReadLine()), float.Parse(file.ReadLine()), float.Parse(file.ReadLine()), float.Parse(file.ReadLine()));

            int numParts = int.Parse(file.ReadLine());
            parts = new PointF[numParts][];

            for (int i = 0; i < numParts; i++)
            {
                parts[i] = new PointF[int.Parse(file.ReadLine())];
                for (int j = 0; j < parts[i].Length; j++)
                {
                    string[] coords = file.ReadLine().Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                    parts[i][j] = new PointF(float.Parse(coords[0]), float.Parse(coords[1]));
                }
            }
        }

        public override void Draw(Graphics gfx)
        {
            foreach (var line in parts)
                gfx.DrawLines(pen, line);
        }
    }
    

    public class Polygon : PolyLine
    {
        public static readonly Color[] MAP_COLORS = { Color.LightGreen, Color.FromArgb(232, 185, 81), Color.FromArgb(166, 156, 84), Color.FromArgb(131, 165, 91), Color.FromArgb(245, 241, 150), Color.FromArgb(79, 132, 126), Color.FromArgb(191, 203, 95), Color.FromArgb(231, 162, 129), Color.Snow };
        
        Brush brush;


        public Polygon(StreamReader file, Pen pen) : base(file, pen)
        {
            ShapeType = Visualizer.SHAPE_TYPE_POLYGON;

            int color = int.Parse(file.ReadLine()) / 100;
            brush = new SolidBrush(MAP_COLORS[color]);
        }

        public override void Draw(Graphics gfx)
        {
            foreach (var polygon in parts)
            {
                if (!Highlighted)
                    gfx.FillPolygon(brush, polygon);
                else
                    gfx.FillPolygon(Brushes.Purple, polygon);

                gfx.DrawPolygon(pen, polygon);
            }
        }

        public bool IsPointInPolygon(float x, float y, out double avgDistance)
        {
            foreach (var part in parts)
            {
                double angle = 0;
                avgDistance = 0;

                for (int i = 0; i < part.Length - 1; i++)
                {
                    //calculate the angle between (x, y) and the two points that form a segment of the polygon
                    double x1 = part[i].X - x;
                    double y1 = part[i].Y - y;
                    double x2 = part[(i + 1) % part.Length].X - x;
                    double y2 = part[(i + 1) % part.Length].Y - y;

                    double theta1 = Math.Atan2(y1, x1);
                    double theta2 = Math.Atan2(y2, x2);
                    double dtheta = theta2 - theta1;

                    while (dtheta > Math.PI)
                        dtheta -= Math.PI * 2;
                    while (dtheta < -Math.PI)
                        dtheta += Math.PI * 2;

                    angle += dtheta;
                    
                    //calculate the distance between (x, y) and the polygon's points
                    avgDistance += Math.Sqrt(x1 * x1 + y1 * y1);
                    if (i == part.Length - 2)
                        avgDistance += Math.Sqrt(x2 * x2 + y2 * y2);
                }

                //if the sum of all angles is not 0 then the point is in the polygon
                if (Math.Abs(angle) > 0.01)
                    return true;
            }

            avgDistance = double.MaxValue;
            return false;
        }
    }
}