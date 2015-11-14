using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Net;

namespace GeoDataExtractor
{
    public partial class formMain : Form
    {
        void loadShapefile(string filePath)
        {
            BinaryReader file = new BinaryReader(new FileStream(filePath, FileMode.Open));

            int fileCode = file.ReadInt32();
            fileCode = IPAddress.HostToNetworkOrder(fileCode);
            if (fileCode != 0x0000270a && MessageBox.Show("File code doesn't match the Shapefile specification. Continue loading file anyway?", "WARNING!", MessageBoxButtons.OKCancel,MessageBoxIcon.Warning) == DialogResult.OK)
            {
                //unused; five uint32's
                for (int i = 0; i < 5; i++)
                    file.ReadUInt32();
            }
            
            file.Close();
        }


        public formMain()
        {
            InitializeComponent();
        }

        private void formMain_Load(object sender, EventArgs e)
        {
            //debugging
            loadShapefile(@"C:\Users\Winterstark\Desktop\ne_110m_coastline.shp");
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
                loadShapefile(files[0]);
        }
    }
}
