using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlateRecognation
{
    internal class DisplayManager
    {
        public static void PictureBoxInvoke(PictureBox pictureBox, Bitmap bitmap)
        {
            if (pictureBox.InvokeRequired)
                pictureBox.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    if (pictureBox.Image != null)
                    {
                        pictureBox.Image.Dispose(); // Önceki resmi temizle
                        pictureBox.Image = null;
                    }
                      


                    pictureBox.Image = bitmap;
                });
            else
            {

                if (pictureBox.Image != null)
                {
                    pictureBox.Image.Dispose(); // Önceki resmi temizle
                    pictureBox.Image = null;
                }

                pictureBox.Image = bitmap;
            }
        }

        public static void PictureBoxImageInvoke(PictureBox pictureBox, Image image)
        {
            if (pictureBox.InvokeRequired)
                pictureBox.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    pictureBox.Image = image;
                });
            else
            {
                pictureBox.Image = image;
            }
        }

        public static void PictureBoxInvoke(PictureBox pictureBox, Bitmap bitmap, PictureBox pictureBox1, Bitmap bitmap1)
        {
            if (pictureBox.InvokeRequired)
                pictureBox.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    if (pictureBox.Image != null)
                    {
                        pictureBox.Image.Dispose(); // Önceki resmi temizle
                        pictureBox.Image = null;
                    }

                   


                    pictureBox.Image = bitmap;
                });
            else
            {

                if (pictureBox.Image != null)
                {
                    pictureBox.Image.Dispose(); // Önceki resmi temizle
                    pictureBox.Image = null;
                }

                pictureBox.Image = bitmap;
            }

            if (pictureBox1.InvokeRequired)
                pictureBox1.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    if (pictureBox1.Image != null)
                    {
                        pictureBox1.Image.Dispose(); // Önceki resmi temizle
                        pictureBox1.Image = null;
                    }



                    pictureBox1.Image = bitmap1;
                });
            else
            {

                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose(); // Önceki resmi temizle
                    pictureBox1.Image = null;
                }

                pictureBox1.Image = bitmap1;
            }
        }

        public static void ButtonBoxInvoke(Button button, string text)
        {
            if (button.InvokeRequired)
                button.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    button.Text = text;
                });
            else
            {
                button.Text = text;
            }
        }

        public static void ButtonBoxBackgroundImageInvoke(Button button, Image image)
        {
            if (button.InvokeRequired)
                button.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    button.BackgroundImage = image;
                });
            else
            {
                button.BackgroundImage = image;
            }
        }

        public static void TextBoxInvoke(TextBox textBox, string text)
        {
            if (textBox.InvokeRequired)
                textBox.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    textBox.Text = text;
                });
            else
            {
                textBox.Text = text;
            }
        }


        public static void LabelInvoke(Label label, string text)
        {
            if (label.InvokeRequired)
                label.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    label.Text = text;
                });
            else
            {
                label.Text = text;
            }
        }


        public static void LabelInvoke(Label label, string text, int clearDelayMilliseconds = 3000)
        {
            if (label.InvokeRequired)
                label.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    SetLabelWithAutoClear(label, text, clearDelayMilliseconds);
                });
            else
            {
                SetLabelWithAutoClear(label, text, clearDelayMilliseconds);
            }
        }


        //private static async void SetLabelWithAutoClear(Label label, string text, int delay)
        //{
        //    label.Text = text;
        //    await Task.Delay(delay);
        //    label.Text = string.Empty;
        //}

        private static readonly object labelLock = new object();

        private static void SetLabelWithAutoClear(Label label, string text, int delay)
        {
            lock (labelLock) // Her label için ayrı lock yazabilirsin
            {
                label.Text = text;

                // Eski bir timer varsa iptal edelim (etiketlenmişse)
                if (label.Tag is System.Windows.Forms.Timer existingTimer)
                {
                    existingTimer.Stop();
                    existingTimer.Dispose();
                }

                var timer = new System.Windows.Forms.Timer();
                timer.Interval = delay;
                timer.Tick += (s, e) =>
                {
                    label.Text = string.Empty;
                    timer.Stop();
                    timer.Dispose();
                    label.Tag = null;
                };

                label.Tag = timer; // Timer'ı etiket olarak sakla, varsa sonradan iptal et
                timer.Start();
            }
        }

        //public static void TransparentLabelInvoke(TransparentLabel label, string text)
        //{
        //    if (label.InvokeRequired)
        //        label.Invoke((System.Windows.Forms.MethodInvoker)delegate
        //        {
        //            label.Text = text;
        //        });
        //    else
        //    {
        //        label.Text = text;
        //    }
        //}

        public static void RichTextBoxInvoke(RichTextBox richTextBox, string text)
        {
            if (richTextBox.InvokeRequired)
                richTextBox.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    richTextBox.AppendText(text + "\n", Color.Black);
                });
            else
            {
                richTextBox.AppendText(text + "\n", Color.Black);
            }
        }

        public static void RichTextBoxInvokeWithLine(RichTextBox richTextBox, string text)
        {
            if (richTextBox.InvokeRequired)
                richTextBox.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    richTextBox.AppendText("=========================================" + "\n", Color.Black); ;
                    richTextBox.AppendText(text + "\n", Color.Black);
                    richTextBox.AppendText("=========================================" + "\n", Color.Black);
                });
            else
            {
                richTextBox.AppendText("=========================================" + "\n", Color.Black); ;
                richTextBox.AppendText(text + "\n", Color.Black);
                richTextBox.AppendText("=========================================" + "\n", Color.Black);
            }
        }


        public static void RichTextBoxWithAppendLineInvoke(RichTextBox richTextBox, string text, Color color)
        {
            if (richTextBox.InvokeRequired)
                richTextBox.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    richTextBox.AppendText("****************************************************" + "\n", color);
                    richTextBox.AppendText(text + "\n", color);
                    richTextBox.AppendText("****************************************************" + "\n", color);
                });
            else
            {
                richTextBox.AppendText("****************************************************" + "\n", color);
                richTextBox.AppendText(text + "\n", color);
                richTextBox.AppendText("****************************************************" + "\n", color);
            }
        }

        public static int DataGridViewAddRowInvoke(DataGridView dataGridView)
        {
            int addingNewRow = -1;


            if (dataGridView.InvokeRequired)
                dataGridView.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    addingNewRow = dataGridView.Rows.Add();
                });
            else
            {
                addingNewRow = dataGridView.Rows.Add();
            }
            return addingNewRow;

        }



        public static int DataGridViewAddRowPlateResultInvoke(DataGridView dataGridView, Bitmap frame, Bitmap PlateImage, string ReadingResult, double Probability)
        {
            int addingNewRow = -1;


            if (dataGridView.InvokeRequired)
                dataGridView.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    //addingNewRow = dataGridView.Rows.Add(plateResult.plate, plateResult.segmented, plateResult.threshould, plateResult.readingPlateResult, plateResult.readingPlateResultProbability);

                    //Bitmap osman = new Bitmap(frame);

                    //string path = "D:\\Ahmet";

                    //osman.Save(path + "\\" + ReadingResult + DateTime.Now.ToLongDateString() + "_" +
                    //                                      DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" +
                    //                                      DateTime.Now.Millisecond.ToString() + ".jpg");


                    addingNewRow = dataGridView.Rows.Add(PlateImage.Clone(), ReadingResult, Probability);



                    int lastRowIndex = dataGridView.Rows.Count - 1;
                    dataGridView.Rows[lastRowIndex].Selected = true;

                    DataGridViewFirstDisplayedScrollingRowIndexInvoke(dataGridView, lastRowIndex);


                });
            else
            {
                //addingNewRow = dataGridView.Rows.Add(plateResult.plate, plateResult.segmented, plateResult.threshould, plateResult.readingPlateResult, plateResult.readingPlateResultProbability);


                //Bitmap osman = new Bitmap(frame);

                //string path = "D:\\Ahmet";

                //osman.Save(path + "\\" + ReadingResult + DateTime.Now.ToLongDateString() + "_" +
                //                                      DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" +
                //                                      DateTime.Now.Millisecond.ToString() + ".jpg");


                addingNewRow = dataGridView.Rows.Add(PlateImage.Clone(), ReadingResult, Probability);



                int lastRowIndex = dataGridView.Rows.Count - 1;
                dataGridView.Rows[lastRowIndex].Selected = true;

                DataGridViewFirstDisplayedScrollingRowIndexInvoke(dataGridView, lastRowIndex);


            }
            return addingNewRow;

        }


        public static int DataGridViewAddRowPlateResultInvoke(DataGridView dataGridView, Bitmap PlateImage, string ReadingResult, double Probability)
        {
            int addingNewRow = -1;


            if (dataGridView.InvokeRequired)
                dataGridView.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    //addingNewRow = dataGridView.Rows.Add(plateResult.plate, plateResult.segmented, plateResult.threshould, plateResult.readingPlateResult, plateResult.readingPlateResultProbability);

                    
                    addingNewRow = dataGridView.Rows.Add(PlateImage.Clone(), ReadingResult, Probability);



                    int lastRowIndex = dataGridView.Rows.Count - 1;
                    dataGridView.Rows[lastRowIndex].Selected = true;

                    DataGridViewFirstDisplayedScrollingRowIndexInvoke(dataGridView, lastRowIndex);


                });
            else
            {
                //addingNewRow = dataGridView.Rows.Add(plateResult.plate, plateResult.segmented, plateResult.threshould, plateResult.readingPlateResult, plateResult.readingPlateResultProbability);

                addingNewRow = dataGridView.Rows.Add(PlateImage.Clone(), ReadingResult, Probability);



                int lastRowIndex = dataGridView.Rows.Count - 1;
                dataGridView.Rows[lastRowIndex].Selected = true;

                DataGridViewFirstDisplayedScrollingRowIndexInvoke(dataGridView, lastRowIndex);


            }
            return addingNewRow;

        }

        public static int DataGridViewAddRowPlateResultInvoke(DataGridView dataGridView, PlateResult plateResult)
        {
            int addingNewRow = -1;


            if (dataGridView.InvokeRequired)
                dataGridView.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    //addingNewRow = dataGridView.Rows.Add(plateResult.plate, plateResult.segmented, plateResult.threshould, plateResult.readingPlateResult, plateResult.readingPlateResultProbability);

                    addingNewRow = dataGridView.Rows.Add(plateResult.plate,  plateResult.readingPlateResult, plateResult.readingPlateResultProbability);
                });
            else
            {
                //addingNewRow = dataGridView.Rows.Add(plateResult.plate, plateResult.segmented, plateResult.threshould, plateResult.readingPlateResult, plateResult.readingPlateResultProbability);

                addingNewRow = dataGridView.Rows.Add(plateResult.plate,  plateResult.readingPlateResult, plateResult.readingPlateResultProbability);
            }
            return addingNewRow;

        }

        public static void DataGridViewRowClearInvoke(DataGridView dataGridView)
        {

            if (dataGridView.InvokeRequired)
                dataGridView.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    dataGridView.Rows.Clear();
                });
            else
            {
                dataGridView.Rows.Clear();
            }


        }

        public static int DataGridViewFirstDisplayedScrollingRowIndexInvoke(DataGridView dataGridView, int rowIndex)
        {
            int addingNewRow = -1;


            if (dataGridView.InvokeRequired)
                dataGridView.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    addingNewRow = dataGridView.FirstDisplayedScrollingRowIndex = rowIndex;
                });
            else
            {
                addingNewRow = dataGridView.FirstDisplayedScrollingRowIndex = rowIndex;
            }


            return addingNewRow;

        }

        public static int ComboBoxSelectedIndexInvoke(ComboBox comboBox, int selectedIndex)
        {
            int isItemSelectedIndex = -1;


            if (comboBox.InvokeRequired)
                comboBox.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    isItemSelectedIndex = (comboBox.SelectedIndex = selectedIndex);
                });
            else
            {
                isItemSelectedIndex = (comboBox.SelectedIndex = selectedIndex);
            }
            return isItemSelectedIndex;

        }

        public static void SetDoubleBuffered(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, control, new object[] { true });
        }
    }
}
