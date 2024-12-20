using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

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
        private List<string> _videoPaths;


        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            //_initialMousePosition = Mouse.GetPosition(this);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load video paths from JSON file
            string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "videos.json");
            Console.WriteLine("Loading JSON file from: " + jsonFilePath);

            if (File.Exists(jsonFilePath))
            {
                string jsonString = File.ReadAllText(jsonFilePath);
                FilePaths filePaths = JsonSerializer.Deserialize<FilePaths>(jsonString);
                _videoPaths = filePaths.Videos;

                // Log the loaded video paths
                Console.WriteLine("Loaded video paths:");
                foreach (var path in _videoPaths)
                {
                    Console.WriteLine(path);
                }

                if (_videoPaths.Count > 0)
                {
                    // Set up the main timer
                    _timer = new DispatcherTimer();
                    _timer.Interval = TimeSpan.FromMinutes(10); // 10 minutes interval
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
            if (_currentVideoIndex >= _videoPaths.Count)
            {
                _currentVideoIndex = 0;
            }

            string videoPath = _videoPaths[_currentVideoIndex];
            Console.WriteLine("Playing video: " + videoPath);

            if (File.Exists(videoPath))
            {
                mediaElement.Source = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, videoPath), UriKind.Absolute);
                mediaElement.Play();
            }
            else
            {
                MessageBox.Show("Video file not found: " + videoPath);
                Application.Current.Shutdown();
            }

            _currentVideoIndex++;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            PlayNextVideo();
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Loop the current video until the timer ticks
            mediaElement.Position = TimeSpan.Zero;
            mediaElement.Play();
        }
        //private void Window_MouseMove(object sender, MouseEventArgs e)
        //{ 
        //    if (_isInitialMouseMovementIgnored) return;

        //    Point currentMousePosition = Mouse.GetPosition(this);
        //    if (Math.Abs(currentMousePosition.X - _initialMousePosition.X) > 50 || Math.Abs(currentMousePosition.Y - _initialMousePosition.Y) > 50)
        //    { Console.WriteLine("Mouse moved, exiting screensaver.");
        //        Application.Current.Shutdown(); 
        //    }
        //}

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mousePosition != default && (_mousePosition != Mouse.GetPosition(this))) { Application.Current.Shutdown(); }
            _mousePosition = Mouse.GetPosition(this);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Mouse button pressed, exiting screensaver.");
            Application.Current.Shutdown();
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
}
