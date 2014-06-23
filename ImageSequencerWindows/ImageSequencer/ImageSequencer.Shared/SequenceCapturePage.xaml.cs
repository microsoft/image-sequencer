/**
 * Copyright (c) 2013-2014 Microsoft Mobile.
 * See the license file delivered with this project for more information.
 */

using ImageSequencer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ImageSequencer
{

    public sealed partial class SequenceCapturePage : Page
    {

        private List<IRandomAccessStream> _capturedSequence;
        private MediaCapture _mediaCapture;
        private LowLagPhotoSequenceCapture _lowLagPhotoSequenceCapture;
        private Task _saveTask;
        private Boolean _recording = false;
        private List<StorageFile> _files = new List<StorageFile>();
        private int _fileIndex = 1;
        private NavigationHelper _navigationHelper;
        private const int AMOUNT_OF_FRAMES_IN_SEQUENCE = 20;

        public SequenceCapturePage()
        {
            this.InitializeComponent();

            _navigationHelper = new NavigationHelper(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

            InitializeMediaCapture();

#if WINDOWS_PHONE_APP
            Windows.Phone.UI.Input.HardwareButtons.CameraPressed += HardwareButtons_CameraPressed;
#endif
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;

            base.OnNavigatedFrom(e);
            _mediaCapture.Dispose();
#if WINDOWS_PHONE_APP
            Windows.Phone.UI.Input.HardwareButtons.CameraPressed -= HardwareButtons_CameraPressed;
#endif
        }

        private async void InitializeMediaCapture()
        {
            _capturedSequence = new List<IRandomAccessStream>();

            _mediaCapture = new MediaCapture();

            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var backCamera = devices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);

            await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Video,
                PhotoCaptureSource = PhotoCaptureSource.Auto,
                AudioDeviceId = string.Empty,
                VideoDeviceId = backCamera.Id
            });

            captureElement.Source = _mediaCapture;
            await _mediaCapture.StartPreviewAsync();

            var format = ImageEncodingProperties.CreateJpeg();
            format.Width = 640;
            format.Height = 480;

            _lowLagPhotoSequenceCapture = await _mediaCapture.PrepareLowLagPhotoSequenceCaptureAsync(format);            
            _lowLagPhotoSequenceCapture.PhotoCaptured += OnPhotoCaptured;
        }

        public void OnPhotoCaptured(LowLagPhotoSequenceCapture s, PhotoCapturedEventArgs e)
        {
            if (_fileIndex < AMOUNT_OF_FRAMES_IN_SEQUENCE)
            {
                if (_saveTask == null)
                {
                    _saveTask = Save(e.Frame, _fileIndex++);
                }
                else
                {
                    _saveTask = _saveTask.ContinueWith(t => Save(e.Frame, _fileIndex++));
                }
            }
            else
            {
                StopSequenceCapture();
            }
        }

        private async void ShowPreviewPage()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    Frame.Navigate(typeof(SequencePreviewPage), _files);
                }
            );
        }

        private async Task Save(IRandomAccessStream frame, int i)
        {
            var filename = "ImageSequencer." + i + ".jpg";
            var folder = Windows.Storage.ApplicationData.Current.TemporaryFolder;
            var storageFile = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            var stream = await storageFile.OpenAsync(FileAccessMode.ReadWrite);
            await RandomAccessStream.CopyAndCloseAsync(frame, stream);
            _files.Add(storageFile);
        }

        public void CaptureElement_Tapped(object sender, RoutedEventArgs e)
        {
            StartStopCapture();
        }

        public void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            StartStopCapture();
        }

#if WINDOWS_PHONE_APP
        void HardwareButtons_CameraPressed(object sender, Windows.Phone.UI.Input.CameraEventArgs e)
        {
            StartStopCapture();
        }
#endif

        private async void StartStopCapture()
        {
            if (!_recording)
            {
                _recording = true;
                ProgressBarHelper.ShowProgressBar("Capturing");
                await _lowLagPhotoSequenceCapture.StartAsync();
                CaptureButton.Icon = new SymbolIcon(Symbol.Stop);
            }
            else
            {
                StopSequenceCapture();
            }
        }

        public async void StopSequenceCapture()
        {
            _recording = false;
            await _lowLagPhotoSequenceCapture.FinishAsync();
            ProgressBarHelper.HideProgressBar();
            ShowPreviewPage();
        }

        public void About_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }

    }
}
