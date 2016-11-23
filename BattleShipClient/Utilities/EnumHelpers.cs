using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShipClient.Utilities
{
	public static class EnumHelpers
	{
		public static string ToDisplayString(this CellState @this)
		{
			switch (@this)
			{
				case CellState.Hidden:
					return " ";
				case CellState.Hit:
					return "X";
				case CellState.Miss:
					return "O";
				default:
					throw new Exception($"Invalid CellState {@this}");
			}
		}
	}
}
