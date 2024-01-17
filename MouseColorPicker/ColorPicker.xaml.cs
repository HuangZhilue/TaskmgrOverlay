using Gma.System.MouseKeyHook;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace MouseColorPicker;

/// <summary>
/// Interaction logic for ColorPicker.xaml
/// </summary>
public partial class ColorPicker : Window
{
    private IKeyboardMouseEvents GlobalHook { get; } = Hook.GlobalEvents();
    private double ScreenWidth { get; } = SystemParameters.PrimaryScreenWidth;
    private double ScreenHeight { get; } = SystemParameters.PrimaryScreenHeight;
    private object ColorLock { get; } = new object();
    private int LastX { get; set; }
    private int LastY { get; set; }
    public Color ColorResult { get; private set; } = Color.Empty;

    public ColorPicker()
    {
        InitializeComponent();
    }

    public Color ShowPicker()
    {
        Show();// Dialog();

        GlobalHook.MouseMove += GlobalHook_MouseMove;
        GlobalHook.MouseDownExt += GlobalHook_MouseDownExt;
        GlobalHook.KeyPress += GlobalHook_keyPress;
        return ColorResult;
    }

    private void GlobalHook_keyPress(object? sender, KeyPressEventArgs e)
    {
        Close();
    }

    private void GlobalHook_MouseDownExt(object? sender, MouseEventExtArgs e)
    {
        Close();
    }

    private void GlobalHook_MouseMove(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        Debug.WriteLine($"X: {e.X}, Y: {e.Y}");
        int LeftTemp = e.X + 15;
        int TopTemp = e.Y + 15;
        double windowWidth = Width;
        double windowHeight = Height;

        // 如果窗口的位置超出了屏幕的可见区域，则将其移动到屏幕的可见区域内
        double windowLeft = LeftTemp;
        double windowTop = TopTemp;

        if (windowLeft + windowWidth > ScreenWidth)
        {
            windowLeft = ScreenWidth - windowWidth;
        }
        if (windowTop + windowHeight > ScreenHeight)
        {
            windowTop = ScreenHeight - windowHeight * 2;
        }

        Left = windowLeft;
        Top = windowTop;

        Task.Run(() =>
        {
            lock (ColorLock)
            {
                if (LastX == e.X && LastY == e.Y) return;
                LastX = e.X;
                LastY = e.Y;
                try
                {
                    Rectangle captureBounds = new(e.X - 5, e.Y - 5, 10, 10); // 示例：截取屏幕上 (100, 100) 到 (300, 300) 的区域
                    Bitmap screenshot = ScreenCapture.CaptureScreen(captureBounds);

                    // 返回指定位置的像素颜色值
                    int x = 5; // 示例：获取截图中 (50, 50) 位置的颜色值
                    int y = 5;
                    ColorResult = PixelColor.GetPixelColor(screenshot, x, y);
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        try
                        {
                            ColorText.Text = $"{ColorResult.R}, {ColorResult.G}, {ColorResult.B}";
                            ColorRectangle.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(ColorResult.R, ColorResult.G, ColorResult.B));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    });
                    // 输出颜色值
                    Debug.WriteLine($"{DateTime.Now.Ticks}\tPixel color at ({x}, {y}): {ColorResult}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        });
    }

    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        GlobalHook.MouseMove -= GlobalHook_MouseMove;
        GlobalHook.MouseDownExt -= GlobalHook_MouseDownExt;
        GlobalHook.KeyPress -= GlobalHook_keyPress;
        GlobalHook.Dispose();
    }

    public static class ScreenCapture
    {
        public static Bitmap CaptureScreen(Rectangle bounds)
        {
            Bitmap screenshot = new(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }
            return screenshot;
        }
    }

    public static class PixelColor
    {
        public static Color GetPixelColor(Bitmap image, int x, int y)
        {
            return image.GetPixel(x, y);
        }
    }
}