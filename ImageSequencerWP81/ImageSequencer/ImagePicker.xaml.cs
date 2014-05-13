using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.IO;
using Windows.Storage;

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

        private async void PopulateGifs()
        {
            Gifs.Clear();

            var files = await KnownFolders.PicturesLibrary.GetFilesAsync();
            foreach (StorageFile storageFile in files)
            {
                const string pattern = "ImageSequencer\\.\\d+\\.gif";
                if (System.Text.RegularExpressions.Regex.IsMatch(storageFile.Name, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    var stream = (await storageFile.OpenReadAsync()).AsStreamForRead();
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.SetSource(stream);
                    GifThumbnail gifThumbnail = new GifThumbnail
                    {
                        BitmapImage = bitmapImage,
                        FileName = storageFile.Name
                    };
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
            var image = sender as Image;
            if (image != null)
            {
                Uri uri = image.DataContext as Uri;
                int sequenceId = Sequences.IndexOf(uri) + 1;
                NavigationService.Navigate(new Uri("/MainPage.xaml?sequenceId=" + sequenceId, UriKind.Relative));
            }
        }

        public void Gif_Tap(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var thumbnail = button.DataContext as GifThumbnail;
                if (thumbnail != null)
                    NavigationService.Navigate(new Uri("/GifViewer.xaml?imageUri=" + thumbnail.FileName, UriKind.Relative));
            }
        }

        public void About_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        public async void Delete_Click(object sender, EventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var filename = (menuItem.DataContext as GifThumbnail).FileName;
                var storageFile = await KnownFolders.PicturesLibrary.GetFileAsync(filename);
                await storageFile.DeleteAsync();
            }
            PopulateGifs();
        }

        public class GifThumbnail
        {
            public BitmapImage BitmapImage { get; set; }
            public String FileName { get; set; }
        }
    }
}