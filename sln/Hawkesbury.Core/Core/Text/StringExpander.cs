using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Schema;
using Microsoft.SqlServer.Server;

namespace Hawkesbury.Core.Text
{
    public static class StringExpander
    {
        #region Definitions

        public delegate string ExpandNamed(string name, string format, object obj);

        #endregion

        #region Properties

        private static readonly Regex ReNamed = new Regex(@"\{(?<key>[a-zA-Z][a-zA-Z0-9_]*)(?:-(?<index>\d+))?(?:,(?<width>-?\d*))?(?:,(?<maxwidth>\d*))?(?::(?<format>[^\}]+))?\}");
        private static readonly Regex ReIndexed = new Regex(@"\{(?<index>\d+)(?:,(?<width>-?\d*))?(?:,(?<maxwidth>\d*))?(?::(?<format>[^\}]+))?\}");
        private static readonly Regex ReHash = new Regex(@"#(?<code>[uU])?(?:(?<count>\d+)\.)?(?<hex>(?:[0-9a-fA-F]{2})+)");
        private static readonly Regex ReIsHex = new Regex(@"^[0-9a-fA-F\s]+$");
        private static readonly Regex ReIsHexOnly = new Regex(@"^[0-9a-fA-F]+$");
        private static readonly Regex ReExtractHex = new Regex(@"([0-9a-fA-F]+)");

        private static readonly string[] DateTimeKeys = new string[] { "DATE", "TIME", "DATETIME", "UDATE", "UTIME", "UDATETIME" };

        private static readonly Lazy<Dictionary<string, object>> _Expanders = new Lazy<Dictionary<string, object>>(CreateExpanders, true);
        private static readonly Lazy<Random> _Random = new Lazy<Random>(() => new Random(), true);

        #endregion

        #region Methods

        private static int? StringToInt(string s)
        {
            int i;
            if (int.TryParse(s, out i)) return i;
            return null;
        }

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

            ExpandNamed eenv = (k, f, o) => Environment.GetEnvironmentVariable(f);
            rslt["ENV"] = eenv;

            ExpandNamed ernd = (k, f, o) => ((int)o).ToString(f);
            rslt["RANDOM"] = ernd;

            ExpandNamed efl = ExpandFilename;
            rslt["FILE"] = efl;

            return rslt;
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

        private static string GetInnerFormatString(string format) => $"{{0{(!string.IsNullOrEmpty(format) ? ":" + format : "")}}}";

        public static void SetStatic(string key, object value)
        {
            if (value == null && _Expanders.Value.ContainsKey(key))
                _Expanders.Value.Remove(key);
            if (value != null) _Expanders.Value[key] = value;
        }

        public static void SetStatic(string key, ExpandNamed value) => SetStatic(key, (object)value);

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

        #region Extensions

        public static string Expand(this string me, Dictionary<string, object> namedValues, params object[] objects)
        {
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
            foreach (var match in ReHash.Matches(rslt).OfType<Match>())
            {
                var code = match.Groups["code"].Value;
                var factor = "u".Equals(code, StringComparison.CurrentCultureIgnoreCase) ? 4 : 2;
                var count = (StringToInt(match.Groups["count"].Value) ?? 1) * factor;
                var hex = match.Groups["hex"].Value;
                if (hex.Length == count)
                {
                    rslt = rslt.Replace(match.Value, ByteArrayToString(code, FromHexStringToByteArray(hex)));
                }
                else
                {
                    var i = rslt.IndexOf(match.Value);
                    if (i >= 0)
                    {
                        var a = rslt.Substring(0, i);
                        var b = ByteArrayToString(code, FromHexStringToByteArray(hex.Substring(0, count)));
                        var cntlen = match.Groups["count"].Value.Length;
                        if (cntlen > 0) cntlen++;
                        var c = rslt.Substring(i + 1 + cntlen + code.Length + count);
                        rslt = a + b + c;
                    }
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

        public static string Expand(this string me, params object[] objects) => Expand(me, null, objects);

        public static bool IsHex(this string me, bool allowWhiteSpace = true)
            => allowWhiteSpace ? ReIsHex.IsMatch(me) : ReIsHexOnly.IsMatch(me);

        public static byte[] FromHexStringToByteArray(this string me)
        {
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

        #endregion
    }
}
