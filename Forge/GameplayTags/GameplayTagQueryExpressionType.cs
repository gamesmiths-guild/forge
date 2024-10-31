// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayTags;

/// <summary>
/// Types of <see cref="GameplayTagQueryExpression"/>s.
/// </summary>
internal enum GameplayTagQueryExpressionType
{
	Undefined = 0,
	AnyTagsMatch = 1,
	AllTagsMatch = 2,
	NoTagsMatch = 3,
	AnyTagsMatchExact = 4,
	AllTagsMatchExact = 5,
	NoTagsMatchExact = 6,
	AnyExpressionsMatch = 7,
	AllExpressionsMatch = 8,
	NoExpressionsMatch = 9,
}
