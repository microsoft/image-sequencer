using Nokia.Graphics.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows.Shapes;
using System.Windows.Media;
using Nokia.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Storage;

namespace ImageSequencer
{
    class GifExporter
    {

        public static async Task Export(IReadOnlyList<IImageProvider> images, Rect? animatedArea)
        {
            ImageProviderInfo info = await images[0].GetInfoAsync();
            int w = (int)info.ImageSize.Width;
            int h = (int)info.ImageSize.Height;

            IReadOnlyList<IImageProvider> gifRendererSources;
            if (animatedArea.HasValue)
            {
                gifRendererSources = CreateFramedAnimation(images, animatedArea.Value, w, h);
            }
            else
            {
                gifRendererSources = images;
            }

            using (GifRenderer gifRenderer = new GifRenderer())
            {
                gifRenderer.Size = new Windows.Foundation.Size(w, h);
                gifRenderer.Duration = 100;
                gifRenderer.NumberOfAnimationLoops = 10000;
                gifRenderer.Sources = gifRendererSources;

                var buffer = await gifRenderer.RenderAsync();

                using (IsolatedStorageFileStream file = IsolatedStorageFile.GetUserStoreForApplication().CreateFile("exported." + GetFileNameRunningNumber() + ".gif"))
                {
                    Stream bufferStream = buffer.AsStream();
                    bufferStream.CopyTo(file);
                    bufferStream.Close();
                    bufferStream.Dispose();
                    file.Flush();
                }

            }
        }

        private static IReadOnlyList<IImageProvider> CreateFramedAnimation(IReadOnlyList<IImageProvider> images, Rect animationBounds, int w, int h)
        {
            List<IImageProvider> framedAnimation = new List<IImageProvider>();

            WriteableBitmap maskBitmap = new WriteableBitmap(w, h);

            var backgroundRectangle = new Rectangle
            {
                Fill = new SolidColorBrush(Colors.Black),
                Width = w,
                Height = h,
            };

            maskBitmap.Render(backgroundRectangle, new TranslateTransform());

            var foregroundRectangle = new Rectangle
            {
                Fill = new SolidColorBrush(Colors.White),
                Width = animationBounds.Width,
                Height = animationBounds.Height,
            };

            TranslateTransform foregroundTranslate = new TranslateTransform();
            foregroundTranslate.X = animationBounds.X;
            foregroundTranslate.Y = animationBounds.Y;
            maskBitmap.Render(foregroundRectangle, foregroundTranslate);
            maskBitmap.Invalidate();

            foreach (IImageProvider frame in images)
            {
                FilterEffect filterEffect = new FilterEffect(images[0]);

                ImageFusionFilter imageFusionFilter = new ImageFusionFilter(frame, new BitmapImageSource(maskBitmap.AsBitmap()), false);

                filterEffect.Filters = new List<IFilter>() { imageFusionFilter };
                framedAnimation.Add(filterEffect);
            }

            return framedAnimation;
        }

        private static int GetFileNameRunningNumber()
        {
            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
            String[] filenames = store.GetFileNames();
            int max = 0;
            foreach (String filename in filenames)
            {
                max = Math.Max(max, Convert.ToInt32(filename.Split('.')[1]));
            }
            return max + 1;
        }

    }
}
