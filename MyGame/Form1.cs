using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Media;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Windows;
using System.Management; 
using CoreAudioApi;


namespace MyGame
{


    
    public partial class Form1 : Form


    {   
        // <variables>
        //

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        private MMDevice device;
        private MMDeviceEnumerator DevEnum = new MMDeviceEnumerator();
        //

        int Xf, Yf, deltaX, deltaY, step = 0;
        static int dir = 0;
        static int zomDir = 0;
        static int zomDis;

        int curlvl = 0;
        public static double distance;
        int[,] data;
        ucItem[,] items;
        Point curPoint;

        



        // <variables>




        public Form1()
        {
            InitializeComponent();
        }


        


        private void Form1_Load(object sender, EventArgs e)      // load map
        {

            step=0;

            data = new int[16, 16];
            if(items==null)
                items = new ucItem[16, 16];

            string[] lvl = Config.Levels[curlvl].Split(",".ToCharArray());   // load image from number array like 00101010
            for (int i = 0; i < lvl.Length; i++)
            { 
                string tmp = lvl[i];

                for (int j = 0; j < tmp.Length; j++)
                {
                    if (tmp[j].Equals('3'))                         // find the (Xfinal,Yfinal) 终点
                    {
                        Yf = i;
                        Xf = j;
                    }

                    ucItem item=null;
       
                    if (items[i, j] == null)
                    {
                        item = new ucItem();
                        items[i, j] = item;
                    }
                    else
                    {
                        item = items[i, j];
                    }
                    data[i, j] = int.Parse(tmp[j].ToString());                    
                    
                    item.Tag = data[i, j].ToString();
                    item.OldValue = data[i, j];
                    item.Left = j * item.Width;
                    item.Top = i* item.Height ;
                    this.Controls.Add(item);
                    item.RefImage();
                    if (data[i, j] >= 6)      // 6,7,8,9  都是当前坐标 只是载入时的图片不一样 分为四个方向
                    {
                        curPoint = new Point();
                        curPoint.Y = i;
                        curPoint.X = j;
                    }
                }
            }

            getDir();                               //every time load map    calcu  dir / dis       
            ShowInfo();                             //                  update textbox
            textBox1.Focus();
            
        }

        private void ShowInfo()                // update outprint text in the text box
        {
            textBox1.Text = "x:" + curPoint.X + ",y:" + curPoint.Y + " "+zomDir + " " + zomDis;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)    //accept input then move
        {
            Point newPoint1=curPoint;
            Point newPoint2 = curPoint;
            int key = e.KeyValue;
            switch (key)
            {
                case 38://上  up
                    newPoint1.Y--;
                    newPoint2.Y -= 2;
                    data[curPoint.Y, curPoint.X] = 6;
                    GoNewPoint(newPoint1,newPoint2);
                    break;
                case 39://右 right
                    newPoint1.X++;
                    newPoint2.X+=2;
                    data[curPoint.Y, curPoint.X] = 9;
                    GoNewPoint(newPoint1,newPoint2);
                    break;
                case 40://下 down
                    newPoint1.Y++;
                    newPoint2.Y+=2;
                    data[curPoint.Y, curPoint.X] = 7;
                    GoNewPoint(newPoint1, newPoint2);
                    break;
                case 37://左 left
                    newPoint1.X--;
                    newPoint2.X-=2;
                    data[curPoint.Y, curPoint.X] = 8;
                    GoNewPoint(newPoint1, newPoint2);
                    break;
                case 32:
                    if (zomDis <= 2)
                    { 
                        //do something
                    }
                    break;

            }

            ShowInfo();                                              //  每次移动 everytime move update textbox
        }

        private void GoNewPoint(Point newPoint1,Point newPoint2)    //判定下一坐标是否合法 determin the next location is floor
        {
            step++;


            if (newPoint1.X < 0 || newPoint1.Y < 0) return;     
            int n1 = data[newPoint1.Y, newPoint1.X];
            int n2 = data[newPoint2.Y, newPoint2.X];
            if (n1 <= 1)                                //1 代表墙  如果撞墙 则不移动  1 for wall if move to wall return back
            {
                SoundWall();
                return;
            }
            if (n1 == 2)                                // 2 路 可以走      2 for floor can be walk
            {
                data[newPoint1.Y, newPoint1.X] = data[curPoint.Y, curPoint.X];
                RefImg(newPoint1);
                data[curPoint.Y, curPoint.X] = items[curPoint.Y, curPoint.X].OldValue;
                RefImg(curPoint);
                curPoint = newPoint1;  
        
                getDir();                             //走一步非终点的格子 之后重新计算 dir/dis
                                         // everytime move is not finish point  calcu dir/dis
                return;            
            }
            if (n1 == 3)                            //3  终点 
            {
                data[newPoint1.Y, newPoint1.X] = data[curPoint.Y, curPoint.X];
                RefImg(newPoint1);
                data[curPoint.Y, curPoint.X] = items[curPoint.Y, curPoint.X].OldValue;
                RefImg(curPoint);  
         
                CheckFinshed();
            }
      
        }

        public void CheckFinshed()
        {

            SoundTada();
            curlvl++;

     
            if (curlvl >= Config.Levels.Length)
              {
                System.Windows.Forms.MessageBox.Show("game complete!");
                System.Windows.Forms.Application.Exit();
               }
            Form1_Load(null, null);                 //载入下一关卡
            
        }


        void RefImg(Point p)                //刷新图片
        {
            items[p.Y, p.X].Tag = data[p.Y, p.X];
            items[p.Y, p.X].RefImage();           
        }

        #region //                  计算方向和距离
        public void getDir()
        {
            deltaX = Xf - curPoint.X;
            deltaY = Yf - curPoint.Y;
         
           if (curPoint.X == Xf && curPoint.Y > Yf)
            {
                //正上
                dir = 3;
                distance = curPoint.Y - Yf;
            }
            else if (curPoint.X == Xf && curPoint.Y < Yf)
            {
                //正下
                dir = 7;
                distance = Yf - curPoint.Y;
            }
            else if (curPoint.X > Xf && curPoint.Y == Yf)
            {
                //正左
                dir = 5;
                distance = curPoint.X - Xf;
            }
            else if (curPoint.X < Xf && curPoint.Y == Yf)
            {
                //正右
            //    angel = 0;
                dir = 1;
                distance = Xf - curPoint.X;
            }
            else if (deltaX > 0 && deltaY <0)
            {
               //1 右上    
                dir = 2;
                distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
            }
           else if (deltaX < 0 && deltaY < 0)
            {
                //2  左上
                dir = 4;
                distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
            }
           else if (deltaX < 0 && deltaY > 0)
            {
                //3  左下
                dir = 6;
                distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));

            }
           else if (deltaX > 0 && deltaY > 0)
            {
                //4 右下
                 dir = 8;
                 distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
            }


           double temp = 0.5-(distance / 20);
           SetVol(temp);
        }
        #endregion



        public static void PlaySound()      // base on dir, determin which sound file to play
        {
            if (dir == 1)
            {
                SoundRight();
            }
            else if (dir == 2)
            {
                SoundFrontRight();
            }
            else if (dir == 3)
            {
                SoundFront();
            }
            else if (dir == 4)
            {
                SoundFrontLeft();
            }
            else if (dir == 5)
            {
                SoundRight();
            }
            else if (dir == 6)
            {
                SoundBackLeft();
            }
            else if (dir == 7)
            {
                SoundBack();
            } 
            else if (dir == 8)
            {
                SoundBackRight();
            }


        }

        #region  //  音频文件
        public static void SoundTada()
        {
            string soundfile = @".\tada.wav";
            byte[] bt = File.ReadAllBytes(soundfile);
            var sound = new System.Media.SoundPlayer(soundfile);
            sound.Play();
            Thread.Sleep(500);
        }
        public static void SoundWall()
         {
             string soundfile = @".\wall.wav";
              byte[] bt = File.ReadAllBytes(soundfile);
              var sound = new System.Media.SoundPlayer(soundfile);
              sound.Play();
              Thread.Sleep(200);
         }

         public static void SoundBack()
         {
             string soundfile = @".\back.wav";
             byte[] bt = File.ReadAllBytes(soundfile);
             var sound = new System.Media.SoundPlayer(soundfile);
             sound.Play();
             Thread.Sleep(200);
         }

         public static void SoundBackLeft()
         {
             string soundfile = @".\back_left.wav";
             byte[] bt = File.ReadAllBytes(soundfile);
             var sound = new System.Media.SoundPlayer(soundfile);
             sound.Play();
             Thread.Sleep(200);
         }

         public static void SoundBackRight()
         {
             string soundfile = @".\back_right.wav";
             byte[] bt = File.ReadAllBytes(soundfile);
             var sound = new System.Media.SoundPlayer(soundfile);
             sound.Play();
             Thread.Sleep(200);
         }
         public static void SoundFront()
         {
             string soundfile = @".\front.wav";
             var sound = new System.Media.SoundPlayer(soundfile);
             sound.Play();
             Thread.Sleep(200);
         }

         public static void SoundFrontLeft()
         {
             string soundfile = @".\front_left.wav";
             byte[] bt = File.ReadAllBytes(soundfile);
             var sound = new System.Media.SoundPlayer(soundfile);
             sound.Play();
             Thread.Sleep(200);
         }

         public static void SoundFrontRight()
         {
             string soundfile = @".\front_right.wav";
             byte[] bt = File.ReadAllBytes(soundfile);
             var sound = new System.Media.SoundPlayer(soundfile);
             sound.Play();
             Thread.Sleep(200);
         }


         public static void SoundLeft()
         {
             string soundfile = @".\left.wav";
             byte[] bt = File.ReadAllBytes(soundfile);
             var sound = new System.Media.SoundPlayer(soundfile);
             sound.Play();
             Thread.Sleep(200);
         }

         public static void SoundRight()
         {
             string soundfile = @".\right.wav";
             byte[] bt = File.ReadAllBytes(soundfile);
             var sound = new System.Media.SoundPlayer(soundfile);
             sound.Play();
             Thread.Sleep(200);
         }

        #endregion//   音频文件




         public static void SoundLoop()
         {

             Thread.Sleep(500);
             while (true)
             {
                 PlaySound();
                 Thread.Sleep(3000);
             }
         }

         public void SetVol(double arg)
         {
             device = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
             device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)arg;
         }


         public static void ZombieLoop()
         {
             Random rand = new Random();

             zomDir = rand.Next(1, 5);
             zomDis = rand.Next(1, 10);   // 1-9 between

             while (zomDis != 0)
             {

//                 Console.WriteLine("zombie distance :" + zomDis + " direcition: " + zomDir);
                 zomDis--;
                 Thread.Sleep(1000);


             }


             Thread.Sleep(1000);
//             Console.WriteLine("zombie eat your brain :" + zomDis);

         }

       


















    }
}




