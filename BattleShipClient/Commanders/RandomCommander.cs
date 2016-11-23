using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleShipClient.Commanders
{
	public class RandomCommander : ICommander
	{
		public int GetNextTarget(Cell[,] grid, List<Ship> ships)
		{
			var random = new Random();
			var hiddenCells = grid.Cast<Cell>()
				.Where(cell => cell.State == CellState.Hidden)
				.ToList();

			return hiddenCells.ElementAt(random.Next(minValue: 0, maxValue: hiddenCells.Count - 1)).TargetLocation;
		}
	}
}
