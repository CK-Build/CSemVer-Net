using System;

namespace CSemVer
{
    /// <summary>
    /// Extends SVersionLock enum.
    /// </summary>
    public static class SVersionLockExtension
    {
        /// <summary>
        /// Merges this lock with another one: the weakest wins, merging <see cref="SVersionLock.LockMinor"/> with <see cref="SVersionLock.Lock"/>
        /// results in <see cref="SVersionLock.LockMinor"/>.
        /// </summary>
        /// <param name="this">This lock.</param>
        /// <param name="other">The other lock.</param>
        /// <returns>The weakest of the two.</returns>
        public static SVersionLock Union( this SVersionLock @this, SVersionLock other )
        {
            return @this < other ? @this : other;
        }

        /// <summary>
        /// Intersects this lock with another one: the strongest wins, merging <see cref="SVersionLock.LockMinor"/> with <see cref="SVersionLock.Lock"/>
        /// results in <see cref="SVersionLock.Lock"/>.
        /// </summary>
        /// <param name="this">This lock.</param>
        /// <param name="other">The other lock.</param>
        /// <returns>The strongest of the two.</returns>
        public static SVersionLock Intersect( this SVersionLock @this, SVersionLock other )
        {
            return @this > other ? @this : other;
        }
        /// <summary>
        /// Tries to parse one of the <see cref="SVersionLock"/> terms (the <paramref name="head"/> must be at the start, no trimming is done).
        /// Note that match is case insensitive and that all "Lock" wan be written as "Locked".
        /// </summary>
        /// <param name="head">The string to parse.</param>
        /// <param name="l">The read lock. On error, the value is unchanged.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool TryMatch( ReadOnlySpan<char> head, ref SVersionLock l ) => TryMatch( ref head, ref l );

        /// <summary>
        /// Tries to parse one of the <see cref="SVersionLock"/> terms (the <paramref name="head"/> must be at the start, no trimming is done).
        /// Note that match is case insensitive and that all "Lock" wan be written as "Locked".
        /// On success, the <paramref name="head"/> is forwarded right after the match: the head may be on any kind of character.
        /// </summary>
        /// <param name="head">The string to parse.</param>
        /// <param name="l">The read lock. On error, the value is unchanged.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool TryMatch( ref ReadOnlySpan<char> head, ref SVersionLock l )
        {
            if( head.Length == 0 ) return false;
            if( !head.StartsWith( nameof( SVersionLock.Lock ), StringComparison.OrdinalIgnoreCase ) )
            {
                if( head.StartsWith( nameof( SVersionLock.NoLock ), StringComparison.OrdinalIgnoreCase ) )
                {
                    l = SVersionLock.NoLock;
                    head = head.Slice( 6 );
                    return true;
                }
                // Allow previous "None" name.
                if( head.StartsWith( "none", StringComparison.OrdinalIgnoreCase ) )
                {
                    l = SVersionLock.NoLock;
                    head = head.Slice( 4 );
                    return true;
                }
                return false;
            }
            head = head.Slice( 4 );
            if( head.StartsWith( "ed", StringComparison.OrdinalIgnoreCase ) ) head = head.Slice( 2 );
            if( head.StartsWith( "major", StringComparison.OrdinalIgnoreCase ) )
            {
                head = head.Slice( 5 );
                l = SVersionLock.LockMajor;
                return true;
            }
            if( head.StartsWith( "minor", StringComparison.OrdinalIgnoreCase ) )
            {
                head = head.Slice( 5 );
                l = SVersionLock.LockMinor;
                return true;
            }
            if( head.StartsWith( "patch", StringComparison.OrdinalIgnoreCase ) )
            {
                head = head.Slice( 5 );
                l = SVersionLock.LockPatch;
                return true;
            }
            l = SVersionLock.Lock;
            return true;
        }

    }

}
