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

                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(imageFileName, System.IO.FileMode.Open, IsolatedStorageFile.GetUserStoreForApplication()))
                {
                    // Allocate an array large enough for the entire file 
                    byte[] data = new byte[stream.Length];
                    // Read the entire file and then close it 
                    stream.Read(data, 0, data.Length);
                    stream.Close(); 

                    ExtendedImage image = new ExtendedImage();
                    image.LoadingCompleted +=
                        (o, ea) => Dispatcher.BeginInvoke(() => { AnimatedImage.Source = image; });

                    image.SetSource(new MemoryStream(data));
                }
            }
        }

        public void About_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        public void Delete_Click(object sender, EventArgs e)
        {
            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
            store.DeleteFile(imageFileName);
            NavigationService.GoBack();
        }
    }
}