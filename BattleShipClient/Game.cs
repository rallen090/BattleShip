using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BattleShipClient.Commanders;
using BattleShipClient.Protocol;
using BattleShipClient.Utilities;

namespace BattleShipClient
{
	/// <summary>
	/// Instance of a BattleShip game
	/// </summary>
	public class Game
	{
		private readonly TcpConnection _connection;
		private readonly ICommander _commander;
		private Cell[,] _grid;
		private List<Ship> _ships;
		private int _shotCount;
		private int _hitCount;
		private int _missCount;
		private bool _victory;

		public Game(TcpConnection connection, ICommander commander)
		{
			this._connection = connection;
			this._commander = commander;
		}

		public async Task<GameResult> PlayAsync()
		{
			Log.DebugLine("Starting new game...");

			// connect
			await this._connection.ConnectAsync();

			// initialize game
			await this.InitializeAsync();

			// play
			await PlayToEndAsync();

			this.PrintResults();

			return new GameResult
			{
				Shots = this._shotCount,
				Hits = this._hitCount,
				Misses = this._missCount,
				Victory = this._victory
			};
		}

		private async Task InitializeAsync()
		{
			// wait for OK and then instantiate grid
			Log.DebugLine("Waiting for OK message to begin...");
			ResponseMessage message;
			while ((message = await this.GetNextResponse()).Type != ResponseMessageType.Ok) { }

			// initialize grid
			var n = message.GridSize.Value;
			this._grid = new Cell[n,n];
			var count = 0;
			for (var y = 0; y < n; y++)
			{
				for (var x = 0; x < n; x++)
				{
					this._grid[y, x] = new Cell
					{
						// incrementing count here as we traverse the grid
						TargetLocation = count++,
						X = x,
						Y = y,
						State = CellState.Hidden
					};
				}
			}

			// initialize ships
			this._ships = ServerGame.InitializeShips();

			Log.DebugLine($"Game initialized: {n}x{n} grid");
		}

		private async Task PlayToEndAsync()
		{
			int move = -1;
			while (true)
			{
				//Log.DebugLine("Waiting for response...");
				var message = await this.GetNextResponse();
				switch (message.Type)
				{
					case ResponseMessageType.Shoot:
						move = this._commander.GetNextTarget(this._grid, this._ships);
						Log.DebugLine($"Shooting: {move}");
						this.WriteNextMove(move);
						this._shotCount++;
						break;
					case ResponseMessageType.Hit:
						Log.DebugLine("HIT");
						this._grid.Cast<Cell>().Single(c => c.TargetLocation == move).State = CellState.Hit;
						this._hitCount++;
						break;
					case ResponseMessageType.Miss:
						Log.DebugLine("MISS");
						this._grid.Cast<Cell>().Single(c => c.TargetLocation == move).State = CellState.Miss;
						this._missCount++;
						break;
					case ResponseMessageType.Sunk:
						var shipId = message.Value.Value;
						Log.DebugLine($"HIT - {this._ships.Single(s => s.Id == shipId).Name} has sunk");
						this._grid.Cast<Cell>().Single(c => c.TargetLocation == move).State = CellState.Hit;
						this._ships.Single(s => s.Id == shipId).Sunk = true;
						this._hitCount++;
						break;
					case ResponseMessageType.Win:
						this._victory = true;
						Console.ForegroundColor = ConsoleColor.Green;
						Log.DebugLine("Victory!");
						Console.ResetColor();
						return;
					case ResponseMessageType.Lose:
						Console.ForegroundColor = ConsoleColor.DarkRed;
						Log.DebugLine("Defeat!");
						Console.ResetColor();
						return;
					case ResponseMessageType.Error:
						Log.DebugLine($"TcpServer encountered an error: '{message.FullMessage}'");
						return;
					case ResponseMessageType.Unknown:
						Log.DebugLine($"Ignoring unknown command: '{message.FullMessage}'");
						return;
					default:
						throw new Exception($"Unexpected response message: {message.Type.ToString()}");
				}
			}
		}

		private async Task<ResponseMessage> GetNextResponse()
		{
			return MessageEncoder.DecodeResponseMessage(await this._connection.GetNextMessageAsync());
		}

		private void WriteNextMove(int targetLocation)
		{
			this._connection.WriteMessage(MessageEncoder.EncodeActionMessage(new ActionMessage { TargetCell = targetLocation }));
		}

		private void PrintResults()
		{
			Log.DebugLine($"Shots: {this._shotCount} Hits: {this._hitCount} Misses: {this._missCount}");
			Log.DebugLine("Game complete");
		}
	}

	public class Cell
	{
		// protocol specifies location w/ the cell number 0->n^2, rather than {x,y} coordinates, so we store this as well for easy lookups
		public int TargetLocation { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public CellState State {  get; set; }

		// used by server
		public int? ShipId { get; set; }
	}

	public enum CellState
	{
		Hidden = 1,
		Hit = 2,
		Miss = 3
	}

	public class Ship
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public int Length { get; set; }
		public bool Sunk { get; set; }
	}

	public class GameResult
	{
		public bool Victory { get; set; }
		public int Shots { get; set; }
		public int Hits { get; set; }
		public int Misses { get; set; }
		public int PlayerId { get; set; }
	}
}
