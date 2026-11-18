namespace Lianxin.Fleet.Core.Validation;

public readonly record struct ScreenPoint(double X, double Y);

public static class ScreenCoordinates
{
    public static ScreenPoint? Map(double x, double y, double surfaceWidth, double surfaceHeight,
        double imageWidth, double imageHeight, bool clamp = false)
    {
        if (new[] { x, y, surfaceWidth, surfaceHeight, imageWidth, imageHeight }.Any(value => !double.IsFinite(value)) ||
            surfaceWidth <= 0 || surfaceHeight <= 0 || imageWidth <= 0 || imageHeight <= 0)
            return null;
        var scale = Math.Min(surfaceWidth / imageWidth, surfaceHeight / imageHeight);
        var relativeX = (x - (surfaceWidth - imageWidth * scale) / 2) / (imageWidth * scale);
        var relativeY = (y - (surfaceHeight - imageHeight * scale) / 2) / (imageHeight * scale);
        if (!clamp && (relativeX < 0 || relativeX > 1 || relativeY < 0 || relativeY > 1))
            return null;
        return new(Math.Clamp(relativeX, 0, 1), Math.Clamp(relativeY, 0, 1));
    }
}
