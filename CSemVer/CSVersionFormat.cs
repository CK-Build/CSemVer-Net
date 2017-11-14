namespace CSemVer
{
    /// <summary>
    /// Format description for <see cref="CSVersion.ToString(CSVersionFormat,CIBuildDescriptor,bool)"/>.
    /// </summary>
    public enum CSVersionFormat
    {
        /// <summary>
        /// Normalized format is 'v' + <see cref="SemVerWithMarker"/>.
        /// This is the default.
        /// </summary>
        Normalized,

        /// <summary>
        /// Semantic version format.
        /// The prerelease name is the standard one (ie. 'prerelease' for any unknown name) and there is no build meata data.
        /// This includes <see cref="CIBuildDescriptor"/> if an applicable one is provided.
        /// </summary>
        SemVer,

        /// <summary>
        /// Semantic version format.
        /// The prerelease name is the standard one (ie. 'prerelease' for any unknown name) plus build meata data (+invalid).
        /// This includes <see cref="CIBuildDescriptor"/> if an applicable one is provided.
        /// </summary>
        SemVerWithMarker,

        /// <summary>
        /// The file version (see https://msdn.microsoft.com/en-us/library/system.diagnostics.fileversioninfo.fileversion.aspx)
        /// uses the whole 64 bits: it is the <see cref="CSVersion.OrderedVersion"/> left shifted by 1 bit with 
        /// the less significant bit set to 0 for release and 1 CI builds.
        /// </summary>
        FileVersion,

        /// <summary>
        /// Short form. This includes <see cref="CIBuildDescriptor"/> if an applicable one is provided.
        /// </summary>
        ShortForm,

        /// <summary>
        /// NuGet version 2. This includes <see cref="CIBuildDescriptor"/> if an applicable one is provided.
        /// </summary>
        NugetPackageV2 = ShortForm,

        /// <summary>
        /// NuGet format. Currently <see cref="ShortForm"/>.
        /// </summary>
        NuGetPackage = ShortForm,

        /// <summary>
        /// Default is <see cref="Normalized"/>.
        /// </summary>
        Default = Normalized
    }
}
