public static class Diagnostics
{
    /// <summary>
    /// If <paramref name="val" /> is false, throws a <see cref="System.Exception" /> with the <paramref name="message" />.
    /// </summary>
    /// <param name="val">Checks if this value is true.</param>
    /// <param name="message">Message for the thrown exception.</param>
    public static void Assert(bool val, string message)
    {
        if (!val)
        {
            Throw(message);
        }
    }

    /// <summary>
    /// If <paramref name="val" /> is false, throws <paramref name="exception" />.
    /// </summary>
    /// <param name="val">Checks if this value is true.</param>
    /// <param name="exception">Exception to throw.</param>
    public static void Assert(bool val, System.Exception exception)
    {
        if (!val)
        {
            Throw(exception);
        }
    }

    public static void Throw(System.Exception exception)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        throw exception;
#else
        System.IO.File.WriteAllText("Error.txt", exception.ToString());
        UnityEngine.Application.Quit();
#endif
    }

    public static void Throw(string message)
    {
        Throw(new System.Exception(message));
    }
}
