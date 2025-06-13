using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Hawkesbury.Core.Serialization;

namespace Hawkesbury.Core.Xml.Serialization
{
    public interface IXmlSerializableSerializeEvent<Tin> : ISerializeEvent<Tin, string>, IXmlSerializable { }

    public interface IXmlSerializableDeserializeEvent<Tout> : IDeserializeEvent<string, Tout>, IXmlSerializable { }

    public interface XmlSerializableEvents<Tin, Tout> : IXmlSerializableSerializeEvent<Tin>, IXmlSerializableDeserializeEvent<Tout> { }
}
