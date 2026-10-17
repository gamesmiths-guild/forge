// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Receives an <see cref="AbilityBehaviorContext{TData}"/> with its <c>TData</c> known at compile time.
/// </summary>
/// <typeparam name="TResult">The visit result type.</typeparam>
internal interface IActivationDataVisitor<out TResult>
{
	TResult Visit<TData>(AbilityBehaviorContext<TData> context);
}
