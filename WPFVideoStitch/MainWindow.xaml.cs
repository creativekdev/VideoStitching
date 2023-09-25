using Emgu.CV.Stitching;
using Emgu.CV.Structure;
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
using Emgu.CV.Util;
using System.Diagnostics;
using javax.swing.text.html;
using Emgu.CV.UI;
using System.Text.RegularExpressions;
using NAudio.Utils;
using NAudio.Wave;
using System.Windows.Interop;
using System.Drawing;
using System.IO;
using com.sun.java.swing.plaf.windows.resources;
using System.Windows.Threading;
using javax.print.attribute.standard;
using System.DirectoryServices;
using System.Threading;
using Emgu.CV.Flann;
using System.Windows.Controls.Primitives;
using Emgu.CV.CvEnum;
using Emgu.CV.Dpm;
using Emgu.CV.Features2D;
using java.lang;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Emgu.CV.Rapid;
using System.Windows.Media.Media3D;
using com.sun.org.apache.bcel.@internal.generic;

namespace WPFVideoStitch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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


        TimeSpan _position;
        DispatcherTimer _timer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _timer.Interval = TimeSpan.FromMilliseconds(10);
            _timer.Tick += new EventHandler(ticktock);
            _timer.Start();


            stStatus.Visibility = Visibility.Collapsed;
            stText.Visibility = Visibility.Collapsed;
            //mergedVideoCtl.Visibility = Visibility.Collapsed;

            Synchronization.Visibility = Visibility.Collapsed;
            Stitch.Visibility = Visibility.Collapsed;
            Render.Visibility = Visibility.Collapsed;
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
        }

        private void leftVideoCtl_MediaOpened(object sender, RoutedEventArgs e)
        {
            /*
            _position = leftVideoCtl.NaturalDuration.TimeSpan;
            LeftSlide.Minimum = 0;
            LeftSlide.Maximum = _position.TotalSeconds;*/
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

        private void ExtractAudioFormVideo(string videoFilePath , string outputFilePath)
        {
            string ffmpegPath = @"ffmpeg.exe";
            //string arguments = $"-i \"{videoFilePath}\" -vn -acodec copy \"{outputFilePath}\"";

            string arguments = $"-i \"{videoFilePath}\" -vn -acodec  pcm_s16le -ar 44100 -ac 2 \"{outputFilePath}\"";

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

        private void Import_Videos(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Video files (*.mp4)|*.mp4|Video files (*.avi)|*.avi|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                int i = 0;
                foreach (var file in openFileDialog.FileNames)
                {
                    i++;
                    if (i == 1)
                    {
                        leftVideo = file;


                        using( VideoCapture videoCapture = new VideoCapture(leftVideo) )
                        {
                            using( Mat frame = new Mat() )
                            {

                                videoCapture.Read(frame);

                                leftVideoCtl.Source = ToBitmapSource(frame.ToImage<Bgr, byte>());
                            }
                        }

                        if (File.Exists("left.wav"))
                        {
                            // Delete the existing file
                            File.Delete("left.wav");
                        }
                        ExtractAudioFormVideo(file, "left.wav");
                        //leftVideoCtl.Source = new Uri(leftVideo);
//                        leftVideoCtl.MediaOpened += LeftMediaElement_MediaOpened;
                        //leftVideoCtl.Play();

                        //PlayMediaElementWithDelay(leftVideoCtl);
                        //leftVideoCtl.Pause();
                        LeftSlidePlayStatus = false;
                    }
                    else if (i == 2)
                    {
                        rightVideo = file;

                        using (VideoCapture videoCapture = new VideoCapture(rightVideo))
                        {
                            using (Mat frame = new Mat())
                            {

                                videoCapture.Read(frame);

                                rightVideoCtl.Source = ToBitmapSource(frame.ToImage<Bgr, byte>());
                            }
                        }

                        if (File.Exists("right.wav"))
                        {
                            // Delete the existing file
                            File.Delete("right.wav");
                        }

                        ExtractAudioFormVideo(file, "right.wav");

                        //rightVideoCtl.Source = new Uri(rightVideo);
                        //rightVideoCtl.Play();

                        //PlayMediaElementWithDelay(rightVideoCtl);
                        RightSlidePlayStatus = false;

                        Synchronization.Visibility = Visibility.Visible;
                        Stitch.Visibility = Visibility.Visible;
                        Render.Visibility = Visibility.Visible;
                    }
                }
                               
            }
        }
        private void Synchronization_Click(object sender, RoutedEventArgs e)
        {
            VideoSyncronization videoSyncronization = new VideoSyncronization(leftVideo, rightVideo);
            videoSyncronization.MyEvent += frameCount_SetEvent;
            videoSyncronization.Show();
        }

        private void frameCount_SetEvent(object sender , MyEventArgs e)
        {
            //MessageBox.Show(e.FrameCount.ToString());
            frameCount = e.FrameCount;
        }
        public BitmapSource ToBitmapSource(Image<Bgr, byte> image)
        {
            using (System.Drawing.Bitmap bitmap = image.ToBitmap())
            {
                IntPtr hBitmap = bitmap.GetHbitmap();
                try
                {
                    return Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions()
                    );
                }
                finally
                {
//                    NativeMethods.DeleteObject(hBitmap); // Cleanup to prevent memory leaks
                }
            }
        }



        private void Stitch_Click(object sender, RoutedEventArgs e)
        {

            System.Threading.Thread thread = new System.Threading.Thread(CallVideoCreate);
            //System.Threading.Thread thread = new System.Threading.Thread(CallVideoStitching);
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

                    using (VideoCapture videoCapture = new VideoCapture(leftVideo))
                    {
                        using (Mat frame = new Mat())
                        {

                            videoCapture.Read(frame);

                            leftVideoCtl.Source = ToBitmapSource(frame.ToImage<Bgr, byte>());
                        }
                    }


                    //leftVideoCtl.Source = new Uri(files[0]);
                    //leftVideoCtl.Play();
                    //PlayMediaElementWithDelay(leftVideoCtl);
                    //leftVideoCtl.Pause();
                    LeftSlidePlayStatus = false;

                    if( rightVideo!= "" )
                    {
                        Synchronization.Visibility = Visibility.Visible;
                        Stitch.Visibility = Visibility.Visible;
                        Render.Visibility = Visibility.Visible;
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

                    using (VideoCapture videoCapture = new VideoCapture(rightVideo))
                    {
                        using (Mat frame = new Mat())
                        {

                            videoCapture.Read(frame);

                            rightVideoCtl.Source = ToBitmapSource(frame.ToImage<Bgr, byte>());
                        }
                    }

                    //rightVideoCtl.Source = new Uri(files[0]);
                    //rightVideoCtl.Play();
                    //PlayMediaElementWithDelay(rightVideoCtl);
                    //leftVideoCtl.Pause();
                    RightSlidePlayStatus = false;

                    if (leftVideo != "")
                    {
                        Synchronization.Visibility = Visibility.Visible;
                        Stitch.Visibility = Visibility.Visible;
                        Render.Visibility = Visibility.Visible;
                    }

                }
            }
        }

        public void CallVideoStitching()
        {
            using (VideoCapture videoCapture1 = new VideoCapture(leftVideo))
            using (VideoCapture videoCapture2 = new VideoCapture(rightVideo))
            {

                Mat image_a = new Mat();
                Mat image_b = new Mat();

                videoCapture1.Read(image_a);
                videoCapture2.Read(image_b);

                SIFT sift = new SIFT();

                Image<Gray, byte> gray1 = image_a.ToImage<Gray, byte>();
                Image<Gray, byte> gray2 = image_b.ToImage<Gray, byte>();


                VectorOfKeyPoint keypoints1 = new VectorOfKeyPoint();
                Mat descriptor1 = new Mat();
                sift.DetectAndCompute(image_a, null, keypoints1, descriptor1, false);

                VectorOfKeyPoint keypoints2 = new VectorOfKeyPoint();
                Mat descriptor2 = new Mat();
                sift.DetectAndCompute(image_b, null, keypoints2, descriptor2, false);

                DescriptorMatcher matcher = new BFMatcher(DistanceType.L2, false);
                VectorOfVectorOfDMatch rawMatches = new VectorOfVectorOfDMatch();

                matcher.KnnMatch(descriptor1, descriptor2, rawMatches, 2, null);

                List<MDMatch> matches = new List<MDMatch>();

                for (int i = 0; i < rawMatches.Size; i++)
                {
                    VectorOfDMatch rawMatch = rawMatches[i];

                    // Ensure the distance is within a certain ratio of each other (i.e. Lowe's ratio test)
                    if (rawMatch.Size == 2 && rawMatch[0].Distance < rawMatch[1].Distance * 0.75)
                    {
                        matches.Add(rawMatch[0]);
                    }
                }

                if (matches.Count > 4)
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
                }

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
        }

        public void CallVideoCreate()
        {

            Stitcher stitcher = new Stitcher();
            Emgu.CV.Features2D.AKAZE finder = new Emgu.CV.Features2D.AKAZE();
            Emgu.CV.Stitching.WarperCreator warper = new SphericalWarper();

            stitcher.SetFeaturesFinder(finder);
            stitcher.SetWarper(warper);

            stitcher.SetBlender(new MultiBandBlender());


            VideoCapture videoCapture1 = new VideoCapture(leftVideo);
            VideoCapture videoCapture2 = new VideoCapture(rightVideo);

            Mat frame1 = new Mat();
            Mat frame2 = new Mat();

            Mat previous_result = new Mat();

            Mat firstResult = new Mat();

            int frameWidth = 0;
            int frameHeight = 0;

            Application.Current.Dispatcher.Invoke(() =>
            {
                totalFrameCount = (int)videoCapture2.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                stStatus.Maximum = totalFrameCount;
                stStatus.Value = 0;
                stText.Text = "0%";
                stStatus.Visibility = Visibility.Visible;
                stText.Visibility = Visibility.Visible;
                //mergedVideoCtl.Visibility = Visibility.Collapsed;
                //mergedImage.Visibility = Visibility.Visible;
                this.IsEnabled = false;
            });

            int frameDiffCount = 0;
            while (frameDiffCount != frameCount)
            {
                if (frameDiffCount > frameCount)
                {
                    videoCapture2.Read(frame2);
                    frameDiffCount--;
                }
                else if (frameDiffCount < frameCount)
                {
                    videoCapture1.Read(frame1);
                    frameDiffCount++;
                }
            }


            videoCapture1.Read(frame1);
            videoCapture2.Read(frame2);
            Stitcher.Status status;

            while (have_transform != true)
            {
                VectorOfMat firstVM = new VectorOfMat();
                firstVM.Push(frame1);
                firstVM.Push(frame2);

               status = stitcher.EstimateTransform(firstVM);

                status = stitcher.ComposePanorama(firstVM, firstResult);

                if (status == Stitcher.Status.Ok)
                {
                    frameWidth = firstResult.Width;
                    frameHeight = firstResult.Height;
                    have_transform = true;
                    previous_result = firstResult;
                }
            }



            using (VideoWriter videoWriter = new VideoWriter("output.mp4", 25, new System.Drawing.Size(frameWidth, frameHeight), true))
            {
                while (videoCapture1.IsOpened && videoCapture2.IsOpened)
                {
                    videoCapture1.Read(frame1);
                    videoCapture2.Read(frame2);

                    VectorOfMat vm = new VectorOfMat();
                    //Mat result = new Mat();
                    vm.Push(frame1);
                    vm.Push(frame2);
                    if (frame1.IsEmpty || frame2.IsEmpty)
                        break;

                    //Mat result = Generate_Stitch(frame1, frame2);

                    Mat result = new Mat();

                    status = stitcher.ComposePanorama(vm, result);


                    if (status == Stitcher.Status.Ok)
                    {
                        using (Mat resized_mat = new Mat())
                        {
                            using (Mat showing_mat = new Mat())
                            {
                                CvInvoke.Resize(result, resized_mat, new System.Drawing.Size(frameWidth, frameHeight));
                                CvInvoke.Resize(result, showing_mat, new System.Drawing.Size(620, 298));

                                previous_result = result;

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    stStatus.Value += 1;

                                    int progress = (int)(stStatus.Value / totalFrameCount * 100);

                                    stText.Text = progress.ToString() + "%";
                                });
                                if (videoWriter != null)
                                    videoWriter.Write(resized_mat);
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    mergedImage.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                                });
                            }
                        }
                    }
                    else
                    {
                        videoWriter.Write(previous_result);
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                stStatus.Visibility = Visibility.Collapsed;
                stText.Visibility = Visibility.Collapsed;

                string FileName = "output.mp4";
                string currentDirectory = Directory.GetCurrentDirectory();
                string FilePath = System.IO.Path.Combine(currentDirectory, FileName);
                var uri = new Uri(FilePath);
                //mergedVideoCtl.Source = uri;
                //mergedVideoCtl.Play();
                CenteralSlidePlayStatus = true;
                //leftVideoCtl.Position = TimeSpan.FromSeconds(0);
                //rightVideoCtl.Position = TimeSpan.FromSeconds(0);

                //mergedVideoCtl.Visibility = Visibility.Visible;
                //mergedImage.Visibility = Visibility.Collapsed;
                this.IsEnabled = true;
                this.Activate();
            });
        }


        private void Render_Click(object sender, RoutedEventArgs e)
        {
            Render render = new Render();
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
        }

        private void LeftSlide_DragCompleted(object sender , DragCompletedEventArgs e)
        {
            LeftSlideDraggingFlag = false;
            //leftVideoCtl.Position = TimeSpan.FromSeconds(LeftSlide.Value);
            if (LeftSlidePlayStatus == false && leftVideo != null)
            {
                //leftVideoCtl.Play();
                //PlayMediaElementWithDelay(leftVideoCtl);
                //leftVideoCtl.Pause();
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
        }

        private void RightSlide_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            RightSlideDraggingFlag = false;
            //rightVideoCtl.Position = TimeSpan.FromSeconds(RightSlide.Value);
            if (RightSlidePlayStatus == false && rightVideo != null)
            {
                //rightVideoCtl.Play();
                //PlayMediaElementWithDelay(rightVideoCtl);
                //rightVideoCtl.Pause();
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
            //if(leftVideoCtl != null && leftVideoCtl.NaturalDuration.HasTimeSpan)
            {
                if(LeftSlidePlayStatus)
                {
              //      leftVideoCtl.Pause();
                }
                else
                {
                //    leftVideoCtl.Play();
                }
                LeftSlidePlayStatus = !LeftSlidePlayStatus;
            }
        }
        void OnMouseDownPause2Media(object sender, MouseButtonEventArgs args)
        {
            //if (rightVideoCtl != null && rightVideoCtl.NaturalDuration.HasTimeSpan)
            {
                if (RightSlidePlayStatus)
                {
                 //   rightVideoCtl.Pause();
                }
                else
                {
                  //  rightVideoCtl.Play();
                }
                RightSlidePlayStatus = !RightSlidePlayStatus;
            }

        }

        void OnMouseDownPause3Media(object sender, MouseButtonEventArgs args)
            {
            //if (mergedVideoCtl != null && mergedVideoCtl.NaturalDuration.HasTimeSpan)
            {
                if (CenteralSlidePlayStatus)
                {
           //         mergedVideoCtl.Pause();
                }
                else
                {
            //        mergedVideoCtl.Play();
                }
                CenteralSlidePlayStatus = !CenteralSlidePlayStatus;
            }

        }
    }
}
