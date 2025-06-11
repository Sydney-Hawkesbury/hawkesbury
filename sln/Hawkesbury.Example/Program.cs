using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hawkesbury.Core.Text;

namespace Hawkesbury
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, object> ex = new Dictionary<string, object>();
            StringExpander.ExpandNamed fm = (k, f, o) => "Hallo";
            ex.Add("BLA", fm);
            Console.OutputEncoding = Encoding.Unicode;
            Console.WriteLine("Hawkesbury Example");
            Console.WriteLine("Date: '{DATETIME-1}' is '{UDATETIME-1}' | '{0,5:x}' | '{GUID-2}' '{GUID-2}'".Expand(42, new DateTime(2000,1,1), Guid.Empty));
            Console.WriteLine("'{BLA}'".Expand(ex));
            Console.WriteLine("'{ENV,20:COMPUTERNAME}' {RANDOM} {RANDOM:x} [#u{c3bf2012}]".Expand());
            Console.WriteLine("F: '{FILE-0:BASENAME}' | {0}".Expand(new FileInfo(@"C:\Windows\explorer.exe")));
            Console.ReadKey();
        }
    }
}
