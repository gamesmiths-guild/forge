// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;

namespace Gamesmiths.Forge.GameplayTags;

/// <summary>
/// <para>A <see cref="GameplayTagQuery"/> is a logical filter that checks if an <see cref="GameplayTagContainer"/>
/// meets certain criteria.</para>
/// <para>If the criteria are met, the query is considered to "match".</para>
/// </summary>
/// <remarks>
/// <para>GameplayTagQueries allow you to create complex rules using logical expressions to test intersections with
/// another tag container, such as requiring all, any, or none of the tags. You can also define conditions based on
/// groups of sub-expressions (all, any, or none), allowing for highly flexible and nested logic. For example, a query
/// checking for tags ((A AND B) OR (C)) AND (!D) would look like ALL( ANY( ALL(A,B), ALL(C) ), NONE(D) ).</para>
/// <para>Queries can be set up directly in code.</para>
/// <para>Code example for creating a query:</para>
/// <code>
/// GameplayTagQuery query;
/// query.BuildQuery(
///   new GameplayTagQueryExpression()
///     .AllTagsMatch()
///       .AddTag(GameplayTag.RequestTag("enemy.undead.zombie"))
///       .AddTag(GameplayTag.RequestTag("item.consumable.potion.health"))
/// );
/// </code>
/// <para>Internally, queries are stored as byte streams to be memory-efficient and allow fast evaluation at runtime.
/// </para>
/// </remarks>
public class GameplayTagQuery
{
	private readonly List<GameplayTag> _tagDictionary = [];

	/// <summary>
	/// Gets the byte stream that represents this query.
	/// </summary>
	internal List<byte> QueryTokenStream { get; private set; } = [];

	private bool IsEmpty => QueryTokenStream.Count == 0;

	/// <summary>
	/// Static function to assemble and return a query.
	/// </summary>
	/// <param name="queryExpression">The <see cref="GameplayTagQueryExpression"/> for this query.</param>
	/// <returns>The created <see cref="GameplayTagQuery"/>.</returns>
	public static GameplayTagQuery BuildQuery(GameplayTagQueryExpression queryExpression)
	{
		var query = new GameplayTagQuery();
		query.Build(queryExpression);
		return query;
	}

	/// <summary>
	///  Builds a query that matches when any tags in the provided container matches those in the target container.
	/// </summary>
	/// <param name="container"><see cref="GameplayTagContainer"/> containing the tags being queried against.</param>
	/// <returns>A constructed <see cref="GameplayTagQuery"/> for matching any tags.</returns>
	public static GameplayTagQuery MakeQueryMatchAnyTags(GameplayTagContainer container)
	{
		return BuildQuery(
			new GameplayTagQueryExpression()
				.AnyTagsMatch()
				.AddTags(container));
	}

	/// <summary>
	///  Builds a query that matches when any tags in the provided container matches exactly those in the target
	///  container.
	/// </summary>
	/// <param name="container"><see cref="GameplayTagContainer"/> containing the tags being queried against.</param>
	/// <returns>A constructed <see cref="GameplayTagQuery"/> for matching any tags exact.</returns>
	public static GameplayTagQuery MakeQueryMatchAnyTagsExact(GameplayTagContainer container)
	{
		return BuildQuery(
			new GameplayTagQueryExpression()
				.AnyTagsMatchExact()
				.AddTags(container));
	}

	/// <summary>
	/// Builds a query that matches when all tags in the provided container matches those in the target container.
	/// </summary>
	/// <param name="container"><see cref="GameplayTagContainer"/> containing the tags being queried against.</param>
	/// <returns>A constructed <see cref="GameplayTagQuery"/> for matching all tags.</returns>
	public static GameplayTagQuery MakeQueryMatchAllTags(GameplayTagContainer container)
	{
		return BuildQuery(
			new GameplayTagQueryExpression()
				.AllTagsMatch()
				.AddTags(container));
	}

	/// <summary>
	/// Builds a query that matches when all tags in the provided container matches exactly those in the target
	/// container.
	/// </summary>
	/// <param name="container"><see cref="GameplayTagContainer"/> containing the tags being queried against.</param>
	/// <returns>A constructed <see cref="GameplayTagQuery"/> for matching all tags exact.</returns>
	public static GameplayTagQuery MakeQueryMatchAllTagsExact(GameplayTagContainer container)
	{
		return BuildQuery(
			new GameplayTagQueryExpression()
				.AllTagsMatchExact()
				.AddTags(container));
	}

	/// <summary>
	/// Builds a query that matches when no tags in the provided container matches those in the target container.
	/// </summary>
	/// <param name="container"><see cref="GameplayTagContainer"/> containing the tags being queried against.</param>
	/// <returns>A constructed <see cref="GameplayTagQuery"/> for matching no tags.</returns>
	public static GameplayTagQuery MakeQueryMatchNoTags(GameplayTagContainer container)
	{
		return BuildQuery(
			new GameplayTagQueryExpression()
				.NoTagsMatch()
				.AddTags(container));
	}

	/// <summary>
	/// Builds a query that matches when no tags in the provided container matches exactly those in the target
	/// container.
	/// </summary>
	/// <param name="container"><see cref="GameplayTagContainer"/> containing the tags being queried against.</param>
	/// <returns>A constructed <see cref="GameplayTagQuery"/> for matching no tags exactly.</returns>
	public static GameplayTagQuery MakeQueryMatchNoTagsExact(GameplayTagContainer container)
	{
		return BuildQuery(
			new GameplayTagQueryExpression()
				.NoTagsMatchExact()
				.AddTags(container));
	}

	/// <summary>
	/// Builds a query that matches when any tags in the provided container matches a single target tag.
	/// </summary>
	/// <param name="tag">The <see cref="GameplayTag"/> being queried against.</param>
	/// <returns>A constructed <see cref="GameplayTagQuery"/> for matching a single tag.</returns>
	public static GameplayTagQuery MakeQueryMatchTag(GameplayTag tag)
	{
		return BuildQuery(
			new GameplayTagQueryExpression()
				.AllTagsMatch()
				.AddTag(tag));
	}

	/// <summary>
	/// Builds a query that matches when any tags in the provided container matches exactly a single target tag.
	/// </summary>
	/// <param name="tag">The <see cref="GameplayTag"/> being queried against.</param>
	/// <returns>A constructed <see cref="GameplayTagQuery"/> for matching a single tag exact.</returns>
	public static GameplayTagQuery MakeQueryMatchTagExact(GameplayTag tag)
	{
		return BuildQuery(
			new GameplayTagQueryExpression()
				.AllTagsMatchExact()
				.AddTag(tag));
	}

	/// <summary>
	/// Creates this query with the given query expression.
	/// </summary>
	/// <param name="queryExpression">The <see cref="GameplayTagQueryExpression"/> for this query.</param>
	public void Build(GameplayTagQueryExpression queryExpression)
	{
		// Reserve an initial size to minimize memory reallocations, but keep it reasonably small to conserve memory.
		QueryTokenStream = new(128);
		_tagDictionary.Clear();

		// Emits the query, where 1 signals the presence of a query expression.
		QueryTokenStream.Add(1);
		queryExpression.EmitTokens(QueryTokenStream, _tagDictionary);
	}

	/// <summary>
	/// Evaluates whether the specified <see cref="GameplayTagContainer"/> satisfies the conditions defined by this
	/// query.
	/// </summary>
	/// <param name="container">The <see cref="GameplayTagContainer"/> to be tested against this query.</param>
	/// <returns><see langword="true"/> if the given tags match this query, or <see langword="false"/> otherwise.
	/// </returns>
	public bool Matches(GameplayTagContainer container)
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
	/// <param name="container">The <see cref="GameplayTagContainer"/> supplying the new tags.</param>
	public void ReplaceTagsFast(GameplayTagContainer container)
	{
		Debug.Assert(container.Count != _tagDictionary.Count, "Must use containers with the same size.");

		_tagDictionary.Clear();
		_tagDictionary.AddRange(container.GameplayTags);
	}

	/// <summary>
	/// Sets this tag collection to a single, specified tag, preserving the original query structure.
	/// </summary>
	/// <remarks>
	/// Ideal for caching and updating high-frequency queries without altering the logic.
	/// </remarks>
	/// <param name="tag">The <see cref="GameplayTag"/> to replace the unique tag in the query.</param>
	public void ReplaceTagFast(GameplayTag tag)
	{
		Debug.Assert(_tagDictionary.Count != 1, "Must use single containers.");

		_tagDictionary.Clear();
		_tagDictionary.Add(tag);
	}

	internal GameplayTag GetTagFromIndex(int tagIndex)
	{
		return _tagDictionary[tagIndex];
	}
}
