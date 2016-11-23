using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using BattleShipClient.Commanders;
using BattleShipClient.Protocol;
using Medallion.Shell;

namespace BattleShipClient
{
	public class GameRunner : IDisposable
	{
		private readonly IPAddress _ipAddress;
		private readonly int _port;
		private readonly Func<ICommander> _commanderFactory;
		private static readonly List<Command> Servers = new List<Command>();
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

		public async Task<List<GameResult>> RunWithServerAsync(int trials, int? portOverride = null, bool useTcpServer = false)
		{
			Console.WriteLine($"Running games with server and client - {trials} total games");
			List<GameResult> allTrialResults = new List<GameResult>();
			for (var i = 0; i < trials; i++)
			{
				Console.WriteLine($"Running game {i}...");
				var result = await RunOnceWithServer(portOverride, useTcpServer);
				allTrialResults.AddRange(result);
			}
			return allTrialResults;
		}

		public async Task<List<GameResult>> RunWithManyServersAsync(int trials, int servers, bool useTcpServer = false)
		{
			const int portBase = 9900;
			var runningServers = Enumerable.Range(0, servers)
				.Select(i => Task.Run(() => this.RunWithServerAsync(trials, portOverride: portBase + i, useTcpServer: useTcpServer)))
				.ToArray();
			await Task.WhenAll(runningServers);
			return runningServers.SelectMany(s => s.Result).ToList();
		}

		private async Task<List<GameResult>> RunOnceWithServer(int? portOverride = null, bool useTcpServer = false)
		{
			Command server = null;
			TcpServer tcpServer = null;
			try
			{
				if (!useTcpServer)
				{
					var serverArguments = new List<string> { @"C:\dev\BattleShip\bs_server.tcl" };
					if (portOverride != null)
					{
						serverArguments.Add(portOverride.ToString());
					}
					var path = Path.Combine(Directory.GetCurrentDirectory(), "tclkit852.exe");
					var tclExecutablePath = File.Exists(path) ? path : @"C:\dev\BattleShip\tclkit852.exe";
					server = Command.Run(tclExecutablePath,
						arguments: serverArguments,
						options: o => o.StartInfo(i => i.CreateNoWindow = false)
							.StartInfo(i => i.RedirectStandardError = false)
							.StartInfo(i => i.RedirectStandardInput = false)
							.StartInfo(i => i.RedirectStandardOutput = false)
							.StartInfo(i => i.UseShellExecute = true)
					);

					lock (Servers)
					{
						Servers.Add(server);
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
				var clientTask2 = this.RunOnlyClientAsync(portOverride, () => new RandomCommander());
				await Task.WhenAll(clientTask1, clientTask2);

				server?.Kill();
				tcpServer?.Dispose();
				return new List<GameResult> {clientTask1.Result, clientTask2.Result};
			}
			finally
			{
				// make sure we kill this so we don't need to in task manager (thanks tcl...)
				server?.Kill();
			}
		}

		public static void KillServers()
		{
			lock (Servers)
			{
				Servers.ForEach(s => s?.Kill());
				Servers.Clear();
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
