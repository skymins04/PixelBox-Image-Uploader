using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PixelBox
{
    public partial class Form1 : Form
    {
        List<string> imgList = new List<string>();
        const int MAX_IMG = 6;
        int imgCount = 0;

        public Form1()
        {
            InitializeComponent();
            UploadBtn.Enabled = false;
        }

        private void DelImgBtn_Click(object sender, EventArgs e)
        {
            int idx = listBox1.SelectedIndex;
            object ob = listBox1.SelectedItem;
            if (!(idx == -1))
            {
                listBox1.Items.Remove(ob);
                imgList.Remove(imgList[idx]);

                imgCount -= 1;
                if (imgCount == 0) UploadBtn.Enabled = false;
            }
        }

        private void UpBtn_Click(object sender, EventArgs e)
        {
            int idx = listBox1.SelectedIndex;
            if(idx >= 1)
            {
                object obj = listBox1.SelectedItem;
                string imgPath = imgList[idx];

                imgList.Remove(imgList[idx]);
                imgList.Insert(idx - 1, imgPath);

                listBox1.Items.Remove(listBox1.SelectedItem);
                listBox1.Items.Insert(idx - 1, obj);
                listBox1.SetSelected(idx - 1, true);
            }
        }

        private void DownBtn_Click(object sender, EventArgs e)
        {
            int idx = listBox1.SelectedIndex;
            if (!(idx == listBox1.Items.Count-1 || listBox1.SelectedItem == null))
            {
                object obj = listBox1.SelectedItem;
                string imgPath = imgList[idx];

                imgList.Remove(imgList[idx]);
                imgList.Insert(idx + 1, imgPath);

                listBox1.Items.Remove(listBox1.SelectedItem);
                listBox1.Items.Insert(idx + 1, obj);
                listBox1.SetSelected(idx + 1, true);
            }
        }

        private void AddImgBtn_Click(object sender, EventArgs e)
        {
            if(imgCount < 6)
            {
                //다이얼로그 호출
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.DefaultExt = "png";
                openFile.Filter = "PNG 이미지(*.png; *.PNG)|*.png;*.PNG";
                openFile.ShowDialog();

                if (openFile.FileName.Length > 0)
                {
                    Image img = Image.FromFile(openFile.FileName);

                    // 32x32px 검사
                    if (!(img.Height == 32 && img.Width == 32))
                    {
                        if (MessageBox.Show("선택한 이미지는 32x32(px) 크기가 아닙니다.\n이미지의 크기를 조정하시겠습니까?", "크기가 맞지 않는 이미지", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            int number;
                            using (TextReader reader = File.OpenText(".\\resized\\resizedNumber.txt"))
                            {
                                // 파일 번호 가져오기
                                number = int.Parse(reader.ReadLine()) + 1;

                                // 이미지 리사이즈
                                Bitmap tmp = new Bitmap(img, new Size(32, 32));
                                tmp.Save(".\\resized\\resized_" + number.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                                listBox1.Items.Add(Path.GetFileName(openFile.FileName));
                                imgList.Add(Application.StartupPath + ".\\resized\\resized_" + number.ToString() + ".png");                               
                            }
                            // 다음 파일 번호 반영
                            StreamWriter sw = new StreamWriter(".\\resized\\resizedNumber.txt", false);
                            sw.WriteLine(number.ToString());
                            sw.Close();
                        }
                        else return;
                    }
                    else
                    {
                        listBox1.Items.Add(Path.GetFileName(openFile.FileName));
                        imgList.Add(openFile.FileName);
                    }

                    if (comboBox1.SelectedIndex != -1) UploadBtn.Enabled = true;
                    imgCount += 1;
                }
            }
            else
            {
                SystemSounds.Beep.Play();
                MessageBox.Show("이미지는 최대 6장까지만\n업로드 할 수 있습니다.", "이미지 개수 초과");
            }
        }

        private void comboBox1_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();

            // COM 포트 리스트 반영
            string[] comlist = System.IO.Ports.SerialPort.GetPortNames();
            if (comlist.Length > 0) comboBox1.Items.AddRange(comlist);
        }

        private void UploadBtn_Click(object sender, EventArgs e)
        {
            // png2ino 스크립트 실행
            ProcessStartInfo png2ino = new ProcessStartInfo();
            ProcessStartInfo arduinoUpload = new ProcessStartInfo();
            Process p = new Process();

            string png2inoArg = null;
            foreach (string path in imgList){
                png2inoArg += " " + path;
            }

            png2ino.CreateNoWindow = true;
            png2ino.UseShellExecute = false;
            png2ino.FileName = ".\\rgbmatrix\\png2ino.exe";
            png2ino.WindowStyle = ProcessWindowStyle.Hidden;
            png2ino.Arguments = png2inoArg;

            p.StartInfo = png2ino;
            p.Start();
            p.WaitForExit();

            // 메트릭스 데이터 헤더 파일 이동
            if(File.Exists(".\\rgbmatrix\\data.h")) File.Delete(".\\rgbmatrix\\data.h");
            File.Move(".\\data.h", ".\\rgbmatrix\\data.h");

            // 지연시간 반영
            StreamWriter sw = new StreamWriter(".\\rgbmatrix\\delay.h", false);
            sw.WriteLine("const int delayVal = "+numericUpDown1.Value.ToString()+";          ");
            sw.Close();

            // 아두이노 업로드 실행
            arduinoUpload.CreateNoWindow = false;
            arduinoUpload.UseShellExecute = false;
            arduinoUpload.FileName = ".\\arduino-1.8.9\\arduino_debug.exe";
            arduinoUpload.WindowStyle = ProcessWindowStyle.Hidden;
            arduinoUpload.Arguments = "--board arduino:avr:nano --port " + comboBox1.Text + " -v --upload .\\rgbmatrix\\rgbmatrix.ino";

            p.StartInfo = arduinoUpload;
            p.Start();
            p.WaitForExit();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != -1 && listBox1.Items.Count != 0) UploadBtn.Enabled = true;
        }
    }
}
