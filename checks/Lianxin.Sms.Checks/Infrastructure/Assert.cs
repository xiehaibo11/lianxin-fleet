namespace Lianxin.Sms.Checks.Infrastructure;

internal static class Assert
{
    public static void True(bool condition, string message = "Assertion failed")
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
    public static void Equal<T>(T expected, T actual) => True(EqualityComparer<T>.Default.Equals(expected, actual),
        $"Expected {expected}, got {actual}");
    public static async Task WaitFor(Func<Task<bool>> condition)
    {
        for (var i = 0; i < 100; i++)
        {
            if (await condition())
                return;
            await Task.Delay(100);
        }
        throw new TimeoutException("等待模拟设备或同步任务超时。");
    }
}
