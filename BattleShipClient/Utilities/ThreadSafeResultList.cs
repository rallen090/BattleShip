using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShipClient.Utilities
{
	public class ThreadSafeResultList
	{
		private readonly List<GameResult> _results = new List<GameResult>();
		private readonly object _lock = new object();

		public void Add(Tuple<GameResult, GameResult> result)
		{
			lock (this._lock)
			{
				this._results.AddRange(new []{result.Item1, result.Item2});

				var gameCount = this._results.Count / 2;
				Log.WriteLine($"Game {gameCount} complete");
				var groupedResults = this._results.GroupBy(r => r.PlayerId).ToDictionary(s => s.Key, s => s);
				Log.WriteLine($"Player 1 win rate: {groupedResults[1].Count(s => s.Victory)}/{gameCount}");
				Log.WriteLine($"Player 2 win rate: {groupedResults[2].Count(s => s.Victory)}/{gameCount}");
			}
		}
	}
}
