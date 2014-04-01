using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Nokia.Graphics.Imaging;
using ImageSequencer.Resources;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImageSequencer
{
    public partial class MainPage : PhoneApplicationPage
    {
        private ApplicationBarIconButton _playButton;
        private ApplicationBarIconButton _alignButton;
        private ApplicationBarIconButton _frameButton;
        private ApplicationBarIconButton _saveButton;

        private IReadOnlyList<IImageProvider> _unalignedImageProviders;
        private IReadOnlyList<IImageProvider> _alignedImageProviders;
        private IReadOnlyList<IImageProvider> _onScreenImageProviders;

        private WriteableBitmap _onScreenImage;

        private bool _alignEnabled;
        private bool _frameEnabled;

        private int _animationIndex = 0;
        private DispatcherTimer _animationTimer;
        private volatile bool _rendering;

        private Point _dragStart;

        private RectangleGeometry _animatedArea;

        private Semaphore _semaphore = new Semaphore(1, 1);

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            InitializeApplicationBar();
            
            _animationTimer = new DispatcherTimer();
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            Stop();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            String sequenceIdParam;
            int sequenceId = 1;

            if (NavigationContext.QueryString.TryGetValue("sequenceId", out sequenceIdParam))
            {
                sequenceId = Convert.ToInt32(sequenceIdParam);
            }

            List<IImageProvider> imageSequence = CreateImageSequenceFromResources(sequenceId);

            SetImageSequence(imageSequence);
        }

        private void InitializeApplicationBar()
        {
            ApplicationBar = new ApplicationBar();

            _playButton = new ApplicationBarIconButton();
            _playButton.IconUri = new Uri("/Assets/appbar.play.png", UriKind.Relative);
            _playButton.Text = "play";            
            _playButton.Click += new EventHandler(PlayButton_Click);
            ApplicationBar.Buttons.Add(_playButton);

            _alignButton = new ApplicationBarIconButton();
            _alignButton.IconUri = new Uri(@"Assets/appbar.align.disabled.png", UriKind.Relative);
            _alignButton.Text = "align";            
            _alignButton.Click += new EventHandler(AlignButton_Click);
            ApplicationBar.Buttons.Add(_alignButton);

            _frameButton = new ApplicationBarIconButton();
            _frameButton.IconUri = new Uri(@"Assets/appbar.frame.disabled.png", UriKind.Relative);
            _frameButton.Text = "frame";
            _frameButton.Click += new EventHandler(FrameButton_Click);
            ApplicationBar.Buttons.Add(_frameButton);

            _saveButton = new ApplicationBarIconButton();
            _saveButton.IconUri = new Uri(@"Assets/appbar.save.png", UriKind.Relative);
            _saveButton.Text = "save";
            _saveButton.Click += new EventHandler(SaveButton_Click);
            ApplicationBar.Buttons.Add(_saveButton);

            ApplicationBarMenuItem aboutMenuItem = new ApplicationBarMenuItem();
            aboutMenuItem.Text = "about";
            aboutMenuItem.Click += (s, e) => NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
            ApplicationBar.MenuItems.Add(aboutMenuItem);

        }

        private void SetControlsEnabled(bool enabled)
        {
            _playButton.IsEnabled = enabled;
            _alignButton.IsEnabled = enabled;
            _frameButton.IsEnabled = enabled;
            _saveButton.IsEnabled = enabled;
        }

        public async void SetImageSequence(List<IImageProvider> imageProviders)
        {
            ShowProgressIndicator("Aligning frames");
            SetControlsEnabled(false);

            _unalignedImageProviders = imageProviders;
            _onScreenImageProviders = _unalignedImageProviders;

            // Create aligned images
            using (ImageAligner imageAligner = new ImageAligner())
            {
                imageAligner.Sources = _unalignedImageProviders;
                imageAligner.ReferenceSource = _unalignedImageProviders[0];

                _alignedImageProviders = await imageAligner.AlignAsync();
            }

            // Create on-screen bitmap for rendering the image providers
            IImageProvider imageProvider = _onScreenImageProviders[0];
            ImageProviderInfo info = await imageProvider.GetInfoAsync();
            int width = (int)info.ImageSize.Width;
            int height = (int)info.ImageSize.Height;

            _onScreenImage = new WriteableBitmap(width, height);

            // Render the first frame of sequence
            using (WriteableBitmapRenderer writeableBitmapRenderer = new WriteableBitmapRenderer(imageProvider, _onScreenImage))
            {
                ImageElement.Source = await writeableBitmapRenderer.RenderAsync();
                writeableBitmapRenderer.Source = imageProvider;
                writeableBitmapRenderer.WriteableBitmap = new WriteableBitmap(width, height);
                ImageElementBackground.Source = await writeableBitmapRenderer.RenderAsync();
            }

            InitializeAnimatedAreaBasedOnImageDimensions(width, height);

            SetControlsEnabled(true);
            HideProgressIndicator();
        }

        private void InitializeAnimatedAreaBasedOnImageDimensions(int imageWidth, int imageHeight)
        {
            if (_animatedArea == null)
            {
                _animatedArea = new RectangleGeometry();
                int offset = 5;
                AnimatedAreaIndicator.Width = Application.Current.Host.Content.ActualWidth - 1 - (offset * 2);
                AnimatedAreaIndicator.Height = imageHeight - (offset * 2);
                Canvas.SetLeft(AnimatedAreaIndicator, offset);
                Canvas.SetTop(AnimatedAreaIndicator, offset);
                _animatedArea.Rect = new Rect(0, 0, imageWidth, imageHeight);
            }        
        }

        private List<IImageProvider> CreateImageSequenceFromResources(int sequenceId)
        {
            List<IImageProvider> imageProviders = new List<IImageProvider>();

            try
            {
                int i = 0;
                while (true)
                {
                    Uri uri = new Uri(@"Assets/Sequences/sequence." + sequenceId + "." + i + ".jpg", UriKind.Relative);
                    Stream stream = Application.GetResourceStream(uri).Stream;
                    StreamImageSource sis = new StreamImageSource(stream);
                    imageProviders.Add(new StreamImageSource(stream));
                    i++;
                }
            }
            catch (NullReferenceException ex)
            {
                // No more images available
            }

            return imageProviders;
        }

        private void AnimationTimer_Tick(object sender, EventArgs eventArgs)
        {
            RenderForeground(_onScreenImageProviders[_animationIndex]);   
             
            if (_animationIndex == (_onScreenImageProviders.Count() - 1))
            {
                _animationIndex = 0;
            }
            else
            {
                _animationIndex++;
            }
        }

        private async void RenderForeground(IImageProvider imageProvider)
        {
            if (!_rendering && _semaphore.WaitOne(100))
            {
                _rendering = true;

                using (WriteableBitmapRenderer writeableBitmapRenderer = new WriteableBitmapRenderer(imageProvider, _onScreenImage))
                {
                    ImageElement.Source = await writeableBitmapRenderer.RenderAsync();
                }

                _rendering = false;
                _semaphore.Release();
            }
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (_animationTimer.IsEnabled)
            {
                Stop();
            }
            else
            {
                Play();
            }
        }

        private void Stop()
        {
            _animationTimer.Stop();
            _playButton.IconUri = new Uri(@"Assets/appbar.play.png", UriKind.Relative);
        }

        private void Play()
        {
            _animationTimer.Start();
            _playButton.IconUri = new Uri(@"Assets/appbar.pause.png", UriKind.Relative);
        }

        private void AlignButton_Click(object sender, EventArgs e)
        {
            _alignEnabled = !_alignEnabled;

            if (_alignEnabled)
            {
                _onScreenImageProviders = _alignedImageProviders;
                _alignButton.IconUri = new Uri(@"Assets/appbar.align.enabled.png", UriKind.Relative);
            }
            else
            {
                _onScreenImageProviders = _unalignedImageProviders;
                _alignButton.IconUri = new Uri(@"Assets/appbar.align.disabled.png", UriKind.Relative);
            }

            RenderForeground(_onScreenImageProviders[_animationIndex]);
        }

        private void FrameButton_Click(object sender, EventArgs e)
        {
            _frameEnabled = !_frameEnabled;
            AnimatedAreaIndicator.Visibility = _frameEnabled ? Visibility.Visible : Visibility.Collapsed;

            if (!_frameEnabled)
            {
                ImageElement.Clip = null;
                _frameButton.IconUri = new Uri(@"Assets/appbar.frame.disabled.png", UriKind.Relative);
            }
            else
            {
                ImageElement.Clip = _animatedArea;
                _frameButton.IconUri = new Uri(@"Assets/appbar.frame.enabled.png", UriKind.Relative);
            }

            RenderForeground(_onScreenImageProviders[_animationIndex]);
        }

        private async void SaveButton_Click(object sender, EventArgs e)        
        {
            _rendering = true;
            Stop();

            PhoneApplicationPage context = this;
            SetControlsEnabled(false);            

            bool resumePlaybackAfterSave = _animationTimer.IsEnabled;

            ShowProgressIndicator("Saving");

            _semaphore.WaitOne();

            if (_frameEnabled)
            {
                await GifExporter.Export(_onScreenImageProviders, _animatedArea.Rect);
            }
            else
            {
                await GifExporter.Export(_onScreenImageProviders, null);
            }

            _semaphore.Release();

            HideProgressIndicator();

            _rendering = false;
            if (resumePlaybackAfterSave)
            {
                Play();
            }

            SetControlsEnabled(true);
        }

        private void ShowProgressIndicator(String text)
        {
            SystemTray.ProgressIndicator = new ProgressIndicator();
            SystemTray.ProgressIndicator.Text = text;
            SystemTray.ProgressIndicator.IsIndeterminate = true;
            SystemTray.ProgressIndicator.IsVisible = true;
        }

        private void HideProgressIndicator()
        {
            if (SystemTray.ProgressIndicator != null)
            {
                SystemTray.ProgressIndicator.IsVisible = false;
            }
        }

        private void ImageElement_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            if (_frameEnabled)
            {
                double x0 = Math.Min(e.ManipulationOrigin.X, _dragStart.X);
                double x1 = Math.Max(e.ManipulationOrigin.X, _dragStart.X);
                double y0 = Math.Min(e.ManipulationOrigin.Y, _dragStart.Y);                
                double y1 = Math.Max(e.ManipulationOrigin.Y, _dragStart.Y);

                x0 = Math.Max(x0, 0);
                x1 = Math.Min(x1, _onScreenImage.PixelWidth);
                y0 = Math.Max(y0, 0);
                y1 = Math.Min(y1, _onScreenImage.PixelHeight);

                double width = x1 - x0;
                double height = y1 - y0;

                Rect rect = new Rect(x0, y0, width, height);
                Canvas.SetLeft(AnimatedAreaIndicator, rect.X);
                Canvas.SetTop(AnimatedAreaIndicator, rect.Y);
                AnimatedAreaIndicator.Width = rect.Width;
                AnimatedAreaIndicator.Height = rect.Height;

                _animatedArea.Rect = rect;
            }
        }

        private void ImageElement_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            _dragStart = new Point(e.ManipulationOrigin.X, e.ManipulationOrigin.Y);
        }

    }
}