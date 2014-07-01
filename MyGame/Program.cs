using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace MyGame
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        //[STAThread]


        static void Main()
        {
            

            Thread t = new Thread(Form1.SoundLoop);
            t.Start();
            Thread z = new Thread(Form1.ZombieLoop);
            z.Start();



            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            t.Abort();
            z.Abort();

        }



   
    }
}

