// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Base for the resolvers that read an entity's input: the frame of its current tick.
/// </summary>
/// <remarks>
/// <para>The input is read from the input source entity's <see cref="IForgeEntity.Input"/>, so the value is the one
/// every machine simulating the entity reads. An entity without an input resolves to a default value, unless a derived
/// resolver reads something else in <see cref="ResolveWithoutInput"/>, such as an engine's input device.</para>
/// <para>An action the input does not carry reads as never held.</para>
/// </remarks>
/// <param name="inputSource">Resolves the entity whose input is read.</param>
public abstract class EntityInputResolver(IEntityResolver inputSource) : IPropertyResolver
{
	private readonly IEntityResolver _inputSource = inputSource;

	/// <inheritdoc/>
	public abstract Type ValueType { get; }

	/// <summary>
	/// Reads this resolver's value from an entity's input.
	/// </summary>
	/// <param name="input">The input source entity's input.</param>
	/// <returns>The resolved value.</returns>
	protected abstract Variant128 ResolveInput(EntityInput input);

	/// <summary>
	/// Gets the resolver for the ability's owner, the input source when none is given.
	/// </summary>
	protected static IEntityResolver AbilityOwner { get; } = new AbilityOwnerResolver();

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return _inputSource.Resolve(graphContext)?.Input is EntityInput input
			? ResolveInput(input)
			: ResolveWithoutInput(graphContext);
	}

	/// <summary>
	/// Resolves this resolver's value when the input source entity has no input.
	/// </summary>
	/// <param name="graphContext">The graph's context.</param>
	/// <returns>A default value.</returns>
	protected virtual Variant128 ResolveWithoutInput(GraphContext graphContext)
	{
		return default;
	}
}
