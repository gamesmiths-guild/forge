// Copyright © Gamesmiths Guild.

using System.Collections.ObjectModel;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// A mutable property resolver that stores an array of <see cref="Variant128"/> values. This enables graph variables
/// that hold multiple values, such as a list of entity IDs returned by a query node or an array of projectile
/// positions.
/// </summary>
/// <remarks>
/// <para>The <see cref="Resolve"/> method returns the first element of the array (or a default <see cref="Variant128"/>
/// if the array is empty). Use <see cref="GetElement"/> and <see cref="SetElement"/> for indexed access.</para>
/// <para>At graph initialization time, a fresh copy of the array is created for each execution instance so that
/// multiple processors sharing the same graph have independent array state.</para>
/// </remarks>
/// <param name="initialValues">The initial values for the array elements.</param>
/// <param name="elementType">The type of each element in the array.</param>
public class ArrayVariableResolver(Variant128[] initialValues, Type elementType) : IPropertyResolver
{
	private readonly List<Variant128> _values = [.. initialValues];

	/// <inheritdoc/>
	public Type ValueType { get; } = elementType;

	/// <summary>
	/// Gets the number of elements in the array.
	/// </summary>
	public int Length => _values.Count;

	/// <summary>
	/// Gets a read-only view of the current array values.
	/// </summary>
	public ReadOnlyCollection<Variant128> Values => _values.AsReadOnly();

	/// <inheritdoc/>
	/// <remarks>
	/// Returns the first element of the array, or a default <see cref="Variant128"/> if the array is empty.
	/// </remarks>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return _values.Count > 0 ? _values[0] : default;
	}

	/// <summary>
	/// Gets the value at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the element to get.</param>
	/// <returns>The value at the specified index.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
	public Variant128 GetElement(int index)
	{
		return _values[index];
	}

	/// <summary>
	/// Sets the value at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the element to set.</param>
	/// <param name="value">The value to set.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
	public void SetElement(int index, Variant128 value)
	{
		_values[index] = value;
	}

	/// <summary>
	/// Appends a value to the end of the array.
	/// </summary>
	/// <param name="value">The value to add.</param>
	public void Add(Variant128 value)
	{
		_values.Add(value);
	}

	/// <summary>
	/// Removes the element at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the element to remove.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
	public void RemoveAt(int index)
	{
		_values.RemoveAt(index);
	}

	/// <summary>
	/// Removes all elements from the array.
	/// </summary>
	public void Clear()
	{
		_values.Clear();
	}
}
