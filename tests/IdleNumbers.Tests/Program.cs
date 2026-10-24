using System;
using System.Diagnostics;
using IdleNumbers;

internal static class Program
{
    private static int _checks;

    private static void Main(string[] args)
    {
        Tests();
        Console.WriteLine($"PASS: {_checks} checks on standard numbers.");
        Console.WriteLine("Tower arithmetic is intentionally not included in this phase.");
        if (Array.Exists(args, a => a == "--bench")) Bench();
    }

    private static void Tests()
    {
        // Canonical normalization / signed zero / extreme finite double values.
        Eq(new IdleNumber(0, 100), IdleNumber.Zero);
        Eq(new IdleNumber(-0.0, long.MaxValue), IdleNumber.Zero);
        Eq(new IdleNumber(1000), new IdleNumber(1, 3));
        Eq(new IdleNumber(-5000), new IdleNumber(-5, 3));
        Eq(new IdleNumber(2.5, -3).Sign, 1);
        Yes(new IdleNumber(double.Epsilon).Sign > 0, "smallest subnormal survives");
        Yes(new IdleNumber(double.MaxValue).Exponent >= 308, "largest finite double survives");
        Throws<ArgumentOutOfRangeException>(() => new IdleNumber(double.NaN));
        Throws<ArgumentOutOfRangeException>(() => new IdleNumber(double.NegativeInfinity));
        Throws<OverflowException>(() => new IdleNumber(100, long.MaxValue));
        Throws<OverflowException>(() => new IdleNumber(.01, long.MinValue));

        // Ordering must consider signs AND exponents.
        Yes(new IdleNumber(9, 10) < new IdleNumber(1, 100));
        Yes(new IdleNumber(-1, 100) < new IdleNumber(-9, 10));
        Yes(new IdleNumber(1, 9) > IdleNumber.Zero);
        Yes(new IdleNumber(-1, 9) < IdleNumber.Zero);
        Eq(new IdleNumber(2, 4), new IdleNumber(20, 3));
        Eq(IdleNumber.Min(new IdleNumber(3), new IdleNumber(5)), new IdleNumber(3));
        Eq(IdleNumber.Max(new IdleNumber(3), new IdleNumber(5)), new IdleNumber(5));
        Eq(IdleNumber.Clamp(new IdleNumber(50), new IdleNumber(1), new IdleNumber(10)), new IdleNumber(10));
        Throws<ArgumentException>(() => IdleNumber.Clamp(IdleNumber.One, new IdleNumber(5), new IdleNumber(1)));

        // Zero and cancellation must never loop.
        Eq(new IdleNumber(7, 90) - new IdleNumber(7, 90), IdleNumber.Zero);
        Eq(new IdleNumber(1, long.MaxValue) * IdleNumber.Zero, IdleNumber.Zero);
        Eq(IdleNumber.Zero / new IdleNumber(3, -20), IdleNumber.Zero);
        Throws<DivideByZeroException>(() => IdleNumber.One / IdleNumber.Zero);

        // Arithmetic / no mutation / negligible terms.
        Near(new IdleNumber(1, 10) - new IdleNumber(1, 3), new IdleNumber(9.999999, 9));
        Near(new IdleNumber(1, 10) + new IdleNumber(5, 9), new IdleNumber(1.5, 10));
        Eq(new IdleNumber(1, 100) + IdleNumber.One, new IdleNumber(1, 100));
        Near(new IdleNumber(2, 10) * new IdleNumber(3, 20), new IdleNumber(6, 30));
        Near(new IdleNumber(1, 50) / new IdleNumber(2, 49), new IdleNumber(5));
        Near(new IdleNumber(1, -500) / new IdleNumber(1, 1000), new IdleNumber(1, -1500));
        Eq(new IdleNumber(1, long.MaxValue) / new IdleNumber(1, 1), new IdleNumber(1, long.MaxValue - 1));
        Eq(new IdleNumber(1, long.MaxValue) / new IdleNumber(9, -1),
            new IdleNumber(10.0 / 9.0, long.MaxValue));
        Throws<OverflowException>(() => new IdleNumber(1, long.MaxValue) / new IdleNumber(1, -1));
        Throws<OverflowException>(() => new IdleNumber(9, long.MaxValue) * new IdleNumber(9));
        var a = new IdleNumber(2, 80);
        var b = new IdleNumber(1, 79);
        _ = a + b;
        _ = a * b;
        Eq(a, new IdleNumber(2, 80));
        Eq(b, new IdleNumber(1, 79));

        // Integer powers / square root / logarithms.
        Near(new IdleNumber(2, 3).Pow(3), new IdleNumber(8, 9));
        Near(new IdleNumber(2, 3).Pow(-1), new IdleNumber(5, -4));
        Near(new IdleNumber(9, -3).Sqrt(), new IdleNumber(Math.Sqrt(90), -2));
        Near(new IdleNumber(4, 8).Sqrt(), new IdleNumber(2, 4));
        Eq(IdleNumber.Zero.Pow(0), IdleNumber.One);
        Eq(IdleNumber.Zero.Pow(2), IdleNumber.Zero);
        Throws<DivideByZeroException>(() => IdleNumber.Zero.Pow(-1));
        Throws<ArgumentOutOfRangeException>(() => new IdleNumber(-1).Sqrt());
        Throws<ArgumentOutOfRangeException>(() => IdleNumber.Zero.Log10());
        Yes(Math.Abs(new IdleNumber(1, 100).Log10() - 100) < 1e-12);

        // Formatting: 2/3 decimals, K/M/B/T/aa/.../ZZ, significant rollover.
        Eq(new IdleNumber(1250).ToString(), "1.25K");
        Eq(new IdleNumber(1000).ToString(NumberNotation.Scientific), "1.00e3");
        Eq(new IdleNumber(1234567).ToString(NumberNotation.Letters, 3), "1.235M");
        Eq(new IdleNumber(999.9998).ToString(), "1.00K");
        Eq(new IdleNumber(999999.9).ToString(), "1.00M");
        Eq(new IdleNumber(1, 15).ToString(NumberNotation.Letters), "1.00aa");
        Eq(new IdleNumber(1, 18).ToString(NumberNotation.Letters), "1.00ab");
        Eq(new IdleNumber(1, 8124).ToString(NumberNotation.Letters), "1.00ZZ");
        Eq(new IdleNumber(1, 8127).ToString(NumberNotation.Letters), "1.00e8127");
        Eq(new IdleNumber(1, -8).ToString(), "1.00e-8");
        Eq(new IdleNumber(1, -2).ToString(), "0.01");
        Eq(new IdleNumber(1, 6).ToString(NumberNotation.ConwayWechsler), "1.00 million");
        Eq(new IdleNumber(1, 63).ToString(NumberNotation.ConwayWechsler), "1.00 vigintillion");
        Eq(new IdleNumber(1, 66).ToString(NumberNotation.ConwayWechsler), "1.00e66");
        Eq(new IdleNumber(9.9999, 50).ToString(NumberNotation.Scientific), "1.00e51");
        Eq(IdleNumber.Zero.ToString(), "0");
        Throws<ArgumentOutOfRangeException>(() => IdleNumber.One.ToString(NumberNotation.Letters, -1));
        Throws<ArgumentOutOfRangeException>(() => IdleNumber.One.ToString((NumberNotation)999));

        // Storage must not depend on OS locale or a HUD notation.
        Eq(IdleNumber.Parse("1.25e10000000000"), new IdleNumber(1.25, 10_000_000_000));
        Eq(IdleNumber.Parse("-1234.5"), new IdleNumber(-1234.5));
        RoundTrip(new IdleNumber(1.23456789012345, 123456));
        RoundTrip(new IdleNumber(-9.12345678912345, -98765));
        RoundTrip(new IdleNumber(1, long.MaxValue));
        RoundTrip(IdleNumber.Zero);
        Yes(!IdleNumber.TryParse("NaN", out _));
        Yes(!IdleNumber.TryParse("Infinity", out _));
        Yes(!IdleNumber.TryParse("1,25e10", out _));
        Yes(!IdleNumber.TryParse("1e", out _));
        Yes(!IdleNumber.TryParse("1e999999999999999999999", out _));
        Throws<FormatException>(() => IdleNumber.Parse("garbage"));

        // Reproducible property-style smoke tests, limited to standard arithmetic.
        Random rng = new(20261024);
        for (int i = 0; i < 1500; i++)
        {
            var x = new IdleNumber((rng.Next(2) == 0 ? -1 : 1) * (1 + 8 * rng.NextDouble()), rng.Next(-1000, 1000));
            var y = new IdleNumber((rng.Next(2) == 0 ? -1 : 1) * (1 + 8 * rng.NextDouble()), rng.Next(-1000, 1000));
            Eq(x + y, y + x);
            Eq(x * y, y * x);
            Eq(x.CompareTo(y), -y.CompareTo(x));
            Eq(x - y, x + (-y));
            RoundTrip(x);
        }
    }

    private static void Bench()
    {
        const int count = 1_000_000;
        var x = new IdleNumber(1.25, 10);
        var delta = new IdleNumber(1.5, 5);
        var clock = Stopwatch.StartNew();
        for (int i = 0; i < count; i++) x += delta;
        clock.Stop();
        Console.WriteLine($"{count:N0} additions: {clock.Elapsed.TotalMilliseconds:N1}ms, checksum: {x.ToStorageString()}");
        Console.WriteLine("Baseline only: measure in Release on the game's target runtime.");
    }

    private static void RoundTrip(IdleNumber x) => Eq(IdleNumber.Parse(x.ToStorageString()), x);

    private static void Near(IdleNumber actual, IdleNumber expected)
    {
        if (actual == expected) { _checks++; return; }
        if (expected.IsZero) { Eq(actual, IdleNumber.Zero); return; }
        IdleNumber error = (actual - expected).Abs() / expected.Abs();
        Yes(error <= new IdleNumber(1, -12), $"Expected {expected.ToStorageString()}, actual {actual.ToStorageString()}");
    }

    private static void Eq<T>(T actual, T expected)
    {
        _checks++;
        if (!Equals(actual, expected))
            throw new InvalidOperationException($"Expected: {expected}; actual: {actual}");
    }

    private static void Yes(bool passed, string message = "Assertion failed")
    {
        _checks++;
        if (!passed) throw new InvalidOperationException(message);
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        _checks++;
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected exception {typeof(T).Name}.");
    }
}
