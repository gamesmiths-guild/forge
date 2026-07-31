// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by evaluating a <see cref="TagQuery"/> against the tags of the effect behind an
/// <see cref="ActiveEffectHandle"/>.
/// </summary>
/// <remarks>
/// <para>This is the predicate that makes <see cref="ObjectWhereResolver{T}"/> able to filter effect arrays by
/// category, so <c>QueryActiveEffects → ObjectWhere → RemoveEffect</c> dispels by kind without any dedicated node.
/// </para>
/// <para>Invalid or missing handles resolve to <see langword="false"/>.</para>
/// </remarks>
/// <param name="handleResolver">The resolver that produces the active effect handle to inspect.</param>
/// <param name="query">The query to evaluate against the selected tags.</param>
/// <param name="effectTagSource">Which set of the effect's tags to evaluate against. Defaults to the effect's own
/// tags together with the tags it grants.</param>
public class ActiveEffectTagQueryResolver(
	IObjectResolver<ActiveEffectHandle> handleResolver,
	TagQuery query,
	EffectTagSource effectTagSource = EffectTagSource.OwningTags) : IPropertyResolver
{
	private readonly IObjectResolver<ActiveEffectHandle> _handleResolver = handleResolver
		?? throw new ArgumentNullException(nameof(handleResolver));

	private readonly TagQuery _query = query ?? throw new ArgumentNullException(nameof(query));

	private readonly EffectTagSource _effectTagSource = effectTagSource;

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <summary>
	/// Initializes a new instance of the <see cref="ActiveEffectTagQueryResolver"/> class from a query expression.
	/// </summary>
	/// <param name="handleResolver">The resolver that produces the active effect handle to inspect.</param>
	/// <param name="queryExpression">The expression used to build the tag query.</param>
	/// <param name="effectTagSource">Which set of the effect's tags to evaluate against. Defaults to the effect's own
	/// tags together with the tags it grants.</param>
	public ActiveEffectTagQueryResolver(
		IObjectResolver<ActiveEffectHandle> handleResolver,
		TagQueryExpression queryExpression,
		EffectTagSource effectTagSource = EffectTagSource.OwningTags)
		: this(handleResolver, BuildQuery(queryExpression), effectTagSource)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ActiveEffectTagQueryResolver"/> class for the common single-tag
	/// match case.
	/// </summary>
	/// <param name="handleResolver">The resolver that produces the active effect handle to inspect.</param>
	/// <param name="tag">The tag to match against the selected tags.</param>
	/// <param name="effectTagSource">Which set of the effect's tags to evaluate against. Defaults to the effect's own
	/// tags together with the tags it grants.</param>
	public ActiveEffectTagQueryResolver(
		IObjectResolver<ActiveEffectHandle> handleResolver,
		Tag tag,
		EffectTagSource effectTagSource = EffectTagSource.OwningTags)
		: this(handleResolver, TagQuery.MakeQueryMatchTag(tag), effectTagSource)
	{
	}

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Effect? effect = _handleResolver.Resolve(graphContext)?.Effect;

		if (effect is null)
		{
			return new Variant128(false);
		}

		return new Variant128(effect.MatchesTagQuery(_query, GetTagContainer(_effectTagSource, effect)));
	}

	private static TagQuery BuildQuery(TagQueryExpression queryExpression)
	{
		return TagQuery.BuildQuery(
			queryExpression ?? throw new ArgumentNullException(nameof(queryExpression)));
	}

	private static TagContainer? GetTagContainer(EffectTagSource effectTagSource, Effect effect)
	{
		return effectTagSource switch
		{
			EffectTagSource.OwningTags => effect.GetOwningTags(),
			EffectTagSource.EffectTags => effect.EffectData.EffectTags,
			EffectTagSource.GrantedTags => effect.CachedGrantedTags,
			_ => throw new ArgumentOutOfRangeException(
				nameof(effectTagSource),
				effectTagSource,
				$"Unsupported {nameof(EffectTagSource)} value."),
		};
	}
}
