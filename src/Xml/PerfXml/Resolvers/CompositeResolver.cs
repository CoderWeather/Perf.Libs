namespace PerfXml.Resolvers;

public sealed class StaticCompositeResolver : IXmlFormatterResolver {
    public static readonly StaticCompositeResolver Instance = new();

    readonly Dictionary<Type, IXmlFormatter> formatters = new();
    bool isStartedCaching;
    IXmlFormatterResolver[] resolvers = Array.Empty<IXmlFormatterResolver>();
    StaticCompositeResolver() { }

#region IXmlFormatterResolver Members

    public IXmlFormatter<T>? GetFormatter<T>() {
        return Cache<T>.Formatter;
    }

#endregion

    public StaticCompositeResolver WithFormatters(params IXmlFormatter[] formatters) {
        if (Instance.isStartedCaching) {
            throw new InvalidOperationException("Cannot add new formatters when resolver already being used");
        }

        Instance.formatters.Clear();
        foreach (var f in formatters) {
            Instance.formatters[f.Type()] = f;
        }

        return Instance;
    }

    public StaticCompositeResolver WithResolvers(params IXmlFormatterResolver[] resolvers) {
        if (Instance.isStartedCaching) {
            throw new InvalidOperationException("Cannot add new resolvers when resolver already being used");
        }

        if (resolvers == null) {
            throw new ArgumentNullException(nameof(resolvers));
        }

        Instance.resolvers = resolvers;

        return Instance;
    }

    public StaticCompositeResolver With(IXmlFormatter[] formatters, IXmlFormatterResolver[] resolvers) {
        if (Instance.isStartedCaching) {
            throw new InvalidOperationException(
                "Cannot add new formatters or resolvers when resolver already being used"
            );
        }

        WithFormatters(formatters);
        WithResolvers(resolvers);
        return Instance;
    }

    public void SetDefault() {
        Xml.DefaultResolver = Instance;
    }

#region Nested type: Cache

    static class Cache<T> {
        public static readonly IXmlFormatter<T>? Formatter;

        static Cache() {
            Instance.isStartedCaching = true;
            var type = typeof(T);

            if (Instance.formatters.TryGetValue(type, out var f)) {
                Formatter = (IXmlFormatter<T>?)f;
                return;
            }

            foreach (var r in Instance.resolvers) {
                if (r.GetFormatter<T>() is { } rf) {
                    Formatter = rf;
                    return;
                }
            }
        }
    }

#endregion
}
