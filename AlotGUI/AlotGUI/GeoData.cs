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

        public enum GeoType {Landmass, PhysicalRegion, MarineArea, Lake, River, Country, Region, City};

        Shape worldLandmass;
        Dictionary<string, Shape> mapEntities;
        List<Shape> highlightedEntities;

        Bitmap preDrawnMap;
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

            zoom = (float)windowSize.Width / 360;
            viewportX = 180.0f * zoom;
            viewportY = 90.0f * zoom;
            //zoomOnPoint(180.0f * zoom, 90.0f * zoom, zoom);

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
                else if (filePath.Contains("admin"))
                    geoType = Visualizer.GeoType.Region;
                else if (filePath.Contains("cities"))
                    geoType = Visualizer.GeoType.City;
                else if (filePath.Contains("physical"))
                    geoType = Visualizer.GeoType.PhysicalRegion;
                else if (filePath.Contains("marine"))
                    geoType = Visualizer.GeoType.MarineArea;
                else if (filePath.Contains("lakes"))
                    geoType = Visualizer.GeoType.Lake;
                else if (filePath.Contains("rivers"))
                    geoType = Visualizer.GeoType.River;
                else
                    geoType = Visualizer.GeoType.Landmass;

                StreamReader file = new StreamReader(filePath);
                
                while (!file.EndOfStream)
                {
                    string name = file.ReadLine();

                    string shapeType = file.ReadLine();
                    switch (shapeType)
                    {
                        case "POINT":
                            mapEntities.Add(name, new GeoPoint(geoType, file));
                            break;
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

            preDrawMap();
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

            preDrawMap();
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
            return;
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
            try
            {
                float lon = (mx - viewportX) / zoom;
                float lat = -(my - viewportY) / zoom;

                List<Tuple<string, double>> enclosingAreas = new List<Tuple<string, double>>();
                double avgDistance;

                foreach (var entity in mapEntities)
                    if (entity.Value.GeoType == qGeoType && (entity.Value.GeoType == GeoType.City || entity.Value.Box.Contains(lon, lat)) && entity.Value.IsSelected(lon, lat, out avgDistance))
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

                if (selectedArea != "" && qGeoType == GeoType.River)
                {
                    //rivers often have more than one segment; selectedArea needs to include them all
                    string baseName = selectedArea = getRiverBaseName(selectedArea);

                    int i = 2;
                    while (mapEntities.ContainsKey(baseName + " " + i.ToString()))
                        selectedArea += "+" + baseName + " " + (i++).ToString();
                }

                return selectedArea;
            }
            catch (Exception exc)
            {
                MessageBox.Show("Exception in GetSelectedArea: " + exc.Message);
                return "";
            }
        }

        void preDrawMap()
        {
            //if the zoom is small enough, pre-draw the map to improve performance
            if (preDrawnMap != null)
                preDrawnMap.Dispose();

            if (zoom < 10)
            {
                int imgW = (int)(360.0f * zoom);
                int imgH = (int)(180.0f * zoom);
                preDrawnMap = new Bitmap(imgW, imgH);

                Graphics imgGfx = Graphics.FromImage(preDrawnMap);

                //imgGfx.TranslateTransform(windowSize.Width / 2, windowSize.Height / 2 - 90 * zoom);
                //imgGfx.TranslateTransform(viewportX, viewportY);
                imgGfx.TranslateTransform(imgW / 2, imgH / 2);
                imgGfx.ScaleTransform(zoom, -zoom);

                drawMapImage(imgGfx);

                imgGfx.Dispose();
            }
            else
                preDrawnMap = null;
        }

        public void Draw(Graphics gfx)
        {
            if (preDrawnMap != null)
            {
                gfx.DrawImageUnscaled(preDrawnMap, (int)viewportX, (int)viewportY);
                gfx.DrawRectangle(Pens.Red, (int)viewportX, (int)viewportY, preDrawnMap.Width, preDrawnMap.Height);

                drawHighlightedRegions(gfx);
            }
            else
            {
                gfx.TranslateTransform(viewportX, viewportY);
                gfx.ScaleTransform(zoom, -zoom);
                drawMapImage(gfx);
            }
        }

        void drawMapImage(Graphics gfx)
        {
            gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            //gfx.TranslateTransform(windowSize.Width / 2, windowSize.Height / 2 - 90 * zoom);
            //gfx.TranslateTransform(viewportX, viewportY);
            //gfx.ScaleTransform(zoom, -zoom);

            gfx.FillRectangle(seaBrush, -180, -90, 360, 180); //ocean background
            gfx.DrawRectangle(pen, -180, -90, 360, 180); //world frame

            if (qType == 1)
            {
                if (qGeoType == GeoType.Region)
                    drawEntityCollection(gfx, GeoType.Region);
                else
                {
                    if (qGeoType == GeoType.PhysicalRegion)
                        drawEntityCollection(gfx, GeoType.PhysicalRegion);

                    drawEntityCollection(gfx, GeoType.Country);
                    drawEntityCollection(gfx, GeoType.City);

                    drawHighlightedRegions(gfx);

                    if (qGeoType == GeoType.Lake || qGeoType == GeoType.River || qGeoType == GeoType.Country)
                    {
                        drawEntityCollection(gfx, GeoType.River);
                        drawEntityCollection(gfx, GeoType.Lake);
                    }
                }
            }
            else
            {
                worldLandmass.Highlighted = qGeoType == GeoType.Lake || qGeoType == GeoType.River; //change the landmass color if the qType involves lakes or rivers to make them more noticeable
                worldLandmass.Draw(gfx);

                drawHighlightedRegions(gfx);
            }
        }

        void drawEntityCollection(Graphics gfx, GeoType type)
        {
            foreach (var entity in mapEntities)
                if (entity.Value.GeoType == type && (type == GeoType.City || entity.Value.Box.IntersectsWith(viewportBox)))
                    entity.Value.Draw(gfx);
        }

        void drawHighlightedRegions(Graphics gfx)
        {
            foreach (var entity in highlightedEntities)
                if (entity.Box.IntersectsWith(viewportBox))
                    entity.Draw(gfx);
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
                            if (!ArrayContainsString(entities, ent.Key) && !neighbors.ContainsValue(getRiverBaseName(ent.Key)) && ent.Value.GeoType == qGeoType)
                            {
                                string entName = getRiverBaseName(ent.Key);
                                
                                float distance = 0;
                                if (qGeoType != GeoType.River)
                                    distance = distanceBetweenRegions(ent.Value.Box, entitiesBox);
                                else
                                {
                                    //create a Box that includes all segments of the river
                                    RectangleF riverBox = new RectangleF(ent.Value.Box.X, ent.Value.Box.Y, ent.Value.Box.Width, ent.Value.Box.Height);
                                    for (int i = 2; mapEntities.ContainsKey(entName + " " + i.ToString()); i++)
                                        addRectangles(ref riverBox, mapEntities[entName + " " + i.ToString()].Box);

                                    distance = distanceBetweenRegions(riverBox, entitiesBox);
                                }

                                //remember closest 20 regions
                                if (neighbors.Count < 20)
                                    neighbors.Add(distance, entName);
                                else if (distance < neighbors.Keys[19])
                                {
                                    neighbors.RemoveAt(19);
                                    neighbors.Add(distance, entName);
                                }
                            }

                        //choose 5 random neighbors
                        Random rand = new Random((int)DateTime.Now.Ticks);

                        for (int i = 0; i < 5 && neighbors.Count > 0; i++)
                        {
                            int index = rand.Next(neighbors.Count);

                            if (qGeoType != GeoType.River)
                                highlightedEntities.Add(mapEntities[neighbors.Values[index]]);
                            else
                            {
                                //add all river segments
                                string river = getRiverBaseName(neighbors.Values[index]);

                                highlightedEntities.Add(mapEntities[river]);
                                for (int j = 2; mapEntities.ContainsKey(river + " " + j.ToString()); j++)
                                    highlightedEntities.Add(mapEntities[river + " " + j.ToString()]);
                            }

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
                    newZoom = Math.Min(newZoom, 200);

                    zoomOnPoint(highlightedEntitiesBox.X + highlightedEntitiesBox.Width / 2, highlightedEntitiesBox.Y + highlightedEntitiesBox.Height / 2, newZoom);
                }
            }
        }

        public bool ArrayContainsString(string[] arr, string s)
        {
            s = getRiverBaseName(s);

            foreach (string item in arr)
                if (getRiverBaseName(item) == s)
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

        string getRiverBaseName(string entityName)
        {
            if (mapEntities.ContainsKey(entityName) && mapEntities[entityName].GeoType == GeoType.River)
            {
                int ind = entityName.LastIndexOf(' '), tmp;
                if (ind != -1 && ind != entityName.Length - 1 && int.TryParse(entityName.Substring(ind + 1), out tmp))
                    return entityName.Substring(0, entityName.LastIndexOf(' '));
            }

            return entityName;
        }
    }
    

    public abstract class Shape
    {
        public RectangleF Box;
        public Visualizer.GeoType GeoType;
        public bool Highlighted;


        public abstract void Draw(Graphics gfx);

        public abstract bool IsSelected(float x, float y, out double distance); //distance returns the MINIMUM distance for a point/polyline or the AVERAGE distance for a polygon
        
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


    public class GeoPoint : Shape
    {
        PointF point;
        float size;


        public GeoPoint(Visualizer.GeoType geoType, StreamReader file)
        {
            GeoType = geoType;

            string[] coords = file.ReadLine().Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            point = new PointF(float.Parse(coords[0]), float.Parse(coords[1]));

            Box = new RectangleF(point.X, point.Y, 0.01f, 0.01f);

            size = (11 - float.Parse(file.ReadLine())) / 100;
        }

        public override void Draw(Graphics gfx)
        {
            gfx.FillEllipse(Highlighted ? Brushes.Purple : Brushes.Black, point.X - size / 2, point.Y - size / 2, size, size);

            if (size >= 0.05)
            {
                //draw larger cities with a red center circle
                float centerSize = size / 2;
                gfx.FillEllipse(Brushes.DarkRed, point.X - centerSize / 2, point.Y - centerSize / 2, centerSize, centerSize);
            }
        }

        public override bool IsSelected(float x, float y, out double distance)
        {
            float dx = x - point.X;
            float dy = y - point.Y;
            distance = Math.Sqrt(dx * dx + dy * dy);

            return distance <= size / 2;
        }
    }

    
    public class PolyLine : Shape
    {
        protected PointF[][] parts;
        protected Pen pen;


        public PolyLine(Visualizer.GeoType geoType, StreamReader file, Pen pen)
        {
            GeoType = geoType;
            
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

            if (geoType == Visualizer.GeoType.River)
            {
                int scaleRank = int.Parse(file.ReadLine());
                this.pen = new Pen(Color.PowderBlue, 0.028f - (float)scaleRank / 400);
            }
        }

        public override void Draw(Graphics gfx)
        {
            if (Highlighted)
            {
                Pen highlightedPen = new Pen(Color.Purple, pen.Width + 0.1f);
                foreach (var polyline in parts)
                    gfx.DrawLines(highlightedPen, polyline);

                highlightedPen.Dispose();
            }

            foreach (var polyline in parts)
                gfx.DrawLines(pen, polyline);
        }

        public override bool IsSelected(float x, float y, out double minDistance)
        {
            minDistance = double.MaxValue;

            foreach (var polyline in parts)
                for (int i = 0; i < polyline.Length - 1; i++)
                {
                    double dist = lineToPointDistance2D(polyline[i], polyline[i + 1], x, y, true);
                    if (dist < minDistance)
                        minDistance = dist;
                }

            return minDistance < 1;
        }

        #region Calculate distance from point to line segment
        //compute the dot product AB . AC
        private double dotProduct(double[] pointA, double[] pointB, double[] pointC)
        {
            double[] AB = new double[2];
            double[] BC = new double[2];

            AB[0] = pointB[0] - pointA[0];
            AB[1] = pointB[1] - pointA[1];
            BC[0] = pointC[0] - pointB[0];
            BC[1] = pointC[1] - pointB[1];

            return AB[0] * BC[0] + AB[1] * BC[1];
        }

        //compute the cross product AB x AC
        private double crossProduct(double[] pointA, double[] pointB, double[] pointC)
        {
            double[] AB = new double[2];
            double[] AC = new double[2];

            AB[0] = pointB[0] - pointA[0];
            AB[1] = pointB[1] - pointA[1];
            AC[0] = pointC[0] - pointA[0];
            AC[1] = pointC[1] - pointA[1];

            return AB[0] * AC[1] - AB[1] * AC[0];
        }

        //compute the distance from A to B
        double distance(double[] pointA, double[] pointB)
        {
            double d1 = pointA[0] - pointB[0];
            double d2 = pointA[1] - pointB[1];

            return Math.Sqrt(d1 * d1 + d2 * d2);
        }

        //compute the distance from AB to C
        //if isSegment is true, AB is a segment, not a line.
        double lineToPointDistance2D(PointF ptA, PointF ptB, float cx, float cy, bool isSegment)
        {
            double[] pointA = new double[] { ptA.X, ptA.Y };
            double[] pointB = new double[] { ptB.X, ptB.Y };
            double[] pointC = new double[] { cx, cy };
            
            if (isSegment)
            {
                if (dotProduct(pointA, pointB, pointC) > 0)
                    return distance(pointB, pointC);

                if (dotProduct(pointB, pointA, pointC) > 0)
                    return distance(pointA, pointC);
            }

            return Math.Abs(crossProduct(pointA, pointB, pointC) / distance(pointA, pointB));
        } 
        #endregion
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
            int color = int.Parse(file.ReadLine());
            if (geoType != Visualizer.GeoType.Region)
                color /= 100;

            switch (GeoType)
            {
                case Visualizer.GeoType.Landmass:
                    brush = new SolidBrush(MAP_COLORS[color]);
                    highlightedBrush = Brushes.ForestGreen;
                    break;
                case Visualizer.GeoType.Country:
                case Visualizer.GeoType.Region:
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

                if (GeoType != Visualizer.GeoType.Lake)
                    gfx.DrawPolygon(pen, polygon);
            }
        }

        public override bool IsSelected(float x, float y, out double avgDistance)
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