using com.sun.org.apache.bcel.@internal.generic;
using Emgu.CV.CvEnum;
using Emgu.CV.Stitching;
using Emgu.CV;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Emgu.CV.Cuda;

namespace WPFVideoStitch
{
    /// <summary>
    /// Interaction logic for Render.xaml
    /// </summary>
    /// 
    public partial class Render : Window
    {

        String left, right;

        VideoCapture leftCapture = new VideoCapture();
        VideoCapture rightCapture = new VideoCapture();

        Mat leftConvertedMask = new Mat();
        Mat rightConvertedMask = new Mat();

        Mat leftMat = new Mat();
        Mat rightMat = new Mat();

        Matrix<float> RightK = new Matrix<float>(3, 3);
        Matrix<float> RightR = new Matrix<float>(3, 3);
        Matrix<float> LeftR = new Matrix<float>(3, 3);
        Matrix<float> LeftK = new Matrix<float>(3, 3);

        String outputPath;

        int count = 0;
        public Render(String left, String right , int frameCount , double startValue , double endValue)
        {
            InitializeComponent();
            savePath.Text = Properties.Settings.Default.LastFilePath;
            outputPath = savePath.Text;

            stStatus.Visibility = Visibility.Collapsed;
            pbText.Visibility = Visibility.Collapsed;

            this.left = left;
            this.right = right;

            leftCapture = new VideoCapture(left);
            rightCapture = new VideoCapture(right);

            count = (int) endValue - (int) startValue;
            if (frameCount < 0)
            {
                rightCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, startValue - frameCount);
                leftCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, startValue);
            }
            else
            {
                rightCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, startValue);
                leftCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, startValue + frameCount);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Threading.Thread preview = new System.Threading.Thread(CallStitching);
            preview.Start();
        }


        public void CallStitching()
        {
            if (File.Exists("result.jpg"))
            {

                Mat sampleOutputMat = new Mat("result.jpg");

                try
                {
                    VideoWriter videoWriter = new VideoWriter(outputPath, 25, new System.Drawing.Size(sampleOutputMat.Width, sampleOutputMat.Height), true);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        stStatus.Visibility = Visibility.Visible;
                        pbText.Visibility = Visibility.Visible;
                        this.IsEnabled = false;

                        stStatus.Maximum = count;
                        stStatus.Value = 0;
                    });

                    string[] lines = File.ReadAllLines("data2.txt");

                    // parse the data into an array of doubles
                    float[] data = new float[lines.Length];
                    int i, j;
                    for (i = 0; i < lines.Length; i++)
                    {
                        data[i] = float.Parse(lines[i]);
                    }

                    DetailSphericalWarper detailSphericalWarper = new DetailSphericalWarper(data[0]);

                    Mat leftMask = new Mat("mask1.jpg", ImreadModes.Grayscale);
                    Mat rightMask = new Mat("mask2.jpg", ImreadModes.Grayscale);

                    Mat realLeftMask = new Mat();
                    Mat realRightMask = new Mat();

                    CvInvoke.Resize(leftMask, realLeftMask, new System.Drawing.Size((int)data[37], (int)data[39]));
                    CvInvoke.Resize(rightMask, realRightMask, new System.Drawing.Size((int)data[38], (int)data[39]));

                    realLeftMask.ConvertTo(leftConvertedMask, DepthType.Cv8U);
                    realRightMask.ConvertTo(rightConvertedMask, DepthType.Cv8U);

                    System.Drawing.Point[] corners = new System.Drawing.Point[] {
                    new System.Drawing.Point((int)data[40],(int)data[41]),
                    new System.Drawing.Point((int)data[42],(int)data[43])
                };
                    System.Drawing.Size[] sizes = new System.Drawing.Size[] {
                    new System.Drawing.Size((int)data[37], (int)data[39]),
                    new System.Drawing.Size((int)data[38], (int)data[39])
                };
                    for (i = 0; i < 3; i++)
                        for (j = 0; j < 3; j++)
                        {
                            LeftR[i, j] = data[1 + i * 3 + j];
                            RightR[i, j] = data[10 + i * 3 + j];
                            LeftK[i, j] = data[19 + i * 3 + j];
                            RightK[i, j] = data[28 + i * 3 + j];
                        }
                    if (leftCapture.IsOpened && rightCapture.IsOpened)
                    {
                        leftCapture.Read(leftMat);
                        rightCapture.Read(rightMat);
                    }

                    UMat leftXMap = new UMat();
                    UMat leftYMap = new UMat();

                    UMat rightXMap = new UMat();
                    UMat rightYMap = new UMat();

                    detailSphericalWarper.BuildMaps(leftMat.Size, LeftK.Mat, LeftR.Mat, leftXMap, leftYMap);
                    detailSphericalWarper.BuildMaps(rightMat.Size, RightK.Mat, RightR.Mat, rightXMap, rightYMap);

                    int tempcount = count;

                    while (leftCapture.IsOpened && rightCapture.IsOpened && tempcount > 0)
                    {
                        leftCapture.Read(leftMat);
                        rightCapture.Read(rightMat);

                        leftCapture.Read(leftMat);
                        rightCapture.Read(rightMat);

                        tempcount -= 2;

                        if (leftMat.IsEmpty || rightMat.IsEmpty)
                            break;

                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();

                        using (Mat leftWarped = new Mat())
                        using (Mat rightWarped = new Mat())
                        {
                            if (CudaInvoke.HasCuda)
                            {
                                CudaInvoke.Remap(leftMat, leftWarped, leftXMap, leftYMap, Inter.Nearest);
                                CudaInvoke.Remap(rightMat, rightWarped, rightXMap, rightYMap, Inter.Nearest);
                            }
                            else
                            {
                                CvInvoke.Remap(leftMat, leftWarped, leftXMap, leftYMap, Inter.Nearest);
                                CvInvoke.Remap(rightMat, rightWarped, rightXMap, rightYMap, Inter.Nearest);
                            }

                            using (Blender blender = new MultiBandBlender())
                            {
                                blender.Prepare(corners, sizes);
                                blender.Feed(leftWarped, leftConvertedMask, corners[0]);
                                blender.Feed(rightWarped, rightConvertedMask, corners[1]);

                                using (Mat resultImage = new Mat())
                                using (Mat resultMask = new Mat())
                                {
                                    blender.Blend(resultImage, resultMask);
                                    if (resultImage.Depth != DepthType.Cv8U)
                                        resultImage.ConvertTo(resultImage, DepthType.Cv8U);
                                    using (Mat showing_mat = new Mat())
                                    {
                                        CvInvoke.Resize(resultImage, showing_mat, new System.Drawing.Size(sampleOutputMat.Width, sampleOutputMat.Height));

                                        videoWriter.Write(showing_mat);
                                        videoWriter.Write(showing_mat);
                                    }
                                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        stStatus.Value += 2;
                                    });
                                }
                            }
                        }

                        stopwatch.Stop();
                        TimeSpan duration = stopwatch.Elapsed;

                        // Access the duration value in milliseconds
                        double milliseconds = duration.TotalMilliseconds;
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            pbText.Text = ((int)(2000 / milliseconds)).ToString() + "fps";
                        });

                        // Access the duration values
                        int totalSeconds = (int)duration.TotalSeconds;
                    }

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        stStatus.Visibility = Visibility.Collapsed;
                        pbText.Visibility = Visibility.Collapsed;
                        this.IsEnabled = true;

                        System.Windows.MessageBox.Show("Saved Successfully!", "Success!");
                        this.Close();
                    });
                } catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Please select the output file name correctly!", "Error!");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Please stitch first!", "Error!");
            }
        }

        public void TestStitching()
        {
            if (File.Exists("result.jpg"))
            {
                string[] lines = File.ReadAllLines("data2.txt");

                // parse the data into an array of doubles
                float[] data = new float[lines.Length];
                int i, j;
                for (i = 0; i < lines.Length; i++)
                {
                    //data[i] = (float.Parse(lines[i]) + float.Parse(lines1[i]) + float.Parse(lines2[i])) / 3;
                    data[i] = float.Parse(lines[i]);
                }

                DetailSphericalWarper detailSphericalWarper = new DetailSphericalWarper(data[0]);

                Mat sampleOutputMat = new Mat("result.jpg");

                System.Drawing.Point[] corners = new System.Drawing.Point[] {
                    new System.Drawing.Point((int)data[45],(int)data[46]),
                    new System.Drawing.Point((int)data[47],(int)data[48])
                };

                System.Drawing.Size[] sizes = new System.Drawing.Size[] {
                    new System.Drawing.Size((int)data[39], (int)data[40]),
                    new System.Drawing.Size((int)data[43], (int)data[44])
                };
                for (i = 0; i < 3; i++)
                    for (j = 0; j < 3; j++)
                    {
                        LeftR[i, j] = data[1 + i * 3 + j];
                        RightR[i, j] = data[10 + i * 3 + j];
                        LeftK[i, j] = data[19 + i * 3 + j];
                        RightK[i, j] = data[28 + i * 3 + j];
                    }



                if (leftCapture.IsOpened && rightCapture.IsOpened)
                {
                    leftCapture.Read(leftMat);
                    rightCapture.Read(rightMat);

                    using (Mat leftWarped = new Mat())
                    using (Mat rightWarped = new Mat())
                    {
                        detailSphericalWarper.Warp(leftMat, LeftK.Mat, LeftR.Mat, Inter.Nearest, BorderType.Constant, leftWarped);
                        detailSphericalWarper.Warp(rightMat, RightK.Mat, RightR.Mat, Inter.Nearest, BorderType.Constant, rightWarped);
                        using (Mat leftCroppedImage = new Mat(leftWarped, new System.Drawing.Rectangle((int)data[37], (int)data[38], (int)data[39], (int)data[40])))
                        using (Mat rightCroppedImages = new Mat(rightWarped, new System.Drawing.Rectangle((int)data[41], (int)data[42], (int)data[43], (int)data[44])))
                        {
                            using (Blender blender = new MultiBandBlender())
                            {
                                blender.Prepare(corners, sizes);
                                blender.Feed(leftCroppedImage, leftConvertedMask, corners[0]);
                                blender.Feed(rightCroppedImages, rightConvertedMask, corners[1]);

                                using (Mat resultImage = new Mat())
                                using (Mat resultMask = new Mat())
                                {
                                    blender.Blend(resultImage, resultMask);
                                    if (resultImage.Depth != DepthType.Cv8U)
                                    {
                                        // Convert the image to a supported depth
                                        resultImage.ConvertTo(resultImage, DepthType.Cv8U);
                                    }
                                    using (Mat showing_mat = new Mat())
                                    {
                                        CvInvoke.Resize(resultImage, showing_mat, new System.Drawing.Size(sampleOutputMat.Width, sampleOutputMat.Height));

                                        showing_mat.Save("testStitching.jpg");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog();

            openFileDlg.InitialDirectory = Properties.Settings.Default.LastFilePath;

            /*            var result = openFileDlg.ShowDialog();
                        if (result.ToString() != string.Empty)
                        {
                            savePath.Text = openFileDlg.SelectedPath;

                            Properties.Settings.Default.LastFilePath = openFileDlg.SelectedPath;

                            Properties.Settings.Default.Save();
                        }*/

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = @"C:\";
            saveFileDialog.FileName = "output";
            saveFileDialog.DefaultExt = ".mp4";
            saveFileDialog.Filter = "Video Files (*.mp4)|*.mp4|All files (*.*)|*.*";

            var result = saveFileDialog.ShowDialog();

            if (result.ToString() != string.Empty)
            {
                string filePath = saveFileDialog.FileName;

                savePath.Text = filePath;

                outputPath = filePath;

                Properties.Settings.Default.LastFilePath = filePath;

                Properties.Settings.Default.Save();
                // Save the file...
            }
        }
    }
}
