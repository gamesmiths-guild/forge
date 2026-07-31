// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the entity that triggered an active effect, from an <see cref="ActiveEffectHandle"/>.
/// </summary>
/// <remarks>
/// <para>This is <see cref="EffectOwnership.Owner"/> — who triggered the action that caused the effect. Use
/// <see cref="ActiveEffectSourceResolver"/> for what actually caused it.</para>
/// <para>Invalid or missing handles resolve to <see langword="null"/>.</para>
/// </remarks>
/// <param name="handleResolver">The resolver that produces the active effect handle to inspect.</param>
public class ActiveEffectOwnerResolver(IObjectResolver<ActiveEffectHandle> handleResolver)
	: ObjectResolver<IForgeEntity>, IEntityResolver
{
	private readonly IObjectResolver<ActiveEffectHandle> _handleResolver = handleResolver
		?? throw new ArgumentNullException(nameof(handleResolver));

	/// <inheritdoc/>
	[return: MaybeNull]
	public override IForgeEntity Resolve(GraphContext graphContext)
	{
		return _handleResolver.Resolve(graphContext)?.Effect?.Ownership.Owner;
	}
}
