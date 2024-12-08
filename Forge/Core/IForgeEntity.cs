// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayTags;
using Attribute = Gamesmiths.Forge.Attributes.Attribute;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Interface for implementing entities that can be used by the Forge Gameplay Framework.
/// </summary>
public interface IForgeEntity
{
	/// <summary>
	/// Gets the effects manager for this entity.
	/// </summary>
	public GameplayEffectsManager GameplayEffectsManager { get; }

	/// <summary>
	/// Gets the attribute sets of this entity.
	/// TODO: Convert AttributeSets into a container class that also keeps the Attributes dictionary.
	/// </summary>
	public List<AttributeSet> AttributeSets { get; }

	/// <summary>
	/// Gets a dictionary mapping of all attributes of this entity.
	/// </summary>
	public Dictionary<StringKey, Attribute> Attributes { get; }

	/// <summary>
	/// Gets the gameplay tags of this entity.
	/// </summary>
	public GameplayTagContainer GameplayTags { get; }

	/// <summary>
	/// Adds an attribute set to this entity's attribute sets while handling the mapping of <see cref="Attributes"/>.
	/// </summary>
	/// <param name="attributeSet">The attribute set to be added.</param>
	public void AddAttributeSet(AttributeSet attributeSet)
	{
		Debug.Assert(attributeSet is not null, "AttributeSets is not initialized.");

		AttributeSets.Add(attributeSet);

		foreach (KeyValuePair<StringKey, Attribute> attribute in attributeSet.AttributesMap)
		{
			Attributes.Add(attribute.Key, attribute.Value);
		}
	}
}
