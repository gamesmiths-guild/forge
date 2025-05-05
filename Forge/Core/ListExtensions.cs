// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

internal static class ListExtensions
{
	/// <summary>
	/// Adds an element only if they're not already present in the List. If already present will return -1; otherwise
	/// will return the index where it has been added.
	/// </summary>
	/// <typeparam name="T">Generic type for the list.</typeparam>
	/// <param name="list">The list itself.</param>
	/// <param name="element">The element to be added.</param>
	/// <returns>The index where the element has been added; or -1 otherwise.</returns>
	internal static int AddUnique<T>(this List<T> list, T element)
	{
		if (!list.Contains(element))
		{
			list.Add(element);
			return list.Count - 1;
		}

		return list.IndexOf(element);
	}
}
