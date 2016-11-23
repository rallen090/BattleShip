using System;
using System.Linq;

namespace BattleShipClient.Protocol
{
	public class MessageEncoder
	{
		public static string EncodeActionMessage(ActionMessage message)
		{
			return message.TargetCell.ToString();
		}

		public static string EncodeResponseMessage(ResponseMessage message)
		{
			try
			{
				switch (message.Type)
				{
					case ResponseMessageType.Hit:
						return "HIT";
					case ResponseMessageType.Sunk:
						return $"SUNK {message.Value}";
					case ResponseMessageType.Miss:
						return "MISS";
					case ResponseMessageType.Win:
						return "WIN";
					case ResponseMessageType.Lose:
						return "LOSE";
					default:
						throw new Exception("Invalid response message to encode");
				}
			}
			catch (Exception ex)
			{
				var a = ex;
			}
			return null;
		}

		public static ResponseMessage DecodeResponseMessage(string message)
		{
			var split = message.Split(' ').Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
			var command = split.First().ToUpper();
			switch (command)
			{
				case "OK":
					var n = Math.Sqrt((split.Count - 1) / 2);
					return new ResponseMessage { Type = ResponseMessageType.Ok, GridSize = (int)n};
				case "SHOOT":
					return new ResponseMessage { Type = ResponseMessageType.Shoot };
				case "HIT":
					return new ResponseMessage { Type = ResponseMessageType.Hit };
				case "MISS":
					return new ResponseMessage { Type = ResponseMessageType.Miss };
				case "SUNK":
					var value = int.Parse(split.Skip(1).First());
					return new ResponseMessage { Type = ResponseMessageType.Sunk, Value = value };
				case "WIN":
					return new ResponseMessage { Type = ResponseMessageType.Win };
				case "LOSE":
					return new ResponseMessage { Type = ResponseMessageType.Lose };
				case "ERROR":
					return new ResponseMessage { Type = ResponseMessageType.Lose, FullMessage = message };
				default:
					return new ResponseMessage { Type = ResponseMessageType.Unknown, FullMessage = message };
			}
		}
	}

	public class ActionMessage
	{
		public int TargetCell { get; set; }	
	}

	public class ResponseMessage
	{
		public ResponseMessageType Type { get; set; }
		public string FullMessage { get; set; }
		public int? Value { get; set; }
		public int? GridSize { get; set; }
	}

	public enum ResponseMessageType
	{
		Ok = 1,
		Shoot = 2,
		Miss = 3,
		Hit = 4,
		Sunk = 5,
		Win = 6,
		Lose = 7,
		Error = 8,
		Unknown = 9
	}
}
