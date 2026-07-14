using System;
using System.Numerics;

public static class KinematicsMath
{
    private const float Deg2Rad = (float)Math.PI / 180f;
    private const float Rad2Deg = 180f / (float)Math.PI;

    // Последовательность Z-Y-X: Yaw(Z) -> Pitch(Y) -> Roll(X)
    public static Quaternion EulerToQuaternion(float yaw, float pitch, float roll)
    {
        float y = yaw * Deg2Rad;
        float p = pitch * Deg2Rad;
        float r = roll * Deg2Rad;

        Quaternion qYaw = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, y);
        Quaternion qPitch = Quaternion.CreateFromAxisAngle(Vector3.UnitY, p);
        Quaternion qRoll = Quaternion.CreateFromAxisAngle(Vector3.UnitX, r);

        // Правильный порядок для Z-Y-X
        return qYaw * qPitch * qRoll;
    }

    public static (float yaw, float pitch, float roll) QuaternionToEuler(Quaternion q)
    {
        float w = q.W, x = q.X, y = q.Y, z = q.Z;

        // Pitch (вокруг Y)
        float sinP = 2.0f * (w * y - z * x);
        float pitch;
        if (Math.Abs(sinP) >= 1)
            pitch = (float)Math.CopySign(Math.PI / 2, sinP);
        else
            pitch = (float)Math.Asin(sinP);

        // Roll (вокруг X)
        float sinR_cosP = 2.0f * (w * x + y * z);
        float cosR_cosP = 1.0f - 2.0f * (x * x + y * y);
        float roll = (float)Math.Atan2(sinR_cosP, cosR_cosP);

        // Yaw (вокруг Z)
        float sinY_cosP = 2.0f * (w * z + x * y);
        float cosY_cosP = 1.0f - 2.0f * (y * y + z * z);
        float yaw = (float)Math.Atan2(sinY_cosP, cosY_cosP);

        return (yaw * Rad2Deg, pitch * Rad2Deg, roll * Rad2Deg);
    }
}