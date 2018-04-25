namespace CSemVer
{
    /// <summary>
    /// Format description for <see cref="CSVersion.ToString(CSVersionFormat,CIBuildDescriptor,bool)"/>.
    /// </summary>
    public enum CSVersionFormat
    {
        /// <summary>
        /// Normalized format is the same as <see cref="SVersion.ToString()"/> (with a 'v' prefix and <see cref="SVersion.BuildMetaData"/>).
        /// The prerelease name is the standard one (ie. 'prerelease' for any unknown name).
        /// This format does not support <see cref="CIBuildDescriptor"/>.
        /// This is the default.
        /// </summary>
        Normalized,

        /// <summary>
        /// Semantic version format.
        /// The prerelease name is the standard one (ie. 'prerelease' for any unknown name) and there is no build meta data.
        /// This includes <see cref="CIBuildDescriptor"/> if an applicable one is provided.
        /// </summary>
        SemVer,

        /// <summary>
        /// Semantic version format.
        /// The prerelease name is the standard one (ie. 'prerelease' for any unknown name) plus <see cref="SVersion.BuildMetaData"/>
        /// if it exists.
        /// This includes <see cref="CIBuildDescriptor"/> if an applicable one is provided.
        /// </summary>
        SemVerWithBuildMetaData,

        /// <summary>
        /// The file version (see https://msdn.microsoft.com/en-us/library/system.diagnostics.fileversioninfo.fileversion.aspx)
        /// uses the whole 64 bits: it is the <see cref="CSVersion.OrderedVersion"/> left shifted by 1 bit with 
        /// the less significant bit set to 0 for release and 1 CI builds (when a <see cref="CIBuildDescriptor"/> is provided).
        /// </summary>
        FileVersion,

        /// <summary>
        /// Short form. This format is the one to use for package version.
        /// This includes <see cref="CIBuildDescriptor"/> if an applicable one is provided.
        /// It is short, readable even when a CIBuildDescriptor is provided and compatible with any version of NuGet (or
        /// other basic, non conformant, implementation of Semantic Versionning).
        /// </summary>
        ShortForm,

        /// <summary>
        /// NuGet format. The <see cref="ShortForm"/> is always used.
        /// </summary>
        NuGetPackage = ShortForm

    }
}
