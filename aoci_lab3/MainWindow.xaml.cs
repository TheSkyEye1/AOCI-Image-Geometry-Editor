using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Emgu.CV.Structure;
using Emgu.CV;
using static Emgu.Util.Platform;

namespace aoci_lab3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Image<Bgr, byte> sourceImage;

        public MainWindow()
        {
            InitializeComponent();
        }

        public BitmapSource ToBitmapSource(Image<Bgr, byte> image)
        {
            var mat = image.Mat;

            return BitmapSource.Create(
                mat.Width,
                mat.Height,
                96d,
                96d,
                PixelFormats.Bgr24,
                null,
                mat.DataPointer,
                mat.Step * mat.Height,
                mat.Step);
        }
        public Image<Bgr, byte> ToEmguImage(BitmapSource source)
        {
            if (source == null) return null;

            FormatConvertedBitmap safeSource = new FormatConvertedBitmap();
            safeSource.BeginInit();
            safeSource.Source = source;
            safeSource.DestinationFormat = PixelFormats.Bgr24;
            safeSource.EndInit();

            Image<Bgr, byte> resultImage = new Image<Bgr, byte>(safeSource.PixelWidth, safeSource.PixelHeight);
            var mat = resultImage.Mat;

            safeSource.CopyPixels(
                new System.Windows.Int32Rect(0, 0, safeSource.PixelWidth, safeSource.PixelHeight),
                mat.DataPointer,
                mat.Step * mat.Height,
                mat.Step);

            return resultImage;
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Файлы изображений (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                sourceImage = new Image<Bgr, byte>(openFileDialog.FileName);

                MainImage.Source = ToBitmapSource(sourceImage);
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource currentWpfImage = MainImage.Source as BitmapSource;
            if (currentWpfImage == null)
            {
                MessageBox.Show("Отсутсвует изображение");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    Image<Bgr, byte> imageToSave = ToEmguImage(currentWpfImage);
                    imageToSave.Save(saveFileDialog.FileName);

                    MessageBox.Show($"Изображение успешно сохранено в {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {

                    MessageBox.Show($"Ошибка! Не могу сохранить файл. Подробности: {ex.Message}");
                }
            }
        }

        private void UpdateImage_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource currentWpfImage = MainImage.Source as BitmapSource;

            if (currentWpfImage == null)
            {
                MessageBox.Show("Изображение отсутсвует");
                return;
            }

            sourceImage = ToEmguImage(currentWpfImage);
            MessageBox.Show("Изменения применены. Теперь это новый оригинал.");
        }
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;
            MainImage.Source = ToBitmapSource(sourceImage);
        }

        private void OnGeometryFilterChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            double scaleX = ScaleXSlider.Value;
            double scaleY = ScaleYSlider.Value;

            int newWidth = (int)(sourceImage.Width * scaleX);
            int newHeight = (int)(sourceImage.Height * scaleY);

            Image<Bgr, byte> scaledImage = new Image<Bgr, byte>(newWidth, newHeight);

            for (int y_in = 0; y_in < sourceImage.Height; y_in++)
            {
                for (int x_in = 0; x_in < sourceImage.Width; x_in++)
                {
                    int x_out = (int)(x_in * scaleX);
                    int y_out = (int)(y_in * scaleY);

                    if (x_out >= 0 && x_out < newWidth && y_out >= 0 && y_out < newHeight)
                    {
                        scaledImage[y_out, x_out] = sourceImage[y_in, x_in];
                    }
                }
            }

            MainImage.Source = ToBitmapSource(scaledImage);
        }
        private void OnInversedGeometryFilterChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            double scaleX = ScaleInverseXSlider.Value;
            double scaleY = ScaleInverseYSlider.Value;

            double angleDegrees = RotationSlider.Value;

            double angleRadians = angleDegrees * Math.PI / 180.0;
            double cos = Math.Cos(angleRadians);
            double sin = Math.Sin(angleRadians);
            float centerX = sourceImage.Width / 2.0f;
            float centerY = sourceImage.Height / 2.0f;

            double shear = ShearSlider.Value;

            int newWidth = (int)(sourceImage.Width * scaleX);
            int newHeight = (int)(sourceImage.Height * scaleY);

            Image<Bgr, byte> scaledImage = new Image<Bgr, byte>(newWidth, newHeight);

            for (int y_out = 0; y_out < newHeight; y_out++)
            {
                for (int x_out = 0; x_out < newWidth; x_out++)
                {
                    double x_centered = x_out - newWidth / 2.0;
                    double y_centered = y_out - newHeight / 2.0;

                    double x_rotated = x_centered * cos + y_centered * sin;
                    double y_rotated = -x_centered * sin + y_centered * cos;

                    double x_sheared = x_rotated - y_rotated * shear;
                    double y_sheared = y_rotated;

                    double x_scaled = x_sheared / scaleX;
                    double y_scaled = y_sheared / scaleY;

                    double x_in = x_scaled + centerX;
                    double y_in = y_scaled + centerY;

                    if (x_in >= 0 && y_in >= 0 && x_in < sourceImage.Width && y_in < sourceImage.Height)
                    {
                        scaledImage[y_out, x_out] = sourceImage[(int)Math.Truncate(y_in), (int)Math.Truncate(x_in)];
                    }
                }
            }

            MainImage.Source = ToBitmapSource(scaledImage);

        }
    }
}
