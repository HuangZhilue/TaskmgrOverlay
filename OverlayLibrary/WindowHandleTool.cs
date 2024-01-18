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
        IList<Tuple<HWND, RECT>> CvChartWindowList = [];

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
        // CPU缩略图窗口
        HWND thumbnailWindow = HWND.NULL;
        // CPU缩略图窗口的矩形区域
        RECT thumbnailWindowRect = new();
        // 不是缩略图窗口的列表
        IList<HWND> noThumbnailWindowList = [];
        foreach (HWND chidWindow in chidWindowList)
        {
            _ = RealGetWindowClass(chidWindow, titleBuilder, maxTitleLength);
            if (string.IsNullOrWhiteSpace(titleBuilder.ToString())) continue;
            if (!titleBuilder.ToString().Trim().Equals("CtrlNotifySink", StringComparison.OrdinalIgnoreCase)) continue;

            int style = GetWindowLong(chidWindow, WindowLongFlags.GWL_STYLE);
            if (((WindowStyles)style & WindowStyles.WS_VISIBLE) != WindowStyles.WS_VISIBLE) continue;

            RECT rect = new();
            GetWindowRect(chidWindow, out rect);
            Console.WriteLine(titleBuilder + "\tSTYLE:\t" + (WindowStyles)style + "\tRECT:\t" + rect.Width + "x" + rect.Height);
            if (thumbnailWindow == HWND.NULL)
            {
                // “DirectUIHWND”窗口内的第一个子窗口“CtrlNotifySink”，默认当作“CPU”缩略图窗口
                thumbnailWindow = chidWindow;
                thumbnailWindowRect = rect;
            }

            // 判断该窗口的矩形区域是否在“CPU”缩略图窗口的矩形区域的右侧（即 不是缩略图那一列）
            if (rect.Left <= thumbnailWindowRect.Right) continue;

            noThumbnailWindowList.Add(chidWindow);
        }
        if (!noThumbnailWindowList.Any()) throw new NullReferenceException(Resources.找不到CtrlNotifySink样式的窗口);

        // 查找“CtrlNotifySink”窗口的子窗口“CvChartWindow”
        foreach (HWND noThumbnailWindow in noThumbnailWindowList)
        {
            chidWindowList = noThumbnailWindow.EnumChildWindows();
            foreach (HWND chidWindow in chidWindowList)
            {
                _ = RealGetWindowClass(chidWindow, titleBuilder, maxTitleLength);
                if (string.IsNullOrWhiteSpace(titleBuilder.ToString())) continue;
                Console.WriteLine(titleBuilder);
                if (titleBuilder.ToString().Trim().Equals("CvChartWindow", StringComparison.OrdinalIgnoreCase))
                {
                    RECT rect = new();
                    GetWindowRect(chidWindow, out rect);
                    // 之所以用列表来存储，是因为像在CPU窗口中，用“逻辑处理器”来显示不同核心的曲线
                    CvChartWindowList.Add(Tuple.Create(chidWindow, rect));
                    break;
                }
            }
        }
        if (!CvChartWindowList.Any()) throw new NullReferenceException(Resources.找不到CvChartWindow样式的窗口);

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
        var CurveWindowRECTList = cvChartWindowList.Select(c => c.Item2).ToList();
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
