namespace CSemVer
{
    /// <summary>
    /// Extends SVersionLock enum.
    /// </summary>
    public static class SVersionLockExtension
    {
        /// <summary>
        /// Merges this lock with another one: the weakest wins, merging <see cref="SVersionLock.LockedMinor"/> with <see cref="SVersionLock.Locked"/>
        /// results in <see cref="SVersionLock.LockedMinor"/>.
        /// </summary>
        /// <param name="this">This lock.</param>
        /// <param name="other">The other lock.</param>
        /// <returns>The weakest of the two.</returns>
        public static SVersionLock Union( this SVersionLock @this, SVersionLock other )
        {
            return @this < other ? @this : other;
        }
    }

}
