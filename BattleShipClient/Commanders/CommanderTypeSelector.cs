using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShipClient.Commanders
{
	public class CommanderTypeSelector
	{
		public static Func<ICommander> GetCommanderFactory(CommanderType commanderType)
		{
			switch (commanderType)
			{
				case CommanderType.RandomCommander:
					return () => new RandomCommander();
				case CommanderType.ProbabilityCommander:
					return () => new ProbabilityCommander();
				default:
					throw new ArgumentException($"Invalid commander type: {commanderType}");
			}
		}
	}

	public enum CommanderType
	{
		RandomCommander = 1,
		ProbabilityCommander = 2
	}
}
