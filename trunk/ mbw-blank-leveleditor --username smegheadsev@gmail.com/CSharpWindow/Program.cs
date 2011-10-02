using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CSharpWindow
{
    static class Program
    {
        #region DLL Functions
        
        [DllImport("PInvoke.dll")]
        public static extern void _init(Int32 handle, int _width, int _height);

        [DllImport("PInvoke.dll")]
        public static extern void _main();

        [DllImport("PInvoke.dll")]
        public static extern void _shutdown();  

        #endregion 

        [STAThread]
        static void Main()
        {
            // Init assets for the application
            Form1 _mainForm = new Form1();
            LogoWindow _logoForm = new LogoWindow();
            int lastTickCount = System.Environment.TickCount;
            int elapsedTime = 0;
            Image GPGLogo   = Image.FromFile("../Assets/Textures/Logo_GPG.png");
            Image WwiseLogo = Image.FromFile("../Assets/Textures/Logo_Wwise.png");
            Image JKBLogo   = Image.FromFile("../Assets/Textures/Logo_JKB.png");
            Image MBWLogo   = Image.FromFile("../Assets/Textures/Logo_MBW.png");
            
            // Init the engines and show windows
            _init(_mainForm.panel1.Handle.ToInt32(), _mainForm.panel1.Width, _mainForm.panel1.Height); 
            _mainForm.Show();
            //_logoForm.Show();
        

            // Opening credit screen
            for (int i = 0; i < 4; i++, elapsedTime = 0)
            {
                if(i == 0)
                    _logoForm.BackgroundImage = GPGLogo;
                else if(i == 1)
                    _logoForm.BackgroundImage = WwiseLogo;
                else if(i == 2)
                    _logoForm.BackgroundImage = JKBLogo;
                else
                    _logoForm.BackgroundImage = MBWLogo;

                while (elapsedTime < 1500) 
                {
                    Application.DoEvents();
                    _main();
                    elapsedTime += System.Environment.TickCount - lastTickCount;
                    lastTickCount = System.Environment.TickCount;
                }
            }

            // Hide logo form
            _logoForm.Dispose();

            // Main game loop
            while (_mainForm.Created)
            {
                _main();
                Application.DoEvents();
            }

            // Shutdown loaded in images
            GPGLogo.Dispose();
            WwiseLogo.Dispose();
            MBWLogo.Dispose();
            JKBLogo.Dispose();

            _shutdown();
        }
    }
}
