﻿namespace PerfXml.Internal;

static class EnumCache {
    public static string GetName<T>(T value)
        where T : struct, Enum {
        return Cache<T>.ByValue(value);
    }

    public static T ByName<T>(string s)
        where T : struct, Enum {
        return Cache<T>.ByName(s);
    }

    static class Cache<T>
        where T : struct, Enum {
        static readonly Dictionary<string, T> ByNames = new(StringComparer.InvariantCultureIgnoreCase);
        static readonly Dictionary<T, string> ByValues = new();

        public static T ByName(string s) {
            if (ByNames.TryGetValue(s, out var t)) {
                return t;
            }

            ByNames[s] = t = Enum.Parse<T>(s, true);
            ByValues[t] = s;
            return t;
        }

        public static string ByValue(T value) {
            if (ByValues.TryGetValue(value, out var name)) {
                return name;
            }

            ByValues[value] = name = Enum.GetName(typeof(T), value)!;
            ByNames[name] = value;
            return name!;
        }
    }
}
