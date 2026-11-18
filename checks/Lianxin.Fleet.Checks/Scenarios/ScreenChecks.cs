using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static class ScreenChecks
{
    public static Task Coordinates()
    {
        var center = ScreenCoordinates.Map(200, 200, 400, 400, 390, 844);
        Assert.True(center.HasValue);
        Assert.Equal(new ScreenPoint(0.5, 0.5), center!.Value);
        Assert.True(ScreenCoordinates.Map(0, 200, 400, 400, 390, 844) is null, "Portrait side padding must not send a tap");
        Assert.True(ScreenCoordinates.Map(200, 0, 400, 400, 844, 390) is null, "Landscape top padding must not send a tap");
        Assert.Equal(0.0, ScreenCoordinates.Map(-100, 200, 400, 400, 390, 844, true)!.Value.X);
        Assert.Equal(1.0, ScreenCoordinates.Map(1000, 200, 400, 400, 390, 844, true)!.Value.X);
        Assert.True(ScreenCoordinates.Map(double.NaN, 20, 400, 400, 390, 844) is null);
        Assert.True(ScreenCoordinates.Map(10, 10, 0, 400, 390, 844) is null);
        return Task.CompletedTask;
    }
}
