namespace CSemVer
{
    /// <summary>
    /// Format description for <see cref="CSVersion.ToString(CSVersionFormat,CIBuildDescriptor)"/>.
    /// </summary>
    public enum CSVersionFormat
    {
        /// <summary>
        /// Normalized semantic version format.
        /// It is the same as <see cref="SVersion.ToString()"/> when no <see cref="CIBuildDescriptor"/> is provided.
        /// It is short, readable even when a CIBuildDescriptor is provided and compatible with any version of NuGet (or
        /// other basic, non conformant, implementation of Semantic Versionning).
        /// The prerelease short name is the standard one (ie. 'p' for any unknown name).
        /// This is the default.
        /// </summary>
        Normalized,

        /// <summary>
        /// The file version (see https://msdn.microsoft.com/en-us/library/system.diagnostics.fileversioninfo.fileversion.aspx)
        /// uses the whole 64 bits: it is the <see cref="CSVersion.OrderedVersion"/> left shifted by 1 bit with 
        /// the less significant bit set to 0 for release and 1 CI builds (when a <see cref="CIBuildDescriptor"/> is provided).
        /// </summary>
        FileVersion,

        /// <summary>
        /// Long form. This format is the original one that was supposed to be "best" representation.
        /// It appeared that this long form was less readable than the short one (the <see cref="Normalized"/> format).
        /// Since version 6.0.0 this long form is no more the default one.
        /// </summary>
        LongForm,
    }
}
