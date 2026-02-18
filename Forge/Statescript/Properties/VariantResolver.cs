// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// A property resolver that holds a fixed <see cref="Variant128"/> value directly. Use this for constant values in
/// property expressions (e.g., the right-hand side of a <see cref="ComparisonResolver"/>).
/// </summary>
/// <param name="initialValue">The value of the resolver.</param>
/// <param name="valueType">The type of the value this resolver holds.</param>
public class VariantResolver(Variant128 initialValue, Type valueType) : IPropertyResolver
{
	/// <summary>
	/// Gets or sets the current value of this resolver.
	/// </summary>
	public Variant128 Value { get; set; } = initialValue;

	/// <inheritdoc/>
	public Type ValueType { get; } = valueType;

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return Value;
	}

	/// <summary>
	/// Sets the value from a typed input, converting it to a <see cref="Variant128"/>.
	/// </summary>
	/// <typeparam name="T">The type of the value to set. Must be supported by <see cref="Variant128"/>.</typeparam>
	/// <param name="value">The value to set.</param>
	/// <exception cref="ArgumentException">Thrown if the type T is not supported by <see cref="Variant128"/>.
	/// </exception>
	public void Set<T>(T value)
	{
		Value = Variables.CreateVariant(value);
	}
}
