// Copyright © Gamesmiths Guild.

using System.Runtime.CompilerServices;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking.Replication;

// Tells entities apart by reference, whatever equality their host classes define.
internal sealed class EntityReferenceComparer : IEqualityComparer<IForgeEntity>
{
	public static readonly EntityReferenceComparer Instance = new();

	public bool Equals(IForgeEntity? x, IForgeEntity? y)
	{
		return ReferenceEquals(x, y);
	}

	public int GetHashCode(IForgeEntity obj)
	{
		return RuntimeHelpers.GetHashCode(obj);
	}
}
