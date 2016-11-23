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
					defaultValue: 5)
				.ArgumentWithFlag("servers",
					flag: "servers",
					parser: int.Parse,
					defaultValue: 1)
				.ArgumentWithFlag("tcl",
					flag: "tcl",
					parser: bool.Parse,
					defaultValue: true)
				.Build(args);

			Console.WriteLine(arguments.ToConsoleString());
			Console.ReadLine();

			var ipAddress = arguments.GetByName<IPAddress>("ip").Value;
			var port = arguments.GetByName<int>("port").Value;
			var commanderType = arguments.GetByName<CommanderType>("commander").Value;
			var trials = arguments.GetByName<int>("n").Value;
			var serverCount = arguments.GetByName<int>("servers").Value;
			var tcl = arguments.GetByName<bool>("tcl").Value;

			var start = DateTimeOffset.Now;

			// run games
			using (var runner = new GameRunner(ipAddress, port, CommanderTypeSelector.GetCommanderFactory(commanderType)))
			{
				var results = runner.RunWithManyServersAsync(trials: trials, servers: serverCount, useTcpServer: !tcl).Result;
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
			var player1 = results.Select(((result, i) => new {result, i})).Where(s => s.i % 2 == 0).Select(s => s.result).ToList();
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"{wins.Count} games complete!");
			Console.WriteLine($"Shots  - Avg: {wins.Average(w => w.Shots):00} | Min: {wins.Min(w => w.Shots.ToString("00"))} | Max: {wins.Max(w => w.Shots.ToString("00"))}");
			Console.WriteLine($"Hits   - Avg: {wins.Average(w => w.Hits):00} | Min: {wins.Min(w => w.Hits.ToString("00"))} | Max: {wins.Max(w => w.Hits.ToString("00"))}");
			Console.WriteLine($"Misses - Avg: {wins.Average(w => w.Misses):00} | Min: {wins.Min(w => w.Misses.ToString("00"))} | Max: {wins.Max(w => w.Misses.ToString("00"))}");
			Console.WriteLine($"Player 1 win rate: {player1.Count(s => s.Victory)}/{wins.Count}");
			Console.WriteLine($"Player 2 win rate: {wins.Except(player1).Count()}/{wins.Count}");
			Console.ResetColor();
		}
	}
}
