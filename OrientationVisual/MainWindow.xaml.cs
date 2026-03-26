using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OrientationVisual
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            this.DataContext = vm;

            try
            {
                var importer = new HelixToolkit.Wpf.ModelImporter();
                // Загрузка модели из папки запуска
                Model3D model = importer.Load("NS1_v4.obj");

                var group = new Model3DGroup();

                // Создаем группу трансформаций для "Матрешки"
                var transformGroup = new Transform3DGroup();

                // 1. Сначала базовый разворот меша, чтобы привести локальные оси модели 
                // к мировым осям приложения (Forward = +Y, Up = +Z).
                // Поворачиваем на -90 вокруг X, чтобы Y-up стал Z-up.
                transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -90)));

                // 2. Если после этого "нос" смотрит не в +Y, добавляем поворот вокруг Х.
                transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180)));
                transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 180)));

                model.Transform = transformGroup;

                group.Children.Add(model);
                SatelliteContainer.Content = group;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки модели: {ex.Message}");
            }
        }
    }
}