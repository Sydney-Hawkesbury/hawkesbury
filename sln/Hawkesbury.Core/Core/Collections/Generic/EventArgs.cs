using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hawkesbury.Core.Collections.Generic
{
    public class EventArgs<T> : EventArgs
    {
        public T Value { get; }

        public EventArgs(T value)
        {
            Value = value;
        }
    }

    public class EventArgs<TIn, TOut> : EventArgs<TIn>
    {
        public TOut OutValue { get; set; }

        public EventArgs(TIn inValue) : base(inValue)
        {

        }
    }
}
