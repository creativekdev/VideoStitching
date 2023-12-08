using com.sun.tools.corba.se.idl.constExpr;
using com.sun.tools.javadoc;
using Mono.Posix;
using NAudio.Wave;
using NWaves.Audio;
using NWaves.Signals;
using NWaves.Utils;
using NWaves.Transforms;
using sun.misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NWaves.Operations;
using NWaves.Operations.Convolution;
using NWaves.FeatureExtractors.Options;
using NWaves.FeatureExtractors;
using System.Windows.Media.Media3D;
using OxyPlot.Series;
using OxyPlot;
using OxyPlot.WindowsForms;
using System.ComponentModel;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Features2D;
using System.Diagnostics;
using java.util;

namespace WPFVideoStitch
{
    /// <summary>
    /// Interaction logic for VideoSyncronization.xaml
    /// </summary>
    /// 

    public class MyEventArgs : EventArgs
    {
        public int FrameCount { get; }

        public MyEventArgs(int frameCount)
        {
            FrameCount = frameCount;
        }
    }

    public class CloseEventArgs : EventArgs
    {
        public CloseEventArgs()
        {
        }
    }

    public partial class VideoSyncronization : Window
    {
        String left, right;

        public event EventHandler<MyEventArgs> MyEvent;
        public event EventHandler<CloseEventArgs> CloseEvent;

        int frameCount = 0;

        double frameRate = 0;

        int totalTimeLength = 30;//sec

        double randomValue = 1;

        void ShowPlot(string str, float[] values1, float[] values2, float[] values3, int step)
        {
            PlotView myPlot = new PlotView();

            var line1 = new OxyPlot.Series.LineSeries()
            {
                Title = $"Series 1",
                Color = OxyPlot.OxyColors.Blue,
                StrokeThickness = 1,
                MarkerSize = 1,
                MarkerType = OxyPlot.MarkerType.Circle
            };
            for (int i = 0; i < values1.Length; i+=step)
            {
                line1.Points.Add(new OxyPlot.DataPoint(i, values1[i] * 10 ));
            }

            var line2 = new OxyPlot.Series.LineSeries()
            {
                Title = $"Series 2",
                Color = OxyPlot.OxyColors.Red,
                StrokeThickness = 1,
                MarkerSize = 1,
                MarkerType = OxyPlot.MarkerType.Circle
            };
            for (int i = 0; i < values2.Length; i += step)
            {
                line2.Points.Add(new OxyPlot.DataPoint(i, values2[i] * 10 + 1  ));
            }

            var line3 = new OxyPlot.Series.LineSeries()
            {
                Title = $"Series 3",
                Color = OxyPlot.OxyColors.Green,
                StrokeThickness = 1,
                MarkerSize = 1,
                MarkerType = OxyPlot.MarkerType.Circle
            };
            for (int i = 0; i < values3.Length; i += step )
            {
                line3.Points.Add(new OxyPlot.DataPoint(i, values3[i]));
            }


            //Create Plotmodel object
            var myModel = new PlotModel { Title = str };
            myModel.Series.Add(line1);
            myModel.Series.Add(line2);
            //myModel.Series.Add(line3);
            //Assign PlotModel to PlotView
            myPlot.Model = myModel;

            //Set up plot for display
            myPlot.Dock = System.Windows.Forms.DockStyle.Bottom;
            myPlot.Location = new System.Drawing.Point(0, 0);
            myPlot.Size = new System.Drawing.Size(500, 500);
            myPlot.TabIndex = 0;

            //Add plot control to form
            Form window = new Form
            {
                Text = "My User Control",
                TopLevel = true,
                FormBorderStyle = FormBorderStyle.Fixed3D, //Disables user resizing
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = myPlot.Size //size the form to fit the content
            };

            window.Controls.Add(myPlot);
            myPlot.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            window.ShowDialog();
        }
        private void Syncronize_Click(object sender, RoutedEventArgs e)
        {
            System.Threading.Thread calculate = new System.Threading.Thread(Calculate);
            calculate.Start();
        }

        private void Calculate()
        {
            /*            System.Threading.Thread thread = new System.Threading.Thread(GenerateAudioFiles);
                        thread.Start();*/
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                stStatus.Visibility = Visibility.Visible;
                this.IsEnabled = false;
            });

            GenerateAudioFiles();
            WaveFile waveContainer1;
            using (var stream = new FileStream("left.wav", System.IO.FileMode.Open))
            {
                waveContainer1 = new WaveFile(stream);
            }

            DiscreteSignal left1 = waveContainer1[Channels.Left];

            WaveFile waveContainer2;
            using (var stream = new FileStream("right.wav", System.IO.FileMode.Open))
            {
                waveContainer2 = new WaveFile(stream);
            }

            DiscreteSignal left2 = waveContainer2[Channels.Left];

            /*            var mfccs1 = ExtractMFCCs(left1);
                        var mfccs2 = ExtractMFCCs(left2);*/

            DiscreteSignal res = Operation.CrossCorrelate(new DiscreteSignal(1, left1.Samples), new DiscreteSignal(1, left2.Samples));
            float[] crossCorrelation = res.Samples;
            /*
            double[] flatMfccs1 = mfccs1.SelectMany(row => row).ToArray();
            double[] flatMfccs2 = mfccs2.SelectMany(row => row).ToArray();

            ShowPlot("res", flatMfccs1, flatMfccs2, crossCorrelation, 1);*/

            int maxIndex = Array.IndexOf(crossCorrelation, crossCorrelation.Max());

            //ShowPlot("res", left1.Samples, left2.Samples, crossCorrelation, 1000);

            //int timeDelayFrames = mfccs2.Length - maxIndex + mfccs1.Length;
            int timeDelayFrames = maxIndex - left1.Samples.Length;
            
            double calcDuration = left1.Duration > left2.Duration ? left2.Duration : left1.Duration;

            if (calcDuration > totalTimeLength) calcDuration = totalTimeLength;
            
            if (timeDelayFrames < 0)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    firstVideoFrame.Text = "0";
                    firstVideoSecond.Text = "0ms";
                    secondVideoFrame.Text = ((int)(-timeDelayFrames * calcDuration / left1.Samples.Length * frameRate)).ToString();
                    secondVideoSecond.Text = ((int)(-timeDelayFrames * calcDuration * 1000 / left1.Samples.Length)).ToString() + "ms";
                });
            }
            else
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    firstVideoFrame.Text = ((int)(timeDelayFrames * calcDuration / left1.Samples.Length * frameRate)).ToString();
                    firstVideoSecond.Text = ((int)(timeDelayFrames * calcDuration * 1000 / left1.Samples.Length)).ToString() + "ms";
                    secondVideoFrame.Text = "0";
                    secondVideoSecond.Text = "0ms";
                });
            }

            frameCount = (int)(timeDelayFrames * left1.Duration / left1.Samples.Length * frameRate);
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                stStatus.Visibility = Visibility.Collapsed;
                this.IsEnabled = true;
            });
        }


        static int FindIndexOfMaximum(float[] array)
        {
            int index = 0;
            float max = array[0];
            for (int i = 1; i < array.Length; i++)
            {
                if (array[i] > max)
                {
                    max = array[i];
                    index = i;
                }
            }
            return index;
        }

        double[][] ExtractMFCCs(DiscreteSignal audioSignal)
        {
            var mfccOptions = new MfccOptions
            {
                SamplingRate = audioSignal.SamplingRate,
                FeatureCount = 1,
                FrameDuration = 0.032/*sec*/,
                HopDuration = 0.015/*sec*/, 
                FilterBankSize = 26,
                PreEmphasis = 0.97,
            };
            var mfccExtractor = new MfccExtractor(mfccOptions);

            // Compute MFCCs from the audio signal
            float[][] res= mfccExtractor.ComputeFrom(audioSignal).ToArray();
            int numRows = res.Length;
            int numCols = res[0].Length;
            double[][] doubleArray = new double[numRows][];

            for (int i = 0; i < numRows; i++)
            {
                doubleArray[i] = new double[numCols]; // Initialize sub-array

                for (int j = 0; j < numCols; j++)
                {
                    doubleArray[i][j] = (double)res[i][j]; // Convert and assign
                }
            }
            return doubleArray;
        }
/*        float[] CalculateCrossCorrelation(double[][] mfccs1, double[][] mfccs2)
        {
*//*            double[] flatMfccs1 = mfccs1.SelectMany(row => row).ToArray();
            double[] flatMfccs2 = mfccs2.SelectMany(row => row).ToArray();
            //float[] f1 = new float[flatMfccs1.Length];
            //float[] f2 = new float[flatMfccs2.Length];
            float[] f1 = new float[5];
            float[] f2 = new float[10];


            for (int i = 0; i < 5; i++)
                f1[i] = (float)i + 5;
            for( int i = 0; i < 10; i++)
                f2[i] = (float)i + 1;*/
/*            for (int i = 0; i <flatMfccs1.Length; i++)
            {
                f1[i] = (float)flatMfccs1[i];
            }
            for (int i = 0; i < flatMfccs2.Length; i++)
            { 
                f2[i] = (float)flatMfccs2[i];
            }*//*
            
            //int n = flatMfccs1.Length + flatMfccs2.Length - 1;
            DiscreteSignal res = Operation.CrossCorrelate(new DiscreteSignal(1,f2), new DiscreteSignal(1, f1));

            return res.Samples;
        }*/

        private void ReSetButton_Click(object sender, RoutedEventArgs e)
        {
            firstVideoFrame.Text = "0";
            secondVideoFrame.Text = "0";
            secondVideoSecond.Text = "0ms";
            firstVideoSecond.Text = "0ms";
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {

            frameCount = int.Parse(firstVideoFrame.Text) - int.Parse(secondVideoFrame.Text);
            MyEvent?.Invoke(this, new MyEventArgs(frameCount));
            this.Close();
        }

        private void CancleButton_Click(object sender, RoutedEventArgs e)
        {
            CloseEvent?.Invoke(this , new CloseEventArgs());
            this.Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            frameCount = int.Parse(firstVideoFrame.Text) - int.Parse(secondVideoFrame.Text);
            MyEvent?.Invoke(this, new MyEventArgs(frameCount));
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Check if the input is a valid number
            if (!IsNumeric(e.Text))
            {
                e.Handled = true; // Mark the event as handled to prevent the input from being processed
            }
        }

        private bool IsNumeric(string input)
        {
            return int.TryParse(input, out _); // Try to parse the input as an integer
        }

        private void firstVideoFrame_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (firstVideoFrame.Text == "")
            {
                firstVideoFrame.Text = "0";
                firstVideoSecond.Text = "0ms";
            }
            else
                firstVideoSecond.Text = ((int)(1000 / frameRate * (int.Parse(firstVideoFrame.Text)))).ToString() + "ms";
        }

        private void secondVideoFrame_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (secondVideoFrame.Text == "")
            {
                secondVideoFrame.Text = "0";
                secondVideoSecond.Text = "0ms";
            }
            else
                secondVideoSecond.Text = ((int)(1000 / frameRate * (int.Parse(secondVideoFrame.Text)))).ToString() + "ms";
        }

        private void GenerateAudioFiles()
        {
            ExtractAudioFromVideo(left, "left.wav");
            ExtractAudioFromVideo(right, "right.wav");
        }

        private void ExtractAudioFromVideo(string videoFilePath, string outputFilePath)
        {

            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            string ffmpegPath = @"ffmpeg.exe";
            //string arguments = $"-i \"{videoFilePath}\" -vn -acodec copy \"{outputFilePath}\"";

            //string arguments = $"-i \"{videoFilePath}\" -ss 0 -t {totalTimeLength}s -q:a 0 \"{outputFilePath}\"";
            string arguments = $"-i \"{videoFilePath}\" -ss 0 -t {totalTimeLength}s -q:a 0 \"{outputFilePath}\"";

            var process = new System.Diagnostics.Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(searchRange.Text == "") searchRange.Text = "0";

            totalTimeLength = int.Parse(searchRange.Text);

            System.Random random = new System.Random();
            randomValue = random.NextDouble() + 0.5;
        }

        public VideoSyncronization(String left, String right)
        {
            InitializeComponent();
            this.left = left;
            this.right = right;
            //listView.Items.Clear();

            firstVideoName.Text = System.IO.Path.GetFileName(left);
            secondVideoName.Text = System.IO.Path.GetFileName(right);

            firstVideoFrame.Text = "0";
            secondVideoFrame.Text = "0";
            secondVideoSecond.Text = "0ms";
            firstVideoSecond.Text = "0ms";

            stStatus.Visibility = Visibility.Collapsed;


            using (VideoCapture videoCapture = new VideoCapture(left))
            {
                this.frameRate = videoCapture.Get(Emgu.CV.CvEnum.CapProp.Fps);
            }
        }
    }
    public class MyItem
    {
        public string Video { get; set; }

        public string StartTime { get; set; }
        public string NearestFrame { get; set; }
    }
}
