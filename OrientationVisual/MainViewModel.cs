using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
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

    private bool _isSyncing = false;

    public MainViewModel()
    {
        LoadModel();
    }

    private void LoadModel()
    {
        // Здесь будет логика загрузки NS1_v4.obj через Helix ModelImporter
    }

    partial void OnRollChanged(float value) => SyncFromEuler();
    partial void OnPitchChanged(float value) => SyncFromEuler();
    partial void OnYawChanged(float value) => SyncFromEuler();

    private void SyncFromEuler()
    {
        if (_isSyncing) return;
        _isSyncing = true;

        // Используем математический кватернион
        NumQ q = KinematicsMath.EulerToQuaternion(Yaw, Pitch, Roll);
        UpdateQuaternionFields(q);
        ApplyToModel(q);

        _isSyncing = false;
    }

    [RelayCommand]
    private void ApplyQuaternion()
    {
        if (float.TryParse(QW, out var w) && float.TryParse(QX, out var x) &&
            float.TryParse(QY, out var y) && float.TryParse(QZ, out var z))
        {
            NumQ q = new NumQ(x, y, z, w);
            // Метод Length() есть только у System.Numerics.Quaternion
            if (q.Length() > 0) q = NumQ.Normalize(q);

            var (yaw, pitch, roll) = KinematicsMath.QuaternionToEuler(q);

            _isSyncing = true;
            Yaw = yaw; Pitch = pitch; Roll = roll;
            UpdateQuaternionFields(q);
            ApplyToModel(q);
            _isSyncing = false;
        }
    }

    // По требованию: показываю полную функцию конвертации и применения
    private void ApplyToModel(NumQ q)
    {
        // Явно преобразуем из System.Numerics в System.Windows.Media.Media3D
        // ВНИМАНИЕ: Порядок аргументов в конструкторе MediaQ: (x, y, z, w)
        MediaQ visualQ = new MediaQ(q.X, q.Y, q.Z, q.W);

        SatelliteRotation = new QuaternionRotation3D(visualQ);
    }

    private void UpdateQuaternionFields(NumQ q)
    {
        QW = q.W.ToString("F5");
        QX = q.X.ToString("F5");
        QY = q.Y.ToString("F5");
        QZ = q.Z.ToString("F5");
        Magnitude = q.Length();
    }
}