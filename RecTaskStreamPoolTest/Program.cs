using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WebTvTest.RecTask;

namespace RecTaskStreamPoolTest
{
    class Program
    {
        static void Main(string[] args)
        {
            uint t = 1;
            if (args.Length == 1)
            {
                uint.TryParse(args[0], out t);
                
            }
            Console.Error.WriteLine("TaskID {0}", t);
            StreamPool p = new StreamPool(t);
            byte[] buf = new byte[188 * 4096];
            ulong offset = 0;
            Stream o = Console.OpenStandardOutput();
            while (true)
            {
                int s = p.Read(buf, 4096, ref offset);
                o.Write(buf, 0, s);
                o.Flush();
            }
        }
    }
}
