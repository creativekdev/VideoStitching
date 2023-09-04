﻿using com.sun.tools.corba.se.idl.constExpr;
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

namespace WPFVideoStitch
{
    /// <summary>
    /// Interaction logic for VideoSyncronization.xaml
    /// </summary>
    public partial class VideoSyncronization : Window
    {
        String left, right;

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
                line1.Points.Add(new OxyPlot.DataPoint(i, values1[i]));
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
                line2.Points.Add(new OxyPlot.DataPoint(i, values2[i]));
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
            myModel.Series.Add(line3);
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


            var mfccs1 = ExtractMFCCs(left1);
            var mfccs2 = ExtractMFCCs(left2);
            float[] crossCorrelation = CalculateCrossCorrelation(mfccs1, mfccs2);
//            ShowPlot("res", left1.Samples, left2.Samples, crossCorrelation);
            // Find the index of the maximum correlation value (alignment)
            int maxIndex = Array.IndexOf(crossCorrelation, crossCorrelation.Max());
            // Calculate the time delay (alignment) in frames
            int timeDelayFrames = maxIndex - mfccs1.Length;
            listView.Items.Clear();
            listView.Items.Add(new MyItem { Video = left, StartTime = "0", NearestFrame = "0" });
            listView.Items.Add(new MyItem { Video = right, StartTime = timeDelayFrames.ToString(), NearestFrame = timeDelayFrames.ToString() });
            //var crossCorrelation = CalculateCrossCorrelation(mfccVectors1, mfccVectors2);
            //            var xcorr = Operation.CrossCorrelate(left1, left2);
            //            int maxIndex = Array.IndexOf(xcorr.Samples, xcorr.Samples.Max());
            //          ShowPlot("res", xcorr.Samples, xcorr.Samples, xcorr.Samples, 1000);

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
                //...unspecified parameters will have default values 
            };
            // Create an MFCC feature extractor
            var mfccExtractor = new MfccExtractor(mfccOptions);

            // Compute MFCCs from the audio signal
            float[][] res= mfccExtractor.ComputeFrom(audioSignal).ToArray();
            // Create a new double[][] array with the same dimensions
            int numRows = res.Length;
            int numCols = res[0].Length;
            double[][] doubleArray = new double[numRows][];

            // Iterate through the floatArray and convert to doubleArray
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
        float[] CalculateCrossCorrelation(double[][] mfccs1, double[][] mfccs2)
        {
            // Convert 2D MFCC arrays into 1D arrays
            double[] flatMfccs1 = mfccs1.SelectMany(row => row).ToArray();
            double[] flatMfccs2 = mfccs2.SelectMany(row => row).ToArray();
            float[] f1 = new float[flatMfccs1.Length];
            float[] f2 = new float[flatMfccs2.Length];
  
            for (int i = 100; i <flatMfccs1.Length -  100; i++)
            {
                f1[i] = (float)flatMfccs1[i];
            }
            for (int i = 100; i < flatMfccs2.Length - 100; i++)
            {
                f2[i] = (float)flatMfccs2[i];
            }
            
            int n = flatMfccs1.Length + flatMfccs2.Length - 1;
            DiscreteSignal res = Operation.CrossCorrelate(new DiscreteSignal(1,f1), new DiscreteSignal(1, f2));
//            ShowPlot("res", f1, f2, res.Samples,1);

            return res.Samples;
        }
        public VideoSyncronization(String left, String right)
        {
            InitializeComponent();
            this.left = left;
            this.right = right;
            listView.Items.Clear();
            listView.Items.Add(new MyItem { Video = left, StartTime = "0", NearestFrame = "0" });
            listView.Items.Add(new MyItem { Video = right, StartTime = "0", NearestFrame = "0" });

        }
    }
    public class MyItem
    {
        public string Video { get; set; }

        public string StartTime { get; set; }
        public string NearestFrame { get; set; }
    }
}
