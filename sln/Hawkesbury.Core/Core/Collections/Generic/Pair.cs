using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Hawkesbury.Core.Collections.Generic
{
    public class Pair<T1, T2> : IEquatable<Pair<T1, T2>>
    {
        #region Properties

        [XmlAttribute]
        public T1 Item1 { get; set; }

        [XmlAttribute]
        public T2 Item2 { get; set; }

        #endregion

        #region Constructors

        public Pair() { }

        public Pair(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        #endregion

        #region Methods

        public bool Equals(Pair<T1, T2> other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Objects.IsNull(Item1, Item2, other.Item1, other.Item2)) return true;
            return (Item1?.Equals(other.Item1) ?? false) && (Item2?.Equals(other.Item2) ?? false);
        }

        #endregion
    }
}
