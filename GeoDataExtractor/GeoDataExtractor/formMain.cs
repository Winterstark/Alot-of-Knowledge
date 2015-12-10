using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Data.OleDb;
using System.Data;

namespace GeoDataExtractor
{
    public partial class formMain : Form
    {
        Visualizer viz;
        DataTable dbfTable;
        string fileName;
        int nameColumn, miscColumn, typeColumn, countryColumn, unknownShapeCount;
        bool massCheckingInProgress;


        void loadDBF(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Shapes will not be identified.", "Could not locate DBF file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                nameColumn = -1;
            }
            else
            {
                dbfTable = ParseDBF.ReadDBF(filePath);
                
                //find the name, color/width, and type columns
                nameColumn = getColumnIndex("NAME_LONG");
                if (nameColumn == -1)
                    nameColumn = getColumnIndex("NAME");
                if (nameColumn == -1)
                    MessageBox.Show("shapes will not be identified.", "DBF file has no NAME column", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                
                miscColumn = getColumnIndex("MAPCOLOR"); //miscColumn represents the color of a polygon...
                if (miscColumn == -1)
                    miscColumn = getColumnIndex("scalerank"); //...or the width of a polyline or point

                typeColumn = getColumnIndex("type_en");
                if (typeColumn == -1)
                    typeColumn = getColumnIndex("TYPE");

                countryColumn = getColumnIndex("SOV0NAME");
                if (countryColumn == -1)
                    countryColumn = getColumnIndex("admin");
                chkSaveCountry.Enabled = chkSaveCountry.Checked = countryColumn != -1;
            }
        }

        int getColumnIndex(string columnTitle)
        {
            for (int i = 0; i < dbfTable.Columns.Count; i++)
                if (dbfTable.Columns[i].ColumnName.ToUpper().Contains(columnTitle.ToUpper()))
                    return i;

            return -1;
        }

        string getNextItemFromColumn(int column, string defaultValue)
        {
            if (column != -1 && chklistShapes.Items.Count < dbfTable.Rows.Count)
            {
                string name = dbfTable.Rows[chklistShapes.Items.Count][column].ToString().Trim();

                if (name == "")
                    name = "Unknown shape " + (++unknownShapeCount).ToString() + " (" + fileName + ")";

                return name;
            }
            else
                return defaultValue;
        }

        string checkIfDuplicateName(string name)
        {
            if (chklistShapes.Items.Contains(name))
            {
                int ind = 2;
                while (chklistShapes.Items.Contains(name + " " + ind))
                    ind++;

                name = name + " " + ind;
            }

            return name;
        }
        
        void loadShapefile(string filePath)
        {
            fileName = Path.GetFileNameWithoutExtension(filePath);

            chklistShapes.Items.Clear();
            unknownShapeCount = 0;

            BinaryReader file = new BinaryReader(new FileStream(filePath, FileMode.Open));
            if (readBigEndianInt32(file) == 0x0000270a || MessageBox.Show("File code doesn't match the Shapefile specification. Continue loading file anyway?", "WARNING!", MessageBoxButtons.OKCancel,MessageBoxIcon.Warning) == DialogResult.OK)
            {
                try
                {
                    //unused; five uint32's
                    for (int i = 0; i < 5; i++)
                        file.ReadUInt32();

                    int fileLength = readBigEndianInt32(file);
                    int version = file.ReadInt32();
                    int shapeType = file.ReadInt32();

                    double minX = file.ReadDouble();
                    double minY = file.ReadDouble();
                    double maxX = file.ReadDouble();
                    double maxY = file.ReadDouble();
                    double minZ = file.ReadDouble();
                    double maxZ = file.ReadDouble();
                    double minM = file.ReadDouble();
                    double maxM = file.ReadDouble();

                    int processedLength = 50; //lengths are measured in 16-bit words
                    while (processedLength < fileLength)
                    {
                        readBigEndianInt32(file); //ignore record number
                        int recordLength = readBigEndianInt32(file);
                        int recordShapeType = file.ReadInt32();
                        string name;

                        switch (recordShapeType)
                        {
                            case Visualizer.SHAPE_TYPE_POINT:
                                name = checkIfDuplicateName(getNextItemFromColumn(nameColumn, "Point"));
                                
                                viz.AddShape(new Point(file, Visualizer.SHAPE_TYPE_POINT, getNextItemFromColumn(typeColumn, "UNKNOWN_TYPE"), int.Parse(getNextItemFromColumn(miscColumn, "0")), getNextItemFromColumn(countryColumn, "UNKNOWN COUNTRY")), name);
                                chklistShapes.Items.Add(name);
                                break;
                            case Visualizer.SHAPE_TYPE_POLYLINE:
                                name = checkIfDuplicateName(getNextItemFromColumn(nameColumn, "PolyLine"));

                                viz.AddShape(new Poly(file, Visualizer.SHAPE_TYPE_POLYLINE, getNextItemFromColumn(typeColumn, "UNKNOWN_TYPE"), int.Parse(getNextItemFromColumn(miscColumn, "0")), getNextItemFromColumn(countryColumn, "UNKNOWN COUNTRY")), name);
                                chklistShapes.Items.Add(name);
                                break;
                            case Visualizer.SHAPE_TYPE_POLYGON:
                                name = checkIfDuplicateName(getNextItemFromColumn(nameColumn, "Polygon"));
                                
                                viz.AddShape(new Poly(file, Visualizer.SHAPE_TYPE_POLYGON, getNextItemFromColumn(typeColumn, "UNKNOWN_TYPE"), int.Parse(getNextItemFromColumn(miscColumn, "0")), getNextItemFromColumn(countryColumn, "UNKNOWN COUNTRY")), name);
                                chklistShapes.Items.Add(name);
                                break;
                            default:
                                MessageBox.Show("Unsupported shape type: " + recordShapeType.ToString());
                                break;
                        }

                        processedLength += 4 + recordLength; //4 = length of the record header
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Encountered exception while reading file!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            file.Close();

            lblShapesCount.Text = chklistShapes.Items.Count + " shapes:";
        }

        int readBigEndianInt32(BinaryReader file)
        {
            return IPAddress.HostToNetworkOrder(file.ReadInt32());
        }

        void updateCheckedShapes()
        {
            buttCheckNone.Enabled = chklistShapes.CheckedItems.Count != 0;
            buttCheckAll.Enabled = chklistShapes.CheckedItems.Count != chklistShapes.Items.Count;
            buttSave.Enabled = chklistShapes.CheckedItems.Count != 0;

            viz.UpdateCheckedShapes(chklistShapes.CheckedIndices);
            viz.Draw();
        }


        public formMain()
        {
            InitializeComponent();
        }

        private void formMain_Load(object sender, EventArgs e)
        {
            viz = new Visualizer(picDisplay.CreateGraphics(), picDisplay.ClientSize);

            massCheckingInProgress = false;
            lblDragRecipient.Left = 12; //put label on top
        }

        private void lblDragRecipient_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
                lblDragRecipient.BackColor = SystemColors.Highlight;
            }
        }

        private void lblDragRecipient_DragLeave(object sender, EventArgs e)
        {
            lblDragRecipient.BackColor = SystemColors.Control;
        }

        private void lblDragRecipient_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files.Length > 0)
            {
                loadDBF(files[0].Replace(".shp", ".dbf"));
                loadShapefile(files[0]);

                lblDragRecipient.Visible = false;
            }
        }

        private void chklistShapes_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (!massCheckingInProgress)
                timerRefreshDisplay.Enabled = true; //using a timer to trigger refresh because the checked item's state has not been updated yet
        }

        private void buttCheckAll_Click(object sender, EventArgs e)
        {
            massCheckingInProgress = true;
            for (int i = 0; i < chklistShapes.Items.Count; i++)
                chklistShapes.SetItemChecked(i, true);
            massCheckingInProgress = false;

            updateCheckedShapes();
        }

        private void buttCheckNone_Click(object sender, EventArgs e)
        {
            massCheckingInProgress = true;
            for (int i = 0; i < chklistShapes.Items.Count; i++)
                chklistShapes.SetItemChecked(i, false);
            massCheckingInProgress = false;

            updateCheckedShapes();
        }

        private void buttSave_Click(object sender, EventArgs e)
        {
            string alotEntries = "";

            saveDialog.ShowDialog();
            if (saveDialog.FileName != "")
            {
                string name = "";
                if (InputBox.Show("Enter name for the shape collection:", "Enter nothing to save shapes separately.", ref name) == DialogResult.OK)
                {
                    StreamWriter file = new StreamWriter(saveDialog.FileName, true);

                    if (name != "")
                    {
                        file.WriteLine(name);
                        alotEntries = viz.Save(file, false, chkSaveCountry.Checked);
                    }
                    else
                        alotEntries = viz.Save(file, true, chkSaveCountry.Checked);

                    file.Close();
                }
            }

            //create entries for Alot
            saveDialog.FileName = saveDialog.FileName.Replace(".txt", "_entries.txt");
            saveDialog.ShowDialog();
            if (saveDialog.FileName != "")
            {
                StreamWriter file = new StreamWriter(saveDialog.FileName, true);
                file.Write(alotEntries);
                file.Close();
            }
        }

        private void numDefaultColor_ValueChanged(object sender, EventArgs e)
        {
            viz.SetDefaultColor((int)numDefaultColor.Value);
        }

        private void buttClose_Click(object sender, EventArgs e)
        {
            lblDragRecipient.BackColor = SystemColors.Control;
            lblDragRecipient.Visible = true;

            buttCheckAll.Enabled = true;
            buttCheckNone.Enabled = false;
            buttSave.Enabled = false;
        }

        private void timerRefreshDisplay_Tick(object sender, EventArgs e)
        {
            timerRefreshDisplay.Enabled = false;
            updateCheckedShapes();
        }

        private void chkShowWorldCoastline_CheckedChanged(object sender, EventArgs e)
        {
            viz.ShowCoastline = chkShowWorldCoastline.Checked;
            viz.Draw();
        }
    }
}
