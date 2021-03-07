using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DynmapUniter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static WriteableBitmap resBitmap;
        ObservableCollection<string> log = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textBox_xMin.Text = "-2000";
            textBox_zMin.Text = "-6000";
            textBox_xMax.Text = "6000";
            textBox_zMax.Text = "2000";
            textBox_dynmap.Text = "D:/Games/MC Servers/Test_render/plugins/dynmap/web/tiles/world/flat/";
        }

        private async void convertButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            convertButton.IsEnabled = false;
            int scale = int.Parse(comboBox_scale.Text);
            try
            {
                int x1 = int.Parse(textBox_xMin.Text);
                int x2 = int.Parse(textBox_xMax.Text);
                int z1 = int.Parse(textBox_zMin.Text);
                int z2 = int.Parse(textBox_zMax.Text);

                Int32Rect coords = new Int32Rect(x1, z1, x2 - x1, z2 - z1);

                string dynmapPath = textBox_dynmap.Text;
                string resPath = textBox_res.Text;
                var progress = new Progress<double>(value => progressBar.Value = value);
                await Task.Run(() => Merge(dynmapPath, resPath, scale, coords, progress));
            }
            catch (Exception) { }

            convertButton.IsEnabled = true;
            logBox.ItemsSource = null;
            logBox.ItemsSource = log;
        }

        private void Merge(string dynmapPath, string resPath, int scale, Int32Rect includingCoords, IProgress<double> progress)
        {
            Int32Rect coords = new Int32Rect(
                includingCoords.X, -(includingCoords.Y + includingCoords.Height),
                includingCoords.Width, includingCoords.Height
                );
            int chunkStep = (int)Math.Pow(2, scale);
            int mapChunk = 32 * chunkStep;
            int mapRegion = 1024;
            int lowX = GetLow(coords.X, mapChunk);
            int lowZ = GetLow(coords.Y, mapChunk);
            int highX = GetLow(coords.X + coords.Width, mapChunk) + 1;
            int highZ = GetLow(coords.Y + coords.Height, mapChunk) + 1;
            int regLowX = GetLow(coords.X, mapRegion);
            int regLowZ = GetLow(coords.Y, mapRegion);
            int regHighX = GetLow(coords.X + coords.Width, mapRegion) + 1;
            int regHighZ = GetLow(coords.Y + coords.Height, mapRegion) + 1;

            int width = (highX - lowX) * 128;
            int height = (highZ - lowZ) * 128;

            resBitmap = new WriteableBitmap(
                width, height,
                96, 96,
                PixelFormats.Bgr32, null);
            log.Add(width + " " + height);

            string chunkPrefix = "";
            if (scale > 0)
                chunkPrefix = new string('z', scale) + "_";

            double regCount = (regHighX - regLowX) * (regHighZ - regLowZ);
            int processed = 0;
            progress.Report(0);

            for (int regX = regLowX; regX < regHighX; regX++)
            {
                for (int regZ = regLowZ; regZ < regHighZ; regZ++)
                {
                    string regPath = dynmapPath + "/" + regX + "_" + regZ + "/";
                    if (!Directory.Exists(regPath))
                        continue;
                    regPath += chunkPrefix;

                    int xBegin = Math.Max(lowX, regX * mapRegion / mapChunk);
                    int zBegin = Math.Max(lowZ, regZ * mapRegion / mapChunk);
                    int xEnd = Math.Min(highX, (regX + 1) * mapRegion / mapChunk);
                    int zEnd = Math.Min(highZ, (regZ + 1) * mapRegion / mapChunk);
                    for (int x = xBegin; x < xEnd; x++)
                    {
                        for (int z = zBegin; z < zEnd; z++)
                        {
                            int mapX = x * chunkStep;
                            int mapZ = z * chunkStep;
                            string chunkPath = regPath + mapX + "_" + mapZ + ".jpg";
                            if (!File.Exists(chunkPath))
                                continue;
                            try
                            {
                                int picX = (x - lowX) * 128;
                                int picZ = height - 128 - (z - lowZ) * 128;
                                BitmapImage bitmap = new BitmapImage(new Uri(chunkPath));
                                bitmap.CopyPixelsTo(new Int32Rect(0, 0, 128, 128),
                                    resBitmap,
                                    new Int32Rect(picX, picZ, 128, 128));
                                //log.Add(chunkPath);
                            }
                            catch (Exception) { }
                        }
                    }
                    processed++;
                    progress.Report(processed / regCount);
                }
            }

            try
            {
                resBitmap.Save(resPath);
            }
            catch (Exception ex)
            {
                log.Add(ex.Message);
            }
        }

        private int GetLow(int a, int step)
        {
            return ((int)Math.Floor(a / (double)step));
        }
        private int GetHigh(int a, int step)
        {
            return ((int)Math.Ceiling(a / (double)step));
        }
    }
}
