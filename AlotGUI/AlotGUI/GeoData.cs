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

        public enum GeoType {Landmass, Country, PhysicalRegion, MarineArea, Lake};

        Shape worldLandmass;
        Dictionary<string, Shape> mapEntities;
        List<Shape> highlightedEntities;
        
        Brush seaBrush, highlightBrush;
        Pen pen, highlightPen;
        Size windowSize;
        RectangleF viewportBox;
        float viewportX, viewportY, zoom;
        int qType;
        GeoType qGeoType;


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
                GeoType geoType;
                if (filePath.Contains("countries"))
                    geoType = Visualizer.GeoType.Country;
                else if (filePath.Contains("physical"))
                    geoType = Visualizer.GeoType.PhysicalRegion;
                else if (filePath.Contains("marine"))
                    geoType = Visualizer.GeoType.MarineArea;
                else if (filePath.Contains("lakes"))
                    geoType = Visualizer.GeoType.Lake;
                else
                    geoType = Visualizer.GeoType.Landmass;

                StreamReader file = new StreamReader(filePath);
                
                while (!file.EndOfStream)
                {
                    string name = file.ReadLine();
                    if (mapEntities.ContainsKey(name))
                        MessageBox.Show(name, "A map entity with that name already exists!", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    string shapeType = file.ReadLine();
                    switch (shapeType)
                    {
                        case "POLYLINE":
                            mapEntities.Add(name, new PolyLine(geoType, file, pen));
                            break;
                        case "POLYGON":
                            if (geoType == GeoType.Landmass)
                                worldLandmass = new Polygon(GeoType.Landmass, file, pen);
                            else
                                mapEntities.Add(name, new Polygon(geoType, file, pen));
                            break;
                        default:
                            MessageBox.Show(shapeType, "Unsupported shape type!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            viewportBox = new RectangleF(-viewportX / zoom, (viewportY - windowSize.Height) / zoom, windowSize.Width / zoom, windowSize.Height / zoom);
        }

        public string GetSelectedArea(int mx, int my)
        {
            float lon = (mx - viewportX) / zoom;
            float lat = -(my - viewportY) / zoom;

            List<Tuple<string, double>> enclosingAreas = new List<Tuple<string, double>>();
            double avgDistance;

            foreach (var entity in mapEntities)
                if (entity.Value.GeoType == qGeoType && entity.Value.Box.Contains(lon, lat) && ((Polygon)entity.Value).IsPointInPolygon(lon, lat, out avgDistance))
                    enclosingAreas.Add(new Tuple<string, double>(entity.Key, avgDistance));

            //there may be more than one enclosing area if the user selected an enclave (e.g. San Marino or Lesotho), so select the one whose points are on average closest to the point of selection
            double minAvgDistance = double.MaxValue;
            string selectedArea = "";

            foreach (var area in enclosingAreas)
                if (area.Item2 < minAvgDistance)
                {
                    selectedArea = area.Item1;
                    minAvgDistance = area.Item2;
                }

            return selectedArea;
        }

        public void Draw(Graphics gfx)
        {
            gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            gfx.TranslateTransform(viewportX, viewportY);
            gfx.ScaleTransform(zoom, -zoom);

            gfx.FillRectangle(seaBrush, -180, -90, 360, 180); //ocean background
            gfx.DrawRectangle(pen, -180, -90, 360, 180); //world frame

            if (qType == 1)
            {
                if (qGeoType == GeoType.PhysicalRegion)
                    drawEntityCollection(gfx, GeoType.PhysicalRegion);

                drawEntityCollection(gfx, GeoType.Country);

                if (qGeoType == GeoType.Lake || qGeoType == GeoType.Country)
                    drawEntityCollection(gfx, GeoType.Lake);
            }
            else
                worldLandmass.Draw(gfx);

            //draw highlighted region
            foreach (var entity in highlightedEntities)
                if (entity.Box.IntersectsWith(viewportBox))
                    entity.Draw(gfx);
        }

        void drawEntityCollection(Graphics gfx, GeoType type)
        {
            foreach (var entity in mapEntities)
                if (entity.Value.GeoType == type && entity.Value.Box.IntersectsWith(viewportBox))
                    entity.Value.Draw(gfx);
        }

        public void Highlight(string[] entities, int qType)
        {
            foreach (var ent in mapEntities)
                ent.Value.Highlighted = false;

            bool mapEntitiesExist = true;
            foreach (string mapEnt in entities)
                if (mapEntities.ContainsKey(mapEnt))
                {
                    if (qType != 2)
                        mapEntities[mapEnt].Highlighted = true;
                }
                else
                {
                    mapEntitiesExist = false;
                    break;
                }

            if (qType != -2 && entities != null && mapEntitiesExist)
            {
                this.qType = qType;
                qGeoType = mapEntities[entities[0]].GeoType;
                highlightedEntities.Clear();

                switch (qType)
                {
                    case 2:
                        //select five more random entities (from the nearest neighbors)
                        SortedList<float, string> neighbors = new SortedList<float, string>(); //key is the approximated distance to the region, value is the region's name

                        RectangleF entitiesBox = new RectangleF(mapEntities[entities[0]].Box.X, mapEntities[entities[0]].Box.Y, mapEntities[entities[0]].Box.Width, mapEntities[entities[0]].Box.Height);
                        for (int i = 1; i < entities.Length; i++)
                            addRectangles(ref entitiesBox, mapEntities[entities[i]].Box);
                        
                        foreach (var ent in mapEntities)
                            if (!ArrayContainsString(entities, ent.Key) && ent.Value.GeoType == qGeoType)
                            {
                                float distance = distanceBetweenRegions(ent.Value.Box, entitiesBox);

                                //remember closest 20 regions
                                if (neighbors.Count < 20)
                                    neighbors.Add(distance, ent.Key);
                                else if (distance < neighbors.Keys[19])
                                {
                                    neighbors.RemoveAt(19);
                                    neighbors.Add(distance, ent.Key);
                                }
                            }

                        //choose 5 random neighbors
                        Random rand = new Random((int)DateTime.Now.Ticks);
                        while (highlightedEntities.Count < 5 && neighbors.Count > 0)
                        {
                            int index = rand.Next(neighbors.Count);
                            highlightedEntities.Add(mapEntities[neighbors.Values[index]]);
                            neighbors.RemoveAt(index);
                        }
                        break;
                    case 3:
                        zoomOnPoint(0, 0, windowSize.Width / 360); //unzoom all the way
                        return;
                }

                foreach (string ent in entities)
                    highlightedEntities.Add(mapEntities[ent]);

                RectangleF highlightedEntitiesBox = new RectangleF(highlightedEntities[0].Box.X, highlightedEntities[0].Box.Y, highlightedEntities[0].Box.Width, highlightedEntities[0].Box.Height);
                for (int i = 1; i < highlightedEntities.Count; i++)
                    addRectangles(ref highlightedEntitiesBox, highlightedEntities[i].Box);

                if (qType > 0)
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

        public bool ArrayContainsString(string[] arr, string s)
        {
            foreach (string item in arr)
                if (item == s)
                    return true;

            return false;
        }

        void addRectangles(ref RectangleF mainBox, RectangleF boxToAdd)
        {
            if (boxToAdd.X < mainBox.X)
            {
                mainBox.Width += mainBox.X - boxToAdd.X;
                mainBox.X = boxToAdd.X;
            }
            if (boxToAdd.Y < mainBox.Y)
            {
                mainBox.Height += mainBox.Y - boxToAdd.Y;
                mainBox.Y = boxToAdd.Y;
            }
            if (boxToAdd.Right > mainBox.Right)
                mainBox.Width = boxToAdd.Right - mainBox.X;
            if (boxToAdd.Bottom > mainBox.Bottom)
                mainBox.Height = boxToAdd.Bottom - mainBox.Y;
        }

        float distanceBetweenRegions(RectangleF box1, RectangleF box2)
        {
            float x1 = box1.Left + box1.Width / 2;
            float y1 = box1.Top + box1.Height / 2;
            float x2 = box2.Left + box2.Width / 2;
            float y2 = box2.Top + box2.Height / 2;

            return (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        }
    }
    

    public abstract class Shape
    {
        public RectangleF Box;
        public Visualizer.GeoType GeoType;
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


        public PolyLine(Visualizer.GeoType geoType, StreamReader file, Pen pen)
        {
            GeoType = geoType;

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
        public static readonly Color[] MAP_COLORS = {
            Color.LightGreen, //landmass color
            Color.FromArgb(232, 185, 81), Color.FromArgb(166, 156, 84), Color.FromArgb(131, 165, 91), Color.FromArgb(245, 241, 150), Color.FromArgb(79, 132, 126), Color.FromArgb(191, 203, 95), Color.FromArgb(231, 162, 129), //country colors
            Color.GhostWhite, //Greenland and Antarctica
            Color.ForestGreen, Color.OliveDrab, Color.DarkOrange, Color.OrangeRed, Color.Snow, Color.SaddleBrown, Color.DeepSkyBlue }; //physical regions colors (peninsula, island, desert, canyon, mountain, plateau, archipelago)

        Brush brush, highlightedBrush;
        
        
        public Polygon(Visualizer.GeoType geoType, StreamReader file, Pen pen) : base(geoType, file, pen)
        {
            ShapeType = Visualizer.SHAPE_TYPE_POLYGON;

            int color = int.Parse(file.ReadLine()) / 100;

            switch (GeoType)
            {
                case Visualizer.GeoType.Landmass:
                    brush = new SolidBrush(MAP_COLORS[color]);
                    break;
                case Visualizer.GeoType.Country:
                    brush = new SolidBrush(MAP_COLORS[color]);
                    highlightedBrush = Brushes.Purple;
                    break;
                case Visualizer.GeoType.PhysicalRegion:
                    brush = new SolidBrush(Color.FromArgb(64, MAP_COLORS[color]));
                    highlightedBrush = new SolidBrush(Color.FromArgb(192, MAP_COLORS[color]));
                    break;
                case Visualizer.GeoType.MarineArea:
                    highlightedBrush = Brushes.LightBlue;
                    break;
                case Visualizer.GeoType.Lake:
                    brush = Brushes.PowderBlue;
                    highlightedBrush = Brushes.LightSkyBlue;
                    break;
            }
        }

        public override void Draw(Graphics gfx)
        {
            foreach (var polygon in parts)
            {
                if (!Highlighted)
                {
                    if (brush != null)
                        gfx.FillPolygon(brush, polygon);
                }
                else
                {
                    if (highlightedBrush != null)
                        gfx.FillPolygon(highlightedBrush, polygon);
                }

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