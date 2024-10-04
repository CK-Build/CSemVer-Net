using System;

namespace CSemVer;

/// <summary>
/// Defines a "Min-Max" (this is the string representation) filter of <see cref="PackageQuality"/>.
/// The <c>default</c> is "CI-CI".
/// </summary>
public readonly struct PackageQualityFilter : IEquatable<PackageQualityFilter>
{
    readonly PackageQuality _min;
    readonly PackageQuality _max;

    /// <summary>
    /// Gets the minimal package quality.
    /// </summary>
    public PackageQuality Min => _min;

    /// <summary>
    /// Gets the maximal package quality.
    /// </summary>
    public PackageQuality Max => _max;

    /// <summary>
    /// Gets whether this filter allows the specified quality.
    /// </summary>
    /// <param name="q">The quality to challenge.</param>
    /// <returns>Whether <paramref name="q"/> is accepted or not.</returns>
    public bool Accepts( PackageQuality q ) => q >= _min && q <= _max;

    /// <summary>
    /// Initializes a new filter. Min must be lower or equal to max otherwise an <see cref="ArgumentException"/> is thrown.
    /// </summary>
    /// <param name="min">The minimal quality.</param>
    /// <param name="max">The maximal quality.</param>
    public PackageQualityFilter( PackageQuality min, PackageQuality max )
    {
        if( min > max ) throw new ArgumentException( "min must be lower or equal to max." );
        _min = min;
        _max = max;
    }

    /// <summary>
    /// Initializes a new filter from a string.
    /// Throws an <see cref="ArgumentException"/> on invalid syntax.
    /// Simply uses <see cref="TryParse(ReadOnlySpan{char}, out PackageQualityFilter)"/>) to handle invalid syntax.
    /// </summary>
    /// <param name="s">The string.</param>
    public PackageQualityFilter( ReadOnlySpan<char> s )
    {
        if( !TryParse( s, out PackageQualityFilter p ) ) throw new ArgumentException( "Invalid PackageQualityFilter syntax." );
        _min = p._min;
        _max = p._max;
    }

    /// <summary>
    /// Overridden to return "<see cref="Min"/>-<see cref="Max"/>".
    /// </summary>
    /// <returns>The "Min-Max" string.</returns>
    public override string ToString() => Min.ToString() + '-' + Max.ToString();

    /// <summary>
    /// Attempts to parse a string as a <see cref="PackageQualityFilter"/>.
    /// Note that the parse is case insensitive, that white spaces are silently ignored and min/max can be reversed.
    /// <para>
    /// Examples:
    /// "Stable" (is the same as "Stable-Stable"): only <see cref="PackageQuality.Stable"/> is accepted
    /// "CI-Stable" (is the same as "-Stable" or "CI-" or ""): everything is accepted.
    /// "-ReleaseCandidate" (same as "CI-RC" or "CI-ReleaseCandidate"): everything except Stable.
    /// "Exploratory-Preview": No CI, ReleaseCandidate, nor Stable.
    /// </para>
    /// </summary>
    /// <param name="head">The string to parse (leading and internal white spaces between tokens are skipped).</param>
    /// <param name="filter">The result.</param>
    /// <returns>True on success, false on error.</returns>
    public static bool TryParse( ReadOnlySpan<char> head, out PackageQualityFilter filter ) => TryParse( ref head, out filter );

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, out PackageQualityFilter)"/>
    public static bool TryParse( ref ReadOnlySpan<char> head, out PackageQualityFilter filter )
    {
        var start = head;
        var min = PackageQuality.CI;
        var max = PackageQuality.Stable;
        head = head.TrimStart();
        bool hasMin = PackageQualityExtension.TryMatch( ref head, ref min );
        bool hasMax = false;
        var sHead = head;
        if( (head = head.TrimStart()).Length > 0 && head[0] == '-' )
        {
            if( !hasMin ) sHead = head;
            head = head.Slice( 1 ).TrimStart();
            hasMax = PackageQualityExtension.TryMatch( ref head, ref max );
            if( !hasMax ) head = sHead;
        }
        else head = sHead;
        if( hasMin || hasMax )
        {
            if( max == PackageQuality.CI ) max = PackageQuality.Stable;
            if( min > max )
            {
                (min, max) = (max, min);
            }
            filter = new PackageQualityFilter( min, max );
            return true;
        }
        filter = new PackageQualityFilter();
        head = start;
        return false;
    }

    /// <summary>
    /// Implements equality operator.
    /// </summary>
    /// <param name="other">Other filter.</param>
    /// <returns>True on success, false if other is different than this one.</returns>
    public bool Equals( PackageQualityFilter other ) => _min == other._min && _max == other._max;

    /// <summary>
    /// Overridden to call <see cref="Equals(PackageQualityFilter)"/>.
    /// </summary>
    /// <param name="obj">The other object.</param>
    /// <returns>True on success, false if other is not a filter or is different than this one.</returns>
    public override bool Equals( object obj ) => obj is PackageQualityFilter f && Equals( f );

    /// <summary>
    /// Overridden to match <see cref="Equals(PackageQualityFilter)"/>.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => (int)_min << 8 | (int)_max;
}
