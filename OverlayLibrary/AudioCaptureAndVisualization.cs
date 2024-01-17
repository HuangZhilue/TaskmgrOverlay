using NAudio.Dsp;
using NAudio.Wave;
using System.Drawing;

namespace OverlayLibrary;

public class AudioCaptureAndVisualization
{
    private static WasapiLoopbackCapture WasapiLoopbackCapture { get; } = new();
    private static float[] AllSamples { get; set; } = [];
    public static bool IsRecording { get; private set; } = false;
    private static object IsRecordingLock { get; set; } = new();
    private static object SampleLock { get; set; } = new();

    public static void StartAudioCapture()
    {
        lock (IsRecordingLock)
        {
            if (!IsRecording)
            {
                IsRecording = true;
                WasapiLoopbackCapture.DataAvailable += LoopbackCapture_DataAvailable;
                WasapiLoopbackCapture.StartRecording();
            }
        }
    }

    public static void StopAudioCapture()
    {
        lock (IsRecordingLock)
        {
            if (IsRecording)
            {
                WasapiLoopbackCapture.StopRecording();
                WasapiLoopbackCapture.DataAvailable -= LoopbackCapture_DataAvailable;
                IsRecording = false;
            }
        }
    }

    public static Bitmap GetWaveCurve(int width, int height, Pen drawCurvePen, Color backgroundColor, int fillClosedCurveAlpha = 64)
    {
        return DrawWaveCurve(AllSamples, width, height, drawCurvePen, backgroundColor, fillClosedCurveAlpha);
    }

    private static void LoopbackCapture_DataAvailable(object? sender, WaveInEventArgs e)
    {
        lock (SampleLock)
        {
            // 捕捉声卡输出
            // 提取数据中的采样
            AllSamples = Enumerable.Range(0, e.BytesRecorded / 4)   // 除以四是因为, 缓冲区内每 4 个字节构成一个浮点数, 一个浮点数是一个采样
                .Select(i => BitConverter.ToSingle(e.Buffer, i * 4))  // 转换为 float
                .ToArray();
        }
    }

    private static Bitmap DrawWaveCurve(float[] allSamples, int width, int height, Pen drawCurvePen, Color backgroundColor, int fillClosedCurveAlpha = 64)
    {
        if (allSamples.Length == 0) return null!;
        // 分离左右通道
        int channelCount = WasapiLoopbackCapture.WaveFormat.Channels;   // WasapiLoopbackCapture 的 WaveFormat 指定了当前声音的波形格式, 其中包含就通道数
        float[][] channelSamples = Enumerable
            .Range(0, channelCount)
            .Select(channel => Enumerable
                .Range(0, allSamples.Length / channelCount)
                .Select(i => allSamples[channel + i * channelCount])
                .ToArray())
            .ToArray();
        // 取通道平均值
        // 例如通道数为2, 那么左声道的采样为 ChannelSamples[0], 右声道为 ChannelSamples[1]
        float[] averageSamples = Enumerable
            .Range(0, allSamples.Length / channelCount)
            .Select(index => Enumerable
                .Range(0, channelCount)
                .Select(channel => channelSamples[channel][index])
                .Average())
            .ToArray();

        // 我们将对 AverageSamples 进行傅里叶变换, 得到一个复数数组
        // 因为对于快速傅里叶变换算法, 需要数据长度为 2 的 n 次方, 这里进行
        double log = Math.Ceiling(Math.Log(averageSamples.Length, 2));   // 取对数并向上取整
        int newLen = (int)Math.Pow(2, log);                             // 计算新长度
        float[] filledSamples = new float[newLen];
        Array.Copy(averageSamples, filledSamples, averageSamples.Length);   // 拷贝到新数组
        Complex[] complexSrc = filledSamples
            .Select(v => new Complex() { X = v })        // 将采样转换为复数
            .ToArray();
        FastFourierTransform.FFT(false, (int)log, complexSrc);   // 进行傅里叶变换
        // 变换之后, complexSrc 则已经被处理过, 其中存储了频域信息
        // NAudio 的傅里叶变换结果中, 似乎不存在直流分量(这使我们的处理更加方便了), 但它也是有共轭什么的(也就是数据左右对称, 只有一半是有用的)
        // 仍然使用刚刚的 complexSrc 作为变换结果, 它的类型是 Complex[]
        Complex[] halfData = complexSrc
            .Take(complexSrc.Length / 2)
            .ToArray();    // 一半的数据
        double[] dftData = halfData
            .Select(v => Math.Sqrt(v.X * v.X + v.Y * v.Y))  // 取复数的模
            .ToArray();    // 将复数结果转换为我们所需要的频率幅度
        // 其实, 到这里你完全可以把这些数据绘制到窗口上, 这已经算是频域图象了, 但是对于音乐可视化来讲, 某些频率的数据我们完全不需要
        // 例如 10000Hz 的频率, 我们完全没必要去绘制它, 取 最小频率 ~ 2500Hz 足矣
        // 对于变换结果, 每两个数据之间所差的频率计算公式为 采样率/采样数, 那么我们要取的个数也可以由 2500 / (采样率 / 采样数) 来得出
        int count = 2500 / (WasapiLoopbackCapture.WaveFormat.SampleRate / filledSamples.Length);
        double[] finalData = dftData.Take(count).ToArray();

        // 绘制时域图象

        // 设置波形图的参数
        Bitmap bitmap = new(width, height);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(backgroundColor);

            //// height 为窗口的显示区域高度
            //// 设定通道采样平均值为 AverageSamples, 类型为 float[]
            //PointF[] points = averageSamples
            //    .Select((v, i) => new PointF(i, (float)(height - v)))
            //    .ToArray();   // 将数据转换为一个个的坐标点
            //graphics.DrawLines(Pens.Black, points);   // 连接这些点, 画线

            List<double> tempFinalData = [.. finalData];
            tempFinalData.Insert(0, 0);
            tempFinalData.Add(0);
            // height 为窗口高度
            PointF[] points2 = tempFinalData
                .Select((v, i) => new PointF(i * width / (float)(tempFinalData.Count - 1), (float)(height - v)))
                .ToArray();
            graphics.DrawCurve(drawCurvePen ?? new(Color.Purple, 5), points2);    // Graphics 可以直接绘制曲线

            // 填充路径下方的空间
            graphics.FillClosedCurve(new SolidBrush(Color.FromArgb(fillClosedCurveAlpha, (drawCurvePen ?? new(Color.Purple)).Color)) ?? new SolidBrush(Color.FromArgb(fillClosedCurveAlpha, Color.Purple)), points2);
        }

        // 保存绘制好的波形图
        //Cv2.ImShow("Waveform", bitmap.ToMat());
        //string name = $"waveform_{DateTime.Now.Ticks}_{Guid.NewGuid()}.png";
        //if (!Path.Exists($"{AppContext.BaseDirectory}/Wave/"))
        //    Directory.CreateDirectory($"{AppContext.BaseDirectory}/Wave/");
        //bitmap.Save($"{AppContext.BaseDirectory}/Wave/{name}", System.Drawing.Imaging.ImageFormat.Png);
        ////bitmap.Dispose();

        //Console.WriteLine("波形图已保存为:\t" + name);
        return bitmap;
    }
}
