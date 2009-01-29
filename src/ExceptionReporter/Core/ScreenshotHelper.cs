using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ExceptionReporting.Core
{
	public static class ScreenshotHelper
    {
        public const string ScreenshotFileName = "ExceptionReport_Screenshot.gif";
		public static Bitmap ScreenShot()
		{
			Rectangle rectangle = Rectangle.Empty;

			foreach (Screen screen in Screen.AllScreens)
			{
				rectangle = Rectangle.Union(rectangle, screen.Bounds);
			}

			var bitmap = new Bitmap(rectangle.Width, rectangle.Height, PixelFormat.Format32bppArgb);

			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.CopyFromScreen(rectangle.X, rectangle.Y, 0, 0, rectangle.Size, CopyPixelOperation.SourceCopy);
			}

			return bitmap;
		}

		public static string GetBitmapAsFile(Bitmap bitmap)
		{
		    string tempFileName = Path.GetTempPath() + ScreenshotFileName;
			bitmap.Save(tempFileName,ImageFormat.Gif);
			return tempFileName;
		}
	}
}
