using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleShipClient.Commanders;
using BattleShipClient.Protocol;
using BattleShipClient.Utilities;

namespace BattleShipClient
{
	public class ServerGame
	{
		private readonly int _gridLength;
		private readonly Cell[,] _grid;
		private readonly List<Ship> _ships = InitializeShips();
		private readonly Dictionary<int, ISet<int>> _shipHitsById = new Dictionary<int, ISet<int>>();
		private List<Cell> _shipCells;

		private bool _won;

		public ServerGame(int gridLength)
		{
			this._gridLength = gridLength;
			this._grid = new Cell[gridLength, gridLength];
			this._ships.ForEach(s => this._shipHitsById.Add(s.Id, new HashSet<int>()));

			this.Initialize();
		}

		public string ToMapString()
		{
			return string.Join(" ", this._grid.Cast<Cell>()
				.Select(s => s.State != CellState.Hit ? (s.State == CellState.Hidden ? 0 : -99) : s.ShipId));
		}

		private void Initialize()
		{
			var count = 0;
			for (var y = 0; y < this._gridLength; y++)
			{
				for (var x = 0; x < this._gridLength; x++)
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

			this.RandomizeShipLocations();

			this._shipCells = this._grid.Cast<Cell>().Where(c => c.ShipId != null).Distinct().ToList();
			Throw.IfNull(this._shipCells.Count != 17, "Invalid ship placement!");
		}

		public ResponseMessage Shoot(int targetLocation)
		{
			Cell hitCell;
			if ((hitCell = this._shipCells.SingleOrDefault(c => c.TargetLocation == targetLocation)) != null)
			{
				var shipId = hitCell.ShipId.Value;
				var hitSpots = this._shipHitsById[shipId];
				hitSpots.Add(targetLocation);

				if (this._shipHitsById.Values.SelectMany(s => s).Count() == 17)
				{
					this._won = true;
					return new ResponseMessage { Type = ResponseMessageType.Win };
				}

				// sunk if hit all spots
				if (hitSpots.Count == this._ships.Single(s => s.Id == shipId).Length)
				{
					return new ResponseMessage { Type = ResponseMessageType.Sunk, Value = shipId };
				}

				// otherwise, hit
				return new ResponseMessage { Type = ResponseMessageType.Hit };
			}

			return new ResponseMessage { Type = ResponseMessageType.Miss };
		}

		public bool Won()
		{
			return this._won;
		}

		private void RandomizeShipLocations()
		{
			var random = new Random();
			foreach (var ship in this._ships.OrderByDescending(s => s.Length))
			{
				Orientation orientation;
				int x, y;
				do
				{
					orientation = (Orientation)random.Next(minValue: 0, maxValue: 1);
					var yMax = orientation == Orientation.Vertical ? this._grid.GetLength(0) - ship.Length : this._grid.GetLength(0) - 1;
					var xMax = orientation == Orientation.Horizontal ? this._grid.GetLength(1) - ship.Length : this._grid.GetLength(1) - 1;
					x = random.Next(minValue: 0, maxValue: xMax);
					y = random.Next(minValue: 0, maxValue: yMax);
				}
				while (!this.TryPlaceShip(this._grid[y, x], ship, orientation));
			}
		}

		private bool TryPlaceShip(Cell startingCell, Ship ship, Orientation orientation)
		{
			if (orientation == Orientation.Vertical)
			{
				var shipCells = Enumerable.Range(startingCell.Y, ship.Length)
					.Select(i => this._grid[i, startingCell.X])
					.ToList();

				// check if ship already exists there
				if (shipCells.All(s => s.ShipId == null))
				{
					shipCells.ForEach(c => c.ShipId = ship.Id);
					return true;
				}
			}
			else
			{
				var shipCells = Enumerable.Range(startingCell.X, ship.Length)
					.Select(i => this._grid[startingCell.Y, i])
					.ToList();

				// check if ship already exists there
				if (shipCells.All(s => s.ShipId == null))
				{
					shipCells.ForEach(c => c.ShipId = ship.Id);
					return true;
				}
			}
			return false;
		}

		public static List<Ship> InitializeShips()
		{
			return new List<Ship>
			{
				new Ship
				{
					Id = 1,
					Name = "Aircraft Carrier",
					Length = 5,
					Sunk = false
				},
				new Ship
				{
					Id = 2,
					Name = "Battleship",
					Length = 4,
					Sunk = false
				},
				new Ship
				{
					Id = 3,
					Name = "Submarine",
					Length = 3,
					Sunk = false
				},
				new Ship
				{
					Id = 4,
					Name = "Cruiser",
					Length = 3,
					Sunk = false
				},
				new Ship
				{
					Id = 5,
					Name = "Destroyer",
					Length = 2,
					Sunk = false
				}
			};
		}

		private enum Orientation
		{
			Vertical = 0,
			Horizontal = 1
		}
	}
}
