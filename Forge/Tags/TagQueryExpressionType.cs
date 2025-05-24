// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Tags;

/// <summary>
/// Types of <see cref="TagQueryExpression"/>s.
/// </summary>
public enum TagQueryExpressionType
{
	/// <summary>
	/// Not defined.
	/// </summary>
	Undefined = 0,

	/// <summary>
	/// Any tags must match.
	/// </summary>
	AnyTagsMatch = 1,

	/// <summary>
	/// All tags must match.
	/// </summary>
	AllTagsMatch = 2,

	/// <summary>
	/// No tags must match.
	/// </summary>
	NoTagsMatch = 3,

	/// <summary>
	/// Any tags must match exactly.
	/// </summary>
	AnyTagsMatchExact = 4,

	/// <summary>
	/// All tags must match exactly.
	/// </summary>
	AllTagsMatchExact = 5,

	/// <summary>
	/// No tags must match exactly.
	/// </summary>
	NoTagsMatchExact = 6,

	/// <summary>
	/// Any expressions must match.
	/// </summary>
	AnyExpressionsMatch = 7,

	/// <summary>
	/// All expressions must match.
	/// </summary>
	AllExpressionsMatch = 8,

	/// <summary>
	/// No expressions must match.
	/// </summary>
	NoExpressionsMatch = 9,
}
