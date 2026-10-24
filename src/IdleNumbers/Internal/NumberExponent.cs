using System;

namespace IdleNumbers.Internal;

/// <summary>
/// Reserved exponent model. Standard: E = Local.
/// Future astronomical: E = Tower.Coefficient * T_Layer(Top) + Local.
/// All phase-1 arithmetic operates exclusively on standard exponents.
/// </summary>
internal readonly struct NumberExponent : IEquatable<NumberExponent>
{
    internal long Local { get; }
    internal TowerScale Tower { get; }
    internal bool HasTower => !Tower.IsEmpty;

    internal NumberExponent(long local)
    {
        Local = local;
        Tower = default;
    }

    // For phase 2 only. Not exposed by IdleNumber until normalization, ordering,
    // cancellation and storage semantics for towers are fully implemented.
    internal static NumberExponent WithTower(long local, TowerScale tower)
    {
        if (tower.IsEmpty) throw new ArgumentException("Tower must be nonempty.", nameof(tower));
        return new NumberExponent(local, tower);
    }

    private NumberExponent(long local, TowerScale tower)
    {
        Local = local;
        Tower = tower;
    }

    public bool Equals(NumberExponent other) => Local == other.Local && Tower.Equals(other.Tower);
    public override bool Equals(object? obj) => obj is NumberExponent other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Local, Tower);
}
