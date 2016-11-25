using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BattleShipClient.Commanders;
using BattleShipClient.Utilities;

namespace BattleShipClient
{
	public class Program
	{
		public static void Main(string[] args)
		{
			// set console to terminate the server if running
			ShutdownHelper.SetConsoleCtrlHandler(ShutdownHelper.ConsoleCtrlCheck, add: true);

			Console.WriteLine("---- BattleShip Client ----");
			Console.WriteLine("Version: 1.0.0");
			Console.WriteLine();

			// read inputs if applicable, otherwise default to local connection settings
			var arguments = new CommandLineArgumentsBuilder()
				.ArgumentWithFlag("ip", 
					flag: "ip", 
					parser: IPAddress.Parse,
					// localhost (127.0.0.1)
					defaultValue: IPAddress.Loopback)
				.ArgumentWithFlag("port", 
					flag: "port", 
					parser: int.Parse,
					// default port
					defaultValue: 9900)
				.ArgumentWithFlag("commander", 
					flag: "commander", 
					parser: (s) => (CommanderType)int.Parse(s), 
					defaultValue: CommanderType.ProbabilityCommander)
				.ArgumentWithFlag("n",
					flag: "n",
					parser: int.Parse,
					defaultValue: 1)
				.ArgumentWithFlag("servers",
					flag: "servers",
					parser: int.Parse,
					defaultValue: 4)
				.ArgumentWithFlag("tclServer",
					flag: "tclServer",
					parser: bool.Parse,
					defaultValue: true)
				.ArgumentWithFlag("tclClient",
					flag: "tclClient",
					parser: bool.Parse,
					defaultValue: true)
				.Build(args);

			Console.WriteLine(arguments.ToConsoleString());
			Console.WriteLine("Press enter to start! (exit terminal or CRTL+C to terminate)");
			Console.ReadLine();

			var ipAddress = arguments.GetByName<IPAddress>("ip").Value;
			var port = arguments.GetByName<int>("port").Value;
			var commanderType = arguments.GetByName<CommanderType>("commander").Value;
			var trials = arguments.GetByName<int>("n").Value;
			var serverCount = arguments.GetByName<int>("servers").Value;
			var tclServer = arguments.GetByName<bool>("tclServer").Value;
			var tclClient = arguments.GetByName<bool>("tclClient").Value;

			var start = DateTimeOffset.Now;

			// run games
			using (var runner = new GameRunner(ipAddress, port, CommanderTypeSelector.GetCommanderFactory(commanderType)))
			{
				var results = runner.RunWithManyServersAsync(trials: trials, servers: serverCount, useTclServer: tclServer, useTclClient: tclClient).Result;
				PrintResults(results);
			}

			Console.WriteLine($"Duration: {DateTimeOffset.Now - start}");

			Console.WriteLine("Press enter to exit...");
			Console.ReadLine();
			Console.WriteLine("Exiting...");
		}

		private static void PrintResults(List<GameResult> results)
		{
			var wins = results.Where(r => r.Victory).ToList();
			var groupedResults = results.GroupBy(r => r.PlayerId).ToDictionary(s => s.Key, s => s);
			var player1 = groupedResults[1];
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"{wins.Count} games complete!");
			Console.WriteLine($"Shots  - Avg: {player1.Average(w => w.Shots):00} | Min: {player1.Min(w => w.Shots.ToString("00"))} | Max: {player1.Max(w => w.Shots.ToString("00"))}");
			Console.WriteLine($"Hits   - Avg: {player1.Average(w => w.Hits):00} | Min: {player1.Min(w => w.Hits.ToString("00"))} | Max: {player1.Max(w => w.Hits.ToString("00"))}");
			Console.WriteLine($"Misses - Avg: {player1.Average(w => w.Misses):00} | Min: {player1.Min(w => w.Misses.ToString("00"))} | Max: {player1.Max(w => w.Misses.ToString("00"))}");
			Console.WriteLine($"Player 1 win rate: {groupedResults[1].Count(s => s.Victory)}/{wins.Count}");
			Console.WriteLine($"Player 2 win rate: {groupedResults[2].Count(s => s.Victory)}/{wins.Count}");
			Console.ResetColor();
		}

		private static void PrintPlayerResults(IGrouping<int, GameResult> resultsForPlayer)
		{
			Console.WriteLine();
			Console.WriteLine($"Shots  - Avg: {resultsForPlayer.Average(w => w.Shots):00} | Min: {resultsForPlayer.Min(w => w.Shots.ToString("00"))} | Max: {resultsForPlayer.Max(w => w.Shots.ToString("00"))}");
			Console.WriteLine($"Hits   - Avg: {resultsForPlayer.Average(w => w.Hits):00} | Min: {resultsForPlayer.Min(w => w.Hits.ToString("00"))} | Max: {resultsForPlayer.Max(w => w.Hits.ToString("00"))}");
			Console.WriteLine($"Misses - Avg: {resultsForPlayer.Average(w => w.Misses):00} | Min: {resultsForPlayer.Min(w => w.Misses.ToString("00"))} | Max: {resultsForPlayer.Max(w => w.Misses.ToString("00"))}");
		}
	}
}
