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
	/// Gets the net change the operation made to an attribute's current value; zero for one it never touched.
	/// </summary>
	/// <param name="attribute">The attribute to look up.</param>
	/// <returns>The net change to the attribute's current value.</returns>
	internal int DeltaOf(EntityAttribute attribute)
	{
		int index = IndexOf(attribute);

		return index < 0 ? 0 : _changes[index].Delta;
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
