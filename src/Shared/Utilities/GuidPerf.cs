using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Utilities;

public static class GuidPerf {
	[ThreadStatic]
	private static Random? RandomPerThread;

	private static Random Random => RandomPerThread ??= new();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe Guid NewCryptoResist() {
		var bytes = stackalloc byte[16];
		var span = new Span<byte>(bytes, 16);
		RandomNumberGenerator.Fill(span);
		return *(Guid*)bytes;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe Guid New() {
		var bytes = stackalloc byte[16];
		var span = new Span<byte>(bytes, 16);
		Random.NextBytes(span);
		return *(Guid*)bytes;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe Guid NewWithBit(GuidBits index, byte value) {
		if (index.InRange() is false) {
			throw new ArgumentOutOfRangeException(nameof(index), index, null);
		}

		var bytes = stackalloc byte[16];
		var span = new Span<byte>(bytes, 16);
		Random.NextBytes(span);
		bytes[(byte)index] = value;

		return *(Guid*)bytes;
	}

	/// <param name="guid"></param>
	/// <param name="index">bit: [0,15]</param>
	/// <param name="value"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe void SetBit(ref Guid guid, GuidBits index, byte value) {
		if (index.InRange() is false) {
			throw new ArgumentOutOfRangeException(nameof(index), index, null);
		}

		fixed (Guid* gr = &guid) {
			var byteRef = (byte*)gr;
			*(byteRef + (byte)index) = value;
		}
	}

	/// <param name="guid"></param>
	/// <param name="index">bit: [0,15]</param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe byte CheckBit(ref Guid guid, GuidBits index) {
		if (index.InRange() is false) {
			throw new ArgumentOutOfRangeException(nameof(index), index, null);
		}

		fixed (Guid* gr = &guid) {
			var byteRef = (byte*)gr;
			return *(byteRef + (byte)index);
		}
	}
}

public enum GuidBits : byte {
	Zero = 0,
	One = 1,
	Two = 2,
	Three = 3,
	Four = 4,
	Five = 5,
	Six = 6,
	Seven = 7,
	Eight = 8,
	Nine = 9,
	Ten = 10,
	Eleven = 11,
	Twelve = 12,
	Thirteen = 13,
	Fourteen = 14,
	Fifteen = 15
}

public static class GuidBitsExtensions {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool InRange(this GuidBits gb) {
		var b = (byte)gb;
		return b is >= 0 and <= 15;
	}
}
