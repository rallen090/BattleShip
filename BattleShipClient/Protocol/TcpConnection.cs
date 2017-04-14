using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using BattleShipClient.Utilities;

namespace BattleShipClient.Protocol
{
	/// <summary>
	/// Manages reading from and writing to a <see cref="TcpClient"/> connection
	/// </summary>
	public class TcpConnection : IDisposable
	{
		private readonly TimeSpan _ioDelay = TimeSpan.FromMilliseconds(20);
		private readonly IPAddress _ipAddress;
		private readonly int _port;
		private TcpClient _tcpClient;
		private readonly CancellationTokenSource _cancellationSource;
		private Task _asyncProcessor;
		private readonly Queue<string> _inputMessages = new Queue<string>();
		private readonly Queue<string> _outputMessages = new Queue<string>();

		public TcpConnection(IPAddress ipAddress, int port)
		{
			this._ipAddress = ipAddress;
			this._port = port;
			this._cancellationSource = new CancellationTokenSource();
		}

		public async Task ConnectAsync()
		{
			if (this._tcpClient == null)
			{
				Log.DebugLine($"Connecting to TCP server at '{this._ipAddress}:{this._port}'...");
				this._tcpClient = new TcpClient();

				try
				{
					await this._tcpClient.ConnectAsync(this._ipAddress, this._port);
				}
				catch (Exception ex)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Log.DebugLine($"Failed to connect to a TCP server at '{this._ipAddress}:{this._port}'. Ensure that there is an active server hosted there. \nFull error details: \n{ex}");
					Console.ForegroundColor = ConsoleColor.White;
					Log.DebugLine("Press enter to exit...");
					// wait for enter before exiting
					Console.ReadLine();
					Environment.Exit(1);
				}
				Log.DebugLine($"Connected to '{this._ipAddress}:{this._port}'");

				// kick off async reader/writer
				this._asyncProcessor = this.ReadWriteAsync(this._tcpClient, this._cancellationSource.Token);
			}
		}

		public async Task<string> GetNextMessageAsync()
		{
			this.ThrowIfNotConnected();

			string message;
			while (true)
			{
				lock (this._inputMessages)
				{
					if (this._inputMessages.Count > 0)
					{
						message = this._inputMessages.Dequeue();
						break;
					}
				}
				await Task.Delay(500);
			}

			return message;
		}

		public void WriteMessage(string message)
		{
			this.ThrowIfNotConnected();

			lock (this._outputMessages)
			{
				this._outputMessages.Enqueue(message);
			}
		}

		#region ---- Async IO ----

		private async Task ReadWriteAsync(TcpClient tcpClient, CancellationToken token)
		{
			try
			{
				using (var stream = tcpClient.GetStream())
				using (var reader = new StreamReader(stream))
				using (var writer = new StreamWriter(stream))
				{
					// make separate read/write tasks
					var readTask = this.ReadAsync(reader, token);
					var writerTask = this.WriteAsync(writer, token);

					// wait for them to exit (via the token)
					await Task.WhenAll(readTask, writerTask);
				}
			}
			// ignore if it gets canceled
			catch (TaskCanceledException) { }
			catch (Exception ex)
			{
				Log.DebugLine("Error reading/writing: " + ex);
			}
			Log.DebugLine("Connection stream closed");
		}

		private async Task ReadAsync(StreamReader reader, CancellationToken token)
		{
			Log.DebugLine("Reading stream asynchronously...");

			while (!token.IsCancellationRequested)
			{
				// read if there is content
				string inputMessage;
				if (this._outputMessages.Count == 0 && !string.IsNullOrWhiteSpace(inputMessage = await reader.ReadLineAsync().WithCancellation(token)))
				{
					lock (this._inputMessages)
					{
						this._inputMessages.Enqueue(inputMessage);
					}
				}

				// delay for more content
				await Task.Delay(this._ioDelay, token);
			}
		}

		private async Task WriteAsync(StreamWriter writer, CancellationToken token)
		{
			Log.DebugLine("Writing stream asynchronously...");

			while (!token.IsCancellationRequested)
			{
				// write if there is content
				var messagesToWrite = new List<string>();
				lock (this._outputMessages)
				{
					while (this._outputMessages.Count > 0)
					{
						messagesToWrite.Add(this._outputMessages.Dequeue());
					}
				}
				foreach (var message in messagesToWrite)
				{
					await writer.WriteLineAsync(message);
					await writer.FlushAsync();
				}

				// delay for more content
				await Task.Delay(this._ioDelay, token);
			}
		}

		#endregion 

		public void Dispose()
		{
			// disconnect if we have a connection
			Log.DebugLine("Shutting down...");
			this._cancellationSource.Cancel();
			this._asyncProcessor.Wait(TimeSpan.FromSeconds(10));
			this._tcpClient.Close();
			this._cancellationSource.Dispose();
			Log.DebugLine("Successfully shut down");
		}

		private void ThrowIfNotConnected()
		{
			if (this._tcpClient == null)
			{
				throw new ServerException($"{typeof(TcpConnection).Name} has not connected to a server. You must call Connect() before reading/writing messages.");
			}
		}
	}
}
