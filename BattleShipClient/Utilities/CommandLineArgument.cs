using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShipClient.Utilities
{
	public sealed class CommandLineArgumentsBuilder
	{
		private readonly List<CommandLineArgument> _arguments = new List<CommandLineArgument>();


		public CommandLineArgumentsBuilder Argument(string name, string defaultValue)
		{
			this.Argument<string>(name, (s) => s, defaultValue);
			return this;
		}

		public CommandLineArgumentsBuilder ArgumentWithFlag(string name, string flag, string defaultValue)
		{
			this.ArgumentWithFlag<string>(name, flag, (s) => s, defaultValue);
			return this;
		}

		public CommandLineArgumentsBuilder Argument<T>(string name, Func<string, T> parser, T defaultValue = default(T))
		{
			this._arguments.Add(new CommandLineArgument<T>(name, parser, defaultValue));
			return this;
		}

		public CommandLineArgumentsBuilder ArgumentWithFlag<T>(string name, string flag, Func<string, T> parser, T defaultValue = default(T))
		{
			this._arguments.Add(new CommandLineArgument<T>(name, parser, defaultValue, flag: flag));
			return this;
		}

		public List<CommandLineArgument> Build(string[] arguments)
		{
			var nonFlagArgs = this._arguments.Where(a => a.Flag == null).ToList();
			var flagArgs = this._arguments.Where(a => a.Flag != null).ToList();

			var argumentCount = arguments.TakeWhile(a => !a.StartsWith("-")).Count();
			int count = 0;
			foreach (var arg in nonFlagArgs)
			{
				if (count < argumentCount)
				{
					arg.SetValue(arguments[count]);
				}
				else
				{
					arg.SetToDefault();
				}
				count++;
			}

			var setFlaggedArguments = new List<CommandLineArgument>();
			while (count + 1 < arguments.Length)
			{
				var arg = arguments[count];

				var matchedFlag = flagArgs.SingleOrDefault(s => "-" + s.Flag == arg);
				if (matchedFlag != null)
				{
					var value = arguments[count + 1];
					matchedFlag.SetValue(value);
					setFlaggedArguments.Add(matchedFlag);
				}

				count += 2;
			}
			flagArgs.Except(setFlaggedArguments).ToList().ForEach(a => a.SetToDefault());

			return this._arguments;
		}
	}

	public class CommandLineArgument<T> : CommandLineArgument
	{
		internal CommandLineArgument(string name, Func<string, T> parser, T defaultValue = default(T), string flag = null) : base(name, flag)
		{
			this.Parser = parser;
			this.DefaultValue = defaultValue;
		}

		public T Value { get; internal set; }
		public T DefaultValue { get; }
		private Func<string, T> Parser { get; }
		public override Type ArgumentType() => typeof(T);

		internal override void SetValue(string value)
		{
			try
			{
				this.Value = this.Parser(value);
			}
			catch
			{
				this.SetToDefault();
			}
		}

		internal override void SetToDefault()
		{
			this.Value = this.DefaultValue;
		}

		public override bool IsType<T2>()
		{
			return typeof(T) == typeof(T2);
		}

		public override string GetStringValue()
		{
			return Value.ToString();
		}
	}

	public abstract class CommandLineArgument
	{
		internal CommandLineArgument(string name, string flag = null)
		{
			this.Name = name;
			this.Flag = flag;
		}

		public string Name { get; }
		public string Flag { get; }

		internal abstract void SetValue(string value);
		internal abstract void SetToDefault();

		public abstract Type ArgumentType();
		public abstract bool IsType<T>();

		public abstract string GetStringValue();
	}

	public static class CommandLineArgumentHelpers
	{
		public static CommandLineArgument<T> GetByName<T>(this List<CommandLineArgument> arguments, string name)
		{
			return (CommandLineArgument<T>) arguments.SingleOrDefault(s => s.Name == name);
		}

		public static string ToConsoleString(this List<CommandLineArgument> arguments)
		{
			var nonFlagArguments = arguments.Where(a => a.Flag == null).ToList();
			var flagArguments = arguments.Where(a => a.Flag != null).ToList();
			var consoleStringBuilder = new StringBuilder("Command Line Arguments:");
			consoleStringBuilder.AppendLine();
			nonFlagArguments.Select(a => $"[{a.Name}] ({a.GetStringValue()} as {a.ArgumentType()})").ToList().ForEach(a => consoleStringBuilder.AppendLine(a));
			flagArguments.Select(a => $"-{a.Flag} [{a.Name}] ({a.GetStringValue()} as {a.ArgumentType().Name})").ToList().ForEach(a => consoleStringBuilder.AppendLine(a));
			return consoleStringBuilder.ToString();
		}
	}
}
