using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace CDD
{
    public partial class Form1 : Form
    {

        SerialPort sp = new SerialPort();
        int NumPack;            //包数
        int ImageSize;          //图片大小
        public SortedList<int, byte[]> ReliablephotoDataListOne = new SortedList<int, byte[]>();//用于存储二进制数据
        int packetsize=0;//每包的大小
        int count=0;
        string curFileNameOne;
        FileStream fs1;
        public delegate void TxtAppendHandler(string msg);


/*        public void writeFile(string content)
        {
            if (content.Substring(0, 14) == "55 48 23 55 52".Substring(0, 14))
            {
                string result1, result2, result3, result4;
                result1 = content.Substring(27, 2);
                result2 = content.Substring(30, 2);
                NumPack = Convert.ToInt32(result2 + result1, 16);
                textBox3.Text += ("分包数量:" + NumPack + "\n");

                result1 = content.Substring(15, 2);
                result2 = content.Substring(18, 2);
                result3 = content.Substring(21, 2);
                result4 = content.Substring(24, 2);

                ImageSize = Convert.ToInt32(result4 + result3 + result2 + result1, 16);
                textBox2.Text += ("图片大小:" + ImageSize + "字节\n");

                textBox3.Text = NumPack.ToString();
                textBox2.Text = ImageSize.ToString();
            }
        }
*/

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //初始化SerialPort对象
            sp.NewLine = "/r/n";
            sp.RtsEnable = true;
            //添加事件注册
            sp.DataReceived += sp_DataReceived;
        }




        public void setText(string msg)
        {//显示数据包总数和图像大小
            string[] data = msg.Split(' ');
            textBox3.Text = data[0];//packetCount.ToString();
            textBox2.Text = data[1];
            NumPack = Convert.ToInt32(data[0]);
            ImageSize = Convert.ToInt32(data[1]);
        }
        /// <summary>
        /// 串口接收数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sp_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            System.Threading.Thread.Sleep(100);//延时1ms，因为一次串口发送的最大数据包是80字节，在波特率为38400时，串口传输时间大概为17ms,再加上处理时间，不会超过20ms.

            byte[] imageData = new byte[packetsize + 9];
            byte[] packet = new byte[packetsize];
            byte[] command = new byte[5];
            byte[] commandImgInfo = new byte[12];
            command[0] = Convert.ToByte(sp.ReadByte());
            command[1] = Convert.ToByte(sp.ReadByte());
            if (command[0] == 'U' && command[1] == 'H')
            {//R数据帧1个字节(’U’) +1个字节(’R’) + 4个字节(图像大小) + 2个字节(分包数量) +1个字节(’#’)；
                sp.Read(commandImgInfo, 0, 10);
                NumPack = (commandImgInfo[8] << 8) | commandImgInfo[7];//分包数量
                ImageSize = (commandImgInfo[6] << 24) | (commandImgInfo[5] << 16) | (commandImgInfo[4] << 8) | commandImgInfo[3];//图像大小

                TxtAppendHandler deltxt = new TxtAppendHandler(setText);//使用委托机制处理线程问题
                this.Invoke(deltxt, new object[] { NumPack.ToString() + " " + ImageSize.ToString() });
            }
            else if (command[0] == 'U' && command[1] == 'E')
            {//F数据帧1个字节(’U’) +1 个字节(’F’) + 2个字节(包号) + 2个字节(包长度) + N 个字节(图像数据) + 2个字节(校验和)；
                sp.Read(imageData, 0, packetsize + 9);
                count = (imageData[4] << 8) | (imageData[3]);
            //    textBox4.Text = Convert.ToString(count);
                for (int i = 7; i < imageData.Length - 2; i++)
                {
                    packet[i - 7] = imageData[i];
                }
                if (count <= NumPack)
                {
                    ReliablephotoDataListOne.Add(count, packet);//将每次的数据包都存储起来
                }
     //           TxtAppendHandler deltxt = new TxtAppendHandler(showCountText);//使用委托机制处理线程问题
     //           this.Invoke(deltxt, new object[] { count.ToString() });
            }
            else
            {//不做处理
            }
            if (count == NumPack)
            { 
                try
                {
                    curFileNameOne = GblDefine.curPicPathName + "\\" + DateTime.Now.ToString("yyyy-M-d H-m-s") + ".jpg";

                    fs1 = new FileStream(curFileNameOne, FileMode.Create);//存储图片
                    BinaryWriter w1 = new BinaryWriter(fs1);

                    for (int i = 1; i <= ReliablephotoDataListOne.Count; i++)
                    {
                        w1.Write(ReliablephotoDataListOne[i]);
                    }
                    w1.Flush();
                    fs1.Flush();
                    w1.Close();
                    fs1.Close();
                    FileInfo fi1 = new FileInfo(curFileNameOne);
                    if (fi1.Length == 0)
                    {
                        MessageBox.Show("Image collect failed！");
                    }
                    else
                    {
                        MessageBox.Show("Image collect end！");
                        pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
                        pictureBox1.Image = Image.FromFile(curFileNameOne, false);
                    }
                    ReliablephotoDataListOne.Clear();//清空数据
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void showCountText(string msg)
        {//显示目前已经获取到的数据包数量
     //       nodeNum.Text = msg;
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void chuankou_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            sp.PortName = chuankou.Text;//设置端口号
            sp.BaudRate = Convert.ToInt32(bote.Text);//设置比特率
            sp.DataBits = Convert.ToInt32(shuju.Text);//数据位
            sp.Parity = (Parity)Enum.Parse(typeof(Parity), jiaoyan.SelectedItem.ToString());//设置奇偶校验位
            switch (tingzhi.Text)//停止位
            {
                case "1":
                    sp.StopBits = StopBits.One;
                    break;
                case "2":
                    sp.StopBits = StopBits.Two;
                    break;
            }
            try
            {
                sp.Open();
                sp.RtsEnable = true;
                MessageBox.Show("串口已打开！");
            }
            catch (Exception ex)
            {
                //tssStatus.Text = "串口状态：打开串口失败！";
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 字符串转16进制字节数组
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        private static byte[] GetBytes(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int size = Convert.ToInt32(textBox1.Text);
            packetsize = size;
            string stemp = Convert.ToString(size, 16);
            string temp = null;
            byte[] Image1 = { 0x55, 0x48, 0x31, 0x00, 0x00, 0x23 };//160*128像素图像
            byte[] Image2 = { 0x55, 0x48, 0x32, 0x00, 0x00, 0x23 };//320*240像素图像
            byte[] Image3 = { 0x55, 0x48, 0x33, 0x00, 0x00, 0x23 };//640*480像素图像
            if (stemp.Length == 1)
            {
                temp = "0" + stemp;
            }
            else
            {
                temp = stemp;
            }
            byte[] ps = GetBytes(temp);
            if (radioButton1.Checked)
            {
                Image1[3] = ps[0];//设置数据分包大小
                if (size > 255)
                {
                    Image1[4] = ps[1];
                }
                sp.Write(Image1, 0, Image1.Length);//发送采集命令
            }
            else if (radioButton2.Checked)
            {
                Image2[3] = ps[0];//设置数据分包大小
                if (size > 255)
                {
                    Image2[4] = ps[1];
                }
                sp.Write(Image2, 0, Image2.Length);//发送采集命令
            }
            else if (radioButton3.Checked)
            {
                Image3[3] = ps[0];//设置数据分包大小
                if (size > 255)
                {
                    Image3[4] = ps[1];
                }
                sp.Write(Image3, 0, Image3.Length);//发送采集命令
            }
            else
            {

            }
            Thread.Sleep(150);//摄像头接到拍图命令后，需耗时 T p ，该延时最短为150ms
        }



       

        public void getImagePacket()
        {//得到图像的所有分包
            for (int i = 1; i <= NumPack; i++)
            {
                string temp = Convert.ToString(i, 16);
                string stemp = null;
                if (temp.Length == 1)
                {
                    stemp = "000" + temp;
                }
                else if (temp.Length == 2)
                {
                    stemp = "00" + temp;
                }
                else if (temp.Length == 3)
                {
                    stemp = "0" + temp;
                }
                else
                {
                    stemp = temp;
                }
                byte[] packetID = GetBytes(stemp);
                byte[] Require_Image = { 0x55, 0x45, 0x01, 0x00, 0x23 };//请求图像数据包
                Require_Image[2] = packetID[1];
                Require_Image[3] = packetID[0];

                sp.Write(Require_Image, 0, Require_Image.Length);//发送采集命令
                Thread.Sleep(packetsize * 8 / sp.BaudRate + 100);//每次至少延时每个数据包的最小传送时间
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.Threading.Thread th1 = new Thread(new ThreadStart(getImagePacket));
            th1.Start();
        }

        






    }
    class GblDefine
    {
        public static string curPicPathName = "d:\\";
    }
}