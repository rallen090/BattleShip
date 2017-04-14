using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using BattleShipClient.Commanders;
using BattleShipClient.Protocol;
using BattleShipClient.Utilities;
using Medallion.Shell;

namespace BattleShipClient
{
	public class GameRunner : IDisposable
	{
		private readonly IPAddress _ipAddress;
		private readonly int _port;
		private readonly Func<ICommander> _commanderFactory;
		private static readonly List<Command> Processes = new List<Command>();
		private static readonly List<TcpServer> TcpServers = new List<TcpServer>();

		public GameRunner(IPAddress ipAddress, int port, Func<ICommander> commanderFactory)
		{
			this._ipAddress = ipAddress;
			this._port = port;
			this._commanderFactory = commanderFactory;
		}

		public async Task<GameResult> RunOnlyClientAsync(int? portOverride = null, Func<ICommander> commanderOverride = null)
		{
			using (var connection = new TcpConnection(this._ipAddress, portOverride ?? this._port))
			{
				var game = new Game(connection, commanderOverride != null ? commanderOverride() : this._commanderFactory());
				return await game.PlayAsync();
			}
		}

		public async Task<List<GameResult>> RunWithServerAsync(ThreadSafeResultList results, int trials, int? portOverride = null, bool useTclServer = false, bool useTclClient = false)
		{
			Log.DebugLine($"Running games with server and client - {trials} total games");
			List<GameResult> allTrialResults = new List<GameResult>();
			for (var i = 0; i < trials; i++)
			{
				Log.DebugLine($"Running game {i}...");
				var result = await RunOnceWithServer(results, useTclServer, useTclClient, portOverride);
				allTrialResults.AddRange(result);
			}
			return allTrialResults;
		}

		public async Task<List<GameResult>> RunWithManyServersAsync(ThreadSafeResultList results, int trials, int servers, bool useTclServer, bool useTclClient)
		{
			const int portBase = 9900;
			var runningServers = Enumerable.Range(0, servers)
				.Select(i => this.RunWithServerAsync(results, trials, portOverride: portBase + i, useTclServer: useTclServer, useTclClient: useTclClient))
				.ToArray();
			await Task.WhenAll(runningServers);
			return runningServers.SelectMany(s => s.Result).ToList();
		}

		private async Task<List<GameResult>> RunOnceWithServer(ThreadSafeResultList results, bool useTclServer, bool useTclClient, int? portOverride = null)
		{
			Command tclServer = null;
			Command tclClient = null;
			TcpServer tcpServer = null;
			try
			{
				if (useTclServer)
				{
					var tclPath = Path.Combine(Directory.GetCurrentDirectory(), "tclkit852.exe");
					var tclServerPath = Path.Combine(Directory.GetCurrentDirectory(), "bs_server.tcl");
					var serverArguments = new List<string> { File.Exists(tclServerPath) ? tclServerPath : @"C:\dev\BattleShip\bs_server.tcl" };
					if (portOverride != null)
					{
						serverArguments.Add(portOverride.ToString());
					}
					var tclExecutablePath = File.Exists(tclPath) ? tclPath : @"C:\dev\BattleShip\tclkit852.exe";
					tclServer = Command.Run(tclExecutablePath,
						arguments: serverArguments,
						options: o => o.StartInfo(i => i.CreateNoWindow = false)
							.StartInfo(i => i.RedirectStandardError = false)
							.StartInfo(i => i.RedirectStandardInput = false)
							.StartInfo(i => i.RedirectStandardOutput = false)
							.StartInfo(i => i.UseShellExecute = true)
					);

					lock (Processes)
					{
						Processes.Add(tclServer);
					}
				}
				else
				{
					tcpServer = new TcpServer(this._ipAddress, portOverride ?? this._port);
					tcpServer.Connect();
					lock (TcpServers)
					{
						TcpServers.Add(tcpServer);
					}
				}

				// wait for the server to start
				await Task.Delay(100);

				var clientTask1 = this.RunOnlyClientAsync(portOverride);

				Task<GameResult> clientTask2 = Task.FromResult(new GameResult { PlayerId = 2});
				
				if (useTclClient)
				{
					var tclPath = Path.Combine(Directory.GetCurrentDirectory(), "tclkit852.exe");
					var tclClientPath = Path.Combine(Directory.GetCurrentDirectory(), "bs_client.tcl");
					var serverArguments = new List<string> { File.Exists(tclClientPath) ? tclClientPath : @"C:\dev\BattleShip\bs_client123.tcl" };
					if (portOverride != null)
					{
						serverArguments.Add(portOverride.ToString());
					}
					var tclExecutablePath = File.Exists(tclPath) ? tclPath : @"C:\dev\BattleShip\tclkit852.exe";
					tclClient = Command.Run(tclExecutablePath,
						arguments: serverArguments,
						options: o => o.StartInfo(i => i.CreateNoWindow = false)
							.StartInfo(i => i.RedirectStandardError = false)
							.StartInfo(i => i.RedirectStandardInput = false)
							.StartInfo(i => i.RedirectStandardOutput = false)
							.StartInfo(i => i.UseShellExecute = true)
					);

					lock (Processes)
					{
						Processes.Add(tclClient);
					}
				}
				else
				{
					clientTask2 = this.RunOnlyClientAsync(portOverride);
				}
				
				await Task.WhenAll(clientTask1, clientTask2);

				tclClient?.Kill();
				tcpServer?.Dispose();

				var p1Result = clientTask1.Result;
				var p2Result = clientTask2.Result;
				p1Result.PlayerId = 1;
				p2Result.PlayerId = 2;
				p2Result.Victory = !p1Result.Victory;

				results.Add(Tuple.Create(p1Result, p2Result));

				return new List<GameResult> {p1Result, p2Result };
			}
			finally
			{
				// make sure we kill this so we don't need to in task manager (thanks tcl...)
				tclServer?.Kill();
				tclClient?.Kill();
			}
		}

		public static void KillServers()
		{
			lock (Processes)
			{
				Processes.ForEach(s => s?.Kill());
				Processes.Clear();
			}
			lock (TcpServers)
			{
				TcpServers.ForEach(s => s?.Dispose());
				TcpServers.Clear();
			}
		}

		public void Dispose()
		{
			KillServers();
		}
	}
}
