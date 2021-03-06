﻿#region License Information (GPL v2)

/*
    ZScreen - A program that allows you to upload screenshots in one keystroke.
    Copyright (C) 2008-2011 ZScreen Developers

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v2)

#region Source code: Greenshot (GPL)

/*
    This file originated from the Greenshot project (GPL). It may or may not have been modified.
    Please do not contact Greenshot about errors with this code. Instead contact the creators of this program.
    URL: http://greenshot.sourceforge.net/
    Code (CVS): http://greenshot.cvs.sourceforge.net/viewvc/greenshot/
*/

#endregion Source code: Greenshot (GPL)

/*
 * Erstellt mit SharpDevelop.
 * Benutzer: thomas
 * Datum: 22.03.2007
 * Zeit: 23:09
 *
 * Sie k�nnen diese Vorlage unter Extras > Optionen > Codeerstellung > Standardheader �ndern.
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Greenshot.Configuration;
using Greenshot.Drawing;
using Greenshot.Helpers;

namespace Greenshot
{
    public partial class ImageEditorForm : Form
    {
        public BackgroundWorker MyWorker { get; set; }

        public bool AutoSave = true;

        private ColorDialog colorDialog = ColorDialog.GetInstance();
        private string lastSaveFullPath;
        private AppConfig conf = AppConfig.GetInstance();
        private Surface surface;

        public ImageEditorForm()
        {
            InitializeComponent();

            surface = new Surface();
            surface.SizeMode = PictureBoxSizeMode.AutoSize;
            surface.TabStop = false;
            surface.MovingElementChanged += new SurfaceElementEventHandler(surfaceMovingElementChanged);
            panel1.Controls.Add(surface);

            this.colorDialog.RecentColors = conf.Editor_RecentColors;

            UpdateFormControls();
        }

        private void UpdateFormControls()
        {
            Bitmap imgBorder = DrawColorButton(ColorType.Border);
            btnBorderColor.Image = imgBorder;
            borderColorToolStripMenuItem.Image = imgBorder;

            Bitmap imgBackground = DrawColorButton(ColorType.Background);
            btnBackgroundColor.Image = imgBackground;
            backgroundColorToolStripMenuItem.Image = imgBackground;

            Bitmap imgGradient = DrawColorButton(ColorType.Gradient);
            btnGradientColor.Image = imgGradient;
            gradientColorToolStripMenuItem.Image = imgGradient;

            tscbGradientType.Items.Add("None");
            tscbGradientType.Items.AddRange(Enum.GetNames(typeof(LinearGradientMode)));
            tscbGradientType.Text = conf.Editor_GradientType;

            cbThickness.Text = conf.Editor_Thickness.ToString();

            btnArrowHeads.Image = DrawArrowHeadsButton(surface.ArrowHead, btnArrowHeads.ContentRectangle);
        }

        public Image GetImage()
        {
            return surface.GetImageForExport();
        }

        public void SetImage(Image img)
        {
            surface.Image = img;

            int gap = 150;
            if (Screen.PrimaryScreen.Bounds.Width - img.Width > gap || Screen.PrimaryScreen.Bounds.Height - img.Height > gap)
            {
                this.Size = new Size(img.Width + gap, img.Height + gap);
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
            }
        }

        public void SetImagePath(string fullpath)
        {
            this.lastSaveFullPath = fullpath;
            if (fullpath == null) return;
            updateStatusLabel("Image saved to %storagelocation%.".Replace("%storagelocation%", fullpath), fileSavedStatusContextMenu);
            this.Text = "Image Annotator" + " - " + Path.GetFileName(fullpath);
            this.saveToolStripMenuItem.Enabled = true;
        }

        private void surfaceMovingElementChanged(object sender, DrawableContainerList selectedElements)
        {
            bool elementSelected = (selectedElements.Count > 0);
            this.btnCopy.Enabled = elementSelected;
            this.btnCut.Enabled = elementSelected;
            this.btnDelete.Enabled = elementSelected;
            this.copyToolStripMenuItem.Enabled = elementSelected;
            this.cutToolStripMenuItem.Enabled = elementSelected;
            this.duplicateToolStripMenuItem.Enabled = elementSelected;
            this.removeObjectToolStripMenuItem.Enabled = elementSelected;

            //this.btnBorderColor.Enabled = this.borderColorToolStripMenuItem.Enabled =
            //(elementSelected && selectedElements.PropertySupported(DrawableContainer.Property.LINECOLOR));
            //this.btnBackgroundColor.Enabled = this.backgroundColorToolStripMenuItem.Enabled =
            //(elementSelected && selectedElements.PropertySupported(DrawableContainer.Property.FILLCOLOR));
            //this.comboBoxThickness.Enabled = this.lineThicknessToolStripMenuItem.Enabled =
            //(elementSelected && selectedElements.PropertySupported(DrawableContainer.Property.THICKNESS));
            //this.btnArrowHeads.Enabled = this.arrowHeadsToolStripMenuItem.Enabled =
            //(elementSelected && selectedElements.PropertySupported(DrawableContainer.Property.ARROWHEADS));

            bool push = surface.CanPushSelectionDown();
            bool pull = surface.CanPullSelectionUp();
            this.arrangeToolStripMenuItem.Enabled = (push || pull);
            if (this.arrangeToolStripMenuItem.Enabled)
            {
                this.upToTopToolStripMenuItem.Enabled = pull;
                this.upOneLevelToolStripMenuItem.Enabled = pull;
                this.downToBottomToolStripMenuItem.Enabled = push;
                this.downOneLevelToolStripMenuItem.Enabled = push;
            }
        }

        #region Filesystem options

        private void SaveToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            Save();
        }

        private void BtnSaveClick(object sender, EventArgs e)
        {
            Save();
        }

        private void Save()
        {
            try
            {
                if (lastSaveFullPath != null)
                {
                    ImageOutput.Save(surface.GetImageForExport(), lastSaveFullPath);
                    updateStatusLabel("Image saved to %storagelocation%.".Replace("%storagelocation%", lastSaveFullPath), fileSavedStatusContextMenu);
                }
                else
                {
                    SaveAs();
                }
            }
            catch (System.Runtime.InteropServices.ExternalException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SaveAsToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            SaveAs();
        }

        private void SaveAs()
        {
            if (MyWorker != null)
            {
                MyWorker.ReportProgress(103, surface.GetImageForExport());
            }
        }

        private void CopyImageToClipboardToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            if (this.MyWorker != null)
            {
                this.MyWorker.ReportProgress(102, surface.GetImageForExport());
            }
        }

        private void BtnClipboardClick(object sender, EventArgs e)
        {
            this.CopyImageToClipboardToolStripMenuItemClick(sender, e);
        }

        private void PrintToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (this.MyWorker != null)
            {
                this.MyWorker.ReportProgress(101, surface.GetImageForExport());
            }
        }

        private void BtnPrintClick(object sender, EventArgs e)
        {
            PrintToolStripMenuItemClick(sender, e);
        }

        private void CloseToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            this.Close();
        }

        #endregion Filesystem options

        #region Drawing options

        private void CheckAllButtons(bool check)
        {
            btnCursor.Checked = btnRect.Checked = btnEllipse.Checked = btnLine.Checked = btnText.Checked = check;
        }

        private void BtnCursorClick(object sender, EventArgs e)
        {
            surface.DrawingMode = Surface.DrawingModes.None;
            CheckAllButtons(false);
            btnCursor.Checked = true;
        }

        private void BtnRectClick(object sender, EventArgs e)
        {
            surface.DrawingMode = Surface.DrawingModes.Rect;
            CheckAllButtons(false);
            btnRect.Checked = true;
        }

        private void BtnEllipseClick(object sender, EventArgs e)
        {
            surface.DrawingMode = Surface.DrawingModes.Ellipse;
            CheckAllButtons(false);
            btnEllipse.Checked = true;
        }

        private void BtnLineClick(object sender, EventArgs e)
        {
            surface.DrawingMode = Surface.DrawingModes.Line;
            CheckAllButtons(false);
            btnLine.Checked = true;
        }

        private void BtnTextClick(object sender, EventArgs e)
        {
            surface.DrawingMode = Surface.DrawingModes.Text;
            CheckAllButtons(false);
            btnText.Checked = true;
        }

        private void AddRectangleToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            BtnRectClick(sender, e);
        }

        private void AddEllipseToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            BtnEllipseClick(sender, e);
        }

        private void AddTextBoxToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            BtnTextClick(sender, e);
        }

        private void DrawLineToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            BtnLineClick(sender, e);
        }

        private void RemoveObjectToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            surface.RemoveSelectedElements();
        }

        private void BtnDeleteClick(object sender, EventArgs e)
        {
            RemoveObjectToolStripMenuItemClick(sender, e);
        }

        #endregion Drawing options

        #region Copy & paste options

        private void CutToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            if (surface.CutSelectedElements(MyWorker))
            {
                this.btnPaste.Enabled = true;
                this.pasteToolStripMenuItem.Enabled = true;
            }
        }

        private void BtnCutClick(object sender, System.EventArgs e)
        {
            CutToolStripMenuItemClick(sender, e);
        }

        private void CopyToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            if (surface.CopySelectedElements(this.MyWorker))
            {
                this.btnPaste.Enabled = true;
                this.pasteToolStripMenuItem.Enabled = true;
            }
        }

        private void BtnCopyClick(object sender, System.EventArgs e)
        {
            CopyToolStripMenuItemClick(sender, e);
        }

        private void PasteToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            surface.PasteElementFromClipboard();
        }

        private void BtnPasteClick(object sender, System.EventArgs e)
        {
            PasteToolStripMenuItemClick(sender, e);
        }

        private void DuplicateToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            surface.DuplicateSelectedElements();
        }

        #endregion Copy & paste options

        #region Element properties

        private void UpOneLevelToolStripMenuItemClick(object sender, EventArgs e)
        {
            surface.PullElementsUp();
        }

        private void DownOneLevelToolStripMenuItemClick(object sender, EventArgs e)
        {
            surface.PushElementsDown();
        }

        private void UpToTopToolStripMenuItemClick(object sender, EventArgs e)
        {
            surface.PullElementsToTop();
        }

        private void DownToBottomToolStripMenuItemClick(object sender, EventArgs e)
        {
            surface.PushElementsToBottom();
        }

        private void BtnBorderColorClick(object sender, System.EventArgs e)
        {
            SelectBorderColor();
        }

        private void SelectBorderColorToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            SelectBorderColor();
        }

        private void SelectBorderColor()
        {
            colorDialog.Color = surface.ForeColor;
            if (colorDialog.ShowDialog() != DialogResult.Cancel)
            {
                conf.Editor_ForeColor = colorDialog.Color;
                conf.Editor_RecentColors = colorDialog.RecentColors;
                conf.Save();
                surface.ForeColor = colorDialog.Color;

                Bitmap img = DrawColorButton(ColorType.Border);
                btnBorderColor.Image = img;
                borderColorToolStripMenuItem.Image = img;
            }
        }

        private void BtnBackColorClick(object sender, System.EventArgs e)
        {
            SelectBackgroundColor();
        }

        private void SelectBackgroundColorToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            SelectBackgroundColor();
        }

        private void SelectBackgroundColor()
        {
            colorDialog.Color = surface.BackColor;
            if (colorDialog.ShowDialog() != DialogResult.Cancel)
            {
                surface.BackColor = colorDialog.Color;
                conf.Editor_BackColor = colorDialog.Color;
                conf.Editor_RecentColors = colorDialog.RecentColors;
                conf.Save();

                Bitmap img = DrawColorButton(ColorType.Background);
                btnBackgroundColor.Image = img;
                backgroundColorToolStripMenuItem.Image = img;
            }
        }

        private void btnGradientColor_Click(object sender, EventArgs e)
        {
            SelectGradientColor();
        }

        private void gradientColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectGradientColor();
        }

        private void SelectGradientColor()
        {
            colorDialog.Color = surface.GradientColor;
            if (colorDialog.ShowDialog() != DialogResult.Cancel)
            {
                surface.GradientColor = colorDialog.Color;
                conf.Editor_GradientColor = colorDialog.Color;
                conf.Editor_RecentColors = colorDialog.RecentColors;
                conf.Save();

                Bitmap img = DrawColorButton(ColorType.Gradient);
                btnGradientColor.Image = img;
                gradientColorToolStripMenuItem.Image = img;
            }
        }

        public enum ColorType { Border, Background, Gradient, Preview }

        private Bitmap DrawColorButton(ColorType colorType)
        {
            Bitmap img = new Bitmap(18, 18);
            img = DrawCheckersPattern(new Size(18, 18), 5);
            Graphics g = Graphics.FromImage(img);

            Brush brush = Brushes.Transparent;
            if (colorType == ColorType.Background || (colorType == ColorType.Preview && surface.GradientType == "None"))
            {
                brush = new SolidBrush(surface.BackColor);
            }
            else if (colorType == ColorType.Gradient)
            {
                brush = new SolidBrush(surface.GradientColor);
            }
            else if (colorType == ColorType.Preview)
            {
                LinearGradientMode gradientMode = (LinearGradientMode)Enum.Parse(typeof(LinearGradientMode), surface.GradientType);
                brush = new LinearGradientBrush(new Rectangle(0, 0, 18, 18), surface.BackColor, surface.GradientColor, gradientMode);
            }
            g.FillRectangle(brush, new Rectangle(0, 0, 18, 18));

            if (colorType == ColorType.Border || colorType == ColorType.Preview)
            {
                g.DrawRectangle(new Pen(surface.ForeColor), new Rectangle(0, 0, 17, 17));
            }

            if (colorType != ColorType.Preview)
            {
                UpdateColorPreview();
            }

            return img;
        }

        private void UpdateColorPreview()
        {
            btnColorPreview.Image = DrawColorButton(ColorType.Preview);
        }

        private Bitmap DrawCheckersPattern(Size size, int boxSize)
        {
            Bitmap img = new Bitmap(size.Width, size.Height);
            Graphics g = Graphics.FromImage(img);
            Color color;
            for (int x = 0; x < size.Width; x += boxSize)
            {
                for (int y = 0; y < size.Height; y += boxSize)
                {
                    if ((x + y) % 2 == 0)
                    {
                        color = Color.LightGray;
                    }
                    else
                    {
                        color = Color.WhiteSmoke;
                    }
                    g.FillRectangle(new SolidBrush(color), new Rectangle(x, y, boxSize, boxSize));
                }
            }
            return img;
        }

        private void tscbGradientType_SelectedIndexChanged(object sender, EventArgs e)
        {
            surface.GradientType = (string)tscbGradientType.SelectedItem;
            conf.Editor_GradientType = (string)tscbGradientType.SelectedItem;
            conf.Save();

            UpdateColorPreview();
        }

        private void cbThickness_SelectedIndexChanged(object sender, EventArgs e)
        {
            ThicknessChanged(((ToolStripComboBox)sender).Text);
        }

        private void LineThicknessValueToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            ThicknessChanged(((ToolStripMenuItem)sender).Text);
        }

        private void ThicknessChanged(string value)
        {
            int number;
            if (Int32.TryParse(value, out number))
            {
                surface.Thickness = number;
                conf.Editor_Thickness = number;
                conf.Save();
            }
        }

        private Bitmap DrawArrowHeadsButton(ArrowHeads arrowHeads, Rectangle rect)
        {
            Bitmap img = new Bitmap(rect.Width, rect.Height);
            Graphics g = Graphics.FromImage(img);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            Pen pen = new Pen(Color.Black) { Width = 1 };

            AdjustableArrowCap aac = new AdjustableArrowCap(4, 5);
            if (arrowHeads == ArrowHeads.Start || arrowHeads == ArrowHeads.Both) pen.CustomStartCap = aac;
            if (arrowHeads == ArrowHeads.End || arrowHeads == ArrowHeads.Both) pen.CustomEndCap = aac;

            g.DrawLine(pen, 3, rect.Height / 2, rect.Width - 3, rect.Height / 2);
            return img;
        }

        private void ArrowHeadsStartPointToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            ArrowHeadsChanged(ArrowHeads.Start);
        }

        private void ArrowHeadsEndPointToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            ArrowHeadsChanged(ArrowHeads.End);
        }

        private void ArrowHeadsBothToolStripMenuItemClick(object sender, EventArgs e)
        {
            ArrowHeadsChanged(ArrowHeads.Both);
        }

        private void ArrowHeadsNoneToolStripMenuItemClick(object sender, EventArgs e)
        {
            ArrowHeadsChanged(ArrowHeads.None);
        }

        private void ArrowHeadsChanged(ArrowHeads arrowHeads)
        {
            surface.ArrowHead = arrowHeads;
            conf.Editor_ArrowHeads = arrowHeads;
            conf.Save();

            btnArrowHeads.Image = DrawArrowHeadsButton(arrowHeads, btnArrowHeads.ContentRectangle);
        }

        #endregion Element properties

        #region Help

        private void AboutToolStripMenuItemClick(object sender, System.EventArgs e)
        {
            new AboutForm().Show();
        }

        #endregion Help

        #region Image editor event handlers

        private void ImageEditorForm_Shown(object sender, EventArgs e)
        {
            this.BringToFront();
        }

        private void ImageEditorFormActivated(object sender, EventArgs e)
        {
            this.btnPaste.Enabled = false;
            this.pasteToolStripMenuItem.Enabled = false;
        }

        private void ImageEditorFormFormClosing(object sender, FormClosingEventArgs e)
        {
            if (surface.IsEdited())
            {
                if (AutoSave) //Auto save before close
                {
                    Save();
                }
                else //Prompt for save if image edited before close
                {
                    if (MessageBox.Show("Do you want to save changes to the image?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                    {
                        Save();
                    }
                }
            }

            conf.Editor_WindowSize = Size;
            conf.Save();
            GC.Collect();
        }

        private void ImageEditorFormKeyUp(object sender, KeyEventArgs e)
        {
            if (Keys.Escape.Equals(e.KeyCode))
            {
                BtnCursorClick(sender, e);
            }
            else if (Keys.R.Equals(e.KeyCode))
            {
                BtnRectClick(sender, e);
            }
            else if (Keys.E.Equals(e.KeyCode))
            {
                BtnEllipseClick(sender, e);
            }
            else if (Keys.L.Equals(e.KeyCode))
            {
                BtnLineClick(sender, e);
            }
            else if (Keys.T.Equals(e.KeyCode))
            {
                BtnTextClick(sender, e);
            }
        }

        #endregion Image editor event handlers

        #region Cursor key strokes

        protected override bool ProcessCmdKey(ref Message msg, Keys k)
        {
            surface.ProcessCmdKey(k);
            return base.ProcessCmdKey(ref msg, k);
        }

        #endregion Cursor key strokes

        #region Status label handling

        private void updateStatusLabel(string text, ContextMenuStrip contextMenu)
        {
            statusLabel.Text = text;
            statusStrip1.ContextMenuStrip = contextMenu;
        }

        private void updateStatusLabel(string text)
        {
            updateStatusLabel(text, null);
        }

        private void clearStatusLabel()
        {
            updateStatusLabel(null, null);
        }

        private void StatusLabelClicked(object sender, MouseEventArgs e)
        {
            ToolStrip ss = (StatusStrip)((ToolStripStatusLabel)sender).Owner;
            if (ss.ContextMenuStrip != null)
            {
                ss.ContextMenuStrip.Show(ss, e.X, e.Y);
            }
        }

        private void CopyPathMenuItemClick(object sender, EventArgs e)
        {
            Clipboard.SetText(lastSaveFullPath); // ok
        }

        private void OpenDirectoryMenuItemClick(object sender, EventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo("explorer");
            psi.Arguments = Path.GetDirectoryName(lastSaveFullPath);
            psi.UseShellExecute = false;
            Process p = new Process();
            p.StartInfo = psi;
            p.Start();
        }

        #endregion Status label handling
    }
}