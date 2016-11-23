using System.Collections.Generic;

namespace BattleShipClient.Commanders
{
	/// <summary>
	/// API for making moves in a BattleShip <see cref="Game"/>
	/// </summary>
	public interface ICommander
	{
		int GetNextTarget(Cell[,] grid, List<Ship> ships);
	}
}
