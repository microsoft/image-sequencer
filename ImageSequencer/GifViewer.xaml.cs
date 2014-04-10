using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using ImageTools;
using System.IO.IsolatedStorage;
using System.IO;
using ImageTools.IO;
using ImageTools.IO.Gif;
using Windows.Storage;

namespace ImageSequencer
{
    public partial class GifViewer : PhoneApplicationPage
    {

        private String imageFileName;

        public GifViewer()
        {
            Decoders.AddDecoder<GifDecoder>();
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (NavigationContext.QueryString.TryGetValue("imageUri", out imageFileName))
            {
                FilenameText.Text = imageFileName;

                LoadImage(imageFileName);
            }
        }

        public async void LoadImage(String filename)
        {

            var file = await KnownFolders.PicturesLibrary.GetFileAsync(filename);

                ExtendedImage image = new ExtendedImage();
                image.LoadingCompleted +=
                    (o, ea) => Dispatcher.BeginInvoke(() => { AnimatedImage.Source = image; });

                image.SetSource((await file.OpenReadAsync()).AsStreamForRead());
 
        }

        public void About_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        public async void Delete_Click(object sender, EventArgs e)
        {
            var storageFile = await KnownFolders.PicturesLibrary.GetFileAsync(imageFileName);
            await storageFile.DeleteAsync();
            NavigationService.GoBack();
        }
    }
}