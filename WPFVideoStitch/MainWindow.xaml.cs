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


        private void GetFrameCount(string videoPath)
        {
            string ffmpegPath = @"ffmpeg.exe";
            //string arguments = $"-i \"{videoFilePath}\" -vn -acodec copy \"{outputFilePath}\"";
            string arguments = $"-i \"{videoPath}\" -map 0:v:0 -c copy -f null - 2>&1 | findstr \"frame\" > output.txt";
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

                        LeftSlide.Minimum = 0;
                        LeftSlide.Maximum = leftCapture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                        lefttotalframecount = (int)LeftSlide.Maximum;
                        //GetFrameCount(file);


                        //leftVideoCtl.Source = new Uri(leftVideo);
                        //                        leftVideoCtl.MediaOpened += LeftMediaElement_MediaOpened;
                        //leftVideoCtl.Play();

                        //PlayMediaElementWithDelay(leftVideoCtl);
                        //leftVideoCtl.Pause();
                        leftThreadRunning = true;
                        
                        
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
                        RightSlide.Minimum = 0;
                        RightSlide.Maximum = rightCapture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                        righttotalframecount = (int)RightSlide.Maximum;

                        RightSlidePlayStatus = false;

                        Synchronization.Visibility = Visibility.Visible;
                        Stitch.Visibility = Visibility.Visible;
                        Render.Visibility = Visibility.Visible;

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
            leftMat.Save("left.jpg");

            rightCapture.Read(rightMat);
            rightMat.Save("right.jpg");

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

            System.Threading.Thread thread = new System.Threading.Thread(CallStitching);
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

                    leftCapture.Dispose();
                    leftCapture = new VideoCapture(leftVideo);

                    leftCapture.Read(leftMat);
                    //Mat frame= leftCapture.QuerySmallFrame();
                    //leftVideoCtl.Source = ToBitmapSource(frame.ToImage<Bgr, byte>());


                    LeftSlide.Minimum = 0;
                    LeftSlide.Maximum = leftCapture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                    lefttotalframecount = (int)LeftSlide.Maximum;

                    ExtractImageFromVideo(leftVideo, 1, leftVideoCtl);


                    //leftVideoCtl.Source = new Uri(files[0]);
                    //leftVideoCtl.Play();
                    //PlayMediaElementWithDelay(leftVideoCtl);
                    //leftVideoCtl.Pause();
                    LeftSlidePlayStatus = false;

                    leftThreadRunning = true;
                    System.Threading.Thread left = new System.Threading.Thread(ShowLeftVideo);
                    left.Start();

                    if ( rightVideo!= "" )
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


                    rightCapture.Dispose();
                    rightCapture = new VideoCapture(rightVideo);

                    rightCapture.Read(rightMat);

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

                    rightThreadRunning = true;
                    System.Threading.Thread right = new System.Threading.Thread(ShowRightVideo);
                    right.Start();

                    if (leftVideo != "")
                    {
                        Synchronization.Visibility = Visibility.Visible;
                        Stitch.Visibility = Visibility.Visible;
                        Render.Visibility = Visibility.Visible;
                    }

                }
            }
        }

        public void CallStitching()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                stStatus.Visibility = Visibility.Visible;
                stStatus.IsIndeterminate = true;
                this.IsEnabled = false;


                LeftSlidePlayStatus = false;
                RightSlidePlayStatus = false;
            });

            using (Emgu.CV.Features2D.AKAZE finder = new Emgu.CV.Features2D.AKAZE())
            using (Emgu.CV.Stitching.WarperCreator warper = new SphericalWarper())
            {
                stitcher.SetFeaturesFinder(finder);
                stitcher.SetWarper(warper);
                using (VectorOfMat firstVM = new VectorOfMat())
                using (Mat resultMat = new Mat())
                {



                    /*                    Image<Bgr, byte>[] sourceImages = new Image<Bgr, byte>[2];
                                        sourceImages[0] = new Image<Bgr, byte>("left.jpg");
                                        sourceImages[1] = new Image<Bgr, byte>("right.jpg");*/
                    //firstVM.Push(sourceImages);

                    leftCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, lefttotalframecount/2);
                    rightCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, lefttotalframecount/2 - frameCount);

                    while(leftCapture.IsOpened && rightCapture.IsOpened)
                    {

                        leftCapture.Read(leftMat);
                        rightCapture.Read(rightMat);

                        firstVM.Clear();

                        firstVM.Push(leftMat);
                        firstVM.Push(rightMat);

                        Stitcher.Status status = stitcher.EstimateTransform(firstVM);

                        status = stitcher.ComposePanorama(firstVM, resultMat);

                        if (status == Stitcher.Status.Ok)
                            break;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        //stStatus.Visibility = Visibility.Collapsed;
                        stStatus.IsIndeterminate = false;
                        stStatus.Maximum = leftCapture.Get(Emgu.CV.CvEnum.CapProp.FrameCount);
                        stStatus.Value = leftVideoSlideValue;
                        mergedImage.Source = ToBitmapSource(resultMat.ToImage<Bgr, byte>());
                    });

                    while (leftCapture.IsOpened && rightCapture.IsOpened)
                    {
                        leftCapture.Read(leftMat);
                        rightCapture.Read(rightMat);

                        firstVM.Clear();

                        firstVM.Push(leftMat);
                        firstVM.Push(rightMat);
                        if (leftMat.IsEmpty || rightMat.IsEmpty)
                            break;

                        //Mat result = Generate_Stitch(frame1, frame2);

                        using (Mat result = new Mat())
                        {
                            Stitcher.Status status = stitcher.ComposePanorama(firstVM, result);

                            if (status == Stitcher.Status.Ok)
                            {
                                using (Mat resized_mat = new Mat())
                                {
                                    using (Mat showing_mat = new Mat())
                                    {
                                        CvInvoke.Resize(result, showing_mat, new System.Drawing.Size(620, 298));

                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            stStatus.Value += 2;
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

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.IsEnabled = true;
                        stStatus.Visibility = Visibility.Collapsed;

                        leftCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, leftVideoSlideValue);
                        rightCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames , rightVideoSlideValue);
                    });
                }
            }

            //mergedThreadRunning = true;
            //System.Threading.Thread merged = new System.Threading.Thread(ShowMergedVideo);
            //merged.Start();
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
            tempStatus = LeftSlidePlayStatus;
            LeftSlidePlayStatus = false;
        }

        private void LeftSlide_DragCompleted(object sender , DragCompletedEventArgs e)
        {
            LeftSlideDraggingFlag = false;
            leftVideoSlideValue = (int)LeftSlide.Value;



            leftCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames , leftVideoSlideValue);

            using (Mat frame = leftCapture.QuerySmallFrame())
            {
                LeftSlide.Value = leftVideoSlideValue;

                Mat showing_mat = new Mat();
                CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));

                leftVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                LeftSlidePlayStatus = tempStatus;
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
            rightCapture.Set(Emgu.CV.CvEnum.CapProp.PosFrames, rightVideoSlideValue);


            using (Mat frame = rightCapture.QuerySmallFrame())
            {
                RightSlide.Value = rightVideoSlideValue;
                Mat showing_mat = new Mat();
                CvInvoke.Resize(frame, showing_mat, new System.Drawing.Size(800, 600));
                rightVideoCtl.Source = ToBitmapSource(showing_mat.ToImage<Bgr, byte>());
                RightSlidePlayStatus = tempStatus;
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
        }
        void OnMouseDownPause2Media(object sender, MouseButtonEventArgs args)
        {

            RightSlidePlayStatus = !RightSlidePlayStatus;
        }

        void OnMouseDownPause3Media(object sender, MouseButtonEventArgs args)
        {
            if(LeftSlidePlayStatus == false && RightSlidePlayStatus == false) { LeftSlidePlayStatus = true; RightSlidePlayStatus = true; }
            else { LeftSlidePlayStatus = false; RightSlidePlayStatus = false; }
        }
    }
}
