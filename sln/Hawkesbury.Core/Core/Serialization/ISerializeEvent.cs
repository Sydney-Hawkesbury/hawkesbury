using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Hawkesbury.Core.Collections.Generic;

namespace Hawkesbury.Core.Serialization
{

    public delegate void SerializeEvent<Tsrc, TSerialized>(object sender, EventArgs<Tsrc, TSerialized> e);
    public interface ISerializeEvent<Tsrc, TSerialized> : ISerializable
    {
        event SerializeEvent<Tsrc, TSerialized> OnSerialize;
    }
    public interface IDeserializeEvent<TSerialized, Tout> : ISerializable
    {
        event SerializeEvent<TSerialized, Tout> OnDeserialize;
    }
}
