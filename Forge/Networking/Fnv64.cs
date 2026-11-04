// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking;

// FNV-1a over 64 bits, for the hashes two machines compare in a handshake: the same on every platform and in every
// run, unlike the runtime's randomized string hashing.
internal static class Fnv64
{
	public const ulong Offset = 0xCBF29CE484222325;

	private const ulong Prime = 0x100000001B3;

	public static ulong Add(ulong hash, ulong value)
	{
		unchecked
		{
			for (int i = 0; i < 8; i++)
			{
				hash = (hash ^ ((value >> (8 * i)) & 0xFF)) * Prime;
			}

			return hash;
		}
	}

	public static ulong Add(ulong hash, string text)
	{
		unchecked
		{
			foreach (char character in text)
			{
				hash = (hash ^ character) * Prime;
			}

			// The length keeps "ab" + "c" apart from "a" + "bc".
			return Add(hash, (ulong)text.Length);
		}
	}
}
