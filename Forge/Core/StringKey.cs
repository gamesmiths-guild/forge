// Copyright © 2024 Gamesmiths Guild.

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
	private readonly string? _key;

	private readonly int _hashCode;

	/// <summary>
	/// Gets a default Empty <see cref="StringKey"/>.
	/// </summary>
	public static StringKey Empty { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="StringKey"/> struct.
	/// </summary>
	/// <param name="key">The <see cref="string"/> value used to initialize the <see cref="StringKey"/>. The provided
	/// string will be trimmed of any leading or trailing whitespace and stored in lowercase. It must not be null,
	/// empty, or contain only whitespace.</param>
	public StringKey(string key)
	{
		ArgumentNullException.ThrowIfNull(key);

		// Intern and store the string in lowercase to ensure case-insensitive uniqueness.
		_key = string.Intern(key.Trim().ToLowerInvariant());

		// Use OrdinalIgnoreCase for case-insensitive hashing.
		_hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(_key);
	}

	/// <inheritdoc />
	public override readonly string ToString()
	{
		if (string.IsNullOrEmpty(_key))
		{
			return string.Empty;
		}

		return _key;
	}

	/// <inheritdoc />
	public override readonly int GetHashCode()
	{
		return _hashCode;
	}

	/// <inheritdoc />
	public override readonly bool Equals(object? obj)
	{
		if (obj is StringKey other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public readonly bool Equals(StringKey other)
	{
		if (_key is not null)
		{
			return _key.Equals(other._key, StringComparison.OrdinalIgnoreCase);
		}

		return other._key is null;
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

		throw new ArgumentException($"Object is not a valid {typeof(StringKey)}", nameof(obj));
	}

	/// <inheritdoc/>
	public readonly int CompareTo(StringKey other)
	{
		return string.Compare(_key, other._key, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Implicitly converts a <see cref="StringKey"/> to its underlying <see cref="string"/> representation.
	/// </summary>
	/// <param name="stringId">The <see cref="StringKey"/> to convert.</param>
	public static implicit operator string(StringKey stringId)
	{
		if (string.IsNullOrEmpty(stringId._key))
		{
			return string.Empty;
		}

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
	/// <param name="lhs">The first <see cref="StringKey"/> to compare.</param>
	/// <param name="rhs">The second <see cref="StringKey"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(StringKey lhs, StringKey rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="StringKey"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="StringKey"/> to compare.</param>
	/// <param name="rhs">The second <see cref="StringKey"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(StringKey lhs, StringKey rhs)
	{
		return !lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines whether one <see cref="StringKey"/> is lexically less than another <see cref="StringKey"/> using
	/// <see cref="StringComparison.OrdinalIgnoreCase"/>.
	/// </summary>
	/// <param name="lhs">The first <see cref="StringKey"/>.</param>
	/// <param name="rhs">The second <see cref="StringKey"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="lhs"/> is lexically less than <paramref name="rhs"/>;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator <(StringKey lhs, StringKey rhs)
	{
		return lhs.CompareTo(rhs) < 0;
	}

	/// <summary>
	/// Determines whether one <see cref="StringKey"/> is lexically greater than another <see cref="StringKey"/> using
	/// <see cref="StringComparison.OrdinalIgnoreCase"/>.
	/// </summary>
	/// <param name="lhs">The first <see cref="StringKey"/>.</param>
	/// <param name="rhs">The second <see cref="StringKey"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="lhs"/> is lexically greater than <paramref name="rhs"/>;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator >(StringKey lhs, StringKey rhs)
	{
		return lhs.CompareTo(rhs) > 0;
	}

	/// <summary>
	/// Determines whether one <see cref="StringKey"/> is lexically less than or equal to another
	/// <see cref="StringKey"/> using <see cref="StringComparison.OrdinalIgnoreCase"/>.
	/// </summary>
	/// <param name="lhs">The first <see cref="StringKey"/>.</param>
	/// <param name="rhs">The second <see cref="StringKey"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="lhs"/> is lexically less than or equal to
	/// <paramref name="rhs"/>; otherwise, <see langword="false"/>.</returns>
	public static bool operator <=(StringKey lhs, StringKey rhs)
	{
		return lhs.CompareTo(rhs) <= 0;
	}

	/// <summary>
	/// Determines whether one <see cref="StringKey"/> is lexically greater than or equal to another
	/// <see cref="StringKey"/> using <see cref="StringComparison.OrdinalIgnoreCase"/>.
	/// </summary>
	/// <param name="lhs">The first <see cref="StringKey"/>.</param>
	/// <param name="rhs">The second <see cref="StringKey"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="lhs"/> is lexically greater than or equal to
	/// <paramref name="rhs"/>; otherwise, <see langword="false"/>.</returns>
	public static bool operator >=(StringKey lhs, StringKey rhs)
	{
		return lhs.CompareTo(rhs) >= 0;
	}
}
