using System;
using System.Collections.Generic;
using System.Text;

namespace CSemVer
{
    public partial class SVersionBound
    {
        static ref ReadOnlySpan<char> Trim( ref ReadOnlySpan<char> s ) { s = s.TrimStart(); return ref s; }

        static bool TryMatch( ref ReadOnlySpan<char> s, char c )
        {
            if( s[0] == c )
            {
                s = s.Slice( 1 );
                return true;
            }
            return false;
        }

        static bool TryMatchNonNegativeInt( ref ReadOnlySpan<char> s, out int i )
        {
            i = 0;
            int v = s[0] - '0';
            if( v >= 0 && v <= 9 )
            {
                do
                {
                    i = i * 10 + v;
                    if( s.Length == 0 ) break;
                    s = s.Slice( 1 );
                    v = s[0] - '0';
                }
                while( v >= 0 && v <= 9 );
                return true;
            }
            return false;
        }

        static bool TryMatchXStarInt( ref ReadOnlySpan<char> s, out int i )
        {
            if( s[0] == '*' || s[0] == 'x' || s[0] == 'X' )
            {
                s = s.Slice( 1 );
                i = -1;
                return true;
            }
            return TryMatchNonNegativeInt( ref s, out i );
        }

    }
}
