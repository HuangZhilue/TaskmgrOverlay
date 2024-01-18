# TaskmgrOverlay

��Ƶ���ӻ�����������ʾ�����������

Audio visualization, but displayed in task manager

~~�п��ٸ� Bad Apple~~

![TaskmgrOverlay_Result_CPU](README/TaskmgrOverlay_Result_CPU.png)

![TaskmgrOverlay_Result_GPU](README/TaskmgrOverlay_Result_GPU.png)

## ���ʹ�� / How to use

![TaskmgrOverlay_HowToUse](README/TaskmgrOverlay_HowToUse.png)

- 0: ����������� / Open Taskmgr
- 1: ���ҡ���������������ܱ�ǩ���� / Find "Taskmgr" performance tag window
- 2: ��ѡ���� / Optional operation
    - ��ȡ����������ɫ / Get the primary and secondary colors of the window
    - �������޸Ĵ���������ɫ / Modify the primary and secondary colors of the window
    - �޸�͸���� / Modify transparency
    - �޸Ļ�ͼ�ӳ� / Modify drawing delay
    - �޸Ļ�ͼ�߶����ű��� / Modify drawing height scale
- 3: ��ʼ����ϵͳ��Ƶ��� / Start capturing system audio output
- 4: �������� / Play some music
- 5: �鿴����������Ĵ��ڱ仯 / See the changes of the Taskmgr window

## ��װ������

- ϵͳҪ�� / System requirements
    - Windows 11
    - ```Warning``` Windows 10 �������ݲ�֧�� / Windows 10 and below is not supported

- .Net Core 8.0 
    - �����Ѿ����� .Net Core 8.0 ����ʱ / The program has integrated .Net Core 8.0 runtime
    - �����ʾ����ʱ���󣬿����ذ�װ .Net Core 8.0 ����ʱ���� / If you get runtime error, download runtime from here
        - [.NET Desktop Runtime 8.0.1](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.1-windows-x64-installer)

# OverlayLibrary

������Ƶ���ӻ��ĺ��Ĺ��ܿ� / Core functionality library for audio visualization

## WindowHandleTool.cs

- ������ҡ�������������Ĵ��ھ�� / Find the window handle of the Taskmgr
- �ڶ�Ӧ�Ĵ��ھ���ϻ���ͼƬ / Draw the image on the corresponding window handle

## ImageHelper.cs

- �ڶ�Ӧ����Ļ�����ͼ / Take a screenshot of the corresponding screen area
- ����ͼƬ���ָ�ͼƬ�Լ���ȡ��ɫ / Adjust the image, split the image, and extract the color

## AudioCaptureAndVisualization.cs

- ����ϵͳ��Ƶ��� / Capture system  audio output
- ������Ƶ���ӻ���ͼƬ / Draw audio visualization image

## ʹ��ʾ�� / Examples

��ֱ�Ӳο� TaskmgrOverlay/ViewModels/MainViewModel.cs

See the examples in TaskmgrOverlay/ViewModels/MainViewModel.cs


``` csharp
using OverlayLibrary;

var CvChartWindowList = WindowHandleTool.GetCvChartWindowList();
var MaxCurveWindowRECT = CvChartWindowList.CalcMaxCurveWindowRECT();

var WaveCurvePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 57, 184, 227), 2.0f);
var WaveCurveBackgroundColor = System.Drawing.Color.FromArgb(255, 25, 25, 25);

AudioCaptureAndVisualization.StartAudioCapture();
Task.Run(async () =>
{
    while (AudioCaptureAndVisualization.IsRecording)
    {
        await Task.Delay(100).ConfigureAwait(false);

        if (MaxCurveWindowRECT == Vanara.PInvoke.RECT.Empty || CvChartWindowList.Count == 0) continue;

        System.Drawing.Bitmap waveCurveBitmap = AudioCaptureAndVisualization.GetWaveCurve(MaxCurveWindowRECT.Width, MaxCurveWindowRECT.Height, WaveCurvePen, WaveCurveBackgroundColor, (int)(0.64 * 255));
        if (waveCurveBitmap == null) continue;
        System.Drawing.Bitmap adjustedImage = ImageHelper.AdjustImage(waveCurveBitmap, MaxCurveWindowRECT, DisplayMode.Fill, Alignment.Center);
        for (int i = 0; i < CvChartWindowList.Count; i++)
        {
            Vanara.PInvoke.HWND windowHandle = CvChartWindowList[i].Item1;
            Vanara.PInvoke.RECT rectangle = CvChartWindowList[i].Item2;
            System.Drawing.Bitmap cropImage = ImageHelper.CropImage(adjustedImage, rectangle, MaxCurveWindowRECT.X, MaxCurveWindowRECT.Y);
            WindowHandleTool.ShowImage(windowHandle, cropImage);
        }
    }
});
```
