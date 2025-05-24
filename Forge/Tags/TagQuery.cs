// Copyright © Gamesmiths Guild.

using System.Diagnostics;

namespace Gamesmiths.Forge.Tags;

/// <summary>
/// <para>A <see cref="TagQuery"/> is a logical filter that checks if an <see cref="TagContainer"/>
/// meets certain criteria.</para>
/// <para>If the criteria are met, the query is considered to "match".</para>
/// </summary>
/// <remarks>
/// <para>TagQueries allow you to create complex rules using logical expressions to test intersections with another tag
/// container, such as requiring all, any, or none of the tags. You can also define conditions based on groups of
/// sub-expressions (all, any, or none), allowing for highly flexible and nested logic. For example, a query checking
/// for tags ((A AND B) OR (C)) AND (!D) would look like ALL( ANY( ALL(A,B), ALL(C) ), NONE(D) ).</para>
/// <para>Queries can be set up directly in code.</para>
/// <para>Code example for creating a query:</para>
/// <code>
/// TagQuery query;
/// query.BuildQuery(
///   new TagQueryExpression(tagsManager)
///     .AllTagsMatch()
///       .AddTag("enemy.undead.zombie")
///       .AddTag("item.consumable.potion.health")
/// );
/// </code>
/// <para>Internally, queries are stored as byte streams to be memory-efficient and allow fast evaluation at runtime.
/// </para>
/// </remarks>
public class TagQuery
{
	private readonly List<Tag> _tagDictionary = [];

	/// <summary>
	/// Gets the byte stream that represents this query.
	/// </summary>
	internal List<byte> QueryTokenStream { get; private set; } = [];

	internal bool IsEmpty => QueryTokenStream.Count == 0;

	/// <summary>
	/// Static function to assemble and return a query.
	/// </summary>
	/// <param name="queryExpression">The <see cref="TagQueryExpression"/> for this query.</param>
	/// <returns>The created <see cref="TagQuery"/>.</returns>
	public static TagQuery BuildQuery(TagQueryExpression queryExpression)
	{
		var query = new TagQuery();
		query.Build(queryExpression);
		return query;
	}

	/// <summary>
	///  Builds a query that matches when any tags in the provided container matches those in the target container.
	/// </summary>
	/// <param name="container"><see cref="TagContainer"/> containing the tags being queried against.</param>
	/// <returns>A constructed <see cref="TagQuery"/> for matching any tags.</returns>
	public static TagQuery MakeQueryMatchAnyTags(TagContainer container)
	{
		return BuildQuery(
			new TagQueryExpression(container.TagsManager)
				.AnyTagsMatch()
				.AddTags(container));
	}

	/// <summary>
	///  Builds a query that matches when any tags in the provided container matches exactly those in the target
	///  container.
	/// </summary>
	/// <param name="container"><see cref="TagContainer"/> containing the tags being queried against.</param>
	/// <returns>A constructed <see cref="TagQuery"/> for matching any tags exact.</returns>
	public static TagQuery MakeQueryMatchAnyTagsExact(TagContainer container)
	{
		return BuildQuery(
			new TagQueryExpression(container.TagsManager)
				.AnyTagsMatchExact()
				.AddTags(container));
	}

	/// <summary>
	/// Builds a query that matches when all tags in the provided container matches those in the target container.
	/// </summary>
	/// <param name="container"><see cref="TagContainer"/> containing the tags being queried against.</param>
	/// <returns>A constructed <see cref="TagQuery"/> for matching all tags.</returns>
	public static TagQuery MakeQueryMatchAllTags(TagContainer container)
	{
		return BuildQuery(
			new TagQueryExpression(container.TagsManager)
				.AllTagsMatch()
				.AddTags(container));
	}

	/// <summary>
	/// Builds a query that matches when all tags in the provided container matches exactly those in the target
	/// container.
	/// </summary>
	/// <param name="container"><see cref="TagContainer"/> containing the tags being queried against.</param>
	/// <returns>A constructed <see cref="TagQuery"/> for matching all tags exact.</returns>
	public static TagQuery MakeQueryMatchAllTagsExact(TagContainer container)
	{
		return BuildQuery(
			new TagQueryExpression(container.TagsManager)
				.AllTagsMatchExact()
				.AddTags(container));
	}

	/// <summary>
	/// Builds a query that matches when no tags in the provided container matches those in the target container.
	/// </summary>
	/// <param name="container"><see cref="TagContainer"/> containing the tags being queried against.</param>
	/// <returns>A constructed <see cref="TagQuery"/> for matching no tags.</returns>
	public static TagQuery MakeQueryMatchNoTags(TagContainer container)
	{
		return BuildQuery(
			new TagQueryExpression(container.TagsManager)
				.NoTagsMatch()
				.AddTags(container));
	}

	/// <summary>
	/// Builds a query that matches when no tags in the provided container matches exactly those in the target
	/// container.
	/// </summary>
	/// <param name="container"><see cref="TagContainer"/> containing the tags being queried against.</param>
	/// <returns>A constructed <see cref="TagQuery"/> for matching no tags exactly.</returns>
	public static TagQuery MakeQueryMatchNoTagsExact(TagContainer container)
	{
		return BuildQuery(
			new TagQueryExpression(container.TagsManager)
				.NoTagsMatchExact()
				.AddTags(container));
	}

	/// <summary>
	/// Builds a query that matches when any tags in the provided container matches a single target tag.
	/// </summary>
	/// <param name="tag">The <see cref="Tag"/> being queried against.</param>
	/// <returns>A constructed <see cref="TagQuery"/> for matching a single tag.</returns>
	public static TagQuery MakeQueryMatchTag(Tag tag)
	{
		Debug.Assert(tag.TagsManager is not null, "Shouldn't be able to use tags without managers.");

		return BuildQuery(
			new TagQueryExpression(tag.TagsManager)
				.AllTagsMatch()
				.AddTag(tag));
	}

	/// <summary>
	/// Builds a query that matches when any tags in the provided container matches exactly a single target tag.
	/// </summary>
	/// <param name="tag">The <see cref="Tag"/> being queried against.</param>
	/// <returns>A constructed <see cref="TagQuery"/> for matching a single tag exact.</returns>
	public static TagQuery MakeQueryMatchTagExact(Tag tag)
	{
		Debug.Assert(tag.TagsManager is not null, "Shouldn't be able to use tags without managers.");

		return BuildQuery(
			new TagQueryExpression(tag.TagsManager)
				.AllTagsMatchExact()
				.AddTag(tag));
	}

	/// <summary>
	/// Creates this query with the given query expression.
	/// </summary>
	/// <param name="queryExpression">The <see cref="TagQueryExpression"/> for this query.</param>
	public void Build(TagQueryExpression queryExpression)
	{
		// Reserve an initial size to minimize memory reallocations, but keep it reasonably small to conserve memory.
		QueryTokenStream = new(128);
		_tagDictionary.Clear();

		// Emits the query, where 1 signals the presence of a query expression.
		QueryTokenStream.Add(1);
		queryExpression.EmitTokens(QueryTokenStream, _tagDictionary);
	}

	/// <summary>
	/// Evaluates whether the specified <see cref="TagContainer"/> satisfies the conditions defined by this
	/// query.
	/// </summary>
	/// <param name="container">The <see cref="TagContainer"/> to be tested against this query.</param>
	/// <returns><see langword="true"/> if the given tags match this query, or <see langword="false"/> otherwise.
	/// </returns>
	public bool Matches(TagContainer container)
	{
		if (IsEmpty)
		{
			return false;
		}

		QueryEvaluator queryEvaluator = new(this);
		return queryEvaluator.Evaluate(container);
	}

	/// <summary>
	/// Updates this tag set by replacing its existing tags with those from the specified container, without altering
	/// the query logic.
	/// </summary>
	/// <remarks>
	/// Ideal for frequently refreshed queries, provided the container size remains consistent.
	/// </remarks>
	/// <param name="container">The <see cref="TagContainer"/> supplying the new tags.</param>
	public void ReplaceTagsFast(TagContainer container)
	{
		Debug.Assert(container.Count == _tagDictionary.Count, "Must use containers with the same size.");

		_tagDictionary.Clear();
		_tagDictionary.AddRange(container.Tags);
	}

	/// <summary>
	/// Sets this tag collection to a single, specified tag, preserving the original query structure.
	/// </summary>
	/// <remarks>
	/// Ideal for caching and updating high-frequency queries without altering the logic.
	/// </remarks>
	/// <param name="tag">The <see cref="Tag"/> to replace the unique tag in the query.</param>
	public void ReplaceTagFast(Tag tag)
	{
		Debug.Assert(_tagDictionary.Count == 1, "Must use single containers.");

		_tagDictionary.Clear();
		_tagDictionary.Add(tag);
	}

	internal Tag GetTagFromIndex(int tagIndex)
	{
		return _tagDictionary[tagIndex];
	}
}
