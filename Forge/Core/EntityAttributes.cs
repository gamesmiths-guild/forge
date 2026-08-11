// Copyright © Gamesmiths Guild.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Attributes;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Container class which handles and manages all <see cref="AttributeSet"/>s and <see cref="EntityAttribute"/>s of an
/// entity.
/// Attributes can be accessed with the indexer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EntityAttributes"/> class.
/// </remarks>
/// <param name="owner">The owner of this manager.</param>
public class EntityAttributes(IForgeEntity owner) : IEnumerable<EntityAttribute>
{
	private readonly Dictionary<StringKey, EntityAttribute> _attributes = [];
	private readonly List<AttributeSet> _attributeSets = [];

	/// <summary>
	/// Event invoked when an attribute set is added to this entity, carrying the set.
	/// </summary>
	/// <remarks>
	/// Raised after the entity's attributes and its active effects have settled around the new set, so handlers
	/// observe the finished state.
	/// </remarks>
	public event Action<AttributeSet>? OnAttributeSetAdded;

	/// <summary>
	/// Event invoked when an attribute set is removed from this entity, carrying the set.
	/// </summary>
	/// <remarks>
	/// Raised after the set's attributes have been detached and the active effects have been re-evaluated without
	/// them. The set itself keeps its attributes and their values, so it can be added back later.
	/// </remarks>
	public event Action<AttributeSet>? OnAttributeSetRemoved;

	/// <summary>
	/// Gets the owner of this manager.
	/// </summary>
	public IForgeEntity Owner { get; } = owner;

	/// <summary>
	/// Gets the attribute sets of this entity.
	/// </summary>
	/// <remarks>
	/// Read-only: the manager keeps this list in step with the attribute mapping behind the indexer, so sets are
	/// added and removed through <see cref="AddAttributeSet"/> and <see cref="RemoveAttributeSet"/> rather than
	/// through this list.
	/// </remarks>
	public IReadOnlyList<AttributeSet> AttributeSets => _attributeSets;

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
	/// <param name="owner">The owner of this manager.</param>
	/// <param name="attributeSet">An initial attribute set for initialization.</param>
	public EntityAttributes(IForgeEntity owner, AttributeSet attributeSet)
		: this(owner)
	{
		AttachAttributeSet(attributeSet);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EntityAttributes"/> class.
	/// </summary>
	/// <param name="owner">The owner of this manager.</param>
	/// <param name="attributeSets">A number of attribute sets for initialization.</param>
	public EntityAttributes(IForgeEntity owner, AttributeSet[] attributeSets)
		: this(owner)
	{
		foreach (AttributeSet attributeSet in attributeSets)
		{
			AttachAttributeSet(attributeSet);
		}
	}

	/// <summary>
	/// Adds an attribute set to this manager's attribute sets while handling the mapping of
	/// <see cref="EntityAttributes"/>.
	/// </summary>
	/// <remarks>
	/// Adding a set to a live entity re-evaluates its active effects, so an effect carrying a modifier for one of the
	/// new attributes starts contributing immediately instead of waiting for something else to trigger a
	/// re-evaluation. The set keeps whatever values its attributes already hold, so a set that was removed earlier
	/// comes back exactly as it left.
	/// </remarks>
	/// <param name="attributeSet">The attribute set to be added.</param>
	public void AddAttributeSet(AttributeSet attributeSet)
	{
		Validation.Assert(attributeSet is not null, "AttributeSet is not initialized.");
		Validation.Assert(
			Owner.EffectsManager is not null,
			"The owner's EffectsManager must exist before its attribute sets can change at runtime.");

		Owner.EffectsManager.RebuildAroundAttributeChange(() => AttachAttributeSet(attributeSet));

		OnAttributeSetAdded?.Invoke(attributeSet);
	}

	/// <summary>
	/// Removes an attribute set from this manager's attribute sets while handling the mapping of
	/// <see cref="EntityAttributes"/>.
	/// </summary>
	/// <remarks>
	/// <para>Active effects survive the removal. Their modifiers for the departing attributes are unwound first and
	/// then dropped on re-evaluation, so an effect that also modifies attributes the entity keeps goes on applying
	/// those. This matches how the rest of the system treats a modifier naming an attribute the target does not have:
	/// it is skipped, not an error.</para>
	/// <para>Two consequences are worth knowing. Values already captured into an effect's snapshots are **not**
	/// rolled back, since a snapshot is a reading taken at a point in time. And an ability whose cost is charged
	/// against a departing attribute becomes uncastable, because a cost that can never be paid is refused rather than
	/// quietly skipped.</para>
	/// <para>The set is not modified: it keeps its attributes and their current values, so it can be added back to
	/// this entity later.</para>
	/// </remarks>
	/// <param name="attributeSet">The attribute set to be removed.</param>
	/// <returns><see langword="true"/> if the attribute set was found and removed; otherwise,
	/// <see langword="false"/>.</returns>
	public bool RemoveAttributeSet(AttributeSet attributeSet)
	{
		Validation.Assert(attributeSet is not null, "AttributeSet is not initialized.");
		Validation.Assert(
			Owner.EffectsManager is not null,
			"The owner's EffectsManager must exist before its attribute sets can change at runtime.");

		if (!_attributeSets.Contains(attributeSet))
		{
			return false;
		}

		Owner.EffectsManager.RebuildAroundAttributeChange(() =>
		{
			// Flushed after the modifiers have come off and while these attributes are still reachable. Taking a
			// modifier off is itself a change, and once they are detached nothing enumerates them to raise it, so the
			// delta would sit pending on an object no longer connected to anything.
			foreach (EntityAttribute attribute in attributeSet.AttributesMap.Values)
			{
				attribute.ApplyPendingValueChanges();
			}

			DetachAttributeSet(attributeSet);
		});

		OnAttributeSetRemoved?.Invoke(attributeSet);

		return true;
	}

	/// <summary>
	/// Tries to get an attribute of this entity from its key.
	/// </summary>
	/// <param name="key">The attribute key.</param>
	/// <param name="attribute">The attribute for the given key.</param>
	/// <returns><see langword="true"/> if the entity has an attribute for that key; otherwise,
	/// <see langword="false"/>.</returns>
	public bool TryGetAttribute(StringKey key, [NotNullWhen(true)] out EntityAttribute? attribute)
	{
		return _attributes.TryGetValue(key, out attribute);
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

	private void AttachAttributeSet(AttributeSet attributeSet)
	{
		foreach (KeyValuePair<StringKey, EntityAttribute> attribute in attributeSet.AttributesMap)
		{
			_attributes.Add(attribute.Key, attribute.Value);
		}

		_attributeSets.Add(attributeSet);
	}

	private void DetachAttributeSet(AttributeSet attributeSet)
	{
		foreach (StringKey attributeKey in attributeSet.AttributesMap.Keys)
		{
			_attributes.Remove(attributeKey);
		}

		_attributeSets.Remove(attributeSet);
	}
}
