using NUnitLite;
using System.Reflection;

namespace CSemVer.NetCore.Tests
{
    public static class Program
    {
        public static int Main( string[] args )
        {
            return new AutoRun( Assembly.GetEntryAssembly() ).Execute( args );
        }
    }
}
