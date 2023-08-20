using Microsoft.Win32;
using System;
using System.Text;
using System.Windows;
using System.Diagnostics;
using System.Windows.Interop;
using System.IO;

namespace WPFVideoStitch
{
    /// <summary>
    /// Interaction logic for VideoMerger.xaml
    /// </summary>
    public partial class VideoMerger : Window
    {
        public VideoMerger()
        {
            InitializeComponent();
        }

        private void Add_Videos(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Video files (*.mp4)|*.mp4|Video files (*.avi)|*.avi|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var filename in openFileDialog.FileNames)
                {
                    VideoPanel.Items.Add(filename);
                }
            }
        }

        private void Merge_Click(object sender, RoutedEventArgs e)
        {
            //            string strCmdText = " -f concat -safe 0 -i videos.txt -c copy out.mp4";
            if(VideoPanel.Items.Count == 0)
            {
                MessageBox.Show("Please select video files.", "Alert", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            File.WriteAllText("videos.txt", "");
            foreach (var item in VideoPanel.Items)
            {
                File.AppendAllText("videos.txt", "file '" + item.ToString() +"'\n");
            }
            outputList.Items.Clear();
            outputList.Items.Add("Merging...");

            string command = "ffmpeg";
            string arguments = " -f concat -safe 0 -i videos.txt -c copy \""+outputPath.Text+"/out.mp4\"";

            var process = new Process();

            process.StartInfo.FileName = command;
            if (!string.IsNullOrEmpty(arguments))
            {
                process.StartInfo.Arguments = arguments;
            }

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            var stdOutput = new StringBuilder();
            process.OutputDataReceived += (sender, args) => stdOutput.AppendLine(args.Data); // Use AppendLine rather than Append since args.Data is one line of output, not including the newline character.

            string stdError = null;
            try
            {
                process.Start();
                process.BeginOutputReadLine();
                stdError = process.StandardError.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                //                throw new Exception("OS error while executing " + Format(filename, arguments) + ": " + e.Message, e);
                outputList.Items.Add(ex.Message);
            }

            if (process.ExitCode == 0)
            {
                stdOutput.ToString();
                outputList.Items.Add(stdOutput.ToString());
            }
            else
            {
                var message = new StringBuilder();

                if (!string.IsNullOrEmpty(stdError))
                {
                    message.AppendLine(stdError);
                }

                if (stdOutput.Length != 0)
                {
                    message.AppendLine("Std output:");
                    message.AppendLine(stdOutput.ToString());
                }
                outputList.Items.Add(message);
                //throw new Exception(Format(filename, arguments) + " finished with exit code = " + process.ExitCode + ": " + message);
            }
            outputList.Items.Add("Finished");
        }
        private void ListClear_Click(object sender, RoutedEventArgs e)
        {
            VideoPanel.Items.Clear();
        }
        private void OutputPath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog();
            var result = openFileDlg.ShowDialog();
            if (result.ToString() != string.Empty)
            {
                outputPath.Text = openFileDlg.SelectedPath;
            }
        }
        private void Sort_Click(object sender, RoutedEventArgs e)
        {

        }
    }

}
