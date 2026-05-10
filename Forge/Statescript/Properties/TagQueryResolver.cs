// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by evaluating a <see cref="TagQuery"/> against one of the ability owner's tag containers.
/// </summary>
/// <remarks>
/// <para>This resolver requires the graph to be driven by an ability. It retrieves the owner entity from the
/// <see cref="AbilityBehaviorContext"/> stored in the graph's <see cref="GraphContext.ActivationContext"/>.</para>
/// <para>If the graph has no activation context or the activation context is not an
/// <see cref="AbilityBehaviorContext"/>, the resolver always returns <see langword="false"/>.</para>
/// </remarks>
public class TagQueryResolver : IPropertyResolver
{
	private readonly TagQuery _query;
	private readonly TagQuerySource _tagQuerySource;

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <summary>
	/// Initializes a new instance of the <see cref="TagQueryResolver"/> class from a prebuilt query.
	/// </summary>
	/// <param name="query">The query to evaluate against the selected tag container.</param>
	/// <param name="tagQuerySource">Which tag container to evaluate against. Defaults to all tags.</param>
	public TagQueryResolver(
		TagQuery query,
		TagQuerySource tagQuerySource = TagQuerySource.AllTags)
	{
		EnsureNotNull(query, nameof(query));

		_query = query;
		_tagQuerySource = tagQuerySource;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagQueryResolver"/> class from a query expression.
	/// </summary>
	/// <param name="queryExpression">The expression used to build the tag query.</param>
	/// <param name="tagQuerySource">Which tag container to evaluate against. Defaults to all tags.</param>
	public TagQueryResolver(
		TagQueryExpression queryExpression,
		TagQuerySource tagQuerySource = TagQuerySource.AllTags)
		: this(BuildQuery(queryExpression), tagQuerySource)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagQueryResolver"/> class for the common single-tag match case.
	/// </summary>
	/// <param name="tag">The tag to match against the selected tag container.</param>
	/// <param name="tagQuerySource">Which tag container to evaluate against. Defaults to all tags.</param>
	public TagQueryResolver(
		Tag tag,
		TagQuerySource tagQuerySource = TagQuerySource.AllTags)
		: this(TagQuery.MakeQueryMatchTag(tag), tagQuerySource)
	{
	}

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (!graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext))
		{
			return new Variant128(false);
		}

		return new Variant128(_query.Matches(GetTagContainer(_tagQuerySource, abilityContext.Owner.Tags)));
	}

	private static TagQuery BuildQuery(TagQueryExpression queryExpression)
	{
		EnsureNotNull(queryExpression, nameof(queryExpression));

		return TagQuery.BuildQuery(queryExpression);
	}

	private static TagContainer GetTagContainer(
		TagQuerySource tagQuerySource,
		Core.EntityTags entityTags)
	{
		return tagQuerySource switch
		{
			TagQuerySource.AllTags => entityTags.AllTags,
			TagQuerySource.BaseTags => entityTags.BaseTags,
			TagQuerySource.ModifierTags => entityTags.ModifierTags,
			_ => throw new ArgumentOutOfRangeException(
				nameof(tagQuerySource),
				tagQuerySource,
				$"Unsupported {nameof(TagQuerySource)} value."),
		};
	}

	private static void EnsureNotNull<T>(T value, string paramName)
		where T : class
	{
#if NET8_0_OR_GREATER
		_ = paramName;
		ArgumentNullException.ThrowIfNull(value);
#else
		if (value is null)
		{
			throw new ArgumentNullException(paramName);
		}
#endif
	}
}
