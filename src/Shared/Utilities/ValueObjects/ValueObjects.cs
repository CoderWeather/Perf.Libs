using System.Text.RegularExpressions;
using Perf.ValueObjects;

namespace Utilities.ValueObjects;

public readonly partial record struct PhoneNumber : IValidatableValueObject<long> {
	public const long MinValue = 100_000_00_00;
	public const long MaxValue = 999_999_99_99;
	public bool IsValid() => Validate(Value);
	public static bool Validate(long l) => l is >= MinValue and <= MaxValue;
}

public readonly partial record struct Email : IValidatableValueObject<string> {
	[GeneratedRegex(
		@"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9]))\.){3}(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9])|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])"
	)]
	private static partial Regex GetRegex();

	public static readonly Regex Regex = GetRegex();

	public bool IsValid() => Validate(Value);

	public static bool Validate(string str) => Regex.IsMatch(str);
}

public readonly partial struct NameString : IValidatableValueObject<string> {
	public bool IsValid() => Validate(value);

	public static bool Validate(string str) {
		foreach (var ch in str.AsSpan()) {
			// TODO проверить правила наличия символов в имени по паспорту
			if (char.IsLetter(ch) is false && ch is not '`' or '\'' or '-') {
				return false;
			}
		}

		return true;
	}
}

public readonly partial struct LetterOnlyString : IValidatableValueObject<string> {
	public bool IsValid() => Validate(value);

	public static bool Validate(string str) {
		foreach (var ch in str.AsSpan()) {
			if (char.IsLetter(ch) is false) {
				return false;
			}
		}

		return true;
	}
}

public readonly partial struct DigitOnlyString : IValidatableValueObject<string> {
	public bool IsValid() => Validate(value);

	public static bool Validate(string str) {
		foreach (var ch in str.AsSpan()) {
			if (char.IsDigit(ch) is false) {
				return false;
			}
		}

		return true;
	}
}
