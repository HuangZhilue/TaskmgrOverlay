using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OverlayLibrary;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
//using OpenCvSharp;
//using OpenCvSharp.Extensions;

namespace TaskmgrOverlay.ViewModels;

public partial class MainViewModel : ObservableObject
{
    /// <summary>
    /// 任务管理器的截图
    /// </summary>
    [ObservableProperty]
    private BitmapSource _screenshot;
    /// <summary>
    /// 背景颜色
    /// </summary>
    [ObservableProperty]
    private System.Windows.Media.Color _backgroundColor = System.Windows.Media.Color.FromRgb(25, 25, 25);// new SolidColorBrush(System.Windows.Media.Color.FromRgb(255,0,255));
    [ObservableProperty]
    private SolidColorBrush _backgroundSolidColorBrush = new(System.Windows.Media.Color.FromRgb(25, 25, 25));
    /// <summary>
    /// 前景颜色
    /// </summary>
    [ObservableProperty]
    private System.Windows.Media.Color _foregroundColor = System.Windows.Media.Color.FromRgb(57, 184, 227);
    [ObservableProperty]
    private SolidColorBrush _foregroundSolidColorBrush = new(System.Windows.Media.Color.FromRgb(57, 184, 227));
    /// <summary>
    /// 透明度
    /// </summary>
    [ObservableProperty]
    private double _alpha = 0.20d;
    /// <summary>
    /// 绘图延迟
    /// </summary>
    [ObservableProperty]
    private int _drawWaveCurveDelay = 100;
    /// <summary>
    /// 绘图高度缩放比例
    /// </summary>
    [ObservableProperty]
    private double _heightScaling = 4;
    /// <summary>
    /// 最高频率限制
    /// </summary>
    [ObservableProperty]
    private int _maximumFrequencyLimit = 2500;
    /// <summary>
    /// 是否应用采样压缩
    /// </summary>
    [ObservableProperty]
    private bool _enableSampleCompression = false;
    /// <summary>
    /// 是否应用高斯过滤
    /// </summary>
    [ObservableProperty]
    private bool _enableGaussianFilter = true;
    /// <summary>
    /// 是否应用A权重
    /// </summary>
    [ObservableProperty]
    private bool _enableAWeighted = true;
    /// <summary>
    /// 正在录制
    /// </summary>
    [ObservableProperty]
    private bool _isRecording = false;

    private IList<Tuple<Vanara.PInvoke.HWND, Vanara.PInvoke.RECT>> CvChartWindowList { get; set; } = [];
    private Vanara.PInvoke.RECT MaxCurveWindowRECT { get; set; }
    private Bitmap ScreenshotBitmap { get; set; }
    private System.Drawing.Pen WaveCurvePen { get; set; } = new System.Drawing.Pen(Color.FromArgb(255, 57, 184, 227), 2.0f);
    private Color WaveCurveBackgroundColor { get; set; } = Color.FromArgb(255, 25, 25, 25);

    public MainViewModel()
    {
    }

    [RelayCommand]
    private void OnBackgroundColorChange()
    {
        BackgroundSolidColorBrush = new(BackgroundColor);
        WaveCurveBackgroundColor = Color.FromArgb(255, BackgroundColor.R, BackgroundColor.G, BackgroundColor.B);
    }

    [RelayCommand]
    private void OnForegroundColorChange()
    {
        ForegroundSolidColorBrush = new(ForegroundColor);
        WaveCurvePen = new System.Drawing.Pen(Color.FromArgb(255, ForegroundColor.R, ForegroundColor.G, ForegroundColor.B), 2.0f);
    }

    [RelayCommand]
    private void OnGetCvChartWindow()
    {
        try
        {
            CvChartWindowList = WindowHandleTool.GetCvChartWindowList();
            MaxCurveWindowRECT = CvChartWindowList.CalcMaxCurveWindowRECT();
            ScreenshotBitmap = ImageHelper.CaptureScreen(MaxCurveWindowRECT);
            Screenshot = Convert(ScreenshotBitmap);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void OnGetColor()
    {
        try
        {
            List<Color> mainColors = ImageHelper.ExtractDominantColors(ScreenshotBitmap);
            ImageHelper.GetLightestAndDarkestColors(mainColors, out Color lightestColor, out Color darkestColor);
            bool isDarkTheme = darkestColor == mainColors.FirstOrDefault();
            Color backgroundColor = isDarkTheme ? darkestColor : lightestColor;
            Color foregroundColor = isDarkTheme ? lightestColor : darkestColor;
            BackgroundColor = System.Windows.Media.Color.FromRgb(backgroundColor.R, backgroundColor.G, backgroundColor.B);
            ForegroundColor = System.Windows.Media.Color.FromRgb(foregroundColor.R, foregroundColor.G, foregroundColor.B);

            WaveCurvePen = new System.Drawing.Pen(Color.FromArgb(255, foregroundColor.R, foregroundColor.G, foregroundColor.B), 2.0f);
            WaveCurveBackgroundColor = Color.FromArgb(255, backgroundColor.R, backgroundColor.G, backgroundColor.B);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void OnRecording()
    {
        try
        {
            if (IsRecording)
            {
                AudioCaptureAndVisualization.StopAudioCapture();
            }
            else
            {
                AudioCaptureAndVisualization.StartAudioCapture();
                Task.Run(async () =>
                {
                    while (AudioCaptureAndVisualization.IsRecording)
                    {
                        await Task.Delay(DrawWaveCurveDelay).ConfigureAwait(false);
                        try
                        {
                            if (MaxCurveWindowRECT == Vanara.PInvoke.RECT.Empty || CvChartWindowList.Count == 0) continue;

                            Bitmap waveCurveBitmap = AudioCaptureAndVisualization.GetWaveCurve(MaxCurveWindowRECT.Width, MaxCurveWindowRECT.Height, HeightScaling, WaveCurvePen, WaveCurveBackgroundColor, (int)(Alpha * 255), MaximumFrequencyLimit, EnableSampleCompression, EnableGaussianFilter, EnableAWeighted);
                            if (waveCurveBitmap == null) continue;
                            //Cv2.ImShow("WaveCurve", waveCurveBitmap.ToMat());
                            // 按照最大矩形区域的大小（以及相关参数）来调整图片大小
                            Bitmap adjustedImage = ImageHelper.AdjustImage(waveCurveBitmap, MaxCurveWindowRECT, DisplayMode.Fill, Alignment.Center);
                            //Cv2.ImShow("AdjustedImage", adjustedImage.ToMat());
                            for (int i = 0; i < CvChartWindowList.Count; i++)
                            {
                                Vanara.PInvoke.HWND windowHandle = CvChartWindowList[i].Item1;
                                Vanara.PInvoke.RECT rectangle = CvChartWindowList[i].Item2;
                                // 按照所需要的矩形区域来裁剪图片
                                Bitmap cropImage = ImageHelper.CropImage(adjustedImage, rectangle, MaxCurveWindowRECT.X, MaxCurveWindowRECT.Y);
                                //Cv2.ImShow($"windowHandle_{i}", cropImage.ToMat());
                                // 在对应的“窗口”上显示裁剪后的图片
                                WindowHandleTool.ShowImage(windowHandle, cropImage);
                            }
                            //Cv2.WaitKey(0);
                        }
                        catch (Exception ex)
                        {
                            _ = MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                });
            }
            IsRecording = AudioCaptureAndVisualization.IsRecording;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static BitmapSource Convert(Bitmap bitmap)
    {
        if (bitmap == null)
        {
            return null;
        }
        nint handle = bitmap.GetHbitmap();
        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
        finally { DeleteObject(handle); }
    }

    [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
    [return: MarshalAs(UnmanagedType.Bool)]
#pragma warning disable CA1401 // P/Invokes 应该是不可见的
#pragma warning disable SYSLIB1054 // 使用 “LibraryImportAttribute” 而不是 “DllImportAttribute” 在编译时生成 P/Invoke 封送代码
    public static extern bool DeleteObject([In] IntPtr hObject);
#pragma warning restore SYSLIB1054 // 使用 “LibraryImportAttribute” 而不是 “DllImportAttribute” 在编译时生成 P/Invoke 封送代码
#pragma warning restore CA1401 // P/Invokes 应该是不可见的
}
