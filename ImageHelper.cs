using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ImageProcessor
{
    public static class ImageHelper
    {
        public static byte[] ToByteArray(Bitmap image, ImageFormat format)
        {
            using (var stream = new MemoryStream())
            {
                image.Save(stream, format);
                return stream.ToArray();
            }
        }

        public static byte[] CropImage(byte[] image, Size size, ImageFormat format = null)
        {
            using (var stream = new MemoryStream(image))
            {
                var source = new Bitmap(stream);

                var targetProportion = size.Width/(decimal) size.Height;
                var originalProportion = source.Width/(decimal) source.Height;

                decimal targetWidth = 0;
                decimal targetHeight = 0;

                if (originalProportion < targetProportion)
                {
                    targetWidth = source.Width;
                    targetHeight = source.Width/targetProportion;
                }
                else
                {
                    targetWidth = source.Height*targetProportion;
                    targetHeight = source.Height;
                }

                var centerX = (decimal) source.Width/2;
                var centerY = (decimal) source.Height/2;

                var locationX = centerX - targetWidth/2;
                var locationY = centerY - targetHeight/2;

                var location = new Point((int) locationX, (int) locationY);
                var targetSize = new Size((int) Math.Ceiling(targetWidth), (int) Math.Ceiling(targetHeight));
                var section = new Rectangle(location, targetSize);
                var targetSection = new Rectangle(new Point(0, 0), targetSize);

                var target = new Bitmap(section.Width, section.Height, source.PixelFormat);

                using (var newGraphics = Graphics.FromImage(target))
                {
                    newGraphics.Clear(Color.Transparent);
                    DrawingSettings(newGraphics);
                    newGraphics.DrawImage(source, targetSection, section, GraphicsUnit.Pixel);
                    newGraphics.Save();
                }

                return ToByteArray(target, format ?? ImageFormat.Bmp);
            }
        }

        public static byte[] ResizeImage(byte[] image, Size size, ImageFormat format = null)
        {
            using (var stream = new MemoryStream(image))
            {
                var source = new Bitmap(stream);

                var thumbSize = GetScaledSize(source.Width, source.Height, size.Width, size.Height);

                var imgOutput = new Bitmap(thumbSize.Width, thumbSize.Height, source.PixelFormat);

                using (var newGraphics = Graphics.FromImage(imgOutput))
                {
                    newGraphics.Clear(Color.FromArgb(0, 255, 255, 255));
                    DrawingSettings(newGraphics);
                    newGraphics.DrawImage(source, 0, 0, thumbSize.Width, thumbSize.Height);
                    newGraphics.Save();
                }

                return ToByteArray(imgOutput, format ?? ImageFormat.Bmp);
            }
        }

        public static byte[] ReduceImageQuality(byte[] image, long quality, ImageFormat format = null)
        {
            using (var stream = new MemoryStream(image))
            {
                var source = new Bitmap(stream);

                var codecs = ImageCodecInfo.GetImageDecoders();
                format = format ?? ImageFormat.Jpeg;
                var jgpEncoder = codecs.First(c => c.FormatID == format.Guid);
                var encoder = Encoder.Quality;
                var myEncoderParameters = new EncoderParameters(1);
                myEncoderParameters.Param[0] = new EncoderParameter(encoder, quality);
                using (var ms = new MemoryStream())
                {
                    source.Save(ms, jgpEncoder, myEncoderParameters);
                    return ms.ToArray();
                }   
            }
        }

        public static byte[] CorrectImage(byte[] image, Size size, ImageFormat format = null)
        {
            using (var stream = new MemoryStream(image))
            {
                var source = new Bitmap(stream);

                var location = new Point(source.Width - size.Width, source.Height - size.Height);
                var section = new Rectangle(location, size);
                var targetSection = new Rectangle(new Point(0, 0), size);

                var target = new Bitmap(section.Width, section.Height, source.PixelFormat);

                using (var newGraphics = Graphics.FromImage(target))
                {
                    newGraphics.Clear(Color.Transparent);
                    DrawingSettings(newGraphics);
                    newGraphics.DrawImage(source, targetSection, section, GraphicsUnit.Pixel);
                    newGraphics.Save();
                }

                return ToByteArray(target, format ?? ImageFormat.Bmp);
            }
        }

        public static byte[] ProcessImage(byte[] image, Size size, ImageFormat format = null)
        {
            var cropped = CropImage(image, size, format);
            if (cropped == null) return null;

            var resized = ResizeImage(cropped, size, format);
            var corrected = CorrectImage(resized, size, format);

            return ReduceImageQuality(corrected, 100, format);
        }

        private static Size GetScaledSize(int cx, int cy, int dx, int dy)
        {
            var dimensionScale = (double)dx / dy;
            var currentScale = (double)cx / cy;

            var x = (currentScale > dimensionScale) ? dx : (int)Math.Ceiling(dy * currentScale);
            var y = (currentScale > dimensionScale) ? (int)Math.Ceiling(dx / currentScale) : dy;
            return new Size(x, y);
        }

        private static void DrawingSettings(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingMode = CompositingMode.SourceCopy;
        }
    }
}
