using System.Drawing;
using System.Drawing.Drawing2D;
using Vanara.PInvoke;

namespace OverlayLibrary;

public enum DisplayMode
{
    /// <summary>
    /// 填充模式，将图片拉伸或压缩以填充整个显示区域
    /// </summary>
    Fill,
    /// <summary>
    /// 适应模式，保持图片宽高比，将图片缩放以适应显示区域
    /// </summary>
    Fit,
    /// <summary>
    /// 裁剪模式，将图片按比例缩放并裁剪，以填充整个显示区域
    /// </summary>
    Crop,
}

public enum Alignment
{
    Left,
    Right,
    Center,
    Top,
    Bottom
}

public static class ImageHelper
{

    /// <summary>
    /// 调整图片
    /// </summary>
    /// <param name="image">要被调整的图片</param>
    /// <param name="displayRectangle">调整后要显示的图片区域（图片大小）</param>
    /// <param name="mode">图片显示模式（填充、适应、裁剪）</param>
    /// <param name="alignment">图片显示位置（上下左右居中）</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Bitmap AdjustImage(Bitmap image, RECT displayRectangle, DisplayMode mode = DisplayMode.Fit, Alignment alignment = Alignment.Center)
    {
        int width = displayRectangle.Width;
        int height = displayRectangle.Height;

        // 计算图片的宽高比
        double imageAspectRatio = (double)image.Width / image.Height;
        // 计算显示区域的宽高比
        double displayAspectRatio = (double)width / height;

        int adjustedWidth;
        int adjustedHeight;
        int x = 0;
        int y = 0;

        switch (mode)
        {
            case DisplayMode.Fill:
                // 填充模式，将图片拉伸或压缩以填充整个显示区域
                adjustedWidth = width;
                adjustedHeight = height;
                break;
            case DisplayMode.Fit:
                // 适应模式，保持图片宽高比，将图片缩放以适应显示区域
                if (imageAspectRatio > displayAspectRatio)
                {
                    adjustedWidth = width;
                    adjustedHeight = (int)(width / imageAspectRatio);
                    y = (height - adjustedHeight) / 2;
                }
                else
                {
                    adjustedWidth = (int)(height * imageAspectRatio);
                    adjustedHeight = height;
                    x = (width - adjustedWidth) / 2;
                }
                break;
            case DisplayMode.Crop:
                // 裁剪模式，将图片按比例缩放并裁剪，以填充整个显示区域
                if (imageAspectRatio > displayAspectRatio)
                {
                    adjustedWidth = (int)(height * imageAspectRatio);
                    adjustedHeight = height;
                    x = (width - adjustedWidth) / 2;
                }
                else
                {
                    adjustedWidth = width;
                    adjustedHeight = (int)(width / imageAspectRatio);
                    y = (height - adjustedHeight) / 2;
                }
                break;
            default:
                throw new ArgumentException("Invalid display mode.");
        }

        // 根据对齐方式调整 x 和 y 坐标
        switch (alignment)
        {
            case Alignment.Left:
                x = 0;
                break;
            case Alignment.Right:
                x = width - adjustedWidth;
                break;
            case Alignment.Center:
                x = (width - adjustedWidth) / 2;
                y = (height - adjustedHeight) / 2;
                break;
            case Alignment.Top:
                y = 0;
                break;
            case Alignment.Bottom:
                y = height - adjustedHeight;
                break;
            default:
                throw new ArgumentException("Invalid alignment.");
        }

        // 创建一个新的位图，并绘制调整后的图片内容
        Bitmap adjustedImage = new(width, height, image.PixelFormat);
        using (Graphics graphics = Graphics.FromImage(adjustedImage))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;

            // 绘制调整后的图片
            graphics.DrawImage(image, new RECT(x, y, adjustedWidth, adjustedHeight));
        }

        // 返回调整后的图片
        return adjustedImage;
    }

    /// <summary>
    /// 从图片中截取对应区域的截图
    /// </summary>
    /// <param name="adjustedImage">被截取的图片</param>
    /// <param name="rectangles">要截取的区域的列表</param>
    /// <param name="start_x">从区域中修正的起始量X（如果要截取的区域的主区域不是从0开始的话，就需要设置该值）</param>
    /// <param name="start_y">从区域中修正的起始量Y（如果要截取的区域的主区域不是从0开始的话，就需要设置该值）</param>
    /// <returns></returns>
    public static List<Bitmap> CropImages(Bitmap adjustedImage, List<RECT> rectangles, int start_x = 0, int start_y = 0)
    {
        List<Bitmap> croppedImages = [];

        foreach (RECT rectangle in rectangles)
        {
            croppedImages.Add(CropImage(adjustedImage, rectangle, start_x, start_y));
        }

        return croppedImages;
    }

    /// <summary>
    /// 从图片中截取对应区域的截图
    /// </summary>
    /// <param name="mainImage">被截取的图片</param>
    /// <param name="rectangle">要截取的区域</param>
    /// <param name="start_x">从区域中修正的起始量X（如果要截取的区域的主区域不是从0开始的话，就需要设置该值）</param>
    /// <param name="start_y">从区域中修正的起始量Y（如果要截取的区域的主区域不是从0开始的话，就需要设置该值）</param>
    /// <returns></returns>
    public static Bitmap CropImage(Bitmap mainImage, RECT rectangle, int start_x = 0, int start_y = 0)
    {
        Bitmap croppedImage = new(rectangle.Width, rectangle.Height, mainImage.PixelFormat);

        using (Graphics graphics = Graphics.FromImage(croppedImage))
        {
            graphics.Clear(Color.Transparent);
            graphics.DrawImage(mainImage, new RECT(0, 0, rectangle.Width, rectangle.Height), new RECT(rectangle.Left - start_x, rectangle.Top - start_y, rectangle.Right - start_x, rectangle.Bottom - start_y), GraphicsUnit.Pixel);
        }

        //Cv2.ImShow("Cropped Image" + rectangle.ToString(), croppedImage.ToMat());

        return croppedImage;
    }

    /// <summary>
    /// 根据矩形所在区域截取屏幕
    /// </summary>
    /// <param name="area"></param>
    /// <returns></returns>
    public static Bitmap CaptureScreen(RECT area)
    {
        Bitmap screenshot = new(area.Width, area.Height);
        using (Graphics graphics = Graphics.FromImage(screenshot))
        {
            graphics.CopyFromScreen(area.Left, area.Top, 0, 0, area.Size);
        }
        return screenshot;
    }

    /// <summary>
    /// 从图片中提取最主要的几种的颜色值
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    public static List<Color> ExtractDominantColors(Bitmap image)
    {
        Dictionary<Color, int> colorCounts = [];

        // 遍历图片的每个像素
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Color pixelColor = image.GetPixel(x, y);

                // 统计每个颜色出现的次数
                if (colorCounts.TryGetValue(pixelColor, out int value))
                {
                    colorCounts[pixelColor] = ++value;
                }
                else
                {
                    colorCounts[pixelColor] = 1;
                }
            }
        }

        // 按颜色出现次数从高到低排序
        List<Color> dominantColors = colorCounts.OrderByDescending(c => c.Value)
                                                .Select(c => c.Key)
                                                .ToList();

        return dominantColors;
    }

    /// <summary>
    /// 判断两个颜色是否相似
    /// </summary>
    /// <param name="color1"></param>
    /// <param name="color2"></param>
    /// <param name="brightnessThreshold"></param>
    /// <param name="hueThreshold"></param>
    /// <returns></returns>
    public static bool AreColorsSimilar(Color color1, Color color2, float brightnessThreshold, float hueThreshold)
    {
        float brightnessDiff = Math.Abs(color1.GetBrightness() - color2.GetBrightness());
        float hueDiff = Math.Abs(color1.GetHue() - color2.GetHue());

        return brightnessDiff <= brightnessThreshold && hueDiff <= hueThreshold;
    }

    /// <summary>
    /// 从颜色列表中获取最亮和最暗的颜色
    /// </summary>
    /// <param name="colors"></param>
    /// <param name="lightestColor"></param>
    /// <param name="darkestColor"></param>
    public static void GetLightestAndDarkestColors(List<Color> colors, out Color lightestColor, out Color darkestColor)
    {
        lightestColor = Color.Empty;
        darkestColor = Color.Empty;

        if (colors == null || colors.Count == 0)
        {
            return;
        }

        float maxBrightness = float.MinValue;
        float minBrightness = float.MaxValue;

        foreach (Color color in colors)
        {
            float brightness = color.GetBrightness();

            if (brightness > maxBrightness)
            {
                maxBrightness = brightness;
                lightestColor = color;
            }

            if (brightness < minBrightness)
            {
                minBrightness = brightness;
                darkestColor = color;
            }
        }
    }
}
