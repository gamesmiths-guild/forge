// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Specifies the ordering applied by sorting resolvers such as <see cref="OrderByResolver"/> and
/// <see cref="ObjectOrderByResolver{T}"/>.
/// </summary>
public enum SortDirection
{
	/// <summary>
	/// Elements are ordered from the smallest key to the largest key.
	/// </summary>
	Ascending = 0,

	/// <summary>
	/// Elements are ordered from the largest key to the smallest key.
	/// </summary>
	Descending = 1,
}
