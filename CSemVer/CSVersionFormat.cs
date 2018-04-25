namespace CSemVer
{
    /// <summary>
    /// Format description for <see cref="CSVersion.ToString(CSVersionFormat,CIBuildDescriptor,bool)"/>.
    /// </summary>
    public enum CSVersionFormat
    {
        /// <summary>
        /// Normalized semantic version format.
        /// It is the same as <see cref="SVersion.ToString()"/> when no <see cref="CIBuildDescriptor"/> is provided (it
        /// does not include the <see cref="SVersion.BuildMetaData"/>).
        /// The prerelease name is the standard one (ie. 'prerelease' for any unknown name).
        /// This is the default.
        /// </summary>
        Normalized,

        /// <summary>
        /// Same as <see cref="Normalized"/> with the trailng +<see cref="SVersion.BuildMetaData"/>.
        /// </summary>
        NormalizedWithBuildMetaData,

        /// <summary>
        /// The file version (see https://msdn.microsoft.com/en-us/library/system.diagnostics.fileversioninfo.fileversion.aspx)
        /// uses the whole 64 bits: it is the <see cref="CSVersion.OrderedVersion"/> left shifted by 1 bit with 
        /// the less significant bit set to 0 for release and 1 CI builds (when a <see cref="CIBuildDescriptor"/> is provided).
        /// </summary>
        FileVersion,

        /// <summary>
        /// Short form. This format is the one to use for package version.
        /// This includes <see cref="CIBuildDescriptor"/> if an applicable one is provided but not the <see cref="SVersion.BuildMetaData"/>.
        /// It is short, readable even when a CIBuildDescriptor is provided and compatible with any version of NuGet (or
        /// other basic, non conformant, implementation of Semantic Versionning).
        /// </summary>
        ShortForm,

        /// <summary>
        /// Same as <see cref="ShortForm"/> with the trailng +<see cref="SVersion.BuildMetaData"/>.
        /// </summary>
        ShortFormWithhBuildMetaData,

        /// <summary>
        /// NuGet format. The <see cref="ShortForm"/> is always used.
        /// </summary>
        NuGetPackage = ShortForm

    }
}
