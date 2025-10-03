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
        private Bgr BilinearInterpolate(Image<Bgr, byte> image, float x, float y)
        {
            int x1 = (int)x;
            int y1 = (int)y;
            int x2 = x1 + 1;
            int y2 = y1 + 1;

            Bgr p11 = image[y1, x1];
            Bgr p12 = image[y2, x1];
            Bgr p21 = image[y1, x2];
            Bgr p22 = image[y2, x2];

            
            float fx = x - x1;
            float fy = y - y1;

            double r_top = p11.Red * (1 - fx) + p21.Red * fx;
            double g_top = p11.Green * (1 - fx) + p21.Green * fx;
            double b_top = p11.Blue * (1 - fx) + p21.Blue * fx;

            double r_bottom = p12.Red * (1 - fx) + p22.Red * fx;
            double g_bottom = p12.Green * (1 - fx) + p22.Green * fx;
            double b_bottom = p12.Blue * (1 - fx) + p22.Blue * fx;

            double r = r_top * (1 - fy) + r_bottom * fy;
            double g = g_top * (1 - fy) + g_bottom * fy;
            double b = b_top * (1 - fy) + b_bottom * fy;

            return new Bgr(b, g, r);
        }

        private void OnInversedGeometryFilterChanged(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            double scaleX = ScaleInverseXSlider.Value;
            double scaleY = ScaleInverseYSlider.Value;

            double angleDegrees = RotationSlider.Value;

            double angleRadians = angleDegrees * Math.PI / 180.0;
            double cos = Math.Cos(angleRadians);
            double sin = Math.Sin(angleRadians);

            float centerX, centerY = 0;

            if(float.TryParse(xCord.Text,out centerX) && float.TryParse(yCord.Text,out centerY))
            {
                if(centerX < 0 || centerY < 0)
                {
                    centerX = sourceImage.Width / 2.0f;
                    centerY = sourceImage.Height / 2.0f;
                }
            }

            double shear = ShearSlider.Value;

            bool useInterpolation = InterpolationCheckbox.IsChecked.Value;

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

                    if (x_in >= 0 && y_in >= 0 && x_in < sourceImage.Width - 1 && y_in < sourceImage.Height - 1)
                    {
                        Bgr color;

                        if (useInterpolation)
                        {
                            color = BilinearInterpolate(sourceImage, (float)x_in, (float)y_in);
                        }
                        else
                        {
                            color = sourceImage[(int)Math.Truncate(y_in), (int)Math.Truncate(x_in)];
                        }

                        scaledImage[y_out, x_out] = color;
                    }
                }
            }

            MainImage.Source = ToBitmapSource(scaledImage);
        }

        private void OnWaveFilterChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> wavyImage = sourceImage.Clone();

            double waveAmplitude = WaveAmplitude.Value;
            double waveFrequency = WaveFrequency.Value;

            for (int y_out = 0; y_out < wavyImage.Height; y_out++)
            {
                for (int x_out = 0; x_out < wavyImage.Width; x_out++)
                {
                    double x_in = x_out / scaleX;
                    double y_in
                }
            }
        }
    }
}
