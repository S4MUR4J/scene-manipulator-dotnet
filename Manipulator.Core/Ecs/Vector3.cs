namespace Manipulator.Core.Ecs;

public readonly record struct Vector3(float X, float Y, float Z)
{
    public static Vector3 Zero => new Vector3(0, 0, 0);
    public static Vector3 One => new Vector3(1, 1, 1);
    public static Vector3 Up => new Vector3(0, 1, 0);

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
}
