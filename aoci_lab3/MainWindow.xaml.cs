using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Emgu.CV.Structure;
using Emgu.CV;
using static Emgu.Util.Platform;
using System.Drawing;
using System.Security.Cryptography.Xml;

namespace aoci_lab3
{
    public partial class MainWindow : Window
    {
        // --- Код повторяется из лабораторной работы #1 ---

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

        // --- Фильтры и эффекты ---

        // Алгоритм выполняет масштабирование изображения, используя прямое преобразование.
        private void OnGeometryFilterChanged(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            int translateX = (int)BasicTranslateSliderX.Value;
            int translateY = (int)BasicTranslateSliderY.Value;
            double scaleX = BasicScaleSliderX.Value;
            double scaleY = BasicScaleSliderY.Value;
            double angleDegrees = BasicRotationSlider.Value * Math.PI / 180.0;
            double skewXRad = BasicShearSliderX.Value * Math.PI / 180.0;
            double skewYRad = BasicShearSliderY.Value * Math.PI / 180.0;

            double centerX = sourceImage.Width / 2;
            double centerY = sourceImage.Height / 2;

            int newWidth = (int)(sourceImage.Width * scaleX);
            int newHeight = (int)(sourceImage.Height * scaleY);

            Image<Bgr, byte> scaledImage = new Image<Bgr, byte>(newWidth, newHeight);

            int brushWidth = (int)Math.Ceiling(scaleX) + 1;
            if (brushWidth < 1) brushWidth = 1;
            // Иногда добавляют +1 для перестраховки от дыр при вращении:
            // int brushWidth = (int)Math.Ceiling(scaleX) + 1; 

            int brushHeight = (int)Math.Ceiling(scaleY);
            if (brushHeight < 1) brushHeight = 1;

            // Оптимизация тригонометрии
            double cosTheta = Math.Cos(angleDegrees);
            double sinTheta = Math.Sin(angleDegrees);

            for (int y_in = 0; y_in < sourceImage.Height; y_in++)
            {
                for (int x_in = 0; x_in < sourceImage.Width; x_in++)
                {
                    double x1 = x_in + translateX;
                    double y1 = y_in + translateY;

                    double x2 = x1 * scaleX;
                    double y2 = y1 * scaleY;

                    double x3 = x2 + y2 * skewXRad;
                    double y3 = y2 + x2 * skewYRad;

                    double x4 = x3 - centerX;
                    double y4 = y3 - centerY;

                    double x5 = (int)(x4 * Math.Cos(angleDegrees) - y4 * Math.Sin(angleDegrees));
                    double y5 = (int)(x4 * Math.Sin(angleDegrees) + y4 * Math.Cos(angleDegrees));

                    int x_out = (int)(x5 + centerX);
                    int y_out = (int)(y5 + centerY);

                    // Получаем цвет исходного пикселя
                    Bgr color = sourceImage[y_in, x_in];

                    // === ЗАПОЛНЕНИЕ ДЫР (Splatting) ===
                    // Рисуем прямоугольник вместо одной точки
                    for (int j = 0; j < brushHeight; j++)
                    {
                        for (int i = 0; i < brushWidth; i++)
                        {
                            int currentX = x_out + i;
                            int currentY = y_out + j;

                            // Проверка границ для каждого под-пикселя
                            if (currentX >= 0 && currentX < newWidth &&
                                currentY >= 0 && currentY < newHeight)
                            {
                                scaledImage[currentY, currentX] = color;
                            }
                        }
                    }
                }
            }

            MainImage.Source = ToBitmapSource(scaledImage);


            //for (int y_in = 0; y_in < sourceImage.Height; y_in++)
            //{
            //    for (int x_in = 0; x_in < sourceImage.Width; x_in++)
            //    {
            //        double x1 = x_in + translateX;
            //        double y1 = y_in + translateY;

            //        double x2 = x1 * scaleX;
            //        double y2 = y1 * scaleY;

            //        double x3 = x2 +  y2 * skewXRad;
            //        double y3 = y2 + x2 * skewYRad;

            //        double x4 = x3 - centerX;
            //        double y4 = y3 - centerY;

            //        double x5 = (int)(x4 * Math.Cos(angleDegrees) - y4 * Math.Sin(angleDegrees));
            //        double y5 = (int)(x4 * Math.Sin(angleDegrees) + y4 * Math.Cos(angleDegrees));

            //        int x_out = (int)(x5 + centerX);
            //        int y_out = (int)(y5 + centerY);

            //        //if (x_out >= 0 && x_out < newWidth && y_out >= 0 && y_out < newHeight)
            //        //{
            //        //    scaledImage[y_out, x_out] = sourceImage[y_in, x_in];
            //        //}

            //        if (x_out >= 0 && x_out < newWidth && y_out >= 0 && y_out < newHeight)
            //        {
            //            Bgr color;

            //            if (InterpolationCheckbox.IsChecked == true)
            //            {
            //                //Используем билинейную интерполяцию для гладкости.
            //                color = BilinearInterpolate(sourceImage, x_in, y_in);
            //            }
            //            else
            //            {
            //                // Простой вариант - берем цвет ближайшего пикселя.
            //                color = sourceImage[y_in,x_in];
            //            }

            //            scaledImage[y_out, x_out] = color;
            //        }
            //    }
            //}

            //MainImage.Source = ToBitmapSource(scaledImage);
        }

        //Функция вычисляет цвет для пикселя с дробными координатами с помощью билинейной интерполяции
        //Алгоритм:
        //1. Найти 4 пикселя, окружающие точку (x, y).
        //2. Выполнить линейную интерполяцию по горизонтали для верхней и нижней пары пикселей.
        //3. Выполнить линейную интерполяцию по вертикали между двумя результатами из предыдущего шага.
        //Результат - цвет нашего нового пикселя
        private Bgr BilinearInterpolate(Image<Bgr, byte> image, float x, float y)
        {
            if (x > 0 && y > 0 && x < image.Width-1 && y < image.Height-1)
            {
                //Находим координаты 4-х опорных пикселей (квадрат, в который попала точка).
                int x1 = (int)x;
                int y1 = (int)y;
                int x2 = x1 + 1;
                int y2 = y1 + 1;

                //Получаем цвета этих пикселей.
                Bgr p11 = image[y1, x1];  //Верхний левый
                Bgr p12 = image[y2, x1];  //Нижний левый
                Bgr p21 = image[y1, x2];  //Верхний правый
                Bgr p22 = image[y2, x2];  //Нижний правый

                //Вычисляем дробные части — "расстояние" от левой (fx) и верхней (fy) границы.
                float fx = x - x1;
                float fy = y - y1;

                //Линейно интерполируем по горизонтали.
                //Смешиваем цвета верхней пары пикселей (p11 и p21).
                double r_top = p11.Red * (1 - fx) + p21.Red * fx;
                double g_top = p11.Green * (1 - fx) + p21.Green * fx;
                double b_top = p11.Blue * (1 - fx) + p21.Blue * fx;

                //Смешиваем цвета нижней пары пикселей (p12 и p22).
                double r_bottom = p12.Red * (1 - fx) + p22.Red * fx;
                double g_bottom = p12.Green * (1 - fx) + p22.Green * fx;
                double b_bottom = p12.Blue * (1 - fx) + p22.Blue * fx;

                //Линейно интерполируем по вертикали между двумя полученными "средними" цветами.
                double r = r_top * (1 - fy) + r_bottom * fy;
                double g = g_top * (1 - fy) + g_bottom * fy;
                double b = b_top * (1 - fy) + b_bottom * fy;

                return new Bgr(b, g, r);
            }
            return new Bgr(0, 0, 0);
        }

        //Выполняет масштабирование, вращение и сдвиг, используя обратное преобразование.
        //Это правильный подход для геометрических трансформаций.

        //Алгоритм проходит по каждому пикселю ВЫХОДНОГО изображения и для каждого из них вычисляет, из какой точки ИСХОДНОГО изображения нужно взять цвет.
        //Это решает все проблемы прямого подхода: дыр не возникает, т.к. каждый пиксель гарантированно будет заполнен.
        private void OnInversedGeometryFilterChanged(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null) return;

            //Получаем параметры трансформации из UI
            double scaleX = ScaleInverseXSlider.Value;
            double scaleY = ScaleInverseYSlider.Value;
            double angleDegrees = RotationSlider.Value;
            double shear = ShearSlider.Value; // Сдвиг (скос)
            bool useInterpolation = InterpolationCheckbox.IsChecked.Value;

            //Подготовительные вычисления
            double angleRadians = angleDegrees * Math.PI / 180.0; //Для расчета поворота нужны радианы
            double cos = Math.Cos(angleRadians);
            double sin = Math.Sin(angleRadians);

            //Центр вращения (по умолчанию центр изображения).
            float centerX = -1;
            float centerY = -1;
            float.TryParse(xCord.Text, out centerX); //Пытаемся прочитать из TextBox координаты центра поворота
            float.TryParse(yCord.Text, out centerY);
            if(centerX == -1) centerX = sourceImage.Width / 2.0f;
            if(centerY == -1) centerY = sourceImage.Height / 2.0f;

            //Новые размеры изображения.
            int newWidth = (int)(sourceImage.Width * scaleX);
            int newHeight = (int)(sourceImage.Height * scaleY);

            Image<Bgr, byte> scaledImage = new Image<Bgr, byte>(newWidth, newHeight);

            //Проходим по ВЫХОДНОМУ изображению.
            for (int y_out = 0; y_out < newHeight; y_out++)
            {
                for (int x_out = 0; x_out < newWidth; x_out++)
                {

                    //Чтобы найти исходные значения пикселя (x_in, y_in) для выходного значения (x_out, y_out), мы применяем
                    //все трансформации в обратном порядке и с обратными операциями.

                    //Центрируем координаты относительно центра нового изображения.
                    //Это нужно, чтобы вращение и масштабирование происходили вокруг центра.
                    double x_centered = x_out - newWidth / 2.0;
                    double y_centered = y_out - newHeight / 2.0;

                    //Вращаем в обратную сторону.
                    double x_rotated = x_centered * cos + y_centered * sin;
                    double y_rotated = -x_centered * sin + y_centered * cos;

                    //Обратный сдвиг.
                    double x_sheared = x_rotated - y_rotated * shear;
                    double y_sheared = y_rotated;

                    //Обратное масштабирование (деление вместо умножения).
                    double x_scaled = x_sheared / scaleX;
                    double y_scaled = y_sheared / scaleY;

                    //Возвращаем координаты в исходную систему (смещаем от центра источника).
                    double x_in = x_scaled + centerX;
                    double y_in = y_scaled + centerY;

                    //Проверяем, что вычисленные координаты находятся в пределах исходного изображения.
                    if (x_in >= 0 && y_in >= 0 && x_in < sourceImage.Width - 1 && y_in < sourceImage.Height - 1)
                    {
                        Bgr color;

                        if (useInterpolation)
                        {
                            //Используем билинейную интерполяцию для гладкости.
                            color = BilinearInterpolate(sourceImage, (float)x_in, (float)y_in);
                        }
                        else
                        { 
                            // Простой вариант - берем цвет ближайшего пикселя.
                            color = sourceImage[(int)Math.Truncate(y_in), (int)Math.Truncate(x_in)];
                        }

                        scaledImage[y_out, x_out] = color;
                    }
                }
            }

            MainImage.Source = ToBitmapSource(scaledImage);
        }

        //Функция применяет к изображению эффект горизонтальных волн.
        private void OnWaveFilterChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            //Здесь мы не меняем размер, поэтому можно использовать Clone().
            Image<Bgr, byte> wavyImage = sourceImage.Clone();

            double waveAmplitude = WaveAmplitude.Value;
            double waveFrequency = WaveFrequency.Value;

            for (int y_out = 0; y_out < wavyImage.Height; y_out++)
            {
                for (int x_out = 0; x_out < wavyImage.Width; x_out++)
                {
                    //Горизонтальное смещение (offsetX) зависит от вертикальной координаты (y_out) и синусоиды, что и создает эффект волны.
                    double offsetX = waveAmplitude * Math.Sin(y_out * waveFrequency);

                    //Вычисляем исходную координату x_in, смещая ее на offsetX.
                    double x_in = x_out + offsetX;
                    double y_in = y_out; //Координата y не меняется

                    if (x_in >= 0 && y_in >= 0 && x_in < sourceImage.Width - 1 && y_in < sourceImage.Height - 1)
                    {
                        wavyImage[y_out, x_out] = sourceImage[(int)Math.Truncate(y_in), (int)Math.Truncate(x_in)];
                    }
                }
            }

            MainImage.Source = ToBitmapSource(wavyImage);
        }

        //Функция применяет к изображению эффект закручивания вокруг центра.
        private void OnTwirlFilterChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sourceImage == null) return;

            Image<Bgr, byte> twirlImage = sourceImage.Clone();

            double twirlStrength = TwirlStrength.Value;

            if (Math.Abs(twirlStrength) > 0.01)
            {
                float centerX = sourceImage.Width / 2.0f;
                float centerY = sourceImage.Height / 2.0f;

                //Максимальный радиус, в пределах которого будет действовать эффект.
                double twirlRadius = Math.Min(centerX, centerY);

                for (int y_out = 0; y_out < twirlImage.Height; y_out++)
                {
                    for (int x_out = 0; x_out < twirlImage.Width; x_out++)
                    {
                        //Переходим от обычных координат к координатам относительно центра.
                        double dx = x_out - centerX;
                        double dy = y_out - centerY;

                        //Конвертируем декартовы координаты (dx, dy) в полярные (distance, angle).
                        double distance = Math.Sqrt(dx * dx + dy * dy);
                        double angle = Math.Atan2(dy, dx); // Угол

                        //Эффект применяется только внутри заданного радиуса.
                        if (distance < twirlRadius)
                        {
                            //Модифицируем угол. Сила смещения (factor) зависит от расстояния до центра:
                            //в центре она максимальна (1.0), на краю радиуса - нулевая (0.0).
                            double factor = 1.0 - (distance / twirlRadius);
                            angle += twirlStrength * factor; //Смещаем угол

                            //Конвертируем обратно в декартовы координаты, чтобы найти x_in, y_in.
                            double x_in = centerX + distance * Math.Cos(angle);
                            double y_in = centerY + distance * Math.Sin(angle);

                            if (x_in >= 0 && y_in >= 0 && x_in < sourceImage.Width - 1 && y_in < sourceImage.Height - 1)
                            {
                                twirlImage[y_out, x_out] = BilinearInterpolate(sourceImage, (float)x_in, (float)y_in);
                            }
                        }
                        else
                        {
                            //Если пиксель за пределами радиуса, просто копируем его цвет.
                            twirlImage[y_out, x_out] = sourceImage[y_out, x_out];
                        }
                    }
                }
            }

            MainImage.Source = ToBitmapSource(twirlImage);
        }

    }
}
