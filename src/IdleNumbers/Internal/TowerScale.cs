using System;

namespace IdleNumbers.Internal;

/// <summary>
/// RESERVED FOR PHASE 2; not yet used by public numerical operations.
/// Describes a *single* tower term: Coefficient * T_Layer(Top), with
/// T_0(h)=h, T_1(h)=10^h, T_2(h)=10^(10^h), and so forth.
/// A tower is symbolic: NEVER evaluate 10^T numerically.
/// </summary>
internal readonly struct TowerScale : IEquatable<TowerScale>
{
    internal int Layer { get; }
    internal int Top { get; }
    internal double Coefficient { get; }

    // default(TowerScale) means "no tower".
    internal bool IsEmpty => Layer == 0;

    private TowerScale(int layer, int top, double coefficient)
    {
        Layer = layer;
        Top = top;
        Coefficient = coefficient;
    }

    internal static TowerScale Create(int layer, int top, double coefficient)
    {
        if (layer < 1) throw new ArgumentOutOfRangeException(nameof(layer), "An astronomical tower needs at least one level.");
        if (top < 1) throw new ArgumentOutOfRangeException(nameof(top), "The tower's top must be positive.");
        if (double.IsNaN(coefficient) || double.IsInfinity(coefficient) || coefficient == 0)
            throw new ArgumentOutOfRangeException(nameof(coefficient), "The tower coefficient must be finite and nonzero.");
        return new TowerScale(layer, top, coefficient);
    }

    public bool Equals(TowerScale other) =>
        Layer == other.Layer && Top == other.Top && Coefficient.Equals(other.Coefficient);

    public override bool Equals(object? obj) => obj is TowerScale other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Layer, Top, Coefficient);
}
