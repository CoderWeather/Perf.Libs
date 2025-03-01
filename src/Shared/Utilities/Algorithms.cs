namespace Utilities;

public static class Algorithms {
    public static bool LuhnValidation(ReadOnlySpan<char> span) {
        if (span.Length <= 1) {
            return false;
        }

        var validationDigit = span[^1] - '0';
        span = span[..^1];
        var sum = 0;
        for (var i = 0; i < span.Length; i++) {
            var ch = span[i];
            var d = ch - '0';
            if (i % 2 is not 0) {
                d = d * 2;
                if (d > 9) {
                    d = d - 9;
                }
            }

            sum += d;
        }

        var calculatedValidationDigit = (10 - sum % 10) % 10;

        return calculatedValidationDigit == validationDigit;
    }
}
