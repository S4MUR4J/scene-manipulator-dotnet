namespace Manipulator.Core.Ecs;

public readonly record struct Vector3(float X, float Y, float Z)
{
    public static Vector3 Zero => new Vector3(0, 0, 0);
    public static Vector3 One => new Vector3(1, 1, 1);
    public static Vector3 Up => new Vector3(0, 1, 0);
    public static Vector3 Down => new Vector3(0, -1, 0);
    public static Vector3 Left => new Vector3(-1, 0, 0);
    public static Vector3 Right => new Vector3(1, 0, 0);
    public static Vector3 Forward => new Vector3(0, 0, 1);
    public static Vector3 Back => new Vector3(0, 0, -1);

    public static Vector3 operator +(Vector3 a, Vector3 b)
    {
        var result = new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        return result;
    }

    public static Vector3 operator -(Vector3 a, Vector3 b)
    {
        var result = new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        return result;
    }

    public static Vector3 operator *(Vector3 a, Vector3 b)
    {
        var result = new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        return result;
    }

    public static Vector3 operator *(Vector3 a, float b) => new Vector3(a.X * b, a.Y * b, a.Z * b);

    public static Vector3 operator *(float a, Vector3 b) => new Vector3(a * b.X, a * b.Y, a * b.Z);

    public bool IsFinite() => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Z);
}
