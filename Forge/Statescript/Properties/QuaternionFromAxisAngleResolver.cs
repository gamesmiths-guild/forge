// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a quaternion created from an axis and angle, using
/// <see cref="Quaternion.CreateFromAxisAngle(Vector3, float)"/>.
/// </summary>
/// <param name="axis">The resolver for the axis vector.</param>
/// <param name="angle">The resolver for the angle in radians.</param>
public class QuaternionFromAxisAngleResolver(IPropertyResolver axis, IPropertyResolver angle) : IPropertyResolver
{
	private readonly IPropertyResolver _axis = axis;

	private readonly IPropertyResolver _angle = angle;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(axis.ValueType, angle.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Vector3 axisValue = _axis.Resolve(graphContext).AsVector3();
		float angleValue = _angle.Resolve(graphContext).AsFloat();
		return new Variant128(Quaternion.CreateFromAxisAngle(axisValue, angleValue));
	}

	private static Type ValidateTypes(Type axisType, Type angleType)
	{
		if (axisType != typeof(Vector3))
		{
			throw new ArgumentException(
				$"QuaternionFromAxisAngleResolver requires 'axis' to be Vector3. Got '{axisType}'.");
		}

		if (angleType != typeof(float))
		{
			throw new ArgumentException(
				$"QuaternionFromAxisAngleResolver requires 'angle' to be float. Got '{angleType}'.");
		}

		return typeof(Quaternion);
	}
}
