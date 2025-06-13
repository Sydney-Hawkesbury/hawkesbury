using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hawkesbury.Core
{
    public static class Objects
    {
        /// <summary>
        /// Checks if all parameters are null
        /// </summary>
        /// <param name="objs"></param>
        /// <returns></returns>
        public static bool IsNull(params object[] objs) => objs?.All(o => o is null) ?? true;

        public static T[] AsArray<T>(params T[] values) => values;
    }
}
