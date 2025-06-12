using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Hawkesbury.Core;
using Hawkesbury.Core.ComponentModel;
using Hawkesbury.Core.Settings;
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
            Console.WriteLine("Date: '{DATETIME-1}' is '{UDATETIME-1}' | '{0,5:x}' | '{GUID}' '{GUID-2}'".Expand(42, new DateTime(2000, 1, 1), Guid.Empty));
            Console.WriteLine("'{BLA} {BLU}' [{REPEAT-0:10}]".Expand(ex, "sq"));
            Console.WriteLine("'{ENV-0:LCASE}' {RANDOM} {RANDOM:x} [#u{c3bf2012}]".Expand("COMPUTERNAME"));
            Console.WriteLine("F: '{FILE-0:BASENAME}' | {0}".Expand(new FileInfo(@"C:\Windows\explorer.exe")));
            Console.WriteLine("{TEXT-0} {TEXT-0:UCASE} {TEXT-1:LCASE}".Expand("hallo", "BALLO"));
            Console.WriteLine("{COUNTER} {COUNTER-2} {COUNTER} ".Expand("hallo", "BALLO", -42));

            AppSettingsSimple appSettings1 = AppSettingsSimple.GetInstance();
            appSettings1.MostResecentlyUsedFiles.LastValue = "asdf";
            appSettings1.MostResecentlyUsedFiles.LastValue = "null";
            appSettings1.MostResecentlyUsedFiles.LastValue = null;
            appSettings1.Example1 = "Hallo1";
            appSettings1.Example2 = "Ballo";
            appSettings1.SimpleSettings = null;
             Console.ReadKey();
        }
    }
}
