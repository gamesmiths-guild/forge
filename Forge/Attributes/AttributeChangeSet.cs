// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Attributes;

/// <summary>
/// The net change one operation — an effect executing, applying, restacking or being rebuilt — made to each attribute
/// of an entity, kept apart from whatever else changed those attributes meanwhile.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="EntityAttribute"/> also keeps an entity-wide pending change, but that one accumulates across every
/// operation until the next flush publishes it, so a nested operation — a raised event activating an ability that
/// commits its cost while a hit is still landing — would read the hit's deltas as its own. Cues and the
/// modifier-success check read this instead, through the scope <see cref="EntityAttributes.BeginChanges"/> opens
/// around the operation.
/// </para>
/// <para>
/// Pooled by its <see cref="EntityAttributes"/>, so an execution costs no allocation once warm. A few entries searched
/// linearly are cheaper than a dictionary for the handful of attributes one effect touches.
/// </para>
/// </remarks>
internal sealed class AttributeChangeSet
{
	private readonly List<AttributeChange> _changes = [];

	/// <summary>
	/// Gets a value indicating whether the operation left any attribute's current value different from before.
	/// </summary>
	internal bool HasChanges
	{
		get
		{
			foreach (AttributeChange change in _changes)
			{
				if (change.Delta != 0)
				{
					return true;
				}
			}

			return false;
		}
	}

	/// <summary>
	/// Gets the net change the operation made to the current value of the attribute with a key; zero for one it never
	/// touched.
	/// </summary>
	/// <remarks>
	/// Looked up by key rather than through the entity's attributes so the change survives the attribute leaving: an
	/// attribute set being removed unapplies its modifiers, which is recorded here, and detaches the attributes before
	/// the update cues read it, and a hook can swap a set for a fresh instance between an operation and its cues.
	/// </remarks>
	/// <param name="attributeKey">The key of the attribute to look up.</param>
	/// <returns>The net change to the attribute's current value.</returns>
	internal int DeltaOf(StringKey attributeKey)
	{
		for (int i = 0; i < _changes.Count; i++)
		{
			if (_changes[i].Attribute.Key == attributeKey)
			{
				return _changes[i].Delta;
			}
		}

		return 0;
	}

	internal void Record(EntityAttribute attribute, int delta)
	{
		int index = IndexOf(attribute);

		if (index < 0)
		{
			_changes.Add(new AttributeChange(attribute, delta));
			return;
		}

		_changes[index] = new AttributeChange(attribute, _changes[index].Delta + delta);
	}

	internal void Clear()
	{
		_changes.Clear();
	}

	private int IndexOf(EntityAttribute attribute)
	{
		for (int i = 0; i < _changes.Count; i++)
		{
			if (ReferenceEquals(_changes[i].Attribute, attribute))
			{
				return i;
			}
		}

		return -1;
	}

	private readonly record struct AttributeChange(EntityAttribute Attribute, int Delta);
}
