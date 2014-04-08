using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using System.IO;

namespace ImageSequencer
{
    public partial class ImagePicker : PhoneApplicationPage
    {

        public ObservableCollection<Uri> Sequences { get; private set; }
        public ObservableCollection<GifThumbnail> Gifs { get; private set; }

        public ImagePicker()
        {

            InitializeComponent();
            Gifs = new ObservableCollection<GifThumbnail>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            PopulateSources();            
            PopulateGifs();

            DataContext = this;        
        }

        private void PopulateSources()
        {
            Sequences = new ObservableCollection<Uri>()
            {
                new Uri(@"Assets/Sequences/sequence.1.0.jpg", UriKind.Relative),
                new Uri(@"Assets/Sequences/sequence.2.0.jpg", UriKind.Relative)
            };
        }

        private void PopulateGifs()
        {
            Gifs.Clear();

            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
            String[] fileNames = store.GetFileNames();

            foreach (String filename in fileNames)
            {
                using (var sourceFile = store.OpenFile(filename, FileMode.Open, FileAccess.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.SetSource(sourceFile);
                    GifThumbnail gifThumbnail = new GifThumbnail();
                    gifThumbnail.BitmapImage = bitmapImage;
                    gifThumbnail.FileName = filename;
                    Gifs.Add(gifThumbnail);
                }
            }

            if (Gifs.Count() == 0)
            {
                ThereAreNoSavedGifsTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                ThereAreNoSavedGifsTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        public void Thumbnail_Tap(object sender, EventArgs e)
        {
            Uri uri = (sender as Image).DataContext as Uri;
            int sequenceId = Sequences.IndexOf(uri) + 1;
            NavigationService.Navigate(new Uri("/MainPage.xaml?sequenceId=" + sequenceId, UriKind.Relative));
        }

        public void Gif_Tap(object sender, EventArgs e)
        {
            GifThumbnail thumbnail = (sender as StackPanel).DataContext as GifThumbnail;
            NavigationService.Navigate(new Uri("/GifViewer.xaml?imageUri=" + thumbnail.FileName, UriKind.Relative));
        }

        public void About_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        public void Delete_Click(object sender, EventArgs e)
        {
            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
            store.DeleteFile(((sender as MenuItem).DataContext as GifThumbnail).FileName);
            PopulateGifs();
        }

        public class GifThumbnail
        {
            public BitmapImage BitmapImage { get; set; }
            public String FileName { get; set; }
        }
    }
}