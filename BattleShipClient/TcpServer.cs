using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleShipClient.Protocol;
using BattleShipClient.Utilities;

namespace BattleShipClient
{
	public class TcpServer : IDisposable
	{
		private readonly CancellationTokenSource _cancellationSource;
		private readonly IPAddress _ipAddress;
		private readonly int _port;
		private TcpListener _server;
		private Task _serverTask;

		public TcpServer(IPAddress ipAddress, int port)
		{
			this._ipAddress = ipAddress;
			this._port = port;
			this._cancellationSource = new CancellationTokenSource();
		}

		public void Connect()
		{
			this._server = new TcpListener(this._ipAddress, this._port);
			this._server.Start();

			this._serverTask = Task.Run(async () =>
			{
				using (var client1 = this._server.AcceptTcpClient())
				using (var client2 = this._server.AcceptTcpClient())
				using (var r1 = new StreamReader(client1.GetStream()))
				using (var r2 = new StreamReader(client2.GetStream()))
				using (var w1 = new StreamWriter(client1.GetStream()))
				using (var w2 = new StreamWriter(client2.GetStream()))
				{
					var game1 = new ServerGame(10);
					var game2 = new ServerGame(10);
					var t1 = Task.Run(async () =>
					{
						await w1.WriteLineAsync("OK " + string.Join(" ", Enumerable.Range(0, 200)));
						await w1.FlushAsync();
						await w2.WriteLineAsync("OK " + string.Join(" ", Enumerable.Range(0, 200)));
						await w2.FlushAsync();

						while (!this._cancellationSource.Token.IsCancellationRequested)
						{
							var m1 = game2.Won() ? "LOSE" : $"SHOOT {game1.ToMapString()} {game2.ToMapString()}";
							await w1.WriteLineAsync(m1);
							await w1.FlushAsync();
								
							string l1;
							while(string.IsNullOrWhiteSpace(l1 = await r1.ReadLineAsync().WithCancellation(this._cancellationSource.Token))) { }

							var response1 = game1.Shoot(int.Parse(l1.Trim()));
							var responseString1 = MessageEncoder.EncodeResponseMessage(response1);

							await w1.WriteLineAsync(responseString1);
							await w1.FlushAsync();

							var m2 = game1.Won() ? "LOSE" : $"SHOOT {game2.ToMapString()} {game1.ToMapString()}";
							await w2.WriteLineAsync(m2);
							await w2.FlushAsync();

							string l2;
							while (string.IsNullOrWhiteSpace(l2 = await r2.ReadLineAsync().WithCancellation(this._cancellationSource.Token))) { }

							var response2 = game2.Shoot(int.Parse(l2.Trim()));
							var responseString2 = MessageEncoder.EncodeResponseMessage(response2);

							await w2.WriteLineAsync(responseString2);
							await w2.FlushAsync();
						}
					});
					//var t2 = Task.Run(async () =>
					//{
					//	await w2.WriteLineAsync("OK " + string.Join(" ", Enumerable.Range(0, 200)));
					//	await w2.FlushAsync();
					//	int count = 0;
					//	while (!this._cancellationSource.Token.IsCancellationRequested)
					//	{
					//		await w2.WriteLineAsync(count == 45 ? "LOSE" : "SHOOT");
					//		await w2.FlushAsync();
					//		var l = await r2.ReadLineAsync().WithCancellation(this._cancellationSource.Token);
					//		await w2.WriteLineAsync("MISS");
					//		await w2.FlushAsync();
					//		count++;
					//	}
					//});
					//await Task.WhenAll(t1, t2);
					await t1;
				}
			}, this._cancellationSource.Token);
		}

		public void Dispose()
		{
			this._cancellationSource.Cancel();
			this._server.Stop();
		}
	}
}
