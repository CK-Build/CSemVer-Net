using CSemVer;
using System.Collections;

namespace CSemVer
{
    /// <summary>
    /// Simple way to allow or forbids patch/minor/major new versions.
    /// The numeric values of these enums are from weakest (no constraint) to strongest (exact version).
    /// </summary>
    public enum SVersionLock : byte
    {
        /// <summary>
        /// Version can be freely upgraded, no restriction apply.
        /// <para>
        /// For npm, this corresponds to the a '>=' prefix (when the version is naked, '=' applies that is <see cref="Lock"/> for us).
        /// However, all -prerelases are rejected by default unless the "includePrerelease" flag is used (see https://github.com/npm/node-semver#prerelease-tags).
        /// </para>
        /// <para>
        /// For NuGet, this corresponds to a naked version (Minimum version, inclusive): "1.2.3" is like the npm's ">=1.2.3".
        /// </para>
        /// </summary>
        NoLock = 0,

        /// <summary>
        /// Allows Minor and/or Patch-level changes.
        /// <para>
        /// For npm, this is the ^ (Caret Ranges) version range specification when the Major is at
        /// least 1: "^2.3.4" matches "2.3.4" to "&lt;3.0.0". When Major is 0, npm handle the ^ specifically:
        /// 0.0.x versions are <see cref="LockPatch"/> and 0.x.y versions (where x >= 1) are <see cref="LockMinor"/>.
        /// </para>
        /// <para>
        /// Important: this ^ excludes prereleases!
        /// </para>
        /// <para>
        /// For NuGet, this can be specified with the "Mixed inclusive minimum and exclusive maximum version"
        /// with the trick of the lowest prerelease tag being "0" to avoid accepting prerelease of the next major!
        /// syntax: "[1.2.3,2.0.0-0)" (the general pattern being "[X.Y.Z,X+1.0.0-0)").
        /// </para>
        /// <para>
        /// Note that until https://github.com/NuGet/NuGetGallery/issues/6948 is resolved, the "-0" trick cannot be used
        /// on https://nuget.org. As long as CSemVer is used (or the -alpha convention is respected for the first prerelease
        /// version), "-a" must be used instead of "-0". The pattern becomes: "[X.Y.Z,X+1.0.0-a)". 
        /// </para>
        /// </summary>
        LockMajor = 1,

        /// <summary>
        /// Allows Patch-level changes.
        /// <para>
        /// This is the ~ (Tilde Ranges) of npm version range specification when at least Major.Minor
        /// are specified: "~1.2.3" matches "1.2.3" to "&lt;1.3.0".
        /// When only the Major is specified (like in "~2"), it is a <see cref="LockMajor"/>.
        /// <para>
        /// Important: this ~ excludes prereleases unless the "includePrerelease" flag is used (see https://github.com/npm/node-semver#prerelease-tags).
        /// </para>
        /// </para>
        /// <para>
        /// For NuGet, this can be specified with the "Mixed inclusive minimum and exclusive maximum version"
        /// with the trick of the lowest prerelease tag being "0" to avoid accepting prerelease of the next minor!
        /// syntax: "[1.2.3,1.3.0-0)" (the general pattern being "[X.Y.Z,X.Y+1.0-0)").
        /// </para>
        /// <para>
        /// Note that until https://github.com/NuGet/NuGetGallery/issues/6948 is resolved, the "-0" trick cannot be used
        /// on https://nuget.org. As long as CSemVer is used (or the -alpha convention is respected for the first prerelease
        /// version), "-a" must be used instead of "-0". The pattern becomes: "[X.Y.Z,X.Y+1.0-a)". 
        /// </para>
        /// </summary>
        LockMinor = 2,

        /// <summary>
        /// Same as <see cref="Lock"/> except that pre-releases of the next patch are allowed.
        /// <para>
        /// For npm, this is de facto the option when the version has a prerelease tag.
        /// See https://github.com/npm/node-semver#prerelease-tags.
        /// </para>
        /// <para>
        /// For NuGet, this can be specified with the "Mixed inclusive minimum and exclusive maximum version"
        /// with the trick of the lowest prerelease tag being "0" to avoid accepting prerelease of the next patch.
        /// syntax: "[1.2.3,1.2.4-0)" (the general pattern being "[X.Y.Z,X.Y.Z+1-0)").
        /// </para>
        /// <para>
        /// Note that until https://github.com/NuGet/NuGetGallery/issues/6948 is resolved, the "-0" trick cannot be used
        /// on https://nuget.org. As long as CSemVer is used (or the -alpha convention is respected for the first prerelease
        /// version), "-a" must be used instead of "-0". The pattern becomes: "[X.Y.Z,X.Y.Z+1-a)" (nuget, just like CSemVer, is case insensitive). 
        /// </para>
        /// </summary>
        LockPatch = 3,

        /// <summary>
        /// Fixed version. Such fixed versions should be changed manually: it indicates that, for any reason, there is a
        /// strict adherence to the dependency's version.
        /// <para>
        /// For npm, this is the default for naked version: the '=' prefix is considered ("1.2.3" is like "=1.2.3").
        /// </para>
        /// <para>
        /// For NuGet, this is an "Exact version match" denoted by brackets: "[1.2.3]".
        /// </para>
        /// </summary>
        Lock = 4
    }

}
