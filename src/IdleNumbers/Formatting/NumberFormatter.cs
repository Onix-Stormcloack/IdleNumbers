using System;
using System.Globalization;
using IdleNumbers;

namespace IdleNumbers.Formatting;

internal static class NumberFormatter
{
    // Short-scale English names. Conway-Wechsler composition ABOVE vigintillion
    // is a later phase: falling back to scientific is intentional and reliable.
    private static readonly string[] KnownIllions =
    {
        "", "thousand", "million", "billion", "trillion", "quadrillion", "quintillion",
        "sextillion", "septillion", "octillion", "nonillion", "decillion",
        "undecillion", "duodecillion", "tredecillion", "quattuordecillion",
        "quindecillion", "sexdecillion", "septendecillion", "octodecillion",
        "novemdecillion", "vigintillion"
    };
    private static readonly char[] Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    internal static string Format(IdleNumber number, NumberNotation notation, int decimalPlaces)
    {
        if (decimalPlaces < 0 || decimalPlaces > 14)
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Choose 0..14 decimals.");
        if (notation is not NumberNotation.Scientific and not NumberNotation.Letters and not NumberNotation.ConwayWechsler)
            throw new ArgumentOutOfRangeException(nameof(notation));
        if (number.IsZero) return "0";

        string sign = number.Sign < 0 ? "-" : "";
        double mantissa = Math.Abs(number.Mantissa);
        long exponent = number.Exponent;

        if (notation == NumberNotation.Scientific)
            return Scientific(sign, mantissa, exponent, decimalPlaces);

        // Never accidentally display a tiny nonzero number as "0.00".
        if (exponent < -decimalPlaces)
            return Scientific(sign, mantissa, exponent, decimalPlaces);

        if (exponent < 3)
        {
            double plain = mantissa * Math.Pow(10, exponent); // bounded exponent here
            double rounded = Math.Round(plain, decimalPlaces, MidpointRounding.AwayFromZero);
            if (rounded < 1000)
                return sign + Fixed(rounded, decimalPlaces);
            return Grouped(sign, 1, 1, notation, decimalPlaces, mantissa, exponent);
        }

        long group = exponent / 3;
        int remaining = (int)(exponent % 3);
        double scaled = mantissa * Math.Pow(10, remaining);
        double roundedScaled = Math.Round(scaled, decimalPlaces, MidpointRounding.AwayFromZero);
        if (roundedScaled >= 1000)
        {
            group++;
            roundedScaled /= 1000;
        }
        return Grouped(sign, roundedScaled, group, notation, decimalPlaces, mantissa, exponent);
    }

    private static string Grouped(string sign, double scaled, long group,
        NumberNotation notation, int decimals, double originalMantissa, long originalExponent)
    {
        string? suffix = notation == NumberNotation.Letters
            ? LettersSuffix(group)
            : IllionSuffix(group);
        if (suffix is null)
            return Scientific(sign, originalMantissa, originalExponent, decimals);
        string separator = notation == NumberNotation.ConwayWechsler ? " " : "";
        return sign + Fixed(scaled, decimals) + separator + suffix;
    }

    private static string Scientific(string sign, double mantissa, long exponent, int decimals)
    {
        double rounded = Math.Round(mantissa, decimals, MidpointRounding.AwayFromZero);
        if (rounded >= 10 && exponent < long.MaxValue)
        {
            rounded /= 10;
            exponent++;
        }
        // At long.MaxValue, 10.00eMax is mathematically sound even though not normalized.
        return sign + Fixed(rounded, decimals) + "e" + exponent.ToString(CultureInfo.InvariantCulture);
    }

    private static string Fixed(double value, int decimals) =>
        value.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    private static string? IllionSuffix(long group) =>
        group > 0 && group < KnownIllions.Length ? KnownIllions[(int)group] : null;

    // Thousand group 1->K, 2->M, 3->B, 4->T, 5->aa, 6->ab...
    // 52x52 codes from aa to ZZ, then scientific fallback.
    private static string? LettersSuffix(long group)
    {
        return group switch
        {
            1 => "K",
            2 => "M",
            3 => "B",
            4 => "T",
            _ => TwoLetterSuffix(group)
        };
    }

    private static string? TwoLetterSuffix(long group)
    {
        long index = group - 5;
        if (index < 0 || index >= 52L * 52L) return null;
        return new string(new[] { Alphabet[(int)(index / 52)], Alphabet[(int)(index % 52)] });
    }
}
