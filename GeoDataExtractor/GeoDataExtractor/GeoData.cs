using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace GeoDataExtractor
{
    public class Visualizer
    {
        public const string WORLD_COASTLINE_PATH = @"C:\dev\scripts\Alot of Knowledge\dat knowledge\!GEODATA\world landmass.txt";
        public const int SHAPE_TYPE_POLYLINE = 3;
        public const int SHAPE_TYPE_POLYGON = 5;

        Graphics gfx;
        Pen pen;

        Size picSize;
        float viewportX, viewportY, zoom;
        public bool ShowCoastline;
        int defaultColor;

        Shape worldCoastline;
        List<Shape> shapes;
        List<string> shapeNames;
        CheckedListBox.CheckedIndexCollection checkedShapes;

        public Visualizer(Graphics gfx, Size picSize)
        {
            this.gfx = gfx;
            pen = new Pen(Color.Black, 0.1f);

            this.picSize = picSize;
            shapes = new List<Shape>();
            shapeNames = new List<string>();

            loadWorldCoastline();
            ShowCoastline = true;
            defaultColor = 0;

            zoom = (float)picSize.Width / 360;
            viewportX = 180 * zoom;
            viewportY = 90 * zoom;
        }

        void loadWorldCoastline()
        {
            StreamReader file = new StreamReader(WORLD_COASTLINE_PATH);
            while (!file.EndOfStream)
            {
                string name = file.ReadLine();
                file.ReadLine(); //skip POLYGON tag

                worldCoastline = new Poly(file, Visualizer.SHAPE_TYPE_POLYGON);
            }
            file.Close();
        }

        public void AddShape(Shape shape, string name)
        {
            shapes.Add(shape);
            shapeNames.Add(name);
        }
        
        public void UpdateCheckedShapes(CheckedListBox.CheckedIndexCollection checkedShapes)
        {
            this.checkedShapes = checkedShapes;
        }
        
        public void Draw()
        {
            gfx.Clear(SystemColors.Control);
            gfx.ResetTransform();

            gfx.TranslateTransform(viewportX, viewportY);
            gfx.ScaleTransform(zoom, -zoom);

            if (ShowCoastline) //draw coastline
                worldCoastline.Draw(gfx, pen);

            //draw checked shapes
            if (checkedShapes != null)
                for (int i = 0; i < shapes.Count; i++)
                    if (checkedShapes.Contains(i))
                        shapes[i].Draw(gfx, pen);
        }
        
        public void SetDefaultColor(int color)
        {
            for (int i = 0; i < shapes.Count; i++)
                if (shapes[i].ShapeType == SHAPE_TYPE_POLYGON)
                {
                    Poly polygon = (Poly)shapes[i];
                    if (polygon.Color == defaultColor)
                        polygon.Color = color;
                }

            defaultColor = color;
        }

        public string Save(StreamWriter file, bool saveAllShapes)
        {
            string alotEntries = "{\n";

            if (!saveAllShapes)
            {
                for (int i = 0; i < shapes.Count; i++)
                    if (checkedShapes.Contains(i))
                        shapes[i].Save(file);

                alotEntries += "\t\"SHAPE COLLECTION\": \"GEO:" + shapes[0].EntryType + "/SHAPE COLLECTION\",\n";
            }
            else
            {
                for (int i = 0; i < shapes.Count; i++)
                    if (checkedShapes.Contains(i))
                    {
                        file.WriteLine(shapeNames[i]);
                        shapes[i].Save(file);

                        alotEntries += "\t\"" + shapeNames[i] + "\": \"GEO:" + shapes[i].EntryType + "/" + shapeNames[i] + "\",\n";
                    }
            }

            return alotEntries + "}";
        }

        public static string ShapeTypeToString(int shapeType)
        {
            switch(shapeType)
            {
                case SHAPE_TYPE_POLYLINE:
                    return "POLYLINE";
                case SHAPE_TYPE_POLYGON:
                    return "POLYGON";
                default:
                    return "UNKNOWN SHAPE TYPE";
            }
        }
    }

    public abstract class Shape
    {
        public abstract int ShapeType
        {
            get;
        }
        public string EntryType;
        public RectangleF box;
        
        public abstract void Draw(Graphics gfx, Pen pen);

        public abstract void Save(StreamWriter file);
        
        public void EnlargeViewportBox(ref RectangleF viewportBox)
        {
            viewportBox.X = Math.Min(viewportBox.X, box.X);
            viewportBox.Y = Math.Min(viewportBox.Y, box.Y);

            float boxMaxX = box.X + box.Width;
            if (boxMaxX > viewportBox.X + viewportBox.Width)
                viewportBox.Width = boxMaxX - viewportBox.X;

            float boxMaxY = box.Y + box.Height;
            if (boxMaxY > viewportBox.Y + viewportBox.Height)
                viewportBox.Height = boxMaxY - viewportBox.Y;
        }
    }

    public class Poly : Shape //can represent both PolyLine and Polygon
    {
        public override int ShapeType
        {
            get;
        }
        public int Color;
        PointF[][] parts;


        public Poly(BinaryReader file, int shapeType, string entryType, int color)
        {
            ShapeType = shapeType;
            this.EntryType = entryType;
            this.Color = color;

            //read Poly from Shapefile
            float minX = (float)file.ReadDouble();
            float minY = (float)file.ReadDouble();
            float maxX = (float)file.ReadDouble();
            float maxY = (float)file.ReadDouble();
            box = new RectangleF(minX, minY, maxX - minX, maxY - minY);

            int numParts = file.ReadInt32();
            parts = new PointF[numParts][];

            int numPoints = file.ReadInt32(); //total number of points
            file.ReadInt32(); //skip first part index (which should always be 0)
            int prevPartIndex = 0;

            for (int i = 0; i < numParts - 1; i++)
            {
                int partIndex = file.ReadInt32(); //index to first point in (next) part
                parts[i] = new PointF[partIndex - prevPartIndex];
                prevPartIndex = partIndex;
            }
            parts[parts.Length - 1] = new PointF[numPoints - prevPartIndex]; //final part

            for (int i = 0; i < numParts; i++)
                for (int j = 0; j < parts[i].Length; j++)
                    parts[i][j] = new PointF((float)file.ReadDouble(), (float)file.ReadDouble());
        }

        public Poly(StreamReader file, int shapeType)
        {
            ShapeType = shapeType;

            //read Poly from text file
            box = new RectangleF(float.Parse(file.ReadLine()), float.Parse(file.ReadLine()), float.Parse(file.ReadLine()), float.Parse(file.ReadLine()));

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

            if (ShapeType == Visualizer.SHAPE_TYPE_POLYGON)
                Color = int.Parse(file.ReadLine());
        }

        public override void Draw(Graphics gfx, Pen pen)
        {
            if (ShapeType == Visualizer.SHAPE_TYPE_POLYLINE)
                foreach (var line in parts)
                    gfx.DrawLines(pen, line);
            else
                foreach (var polygon in parts)
                    gfx.DrawPolygon(pen, polygon);
        }

        public override void Save(StreamWriter file)
        {
            file.WriteLine(Visualizer.ShapeTypeToString(ShapeType));

            file.WriteLine(box.X);
            file.WriteLine(box.Y);
            file.WriteLine(box.Width);
            file.WriteLine(box.Height);

            file.WriteLine(parts.Length);
            foreach (var part in parts)
            {
                file.WriteLine(part.Length);
                foreach (var point in part)
                    file.WriteLine(point.X + ", " + point.Y);
            }

            if (ShapeType == Visualizer.SHAPE_TYPE_POLYGON)
                file.WriteLine(Color);
        }
    }
}
