// Copyright © Gamesmiths Guild.

using BenchmarkDotNet.Running;

namespace Gamesmiths.Forge.Benchmarks;

internal static class Program
{
#pragma warning disable IDE0210 // Convert to top-level statements
	private static void Main(string[] args)
#pragma warning restore IDE0210 // Convert to top-level statements
	{
		BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
	}
}
