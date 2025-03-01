namespace Perf.Holders.Generator;

using Microsoft.CodeAnalysis.CSharp;

readonly record struct CompInfo(LanguageVersion? Version);

readonly record struct BasicHolderContextInfo(
    string MinimalNameWithGenericMetadata,
    Dictionary<string, string?> PatternValues
) {
    public bool Equals(BasicHolderContextInfo other) {
        if (PatternValues == null! || other.PatternValues == null!) {
            return false;
        }

        if (other.PatternValues.Count != PatternValues.Count) {
            return false;
        }

        if (MinimalNameWithGenericMetadata == null!
            || other.MinimalNameWithGenericMetadata == null!
            || MinimalNameWithGenericMetadata.Equals(other.MinimalNameWithGenericMetadata) is false) {
            return false;
        }

        var otherPv = other.PatternValues;
        foreach (var p in PatternValues) {
            var k = p.Key;
            var v = p.Value;
            if (otherPv.TryGetValue(k, out var otherValue) is false || otherValue?.Equals(v) is false) {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode() {
        if (MinimalNameWithGenericMetadata == null!) {
            return 0;
        }

        HashCode hc = default;
        hc.Add(MinimalNameWithGenericMetadata);
        foreach (var p in PatternValues) {
            hc.Add(p.Key);
            hc.Add(p.Value);
        }

        return hc.ToHashCode();
    }
}
