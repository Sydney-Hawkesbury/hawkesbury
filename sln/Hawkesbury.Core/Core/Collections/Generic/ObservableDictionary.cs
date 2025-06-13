using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Hawkesbury.Core.Linq;

namespace Hawkesbury.Core.Collections.Generic
{
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, IXmlSerializable
    {
        #region Definitions

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event EventHandler<SerialzeDictionaryEntryEventArgs<TKey, TValue>> SerializeDictionaryEntry;
        public static event EventHandler<DeserialzeDictionaryEntryEventArgs<TKey, TValue>> DeserializeDictionaryEntry;

        #endregion

        #region Properties

        private Dictionary<TKey, TValue> _InnerDictionary;

        public TValue this[TKey key] { get => _InnerDictionary[key]; set => Set(key, value); }
        public TValue this[TKey key, TValue defaultValue] => _InnerDictionary.ContainsKey(key) ? _InnerDictionary[key] : defaultValue;

        public ICollection<TKey> Keys => _InnerDictionary.Keys;

        public ICollection<TValue> Values => _InnerDictionary.Values;

        public int Count => _InnerDictionary.Count;

        public bool IsReadOnly => false;

        #endregion

        #region Constructors

        public ObservableDictionary()
        {
            _InnerDictionary = new Dictionary<TKey, TValue>();
        }

        #endregion

        #region Methods

        private void RaiseNotifyCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null) CollectionChanged(this, e);
        }

        public void Set(TKey key, TValue value)
        {
            if (_InnerDictionary.ContainsKey(key))
            {
                var oldValue = _InnerDictionary.First(kvp => kvp.Key.Equals(key));
                _InnerDictionary[key] = value;
                var newValue = _InnerDictionary.First(kvp => kvp.Key.Equals(key));
                NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, oldValue, newValue);
                RaiseNotifyCollectionChanged(e);
            }
            else
            {
                Add(key, value);
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null) return;
            _InnerDictionary.Add(key, value);
            var newKvp = _InnerDictionary.FirstOrDefault(kvp => kvp.Key.Equals(key));
            NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newKvp);
        }

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            _InnerDictionary.Clear();
            NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            RaiseNotifyCollectionChanged(e);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) => _InnerDictionary.Contains(item);

        public bool ContainsKey(TKey key) => _InnerDictionary.ContainsKey(key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0 || arrayIndex > array.Length) throw new IndexOutOfRangeException();

            if (array.Length - arrayIndex < Count) throw new ArgumentException();

            foreach (var kvp in _InnerDictionary) array[arrayIndex++] = kvp;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _InnerDictionary.GetEnumerator();

        public bool Remove(TKey key)
        {
            var item = _InnerDictionary.FirstOrDefault(kvp => kvp.Key.Equals(key));
            bool removed = _InnerDictionary.Remove(key);
            if (removed)
            {
                NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item);
                RaiseNotifyCollectionChanged(e);
            }
            return removed;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!_InnerDictionary.ContainsKey(key))
            {
                value = default;
                return false;
            }
            value = _InnerDictionary[key];
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            var n = reader.Name;
            while (reader.Read())
            {
                if (reader.Name == "Item")
                {
                    string skey = reader.GetAttribute("key");
                    string svalue = reader.GetAttribute("value");
                    if (DeserializeDictionaryEntry != null)
                    {
                        DeserialzeDictionaryEntryEventArgs<TKey, TValue> e = new DeserialzeDictionaryEntryEventArgs<TKey, TValue>(skey, svalue);
                        DeserializeDictionaryEntry(this, e);
                        if (e.Key != null) this[e.Key] = e.Value;
                    }
                    else
                    {
                        TKey key = Convert<TKey>(skey);
                        TValue value = Convert<TValue>(svalue);
                        this[key] = value;
                    }
                }
                else
                {
                    break;
                }
            }
            if (n == reader.Name) reader.Read();
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            _InnerDictionary.ForEach(kvp =>
            {
                string key = null, value = null;
                if (SerializeDictionaryEntry != null)
                {
                    SerialzeDictionaryEntryEventArgs<TKey, TValue> e = new SerialzeDictionaryEntryEventArgs<TKey, TValue>(kvp);
                    SerializeDictionaryEntry(this, e);
                    key = e.SerializedKey;
                    value = e.SerializedValue;
                }

                writer.WriteStartElement("Item");
                writer.WriteAttributeString("key", key ?? kvp.Key.ToString());
                writer.WriteAttributeString("value", value ?? kvp.Value.ToString());
                writer.WriteEndElement();
            });
        }

        private static T Convert<T>(string value)
        {
            if (value is T v) return v;
            return (T)System.Convert.ChangeType(value, typeof(T));
        }

        #endregion
    }

    public class SerialzeDictionaryEntryEventArgs<TKey, TValue> : EventArgs
    {
        public TKey Key { get; }
        public TValue Value { get; }

        public string SerializedKey { get; set; }

        public string SerializedValue { get; set; }

        public SerialzeDictionaryEntryEventArgs(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public SerialzeDictionaryEntryEventArgs(KeyValuePair<TKey, TValue> kvp) : this(kvp.Key, kvp.Value) { }
    }

    public class DeserialzeDictionaryEntryEventArgs<TKey, TValue> : EventArgs
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }

        public string SerializedKey { get; }

        public string SerializedValue { get; }

        public DeserialzeDictionaryEntryEventArgs(string serializedKey, string serializedValue)
        {
            SerializedKey = serializedKey;
            SerializedValue = serializedValue;
        }
    }
}
