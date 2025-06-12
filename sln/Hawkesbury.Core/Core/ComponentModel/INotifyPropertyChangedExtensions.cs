using System;
using System.ComponentModel;
using System.Reflection;
using Hawkesbury.Core.Linq;

namespace Hawkesbury.Core.ComponentModel
{
    public delegate bool EqualsMethod<T>(T x, T y);

    public static class INotifyPropertyChangedExtensions
    {
        #region Properties

        private const string PROPERTYCHANGED_EVENTNAME = nameof(INotifyPropertyChanged.PropertyChanged);
        private const BindingFlags EVENT_BINDING_FLAGS = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        #endregion

        #region Methods

        private static bool IsInitialized(object o)
        {
            var o2 = o as ISupportInitializeNotification;
            if (o2 != null) return o2.IsInitialized;
            return o != null;
        }

        private static bool IsEqual<T>(T x, T y, EqualsMethod<T> equalsMethod)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            if (equalsMethod != null) return equalsMethod(x, y);
            return x.Equals(y);
        }

        private static FieldInfo GetEventField(Type t, string eventName)
        {
            if (t == null) return null;
            FieldInfo eventField = t.GetField(eventName, EVENT_BINDING_FLAGS);
            if (eventField != null) return eventField;
            return GetEventField(t.BaseType, eventName);
        }

        /// <summary>
        /// Raises PropertyChanged-event for all given property names
        /// </summary>
        /// <param name="me"></param>
        /// <param name="propertyNames"></param>
        public static void RaisePropertyChanged(this INotifyPropertyChanged me, params string[] propertyNames)
        {
            if (me != null && propertyNames != null && propertyNames.Length > 0)
            {
                Type t = me.GetType();
                EventInfo eventInfo = t.GetEvent(PROPERTYCHANGED_EVENTNAME);
                var eventDelegate = GetEventField(t, PROPERTYCHANGED_EVENTNAME)?.GetValue(me) as PropertyChangedEventHandler;
                if (eventDelegate != null)
                    propertyNames.ForEach(propertyName => eventDelegate.Invoke(me, new PropertyChangedEventArgs(propertyName)));
            }
        }

        #endregion

        #region Extensions Methods

        /// <summary>
        /// Sets the new value for a property if changed and raises PropertyChanged event for all given names
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="me"></param>
        /// <param name="oldValue">old value</param>
        /// <param name="newValue">new value</param>
        /// <param name="equalsMethod">optional method to check equality</param>
        /// <param name="propertyNames">property names to raise event for</param>
        /// <returns>true if value has changed</returns>
        public static bool SetPropertyValue<T>(this INotifyPropertyChanged me, ref T oldValue, T newValue, EqualsMethod<T> equalsMethod, params string[] propertyNames)
        {
            if (IsEqual(oldValue, newValue, equalsMethod)) return false;
            oldValue = newValue;
            if (IsInitialized(me)) RaisePropertyChanged(me, propertyNames);
            return true;
        }

        /// <summary>
        /// Sets the new value for a property if changed and raises PropertyChanged event for all given names
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="me"></param>
        /// <param name="oldValue">old value</param>
        /// <param name="newValue">new value</param>
        /// <param name="propertyNames">property names to raise event for</param>
        /// <returns>true if value has changed</returns>
        public static bool SetPropertyValue<T>(this INotifyPropertyChanged me, ref T oldValue, T newValue, params string[] propertyNames)
            => SetPropertyValue(me, ref oldValue, newValue, null, propertyNames);

        #endregion
    }
}
