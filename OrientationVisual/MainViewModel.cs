using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Globalization;
using System.Numerics;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using NumQ = System.Numerics.Quaternion;
using MediaQ = System.Windows.Media.Media3D.Quaternion;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private float _roll, _pitch, _yaw;
    [ObservableProperty] private string _qW = "1.00000", _qX = "0.00000", _qY = "0.00000", _qZ = "0.00000";
    [ObservableProperty] private double _magnitude = 1.0;
    [ObservableProperty] private QuaternionRotation3D _satelliteRotation = new();

    // Основная ось визирования: Прямой расчет
    [ObservableProperty] private string _visX = "0.00000";
    [ObservableProperty] private string _visY = "0.00000";
    [ObservableProperty] private string _visZ = "-1.00000";

    // Основная ось визирования: Обратный расчет
    [ObservableProperty] private string _revX = "0.00000";
    [ObservableProperty] private string _revY = "0.00000";
    [ObservableProperty] private string _revZ = "-1.00000";

    // Определение положения по вектору визирования
    [ObservableProperty] private string _inputVisX = "0.00000";
    [ObservableProperty] private string _inputVisY = "0.00000";
    [ObservableProperty] private string _inputVisZ = "-1.00000";

    // АЛЬТЕРНАТИВНАЯ ОСЬ: Локальные координаты
    [ObservableProperty] private string _altLocalX = "0.00000";
    [ObservableProperty] private string _altLocalY = "0.00000";
    [ObservableProperty] private string _altLocalZ = "-1.00000";

    // ИНТЕРАКТИВНЫЙ ВЕКТОР: Положение кончика стрелки в пространстве
    private Point3D _altTargetPoint = new Point3D(0, 0, -4);
    public Point3D AltTargetPoint
    {
        get => _altTargetPoint;
        set
        {
            if (SetProperty(ref _altTargetPoint, value))
            {
                UpdateInteractiveVectorProjections(value);
            }
        }
    }

    // Текстовые свойства для двустороннего ввода мировых проекций интерактивного вектора
    private string _altWorldX = "0.00000";
    public string AltWorldX
    {
        get => _altWorldX;
        set
        {
            if (SetProperty(ref _altWorldX, value))
            {
                ApplyManualWorldProjections();
            }
        }
    }

    private string _altWorldY = "0.00000";
    public string AltWorldY
    {
        get => _altWorldY;
        set
        {
            if (SetProperty(ref _altWorldY, value))
            {
                ApplyManualWorldProjections();
            }
        }
    }

    private string _altWorldZ = "-1.00000";
    public string AltWorldZ
    {
        get => _altWorldZ;
        set
        {
            if (SetProperty(ref _altWorldZ, value))
            {
                ApplyManualWorldProjections();
            }
        }
    }

    private bool _isSyncing = false;
    private bool _isAltSyncing = false; // Флаг защиты от зацикливания интерактивного вектора

    public MainViewModel()
    {
        UpdateInteractiveVectorProjections(AltTargetPoint);
    }

    
    partial void OnRollChanged(float value) => SyncFromEuler();
    partial void OnPitchChanged(float value) => SyncFromEuler();
    partial void OnYawChanged(float value) => SyncFromEuler();

    // Обновление текстовых полей при перемещении вектора МЫШЬЮ
    private void UpdateInteractiveVectorProjections(Point3D point)
    {
        if (_isAltSyncing) return;
        _isAltSyncing = true;

        Vector3D vec = new Vector3D(point.X, point.Y, point.Z);
        if (vec.Length > 0)
        {
            vec.Normalize();
        }

        AltWorldX = vec.X.ToString("F5", CultureInfo.InvariantCulture);
        AltWorldY = vec.Y.ToString("F5", CultureInfo.InvariantCulture);
        AltWorldZ = vec.Z.ToString("F5", CultureInfo.InvariantCulture);

        _isAltSyncing = false;
    }

    // Обновление 3D сцены при РУЧНОМ ВВОДЕ проекций в текстовые поля
    private void ApplyManualWorldProjections()
    {
        if (_isAltSyncing) return;

        string xStr = AltWorldX.Replace(',', '.');
        string yStr = AltWorldY.Replace(',', '.');
        string zStr = AltWorldZ.Replace(',', '.');

        if (double.TryParse(xStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
            double.TryParse(yStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
            double.TryParse(zStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
        {
            Vector3D vec = new Vector3D(x, y, z);
            if (vec.Length == 0) return;

            vec.Normalize(); // Нормируем введенный вектор

            _isAltSyncing = true;
            // Умножаем на 4.0, чтобы сохранить масштаб стрелки (базовый радиус манипулятора)
            AltTargetPoint = new Point3D(vec.X * 4.0, vec.Y * 4.0, vec.Z * 4.0);
            _isAltSyncing = false;
        }
    }

    partial void OnAltLocalXChanged(string value) => RecalculateCurrentProjections();
    partial void OnAltLocalYChanged(string value) => RecalculateCurrentProjections();
    partial void OnAltLocalZChanged(string value) => RecalculateCurrentProjections();

    private void RecalculateCurrentProjections()
    {
        if (_isSyncing) return;
        NumQ q = KinematicsMath.EulerToQuaternion(Yaw, Pitch, Roll);
        UpdateSightAxisProjections(q);
    }

    private void SyncFromEuler()
    {
        if (_isSyncing) return;
        _isSyncing = true;

        NumQ q = KinematicsMath.EulerToQuaternion(Yaw, Pitch, Roll);
        UpdateQuaternionFields(q);
        ApplyToModel(q);

        _isSyncing = false;
    }

    [RelayCommand]
    private void ApplyQuaternion()
    {
        string wStr = QW.Replace(',', '.');
        string xStr = QX.Replace(',', '.');
        string yStr = QY.Replace(',', '.');
        string zStr = QZ.Replace(',', '.');

        if (float.TryParse(wStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var w) &&
            float.TryParse(xStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
            float.TryParse(yStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
            float.TryParse(zStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
        {
            NumQ q = new NumQ(x, y, z, w);
            if (q.Length() > 0) q = NumQ.Normalize(q);

            var (yaw, pitch, roll) = KinematicsMath.QuaternionToEuler(q);

            _isSyncing = true;
            Yaw = yaw; Pitch = pitch; Roll = roll;
            UpdateQuaternionFields(q);
            ApplyToModel(q);
            _isSyncing = false;
        }
    }

    [RelayCommand]
    private void ApplySightVector()
    {
        string xStr = InputVisX.Replace(',', '.');
        string yStr = InputVisY.Replace(',', '.');
        string zStr = InputVisZ.Replace(',', '.');

        if (float.TryParse(xStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
            float.TryParse(yStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
            float.TryParse(zStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
        {
            Vector3 targetVector = new Vector3(x, y, z);
            if (targetVector.Length() == 0) return;
            targetVector = Vector3.Normalize(targetVector);

            Vector3 baseVector = new Vector3(0, 0, -1f);
            NumQ q;
            float dot = Vector3.Dot(baseVector, targetVector);

            if (dot >= 0.99999f)
            {
                q = NumQ.Identity;
            }
            else if (dot <= -0.99999f)
            {
                q = NumQ.CreateFromAxisAngle(new Vector3(1, 0, 0), (float)Math.PI);
            }
            else
            {
                Vector3 cross = Vector3.Cross(baseVector, targetVector);
                q = new NumQ(cross.X, cross.Y, cross.Z, dot + (float)Math.Sqrt(baseVector.LengthSquared() * targetVector.LengthSquared()));
                q = NumQ.Normalize(q);
            }

            var (yaw, pitch, roll) = KinematicsMath.QuaternionToEuler(q);

            _isSyncing = true;
            Yaw = yaw; Pitch = pitch; Roll = roll;
            UpdateQuaternionFields(q);
            ApplyToModel(q);
            _isSyncing = false;

            InputVisX = targetVector.X.ToString("F5", CultureInfo.InvariantCulture);
            InputVisY = targetVector.Y.ToString("F5", CultureInfo.InvariantCulture);
            InputVisZ = targetVector.Z.ToString("F5", CultureInfo.InvariantCulture);
        }
    }

    private void ApplyToModel(NumQ q)
    {
        MediaQ visualQ = new MediaQ(q.X, q.Y, q.Z, q.W);
        SatelliteRotation = new QuaternionRotation3D(visualQ);
    }

    private void UpdateQuaternionFields(NumQ q)
    {
        QW = q.W.ToString("F5", CultureInfo.InvariantCulture);
        QX = q.X.ToString("F5", CultureInfo.InvariantCulture);
        QY = q.Y.ToString("F5", CultureInfo.InvariantCulture);
        QZ = q.Z.ToString("F5", CultureInfo.InvariantCulture);
        Magnitude = q.Length();

        UpdateSightAxisProjections(q);
    }

    private void UpdateSightAxisProjections(NumQ q)
    {
        Vector3 localSightAxis = new Vector3(0, 0, -1f);

        Vector3 worldSightAxis = Vector3.Transform(localSightAxis, q);
        VisX = worldSightAxis.X.ToString("F5", CultureInfo.InvariantCulture);
        VisY = worldSightAxis.Y.ToString("F5", CultureInfo.InvariantCulture);
        VisZ = worldSightAxis.Z.ToString("F5", CultureInfo.InvariantCulture);

        NumQ invertedQ = NumQ.Inverse(q);
        Vector3 reversedSightAxis = Vector3.Transform(localSightAxis, invertedQ);
        RevX = reversedSightAxis.X.ToString("F5", CultureInfo.InvariantCulture);
        RevY = reversedSightAxis.Y.ToString("F5", CultureInfo.InvariantCulture);
        RevZ = reversedSightAxis.Z.ToString("F5", CultureInfo.InvariantCulture);
    }
}