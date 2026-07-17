// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the live <see cref="Effect"/> instance behind an <see cref="ActiveEffectHandle"/>.
/// </summary>
/// <remarks>
/// <para>Bridges the handle lane back to the effect lane, for example to write SetByCaller magnitudes or change the
/// level of an effect the graph only holds a handle for.</para>
/// <para>Invalid or missing handles resolve to <see langword="null"/>.</para>
/// </remarks>
/// <param name="handleResolver">The resolver that produces the active effect handle to inspect.</param>
public class ActiveEffectEffectResolver(IObjectResolver<ActiveEffectHandle> handleResolver) : ObjectResolver<Effect>
{
	private readonly IObjectResolver<ActiveEffectHandle> _handleResolver = handleResolver
		?? throw new ArgumentNullException(nameof(handleResolver));

	/// <inheritdoc/>
	[return: MaybeNull]
	public override Effect Resolve(GraphContext graphContext)
	{
		return _handleResolver.Resolve(graphContext)?.Effect;
	}
}
