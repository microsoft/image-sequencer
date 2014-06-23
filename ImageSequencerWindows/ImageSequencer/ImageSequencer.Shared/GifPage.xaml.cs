/**
 * Copyright (c) 2013-2014 Microsoft Mobile.
 * See the license file delivered with this project for more information.
 */

using ImageSequencer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace ImageSequencer
{

    public sealed partial class GifPage : Page
    {
        private NavigationHelper _navigationHelper;
        private List<WriteableBitmap> _frames;
        private int _currentFrameIndex = 0;
        private String filename;
        private DispatcherTimer timer;

        public GifPage()
        {
            this.InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Tick += Timer_Tick;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _frames = new List<WriteableBitmap>();
            filename = e.Parameter as String;
            FileNameTextBlock.Text = filename;
            ProgressBarHelper.ShowProgressBar("Loading GIF");
            await LoadImage(filename);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            timer.Stop();
            ProgressBarHelper.HideProgressBar();
            base.OnNavigatedFrom(e);
        }

        private void StartAnimation()
        {
            DeleteButton.IsEnabled = true;
            timer.Start();
            ProgressBarHelper.HideProgressBar();
        }

        private void Timer_Tick(object sender, object e)
        {
            GifImage.Source = _frames[_currentFrameIndex];

            _currentFrameIndex++;
            if (_currentFrameIndex >= _frames.Count())
            {
                _currentFrameIndex = 0;
            }
        }

        private async Task LoadImage(String filename)
        {
            var storageFile = await KnownFolders.SavedPictures.GetFileAsync(filename);

            using (var res = await storageFile.OpenAsync(FileAccessMode.Read))
            {
                var bitmapDecoder = await BitmapDecoder.CreateAsync(BitmapDecoder.GifDecoderId, res);

                byte[] firstFrame = null;

                for (uint frameIndex = 0; frameIndex < bitmapDecoder.FrameCount; frameIndex++)
                {
                    var frame = await bitmapDecoder.GetFrameAsync(frameIndex);

                    var writeableBitmap = new WriteableBitmap((int)bitmapDecoder.OrientedPixelWidth, (int)bitmapDecoder.OrientedPixelHeight);

                    var pixelData = await frame.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Straight,
                        new BitmapTransform(),
                        ExifOrientationMode.RespectExifOrientation,
                        ColorManagementMode.DoNotColorManage);
                    var bytes = pixelData.DetachPixelData();

                    if (firstFrame == null)
                    {
                        firstFrame = bytes;
                    }
                    else
                    {
                        // Blend the frame on top of the first frame
                        for (int i = 0; i < bytes.Count(); i += 4)
                        {
                            int alpha = bytes[i + 3];

                            if (alpha == 0 && firstFrame != null)
                            {
                                Array.Copy(firstFrame, i, bytes, i, 4);
                            }
                        }
                    }

                    using (var stream = writeableBitmap.PixelBuffer.AsStream())
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }

                    _frames.Add(writeableBitmap);
                }
            }

            StartAnimation();
        }

        public async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var storageFile = await KnownFolders.SavedPictures.GetFileAsync(filename);
            await storageFile.DeleteAsync();
            _navigationHelper.GoBack();
        }

        public void About_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }
    }
}
