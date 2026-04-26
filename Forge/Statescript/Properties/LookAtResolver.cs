// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a quaternion that rotates to look from one position toward another using an up vector.
/// </summary>
/// <param name="from">The resolver for the source position.</param>
/// <param name="to">The resolver for the target position.</param>
/// <param name="up">The resolver for the up vector.</param>
public class LookAtResolver(IPropertyResolver from, IPropertyResolver to, IPropertyResolver up) : IPropertyResolver
{
	private readonly IPropertyResolver _from = from;
	private readonly IPropertyResolver _to = to;
	private readonly IPropertyResolver _up = up;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(from.ValueType, to.ValueType, up.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Vector3 fromValue = _from.Resolve(graphContext).AsVector3();
		Vector3 toValue = _to.Resolve(graphContext).AsVector3();
		Vector3 upValue = _up.Resolve(graphContext).AsVector3();
		return new Variant128(GameplayMathUtils.LookAt(fromValue, toValue, upValue));
	}

	private static Type ValidateTypes(Type fromType, Type toType, Type upType)
	{
		if (fromType != typeof(Vector3) || toType != typeof(Vector3) || upType != typeof(Vector3))
		{
			throw new ArgumentException(
				"LookAtResolver requires from, to, and up to all be Vector3. " +
				$"Got '{fromType}', '{toType}', and '{upType}'.");
		}

		return typeof(Quaternion);
	}
}
