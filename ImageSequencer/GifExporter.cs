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
                gifRendererSources = await CreateFramedAnimation(images, animatedArea.Value, w, h);
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

        private static async Task<IReadOnlyList<IImageProvider>> CreateFramedAnimation(IReadOnlyList<IImageProvider> images, Rect animationBounds, int w, int h)
        {
            List<IImageProvider> framedAnimation = new List<IImageProvider>();

            WriteableBitmap frameBitmap = new WriteableBitmap(w, h);            

            using (WriteableBitmapRenderer wbr = new WriteableBitmapRenderer())
            {
                foreach (IImageProvider frame in images)
                {
                    // Render background
                    WriteableBitmap backgroundBitmap = new WriteableBitmap(w, h);
                    wbr.Source = images[0];
                    wbr.WriteableBitmap = backgroundBitmap;
                    await wbr.RenderAsync();

                    if (frame != images[0])
                    {
                        // Render foreground
                        wbr.Source = frame;
                        wbr.WriteableBitmap = frameBitmap;
                        await wbr.RenderAsync();

                        for (int y = (int)animationBounds.Y; y < ((int)animationBounds.Y + (int)animationBounds.Height); y++)
                        {
                            Array.Copy(frameBitmap.Pixels,
                                (int)animationBounds.X + (y * w),
                                backgroundBitmap.Pixels,
                                (int)animationBounds.X + (y * w),
                                (int)animationBounds.Width);
                        }
                    }

                    framedAnimation.Add(new BitmapImageSource(backgroundBitmap.AsBitmap()));
                }
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
