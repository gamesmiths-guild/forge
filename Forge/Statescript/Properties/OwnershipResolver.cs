// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Composes an <see cref="EffectOwnership"/> from nested entity resolvers.
/// </summary>
/// <param name="ownerResolver">Resolver used for the ownership owner entity.</param>
/// <param name="sourceResolver">Resolver used for the ownership source entity.</param>
public class OwnershipResolver(
	IEntityResolver? ownerResolver = null,
	IEntityResolver? sourceResolver = null) : ObjectResolver<EffectOwnership>
{
	private readonly IEntityResolver? _ownerResolver = ownerResolver;
	private readonly IEntityResolver? _sourceResolver = sourceResolver;

	/// <inheritdoc/>
	public override EffectOwnership Resolve(GraphContext graphContext)
	{
		return new EffectOwnership(
			_ownerResolver?.Resolve(graphContext),
			_sourceResolver?.Resolve(graphContext));
	}
}
