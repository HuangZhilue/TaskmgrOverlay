using System.Drawing;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.User32;

namespace OverlayLibrary;

public static class WindowHandleTool
{
    const int maxTitleLength = 256;

    public static IList<Tuple<HWND, RECT>> GetCvChartWindowList()
    {
        // 查找“任务管理器”窗口
        HWND windowHandle = FindWindow(null, "任务管理器");
        if (windowHandle == HWND.NULL)
            windowHandle = FindWindow(lpClassName: "TaskManagerWindow");

        StringBuilder titleBuilder = new(maxTitleLength);

        // 查找“任务管理器”窗口的子窗口“TaskManagerMain”
        IList<HWND> chidWindowList = windowHandle.EnumChildWindows();
        windowHandle = HWND.NULL;
        foreach (HWND chidWindow in chidWindowList)
        {
            _ = GetWindowText(chidWindow, titleBuilder, maxTitleLength);
            if (string.IsNullOrWhiteSpace(titleBuilder.ToString())) continue;
            Console.WriteLine(titleBuilder);
            if (titleBuilder.ToString().Trim().Equals("TaskManagerMain", StringComparison.OrdinalIgnoreCase))
            {
                windowHandle = chidWindow;
                break;
            }
        }
        if (windowHandle == HWND.NULL) throw new NullReferenceException(Resources.找不到任务管理器程序);

        // 查找“TaskManagerMain”窗口的子窗口“DirectUIHWND”
        chidWindowList = windowHandle.EnumChildWindows();
        windowHandle = HWND.NULL;
        foreach (HWND chidWindow in chidWindowList)
        {
            _ = RealGetWindowClass(chidWindow, titleBuilder, maxTitleLength);
            if (string.IsNullOrWhiteSpace(titleBuilder.ToString())) continue;
            Console.WriteLine(titleBuilder);
            if (titleBuilder.ToString().Trim().Equals("DirectUIHWND", StringComparison.OrdinalIgnoreCase))
            {
                windowHandle = chidWindow;
                break;
            }
        }
        if (windowHandle == HWND.NULL) throw new NullReferenceException(Resources.找不到DirectUIHWND样式的窗口);

        // 查找“DirectUIHWND”窗口的子窗口“CtrlNotifySink”
        chidWindowList = windowHandle.EnumChildWindows();
        // 右上角的窗口
        HWND rightTopWindow = HWND.NULL;
        // 右上角的窗口的矩形区域
        RECT rightTopWindowRectTemp = new(int.MaxValue, int.MinValue, 0, 0);
        RECT rightTopWindowRect = new();
        double minDistance = double.MaxValue;

        IList<HWND> ctrlNotifySinkList = [];
        List<Tuple<HWND, RECT>> CvChartWindowList = [];
        foreach (HWND chidWindow in chidWindowList)
        {
            _ = RealGetWindowClass(chidWindow, titleBuilder, maxTitleLength);
            if (string.IsNullOrWhiteSpace(titleBuilder.ToString())) continue;
            if (!titleBuilder.ToString().Trim().Equals("CtrlNotifySink", StringComparison.OrdinalIgnoreCase)) continue;

            // 排除掉“隐藏”的窗口
            int style = GetWindowLong(chidWindow, WindowLongFlags.GWL_STYLE);
            if (((WindowStyles)style & WindowStyles.WS_VISIBLE) != WindowStyles.WS_VISIBLE) continue;

            ctrlNotifySinkList.Add(chidWindow);

            // 查找“CtrlNotifySink”窗口的子窗口“CvChartWindow”
            IList<HWND> cvChartWindowList = chidWindow.EnumChildWindows();
            foreach (HWND cvChartWindow in cvChartWindowList)
            {
                _ = RealGetWindowClass(cvChartWindow, titleBuilder, maxTitleLength);
                if (string.IsNullOrWhiteSpace(titleBuilder.ToString())) continue;
                Console.WriteLine(titleBuilder);
                if (titleBuilder.ToString().Trim().Equals("CvChartWindow", StringComparison.OrdinalIgnoreCase))
                {
                    GetWindowRect(cvChartWindow, out RECT rect);
                    // 之所以用列表来存储，是因为像在CPU窗口中，有用“逻辑处理器”来显示不同核心的曲线
                    CvChartWindowList.Add(Tuple.Create(cvChartWindow, rect));

                    // 计算当前窗口的右上角与rightTopWindowRect的左上角的直线距离
                    double distance = Math.Sqrt(Math.Pow(rect.Right - rightTopWindowRectTemp.Right, 2) + Math.Pow(rect.Top - rightTopWindowRectTemp.Top, 2));

                    if (distance < minDistance)
                    {
                        rightTopWindow = cvChartWindow;
                        rightTopWindowRect = rect;
                        minDistance = distance;
                    }
                }
            }
        }
        if (!ctrlNotifySinkList.Any()) throw new NullReferenceException(Resources.找不到CtrlNotifySink样式的窗口);
        if (rightTopWindow == HWND.NULL || rightTopWindowRect.Width == 0 || rightTopWindowRect.Height == 0) throw new NullReferenceException(Resources.找不到右上角的窗口);

        // 找到右上角的窗口之后，再从CvChartWindowList中查找窗口Top值小于“右上角窗口”的窗口（第一个，如果存在的话，通常是CPU缩略图窗口）
        RECT cpuThumbnailWindowRect = CvChartWindowList.FirstOrDefault(cvChartWindow => cvChartWindow.Item2.Top <= rightTopWindowRect.Top - 5)?.Item2 ?? new();
        // 如果存在“CPU”缩略图窗口，则将与该窗口在同一列（Left值差不多）的窗口都一起全部移除掉
        if (cpuThumbnailWindowRect.Width != 0 && cpuThumbnailWindowRect.Height != 0)
        {
            CvChartWindowList = CvChartWindowList.Where(cvChartWindow => !(Math.Abs(cvChartWindow.Item2.Left - cpuThumbnailWindowRect.Left) < 5)).ToList();
        }

        if (CvChartWindowList.Count == 0) throw new NullReferenceException(Resources.找不到CvChartWindow样式的窗口);

        Console.WriteLine("Get CvChartWindow Count:\t" + CvChartWindowList.Count);
        return CvChartWindowList;
    }

    /// <summary>
    /// 从 CvChartWindowList 中计算出所有窗口所占用的最大矩形
    /// </summary>
    /// <param name="cvChartWindowList">窗口列表</param>
    /// <returns>多个窗口所占用的最大矩形区域</returns>
    /// <exception cref="ArgumentException"></exception>
    public static RECT CalcMaxCurveWindowRECT(this IList<Tuple<HWND, RECT>> cvChartWindowList)
    {
        if (cvChartWindowList is null || cvChartWindowList.Count == 0) throw new ArgumentException(null, nameof(cvChartWindowList));
        List<RECT> CurveWindowRECTList = cvChartWindowList.Select(c => c.Item2).ToList();
        RECT maxCurveWindowRECTTemp = new(CurveWindowRECTList.Min(r => r.Left), CurveWindowRECTList.Min(r => r.Top), CurveWindowRECTList.Max(r => r.Right), CurveWindowRECTList.Max(r => r.Bottom));
        return maxCurveWindowRECTTemp;
    }

    /// <summary>
    /// 将图片绘制在对应的窗口句柄所在的矩形区域中
    /// </summary>
    /// <param name="hwnd">窗口句柄</param>
    /// <param name="image">图片</param>
    /// <remarks>如果窗口句柄为空或者图片为空，则不会绘制</remarks>
    public static void ShowImage(this HWND hwnd, Bitmap image)
    {
        if (hwnd.IsNull || image is null) return;
        // 获取屏幕的设备上下文
        SafeHDC screenDC = GetDC(hwnd);

        // 创建一个内存设备上下文
        SafeHDC memoryDC = CreateCompatibleDC(screenDC);

        // 将图像数据拷贝到内存设备上下文中
        nint hBitmap = image.GetHbitmap();
        HGDIOBJ oldBitmap = SelectObject(memoryDC, hBitmap);

        // 获取图像的宽度和高度
        int width = image.Width;
        int height = image.Height;
        {
            // 绘制非透明底图像
            // 将图像绘制到屏幕上
            BitBlt(screenDC, 0, 0, width, height, memoryDC, 0, 0, RasterOperationMode.SRCCOPY);
        }
        //{
        //    // 绘制透明底图像
        //    // 创建一个 BlendFunction 结构体，用于指定透明度和混合模式
        //    BLENDFUNCTION blendFunction = new()
        //    {
        //        BlendOp = 0,
        //        BlendFlags = 0,
        //        SourceConstantAlpha = 0, // 设置透明度，0 表示完全透明，255 表示完全不透明
        //        AlphaFormat = 1
        //    };

        //    // 使用 AlphaBlend 函数绘制带有 alpha 通道的图像
        //    AlphaBlend(screenDC, 0, 0, width, height, memoryDC, 0, 0, width, height, blendFunction);
        //}
        // 清理资源
        SelectObject(memoryDC, oldBitmap);
        DeleteObject(hBitmap);
        DeleteDC(memoryDC);
        ReleaseDC(IntPtr.Zero, screenDC);
    }
}
