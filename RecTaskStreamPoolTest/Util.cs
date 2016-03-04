using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WebTvTest
{
    public class Util
    {
        public static byte[] ReadStream(Stream stream, int size)
        {
            var buffer = new byte[size];
            for (int l = 0; l < buffer.Length; )
            {
                int s = stream.Read(buffer, l, buffer.Length - l);
                if (s <= 0) { return null; }
                l += s;
            }
            return buffer;
        }
        public static int ReadStream(Stream stream, byte[] buffer, int offset, int size)
        {
            int l = 0;
            for (l = 0; l < buffer.Length; )
            {
                int s = stream.Read(buffer, l + offset, buffer.Length - l);
                if (s <= 0) { return l; }
                l += s;
            }
            return l;
        }
        public static string RemoveStartSpace(string input)
        {
            int Pos = 0;
            while ((Pos < input.Length) && (input[Pos] == ' ')) Pos++;
            return input.Substring(Pos);
        }
    }
}
