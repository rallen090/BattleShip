using System;
using System.Collections.Generic;
using System.Linq;
using BattleShipClient.Utilities;

namespace BattleShipClient.Commanders
{
	public class ProbabilityCommander : ICommander
	{
		private readonly List<SunkenShipInfo> _sunkenShips = new List<SunkenShipInfo>();
		private const int HitProbabilityRating = 20;
		private const int NormalProbabilityRating = 1;

		public int GetNextTarget(Cell[,] grid, List<Ship> ships)
		{
			// adjust sunken ships and infer locations
			if (this._sunkenShips.Count < ships.Count)
			{
				this._sunkenShips.AddRange(ships.Where(s => s.Sunk).Except(this._sunkenShips.Select(s => s.Ship)).Select(s => new SunkenShipInfo { Ship = s, Cells = null }));
			}
			foreach (var unknownSunkenShip in this._sunkenShips.Where(s => s.Cells == null))
			{
				this.TryUpdateSunkenShipInfo(unknownSunkenShip, grid);
			}

			var probabilityGrid = new CellProbability[grid.GetLength(0), grid.GetLength(1)];
			foreach (var cell in grid)
			{
				probabilityGrid[cell.Y, cell.X] = new CellProbability
				{
					Cell = cell,
					ProbabilityRating = 0
				};
			}

			var hiddenShips = ships.Where(s => !s.Sunk).ToList();
			foreach (var hiddenShip in hiddenShips)
			{
				this.TraverseGridWithShip(probabilityGrid, grid, hiddenShip, Orientation.Horizontal);
				this.TraverseGridWithShip(probabilityGrid, grid, hiddenShip, Orientation.Vertical);
			}

			Console.ForegroundColor = ConsoleColor.Blue;
			for (var y = 0; y < grid.GetLength(0); y++)
			{
				for (var x = 0; x < grid.GetLength(0); x++)
				{
					var state = grid[y, x].State;
					if (state == CellState.Hidden)
					{
						Console.ForegroundColor = ConsoleColor.DarkYellow;
					}
					else if (state == CellState.Hit)
					{
						Console.ForegroundColor = ConsoleColor.Red;
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.White;
					}
					Log.Debug($"({probabilityGrid[y, x].ProbabilityRating.ToString("00")}) ");
				}
				Log.DebugLine("\n");
			}
			Console.ResetColor();

			// take the max of the grid as our target
			var orderedMoves = probabilityGrid.Cast<CellProbability>()
				.Where(p => p.Cell.State == CellState.Hidden)
				.OrderByDescending(c => c.ProbabilityRating);
			var firstMove = orderedMoves.First();
			var topMoves = orderedMoves.TakeWhile(o => o.ProbabilityRating == firstMove.ProbabilityRating).ToList();

			// decide between equal probabilities
			if (topMoves.Count > 1)
			{
				var sunkenCells = this.GetAllSunkenCells();

				var hitCells = grid.Cast<Cell>().Where(s => s.State == CellState.Hit).Except(sunkenCells).ToList();

				var viableHitCells = hitCells.Where(c => this.GetAdjacentCells(grid, c).Count(s => s.State == CellState.Hidden) == 1);
				var bothMatch = viableHitCells.Intersect(topMoves.Select(s => s.Cell)).ToList();
				if (bothMatch.Any())
				{
					return bothMatch.First().TargetLocation;
				}

				return topMoves.Select(s =>
				{
					var adjecentCells = this.GetAdjacentCells(grid, s.Cell);
					return new { adjacentHits = adjecentCells.Where(c => c.State == CellState.Hit).Except(sunkenCells).Count(), cell = s};
				})
				.OrderByDescending(s => s.adjacentHits)
				.First().cell.Cell.TargetLocation;
			}

			return orderedMoves.First().Cell.TargetLocation;
		}

		private List<Cell> GetAdjacentCells(Cell[,] grid, Cell cell)
		{
			var x = cell.X;
			var y = cell.Y;
			var xOptions = new[] { x - 1, x + 1 }.Where(v => v >= 0 && v < grid.GetLength(1));
			var yOptions = new[] { y - 1, y + 1 }.Where(v => v >= 0 && v < grid.GetLength(0));
			return xOptions.Select(xValue => grid[y, xValue]).Concat(yOptions.Select(yValue => grid[yValue, x])).ToList();
		}

		private void TraverseGridWithShip(CellProbability[,] probabilityGrid, Cell[,] grid, Ship ship, Orientation orientation)
		{
			var shipLength = ship.Length;
			// y is limited by the ship length as we traverse (for vertical)
			var yMax =  orientation == Orientation.Vertical ? grid.GetLength(0) - shipLength : grid.GetLength(0) - 1;
			var xMax = orientation == Orientation.Horizontal ? grid.GetLength(1) - shipLength : grid.GetLength(1) - 1;
			for (var y = 0; y <= yMax; y++)
			{
				for (var x = 0; x <= xMax; x++)
				{
					this.UpdateProbabilityForShip(probabilityGrid, grid, grid[y, x], shipLength, orientation);
				}
			}
		}

		private void UpdateProbabilityForShip(CellProbability[,] probabilityGrid, Cell[,] grid, Cell startingCell, int shipLength, Orientation orientation)
		{
			if (orientation == Orientation.Vertical)
			{
				var shipCells = Enumerable.Range(startingCell.Y, shipLength)
					.Select(i => grid[i, startingCell.X])
					.ToList();
				// if ALL of the cells are either Hidden OR Hit AND not already considered a sunken ship
				if (shipCells.All(s => s.State == CellState.Hidden || (s.State == CellState.Hit && !this.GetAllSunkenCells().Contains(s))))
				{
					// then it is a possible location
					var possibleCells = shipCells
						// but ignore the hit ones since they are already hit
						.Where(c => c.State != CellState.Hit).ToList();

					// weighting the ranking of cells that pass-through HIT cells higher since it is more likely to hit around them
					var weightedRating = possibleCells.Count != shipCells.Count 
						? HitProbabilityRating 
						: NormalProbabilityRating;

					// increment probabilities
					possibleCells.ForEach(p => probabilityGrid[p.Y, p.X].ProbabilityRating += weightedRating);
				}
			}
			else
			{
				var shipCells = Enumerable.Range(startingCell.X, shipLength)
					.Select(i => grid[startingCell.Y, i])
					.ToList();

				// if ALL of the cells are either Hidden OR Hit AND not already considered a sunken ship
				if (shipCells.All(s => s.State == CellState.Hidden || (s.State == CellState.Hit && !this.GetAllSunkenCells().Contains(s))))
				{
					// then it is a possible location
					var possibleCells = shipCells
						// but ignore the hit ones since they are already hit
						.Where(c => c.State != CellState.Hit)
						.ToList();

					// weighting the ranking of cells that pass-through HIT cells higher since it is more likely to hit around them
					var weightedRating = possibleCells.Count != shipCells.Count 
						? HitProbabilityRating 
						: NormalProbabilityRating;

					// increment probabilities
					possibleCells.ForEach(p => probabilityGrid[p.Y, p.X].ProbabilityRating += weightedRating);
				}
			}
		}

		private void TryUpdateSunkenShipInfo(SunkenShipInfo shipInfo, Cell[,] grid)
		{
			var hitCells = grid.Cast<Cell>().Where(c => c.State == CellState.Hit).ToList();
			List<Cell> locationList = null;
			int count = 0;
			foreach (var hitCell in hitCells)
			{
				var vertical = this.CheckIfSunkenFits(grid, hitCell, shipInfo.Ship.Length, Orientation.Vertical);
				var horizontal = this.CheckIfSunkenFits(grid, hitCell, shipInfo.Ship.Length, Orientation.Horizontal);
				if (vertical != null)
				{
					locationList = vertical;
					count++;
				}
				if (horizontal != null)
				{
					locationList = horizontal;
					count++;
				}
				if (count > 1)
				{
					// too many possibilities to call...
					return;
				}
			}

			// update if we have only one option
			if (count == 1 && locationList != null)
			{
				shipInfo.Cells = locationList;
			}
		}

		private List<Cell> CheckIfSunkenFits(Cell[,] grid, Cell startingCell, int shipLength, Orientation orientation)
		{
			if (orientation == Orientation.Vertical)
			{
				// fall out if it goes over grid edge
				var ySize = grid.GetLength(0);
				if (startingCell.Y + shipLength > ySize)
				{
					return null;
				}

				var shipCells = Enumerable.Range(startingCell.Y, shipLength)
					.Select(i => grid[i, startingCell.X])
					.ToList();
				// check that all are hit and NOT already accounted for by a different sunken ship
				if (shipCells.All(s => s.State == CellState.Hit && !this.GetAllSunkenCells().Contains(s)))
				{
					return shipCells;
				}
				return null;
			}
			else
			{
				// fall out if it goes over grid edge
				var xSize = grid.GetLength(1);
				if (startingCell.X + shipLength > xSize)
				{
					return null;
				}

				var shipCells = Enumerable.Range(startingCell.X, shipLength)
					.Select(i => grid[startingCell.Y, i])
					.ToList();
				if (shipCells.All(s => s.State == CellState.Hit))
				{
					return shipCells;
				}
				return null;
			}

		}

		private List<Cell> GetAllSunkenCells()
		{
			return this._sunkenShips.Where(s => s.Cells != null).SelectMany(s => s.Cells).Distinct().ToList();
		}

		private enum Orientation
		{
			Vertical,
			Horizontal
		}

		private class SunkenShipInfo
		{
			public Ship Ship { get; set; }
			public List<Cell> Cells { get; set; } 
		}

		public class CellProbability
		{
			public Cell Cell { get; set; }
			public int ProbabilityRating { get; set; }
		}
	}
}
