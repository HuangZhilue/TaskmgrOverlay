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
            if (IsRecording) return;

            IsRecording = true;
            WasapiLoopbackCapture.DataAvailable += LoopbackCapture_DataAvailable;
            WasapiLoopbackCapture.StartRecording();
        }
    }

    public static void StopAudioCapture()
    {
        lock (IsRecordingLock)
        {
            if (!IsRecording) return;

            WasapiLoopbackCapture.StopRecording();
            WasapiLoopbackCapture.DataAvailable -= LoopbackCapture_DataAvailable;
            IsRecording = false;
        }
    }

    public static Bitmap GetWaveCurve(int width, int height, Pen drawCurvePen, Color backgroundColor, int fillClosedCurveAlpha = 64, int maximumFrequencyLimit = 2500, bool enableSampleCompression = false, bool enableGaussianFilter = false, bool enableAWeighted = false)
    {
        return DrawWaveCurve(AllSamples, width, height, drawCurvePen, backgroundColor, fillClosedCurveAlpha, maximumFrequencyLimit, enableSampleCompression, enableGaussianFilter, enableAWeighted);
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

    private static Bitmap DrawWaveCurve(float[] allSamples, int width, int height, Pen drawCurvePen, Color backgroundColor, int fillClosedCurveAlpha = 64, int maximumFrequencyLimit = 2500, bool enableSampleCompression = false, bool enableGaussianFilter = false, bool enableAWeighted = false)
    {
        if (allSamples.Length == 0) return null!;
        // 获取音频数据的通道数
        int channelCount = WasapiLoopbackCapture.WaveFormat.Channels;
        // 分通道存储样本数据
        float[][] channelSamples = Enumerable
            .Range(0, channelCount)
            .Select(channel => Enumerable
                .Range(0, allSamples.Length / channelCount)
                .Select(i => allSamples[channel + i * channelCount])
                .ToArray())
            .ToArray();
        // 取通道平均值
        float[] averageSamples = Enumerable
            .Range(0, allSamples.Length / channelCount)
            .Select(index => Enumerable
                .Range(0, channelCount)
                .Select(channel => channelSamples[channel][index])
                .Average())
            .ToArray();

        // 因为快速傅里叶变换（FFT）算法对输入数据的长度有特定的要求，通常要求输入数据的长度是2的幂次方。
        // 计算averageSamples数组的长度的对数，并向上取整。这个值表示了填充后的数组应该具有的长度。
        double log = Math.Ceiling(Math.Log(averageSamples.Length, 2));
        // 使用对数的结果计算了新的填充后的样本数组的长度。通过取2的幂次方来得到一个更接近原始长度的值，以便进行填充。
        int newLen = (int)Math.Pow(2, log);
        // 创建一个新的填充样本数组，其长度为上一步计算得到的填充后的长度newLen。
        float[] filledSamples = new float[newLen];
        // 将原始的平均样本数据复制到新的填充样本数组中。如果原始样本数组的长度小于填充后的长度，那么填充样本数组中的剩余部分将被设置为默认值（0）。
        Array.Copy(averageSamples, filledSamples, averageSamples.Length);

        // 将填充样本数组转换为复数数组
        Complex[] complexSrc = filledSamples
            .Select(v => new Complex() { X = v })
            .ToArray();
        // 使用快速傅里叶变换（FFT）算法对复数数组进行频谱分析
        FastFourierTransform.FFT(false, (int)log, complexSrc);

        /*
         * 在进行傅里叶变换后，频谱结果是一个对称的数据集，其中包含了正频率和负频率的信息。
         * 正频率表示信号的频率，负频率则表示信号的相位。
         * 由于正频率和负频率是对称的，因此只需要保留其中一半的数据即可。
         * 
         * 在频谱分析中，我们通常只关注正频率部分，因为负频率部分的信息可以通过正频率部分的对称性推导得出。
         * 因此，为了节省内存和提高计算效率，我们只保留一半的数据。
         * 
         * 通过截取一半的频谱数据，我们可以准确地表示信号的频率分布，而无需保留冗余的负频率数据。
         * 这样可以节省存储空间，并且在后续处理或分析中，只需操作一半的数据，提高了计算效率。
        */
        // 由于傅里叶变换的结果是对称的，只有前一半的数据是有效的。
        Complex[] halfData = complexSrc
            .Take(complexSrc.Length / 2)
            .ToArray();

        /* 
         * 在进行傅里叶变换后，复数数组halfData中的每个元素都表示了对应频率的幅度和相位信息。
         * 为了得到只关注幅度的频率信息，我们可以计算每个复数元素的模，即复数的实部平方加上虚部平方的平方根。
        */
        // 取得频率幅度数据
        double[] dftData = halfData
            .Select(v => Math.Sqrt(v.X * v.X + v.Y * v.Y))
            .ToArray();

        // 根据最大频率限制和采样率，将 dftData 数组的元素截取为一个相对较小的子数组，只保留指定频率范围内的数据。
        // 对于变换结果, 每两个数据之间所差的频率计算公式为 采样率/采样数, 那么我们要取的个数也可以由 目标最高频率 / (采样率 / 采样数) 来得出
        int count = maximumFrequencyLimit / (WasapiLoopbackCapture.WaveFormat.SampleRate / filledSamples.Length);
        dftData = dftData.Take(count).ToArray();

        // 进行采样压缩
        if (enableSampleCompression)
            dftData = SampleCompression(dftData, WasapiLoopbackCapture.WaveFormat.SampleRate, filledSamples.Length);

        // 进行高斯滤波
        if (enableGaussianFilter)
        {
            double mu = 0;
            double sigma = 1;
            int filterRadius = 2;
            AudioGaussianFilter filter = new();
            filter.InitGaussKernel(mu, sigma, filterRadius);
            filter.Filter(dftData);
        }

        // 取得“A权重”加权计算后的数据
        if (enableAWeighted)
            dftData = CalculateAWeightedFrequencies(dftData);

        double[] finalData = dftData;//.Take(7000 / (WasapiLoopbackCapture.WaveFormat.SampleRate / filledSamples.Length)).ToArray();

        // 绘制时域图象

        // 设置波形图的参数
        Bitmap bitmap = new(width, height);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(backgroundColor);

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

    private static double[] CalculateAWeightedFrequencies(double[] dftData)
    {
        double[] weightedFrequencies = new double[dftData.Length];

        for (int i = 0; i < dftData.Length; i++)
        {
            double frequency = 22050 * i / dftData.Length;
            double magnitude = dftData[i];

            double weightedMagnitude = CalculateAWeighting(frequency) * magnitude;
            weightedFrequencies[i] = weightedMagnitude;
        }

        return weightedFrequencies;
    }

    private static double CalculateAWeighting(double f)
    {
        double f2 = f * f;
        return 1.2588966 * 148840000 * f2 * f2 /
            ((f2 + 424.36) * Math.Sqrt((f2 + 11599.29) * (f2 + 544496.41)) * (f2 + 148840000));
    }

    /// <summary>
    /// 采样压缩
    /// </summary>
    /// <param name="frequencies"></param>
    /// <param name="sampleRate"></param>
    /// <param name="sampleCount"></param>
    /// <returns></returns>
    private static double[] SampleCompression(double[] frequencies, int sampleRate, int sampleCount)
    {
        double[] finalData = [];

        // 处理 0 - 3000Hz 的数据
        int startIndex = 0;
        int endIndex = 3000 / (sampleRate / sampleCount);
        finalData = [.. finalData, .. CompressData(frequencies.Take(new Range(startIndex, endIndex)).ToArray(), (int)((endIndex - 1 - startIndex) * 1))];

        // 处理 3000 - 7500Hz 的数据
        startIndex = endIndex;
        endIndex += 7500 / (sampleRate / sampleCount);
        finalData = [.. finalData, .. CompressData(frequencies.Take(new Range(startIndex, endIndex)).ToArray(), (int)((endIndex - 1 - startIndex) * 0.25))];

        // 处理 7500 - 10000Hz 的数据
        startIndex = endIndex;
        endIndex += 10000 / (sampleRate / sampleCount);
        finalData = [.. finalData, .. CompressData(frequencies.Take(new Range(startIndex, endIndex)).ToArray(), (int)((endIndex - 1 - startIndex) * 0.15))];

        // 处理 10000Hz 之后的数据
        startIndex = endIndex;
        endIndex = sampleCount;
        finalData = [.. finalData, .. CompressData(frequencies.Take(new Range(startIndex, endIndex)).ToArray(), (int)((endIndex - 1 - startIndex) * 0.10))];

        return finalData;
    }

    /// <summary>
    /// 降采样
    /// </summary>
    /// <param name="data"></param>
    /// <param name="targetCount"></param>
    /// <returns></returns>
    private static double[] CompressData(double[] data, int targetCount)
    {
        double[] compressedData = new double[targetCount];

        int sourceCount = data.Length;
        double ratio = (double)sourceCount / targetCount;

        for (int i = 0; i < targetCount; i++)
        {
            int startIndex = (int)(i * ratio);
            int endIndex = (int)((i + 1) * ratio);

            double sum = 0.0;
            for (int j = startIndex; j < endIndex; j++)
            {
                sum += data[j];
            }

            compressedData[i] = sum / (endIndex - startIndex);
            if (double.IsNaN(compressedData[i])) compressedData[i] = 0.0;
        }

        return compressedData;
    }
}


public class AudioGaussianFilter
{
    private List<double> GKernel { get; set; }
    private double GKernelSum { get; set; }
    private int FilterRadius { get; set; }

    public AudioGaussianFilter()
    {
        GKernel = [];
        GKernelSum = 0;
        FilterRadius = 0;
    }

    public void InitGaussKernel(double mu, double sigma, int filterRadius)
    {
        FilterRadius = filterRadius;

        for (int i = -filterRadius; i < 1; i++)
        {
            GKernel.Add(Gauss(i, sigma, mu));
        }

        for (int i = filterRadius - 1; i > -1; i--)
        {
            // 对称
            GKernel.Add(GKernel[i]);
        }

        GKernelSum = GKernel.Sum();
        Console.WriteLine(string.Join(", ", GKernel));
    }

    public void Filter(double[] frequencies)
    {
        if (FilterRadius == 0)
            return;

        // 滤波
        for (int i = 0; i < frequencies.Length; i++)
        {
            double count = 0;
            for (int j = i - FilterRadius; j < i + FilterRadius; j++)
            {
                double value = (j >= 0 && j < frequencies.Length) ? frequencies[j] : 0;
                count += value * GKernel[j - i + FilterRadius];
            }

            frequencies[i] = count / GKernelSum;
        }
    }

    private static double Gauss(double x, double sigma, double mu)
    {
        double coefficient = 1.0 / (Math.Sqrt(2 * Math.PI) * sigma);
        double exponent = -((x - mu) * (x - mu)) / (2 * sigma * sigma);
        return coefficient * Math.Exp(exponent);
    }
}
