// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// How the custom data of one type that effects are applied with travels, so a client's copy of the effect holds it
/// too. Registered in the <see cref="NetworkAssetRegistry"/> under <see cref="AssetKind.EffectContext"/>, on every
/// machine. Data of a type nothing is registered for stays on the server.
/// </summary>
public sealed class EffectContextCodec
{
	private readonly Action<BitWriter, EffectApplicationContext> _write;
	private readonly Func<BitReader, EffectApplicationContext> _read;

	internal Type ContextType { get; }

	private EffectContextCodec(
		Type contextType,
		Action<BitWriter, EffectApplicationContext> write,
		Func<BitReader, EffectApplicationContext> read)
	{
		ContextType = contextType;
		_write = write;
		_read = read;
	}

	/// <summary>
	/// Creates the codec of a type.
	/// </summary>
	/// <typeparam name="TData">The type effects are applied with, as in
	/// <see cref="EffectsManager.ApplyEffect{TData}(Effect, TData)"/>.</typeparam>
	/// <param name="write">Writes the data.</param>
	/// <param name="read">Reads the data, following the same sequence of calls as <paramref name="write"/>. A packet is
	/// untrusted, so what can't be right throws a <see cref="MalformedPacketException"/>.</param>
	/// <returns>The codec.</returns>
	public static EffectContextCodec For<TData>(Action<BitWriter, TData> write, Func<BitReader, TData> read)
	{
		return new EffectContextCodec(
			typeof(EffectApplicationContext<TData>),
			(writer, context) => write(writer, ((EffectApplicationContext<TData>)context).Data),
			reader => new EffectApplicationContext<TData>(read(reader)));
	}

	internal void Write(BitWriter writer, EffectApplicationContext context)
	{
		_write(writer, context);
	}

	internal EffectApplicationContext Read(BitReader reader)
	{
		return _read(reader);
	}
}
