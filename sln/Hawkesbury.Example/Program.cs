using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
            for (int i = 21; i < 40; i++) a(i);

            Console.WriteLine(new String('=', Console.WindowWidth - 1));
            Console.WriteLine("Press any key");
            Console.ReadKey();
        }

        static void a(int num)
        {
            Console.WriteLine("Vor Thread Start {0}", num);
            Thread t = new Thread(() =>
            {
                Console.WriteLine("T {0}: Thread gestartet", num);
                Console.WriteLine("T {0}: {1}", num, AppSettingsSimple.Instance.Path);
                AppSettingsSimple.Instance.SimpleSettings["Hallo"] = "ballo";
                Console.WriteLine("T {0}: Removing asdf{0}", num);
                AppSettingsSimple.Instance.SimpleSettings.Remove("asdf" + num.ToString());
                Thread.Sleep(2000);
                Console.WriteLine("T {0}: {1}", num, AppSettingsSimple.Instance.SimpleSettings["asdf" + num.ToString(), "???"]);
            });
            t.Start();
            Thread.Sleep(500);
            Console.WriteLine("Nach Thread Start {0}", num);
            Console.WriteLine(AppSettingsSimple.Instance.SimpleSettings["Hallo"]);
            AppSettingsSimple.Instance.SimpleSettings["asdf"] = "jklö";
            Console.WriteLine("T {0}: Adding asdf{0}", num);
            AppSettingsSimple.Instance.SimpleSettings["asdf" + num.ToString()] = "qwerty" + num.ToString();
            t.Join();
            Console.WriteLine("Thread {0} beendet", num);

        }
    }
}
