// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by evaluating a <see cref="TagQuery"/> against one of a resolved entity's tag containers.
/// </summary>
/// <remarks>
/// <para>By default, this resolver targets the owner entity through <see cref="OwnerEntityResolver"/>.</para>
/// <para>If the selected entity is not available, the resolver returns <see langword="false"/>.</para>
/// </remarks>
public class TagQueryResolver : IPropertyResolver
{
	private static readonly IEntityResolver _defaultEntityResolver = new OwnerEntityResolver();

	private readonly IEntityResolver _entityResolver;
	private readonly TagQuery _query;
	private readonly TagQuerySource _tagQuerySource;

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <summary>
	/// Initializes a new instance of the <see cref="TagQueryResolver"/> class from a prebuilt query.
	/// </summary>
	/// <param name="query">The query to evaluate against the selected tag container.</param>
	public TagQueryResolver(TagQuery query)
		: this(query, _defaultEntityResolver)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagQueryResolver"/> class from a prebuilt query.
	/// </summary>
	/// <param name="query">The query to evaluate against the selected tag container.</param>
	/// <param name="tagQuerySource">Which tag container to evaluate against. Defaults to all tags.</param>
	public TagQueryResolver(TagQuery query, TagQuerySource tagQuerySource)
		: this(query, _defaultEntityResolver, tagQuerySource)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagQueryResolver"/> class from a prebuilt query.
	/// </summary>
	/// <param name="query">The query to evaluate against the selected tag container.</param>
	/// <param name="entityResolver">The entity resolver that selects which entity to inspect.</param>
	/// <param name="tagQuerySource">Which tag container to evaluate against. Defaults to all tags.</param>
	public TagQueryResolver(
		TagQuery query,
		IEntityResolver entityResolver,
		TagQuerySource tagQuerySource = TagQuerySource.AllTags)
	{
		EnsureNotNull(query, nameof(query));
		EnsureNotNull(entityResolver, nameof(entityResolver));

		_entityResolver = entityResolver;
		_query = query;
		_tagQuerySource = tagQuerySource;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagQueryResolver"/> class from a query expression.
	/// </summary>
	/// <param name="queryExpression">The expression used to build the tag query.</param>
	public TagQueryResolver(TagQueryExpression queryExpression)
		: this(queryExpression, _defaultEntityResolver)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagQueryResolver"/> class from a query expression.
	/// </summary>
	/// <param name="queryExpression">The expression used to build the tag query.</param>
	/// <param name="tagQuerySource">Which tag container to evaluate against. Defaults to all tags.</param>
	public TagQueryResolver(
		TagQueryExpression queryExpression,
		TagQuerySource tagQuerySource)
		: this(queryExpression, _defaultEntityResolver, tagQuerySource)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagQueryResolver"/> class from a query expression.
	/// </summary>
	/// <param name="queryExpression">The expression used to build the tag query.</param>
	/// <param name="entityResolver">The entity resolver that selects which entity to inspect.</param>
	/// <param name="tagQuerySource">Which tag container to evaluate against. Defaults to all tags.</param>
	public TagQueryResolver(
		TagQueryExpression queryExpression,
		IEntityResolver entityResolver,
		TagQuerySource tagQuerySource = TagQuerySource.AllTags)
		: this(BuildQuery(queryExpression), entityResolver, tagQuerySource)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagQueryResolver"/> class for the common single-tag match case.
	/// </summary>
	/// <param name="tag">The tag to match against the selected tag container.</param>
	public TagQueryResolver(Tag tag)
		: this(tag, _defaultEntityResolver)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagQueryResolver"/> class for the common single-tag match case.
	/// </summary>
	/// <param name="tag">The tag to match against the selected tag container.</param>
	/// <param name="tagQuerySource">Which tag container to evaluate against. Defaults to all tags.</param>
	public TagQueryResolver(
		Tag tag,
		TagQuerySource tagQuerySource)
		: this(tag, _defaultEntityResolver, tagQuerySource)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagQueryResolver"/> class for the common single-tag match case.
	/// </summary>
	/// <param name="tag">The tag to match against the selected tag container.</param>
	/// <param name="entityResolver">The entity resolver that selects which entity to inspect.</param>
	/// <param name="tagQuerySource">Which tag container to evaluate against. Defaults to all tags.</param>
	public TagQueryResolver(
		Tag tag,
		IEntityResolver entityResolver,
		TagQuerySource tagQuerySource = TagQuerySource.AllTags)
		: this(TagQuery.MakeQueryMatchTag(tag), entityResolver, tagQuerySource)
	{
	}

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);
		if (entity is null)
		{
			return new Variant128(false);
		}

		return new Variant128(_query.Matches(GetTagContainer(_tagQuerySource, entity.Tags)));
	}

	private static TagQuery BuildQuery(TagQueryExpression queryExpression)
	{
		EnsureNotNull(queryExpression, nameof(queryExpression));

		return TagQuery.BuildQuery(queryExpression);
	}

	private static TagContainer GetTagContainer(
		TagQuerySource tagQuerySource,
		EntityTags entityTags)
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
