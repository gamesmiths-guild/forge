// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the mathematical constant π (pi). Returns <see langword="double"/> by default, or <see langword="float"/>
/// if explicitly requested. This avoids magic numbers and communicates intent clearly.
/// </summary>
/// <param name="valueType">The desired output type. Must be <see langword="float"/> or <see langword="double"/>.
/// Defaults to <see langword="double"/> if not specified.</param>
public class PiResolver(Type? valueType = null) : IPropertyResolver
{
	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateType(valueType ?? typeof(double));

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (ValueType == typeof(float))
		{
			return new Variant128(MathF.PI);
		}

		return new Variant128(Math.PI);
	}

	private static Type ValidateType(Type type)
	{
		if (type != typeof(float) && type != typeof(double))
		{
			throw new ArgumentException(
				$"PiResolver only supports float and double output types. Got '{type}'.");
		}

		return type;
	}
}
