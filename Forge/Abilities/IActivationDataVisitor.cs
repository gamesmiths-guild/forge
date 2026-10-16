// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Receives the activation data of an <see cref="AbilityBehaviorContext{TData}"/> with its type known at compile time.
/// </summary>
/// <typeparam name="TResult">The visit result type.</typeparam>
internal interface IActivationDataVisitor<out TResult>
{
	TResult Visit<TData>(TData data);
}
