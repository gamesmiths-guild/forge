// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by matching a full <see cref="EffectQuery"/> against the effect behind an
/// <see cref="ActiveEffectHandle"/>.
/// </summary>
/// <remarks>
/// <para>Use this when the filter needs more than tags — a specific <see cref="EffectData"/>, the source entity, or a
/// modified attribute. For the common tag-only case, <see cref="ActiveEffectTagQueryResolver"/> is cheaper to
/// configure.</para>
/// <para>Composes with <see cref="ObjectWhereResolver{T}"/> exactly as the tag query resolver does.</para>
/// <para>Invalid or missing handles resolve to <see langword="false"/>.</para>
/// </remarks>
/// <param name="handleResolver">The resolver that produces the active effect handle to inspect.</param>
/// <param name="query">The query the effect must match. An empty query matches every effect.</param>
public class EffectQueryMatchResolver(IObjectResolver<ActiveEffectHandle> handleResolver, EffectQuery query)
	: IPropertyResolver
{
	private readonly IObjectResolver<ActiveEffectHandle> _handleResolver = handleResolver
		?? throw new ArgumentNullException(nameof(handleResolver));

	private readonly EffectQuery _query = query;

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return new Variant128(_query.Matches(_handleResolver.Resolve(graphContext)));
	}
}
