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
        private void Stitch_Click(object sender, RoutedEventArgs e)
        {

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
