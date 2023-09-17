using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Media;
using NAudio.Wave;

namespace MIBVirus
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal delegate bool EnumWindowsDelegate(IntPtr hWnd, int param);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, IntPtr windowTitle);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowLong(IntPtr hWnd, WindowLongFlags nIndex, ExtendedWindowStyleFlags dwNewLong);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowLongPtr(IntPtr hWnd, WindowLongFlags nIndex, ExtendedWindowStyleFlags dwNewLong);
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        internal static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, WindowLongFlags nIndex);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        internal static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, WindowLongFlags nIndex);
        [DllImport("user32.dll")]
        internal static extern bool EnumWindows(EnumWindowsDelegate lpfn, int lParam);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr SendMessageTimeout(IntPtr windowHandle, SystemMessages Msg, MessageFlags wParam, int lParam, SendMessageTimeoutFlags flag, uint timeout, out IntPtr result);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        internal static IntPtr GetWindowLongPtr(IntPtr hWnd, WindowLongFlags nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }
        internal static IntPtr SetWindowLongGeneric(IntPtr hWnd, WindowLongFlags nIndex, ExtendedWindowStyleFlags dwNewLong)
        {
            if (IntPtr.Size > 4)
                return SetWindowLongPtr(hWnd, nIndex, dwNewLong);
            else
                return SetWindowLong(hWnd, nIndex, dwNewLong);
        }
        [Flags]
        internal enum WindowLongFlags : int
        {
            GWL_EXSTYLE = -20
        }
        [Flags]
        internal enum ExtendedWindowStyleFlags : int
        {
            WS_EX_TOOLWINDOW = 0x00000080
        }

        [Flags]
        internal enum SystemMessages : int
        {
            WM_USER = 0x400,
            WM_USER_CONTROLPROGMAN = WM_USER + 300
        }

        [Flags]
        internal enum MessageFlags : int
        {
            JOINPROGMAN = 1,
            SPLITPROGMAN = 0,
        }

        [Flags]
        internal enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0000
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        internal static IntPtr workerw = IntPtr.Zero;

        static bool ProcEAE(IntPtr hWnd, int param)
        {
            IntPtr p = FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", IntPtr.Zero);
            if (p != IntPtr.Zero)
                workerw = FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", IntPtr.Zero);
            return true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr hWnd = new WindowInteropHelper(this).Handle;

            workerw = IntPtr.Zero;
            EnumWindows(new EnumWindowsDelegate(ProcEAE), 0);
            if (workerw == IntPtr.Zero)
            {
                SendMessageTimeout(FindWindow("Progman", null), SystemMessages.WM_USER_CONTROLPROGMAN, MessageFlags.SPLITPROGMAN, 0, SendMessageTimeoutFlags.SMTO_NORMAL, 1000, out IntPtr result);
                EnumWindows(new EnumWindowsDelegate(ProcEAE), 0);
            }
            SetWindowLongPtr(hWnd, WindowLongFlags.GWL_EXSTYLE, (ExtendedWindowStyleFlags)GetWindowLongPtr(hWnd, WindowLongFlags.GWL_EXSTYLE) | ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW);
            SetParent(hWnd, workerw);
            MoveWindow(hWnd, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Left, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Top, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, true);
            timer1 = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 8)
            };
            timer1.Tick += Timer1_Tick;
            reader = new Mp3FileReader(new MemoryStream(Properties.Resources.miba));
            var waveOut = new WaveOut();
            waveOut.Init(reader);
            sw = new Stopwatch();
            scfx = new ScreenFX();
            waveOut.PlaybackStopped += new EventHandler<StoppedEventArgs>(WaveOut_PlaybackStopped);
            waveOut.Play();
            sw.Start();
            timer1.Start();
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            scfx.Close();
            Close();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            double n = (sw.ElapsedMilliseconds - startOffset) / beatInterval + 1;
            if (phase < keyframes.Length)
            {
                if (n >= keyframes[phase].Time)
                {
                    keyframes[phase].ExecuteKeyFrame((float)n, this);
                    phase++;
                }
            }
            if (n >= 1)
            {
                if (n <= 20)
                {
                    backgroundGrid.Opacity = n / 20;
                }
                else
                {
                    backgroundGrid.Opacity = 1;
                }
            }
            float picsizemultiply = 1f + ((float)n - 21f) / 184f;
            float anchorx = (float)picsize * (float)backgroundImage.RenderTransformOrigin.X;
            float anchory = (float)picsize * (float)backgroundImage.RenderTransformOrigin.Y;
            float x = (1 - picsizemultiply) * anchorx;
            float y = (1 - picsizemultiply) * anchory;
            backgroundImage.Width = picsize * picsizemultiply;
            backgroundImage.Height = picsize * picsizemultiply;
            backgroundImage.Margin = new Thickness(0, 0, x, y);
            ((TranslateTransform)backgroundImage.RenderTransform).X = x;
            ((TranslateTransform)backgroundImage.RenderTransform).Y = y;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            picsize = ActualWidth;
            if (ActualWidth > ActualHeight)
            {
                picsize = ActualHeight;
            }
            backgroundSpacer.Width = picsize;
            backgroundSpacer.Height = picsize;
            backgroundImage.Width = picsize;
            backgroundImage.Height = picsize;
        }
        private class Keyframe
        {
            public float Time;
            public bool switchFX;
            public bool? zoom;
            public bool? zoomOverride;
            public float? zoomf;
            public bool? badzoom;
            public bool? shake;
            public bool? fade;
            public bool? color;
            public float? colorSpeed;
            public bool? colorReverse;
            public bool start;
            public bool end;
            public void ExecuteKeyFrame(float n, MainWindow w)
            {
                if (switchFX == true)
                {
                    w.scfx.SwitchFX();
                }
                else
                {
                    if (zoom != null)
                    {
                        if ((bool)zoom)
                        {
                            w.scfx.StartZoom();
                        }
                        else
                        {
                            w.scfx.StopZoom();
                        }
                    }
                    if (shake != null)
                    {
                        if ((bool)shake)
                        {
                            w.scfx.StartShake();
                        }
                        else
                        {
                            w.scfx.StopShake();
                        }
                    }
                }
                if (fade != null)
                {
                    if ((bool)fade)
                    {
                        w.scfx.StartFade();
                    }
                    else
                    {
                        w.scfx.StopFade();
                    }
                }
                if (color != null)
                {
                    if ((bool)color)
                    {
                        w.scfx.StartColor();
                    }
                    else
                    {
                        w.scfx.StopColor();
                    }
                }
                if (colorSpeed != null)
                {
                    w.scfx.ColorSpeed = (float)colorSpeed;
                }
                if (colorReverse != null)
                {
                    w.scfx.ColorReverse = (bool)colorReverse;
                }
                if (zoomOverride != null)
                {
                    w.scfx.ZoomOverride = (bool)zoomOverride;
                }
                if (zoomf != null)
                {
                    w.scfx.Zoom = (float)zoomf;
                }
                if (badzoom != null)
                {
                    w.scfx.BadZoom = (bool)badzoom;
                }
                if (start)
                {
                    w.backgroundImage.Visibility = Visibility.Visible;
                }
                if (end)
                {
                    w.backgroundGrid.Visibility = Visibility.Hidden;
                }
            }
        }

        double picsize;
        public const float startOffset = 304;
        int phase = 0;
        public const float beatInterval = 60000f / 120f;
        public static Stopwatch sw;
        Mp3FileReader reader;
        DispatcherTimer timer1;
        private ScreenFX scfx;
        private readonly Keyframe[] keyframes = {
                new Keyframe {
                    Time = 1f,
                    zoom = true
                },
                new Keyframe {
                    Time = 20f,
                    zoom = false
                },
                new Keyframe {
                    Time = 21f,
                    shake = true,
                    start = true
                },
                new Keyframe {
                    Time = 65f,
                    zoom = true
                },
                new Keyframe {
                    Time = 77f,
                    zoom = false
                },
                new Keyframe {
                    Time = 78f,
                    switchFX = true
                },
                new Keyframe {
                    Time = 81f,
                    switchFX = true
                },
                new Keyframe {
                    Time = 96f,
                    zoom = false
                },
                new Keyframe {
                    Time = 97f,
                    shake = true
                },
                new Keyframe {
                    Time = 101f,
                    switchFX = true
                },
                new Keyframe {
                    Time = 114f,
                    switchFX = true
                },
                new Keyframe {
                    Time = 117f,
                    zoom = true,
                    color = true
                },
                new Keyframe {
                    Time = 120f,
                    badzoom = true,
                    colorSpeed = 8f,
                    colorReverse = true
                },
                new Keyframe {
                    Time = 121f,
                    zoomOverride = true,
                    zoomf = 1.10f,
                    badzoom = false,
                    colorSpeed = 2f,
                    colorReverse = false
                },
                new Keyframe {
                    Time = 122f,
                    zoomf = 1.20f
                },
                new Keyframe {
                    Time = 122.25f,
                    zoomf = 1.25f
                },
                new Keyframe {
                    Time = 122.5f,
                    zoomf = 1.3f
                },
                new Keyframe {
                    Time = 122.75f,
                    zoomf = 1.35f
                },
                new Keyframe {
                    Time = 123f,
                    zoomOverride = false
                },
                new Keyframe {
                    Time = 124f,
                    colorSpeed = 8f
                },
                new Keyframe {
                    Time = 128f,
                    zoom = false,
                    colorSpeed = 1f
                },
                new Keyframe {
                    Time = 133f,
                    zoom = true
                },
                new Keyframe {
                    Time = 133.5f,
                    zoom = false
                },
                new Keyframe {
                    Time = 137f,
                    switchFX = true
                },
                new Keyframe {
                    Time = 153f,
                    switchFX = true
                },
                new Keyframe {
                    Time = 155f,
                    zoom = true
                },
                new Keyframe {
                    Time = 155.5f,
                    zoom = false
                },
                new Keyframe {
                    Time = 157f,
                    switchFX = true
                },
                new Keyframe {
                    Time = 159f,
                    zoom = false,
                    shake = true,
                    fade = true
                },
                new Keyframe {
                    Time = 183f,
                    zoom = true,
                    shake = false,
                    color = false,
                    fade = false
                },
                new Keyframe {
                    Time = 199f,
                    zoom = false,
                    color = true
                },
                new Keyframe {
                    Time = 201f,
                    zoom = true
                },
                new Keyframe {
                    Time = 201.5f,
                    zoom = false
                },
                new Keyframe {
                    Time = 203f,
                    zoom = true
                },
                new Keyframe {
                    Time = 205f,
                    zoom = false,
                    color = false,
                    end = true
                },
            };
    }
}
