// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Represents the array element currently being iterated by an array resolver. Iterating resolvers (e.g. filter, sort,
/// and projection resolvers) publish one frame per element on the <see cref="GraphContext"/> element stack while
/// evaluating their nested "lambda" resolvers, and element resolvers read the value back through
/// <see cref="GraphContext.TryGetCurrentElement"/>.
/// </summary>
public readonly struct ElementFrame
{
	/// <summary>
	/// Gets the element value when iterating a value-typed array. Holds a default value when the frame represents an
	/// object-backed element.
	/// </summary>
	public Variant128 Value { get; }

	/// <summary>
	/// Gets the element value when iterating an object-backed array. Holds <see langword="null"/> when the frame
	/// represents a value-typed element.
	/// </summary>
	public object? ObjectValue { get; }

	/// <summary>
	/// Gets the type of the iterated array's elements.
	/// </summary>
	public Type ElementType { get; }

	/// <summary>
	/// Gets the zero-based index of the element within the iterated array.
	/// </summary>
	public int Index { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ElementFrame"/> struct for a value-typed array element.
	/// </summary>
	/// <param name="value">The element value.</param>
	/// <param name="elementType">The type of the element.</param>
	/// <param name="index">The zero-based index of the element within the iterated array.</param>
	public ElementFrame(Variant128 value, Type elementType, int index)
	{
		Value = value;
		ObjectValue = null;
		ElementType = elementType;
		Index = index;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ElementFrame"/> struct for an object-backed array element.
	/// </summary>
	/// <param name="objectValue">The element value.</param>
	/// <param name="elementType">The type of the element.</param>
	/// <param name="index">The zero-based index of the element within the iterated array.</param>
	public ElementFrame(object? objectValue, Type elementType, int index)
	{
		Value = default;
		ObjectValue = objectValue;
		ElementType = elementType;
		Index = index;
	}
}
