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

namespace WPFVideoStitch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        String rightVideo;
        String leftVideo;
        Stitcher stitcher;


        bool LeftSlideDraggingFlag = false;
        bool RightSlideDraggingFlag = false;
        bool CenteralSlideDraggingFlag = false;

        bool LeftSlidePlayStatus = false;
        bool RightSlidePlayStatus = false;
        bool CenteralSlidePlayStatus = false;

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


        }

        void ticktock(object sender, EventArgs e)
        {
            if((leftVideoCtl.Source != null) && (leftVideoCtl.NaturalDuration.HasTimeSpan) && (!LeftSlideDraggingFlag))
                LeftSlide.Value = leftVideoCtl.Position.TotalSeconds;
            if ((rightVideoCtl.Source != null) && (rightVideoCtl.NaturalDuration.HasTimeSpan) && (!RightSlideDraggingFlag))
                RightSlide.Value = rightVideoCtl.Position.TotalSeconds;
            if ((mergedVideoCtl.Source != null) && (mergedVideoCtl.NaturalDuration.HasTimeSpan) && (!CenteralSlideDraggingFlag))
                CenterSlide.Value = mergedVideoCtl.Position.TotalSeconds;
        }

        private void leftVideoCtl_MediaOpened(object sender, RoutedEventArgs e)
        {
            _position = leftVideoCtl.NaturalDuration.TimeSpan;
            LeftSlide.Minimum = 1;
            LeftSlide.Maximum = _position.TotalSeconds;
        }
        private void rightVideoCtl_MediaOpened(object sender, RoutedEventArgs e)
        {
            _position = rightVideoCtl.NaturalDuration.TimeSpan;
            RightSlide.Minimum = 1;
            RightSlide.Maximum = _position.TotalSeconds;
        }

        private void mergedVideoCtl_MediaOpened(object sender, RoutedEventArgs e)
        {
            _position = mergedVideoCtl.NaturalDuration.TimeSpan;
            CenterSlide.Minimum = 1;
            CenterSlide.Maximum = _position.TotalSeconds;
        }

        private void ExtractAudioFormVideo(string videoFilePath , string outputFilePath)
        {
            string ffmpegPath = @"ffmpeg.exe";
            string arguments = $"-i \"{videoFilePath}\" -vn -acodec copy \"{outputFilePath}\"";

            var process = new Process
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

                        if (File.Exists("left.wav"))
                        {
                            // Delete the existing file
                            File.Delete("left.wav");
                        }

                        ExtractAudioFormVideo(file, "left.wav");
                        leftVideoCtl.Source = new Uri(leftVideo);
//                        leftVideoCtl.MediaOpened += LeftMediaElement_MediaOpened;
                        leftVideoCtl.Play();
                        LeftSlidePlayStatus = true;
                    }
                    else if (i == 2)
                    {
                        rightVideo = file;

                        if (File.Exists("right.wav"))
                        {
                            // Delete the existing file
                            File.Delete("right.wav");
                        }

                        ExtractAudioFormVideo(file, "right.wav");

                        rightVideoCtl.Source = new Uri(rightVideo);
                        rightVideoCtl.Play();
                        RightSlidePlayStatus = true;
                    }
                }
                               
            }
        }
        private void Synchronization_Click(object sender, RoutedEventArgs e)
        {
            VideoSyncronization videoSyncronization = new VideoSyncronization(leftVideo, rightVideo);
            videoSyncronization.Show();
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

            Thread thread = new Thread(CallVideoCreate);

            thread.Start();

        }


        public void CallVideoCreate()
        {
            using VideoCapture videoCapture1 = new VideoCapture(leftVideo);
            using VideoCapture videoCapture2 = new VideoCapture(rightVideo);
            using Mat frame1 = new Mat();
            using Mat frame2 = new Mat();
            videoCapture1.Read(frame1);
            videoCapture2.Read(frame2);

            Application.Current.Dispatcher.Invoke(() =>
            {
                stStatus.Maximum = (int)videoCapture2.Get(Emgu.CV.CvEnum.CapProp.FrameCount);

                myText.Content = stStatus.Maximum.ToString();
                stStatus.Value = 0;
                stStatus.Visibility = Visibility.Visible;
                stText.Visibility = Visibility.Visible;

                this.IsEnabled = false;
            });


            //Stitcher stitcher = 
            //frame1.Save("1.png");

            Mat first_result = Generate_Stitch(frame1, frame2);
            int frameWidth = first_result.Width;
            int frameHeight = first_result.Height;
    



            using (VideoWriter videoWriter = new VideoWriter("output.mp4", 25, new System.Drawing.Size(frameWidth, frameHeight), true))
            {
                while (videoCapture1.IsOpened && videoCapture2.IsOpened)
                {
                    videoCapture1.Read(frame1);
                    videoCapture2.Read(frame2);


                    if (frame1.IsEmpty || frame2.IsEmpty)
                        break;

                    Mat result = Generate_Stitch(frame1, frame2);

                    if (result.Width != frame1.Width)
                    {
                        using Mat resized_mat = new Mat();
                        CvInvoke.Resize(result, resized_mat, new System.Drawing.Size(frameWidth, frameHeight));
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            myText.Content = stStatus.Value.ToString();
                            stStatus.Value += 1;
                        });
                        result.Dispose();
                        videoWriter.Write(resized_mat);
                    }
                }
                videoWriter.Dispose();
                
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                stStatus.Visibility = Visibility.Collapsed;
                stText.Visibility = Visibility.Collapsed;


                //MessageBox.Show("Stitching video has finished!", "Success!");

                string FileName = "output.mp4";
                string currentDirectory = Directory.GetCurrentDirectory();
                string FilePath = System.IO.Path.Combine(currentDirectory, FileName);

                var uri = new Uri(FilePath);

                mergedVideoCtl.Source = uri;
                mergedVideoCtl.Play();

                CenteralSlidePlayStatus = true;

                leftVideoCtl.Position = TimeSpan.FromSeconds(0);
                rightVideoCtl.Position = TimeSpan.FromSeconds(0);

                this.IsEnabled = true;
                this.Activate();

            });

            frame1.Dispose();
            frame2.Dispose();
            videoCapture1.Dispose();
            videoCapture2.Dispose();
        }

        private Mat Generate_Stitcher(Mat mat1, Mat mat2)
        {
            using (Stitcher stitcher = new Stitcher())
            using (Emgu.CV.Features2D.AKAZE finder = new Emgu.CV.Features2D.AKAZE())
            using (Emgu.CV.Stitching.WarperCreator warper = new SphericalWarper())
            {
                stitcher.SetFeaturesFinder(finder);
                stitcher.SetWarper(warper);
                using (VectorOfMat vm = new VectorOfMat())
                {
                    Image<Bgr, byte>[] sourceImages = new Image<Bgr, byte>[2];
                    sourceImages[0] = mat1.ToImage<Bgr, Byte>();
                    sourceImages[1] = mat2.ToImage<Bgr, Byte>();

                    if (mat1 == null || mat2 == null) return mat1;

                    Mat result = new Mat();
                    vm.Push(sourceImages);
                    try
                    {
                        Stitcher.Status stitchStatus = stitcher.Stitch(vm, result);
                        if (stitchStatus == Stitcher.Status.Ok)
                            return result;
                        else
                            return mat1;
                    }
                    catch (Exception e)
                    {
                        return mat1;
                    }
                }
            }
        } 
        private Mat Generate_Stitch(Mat mat1 , Mat mat2)
        {
            using (Stitcher stitcher = new Stitcher())
            using (Emgu.CV.Features2D.AKAZE finder = new Emgu.CV.Features2D.AKAZE())
            using (Emgu.CV.Stitching.WarperCreator warper = new SphericalWarper())
            {
                stitcher.SetFeaturesFinder(finder);
                stitcher.SetWarper(warper);
                using (VectorOfMat vm = new VectorOfMat())
                {            
                    Image<Bgr, byte>[] sourceImages = new Image<Bgr, byte>[2];
                    sourceImages[0] = mat1.ToImage<Bgr , Byte>();
                    sourceImages[1] = mat2.ToImage<Bgr, Byte>();

                    if(mat1 == null || mat2 == null) return mat1;

                    Mat result = new Mat();
                    vm.Push(sourceImages);
                    try
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            myText.Content = "before";
                        });

                        Stitcher.Status stitchStatus = stitcher.Stitch(vm, result);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            myText.Content = "after";
                        });
                        if (stitchStatus == Stitcher.Status.Ok)
                            return result;
                        else
                            return mat1;



                    } catch (Exception e)
                    {
                        return mat1;
                    }
                }
            }
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
            leftVideoCtl.Position = TimeSpan.FromSeconds(LeftSlide.Value);
            if (LeftSlidePlayStatus == false && leftVideo != null)
            {
                leftVideoCtl.Play();
                leftVideoCtl.Pause();
            }
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
            rightVideoCtl.Position = TimeSpan.FromSeconds(RightSlide.Value);
            if (RightSlidePlayStatus == false && rightVideo != null)
            {
                rightVideoCtl.Play();
                rightVideoCtl.Pause();
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
            mergedVideoCtl.Position = TimeSpan.FromSeconds(CenterSlide.Value);
            if (CenteralSlidePlayStatus == false)
            {
                mergedVideoCtl.Play();
                mergedVideoCtl.Pause();
            }
        }
        void OnMouseDownPause1Media(object sender, MouseButtonEventArgs args)
        {
            if(leftVideoCtl != null && leftVideoCtl.NaturalDuration.HasTimeSpan)
            {
                if(LeftSlidePlayStatus)
                {
                    leftVideoCtl.Pause();
                }
                else
                {
                    leftVideoCtl.Play();
                }
                LeftSlidePlayStatus = !LeftSlidePlayStatus;
            }
        }
        void OnMouseDownPause2Media(object sender, MouseButtonEventArgs args)
        {
            if (rightVideoCtl != null && rightVideoCtl.NaturalDuration.HasTimeSpan)
            {
                if (RightSlidePlayStatus)
                {
                    rightVideoCtl.Pause();
                }
                else
                {
                    rightVideoCtl.Play();
                }
                RightSlidePlayStatus = !RightSlidePlayStatus;
            }

        }

        void OnMouseDownPause3Media(object sender, MouseButtonEventArgs args)
            {
            if (mergedVideoCtl != null && mergedVideoCtl.NaturalDuration.HasTimeSpan)
            {
                if (CenteralSlidePlayStatus)
                {
                    mergedVideoCtl.Pause();
                }
                else
                {
                    mergedVideoCtl.Play();
                }
                CenteralSlidePlayStatus = !CenteralSlidePlayStatus;
            }

        }
    }
}
