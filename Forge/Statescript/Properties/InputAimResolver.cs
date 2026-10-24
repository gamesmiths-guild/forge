// Copyright © Gamesmiths Guild.

using System.Numerics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves where an entity's input aims: the point aimed at, or the direction aimed in.
/// </summary>
/// <remarks>
/// The aim is the one whatever collected the input found, such as the ground under the mouse, rounded as it was
/// collected. An input that carries no aim resolves to zero. A 2D game's aim has a Z of zero, and the 2D values are
/// the aim's X and Y.
/// </remarks>
/// <param name="inputSource">Resolves the entity whose input is read.</param>
/// <param name="value">Which part of the aim to resolve.</param>
public class InputAimResolver(IEntityResolver inputSource, InputAimValue value) : EntityInputResolver(inputSource)
{
	private readonly InputAimValue _value = value;

	/// <inheritdoc/>
	public override Type ValueType =>
		_value is InputAimValue.Point2D or InputAimValue.Direction2D ? typeof(Vector2) : typeof(Vector3);

	/// <summary>
	/// Initializes a new instance of the <see cref="InputAimResolver"/> class reading the ability owner's input.
	/// </summary>
	/// <param name="value">Which part of the aim to resolve.</param>
	public InputAimResolver(InputAimValue value)
		: this(AbilityOwner, value)
	{
	}

	/// <inheritdoc/>
	protected override Variant128 ResolveInput(EntityInput input)
	{
		InputFrame frame = input.Current;

		if (!frame.HasAim)
		{
			return default;
		}

		return _value switch
		{
			InputAimValue.Point2D => new Variant128(new Vector2(frame.AimPoint.X, frame.AimPoint.Y)),
			InputAimValue.Direction3D => new Variant128(frame.AimDirection),
			InputAimValue.Direction2D => new Variant128(Flatten(frame.AimDirection)),
			_ => new Variant128(frame.AimPoint),
		};
	}

	// A 3D direction read in 2D keeps its length of 1, unless it points straight along Z.
	private static Vector2 Flatten(Vector3 direction)
	{
		var flat = new Vector2(direction.X, direction.Y);
		return flat.LengthSquared() > 0f ? Vector2.Normalize(flat) : Vector2.Zero;
	}
}
