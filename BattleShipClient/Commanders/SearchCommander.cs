using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShipClient.Commanders
{
	public class SearchCommander : ICommander
	{
		public int GetNextTarget(Cell[,] grid, List<Ship> ships)
		{
			return 1;
			//var hiddenCells = grid.Cast<Cell>()
			//	.Where(cell => cell.State == CellState.Hit)
			//	.ToList();
		}

		//private List<Cell> GetSunkCells(List<Cell> hitCells, List<Ship> ships)
		//{
		//	var sunkShips = ships.Where(s => s.Sunk).ToList();

		//	if (sunkShips.Any())
		//	{
				
		//	}
		//}
	}
}
