// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the entity that applied an active effect, from an <see cref="ActiveEffectHandle"/>.
/// </summary>
/// <remarks>
/// <para>This is <see cref="EffectOwnership.Source"/> — what actually caused the effect, such as the weapon or the
/// projectile. Use <see cref="ActiveEffectOwnerResolver"/> for who triggered it.</para>
/// <para>Invalid or missing handles resolve to <see langword="null"/>.</para>
/// </remarks>
/// <param name="handleResolver">The resolver that produces the active effect handle to inspect.</param>
public class ActiveEffectSourceResolver(IObjectResolver<ActiveEffectHandle> handleResolver)
	: ObjectResolver<IForgeEntity>, IEntityResolver
{
	private readonly IObjectResolver<ActiveEffectHandle> _handleResolver = handleResolver
		?? throw new ArgumentNullException(nameof(handleResolver));

	/// <inheritdoc/>
	[return: MaybeNull]
	public override IForgeEntity Resolve(GraphContext graphContext)
	{
		return _handleResolver.Resolve(graphContext)?.Effect?.Ownership.Source;
	}
}
