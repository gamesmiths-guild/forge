// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// An immutable and memory-optimized representation of a string, stored in lowercase and compared in a case-insensitive
/// manner. The struct uses <see cref="string.Intern"/> to ensure that each unique string value is stored only once in
/// memory. Implicit conversions to and from <see cref="string"/> are supported.
/// <para>
/// This struct is case-insensitive and will always store the <see cref="string"/> in lowercase, after trimming any
/// leading or trailing whitespace. It should never be initialized using the <see langword="default"/> keyword or its
/// default <see cref="StringKey()"/> constructor.
/// </para>
/// </summary>
public readonly struct StringKey : IEquatable<StringKey>, IComparable<StringKey>, IComparable
{
	private readonly string _key;

	private readonly int _hashCode;

	/// <summary>
	/// Initializes a new instance of the <see cref="StringKey"/> struct.
	/// This creates an invalid <see cref="StringKey"/> instance and should never be used. Use the
	/// string-based constructor instead, passing a non-null and non-empty <see cref="string"/>.
	/// </summary>
#pragma warning disable S1133 // Deprecated code should be removed
	[Obsolete("Default constructor shouldn't be used. Use the string-based constructor instead.", true)]
#pragma warning restore S1133 // Deprecated code should be removed
	public StringKey()
	{
		// This constructor should not be used.
		_key = string.Empty;
		_hashCode = 0;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StringKey"/> struct.
	/// </summary>
	/// <param name="key">
	/// The <see cref="string"/> value used to initialize the <see cref="StringKey"/>. The provided string will be
	/// rimmed of any leading or trailing whitespace and stored in lowercase. It must not be null, empty, or contain
	/// only whitespace.
	/// </param>
	public StringKey(string key)
	{
		Debug.Assert(!string.IsNullOrWhiteSpace(key), $"A {nameof(StringKey)} should never be null or empty.");

		// Intern and store the string in lowercase to ensure case-insensitive uniqueness.
		_key = string.Intern(key.Trim().ToLowerInvariant());

		// Use OrdinalIgnoreCase for case-insensitive hashing.
		_hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(_key);
	}

	/// <inheritdoc />
	public override readonly string ToString()
	{
		Debug.Assert(!string.IsNullOrWhiteSpace(_key), $"A {nameof(StringKey)} should never be null or empty.");

		return _key;
	}

	/// <inheritdoc />
	public override readonly int GetHashCode()
	{
		Debug.Assert(!string.IsNullOrWhiteSpace(_key), $"A {nameof(StringKey)} should never be null or empty.");

		return _hashCode;
	}

	/// <inheritdoc />
	public override readonly bool Equals(object? obj)
	{
		Debug.Assert(!string.IsNullOrWhiteSpace(_key), $"A {nameof(StringKey)} should never be null or empty.");

		if (obj is StringKey other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public readonly bool Equals(StringKey other)
	{
		Debug.Assert(!string.IsNullOrWhiteSpace(_key), $"A {nameof(StringKey)} should never be null or empty.");
		Debug.Assert(!string.IsNullOrWhiteSpace(other._key), $"A {nameof(StringKey)} should never be null or empty.");

		return _key.Equals(other._key, StringComparison.OrdinalIgnoreCase);
	}

	/// <inheritdoc/>
	public readonly int CompareTo(object? obj)
	{
		if (obj is null)
		{
			return 1;
		}

		if (obj is StringKey other)
		{
			return CompareTo(other);
		}

		throw new ArgumentException($"Object is not a valid {nameof(StringKey)}", nameof(obj));
	}

	/// <inheritdoc/>
	public readonly int CompareTo(StringKey other)
	{
		Debug.Assert(!string.IsNullOrWhiteSpace(_key), $"A {nameof(StringKey)} should never be null or empty.");
		Debug.Assert(!string.IsNullOrWhiteSpace(other._key), $"A {nameof(StringKey)} should never be null or empty.");

		return string.Compare(_key, other._key, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Implicitly converts a <see cref="StringKey"/> to its underlying <see cref="string"/> representation.
	/// </summary>
	/// <param name="stringId">The <see cref="StringKey"/> to convert.</param>
	public static implicit operator string(StringKey stringId)
	{
		Debug.Assert(
			!string.IsNullOrWhiteSpace(stringId._key),
			$"A {nameof(StringKey)} should never be null or empty.");

		return stringId._key;
	}

	/// <summary>
	/// Implicitly converts a <see cref="string"/> to a <see cref="StringKey"/>. The string is stored in lowercase and
	/// compared case-insensitively.
	/// </summary>
	/// <param name="entryString"><see cref="string"/> to be converted.</param>
	public static implicit operator StringKey(string entryString)
	{
		return new StringKey(entryString);
	}

	/// <summary>
	/// Determines if two <see cref="StringKey"/> objects are equal.
	/// </summary>
	/// <param name="a">The first <see cref="StringKey"/> to compare.</param>
	/// <param name="b">The second <see cref="StringKey"/> to compare.</param>
	/// <returns>
	/// <see langword="true"/> if the values of <paramref name="a"/> and <paramref name="b"/> are equal; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public static bool operator ==(StringKey a, StringKey b)
	{
		return a.Equals(b);
	}

	/// <summary>
	/// Determines if two <see cref="StringKey"/> objects are not equal.
	/// </summary>
	/// <param name="a">The first <see cref="StringKey"/> to compare.</param>
	/// <param name="b">The second <see cref="StringKey"/> to compare.</param>
	/// <returns>
	/// <see langword="true"/> if the values of <paramref name="a"/> and <paramref name="b"/> are not equal; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public static bool operator !=(StringKey a, StringKey b)
	{
		return !a.Equals(b);
	}

	/// <summary>
	/// Determines whether one <see cref="StringKey"/> is lexically less than another <see cref="StringKey"/> using
	/// <see cref="StringComparison.OrdinalIgnoreCase"/>.
	/// </summary>
	/// <param name="a">The first <see cref="StringKey"/>.</param>
	/// <param name="b">The second <see cref="StringKey"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="a"/> is lexically less than <paramref name="b"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public static bool operator <(StringKey a, StringKey b)
	{
		return a.CompareTo(b) < 0;
	}

	/// <summary>
	/// Determines whether one <see cref="StringKey"/> is lexically greater than another <see cref="StringKey"/> using
	/// <see cref="StringComparison.OrdinalIgnoreCase"/>.
	/// </summary>
	/// <param name="a">The first <see cref="StringKey"/>.</param>
	/// <param name="b">The second <see cref="StringKey"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="a"/> is lexically greater than <paramref name="b"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public static bool operator >(StringKey a, StringKey b)
	{
		return a.CompareTo(b) > 0;
	}

	/// <summary>
	/// Determines whether one <see cref="StringKey"/> is lexically less than or equal to another
	/// <see cref="StringKey"/> using <see cref="StringComparison.OrdinalIgnoreCase"/>.
	/// </summary>
	/// <param name="a">The first <see cref="StringKey"/>.</param>
	/// <param name="b">The second <see cref="StringKey"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="a"/> is lexically less than or equal to <paramref name="b"/>;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	public static bool operator <=(StringKey a, StringKey b)
	{
		return a.CompareTo(b) <= 0;
	}

	/// <summary>
	/// Determines whether one <see cref="StringKey"/> is lexically greater than or equal to another
	/// <see cref="StringKey"/> using <see cref="StringComparison.OrdinalIgnoreCase"/>.
	/// </summary>
	/// <param name="a">The first <see cref="StringKey"/>.</param>
	/// <param name="b">The second <see cref="StringKey"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="a"/> is lexically greater than or equal to <paramref name="b"/>;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	public static bool operator >=(StringKey a, StringKey b)
	{
		return a.CompareTo(b) >= 0;
	}
}