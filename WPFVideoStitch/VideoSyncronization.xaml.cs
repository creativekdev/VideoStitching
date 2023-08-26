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

namespace WPFVideoStitch
{
    /// <summary>
    /// Interaction logic for VideoSyncronization.xaml
    /// </summary>
    public partial class VideoSyncronization : Window
    {
        String left, right;

        private void Syncronize_Click(object sender, RoutedEventArgs e)
        {
            WaveFile waveContainer1;
            using (var stream = new FileStream("left.wav", System.IO.FileMode.Open))
            {
                waveContainer1 = new WaveFile(stream);
            }

            DiscreteSignal left1 = waveContainer1[Channels.Left];
            DiscreteSignal right1 = waveContainer1[Channels.Right];
            WaveFile waveContainer2;
            using (var stream = new FileStream("left.wav", System.IO.FileMode.Open))
            {
                waveContainer2 = new WaveFile(stream);
            }

            DiscreteSignal left2 = waveContainer2[Channels.Left];
            DiscreteSignal right2 = waveContainer2[Channels.Right];
            var mfccs1 = ExtractMFCCs(left1);
            var mfccs2 = ExtractMFCCs(left1);
            double[] crossCorrelation = CalculateCrossCorrelation(mfccs1, mfccs2);
            // Find the index of the maximum correlation value (alignment)
            int maxIndex = Array.IndexOf(crossCorrelation, crossCorrelation.Max());
            // Calculate the time delay (alignment) in frames
            int timeDelayFrames = maxIndex - mfccs1.Length;

            //var crossCorrelation = CalculateCrossCorrelation(mfccVectors1, mfccVectors2);
            // var xcorr = Operation.CrossCorrelate(mfccVectors1, mfccVectors2);
            int a = 0;
            int b = 0;
            b = a + 1;
        }
        double[][] ExtractMFCCs(DiscreteSignal audioSignal)
        {
            var mfccOptions = new MfccOptions
            {
                SamplingRate = audioSignal.SamplingRate,
                FeatureCount = 13,
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
        double[] CalculateCrossCorrelation(double[][] mfccs1, double[][] mfccs2)
        {
            // Convert 2D MFCC arrays into 1D arrays
            double[] flatMfccs1 = mfccs1.SelectMany(row => row).ToArray();
            double[] flatMfccs2 = mfccs2.SelectMany(row => row).ToArray();

            int n = flatMfccs1.Length + flatMfccs2.Length - 1;
            double[] result = new double[n];

            for (int i = 0; i < n; i++)
            {
                int minIndex = Math.Max(0, i - flatMfccs2.Length + 1);
                int maxIndex = Math.Min(i + 1, flatMfccs1.Length);

                for (int j = minIndex; j < maxIndex; j++)
                {
                    result[i] += flatMfccs1[j] * flatMfccs2[i - j];
                }
            }

            return result;
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
