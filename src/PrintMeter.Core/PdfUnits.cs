namespace PrintMeter.Core;

public static class PdfUnits
{
    public const double PointsPerInch = 72.0;

    public static double PointsToMillimeters(double points) => points * 25.4 / PointsPerInch;
}
