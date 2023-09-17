using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MIBVirus
{
    public partial class ScreenFX : Form
    {
        [DllImport("MAGNIFICATION.dll")]
        internal static extern bool MagInitialize();
        [DllImport("MAGNIFICATION.dll")]
        internal static extern bool MagUninitialize();
        [DllImport("MAGNIFICATION.dll")]
        internal static extern bool MagSetFullscreenTransform(float magLevel, int xOffset, int yOffset);
        [DllImport("MAGNIFICATION.dll", EntryPoint = "#1")]
        private static extern long MagSetFullscreenUseBitmapSmoothing(int value);
        [DllImport("SetScreenEffect.dll")]
        private static extern void SetScreenRot(int grados);
        [DllImport("SetScreenEffect64.dll", EntryPoint = "SetScreenRot")]
        private static extern void SetScreenRot64(int grados);
        [DllImport("SetScreenEffect.dll")]
        private static extern void SetScreenEffect(
            float mul11, float mul12, float mul13,
            float mul21, float mul22, float mul23,
            float mul31, float mul32, float mul33);
        [DllImport("SetScreenEffect64.dll", EntryPoint = "SetScreenEffect")]
        private static extern void SetScreenEffect64(
            float mul11, float mul12, float mul13,
            float mul21, float mul22, float mul23,
            float mul31, float mul32, float mul33);
        [DllImport("SetScreenEffect.dll")]
        private static extern void SetScreenFade(float fade);
        [DllImport("SetScreenEffect64.dll", EntryPoint = "SetScreenFade")]
        private static extern void SetScreenFade64(float fade);
        private static void SetScreenEffectPtr(
            float mul11, float mul12, float mul13,
            float mul21, float mul22, float mul23,
            float mul31, float mul32, float mul33)
        {
            if (IntPtr.Size > 4)
                SetScreenEffect64(
                    mul11, mul12, mul13,
                    mul21, mul22, mul23,
                    mul31, mul32, mul33);
            else
                SetScreenEffect(
                    mul11, mul12, mul13,
                    mul21, mul22, mul23,
                    mul31, mul32, mul33);
        }
        private static void SetScreenRotPtr(int grados)
        {
            if (IntPtr.Size > 4)
                SetScreenRot64(grados);
            else
                SetScreenRot(grados);
        }
        private static void SetScreenFadePtr(float fade)
        {
            if (IntPtr.Size > 4)
                SetScreenFade64(fade);
            else
                SetScreenFade(fade);
        }
        public ScreenFX()
        {
            InitializeComponent();
            MagInitialize();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                int releaseId = 0;
                bool result = int.TryParse(Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "ReleaseId", 0).ToString(), out releaseId);
                if (!result)
                    releaseId = 0;
                if ((Environment.OSVersion.Version.Major >= 10 & releaseId >= 1803))
                {
                    if (Environment.OSVersion.Version.Build >= 16215)
                    {
                        MagSetFullscreenUseBitmapSmoothing(1);
                    }
                }
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams baseParams = base.CreateParams;

                const int WS_EX_NOACTIVATE = 0x08000000;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                baseParams.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;

                return baseParams;
            }
        }

        private void ScreenFX_FormClosed(object sender, FormClosedEventArgs e)
        {
            MagUninitialize();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                int releaseId = 0;
                bool result = int.TryParse(Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "ReleaseId", 0).ToString(), out releaseId);
                if (!result)
                    releaseId = 0;
                if ((Environment.OSVersion.Version.Major >= 10 & releaseId >= 1803))
                {
                    if (Environment.OSVersion.Version.Build >= 16215)
                    {
                        MagSetFullscreenUseBitmapSmoothing(0);
                    }
                }
            }
        }

        private void SetZoom(float z)
        {
            Zoom = z;
            float x = Screen.PrimaryScreen.Bounds.Left;
            float y = Screen.PrimaryScreen.Bounds.Top;
            float w = Screen.PrimaryScreen.Bounds.Width;
            float h = Screen.PrimaryScreen.Bounds.Height;
            float w2 = w * Zoom;
            float h2 = h * Zoom;
            float w3 = w2 - w;
            float h3 = h2 - h;
            float w4 = w3 / Zoom / 2;
            float h4 = h3 / Zoom / 2;
            if (BadZoom)
            {
                w4 = w3 / 2;
                h4 = h3 / 2;
            }
            float w5 = w4;
            float h5 = h4;
            if (timer3.Enabled & !BadZoom )
            {
                float w22 = w * zoomin;
                float h22 = h * zoomin;
                float w32 = w22 - w;
                float h32 = h22 - h;
                float w42 = w32 / zoomin / 2;
                float h42 = h32 / zoomin / 2;
                w5 = w4 + w42 * offsetx;
                h5 = h4 + w42 * offsety;
            }
            MagSetFullscreenTransform(Zoom, (int)x + (int)w5, (int)y + (int)h5);
        }

        private float QuadraticEaseOut(float t, float b, float c, float d)
        {
            t /= d;
            return -c * t * (t - 2) + b;
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (!stopped & enabled)
            {
                t = (MainWindow.sw.ElapsedMilliseconds - MainWindow.startOffset) % MainWindow.beatInterval;
                float t2 = t / MainWindow.beatInterval;
                if (t2 >= 0.5f & scheduledStopTimer)
                {
                    scheduledStop = true;
                }
                float t3 = t = (MainWindow.sw.ElapsedMilliseconds - MainWindow.startOffset) / MainWindow.beatInterval;
                if (t2 > 1)
                {
                    t2 = 1;
                }
                if (!ZoomOverride)
                {
                    if (BadZoom)
                    {
                        zoombase = 1.50f;
                        Zoom = QuadraticEaseOut(t2, 1.0f, zoombase * 1.5f, 1);
                    }
                    else
                    {
                        float zoomnull = 1.0f;
                        if (timer3.Enabled)
                        {
                            zoomnull = zoomin;
                        }
                        zoombase = 1.10f;
                        Zoom = QuadraticEaseOut(t2, zoombase, zoomnull - zoombase, 1);
                    }
                }
                if (Zoom > LastZoom & scheduledStop)
                {
                    timer1.Stop();
                    timer1.Enabled = false;
                    scheduledStop = false;
                    scheduledStopTimer = false;
                    stopped = true;
                    if (!timer3.Enabled)
                    {
                        Zoom = 1f;
                        SetZoom(Zoom);
                        LastZoom = Zoom;
                    }
                }
                else
                {
                    SetZoom(Zoom);
                    LastZoom = Zoom;
                }
            }
        }

        private void Timer2_Tick(object sender, EventArgs e)
        {
            float t2 = (MainWindow.sw.ElapsedMilliseconds - MainWindow.startOffset - colstart) % (MainWindow.beatInterval * 8f / ColorSpeed);
            float t3 = t2 / (MainWindow.beatInterval * 8f / ColorSpeed);
            grados = (int)(t3 * 360f);
            if (grados >= 360)
            {
                grados %= 360;
            }
            if (ColorReverse)
            {
                grados = 360 - grados;
            }
            SetScreenRotPtr(grados);
        }

        private void Timer3_Tick(object sender, EventArgs e)
        {
            if (enabled)
            {
                offsetx = rand.Next(-100, 100) / 100f;
                offsety = rand.Next(-100, 100) / 100f;
                if (stopped)
                {
                    float x = SystemInformation.VirtualScreen.Left;
                    float y = SystemInformation.VirtualScreen.Top;
                    float w = SystemInformation.VirtualScreen.Width;
                    float h = SystemInformation.VirtualScreen.Height;
                    float w2 = w * zoomin;
                    float h2 = h * zoomin;
                    float w3 = w2 - w;
                    float h3 = h2 - h;
                    float w4 = w3 / zoomin / 2;
                    float h4 = h3 / zoomin / 2;
                    float w5 = w4 + w4 * offsetx;
                    float h5 = h4 + w4 * offsety;
                    MagSetFullscreenTransform(zoomin, (int)x + (int)w5, (int)y + (int)h5);
                }
            }
        }

        private void Timer4_Tick(object sender, EventArgs e)
        {
            long ms = MainWindow.sw.ElapsedMilliseconds;
            float t = (ms - MainWindow.startOffset) / MainWindow.beatInterval - 158f;
            float fade;
            if (t < 8)
            {
                float b = 1f;
                float c = -1f;
                float d = 8f;
                fade = c * t / d + b;
            }
            else
            {
                float t2 = t - 8f;
                float b = 0f;
                float c = 2f;
                float d = 16f;
                fade = c * t2 / d + b;
            }
            SetScreenFadePtr(fade);
        }

        public void StartZoom()
        {
            stopped = false;
            scheduledStop = false;
            scheduledStopTimer = false;
            timer1.Enabled = true;
            timer1.Start();
        }

        public void StopZoom()
        {
            scheduledStopTimer = true;
        }

        public void StartColor()
        {
            colstart = MainWindow.sw.ElapsedMilliseconds - MainWindow.startOffset;
            timer2.Enabled = true;
            timer2.Start();
        }

        public void StopColor()
        {
            timer2.Stop();
            timer2.Enabled = false;
            grados = 0;
            SetScreenRotPtr(grados);
        }

        public void StartShake()
        {
            timer3.Enabled = true;
            timer3.Start();
        }

        public void StopShake()
        {
            timer3.Stop();
            timer3.Enabled = false;
        }

        public void SwitchFX()
        {
            if (!timer1.Enabled & timer3.Enabled)
            {
                StopShake();
                StartZoom();
            }
            else
            {
                StartShake();
                StopZoom();
            }
        }

        public void StartFade()
        {
            timer4.Enabled = true;
            timer4.Start();
        }

        public void StopFade()
        {
            timer4.Stop();
            timer4.Enabled = false;
            SetScreenFadePtr(1);
        }

        float colstart = 0;
        int grados = 0;
        bool enabled = true;
        public float Zoom { get; set; }
        private float LastZoom { get; set; }
        public bool BadZoom { get; set; } = false;
        public bool ZoomOverride { get; set; } = false;
        public bool ColorReverse { get; set; } = false;
        public float ColorSpeed { get; set; } = 1f;
        bool stopped = false;
        bool scheduledStop = false;
        bool scheduledStopTimer = false;
        float t;
        float zoombase;
        const float zoomin = 1.0100001f;
        float offsetx;
        float offsety;
        Random rand = new Random();
    }
}
