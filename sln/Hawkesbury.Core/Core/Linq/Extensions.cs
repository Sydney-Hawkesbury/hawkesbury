using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hawkesbury.Core.Linq
{
    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> values, Action<T> action)
        {
            if (values == null || action == null) return;
            foreach (var item in values) action(item);
        }
    }
}
