using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

public static class WriteableBitmapExtentions
{
    // http://csharphelper.com/blog/2018/05/easily-save-a-writeablebitmap-in-wpf-and-c/
    // Save the WriteableBitmap into a PNG file.
    public static void Save(this WriteableBitmap wbitmap,
        string filename)
    {
        using (FileStream stream =
            new FileStream(filename, FileMode.Create))
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(wbitmap));
            encoder.Save(stream);
        }
    }

    // https://stackoverflow.com/a/25726349
    public static void CopyPixelsTo(this BitmapSource sourceImage, Int32Rect sourceRoi, WriteableBitmap destinationImage, Int32Rect destinationRoi)
    {
        var croppedBitmap = new CroppedBitmap(sourceImage, sourceRoi);
        int stride = croppedBitmap.PixelWidth * (croppedBitmap.Format.BitsPerPixel / 8);
        var data = new byte[stride * croppedBitmap.PixelHeight];
        // Is it possible to Copy directly from the sourceImage into the destinationImage?
        croppedBitmap.CopyPixels(data, stride, 0);
        destinationImage.WritePixels(destinationRoi, data, stride, 0);
    }
}