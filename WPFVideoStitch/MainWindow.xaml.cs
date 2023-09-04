﻿using Emgu.CV.Stitching;
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
using System.Windows.Interop;
using System.Drawing;

namespace WPFVideoStitch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        String rightVideo;
        String leftVideo;
        public MainWindow()
        {
            InitializeComponent();
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
                        leftVideoCtl.Source = new Uri(leftVideo);
                        leftVideoCtl.Play();
                    }
                    else if (i == 2)
                    {
                        rightVideo = file;
                        rightVideoCtl.Source = new Uri(rightVideo);
                        rightVideoCtl.Play();
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
            Image<Bgr, byte>[] sourceImages = new Image<Bgr, byte>[2];
            sourceImages[0] = new Image<Bgr, byte> ("cam1.jpg");
            sourceImages[1] = new Image<Bgr, byte>("cam2.jpg");
/*            using (Mat frame = new Mat())
            using (VideoCapture capture = new VideoCapture("Left.avi"))
            while (CvInvoke.WaitKey(1) == -1)
            {
                capture.Read(frame);
                CvInvoke.Imshow("ss", frame);
            }
*/
            //            sourceImages[2] = new Image<Bgr, byte>("cam3.jpg");
            //            sourceImages[3] = new Image<Bgr, byte>("cam4.jpg");
            //            Capture _capture;
            //            _capture = new Capture("test.avi");
            using (Stitcher stitcher = new Stitcher())
            using (Emgu.CV.Features2D.AKAZE finder = new Emgu.CV.Features2D.AKAZE())
            using (Emgu.CV.Stitching.WarperCreator warper = new SphericalWarper())
            {
                stitcher.SetFeaturesFinder(finder);
                stitcher.SetWarper(warper);
                using (VectorOfMat vm = new VectorOfMat())
                {
                    Mat result = new Mat();
                    vm.Push(sourceImages);

//                    Stopwatch watch = Stopwatch.StartNew();

//                    this.Text = "Stitching";
                    Stitcher.Status stitchStatus = stitcher.Stitch(vm, result);
//                    watch.Stop();

                    if (stitchStatus == Stitcher.Status.Ok)
                    {

//                        ImageViewer viewer = new ImageViewer();
//                        viewer.Image = result;
//                        viewer.Show();
                        mergedVideoCtl.Source = ToBitmapSource(result.ToImage<Bgr, byte>());
                        //                      resultImageBox.Image = result;
                        //                    this.Text = String.Format("Stitched in {0} milliseconds.", watch.ElapsedMilliseconds);
                    }
                    else
                    {
                        MessageBox.Show(this, String.Format("Stiching Error: {0}", stitchStatus));
                        mergedVideoCtl.Source = ToBitmapSource(sourceImages[0]);
                        //                     resultImageBox.Image = null;
                    }
                }
                    
                // code to display or save the result 
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
    }
}
