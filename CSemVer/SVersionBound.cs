using System;
using System.Diagnostics;

namespace CSemVer
{
    /// <summary>
    /// Immutable base <see cref="Base"/> valid version that is the inclusive minimum acceptable <see cref="PackageQuality"/>
    /// and an optional <see cref="Lock"/> to the Base's components.
    /// <para>
    /// This aims to define a sensible response to one of the dependency management issue: how to specify "version ranges".
    /// </para>
    /// <para>
    /// The <see cref="Union(in SVersionBound)"/> binary operation defines a partial order (materialized by <see cref="Contains"/>)
    /// on the set of all possible bounds: <see cref="None"/> is the identity element (and the greatest element of the whole set)
    /// and <see cref="All"/> is the absorbing element of the <see cref="Union(in SVersionBound)"/> operation and the lowest element
    /// of the set.
    /// </para>
    /// <para>
    /// Mathematically the set of SVersionBound and the Union operation is a bounded meet semilattice that has an identity
    /// element (the <see cref="None"/>) and an absorbing element (the <see cref="All"/>).
    /// </para>
    /// </summary>
    public readonly partial struct SVersionBound : IEquatable<SVersionBound>
    {
        readonly SVersion? _base;
        readonly PackageQuality _minQuality;

        /// <summary>
        /// All bound with no restriction: <see cref="Base"/> is <see cref="SVersion.ZeroVersion"/> and there is
        /// no restriction: <see cref="Satisfy(in SVersion)"/> is true for any valid version.
        /// This bound is the absorbing element of the <see cref="Union(in SVersionBound)"/> operation.
        /// </summary>
        public static readonly SVersionBound All = new SVersionBound();

        /// <summary>
        /// None bound: <see cref="Base"/> is <see cref="SVersion.LastVersion"/>, <see cref="Lock"/> and <see cref="MinQuality"/> are
        /// the strongest possible (<see cref="SVersionLock.Locked"/> and <see cref="PackageQuality.StableRelease"/>): <see cref="Satisfy(in SVersion)"/> is
        /// true only for the last version.
        /// This bound is the identity element of the <see cref="Union(in SVersionBound)"/> operation.
        /// </summary>
        public static readonly SVersionBound None = new SVersionBound( SVersion.LastVersion, SVersionLock.Locked, PackageQuality.StableRelease );

        /// <summary>
        /// Gets the base version (inclusive minimum version). <see cref="SVersion.IsValid"/> is necessarily true
        /// since it defaults to <see cref="SVersion.ZeroVersion"/>.
        /// </summary>
        public SVersion Base => _base ?? SVersion.ZeroVersion;

        /// <summary>
        /// Gets whether only the same Major, Minor, Patch (or the exact version) of <see cref="Base"/> must be considered.
        /// </summary>
        public SVersionLock Lock { get; }

        /// <summary>
        /// Gets the minimal package quality that must be used.
        /// This is never <see cref="PackageQuality.None"/> (that denotes invalid packages), the minimum is <see cref="PackageQuality.CI"/>.
        /// </summary>
        public PackageQuality MinQuality => _minQuality != PackageQuality.None ? _minQuality : PackageQuality.CI;

        /// <summary>
        /// Initializes a new version range on a valid <see cref="Base"/> version.
        /// </summary>
        /// <param name="version">The base version that must be valid.</param>
        /// <param name="r">The lock to apply.</param>
        /// <param name="minQuality">The minimal quality to accept.</param>
        public SVersionBound( SVersion? version = null, SVersionLock r = SVersionLock.None, PackageQuality minQuality = PackageQuality.None )
        {
            _base = version ?? SVersion.ZeroVersion;
            if( !_base.IsValid ) throw new ArgumentException( "Must be valid. Error: " + _base.ErrorMessage, nameof( version ) );
            _minQuality = minQuality;
            Lock = r;
        }

        /// <summary>
        /// Sets a lock by returning this or a new <see cref="SVersionBound"/>.
        /// </summary>
        /// <param name="r">The lock to set.</param>
        /// <returns>This or a new range.</returns>
        public SVersionBound SetLock( SVersionLock r ) => r != Lock ? new SVersionBound( Base, r, MinQuality ) : this;

        /// <summary>
        /// Sets a lock by returning this or a new <see cref="SVersionBound"/>.
        /// </summary>
        /// <param name="r">The lock to set.</param>
        /// <returns>This or a new range.</returns>
        public SVersionBound SetMinQuality( PackageQuality min ) => MinQuality != min ? new SVersionBound( Base, Lock, min ) : this;

        /// <summary>
        /// Merges this version bound with another one.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public SVersionBound Union( in SVersionBound other )
        {
            var minBase = Base > other.Base ? other : this;
            return minBase.SetLock( Lock.Union( other.Lock ) ).SetMinQuality( MinQuality.Union( other.MinQuality ) );
        }

        /// <summary>
        /// Applies this bound to a version and returns whether it satisfies this <see cref="SVersionBound"/>.
        /// </summary>
        /// <param name="v">The version to challenge.</param>
        /// <returns>True if this version fits in this bound, false otherwise.</returns>
        public bool Satisfy( in SVersion v )
        {
            int cmp = Base.CompareTo( v );
            // If v is lower than this Base, it's over.
            if( cmp > 0 ) return false;
            // If v is the Base, it's trivially okay. 
            if( cmp == 0 ) return true;
            // Is the greater v "reachable"?
            Debug.Assert( v.IsValid, "Since v is greater than this Base and this Base is vaild." );
            switch( Lock )
            {
                case SVersionLock.Locked: return false;
                case SVersionLock.LockedPatch:
                    if( v.Major != Base.Major || v.Minor != Base.Minor || v.Patch != Base.Patch ) return false; break;
                case SVersionLock.LockedMinor:
                    if( v.Major != Base.Major || v.Minor != Base.Minor ) return false; break;
                case SVersionLock.LockedMajor:
                    if( v.Major != Base.Major ) return false; break;
            }
            return MinQuality <= v.PackageQuality;
        }

        public bool Contains( in SVersionBound other )
        {
            // If the other.Base version doesn't satisfy this bound, it's over.
            if( !Satisfy( other.Base ) ) return false;
            // But this is not enough: the versions that the other satisfy must all be satisfied by this bound.
            // If the other allows lowest quality, it's over.
            if( MinQuality > other.MinQuality ) return false;
            // Trivial case for locks: this Lock is stronger than the other one: it's over (for
            // instance, this locks the Minor and the other one only locks the Major: it will allow
            // versions that this one disallows).
            if( Lock > other.Lock ) return false;
            // When the other Lock is stronger than this one, since the other.Base satisfies this Base,
            // the other cannot be more restrictive than this... I know it may not be obvious, but it's true.
            // To "see" this consider that Base versions "starts" with the "locked" part and "ends free". The
            // exclamation point expresses this:
            //  - This: 1!.0.0-beta (LockedMajor) 
            //  - Other: 1.0.0!-rc (LockedPatch)
            // If we are here, then the prefix of the other.Base satisfies this bound: any stronger locks
            // don't change the prefix.
            return true;
        }

        /// <summary>
        /// Equality is based on <see cref="Base"/>, <see cref="MinQuality"/> and <see cref="Lock"/>.
        /// </summary>
        /// <param name="other">The other range.</param>
        /// <returns>True if they are the same version and restrictions.</returns>
        public bool Equals( SVersionBound other ) => Base == other.Base && MinQuality == other.MinQuality && Lock == other.Lock;

        /// <summary>
        /// Equality is based on <see cref="Base"/>, <see cref="MinQuality"/> and <see cref="Lock"/>.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the specified object is equal to this instance; otherwise, false.</returns>
        public override bool Equals( object obj ) => obj is SVersionBound r && Equals( r );

        /// <summary>
        /// Equality is based on <see cref="Base"/>, <see cref="MinQuality"/> and <see cref="Lock"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() => Base.GetHashCode() ^ ((int)MinQuality << 13) ^ ((int)Lock << 26);

        /// <summary>
        /// Overridden to return the base version and the restrictions.
        /// </summary>
        /// <returns>A readable string.</returns>
        public override string ToString() => $"{Base}[{Lock},{MinQuality}]";

    }

}
