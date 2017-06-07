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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AlotWriter
{
    public partial class formMain : Form
    {
        class ExtendedTextBox : TextBox //changes behavior when user double clicks a text segment (so that the selected segment does NOT include the delimiters)
        {
            protected override void WndProc(ref System.Windows.Forms.Message m)
            {
                if (m.Msg == 0x0203 && this.TextLength > 0) //WM_LBUTTONDBLCLK
                {
                    if (this.SelectionStart == 0)
                        this.SelectionStart = 1;
                    if (this.SelectionStart == this.TextLength)
                        this.SelectionStart = this.TextLength - 1;

                    int lb, ub;
                    for (lb = this.SelectionStart; lb >= 0; lb--)
                        if (!char.IsLetterOrDigit(this.Text[lb]))
                            break;
                    for (ub = this.SelectionStart; ub < this.Text.Length; ub++)
                        if (!char.IsLetterOrDigit(this.Text[ub]))
                            break;

                    if (lb != -1 && ub != this.Text.Length && lb != ub)
                    {
                        this.SelectionStart = lb + 1;
                        this.SelectionLength = ub - lb - 1;
                        return;
                    }
                }

                base.WndProc(ref m);
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCaretPos(out Point lpPoint);

        enum CharType { AlphaNum, Whitespace, Punctuation, Unknown };

        const string DAT_KNOWLEDGE = @"C:\dev\scripts\Alot of Knowledge\dat knowledge"; //directory containing knowledge files
        const string IMG_DIR = @"C:\dev\scripts\Alot of Knowledge\dat knowledge\!IMAGES"; //top-level directory for images
        const string SOUNDS_DIR = @"C:\dev\scripts\Alot of Knowledge\dat knowledge\!SOUNDS"; //top-level directory for sounds

        ExtendedTextBox txtInput;
        Dictionary<string, string> classes, attributes;


        int getCaretPosition()
        {
            Point pt;
            GetCaretPos(out pt);

            return txtInput.GetCharIndexFromPosition(pt);
        }

        CharType getCharType(int ind)
        {
            if (ind < 0 || ind >= txtInput.TextLength)
                return CharType.Unknown;

            char c = txtInput.Text[ind];
            if (char.IsLetterOrDigit(c))
                return CharType.AlphaNum;
            else if (char.IsWhiteSpace(c))
                return CharType.Whitespace;
            else
                return CharType.Punctuation;
        }

        void surroundTextSegment(char openingChar, char closingChar)
        {
            int selectionStart = txtInput.SelectionStart;
            int selectionLength = txtInput.SelectionLength;

            txtInput.Text = txtInput.Text.Insert(selectionStart + selectionLength, closingChar.ToString()).Insert(selectionStart, openingChar.ToString());
            txtInput.Select(selectionStart + 1, selectionLength);
        }

        char matchingClosingChar(char openingChar)
        {
            switch (openingChar)
            {
                case '"':
                    return '"';
                case '\'':
                    return '\'';
                case '(':
                    return ')';
                case '[':
                    return ']';
                case '{':
                    return '}';
                default:
                    return '0';
            }
        }

        string getPreviousStringDelimiterPair()
        {
            int closestQuotationMark = txtInput.Text.LastIndexOf('"', txtInput.SelectionStart);
            int closestApostrophe = txtInput.Text.LastIndexOf("'", txtInput.SelectionStart);

            if (closestApostrophe > closestQuotationMark)
                return "''";
            else
                return "\"\"";
        }

        char getCharPrecedingCursor()
        {
            if (txtInput.SelectionStart == 0)
                return ' ';
            else
                return txtInput.Text[txtInput.SelectionStart - 1];
        }

        char getCharFollowingCursor()
        {
            if (txtInput.SelectionStart == txtInput.TextLength)
                return ' ';
            else
                return txtInput.Text[txtInput.SelectionStart];
        }

        string getCharsSurroundingCursor()
        {
            if (txtInput.SelectionStart == 0 || txtInput.SelectionStart + 1 > txtInput.TextLength)
                return "";
            else
                return txtInput.Text.Substring(txtInput.SelectionStart - 1, 2);
        }

        void replaceCharsSurroundingCursor(string replacement, int selectionStartOffset = 0)
        {
            int selectionStart = txtInput.SelectionStart;
            txtInput.Text = txtInput.Text.Remove(selectionStart - 1, 2).Insert(selectionStart - 1, replacement);
            txtInput.SelectionStart = selectionStart + selectionStartOffset;
        }


        public formMain()
        {
            InitializeComponent();
        }

        private void formMain_Load(object sender, EventArgs e)
        {
            //create extended textbox
            txtInput = new ExtendedTextBox();
            this.Controls.Add(txtInput);
            txtInput.BringToFront();
            txtInput.Multiline = true;
            txtInput.AcceptsTab = true;
            txtInput.ScrollBars = ScrollBars.Vertical;
            txtInput.Dock = DockStyle.Fill;

            txtInput.Font = new Font("Consolas", 10);
            txtInput.BackColor = Color.FromArgb(39, 40, 34);
            txtInput.ForeColor = Color.FromArgb(248, 248, 242);

            txtInput.KeyDown += txtInput_KeyDown;
            txtInput.KeyPress += txtInput_KeyPress;

            txtInput.DragEnter += txtInput_DragEnter;
            txtInput.DragDrop += txtInput_DragDrop;
            txtInput.AllowDrop = true;

            //load classes
            classes = new Dictionary<string, string>();

            if (Directory.Exists(Application.StartupPath + "\\classes"))
            {
                foreach (string path in Directory.GetFiles(Application.StartupPath + "\\classes", "*.txt"))
                {
                    menuAddClass.DropDownItems.Insert(menuAddClass.DropDownItems.Count - 2, new ToolStripMenuItem(Path.GetFileNameWithoutExtension(path)));

                    StreamReader file = new StreamReader(path);
                    classes[Path.GetFileNameWithoutExtension(path)] = file.ReadToEnd();
                    file.Close();
                }
            }

            //load attributes
            attributes = new Dictionary<string, string>();

            if (Directory.Exists(Application.StartupPath + "\\attributes"))
            {
                foreach (string path in Directory.GetFiles(Application.StartupPath + "\\attributes", "*.txt"))
                {
                    menuAddAttribute.DropDownItems.Insert(menuAddAttribute.DropDownItems.Count - 2, new ToolStripMenuItem(Path.GetFileNameWithoutExtension(path)));

                    StreamReader file = new StreamReader(path);
                    attributes[Path.GetFileNameWithoutExtension(path)] = file.ReadToEnd();
                    file.Close();
                }
            }

            //scan for knowledge files
            foreach (string path in Directory.GetFiles(DAT_KNOWLEDGE, "*.txt"))
                menuSaveTo.DropDownItems.Add(Path.GetFileNameWithoutExtension(path));
        }

        private void menuAddClass_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.ToString() == "Save class...")
            {
                string className = "class";
                if (InputBox.Show("Save class", "Class name?", ref className) == DialogResult.OK)
                {
                    string path = Application.StartupPath + "\\classes\\" + className + ".txt";
                    bool existingClass = File.Exists(path);

                    if (!existingClass || MessageBox.Show("Overwrite existing class?", "Class name already taken", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.OK)
                    {
                        StreamWriter file = new StreamWriter(path);
                        file.Write(txtInput.Text);
                        file.Close();

                        if (existingClass)
                            className = classes.First(c => c.Key.ToLower() == className.ToLower()).Key; //preserve original case
                        else
                            menuAddClass.DropDownItems.Insert(0, new ToolStripMenuItem(className));

                        classes[className] = txtInput.Text;
                    }
                }
            }
            else
            {
                int prevLen = txtInput.TextLength;
                txtInput.Text += classes[e.ClickedItem.ToString()];

                int classTitleInd = txtInput.Text.IndexOf("''", prevLen);
                if (classTitleInd == -1)
                    classTitleInd = txtInput.Text.IndexOf("\"\"", prevLen);
                if (classTitleInd != -1)
                    txtInput.SelectionStart = classTitleInd + 1;
            }
        }

        private void menuAddAttribute_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.ToString() == "Save attribute...")
            {
                string attributeName = "attribute";
                if (InputBox.Show("Save attribute", "Attribute name?", ref attributeName) == DialogResult.OK)
                {
                    string path = Application.StartupPath + "\\attributes\\" + attributeName + ".txt";
                    bool existingAttribute = File.Exists(path);

                    if (!existingAttribute || MessageBox.Show("Overwrite existing attribute?", "Attribute name already taken", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.OK)
                    {
                        StreamWriter file = new StreamWriter(Application.StartupPath + "\\attributes\\" + attributeName + ".txt");
                        file.Write(txtInput.Text);
                        file.Close();

                        if (existingAttribute)
                            attributeName = attributes.First(a => a.Key.ToLower() == attributeName.ToLower()).Key; //preserve original case
                        else
                            menuAddAttribute.DropDownItems.Insert(0, new ToolStripMenuItem(attributeName));

                        attributes[attributeName] = txtInput.Text;
                    }
                }
            }
            else
            {
                int ind = txtInput.Text.IndexOf('}', txtInput.SelectionStart);
                if (ind == -1)
                    ind = txtInput.TextLength;
                else
                {
                    txtInput.Text = txtInput.Text.Insert(ind - Environment.NewLine.Length, Environment.NewLine + "\t");
                    ind++;
                }

                txtInput.Text = txtInput.Text.Insert(ind, attributes[e.ClickedItem.ToString()]);

                int attributeTitleInd = txtInput.Text.IndexOf("''", ind);
                if (attributeTitleInd == -1)
                    attributeTitleInd = txtInput.Text.IndexOf("\"\"", ind);
                if (attributeTitleInd != -1)
                    txtInput.SelectionStart = attributeTitleInd + 1;
            }
        }

        private void menuSaveTo_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (txtInput.TextLength < Environment.NewLine.Length)
                return;

            string path = DAT_KNOWLEDGE + "\\" + e.ClickedItem.ToString() + ".txt";

            StreamReader fileReader = new StreamReader(path);
            string contents = fileReader.ReadToEnd();
            fileReader.Close();

            int index = contents.LastIndexOf("," + Environment.NewLine) + 1 + Environment.NewLine.Length;
            if (index != -1)
            {
                //add a tab char at the beginning of every line (except the last)
                txtInput.Text = "\t" + txtInput.Text.Replace(Environment.NewLine, Environment.NewLine + '\t');
                txtInput.Text = txtInput.Text.Substring(0, txtInput.TextLength - 1);

                contents = contents.Insert(index, txtInput.Text);

                StreamWriter fileWriter = new StreamWriter(path);
                fileWriter.Write(contents);
                fileWriter.Close();

                txtInput.Text = "";
                Process.Start(path);
            }
        }

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            int selectionStart = txtInput.SelectionStart;

            switch (e.KeyCode)
            {
                case Keys.A:
                    if (e.Control)
                    {
                        txtInput.SelectAll();
                        e.SuppressKeyPress = true;
                    }
                    break;
                case Keys.Tab:
                    //if the TAB character is pressed while the text cursor is within a key segment, generate a blank value segment
                    //the final result should look like any of the following:    "Key": ''    or    "Key": ""    or    "Key": {} or    "Key": []
                    if (getCharsSurroundingCursor() == "''" || getCharsSurroundingCursor() == "\"\"")
                    {
                        replaceCharsSurroundingCursor("{}");
                        e.SuppressKeyPress = true;
                    }
                    else if (getCharsSurroundingCursor() == "{}")
                    {
                        replaceCharsSurroundingCursor("[]");
                        e.SuppressKeyPress = true;

                    }
                    else if (getCharsSurroundingCursor() == "[]")
                    {
                        replaceCharsSurroundingCursor(getPreviousStringDelimiterPair());
                        e.SuppressKeyPress = true;
                    }
                    else if (selectionStart != 0)
                    {
                        int lb;
                        for (lb = selectionStart - 1; lb >= 0; lb--)
                            if (char.IsPunctuation(txtInput.Text[lb]))
                                break;
                        int ub;
                        for (ub = selectionStart - 1; ub < txtInput.TextLength; ub++)
                            if (char.IsPunctuation(txtInput.Text[ub]))
                                break;

                        if (lb != -1 && ub != txtInput.TextLength && txtInput.Text[lb] == txtInput.Text[ub])
                        {
                            txtInput.Text = txtInput.Text.Insert(ub + 1, ": " + txtInput.Text[lb] + txtInput.Text[lb]);
                            txtInput.SelectionStart = ub + 4;
                            e.SuppressKeyPress = true;
                        }
                    }
                    break;
                case Keys.Enter:
                    if (getCharPrecedingCursor() == '"' || getCharPrecedingCursor() == '\'' || getCharPrecedingCursor() == ')' || getCharPrecedingCursor() == ']' || getCharPrecedingCursor() == '}')
                    {
                        txtInput.Text = txtInput.Text.Insert(selectionStart, ",");
                        txtInput.SelectionStart = selectionStart + 1;
                    }
                    else if (getCharPrecedingCursor() == ',' && txtInput.Text.LastIndexOf('{', selectionStart - 1) != -1 && txtInput.Text.IndexOf('}', selectionStart) != -1)
                    {
                        txtInput.Text = txtInput.Text.Insert(selectionStart, Environment.NewLine + "\t" + getPreviousStringDelimiterPair() + ": " + getPreviousStringDelimiterPair() + ",");
                        txtInput.SelectionStart = selectionStart + 4;
                        e.SuppressKeyPress = true;
                    }
                    else if (getCharsSurroundingCursor() == "{}")
                    {
                        txtInput.Text = txtInput.Text.Insert(selectionStart, Environment.NewLine + "\t" + getPreviousStringDelimiterPair() + ": " + getPreviousStringDelimiterPair() + "," + Environment.NewLine);
                        txtInput.SelectionStart = selectionStart + 4;
                        e.SuppressKeyPress = true;
                    }
                    break;
                case Keys.Back:
                    if (e.Control)
                    {
                        CharType deleteCType = getCharType(selectionStart - 1);
                        int lb;
                        for (lb = selectionStart - 1; lb > 0; lb--)
                            if (getCharType(lb - 1) != deleteCType)
                                break;

                        if (lb < selectionStart)
                        {
                            txtInput.Text = txtInput.Text.Remove(lb, selectionStart - lb);
                            txtInput.SelectionStart = lb;
                        }

                        e.SuppressKeyPress = true;
                    }
                    else if (getCharsSurroundingCursor() == "''" || getCharsSurroundingCursor() == "\"\"" || getCharsSurroundingCursor() == "()" || getCharsSurroundingCursor() == "[]" || getCharsSurroundingCursor() == "{}")
                    {
                        replaceCharsSurroundingCursor("", -1);
                        e.SuppressKeyPress = true;
                    }
                    break;
                case Keys.Left:
                    if (e.Control)
                    {

                        int i;
                        for (i = getCaretPosition() - 1; i > 0; i--) //skip any initial whitespaces
                            if (getCharType(i - 1) != CharType.Whitespace)
                                break;

                        CharType moveCType = getCharType(i - 1);
                        for (; i > 0; i--)
                            if (getCharType(i - 1) != moveCType)
                                break;

                        if (e.Shift)
                        {
                            if (i > selectionStart)
                                txtInput.SelectionLength = i - selectionStart;
                            else
                                txtInput.Select(selectionStart + txtInput.SelectionLength, -(selectionStart + txtInput.SelectionLength - i));
                        }
                        else
                            txtInput.Select(i, 0);
                        e.SuppressKeyPress = true;
                    }
                    break;
                case Keys.Right:
                    if (e.Control)
                    {
                        int caretPos = getCaretPosition(), i;
                        for (i = caretPos; i < txtInput.TextLength; i++) //skip any initial whitespaces
                            if (getCharType(i) != CharType.Whitespace)
                                break;

                        CharType moveCType = getCharType(i);
                        for (; i < txtInput.TextLength; i++)
                            if (getCharType(i) != moveCType)
                                break;

                        if (e.Shift)
                        {
                            if (caretPos > selectionStart)
                                txtInput.SelectionLength = i - selectionStart;
                            else
                                txtInput.Select(selectionStart + txtInput.SelectionLength, -(selectionStart + txtInput.SelectionLength - i));
                        }
                        else
                            txtInput.Select(i, 0);
                        e.SuppressKeyPress = true;
                    }
                    break;
            }
        }

        private void txtInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case '"':
                case '\'':
                case '(':
                case '[':
                case '{':
                    if (txtInput.SelectionLength > 0)
                    {
                        surroundTextSegment(e.KeyChar, matchingClosingChar(e.KeyChar));
                        e.Handled = true;
                    }
                    else
                    {
                        if (getCharPrecedingCursor() != e.KeyChar)
                        {
                            int selectionStart = txtInput.SelectionStart;
                            txtInput.Text = txtInput.Text.Insert(selectionStart, matchingClosingChar(e.KeyChar).ToString());
                            txtInput.SelectionStart = selectionStart;
                        }
                    }
                    break;
                case ')':
                case ']':
                case '}':
                    if (getCharFollowingCursor() == e.KeyChar)
                    {
                        txtInput.SelectionStart += 1;
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void txtInput_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void txtInput_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string path = ((string[])(e.Data.GetData(DataFormats.FileDrop)))[0];

                string extension = Path.GetExtension(path), dir;
                saveDialog.Filter = "Original file type|*" + extension;

                if (extension == ".ogg")
                    extension = ".mp3"; //mp3 should be the default sound format

                if (extension == ".mp3")
                    dir = SOUNDS_DIR + "\\";
                else
                {
                    extension = ".jpg"; //jpg should be the default image format
                    dir = IMG_DIR + "\\";
                }

                int index = txtInput.Text.IndexOf("*" + extension, txtInput.SelectionStart); //first search right
                if (index == -1)
                    index = txtInput.Text.LastIndexOf("*" + extension, txtInput.SelectionStart); //then search left

                if (index != -1)
                {
                    int lb = Math.Max(txtInput.Text.LastIndexOf('"', index), txtInput.Text.LastIndexOf("'", index));
                    int ub = Math.Max(txtInput.Text.IndexOf('"', index), txtInput.Text.IndexOf("'", index));

                    if (lb != -1 && ub != -1)
                    {
                        lb++;
                        dir += txtInput.Text.Substring(lb, ub - lb).Replace("\\\\", "\\");

                        saveDialog.InitialDirectory = dir.Substring(0, dir.IndexOf(@"\*") + 1);
                        saveDialog.FileName = Path.GetFileName(path);

                        if (saveDialog.ShowDialog() == DialogResult.OK)
                        {
                            string destPath = saveDialog.FileName;
                            File.Move(path, destPath);

                            if (extension == ".jpg")
                                destPath = destPath.Replace(IMG_DIR + "\\", "");
                            else
                                destPath = destPath.Replace(SOUNDS_DIR + "\\", "");

                            txtInput.Text = txtInput.Text.Remove(lb, ub - lb).Insert(lb, destPath.Replace("\\", "\\\\"));
                        }
                    }
                }
            }
        }
    }
}
