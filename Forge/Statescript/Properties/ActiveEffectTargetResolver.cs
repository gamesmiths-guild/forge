// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the entity an active effect is applied to, from an <see cref="ActiveEffectHandle"/>.
/// </summary>
/// <remarks>
/// Invalid or missing handles resolve to <see langword="null"/>.
/// </remarks>
/// <param name="handleResolver">The resolver that produces the active effect handle to inspect.</param>
public class ActiveEffectTargetResolver(IObjectResolver<ActiveEffectHandle> handleResolver)
	: ObjectResolver<IForgeEntity>, IEntityResolver
{
	private readonly IObjectResolver<ActiveEffectHandle> _handleResolver = handleResolver
		?? throw new ArgumentNullException(nameof(handleResolver));

	/// <inheritdoc/>
	[return: MaybeNull]
	public override IForgeEntity Resolve(GraphContext graphContext)
	{
		return _handleResolver.Resolve(graphContext)?.Target;
	}
}
