using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;


namespace VideoScreenSaver
{
    public partial class MainWindow : Window
    {
        private Point _mousePosition;
        //private Point _initialMousePosition;
        private bool _isInitialMouseMovementIgnored;
        private DispatcherTimer _timer;
        private DispatcherTimer _delayTimer;
        private int _currentVideoIndex = 0;
        //private List<string> _videoPaths;
        private string _videoPaths;
        public static List<MediaConfig> mediaConfigs = new List<MediaConfig>();

        private Process ffplayProcess;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            //Activated += MainWindow_Activated;
            ContentRendered += MainWindow_ContentRendered;
            mediaElement.IsMuted = true;
            //_initialMousePosition = Mouse.GetPosition(this);
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            var controller = ImageBehavior.GetAnimationController(gifImage);
            controller?.Play();
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            if (gifImage.Source != null)
            {
                // Force restart of GIF animation
                var src = gifImage.Source;
                gifImage.Source = null;
                gifImage.Source = src;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load video paths from JSON file
            string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "videos.json");
            Console.WriteLine("Loading JSON file from: " + jsonFilePath);

            if (File.Exists(jsonFilePath))
            {
                string jsonString = File.ReadAllText(jsonFilePath);
                mediaConfigs = JsonSerializer.Deserialize<List<MediaConfig>>(jsonString);
                //MediaConfig filePaths = JsonSerializer.Deserialize<MediaConfig>(jsonString);

                if (mediaConfigs.Count > 0)
                    {
                        // Set up the main timer
                        _timer = new DispatcherTimer();
                        _timer.Interval = TimeSpan.FromMinutes(mediaConfigs[_currentVideoIndex].Delay);
                        //Amount of delay in minutes interval
                        _timer.Tick += Timer_Tick;
                        _timer.Start();

                        // Set up the delay timer for mouse and keyboard detection
                        _delayTimer = new DispatcherTimer();
                        _delayTimer.Interval = TimeSpan.FromSeconds(2); // 2 seconds delay
                        _delayTimer.Tick += DelayTimer_Tick;
                        _delayTimer.Start();

                        // Start playing the first video
                        PlayNextVideo();
                    }
                    else
                    {
                        MessageBox.Show("No videos found in JSON file.");
                        Application.Current.Shutdown();
                    }
            }
            else
            {
                MessageBox.Show("JSON file not found.");
                Application.Current.Shutdown();
            }
        }

        private void DelayTimer_Tick(object sender, EventArgs e)
        {
            // Enable mouse and keyboard detection after the delay
            _delayTimer.Stop();
            Console.WriteLine("Enabling mouse and keyboard event handlers.");
            this.MouseMove += Window_MouseMove;
            this.MouseDown += Window_MouseDown;
            this.KeyDown += Window_KeyDown;
        }

        private void PlayNextVideo()
        {
            if (_currentVideoIndex >= mediaConfigs.Count)
                _currentVideoIndex = 0;

            string path = mediaConfigs[_currentVideoIndex].Videos;

            if (!Path.IsPathRooted(path))
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

            Console.WriteLine("Loading media: " + path);

            if (!File.Exists(path))
            {
                MessageBox.Show("Media file not found: " + path);
                Application.Current.Shutdown();
                return;
            }

            string ext = Path.GetExtension(path).ToLower();

            if (ext == ".gif")
            {
                PlayGif(path);
            }
            else
            {
                PlayVideo(path);
            }

            _currentVideoIndex++;
        }

        private void PlayVideo(string videoPath)
        {
            gifImage.Visibility = Visibility.Collapsed;
            mediaElement.Visibility = Visibility.Visible;

            mediaElement.IsMuted = mediaConfigs[_currentVideoIndex].Sound == 0;
            mediaElement.Source = new Uri(videoPath, UriKind.Absolute);
            mediaElement.Play();
        }

        private void PlayGif(string gifPath)
        {
            gifImage.Visibility = Visibility.Visible;
            mediaElement.Visibility = Visibility.Collapsed;

            var config = mediaConfigs[_currentVideoIndex];
            double scale = config.Size / 100.0;

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(gifPath, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();

            ImageBehavior.SetAnimatedSource(gifImage, image);
            ImageBehavior.SetRepeatBehavior(gifImage, System.Windows.Media.Animation.RepeatBehavior.Forever);
            ImageBehavior.SetAutoStart(gifImage, true);
            //ImageBehavior.SetStretch(gifImage, System.Windows.Media.Stretch.Uniform);
            // Remove or comment out the following line, as ImageBehavior does not have a SetStretch method:
            // ImageBehavior.SetStretch(gifImage, System.Windows.Media.Stretch.Uniform);

            // Instead, set the Stretch property directly on the gifImage control:
            gifImage.Stretch = System.Windows.Media.Stretch.Uniform;

            // Apply scaling transform
            gifImage.RenderTransform = new ScaleTransform(scale, scale);
            gifImage.RenderTransformOrigin = new Point(0.5, 0.5); // center it
        }
        
       

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_currentVideoIndex >= mediaConfigs.Count)
            {
                _currentVideoIndex = 0;
            }

            _timer.Interval = TimeSpan.FromMinutes(mediaConfigs[_currentVideoIndex].Delay);
            // Amount of delay in minutes interval
            _timer.Stop();
            _timer.Start();

            PlayNextVideo();
        }

        private async Task PlayVideoFromMemoryAsync(string VideoPath)
        {
            // Load MP4 file into memory
            byte[] videoBytes = File.ReadAllBytes(VideoPath);

            // Create a MemoryStream
            using (MemoryStream videoStream = new MemoryStream(videoBytes))
            {
                // Save the stream to a temporary file
                string tempFile = Path.GetTempFileName();
                File.WriteAllBytes(tempFile, videoStream.ToArray());

                // Use FFmpeg to play the video
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "ffplay",
                    Arguments = $"-vf \"scale=1980:1080\" -autoexit -loop 0 -volume 0 \"{tempFile}\"",
                    UseShellExecute = false,
                    //RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                ffplayProcess = new Process { StartInfo = startInfo };
                ffplayProcess.Start(); await Task.Run(() => ffplayProcess.WaitForExit());
            }
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Loop the current video until the timer ticks
            mediaElement.Position = TimeSpan.Zero;
            mediaElement.Play();
        }
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mousePosition != default && (_mousePosition != Mouse.GetPosition(this)))
            {

                Application.Current.Shutdown();
                // Stop the video playback this.Close();

            }
            _mousePosition = Mouse.GetPosition(this);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Mouse button pressed, exiting screensaver.");
            Application.Current.Shutdown();
            //ffplayProcess?.Kill();
            //this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine("Key pressed, exiting screensaver.");
            Application.Current.Shutdown();
        }
    }

    public class FilePaths
    {
        public List<string> Videos { get; set; }
    }

    public class MediaConfig
    {
    //[
    // {
    //  "videos": "Formula1_1.mp4",
    //  "sound": 1,
    //  "delay": 10
    // }
    //]
 
        [JsonPropertyName("videos")]
        public string Videos { get; set; }

        [JsonPropertyName("sound")]
        public int Sound { get; set; }

        [JsonPropertyName("delay")]
        public int Delay { get; set; } // Delay in seconds

        [JsonPropertyName("size")]
        public int Size { get; set; } = 100; // default to 100%
    }
}
