using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace BattleShipClient.Utilities
{
	public class ShutdownHelper
	{
		public static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
		{
			// Put your own handler here
			switch (ctrlType)
			{
				case CtrlTypes.CTRL_C_EVENT:
					GameRunner.KillServers();
					Environment.Exit(1);
					break;

				case CtrlTypes.CTRL_BREAK_EVENT:
					GameRunner.KillServers();
					Environment.Exit(1);
					break;

				case CtrlTypes.CTRL_CLOSE_EVENT:
					Console.WriteLine("Program being closed!");
					GameRunner.KillServers();
					break;

				case CtrlTypes.CTRL_LOGOFF_EVENT:
				case CtrlTypes.CTRL_SHUTDOWN_EVENT:
					Console.WriteLine("User is logging off!");
					GameRunner.KillServers();
					break;
			}
			return true;
		}



		#region ---- Unmanaged ----
		// Declare the SetConsoleCtrlHandler function
		// as external and receiving a delegate.

		[DllImport("Kernel32")]
		public static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);

		// A delegate type to be used as the handler routine
		// for SetConsoleCtrlHandler.
		public delegate bool HandlerRoutine(CtrlTypes ctrlType);

		// An enumerated type for the control messages
		// sent to the handler routine.
		public enum CtrlTypes
		{
			CTRL_C_EVENT = 0,
			CTRL_BREAK_EVENT,
			CTRL_CLOSE_EVENT,
			CTRL_LOGOFF_EVENT = 5,
			CTRL_SHUTDOWN_EVENT
		}

		#endregion
	}
}
