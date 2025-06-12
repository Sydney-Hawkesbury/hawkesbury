using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Hawkesbury.Core.ComponentModel;
using Hawkesbury.Core.Linq;

namespace Hawkesbury.Core.Collections.Generic
{
    public class MruCollection<T> : INotifyPropertyChanged where T : IEquatable<T>
    {
        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        private int _MaxValues;
        public int MaxValues { get => Math.Max(_MaxValues, 1); set => this.SetPropertyValue(ref _MaxValues, Math.Max(value, 1), nameof(MaxValues)); }

        private ObservableCollection<T> _Values;
        public ObservableCollection<T> Values
        {
            get { if (_Values == null) SetList(new ObservableCollection<T>()); return _Values; }
            set => SetList(value);
        }

        public int Count => _Values?.Count ?? 0;

        [XmlIgnore]
        public T LastValue
        {
            get => _Values != null && _Values.Count > 0 ? _Values.First() : default(T);
            set
            {
                var last = LastValue;
                if (this.SetPropertyValue(ref last, value, nameof(LastValue)))
                {
                    if (_Values != null)
                    {
                        if (_Values.Contains(value)) _Values.Remove(value);
                        while (_Values.Count >= MaxValues) _Values.RemoveAt(_Values.Count - 1);
                        if ((value as object) != null) _Values.Insert(0, value);
                    }
                }
            }
        }

        #endregion

        #region Constructors -------------------------------------------------

        public MruCollection(int maxValues)
        {
            MaxValues = maxValues;
        }

        public MruCollection() : this(15) { }

        #endregion

        #region Methods

        private void SetList(ObservableCollection<T> values)
        {
            var oldValues = _Values;
            if (this.SetPropertyValue(ref _Values, values, nameof(Values)))
            {
                oldValues?.OfType<INotifyPropertyChanged>().ForEach(itm => itm.PropertyChanged -= Item_PropertyChanged);
                values?.OfType<INotifyPropertyChanged>().ForEach(itm => itm.PropertyChanged += Item_PropertyChanged);
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e) => this.RaisePropertyChanged(nameof(Values));

        public void Clear() => _Values?.Clear();
        public bool Remove(T item) => _Values?.Remove(item) ?? false;

        #endregion
    }
}
