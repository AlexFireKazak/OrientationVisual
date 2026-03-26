using System;
using System.Numerics;

public static class KinematicsMath
{
    private const float Deg2Rad = (float)Math.PI / 180f;
    private const float Rad2Deg = 180f / (float)Math.PI;

    // Последовательность 3-2-1: Yaw(Z) -> Pitch(X) -> Roll(Y)
    // Инвертирован Pitch для соответствия логике Unity (Forward = +Y)
    public static Quaternion EulerToQuaternion(float yaw, float pitch, float roll)
    {
        float y = yaw * Deg2Rad;
        float p = -pitch * Deg2Rad; // Инверсия знака Pitch здесь
        float r = roll * Deg2Rad;

        // Вращения вокруг мировых осей в правой системе координат
        Quaternion qYaw = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), y);
        Quaternion qPitch = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), p);
        Quaternion qRoll = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), r);

        // Результирующий кватернион: Z * X * Y
        return qYaw * qPitch * qRoll;
    }

    public static (float yaw, float pitch, float roll) QuaternionToEuler(Quaternion q)
    {
        float w = q.W; float x = q.X; float y = q.Y; float z = q.Z;

        // Извлечение Pitch (вокруг X)
        float sinP = 2.0f * (w * x - y * z);
        float pitch;
        if (Math.Abs(sinP) >= 1)
            pitch = (float)Math.CopySign(Math.PI / 2, sinP);
        else
            pitch = (float)Math.Asin(sinP);

        // Извлечение Roll (вокруг Y)
        float sinR_cosP = 2.0f * (w * y + x * z);
        float cosR_cosP = 1.0f - 2.0f * (x * x + y * y);
        float roll = (float)Math.Atan2(sinR_cosP, cosR_cosP);

        // Извлечение Yaw (вокруг Z)
        float sinY_cosP = 2.0f * (w * z + x * y);
        float cosy_cosp = 1.0f - 2.0f * (x * x + z * z);
        float yaw = (float)Math.Atan2(sinY_cosP, cosy_cosp);

        // Возвращаем Pitch с обратным знаком, чтобы UI и математика были синхронны
        return (yaw * Rad2Deg, -pitch * Rad2Deg, roll * Rad2Deg);
    }
}