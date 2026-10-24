// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the direction four input actions point in, in an entity's input, as a vector no longer than 1.
/// </summary>
/// <remarks>
/// When one of the input's vectors is made of the four actions, the value is that vector, so a stick stays analog.
/// Otherwise each action is read as a button, and a diagonal is shortened to length 1.
/// </remarks>
/// <param name="inputSource">Resolves the entity whose input is read.</param>
/// <param name="negativeXActionName">The action pushing X towards -1.</param>
/// <param name="positiveXActionName">The action pushing X towards 1.</param>
/// <param name="negativeYActionName">The action pushing Y towards -1.</param>
/// <param name="positiveYActionName">The action pushing Y towards 1.</param>
public class InputVectorResolver(
	IEntityResolver inputSource,
	string negativeXActionName,
	string positiveXActionName,
	string negativeYActionName,
	string positiveYActionName) : EntityInputResolver(inputSource)
{
	private readonly string _negativeXActionName = negativeXActionName;
	private readonly string _positiveXActionName = positiveXActionName;
	private readonly string _negativeYActionName = negativeYActionName;
	private readonly string _positiveYActionName = positiveYActionName;

	/// <inheritdoc/>
	public override Type ValueType => typeof(Vector2);

	/// <summary>
	/// Initializes a new instance of the <see cref="InputVectorResolver"/> class reading the ability owner's input.
	/// </summary>
	/// <param name="negativeXActionName">The action pushing X towards -1.</param>
	/// <param name="positiveXActionName">The action pushing X towards 1.</param>
	/// <param name="negativeYActionName">The action pushing Y towards -1.</param>
	/// <param name="positiveYActionName">The action pushing Y towards 1.</param>
	public InputVectorResolver(
		string negativeXActionName,
		string positiveXActionName,
		string negativeYActionName,
		string positiveYActionName)
		: this(AbilityOwner, negativeXActionName, positiveXActionName, negativeYActionName, positiveYActionName)
	{
	}

	/// <inheritdoc/>
	protected override Variant128 ResolveInput(EntityInput input)
	{
		Vector2 value = input.GetVector(
			_negativeXActionName,
			_positiveXActionName,
			_negativeYActionName,
			_positiveYActionName);

		// A collected vector can be longer than 1, such as a stick pushed into a corner.
		return new Variant128(value.LengthSquared() > 1f ? Vector2.Normalize(value) : value);
	}
}
