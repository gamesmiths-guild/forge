// Copyright © Gamesmiths Guild.

using System.Collections;
using System.Diagnostics;
using Gamesmiths.Forge.Attributes;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Container class which handles and manages all <see cref="AttributeSet"/>s and <see cref="EntityAttribute"/>s of an entity.
/// Attributes can be accessed with the indexer.
/// </summary>
public class EntityAttributes : IEnumerable<EntityAttribute>
{
	private readonly Dictionary<StringKey, EntityAttribute> _attributes = [];

	/// <summary>
	/// Gets the attribute sets of this entity.
	/// </summary>
	public List<AttributeSet> AttributeSets { get; } = [];

	internal IReadOnlyDictionary<StringKey, EntityAttribute> AttributesMap => _attributes;

	/// <summary>
	/// Gets the mapping for the attributes of this container.
	/// </summary>
	/// <param name="key">The attribute key.</param>
	/// <returns>The attribute for the given key.</returns>
	public EntityAttribute this[StringKey key] => _attributes[key];

	/// <summary>
	/// Initializes a new instance of the <see cref="EntityAttributes"/> class.
	/// </summary>
	public EntityAttributes()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EntityAttributes"/> class.
	/// </summary>
	/// <param name="attributeSet">An initial attribute set for initialization.</param>
	public EntityAttributes(AttributeSet attributeSet)
	{
		AddAttributeSet(attributeSet);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EntityAttributes"/> class.
	/// </summary>
	/// <param name="attributeSets">A number of attribute sets for initialization.</param>
	public EntityAttributes(AttributeSet[] attributeSets)
	{
		foreach (AttributeSet attributeSet in attributeSets)
		{
			AddAttributeSet(attributeSet);
		}
	}

	/// <summary>
	/// Adds an attribute set to this managers's attribute sets while handling the mapping of <see cref="EntityAttributes"/>.
	/// </summary>
	/// <param name="attributeSet">The attribute set to be added.</param>
	public void AddAttributeSet(AttributeSet attributeSet)
	{
		Debug.Assert(attributeSet is not null, "AttributeSets is not initialized.");

		AttributeSets.Add(attributeSet);

		foreach (KeyValuePair<StringKey, EntityAttribute> attribute in attributeSet.AttributesMap)
		{
			_attributes.Add(attribute.Key, attribute.Value);
		}
	}

	/// <inheritdoc/>
	public IEnumerator<EntityAttribute> GetEnumerator()
	{
		return _attributes.Values.GetEnumerator();
	}

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return _attributes.Values.GetEnumerator();
	}

#pragma warning disable T0009 // Internal Styling Rule T0009
	internal void ApplyPendingValueChanges()
	{
		foreach (EntityAttribute attribute in _attributes.Values)
		{
			attribute.ApplyPendingValueChanges();
		}
	}

	internal bool ContainsAttribute(StringKey attributeKey)
#pragma warning restore T0009
	{
		return _attributes.ContainsKey(attributeKey);
	}
}
