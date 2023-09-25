using com.sun.org.apache.bcel.@internal.generic;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFVideoStitch
{
    /// <summary>
    /// Interaction logic for Render.xaml
    /// </summary>
    public partial class Render : Window
    {
        public Render()
        {
            InitializeComponent();
            savePath.Text = Properties.Settings.Default.LastFilePath;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Create a new MediaElement control
            /*
             * MediaElement mediaElement = new MediaElement();

            // Set the source of the MediaElement to the video file
            string videoFileName = "output.mp4";
            string currentDirectory = Directory.GetCurrentDirectory();
            string VideoFilePath = System.IO.Path.Combine(currentDirectory, videoFileName);
            var videouri = new Uri(VideoFilePath);

            mediaElement.Source = videouri;

            string AudioFileName = "left.wav";
            string AudioFilePath = System.IO.Path.Combine(currentDirectory, AudioFileName);
            var audiouri = new Uri(AudioFilePath);

            // Create a new MediaTimeline and set its source to the audio file
            MediaTimeline audioTimeline = new MediaTimeline(audiouri);

            // Create a new MediaClock and set its timeline to the audio timeline
            MediaClock audioClock = audioTimeline.CreateClock();

            // Set the clock of the MediaElement to the audio clock
            mediaElement.Clock = audioClock;

            // Create a new MediaEncoder and set its source to the MediaElement
            MediaFoundationEncoder encoder = new MediaFoundationEncoder(mediaElement);

            // Set the output file path of the encoder
            encoder.OutputFilePath = savePath;

            // Start the encoding process
            encoder.Encode();
            */

            string ffmpegPath = @"ffmpeg.exe";
            string arguments = $"-i \"output.mp4\" -i \"left.wav\" -c:v copy -map 0:v:0 -map 1:a:0 -c:a aac -b:a 192k hello.mp4";

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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog();

            openFileDlg.InitialDirectory = Properties.Settings.Default.LastFilePath;

            var result = openFileDlg.ShowDialog();
            if (result.ToString() != string.Empty)
            {
                savePath.Text = openFileDlg.SelectedPath;

                Properties.Settings.Default.LastFilePath = openFileDlg.SelectedPath;

                Properties.Settings.Default.Save();
            }
        }
    }
}
