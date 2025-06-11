using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hawkesbury.Core.Text
{
    /// <summary>
    /// This class provides extended string formatting methods and other string extensions
    /// <list>
    /// <item>+ named placehoders "{NAME[-INDEX][,width][,maxwidth][:FORMAT]}"</item>
    /// <item>+ extended indexed placehoders "{INDEX[,width][,maxwidth][:FORMAT]}"</item>
    /// <item>
    /// + hash encoded byte array expansion for utf-8 and utf-16 encoding "#hh or #uhhhh or #Uhhhh" or #{hh...} or #u{hhhh...} or #U{hhhh...}<para/>
    /// u is for utf-16-le, U is for utf-16-be
    /// </item>
    /// </list>
    /// Default names:
    /// <list>
    /// <item>+ DATE - formats local date as 'yyyy-MM-dd' (no index => current date)</item>
    /// <item>+ TIME - formats local date as 'HH:mm:ss' (no index => current time)</item>
    /// <item>+ DATETIME - formats local date as 'yyyy-MM-dd HH:mm:ss' (no index => current date and time)</item>
    /// <item>+ UDATE - formats UTC date as 'yyyy-MM-dd' (no index => current date)</item>
    /// <item>+ UTIME - formats UTC date as 'HH:mm:ss' (no index => current time)</item>
    /// <item>+ UDATETIME - formats UTC date as 'yyyy-MM-ddTHH:mm:ssZ' (no index => current date and time)</item>
    /// <item>+ GUID (no index => new GUID)</item>
    /// <item>+ RANDOM (no index => new random number)</item>
    /// <item>+ COUNTER (no index => new number [inner counter])</item>
    /// <item>+ FILE - formats a file path: BASENAME, SHORTNAME, EXTENSION, DIRECTORY, ROOT</item>
    /// <item>+ TEXT - formats a string LCASE or UCASE (no index => FORMAT)</item>
    /// <item>+ ENV - expands an environment variable, like TEXT (no index => FORMAT is variable name)</item>
    /// <item>+ REPEAT - repeats a string, count is given as FORMAT (no FORMAT => count = 1)</item>
    /// </list>
    /// </summary>
    public static class StringExpander
    {
        #region Definitions

        /// <summary>
        /// Delegate for named pattern expansion methods
        /// </summary>
        /// <param name="name">pattern name</param>
        /// <param name="format">pattern format string</param>
        /// <param name="obj">object to format</param>
        /// <returns>as string formatted object</returns>
        public delegate string ExpandNamed(string name, string format, object obj);

        #endregion

        #region Properties

        private static readonly Regex ReNamed = new Regex(@"\{(?<key>[a-zA-Z][a-zA-Z0-9_]*)(?:-(?<index>\d+))?(?:,(?<width>-?\d*))?(?:,(?<maxwidth>\d*))?(?::(?<format>[^\}]+))?\}");

        private static readonly Regex ReIndexed = new Regex(@"\{(?<index>\d+)(?:,(?<width>-?\d*))?(?:,(?<maxwidth>\d*))?(?::(?<format>[^\}]+))?\}");

        private static readonly Regex ReHashed1 = new Regex(@"#(?<code>u|U)?(?<hex>(?:(?<=u|U)[0-9a-fA-F]{2})?[0-9a-fA-F]{2})");
        private static readonly Regex ReHashed2 = new Regex(@"#(?<par>\{)(?<hex>(?:[0-9a-fA-F]{2})+)\}");
        private static readonly Regex ReHashed3 = new Regex(@"#(?<code>u|U)(?<par>\{)(?<hex>(?:[0-9a-fA-F]{4})+)\}");

        private static readonly Regex ReIsHex = new Regex(@"^[0-9a-fA-F\s]+$");
        private static readonly Regex ReIsHexOnly = new Regex(@"^[0-9a-fA-F]+$");
        private static readonly Regex ReExtractHex = new Regex(@"([0-9a-fA-F]+)");

        private static readonly Lazy<Dictionary<string, object>> _Expanders = new Lazy<Dictionary<string, object>>(CreateExpanders, true);
        private static readonly Lazy<Random> _Random = new Lazy<Random>(() => new Random(), true);

        private static readonly object _CounterLock = new object();
        private static Int64 _Counter = 0;
        public static Int64 Counter
        {
            get
            {
                lock (_CounterLock)
                {
                    return _Counter++;
                }
            }
            set
            {
                lock (_CounterLock)
                {
                    _Counter = value;
                }
            }
        }

        #endregion

        #region private Methods

        private static Dictionary<string, object> CreateExpanders()
        {
            Dictionary<string, object> rslt = new Dictionary<string, object>();

            ExpandNamed edt = ExpandDateTime;
            rslt["DATE"] = edt;
            rslt["TIME"] = edt;
            rslt["DATETIME"] = edt;
            rslt["UDATE"] = edt;
            rslt["UTIME"] = edt;
            rslt["UDATETIME"] = edt;

            ExpandNamed eguid = (k, f, o) => ((Guid)o).ToString(f);
            rslt["GUID"] = eguid;

            ExpandNamed eenv = ExpandEnv;
            rslt["ENV"] = eenv;

            ExpandNamed ernd = (k, f, o) => Convert.ToInt32(o).ToString(f);
            rslt["RANDOM"] = ernd;

            ExpandNamed efl = ExpandFilename;
            rslt["FILE"] = efl;

            ExpandNamed erpt = ExpandRepeat;
            rslt["REPEAT"] = erpt;

            ExpandNamed etxt = ExpandText;
            rslt["TEXT"] = etxt;

            ExpandNamed ecnt = (k, f, o) =>
            {
                if (o is int i) Counter = i;
                return Counter.ToString(f);
            };
            rslt["COUNTER"] = ecnt;

            return rslt;
        }

        private static int? StringToInt(string s)
        {
            int i;
            if (int.TryParse(s, out i)) return i;
            return null;
        }

        private static string AdjustWidth(string s, int? width, uint? maxwidth)
        {
            if (width.HasValue && Math.Abs(width.Value) > s.Length)
            {
                if (width > 0) s = $"{new string(' ', width.Value - s.Length)}{s}";
                else s = $"{s}{new string(' ', -width.Value - s.Length)}";
            }
            if (maxwidth.HasValue && maxwidth > 0 && maxwidth < s.Length)
            {
                s = s.Substring(0, (int)maxwidth.Value);
            }
            return s;
        }

        private static string ExpandDateTime(string name, string format, object dateobj)
        {
            if (dateobj is DateTime dt)
            {
                if (string.IsNullOrEmpty(format))
                {
                    switch (name)
                    {
                        case "DATE": format = "yyyy-MM-dd"; break;
                        case "TIME": format = "HH:mm:ss"; break;
                        case "DATETIME": format = "yyyy-MM-dd HH:mm:ss"; break;
                        case "UDATE": format = "yyyy-MM-dd"; break;
                        case "UTIME": format = "HH:mm:ss"; break;
                        case "UDATETIME": format = "yyyy-MM-ddTHH:mm:ssZ"; break;
                    }
                }
                return name.StartsWith("U") ? dt.ToUniversalTime().ToString(format) : dt.ToLocalTime().ToString(format);
            }
            return name;
        }

        private static string ExpandFilename(string name, string format, object file)
        {
            string filename = file.ToString();
            switch (format)
            {
                case "BASENAME": return Path.GetFileNameWithoutExtension(filename);
                case "SHORTNAME": return Path.GetFileName(filename);
                case "EXTENSION": return Path.GetExtension(filename).TrimStart('.');
                case "DIRECTORY": return Path.GetDirectoryName(filename);
                case "ROOT": return Path.GetPathRoot(filename);
                default: return filename;
            }
        }

        private static string ExpandRepeat(string name, string format, object value)
        {
            if (value == null) return format ?? string.Empty;
            int i;
            if (!Int32.TryParse(format, out i)) i = 1;
            if (i < 1) return string.Empty;
            if (i == 1) return value.ToString();

            if (value is char c) return new string(c, i);
            string s = value.ToString();
            if (s.Length == 1) return new string(s[0], i);
            StringBuilder sb = new StringBuilder(s);
            while (--i > 0) sb.Append(s);
            return sb.ToString();
        }

        private static string ExpandText(string name, string format, object text)
        {
            if (text == null) return format ?? string.Empty;
            switch (format)
            {
                case "LCASE": return text.ToString().ToLower();
                case "UCASE": return text.ToString().ToUpper();
                default: return text.ToString();
            }
        }

        private static string ExpandEnv(string name, string format, object varname)
        {
            if (varname == null) return Environment.GetEnvironmentVariable(format ?? string.Empty);
            return ExpandText("TEXT", format, Environment.GetEnvironmentVariable(varname.ToString()));
        }

        private static string GetInnerFormatString(string format) => $"{{0{(!string.IsNullOrEmpty(format) ? ":" + format : "")}}}";

        private static string ByteArrayToString(string code, byte[] bytes)
        {
            switch (code)
            {
                case "u": return Encoding.Unicode.GetString(bytes);
                case "U": return Encoding.BigEndianUnicode.GetString(bytes);
                default: return Encoding.UTF8.GetString(bytes);
            }
        }

        #endregion

        #region public Methods

        /// <summary>
        /// Sets a static object for named expansion
        /// </summary>
        /// <param name="key">pattern name</param>
        /// <param name="value">value to use for named pattern</param>
        public static void SetStatic(string key, object value)
        {
            if (value == null && _Expanders.Value.ContainsKey(key))
                _Expanders.Value.Remove(key);
            if (value != null) _Expanders.Value[key] = value;
        }

        /// <summary>
        /// Sets an expansion method for named pattern
        /// </summary>
        /// <param name="key">pattern name</param>
        /// <param name="value">method to expand an object</param>
        public static void SetStatic(string key, ExpandNamed value) => SetStatic(key, (object)value);


        #endregion

        #region Extensions

        /// <summary>
        /// Expands a string
        /// </summary>
        /// <param name="me">string to expand</param>
        /// <param name="namedValues">optional additional values for named patterns</param>
        /// <param name="objects">array of objects to use in indexed patterns</param>
        /// <returns>null or expanded string</returns>
        public static string Expand(this string me, Dictionary<string, object> namedValues, params object[] objects)
        {
            if (string.IsNullOrWhiteSpace(me)) return me;

            string rslt = me;
            DateTime dt = DateTime.UtcNow;
            foreach (var match in ReNamed.Matches(me).OfType<Match>())
            {
                var key = match.Groups["key"].Value;
                var index = StringToInt(match.Groups["index"].Value);
                var width = StringToInt(match.Groups["width"].Value);
                var maxwidth = StringToInt(match.Groups["maxwidth"].Value);
                var format = match.Groups["format"].Value;
                object expandValue = null;
                object value = null;
                if (index.HasValue && objects != null && index >= 0 && index < objects.Length)
                    value = objects[index.Value];
                if (namedValues?.ContainsKey(key) ?? false)
                    expandValue = namedValues[key];
                else if (_Expanders.Value.ContainsKey(key))
                    expandValue = _Expanders.Value[key];
                if (expandValue != null)
                {
                    if (expandValue is ExpandNamed expandNamed)
                    {
                        var i = rslt.IndexOf(match.Value);
                        if (i >= 0)
                        {
                            Func<string, object, string> rpl = (input, val) =>
                            {
                                var a1 = input.Substring(0, i);
                                var b1 = AdjustWidth(expandNamed(key, format, val), width, (uint?)maxwidth);
                                var c1 = input.Substring(i + match.Value.Length);
                                return a1 + b1 + c1;
                            };
                            switch (key)
                            {
                                case "DATE":
                                case "TIME":
                                case "DATETIME":
                                case "UDATE":
                                case "UTIME":
                                case "UDATETIME":
                                    rslt = rslt.Replace(match.Value, AdjustWidth(expandNamed(key, format, value is DateTime d ? d : dt), width, (uint?)maxwidth));
                                    break;
                                case "GUID":
                                    rslt = rpl(rslt, value is Guid g ? g : Guid.NewGuid()); break;
                                case "RANDOM":
                                    rslt = rpl(rslt, value is int r ? r : _Random.Value.Next()); break;
                                case "COUNTER":
                                    rslt = rpl(rslt, value); break;
                                default:
                                    rslt = rslt.Replace(match.Value, AdjustWidth(expandNamed(key, format, value), width, (uint?)maxwidth)); break;
                            }
                        }
                    }
                    else
                    {
                        rslt = rslt.Replace(match.Value, AdjustWidth(string.Format(GetInnerFormatString(format), expandValue), width, (uint?)maxwidth));
                    }
                }
            }
            foreach (var match in ReHashed1.Matches(rslt).OfType<Match>().Concat(ReHashed2.Matches(rslt).OfType<Match>()).Concat(ReHashed3.Matches(rslt).OfType<Match>()))
            {
                var code = match.Groups["code"].Value;
                var hex = match.Groups["hex"].Value;
                var par = match.Groups["par"].Value;
                var i = rslt.IndexOf(match.Value);
                if (i >= 0)
                {
                    var a = rslt.Substring(0, i);
                    var b = ByteArrayToString(code, FromHexStringToByteArray(hex));
                    var c = rslt.Substring(i + 1 + 2 * par.Length + code.Length + hex.Length);
                    rslt = a + b + c;
                }
            }
            if (objects != null && objects.Length > 0)
            {
                foreach (var match in ReIndexed.Matches(me).OfType<Match>())
                {
                    var index = StringToInt(match.Groups["index"].Value).Value;
                    var width = StringToInt(match.Groups["width"].Value);
                    var maxwidth = StringToInt(match.Groups["maxwidth"].Value);
                    var format = match.Groups["format"].Value;
                    if (index >= 0 && index < objects.Length)
                    {
                        string fmt = GetInnerFormatString(format);
                        rslt = rslt.Replace(match.Value, AdjustWidth(string.Format(fmt, objects[index]), width, (uint?)maxwidth));
                    }
                }
            }
            return rslt;
        }

        /// <summary>
        /// Expands a string
        /// </summary>
        /// <param name="me">string to expand</param>
        /// <param name="objects">array of objects to use in indexed patterns</param>
        /// <returns>null or expanded string</returns>
        public static string Expand(this string me, params object[] objects) => Expand(me, null, objects);

        /// <summary>
        /// Tests if string contains only hex numbers
        /// </summary>
        /// <param name="me">string to test</param>
        /// <param name="allowWhiteSpace">also allow whitespaces</param>
        /// <returns></returns>
        public static bool IsHex(this string me, bool allowWhiteSpace = true)
            => string.IsNullOrWhiteSpace(me) ? false : allowWhiteSpace ? ReIsHex.IsMatch(me) : ReIsHexOnly.IsMatch(me);

        /// <summary>
        /// Converts a string of hex numbers to an array of bytes
        /// </summary>
        /// <param name="me">string to convert</param>
        /// <param name="allowWhiteSpace">also allow whitespaces</param>
        /// <returns>null or array of bytes</returns>
        public static byte[] FromHexStringToByteArray(this string me, bool allowWhiteSpace = true)
        {
            if (!me.IsHex(allowWhiteSpace)) return null;

            var hex = string.Join("", ReExtractHex.Matches(me).OfType<Match>());
            List<byte> result = new List<byte>();
            for (int i = 0; i < hex.Length; i += 2)
            {
                if (i + 2 <= hex.Length)
                    result.Add(Convert.ToByte(hex.Substring(i, 2), 16));
                else
                    result.Add(Convert.ToByte(hex.Substring(i, 1), 16));
            }
            return result.ToArray();
        }

        /// <summary>
        /// shortcut to string.Format(...)
        /// </summary>
        /// <param name="me">string to format</param>
        /// <param name="objects">parameters to use in format string</param>
        /// <returns>null or formatted string</returns>
        public static string Format(this string me, params object[] objects) => string.IsNullOrWhiteSpace(me) ? me : string.Format(me, objects);

        public static bool IsNullOrEmpty(this string me) => string.IsNullOrEmpty(me);
        public static bool IsNullOrWhiteSpace(this string me) => string.IsNullOrWhiteSpace(me);

        #endregion
    }
}
