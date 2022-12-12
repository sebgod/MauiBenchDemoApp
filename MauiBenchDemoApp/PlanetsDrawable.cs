using Astap.Lib.Astrometry;
using Astap.Lib.Astrometry.Catalogs;
using Astap.Lib.Astrometry.VSOP87;

namespace MauiBenchDemoApp;

public sealed class PlanetsDrawable : IDrawable
{
    internal static readonly double FramesPerSecond = 25;
    internal static readonly double NormalSpeed = TimeSpan.FromHours(0.25 / FramesPerSecond).TotalDays;

    static readonly TimeSpan NormalInterval = TimeSpan.FromDays(NormalSpeed);

    private DateTimeOffset _time = DateTimeOffset.Now;
    private string _speedFmt = FormatSpeed(NormalInterval);

    private readonly Dictionary<CatalogIndex, (double minX, double maxX, double minY, double maxY)> _orbits = new();

    public PlanetsDrawable()
    {
        var earthDay = _time;

        for (var i = 0; i < 60142; i++)
        {
            earthDay.ToSOFAUtcJdTT(out _, out _, out var tt1, out var tt2);
            double et = (tt1 - 2451545.0 + tt2) / 365250.0;

            if (i < 88)
            {
                CalcOrbitLimits(CatalogIndex.Mercury, et);
            }

            if (i < 225 && (i % 2) == 0)
            {
                CalcOrbitLimits(CatalogIndex.Venus, et);
            }

            if (i < 365 && (i % 2) == 0)
            {
                CalcOrbitLimits(CatalogIndex.Earth, et);
            }

            if (i < 687 && (i % 3) == 0)
            {
                CalcOrbitLimits(CatalogIndex.Mars, et);
            }

            if (i < 4320 && (i % 4) == 0)
            {
                CalcOrbitLimits(CatalogIndex.Jupiter, et);
            }

            if (i < 10767 && (i % 5) == 0)
            {
                CalcOrbitLimits(CatalogIndex.Saturn, et);
            }

            if (i < 30646 && (i % 15) == 0)
            {
                CalcOrbitLimits(CatalogIndex.Uranus, et);
            }

            if (i % 24 == 0)
            {
                CalcOrbitLimits(CatalogIndex.Neptune, et);
            }

            earthDay = earthDay.AddDays(1);
        }
    }

    void CalcOrbitLimits(CatalogIndex catalogIndex, double et)
    {
        var body = new double[3];
        VSOP87a.GetBody(catalogIndex, et, body);

        var minX = double.MaxValue;
        var maxX = double.MinValue;
        var minY = double.MaxValue;
        var maxY = double.MinValue;

        if (_orbits.TryGetValue(catalogIndex, out var minMaxXY))
        {
            (minX, maxX, minY, maxY) = minMaxXY;
        }

        minX = Math.Min(body[0], minX);
        minY = Math.Min(body[1], minY);
        maxX = Math.Max(body[0], maxX);
        maxY = Math.Max(body[1], maxY);

        _orbits[catalogIndex] = (minX, maxX, minY, maxY);
    }

    double MaxOrbitDiameter(CatalogIndex catalogIndex)
    {
        var (minX, maxX, minY, maxY) = _orbits[catalogIndex];
        return Math.Max(maxX - minX, maxY - minY);
    }

    double MinXOrbit(CatalogIndex catalogIndex)
    {
        var (minX, maxX, _, _) = _orbits[catalogIndex];
        return Math.Min(Math.Abs(minX), Math.Abs(maxX));
    }

    double MinYOrbit(CatalogIndex catalogIndex)
    {
        var (_, _, minY, maxY) = _orbits[catalogIndex];
        return Math.Min(Math.Abs(minY), Math.Abs(maxY));
    }

    double MinOrbit(CatalogIndex catalogIndex) => Math.Min(MinXOrbit(catalogIndex), MinYOrbit(catalogIndex));

    public void Advance(TimeSpan timeSpan)
    {
        _time += timeSpan;
        _speedFmt = FormatSpeed(timeSpan);
    }

    public static string FormatSpeed(TimeSpan interval)
    {
        const double DaysPerA = 365.25;

        var speed = interval * FramesPerSecond;
        var sign = speed < TimeSpan.Zero ? -1 : 1;
        if (sign == -1)
        {
            speed = speed.Negate();
        }

        var fmtSign = sign == 1 ? "+" : "-";

        if (speed >= TimeSpan.FromDays(DaysPerA))
        {
            return fmtSign + (speed.TotalDays / DaysPerA).ToString("0.00") + " a";
        }
        else if (speed >= TimeSpan.FromDays(1))
        {
            return fmtSign + speed.TotalDays.ToString("0.00") + " d";
        }
        else if (speed >= TimeSpan.FromHours(1))
        {
            return fmtSign + speed.TotalHours.ToString("0.00") + " h";
        }
        else if (speed >= TimeSpan.FromMinutes(1))
        {
            return fmtSign + speed.TotalMinutes.ToString("0.00") + " m";
        }
        else
        {
            return fmtSign + speed.TotalSeconds.ToString("0.00") + " s";
        }
    }

    public bool OnlyInner { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var planetsDiskFactor = OnlyInner ? 3 : 1;

        canvas.FillColor = Color.FromArgb("717378");
        canvas.FillRectangle(dirtyRect);

        var w = dirtyRect.Width;
        var h = dirtyRect.Height;

        var d = MathF.Min(w, h);
        var cW = w * 0.5f;
        var cH = h * 0.5f;

        double auPerPixel;
        var saturnPixelScale = d / MaxOrbitDiameter(CatalogIndex.Saturn);
        var jupiterPixelScale = d / MaxOrbitDiameter(CatalogIndex.Jupiter);
        var uranusPixelScale = d / MaxOrbitDiameter(CatalogIndex.Uranus);
        var neptunePixelScale = d / MaxOrbitDiameter(CatalogIndex.Neptune);

        if (!OnlyInner && neptunePixelScale > 20)
        {
            auPerPixel = neptunePixelScale;
        }
        else if (!OnlyInner && uranusPixelScale > 20)
        {
            auPerPixel = uranusPixelScale;
        }
        else if (!OnlyInner && saturnPixelScale > 20)
        {
            auPerPixel = saturnPixelScale;
        }
        else if (!OnlyInner && jupiterPixelScale > 20)
        {
            auPerPixel = jupiterPixelScale;
        }
        else
        {
            auPerPixel = d / MaxOrbitDiameter(CatalogIndex.Mars);
        }

        auPerPixel *= 0.9; // margin

        canvas.StrokeColor = Color.FromArgb("000000");

        canvas.DrawString(string.Format("{0:dd/MM/yyyy HH:mm:ss} {1}/s", _time, _speedFmt), 5, 25, HorizontalAlignment.Left);


        DrawOrbit(CatalogIndex.Mercury);
        DrawOrbit(CatalogIndex.Venus);
        DrawOrbit(CatalogIndex.Earth);
        DrawOrbit(CatalogIndex.Mars);
        DrawOrbit(CatalogIndex.Jupiter);
        DrawOrbit(CatalogIndex.Saturn);
        DrawOrbit(CatalogIndex.Uranus);
        DrawOrbit(CatalogIndex.Neptune);

        _time.ToSOFAUtcJdTT(out _, out _, out var tt1, out var tt2);
        double et = (tt1 - 2451545.0 + tt2) / 365250.0;

        var desiredSunSize = 25;
        var minDistance = desiredSunSize / 5 * planetsDiskFactor;
        DrawPlanet(CatalogIndex.Sol, "FCE570", Math.Min(desiredSunSize, Math.Max(2, AUToPix(MinOrbit(CatalogIndex.Mercury)) - minDistance)));
        DrawPlanet(CatalogIndex.Mercury, "B7B8B9", 1 * planetsDiskFactor);
        DrawPlanet(CatalogIndex.Venus, "968396", 2 * planetsDiskFactor);
        DrawPlanet(CatalogIndex.Earth, "287AB8", 2 * planetsDiskFactor);
        DrawPlanet(CatalogIndex.Mars, "9C2E35", 1 * planetsDiskFactor);
        DrawPlanet(CatalogIndex.Jupiter, "Bcafb2", 4 * planetsDiskFactor);
        DrawPlanet(CatalogIndex.Saturn, "Ab604a", 3 * planetsDiskFactor);
        DrawPlanet(CatalogIndex.Uranus, "B2D6DB", 2 * planetsDiskFactor);
        DrawPlanet(CatalogIndex.Neptune, "2990B5", 2 * planetsDiskFactor);

        float AUToPix(double pos) => (float)(auPerPixel * pos);

        void DrawPlanet(CatalogIndex catalogIndex, string color, float radius)
        {
            var body = new double[3];
            VSOP87a.GetBody(catalogIndex, et, body);
            canvas.FillColor = canvas.StrokeColor = Color.FromArgb(color);
            canvas.FillCircle(AUToPix(body[0]) + cW, AUToPix(body[1]) + cH, radius);
        }

        void DrawOrbit(CatalogIndex catalogIndex)
        {
            var (minX, maxX, minY, maxY) = _orbits[catalogIndex];
            canvas.DrawEllipse(AUToPix(minX) + cW, AUToPix(minY) + cH, AUToPix(maxX - minX), AUToPix(maxY - minY));
        }
    }
}
