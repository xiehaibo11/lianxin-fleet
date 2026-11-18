namespace Lianxin.Fleet.Checks.Infrastructure;

internal static class Assert
{
    public static void True(bool condition, string message = "Assertion failed")
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
    public static void Equal<T>(T expected, T actual) => True(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected {expected}, got {actual}");
    public static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
    public static async Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try
        {
            await action();
        }
        catch (T error) { return error; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
