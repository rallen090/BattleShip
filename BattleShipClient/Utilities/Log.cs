using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShipClient.Utilities
{
	public static class Log
	{
		public static bool DebugActive = false;

		public static void DebugLine(string line = "\n")
		{
			if (DebugActive)
			{
				Console.WriteLine(line);
			}
		}

		public static void Debug(string line)
		{
			if (DebugActive)
			{
				Console.Write(line);
			}
		}

		public static void WriteLine(string line)
		{
			Console.WriteLine(line);
		}
	}
}
