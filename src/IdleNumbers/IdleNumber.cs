using System;
using System.Globalization;
using IdleNumbers.Formatting;
using IdleNumbers.Internal;

namespace IdleNumbers;

/// <summary>
/// Signed, immutable base-10 floating number for idle games.
/// Phase 1 represents mantissa * 10^exponent, with a finite double mantissa and
/// a signed Int64 exponent. For nonzero values, 1 &lt;= |mantissa| &lt; 10.
/// The exponent representation already reserves a symbolic tower for phase 2,
/// but tower arithmetic is deliberately NOT part of this release.
/// </summary>
public readonly struct IdleNumber : IEquatable<IdleNumber>, IComparable<IdleNumber>
{
    private readonly double _mantissa;
    private readonly NumberExponent _exponent;

    /// <summary>Normalized mantissa; zero is represented as (0, 0).</summary>
    public double Mantissa => _mantissa;

    /// <summary>Exact decimal exponent of a standard number (tower values are not exposed yet).</summary>
    public long Exponent => _exponent.HasTower
        ? throw new NotSupportedException("Tower exponents are not exposed in phase 1.")
        : _exponent.Local;

    /// <summary>True if a future astronomical representation is present.</summary>
    public bool IsAstronomical => _exponent.HasTower;
    public bool IsZero => _mantissa == 0;
    public int Sign => Math.Sign(_mantissa);

    public static IdleNumber Zero => default;
    public static IdleNumber One => new(1);

    /// <summary>Creates an IdleNumber from a finite standard double.</summary>
    public IdleNumber(double value) : this(value, 0) { }

    /// <summary>Creates a number equal to mantissa * 10^exponent, normalizing its mantissa.</summary>
    public IdleNumber(double mantissa, long exponent)
    {
        if (!double.IsFinite(mantissa))
            throw new ArgumentOutOfRangeException(nameof(mantissa), "NaN and Infinity are not supported.");
        if (mantissa == 0)
        {
            _mantissa = 0;
            _exponent = default;
            return;
        }

        // Only 0..4 large scaling steps are required for any finite double.
        // Large chunks prevent underflow during normalization of subnormals.
        double abs = Math.Abs(mantissa);
        while (abs >= 1e100)
        {
            mantissa /= 1e100;
            exponent = checked(exponent + 100);
            abs = Math.Abs(mantissa);
        }
        while (abs >= 10)
        {
            mantissa /= 10;
            exponent = checked(exponent + 1);
            abs = Math.Abs(mantissa);
        }
        while (abs < 1e-100)
        {
            mantissa *= 1e100;
            exponent = checked(exponent - 100);
            abs = Math.Abs(mantissa);
        }
        while (abs < 1)
        {
            mantissa *= 10;
            exponent = checked(exponent - 1);
            abs = Math.Abs(mantissa);
        }
        _mantissa = mantissa;
        _exponent = new NumberExponent(exponent);
    }

    // Used only when mathematical operations have already normalized the mantissa.
    private IdleNumber(double normalizedMantissa, long normalizedExponent, bool _)
    {
        _mantissa = normalizedMantissa;
        _exponent = new NumberExponent(normalizedExponent);
    }

    public IdleNumber Abs() => Sign < 0 ? -this : this;

    private static void EnsureStandard(IdleNumber left, IdleNumber right)
    {
        if (left.IsAstronomical || right.IsAstronomical)
            throw new NotSupportedException("Tower arithmetic is reserved for phase 2.");
    }

    public static IdleNumber operator +(IdleNumber left, IdleNumber right)
    {
        EnsureStandard(left, right);
        if (left.IsZero) return right;
        if (right.IsZero) return left;
        if (left._exponent.Local < right._exponent.Local) return right + left;

        // Unsigned difference avoids overflow even across long.MinValue..long.MaxValue.
        ulong gap = unchecked((ulong)left._exponent.Local - (ulong)right._exponent.Local);
        if (gap >= 18) return left; // Right cannot influence 15-16 significant digits.
        double aligned = left._mantissa + right._mantissa * Math.Pow(10, -(int)gap);
        return new IdleNumber(aligned, left._exponent.Local);
    }

    public static IdleNumber operator -(IdleNumber left, IdleNumber right) => left + (-right);
    public static IdleNumber operator -(IdleNumber value)
    {
        if (value.IsAstronomical)
            throw new NotSupportedException("Tower arithmetic is reserved for phase 2.");
        return value.IsZero ? Zero : new IdleNumber(-value._mantissa, value._exponent.Local, true);
    }
    public static IdleNumber operator +(IdleNumber value) => value;

    public static IdleNumber operator *(IdleNumber left, IdleNumber right)
    {
        EnsureStandard(left, right);
        if (left.IsZero || right.IsZero) return Zero;
        double mantissa = left._mantissa * right._mantissa;
        int shift = Math.Abs(mantissa) >= 10 ? 1 : 0;
        if (shift != 0) mantissa /= 10;
        long exponent = CheckedExponent((Int128)left._exponent.Local + right._exponent.Local + shift);
        return new IdleNumber(mantissa, exponent, true);
    }

    public static IdleNumber operator /(IdleNumber left, IdleNumber right)
    {
        EnsureStandard(left, right);
        if (right.IsZero) throw new DivideByZeroException();
        if (left.IsZero) return Zero;
        double mantissa = left._mantissa / right._mantissa;
        int shift = Math.Abs(mantissa) < 1 ? -1 : 0;
        if (shift != 0) mantissa *= 10;
        long exponent = CheckedExponent((Int128)left._exponent.Local - right._exponent.Local + shift);
        return new IdleNumber(mantissa, exponent, true);
    }

    private static long CheckedExponent(Int128 value)
    {
        if (value > long.MaxValue || value < long.MinValue)
            throw new OverflowException("Exponent outside Int64 range. Tower arithmetic is not active yet.");
        return (long)value;
    }

    public static bool operator ==(IdleNumber left, IdleNumber right) => left.Equals(right);
    public static bool operator !=(IdleNumber left, IdleNumber right) => !left.Equals(right);
    public static bool operator <(IdleNumber left, IdleNumber right) => left.CompareTo(right) < 0;
    public static bool operator <=(IdleNumber left, IdleNumber right) => left.CompareTo(right) <= 0;
    public static bool operator >(IdleNumber left, IdleNumber right) => left.CompareTo(right) > 0;
    public static bool operator >=(IdleNumber left, IdleNumber right) => left.CompareTo(right) >= 0;

    public int CompareTo(IdleNumber other)
    {
        EnsureStandard(this, other);
        int sign = Sign;
        if (sign != other.Sign) return sign.CompareTo(other.Sign);
        if (sign == 0) return 0;
        int compare = _exponent.Local.CompareTo(other._exponent.Local);
        if (compare == 0) compare = Math.Abs(_mantissa).CompareTo(Math.Abs(other._mantissa));
        return sign > 0 ? compare : -compare;
    }

    public bool Equals(IdleNumber other) => _mantissa.Equals(other._mantissa) && _exponent.Equals(other._exponent);
    public override bool Equals(object? obj) => obj is IdleNumber other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(_mantissa, _exponent);

    public static IdleNumber Min(IdleNumber a, IdleNumber b) => a <= b ? a : b;
    public static IdleNumber Max(IdleNumber a, IdleNumber b) => a >= b ? a : b;
    public static IdleNumber Clamp(IdleNumber value, IdleNumber min, IdleNumber max)
    {
        if (min > max) throw new ArgumentException("min cannot exceed max", nameof(min));
        return Max(min, Min(value, max));
    }

    /// <summary>Integer power. Values exceeding the standard exponent range currently throw.</summary>
    public IdleNumber Pow(long power)
    {
        if (IsAstronomical) throw new NotSupportedException("Tower arithmetic is reserved for phase 2.");
        if (power == 0) return One;
        if (IsZero)
        {
            if (power < 0) throw new DivideByZeroException("0 cannot have a negative power.");
            return Zero;
        }
        // Invert first, avoiding overflow of an unnecessarily enormous intermediate result.
        IdleNumber factor = power < 0 ? One / this : this;
        ulong remaining = power < 0 ? (ulong)(-(power + 1)) + 1UL : (ulong)power;
        IdleNumber result = One;
        while (remaining != 0)
        {
            if ((remaining & 1UL) != 0) result *= factor;
            remaining >>= 1;
            if (remaining != 0) factor *= factor;
        }
        return result;
    }

    /// <summary>Square root of a nonnegative number.</summary>
    public IdleNumber Sqrt()
    {
        if (IsAstronomical) throw new NotSupportedException("Tower arithmetic is reserved for phase 2.");
        if (Sign < 0) throw new ArgumentOutOfRangeException(nameof(Mantissa), "Negative square root.");
        if (IsZero) return Zero;
        long half = _exponent.Local / 2;
        bool odd = (_exponent.Local & 1) != 0;
        if (_exponent.Local < 0 && odd) half--; // floor(exponent / 2)
        return new IdleNumber(Math.Sqrt(_mantissa * (odd ? 10 : 1)), half);
    }

    /// <summary>Approximate logarithm. May lose unit precision above 2^53 exponent.</summary>
    public double Log10()
    {
        if (IsAstronomical) throw new NotSupportedException("Tower arithmetic is reserved for phase 2.");
        if (Sign <= 0) throw new ArgumentOutOfRangeException(nameof(Mantissa), "Log10 requires a positive number.");
        return Math.Log10(_mantissa) + _exponent.Local;
    }

    /// <summary>Invariant, round-trip storage format (never use the HUD string to save).</summary>
    public string ToStorageString()
    {
        if (IsAstronomical) throw new NotSupportedException("Tower storage is reserved for phase 2.");
        return _mantissa.ToString("R", CultureInfo.InvariantCulture) + "e"
            + _exponent.Local.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Renders for the player (2 decimal places by default).</summary>
    public string ToString(NumberNotation notation, int decimalPlaces = 2)
    {
        if (IsAstronomical) throw new NotSupportedException("Tower display is reserved for phase 2.");
        return NumberFormatter.Format(this, notation, decimalPlaces);
    }

    public override string ToString() => ToString(NumberNotation.Letters);

    public static IdleNumber Parse(string input)
    {
        if (TryParse(input, out var parsed)) return parsed;
        throw new FormatException("Expected an invariant-culture value such as 1.25e1000.");
    }

    public static bool TryParse(string? input, out IdleNumber value)
    {
        value = Zero;
        if (string.IsNullOrWhiteSpace(input)) return false;
        string text = input.Trim();
        int marker = text.IndexOfAny(new[] { 'e', 'E' });
        string mantissaText = marker < 0 ? text : text[..marker];
        string exponentText = marker < 0 ? "0" : text[(marker + 1)..];
        if (!double.TryParse(mantissaText, NumberStyles.Float, CultureInfo.InvariantCulture, out double mantissa)
            || !double.IsFinite(mantissa)
            || !long.TryParse(exponentText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long exponent))
            return false;
        try
        {
            value = new IdleNumber(mantissa, exponent);
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}
