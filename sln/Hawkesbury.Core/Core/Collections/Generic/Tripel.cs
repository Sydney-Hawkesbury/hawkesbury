using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Hawkesbury.Core.Collections.Generic
{
    public class Tripel<T1, T2, T3> : Pair<T1, T2>, IEquatable<Tripel<T1, T2, T3>>
    {
        #region Properties

        [XmlAttribute]
        public T3 Item3 { get; set; }

        #endregion

        #region Constructors

        public Tripel() { }

        public Tripel(T1 item1, T2 item2, T3 item3) : base(item1, item2)
        {
            Item3 = item3;
        }

        #endregion

        #region Methods

        public bool Equals(Tripel<T1, T2, T3> other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Objects.IsNull(Item1, Item2, other.Item1, other.Item2)) return true;
            return (Item1?.Equals(other.Item1) ?? false) && (Item2?.Equals(other.Item2) ?? false);
        }

        #endregion
    }
}
