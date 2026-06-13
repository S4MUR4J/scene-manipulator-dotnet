namespace Manipulator.Core.Serialization;

public class SceneDeserializationException : Exception
{
    public SceneDeserializationException(string message)
        : base(message) { }

    public SceneDeserializationException(string message, Exception inner)
        : base(message, inner) { }
}
