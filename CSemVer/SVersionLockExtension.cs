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
        /// Intersects this lock with another one: the strogest wins, merging <see cref="SVersionLock.LockMinor"/> with <see cref="SVersionLock.Lock"/>
        /// results in <see cref="SVersionLock.Lock"/>.
        /// </summary>
        /// <param name="this">This lock.</param>
        /// <param name="other">The other lock.</param>
        /// <returns>The strongest of the two.</returns>
        public static SVersionLock Intersect( this SVersionLock @this, SVersionLock other )
        {
            return @this > other ? @this : other;
        }
    }

}
