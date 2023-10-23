using Emgu.CV.Stitching;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using javax.swing.text.html;
using Emgu.CV.UI;
using System.Text.RegularExpressions;
using NAudio.Utils;
using System.Windows.Interop;
using System.Drawing;
using System.IO;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using com.sun.org.apache.xalan.@internal.xsltc.trax;
using System.Drawing.Drawing2D;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using sun.java2d;
using static sun.management.jmxremote.ConnectorBootstrap;
using System.Runtime.Serialization.Formatters.Binary;
using Emgu.CV.Linemod;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using Emgu.CV.Cuda;

namespace WPFVideoStitch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        System.String rightVideo = "";
        System.String leftVideo = "";

        bool have_transform = false;

        bool LeftSlideDraggingFlag = false;
        bool RightSlideDraggingFlag = false;
        bool CenteralSlideDraggingFlag = false;

        bool LeftSlidePlayStatus = false;
        bool RightSlidePlayStatus = false;
        bool CenteralSlidePlayStatus = false;

        int frameCount = 0;
        int totalFrameCount = 0;

        int lefttotalframecount = 0;
        int righttotalframecount = 0;

        int leftVideoSlideValue = 0;
        int rightVideoSlideValue = 0;

        bool tempStatus;

        VideoCapture leftCapture = new VideoCapture();
        VideoCapture rightCapture = new VideoCapture();

        Mat leftMat = new Mat();
        Mat rightMat = new Mat();

        TimeSpan _position;

        Stitcher stitcher = new Stitcher();


        //DispatcherTimer _timer = new DispatcherTimer();


        bool SynchronizationDialogShow = false;

        bool leftThreadRunning = false;
        bool rightThreadRunning = false;
        bool mergedThreadRunning = false;

        bool stitchedFlag = false;

        Matrix<float> RightK = new Matrix<float>(3, 3);
        Matrix<float> RightR = new Matrix<float>(3, 3);
        Matrix<float> LeftR = new Matrix<float>(3, 3);
        Matrix<float> LeftK = new Matrix<float>(3, 3);
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            //_timer.Interval = TimeSpan.FromMilliseconds(10);
            //_timer.Tick += new EventHandler(ticktock);
            //_timer.Start();


            stStatus.Visibility = Visibility.Collapsed;
            stText.Visibility = Visibility.Collapsed;
            //mergedVideoCtl.Visibility = Visibility.Collapsed;

            Synchronization.Visibility = Visibility.Collapsed;
            Stitch.Visibility = Visibility.Collapsed;
            Render.Visibility = Visibility.Collapsed;
            startPoint.Visibility = Visibility.Collapsed;
            endPoint.Visibility = Visibility.Collapsed;

            //float[] LK = new float[] { 1489.543f, 0, 1343.358f, 0, 1489.543f, 1008.269f, 0, 0, 1 };
            //float[] RK = new float[] { 1500.789f, 0, 1343.358f, 0, 1500.789f, 1008.269f, 0, 0, 1 };
            //float[] LR = new float[] { 0.9245664f, -0.074705034f, -0.37362564f, 0f, 0.98059082f, -0.1966538f, 0.38102096f, 0.18127549f, 0.90662134f };
            //float[] RR = new float[] { 0.9252639f, 0.06550405f, 0.37362558f, 0f, 0.9849769f, -0.17268626f, -0.3793242f, 0.15978038f, 0.91136354f };
        }

        public void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            //_timer.Stop();
            leftThreadRunning = false;
            rightThreadRunning = false;
            mergedThreadRunning = false;
        }

        void ticktock(object sender, EventArgs e)
        {
            /*
            if((leftVideoCtl.Source != null) && (leftVideoCtl.NaturalDuration.HasTimeSpan) && (!LeftSlideDraggingFlag))
                LeftSlide.Value = leftVideoCtl.Position.TotalSeconds;
            if ((rightVideoCtl.Source != null) && (rightVideoCtl.NaturalDuration.HasTimeSpan) && (!RightSlideDraggingFlag))
                RightSlide.Value = rightVideoCtl.Position.TotalSeconds;
            if ((mergedVideoCtl.Source != null) && (mergedVideoCtl.NaturalDuration.HasTimeSpan) && (!CenteralSlideDraggingFlag))
                CenterSlide.Value = mergedVideoCtl.Position.TotalSeconds;*/

            if(LeftSlidePlayStatus == true)
            {
                leftVideoSlideValue += 1;
                LeftSlide.Value = leftVideoSlideValue;
                Mat frame = leftCapture.QuerySmallFrame();
                Mat showing_mat = new Mat();
                CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));

                leftVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());

                //ExtractImageFromVideo(leftVideo, leftVideoSlideValue ,leftVideoCtl);
            }

            if (RightSlidePlayStatus == true)
            {
                rightVideoSlideValue += 1;
                RightSlide.Value = rightVideoSlideValue;
                Mat frame = rightCapture.QuerySmallFrame();
                Mat showing_mat = new Mat();
                CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));
                rightVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                //ExtractImageFromVideo(rightVideo, rightVideoSlideValue, rightVideoCtl);
            }
        }

        private void leftVideoCtl_MediaOpened(object sender, RoutedEventArgs e)
        {
            /*
            _position = leftVideoCtl.NaturalDuration.TimeSpan;
            LeftSlide.Minimum = 0;
            LeftSlide.Maximum = _position.TotalSeconds;
            */
        }
        private void rightVideoCtl_MediaOpened(object sender, RoutedEventArgs e)
        {
            /*
            _position = rightVideoCtl.NaturalDuration.TimeSpan;
            RightSlide.Minimum = 0;
            RightSlide.Maximum = _position.TotalSeconds;*/
        }

        private void mergedVideoCtl_MediaOpened(object sender, RoutedEventArgs e)
        {
            /*
            _position = mergedVideoCtl.NaturalDuration.TimeSpan;
            CenterSlide.Minimum = 0;
            CenterSlide.Maximum = _position.TotalSeconds;*/
        }


        private void GetStitchingValues()
        {

            Application.Current.Dispatcher.Invoke(() =>
            {
                stStatus.Visibility = Visibility.Visible;
                stStatus.IsIndeterminate = true;
                this.IsEnabled = false;
                LeftSlidePlayStatus = false;
                RightSlidePlayStatus = false;
                if (File.Exists("result.jpg"))
                {
                    File.Delete("result.jpg");
                }
            });

            string ffmpegPath = @"stitch.exe";
            //string arguments = $"-i \"{videoFilePath}\" -vn -acodec copy \"{outputFilePath}\"";
            string arguments = $"left.jpg right.jpg -v --confidence_threshold 0.3";

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

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (File.Exists("result.jpg"))
                {
                    using (Mat mat = new Mat("result.jpg"))
                    {
                        mergedImage.Source = ToBitmapSource(mat.ToImage<Bgr, byte>());
                        stitchedFlag = true;
                        Mat matchingImage = new Mat("matches.jpg");
                        CvInvoke.Imshow("Matching", matchingImage);
/*                        VectorOfMat vm = new VectorOfMat();
                        vm.Push(leftMat);
                        vm.Push(rightMat);
                        Mat resultMat = new Mat();
                        stitcher.Stitch(vm, resultMat);

                        resultMat.Save("myresult.jpg");*/

                        mergedImage.Source = ToBitmapSource(mat.ToImage<Bgr, byte>());
                    }
                }

                stStatus.Visibility = Visibility.Collapsed;
                stStatus.IsIndeterminate = false;
                this.IsEnabled = true;
            });
        }

        private void ExtractImageFromVideo(string videoFilePath, int frameNumber , System.Windows.Controls.Image image)
        {

            if (File.Exists("output.jpg"))
            {
                // Delete the existing file
                File.Delete("output.jpg");
            }
            string ffmpegPath = @"ffmpeg.exe";
            //string arguments = $"-i \"{videoFilePath}\" -vn -acodec copy \"{outputFilePath}\"";

            string arguments = $"-i \"{videoFilePath}\" -vf \"select=gte(n\\,{frameNumber})\" -vframes 1 output.jpg";

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

            string FileName = "output.jpg";
            string currentDirectory = Directory.GetCurrentDirectory();
            string FilePath = System.IO.Path.Combine(currentDirectory, FileName);
            BitmapImage bitmapImage = new BitmapImage();

            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

            Uri imageUri = new Uri(FilePath);
            string uniqueQuery = Guid.NewGuid().ToString();
            UriBuilder uriBuilder = new UriBuilder(imageUri);
            uriBuilder.Query = uniqueQuery;

            bitmapImage.UriSource = uriBuilder.Uri;
            bitmapImage.EndInit();
            image.Source = bitmapImage;
        }

        private void ShowLeftVideo()
        {
            while(leftThreadRunning)
            {
                if (LeftSlidePlayStatus == true && leftVideoSlideValue < (lefttotalframecount - 1))
                {
                    if(leftVideoSlideValue == (lefttotalframecount - 2)) LeftSlidePlayStatus = false;
                    leftVideoSlideValue += 1;
                    leftCapture.Read(leftMat);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LeftSlide.Value = leftVideoSlideValue;
                        using (Mat showing_mat = new Mat())
                        {
                            CvInvoke.Resize(leftMat, showing_mat, new System.Drawing.Size(800, 600));
                            leftVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                        }
                    });
                }
                System.Threading.Thread.Sleep(20);
            }
        }

        private void ShowRightVideo()
        {
            while(rightThreadRunning)
            {
                if (RightSlidePlayStatus == true && rightVideoSlideValue < (righttotalframecount - 1))
                {
                    if (rightVideoSlideValue == (righttotalframecount - 2)) RightSlidePlayStatus = false;
                    rightCapture.Read(rightMat);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RightSlide.Value = rightVideoSlideValue;
                        using (Mat showing_mat = new Mat())
                        {
                            CvInvoke.Resize(rightMat, showing_mat, new System.Drawing.Size(800, 600));
                            rightVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                        }
                    });

                    rightVideoSlideValue += 1;
                }

                System.Threading.Thread.Sleep(20);
            }
        }


        public void CallVideoStitching()
        {
            Mat image_a = new Mat("origin2.jpg");
            Mat image_b = new Mat("origin1.jpg");

            //leftCapture.Read(image_a);
            //rightCapture.Read(image_b);

/*            ORB sift = new ORB();

            Image<Gray, byte> gray1 = image_a.ToImage<Gray, byte>();
            Image<Gray, byte> gray2 = image_b.ToImage<Gray, byte>();

            VectorOfKeyPoint keypoints1 = new VectorOfKeyPoint();
            Mat descriptor1 = new Mat();
            sift.DetectAndCompute(gray1, null, keypoints1, descriptor1, false);

            VectorOfKeyPoint keypoints2 = new VectorOfKeyPoint();
            Mat descriptor2 = new Mat();
            sift.DetectAndCompute(gray2, null, keypoints2, descriptor2, false);

            // Draw the keypoints on the input image
            Mat outputImage = image_a.Clone();
            foreach (MKeyPoint keypoint in keypoints1.ToArray())
            {
                CvInvoke.Circle(outputImage, System.Drawing.Point.Round(keypoint.Point), (int)keypoint.Size, new Bgr(System.Drawing.Color.Blue).MCvScalar, 2);
            }

            // Draw the keypoints on the input image
            Mat outputImage1 = image_b.Clone();
            foreach (MKeyPoint keypoint in keypoints2.ToArray())
            {
                CvInvoke.Circle(outputImage1, System.Drawing.Point.Round(keypoint.Point), (int)keypoint.Size, new Bgr(System.Drawing.Color.Blue).MCvScalar, 2);
            }

            outputImage.Save("detect1.jpg");
            outputImage1.Save("detect2.jpg");


            DescriptorMatcher matcher = new BFMatcher(DistanceType.L2, false);
            VectorOfVectorOfDMatch rawMatches = new VectorOfVectorOfDMatch();

            matcher.Add(descriptor1);

            matcher.KnnMatch(descriptor2, rawMatches, 2, null);

            //List<MDMatch> matches = new List<MDMatch>();

            VectorOfDMatch matches = new VectorOfDMatch();

            for (int i = 0; i < rawMatches.Size; i++)
            {
                VectorOfDMatch rawMatch = rawMatches[i];

                // Ensure the distance is within a certain ratio of each other (i.e. Lowe's ratio test)
                if (rawMatch[0].Distance < rawMatch[1].Distance * 0.75)
                {
                    matches.Push(new MDMatch[] { rawMatch[0] });
                }
            }

            VectorOfPointF srcPoints = new VectorOfPointF();
            VectorOfPointF dstPoints = new VectorOfPointF();
            for (int i = 0; i < matches.Size; i++)
            {
                MDMatch match = matches[i];
                srcPoints.Push(new PointF[] { keypoints1[match.QueryIdx].Point });
                dstPoints.Push(new PointF[] { keypoints2[match.TrainIdx].Point });
            }

            Mat matching_image = new Mat();
            Features2DToolbox.DrawMatches(
                image_a, keypoints1,
                image_b, keypoints2,
                matches,
                matching_image,
                new MCvScalar(255, 0, 0),
                new MCvScalar(0, 255, 0),
                null,
                Features2DToolbox.KeypointDrawType.Default);

            matching_image.Save("Matching.jpg");*/

            DetailSphericalWarper detailSphericalWarper = new DetailSphericalWarper(1496);

            Mat leftWarped = new Mat();
            Mat rightWarped = new Mat();

            detailSphericalWarper.Warp(image_a, LeftK.Mat , LeftR.Mat , Inter.Linear , BorderType.Reflect , leftWarped);
            detailSphericalWarper.Warp(image_b, RightK.Mat, RightR.Mat, Inter.Linear, BorderType.Reflect, rightWarped);

            Blender blender = new MultiBandBlender();

            System.Drawing.Point[] corners = new System.Drawing.Point[] {
                new System.Drawing.Point(0,0),
                new System.Drawing.Point(987,0)
            };

            System.Drawing.Size[] sizes = new System.Drawing.Size[] {
                new System.Drawing.Size(2231,1450),
                new System.Drawing.Size(2186,1450)
            };
            blender.Prepare(corners, sizes);

            Mat leftCroppedImage = new Mat(leftWarped, new System.Drawing.Rectangle(206, 213, 2231, 1450));
            Mat rightCroppedImages = new Mat(rightWarped, new System.Drawing.Rectangle(0, 177, 2186, 1450));

            Mat leftMask = new Mat("mask1.jpg", ImreadModes.Grayscale);
            Mat rightMask = new Mat("mask2.jpg", ImreadModes.Grayscale);


            // Convert the type of the mask to CV_8U
            Mat leftConvertedMask = new Mat();
            leftMask.ConvertTo(leftConvertedMask, DepthType.Cv8U);

            Mat rightConvertedMask = new Mat();
            rightMask.ConvertTo(rightConvertedMask, DepthType.Cv8U);

            blender.Feed(leftCroppedImage, leftConvertedMask, corners[0]);
            blender.Feed(rightCroppedImages, rightConvertedMask, corners[1]);

            Mat resultImage = new Mat();
            Mat resultMask = new Mat();

            blender.Blend(resultImage, resultMask);

            //stitcher.GetHashCode();

            //            SphericalWarper

            //            MultiBandBlender blender = new MultiBandBlender();


            //blender.Feed()

            //blender.Blend()

            /*if (matches.Count > 4)
            {
                // Construct the two sets of points
                PointF[] points_a = matches.Select(match => keypoints1[match.QueryIdx].Point).ToArray();
                PointF[] points_b = matches.Select(match => keypoints2[match.TrainIdx].Point).ToArray();

                // Compute the homography between the two sets of points

                Mat homography_matrix = CvInvoke.FindHomography(points_a, points_b, RobustEstimationAlgorithm.Ransac, 4.0);

                System.Drawing.Size outputSize = new System.Drawing.Size(image_a.Width + image_b.Width, image_a.Height);
                Mat result = new Mat(outputSize, image_a.Depth, image_a.NumberOfChannels);

                CvInvoke.WarpPerspective(image_a, result, homography_matrix, outputSize, Inter.Linear, Warp.Default, BorderType.Constant, new MCvScalar(0));

                System.Drawing.Rectangle roi = new System.Drawing.Rectangle(new System.Drawing.Point(image_a.Width, 0), image_b.Size);

                Mat imageBRoi = new Mat(result, roi);
                image_b.CopyTo(imageBRoi);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    mergedImage.Source = ToBitmapSource(result.ToImage<Bgr, byte>());
                });
            }*/

            /*//VectorOfDMatch matches_bf = new VectorOfDMatch();
            //bf.Match(descriptor1, descriptor2, matches_bf);


            List<MDMatch> matchesList = matches_bf.ToArray().ToList();
            matchesList = matchesList.OrderBy(x => x.Distance).ToList();

            matches_bf = new VectorOfDMatch(matchesList.ToArray());

            var indexParams = new KMeansIndexParams(5);

            var searchParams = new SearchParams(checks: 50);

            FlannBasedMatcher flann = new FlannBasedMatcher(indexParams, searchParams);

            // Match the descriptors using FLANN matching
            VectorOfVectorOfDMatch knn_matches = new VectorOfVectorOfDMatch();
            flann.KnnMatch(descriptor1, descriptor2, knn_matches, 2);

            //-- Filter matches using the Lowe's ratio test
            const float ratio_thresh = 0.7f;
            VectorOfDMatch good_matches = new VectorOfDMatch();
            for (int i = 0; i < knn_matches.Size; i++)
            {
                if (knn_matches[i][0].Distance < ratio_thresh * knn_matches[i][1].Distance)
                {
                    good_matches.Push(new MDMatch[] { knn_matches[i][0] });

                }
            }

            List<MDMatch> matchesFlnn = good_matches.ToArray().ToList();
            matchesFlnn = matchesFlnn.OrderBy(x => x.Distance).ToList();

            good_matches = new VectorOfDMatch(matchesFlnn.ToArray());

            // Extract the matched keypoints
            VectorOfPointF srcPoints = new VectorOfPointF();
            VectorOfPointF dstPoints = new VectorOfPointF();
            for (int i = 0; i < good_matches.Size; i++)
            {
                MDMatch match = good_matches[i];
                srcPoints.Push(new PointF[] { keypoints1[match.QueryIdx].Point });
                dstPoints.Push(new PointF[] { keypoints2[match.TrainIdx].Point });
            }

            Mat homography = new Mat();
            Mat mask = new Mat();
            homography = CvInvoke.FindHomography(srcPoints, dstPoints, RobustEstimationAlgorithm.Ransac , 3.0, mask);


            Image<Bgr, byte> image1 = frame1.ToImage<Bgr, byte>();  
            Image<Bgr, byte> image2 = frame2.ToImage<Bgr, byte>();
            // Warp the first image using the homography
            Image<Bgr, byte> result = new Image<Bgr, byte>(image2.Width, image2.Height);
            CvInvoke.WarpPerspective(image1, result, homography, result.Size, Emgu.CV.CvEnum.Inter.Nearest,
                Emgu.CV.CvEnum.Warp.FillOutliers, borderValue: new MCvScalar(0, 0, 0));
                

            // Blending the warped image with the second image using alpha blending
            double alpha = 0.5; // blending factor
            Image<Bgr, byte> blended_image = new Image<Bgr, byte>(image2.Size);
            CvInvoke.AddWeighted(result, alpha, image2, 1 - alpha, 0, blended_image);*/
        }

        private void ShowMergedVideo()
        {
            while (mergedThreadRunning)
            {
                using (VectorOfMat vm = new VectorOfMat())
                {
                    vm.Push(leftMat);
                    vm.Push(rightMat);
                    using (Mat resultMat = new Mat())
                    {
                        stitcher.ComposePanorama(vm, resultMat);
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            using (Mat showing_mat = new Mat())
                            {
                                CvInvoke.Resize(rightMat, showing_mat, new System.Drawing.Size(800, 300));
                                mergedImage.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                            }
                        });
                    }
                }
            }
        }

        private void Import_Videos(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Video files (*.mp4)|*.mp4|Video files (*.avi)|*.avi|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            leftThreadRunning = false;
            rightThreadRunning = false;


            if (openFileDialog.ShowDialog() == true)
            {
                int i = 0;
                foreach (var file in openFileDialog.FileNames)
                {
                    i++;
                    if (i == 1)
                    {
                        leftVideo = file;
                        //ExtractAudioFormVideo(file, "left.wav");
                        
                        leftCapture.Dispose();
                        leftCapture = new VideoCapture(leftVideo);

                        leftCapture.Read(leftMat);
                        //Mat frame = leftCapture.QuerySmallFrame();
                        //leftVideoCtl.Source = ToBitmapSource(frame.ToImage<Bgr, byte>());
                        ExtractImageFromVideo(file , 1 , leftVideoCtl);

                        leftCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, 0);
                        LeftSlide.Minimum = 0;
                        LeftSlide.Maximum = leftCapture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                        lefttotalframecount = (int)LeftSlide.Maximum;
                        LeftSlide.Value = 0;
                        leftVideoSlideValue = 0;
                        //GetFrameCount(file);


                        //leftVideoCtl.Source = new Uri(leftVideo);
                        //                        leftVideoCtl.MediaOpened += LeftMediaElement_MediaOpened;
                        //leftVideoCtl.Play();

                        //PlayMediaElementWithDelay(leftVideoCtl);
                        //leftVideoCtl.Pause();
                        leftThreadRunning = true;

                        stitchedFlag = false;



                        System.Threading.Thread left = new System.Threading.Thread(ShowLeftVideo);
                        left.Start();

                        LeftSlidePlayStatus = false;
                    }
                    else if (i == 2)
                    {
                        rightVideo = file;

                        if (File.Exists("right.wav"))
                        {
                            // Delete the existing file
                            File.Delete("right.wav");
                        }

                        //ExtractAudioFormVideo(file, "right.wav");

                        rightCapture.Dispose();
                        rightCapture = new VideoCapture(rightVideo);

                        rightCapture.Read(rightMat);

                        //Mat frame = rightCapture.QuerySmallFrame();
                        //rightVideoCtl.Source = ToBitmapSource(frame.ToImage<Bgr, byte>());

                        ExtractImageFromVideo(file, 1, rightVideoCtl);

                        //rightVideoCtl.Source = new Uri(rightVideo);
                        //rightVideoCtl.Play();

                        //PlayMediaElementWithDelay(rightVideoCtl);

                        rightCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, 0);
                        RightSlide.Value = 0;
                        rightVideoSlideValue = 0;
                        RightSlide.Minimum = 0;
                        RightSlide.Maximum = rightCapture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                        righttotalframecount = (int)RightSlide.Maximum;


                        stitchedFlag = false;

                        RightSlidePlayStatus = false;

                        Synchronization.Visibility = Visibility.Visible;
                        Stitch.Visibility = Visibility.Visible;
                        Render.Visibility = Visibility.Visible;
                        startPoint.Visibility = Visibility.Visible;
                        endPoint.Visibility = Visibility.Visible;

                        rightThreadRunning = true;
                        System.Threading.Thread right = new System.Threading.Thread(ShowRightVideo);
                        right.Start();

                    }
                }
            }
        }
        private void Synchronization_Click(object sender, RoutedEventArgs e)
        {
            if(SynchronizationDialogShow == false)
            {
                SynchronizationDialogShow = true;
                VideoSyncronization videoSyncronization = new VideoSyncronization(leftVideo, rightVideo);
                videoSyncronization.MyEvent += frameCount_SetEvent;
                videoSyncronization.CloseEvent += frameCount_CloseEvent;
                videoSyncronization.Show();
            }
        }

        private void frameCount_CloseEvent(object sender, EventArgs e)
        {
            SynchronizationDialogShow = false;
        }

        private void frameCount_SetEvent(object sender , MyEventArgs e)
        {
            //MessageBox.Show(e.FrameCount.ToString());
            frameCount = e.FrameCount;
            LeftSlidePlayStatus = false;

            RightSlidePlayStatus = false;

            if (frameCount < 0)
            {
                if ((rightVideoSlideValue - frameCount) > (righttotalframecount - 1))
                {
                    leftVideoSlideValue = rightVideoSlideValue + frameCount;

                    LeftSlide.Value = leftVideoSlideValue;

                    leftCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, leftVideoSlideValue);

                    using (Mat frame = leftCapture.QuerySmallFrame())
                    {
                        LeftSlide.Value = leftVideoSlideValue;

                        using( Mat showing_mat = new Mat())
                        {
                            CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));

                            leftVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                        }

                    }
                }
                else
                {
                    rightVideoSlideValue = leftVideoSlideValue - frameCount;
                    RightSlide.Value = rightVideoSlideValue;

                    rightCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, rightVideoSlideValue);

                    using (Mat frame = rightCapture.QuerySmallFrame())
                    {
                        RightSlide.Value = rightVideoSlideValue;

                        using (Mat showing_mat = new Mat())
                        {
                            CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));

                            rightVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                        }
                    }
                }
            }

            else
            {

                if ((leftVideoSlideValue + frameCount) < (lefttotalframecount - 1))
                {
                    leftVideoSlideValue = rightVideoSlideValue + frameCount;

                    LeftSlide.Value = leftVideoSlideValue;

                    leftCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, leftVideoSlideValue);

                    using (Mat frame = leftCapture.QuerySmallFrame())
                    {
                        LeftSlide.Value = leftVideoSlideValue;

                        using (Mat showing_mat = new Mat())
                        {
                            CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));

                            leftVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                        }
                    }
                }
                else
                {
                    rightVideoSlideValue = leftVideoSlideValue - frameCount;
                    RightSlide.Value = rightVideoSlideValue;

                    rightCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, rightVideoSlideValue);

                    using (Mat frame = rightCapture.QuerySmallFrame())
                    {
                        RightSlide.Value = rightVideoSlideValue;

                        using (Mat showing_mat = new Mat())
                        {
                            CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));

                            rightVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                        }
                    }
                }
            }

            leftCapture.Read(leftMat);
            rightCapture.Read(rightMat);

        }
        public BitmapSource ToBitmapSource(Image<Bgr, byte> image)
        {
            using (System.Drawing.Bitmap bitmap = image.ToBitmap())
            {
                var bitmapData = bitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

                var bitmapSource = BitmapSource.Create(
                    bitmapData.Width, bitmapData.Height,
                    bitmap.HorizontalResolution, bitmap.VerticalResolution,
                    PixelFormats.Bgr24, null,
                    bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

                bitmap.UnlockBits(bitmapData);

                return bitmapSource;
            }
        }



        private void Stitch_Click(object sender, RoutedEventArgs e)
        {

            //System.Threading.Thread thread = new System.Threading.Thread(CallStitching);
            leftCapture.Read(leftMat);
            rightCapture.Read(rightMat);

            leftMat.Save("left.jpg");
            rightMat.Save("right.jpg");

/*            System.Threading.Thread preview = new System.Threading.Thread(CallStitching);
            preview.Start();*/

            System.Threading.Thread thread = new System.Threading.Thread(GetStitchingValues);
            thread.Start();
        }


        private void leftMedia_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }


        private void leftMedia_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length > 0)
                {
                    leftVideo = files[0];

                    leftCapture.Dispose();
                    leftCapture = new VideoCapture(leftVideo);

                    leftCapture.Read(leftMat);
                    //Mat frame= leftCapture.QuerySmallFrame();
                    //leftVideoCtl.Source = ToBitmapSource(frame.ToImage<Bgr, byte>());

                    leftCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, 0);
                    LeftSlide.Value = 0;
                    leftVideoSlideValue = 0;
                    LeftSlide.Minimum = 0;
                    LeftSlide.Maximum = leftCapture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                    lefttotalframecount = (int)LeftSlide.Maximum;

                    ExtractImageFromVideo(leftVideo, 1, leftVideoCtl);


                    //leftVideoCtl.Source = new Uri(files[0]);
                    //leftVideoCtl.Play();
                    //PlayMediaElementWithDelay(leftVideoCtl);
                    //leftVideoCtl.Pause();
                    LeftSlidePlayStatus = false;

                    stitchedFlag = false;

                    leftThreadRunning = true;
                    System.Threading.Thread left = new System.Threading.Thread(ShowLeftVideo);
                    left.Start();

                    if ( rightVideo!= "" )
                    {
                        Synchronization.Visibility = Visibility.Visible;
                        Stitch.Visibility = Visibility.Visible;
                        Render.Visibility = Visibility.Visible;
                        startPoint.Visibility = Visibility.Visible;
                        endPoint.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void rightMedia_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }


        private void rightMedia_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length > 0)
                {
                    rightVideo = files[0];


                    rightCapture.Dispose();
                    rightCapture = new VideoCapture(rightVideo);

                    rightCapture.Read(rightMat);

                    rightCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, 0);
                    RightSlide.Value = 0;
                    rightVideoSlideValue = 0;
                    RightSlide.Minimum = 0;
                    RightSlide.Maximum = rightCapture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                    righttotalframecount = (int)RightSlide.Maximum;

                    ExtractImageFromVideo(rightVideo, 1, rightVideoCtl);

                    //Mat frame = rightCapture.QuerySmallFrame();
                    //rightVideoCtl.Source = ToBitmapSource(frame.ToImage<Bgr, byte>());


                    //videoCapture.Read(frame);

                    //        rightVideoCtl.Source = ToBitmapSource(frame.ToImage<Bgr, byte>());


                    //rightVideoCtl.Source = new Uri(files[0]);
                    //rightVideoCtl.Play();
                    //PlayMediaElementWithDelay(rightVideoCtl);
                    //leftVideoCtl.Pause();
                    RightSlidePlayStatus = false;

                    stitchedFlag = false;

                    rightThreadRunning = true;
                    System.Threading.Thread right = new System.Threading.Thread(ShowRightVideo);
                    right.Start();

                    if (leftVideo != "")
                    {
                        Synchronization.Visibility = Visibility.Visible;
                        Stitch.Visibility = Visibility.Visible;
                        Render.Visibility = Visibility.Visible;
                        startPoint.Visibility = Visibility.Visible;
                        endPoint.Visibility = Visibility.Visible;
                    }

                }
            }
        }

        public void CallStitching()
        {

            Application.Current.Dispatcher.Invoke(() =>
            {
                stStatus.Visibility = Visibility.Visible;
                this.IsEnabled = false;
                LeftSlidePlayStatus = false;
                RightSlidePlayStatus = false;

                stStatus.Maximum = leftCapture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                stStatus.Value = leftVideoSlideValue;
            });

            // read the data from the text file
            //string[] lines = File.ReadAllLines("data.txt");
            //string[] lines1 = File.ReadAllLines("data1.txt");
            string[] lines = File.ReadAllLines("data2.txt");

            // parse the data into an array of doubles
            float[] data = new float[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                //data[i] = (float.Parse(lines[i]) + float.Parse(lines1[i]) + float.Parse(lines2[i])) / 3;
                data[i] = float.Parse(lines[i]);
            }

            DetailSphericalWarper detailSphericalWarper = new DetailSphericalWarper(data[0]);

            Mat leftMask = new Mat("mask1.jpg", ImreadModes.Grayscale);
            Mat rightMask = new Mat("mask2.jpg", ImreadModes.Grayscale);

            System.Drawing.Point[] corners = new System.Drawing.Point[] {
                new System.Drawing.Point((int)data[45],(int)data[46]),
                new System.Drawing.Point((int)data[47],(int)data[48])
            };

            System.Drawing.Size[] sizes = new System.Drawing.Size[] {
                new System.Drawing.Size((int)data[39], (int)data[40]),
                new System.Drawing.Size((int)data[43], (int)data[44])
            };

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    LeftR[i, j] = data[1 + i * 3 + j];
                    RightR[i, j] = data[10 + i * 3 + j];
                    LeftK[i, j] = data[19 + i * 3 + j];
                    RightK[i, j] = data[28 + i * 3 + j];
                }

            Mat leftConvertedMask = new Mat();
            leftMask.ConvertTo(leftConvertedMask, DepthType.Cv8U);
            Mat rightConvertedMask = new Mat();
            rightMask.ConvertTo(rightConvertedMask, DepthType.Cv8U);

            leftMask.Dispose();
            rightMask.Dispose();

            while (leftCapture.IsOpened && rightCapture.IsOpened)
            {
                leftCapture.Read(leftMat);
                rightCapture.Read(rightMat);

                if (leftMat.IsEmpty || rightMat.IsEmpty)
                    break;

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

                                using (Mat showing_mat = new Mat())
                                {
                                    CvInvoke.Resize(resultImage, showing_mat, new System.Drawing.Size(1340, 596));

                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        stStatus.Value += 1;
                                    });
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        mergedImage.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                                    });
                                }
                            }
                        }
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                this.IsEnabled = true;
                stStatus.Visibility = Visibility.Collapsed;

                leftCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, leftVideoSlideValue);
                rightCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames , rightVideoSlideValue);
            });
        }

        private void Render_Click(object sender, RoutedEventArgs e)
        {
            LeftSlidePlayStatus = false;
            RightSlidePlayStatus = false;
            Render render = new Render(leftVideo, rightVideo , frameCount);
            render.Show();
        }
        private void VideoMerger_Click(object sender, RoutedEventArgs e)
        {
            VideoMerger videoMerger = new VideoMerger();
            videoMerger.Show();
        }

        private void LeftSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
        }

        private void LeftSlide_DragStart(object sender, DragStartedEventArgs e)
        {
            LeftSlideDraggingFlag = true;
            tempStatus = LeftSlidePlayStatus;
            LeftSlidePlayStatus = false;
        }

        private void LeftSlide_DragCompleted(object sender , DragCompletedEventArgs e)
        {
            LeftSlideDraggingFlag = false;
            leftVideoSlideValue = (int)LeftSlide.Value;

            leftCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames , leftVideoSlideValue);
            LeftSlide.Value = leftVideoSlideValue;

            using (Mat frame = leftCapture.QuerySmallFrame())
            {

                Mat showing_mat = new Mat();
                CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));

                leftVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                LeftSlidePlayStatus = tempStatus;
            }

            if(RightSlidePlayStatus == false)
            {
                rightVideoSlideValue = leftVideoSlideValue - frameCount;
                rightCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, rightVideoSlideValue);
                RightSlide.Value = rightVideoSlideValue;

                using (Mat frame = rightCapture.QuerySmallFrame())
                {

                    Mat showing_mat = new Mat();
                    CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));

                    rightVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                }
            }
        }

        async void PlayMediaElementWithDelay(MediaElement mediaElement)
        {
            await Task.Delay(200);
            mediaElement.Pause();
        }

        private void RightSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
        }

        private void RightSlide_DragStart(object sender, DragStartedEventArgs e)
        {
            RightSlideDraggingFlag = true;

            tempStatus = RightSlidePlayStatus;
            RightSlidePlayStatus = false;
        }

        private void RightSlide_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            RightSlideDraggingFlag = false;
            rightVideoSlideValue = (int)RightSlide.Value;
            RightSlide.Value = rightVideoSlideValue;
            rightCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, rightVideoSlideValue);


            using (Mat frame = rightCapture.QuerySmallFrame())
            {
                Mat showing_mat = new Mat();
                CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));
                rightVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                RightSlidePlayStatus = tempStatus;
            }

            if (LeftSlidePlayStatus == false)
            {
                leftVideoSlideValue = rightVideoSlideValue + frameCount;
                LeftSlide.Value = leftVideoSlideValue;
                leftCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, leftVideoSlideValue);
                using (Mat frame = leftCapture.QuerySmallFrame())
                {

                    Mat showing_mat = new Mat();
                    CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));

                    leftVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                }
            }
        }

        private void CenterSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void CenterSlide_DragStart(object sender, DragStartedEventArgs e)
        {
            CenteralSlideDraggingFlag = true;
        }

        private void CenterSlide_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            CenteralSlideDraggingFlag = false;
            //mergedVideoCtl.Position = TimeSpan.FromSeconds(CenterSlide.Value);
            if (CenteralSlidePlayStatus == false)
            {
              //  mergedVideoCtl.Play();
              //  PlayMediaElementWithDelay(mergedVideoCtl);
                //mergedVideoCtl.Pause();
            }
        }
        void OnMouseDownPause1Media(object sender, MouseButtonEventArgs args)
        {
           LeftSlidePlayStatus = !LeftSlidePlayStatus;

            if (LeftSlidePlayStatus == false && RightSlidePlayStatus == false)
            {
                rightVideoSlideValue = leftVideoSlideValue - frameCount;
                RightSlide.Value = rightVideoSlideValue;
                rightCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, rightVideoSlideValue);

                using (Mat frame = rightCapture.QuerySmallFrame())
                {

                    Mat showing_mat = new Mat();
                    CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));

                    rightVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                }
            }
        }
        void OnMouseDownPause2Media(object sender, MouseButtonEventArgs args)
        {
            RightSlidePlayStatus = !RightSlidePlayStatus;

            if (LeftSlidePlayStatus == false && RightSlidePlayStatus == false)
            {
                leftVideoSlideValue = rightVideoSlideValue + frameCount;
                LeftSlide.Value = leftVideoSlideValue;
                leftCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, leftVideoSlideValue);

                using (Mat frame = leftCapture.QuerySmallFrame())
                {
                    Mat showing_mat = new Mat();
                    CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));

                    leftVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                }
            }
        }

        void OnMouseDownPause3Media(object sender, MouseButtonEventArgs args)
        {
/*            if (stitchedFlag == false)
            {*/
            if (LeftSlidePlayStatus == false && RightSlidePlayStatus == false) { LeftSlidePlayStatus = true; RightSlidePlayStatus = true; }
            else { LeftSlidePlayStatus = false; RightSlidePlayStatus = false; }
            /*}
            else
            {
                System.Threading.Thread preview = new System.Threading.Thread(CallStitching);
                preview.Start();
            }*/
        }

        private void startPoint_Click(object sender, RoutedEventArgs e)
        {

        }

        private void endPoint_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
