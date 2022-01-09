using System;
using System.Collections.Generic;

namespace LuaGeneration
{
	public static class LuaGeneration
	{
		private static string Symbols = "`~!@#$%^&*()_-+={}|;:<,>.?/";

		private static string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

		private static string Lowercase = "abcdefghijklmnnopqrstuvwxyz";

		private static string Numbers = "0123456789";

		private static string Hexadecimal = "ABCDEFabcdef0123456789";

		private static string Alphabet = Uppercase + Lowercase;

		private static string Alphanumeric = Alphabet + Numbers;

		private static string Characters = Alphabet + Numbers + Symbols;

		private static string[] Operators = new string[13]
		{
			" + ", " - ", " * ", " / ", " ^ ", " % ", " or ", " and ", " == ", " <= ",
			" >= ", " < ", " > "
		};

		private static Random Random = new Random();

		private static List<string> Variables = new List<string>();

		private static double GenerationIntensity = 10.0;

		private static string Source = "";

		private static dynamic ValueLock = null;

		private static int DEFAULT_VARIABLE_NAME_LENGTH = 10;

		private const int DEFAULT_NUMBER_LENGTH = 5;

		private const int MAXIMUM_TABLE_LENGTH = 10;

		private const int MAXIMUM_STRING_LENGTH = 100;

		private const int MAXIMUM_DEPTH = 5;

		private const int MAXIMUM_FUNCTION_DEPTH = 2;

		private static string GenerateVariableName(int Length = 0)
		{
			Length = ((Length > 0) ? Length : DEFAULT_VARIABLE_NAME_LENGTH);
			string String = Alphabet[Random.Next(0, Alphabet.Length)].ToString();
			for (int I = 0; I < Length - 1; I++)
			{
				String += Alphanumeric[Random.Next(0, Alphanumeric.Length)];
			}
			return String;
		}

		private static string GenerateNumber(int Length = 5, bool Integer = true)
		{
			Integer = true;
			string String = "";
			for (int I = 0; I < Length; I++)
			{
				String += Numbers[Random.Next(0, Numbers.Length)];
			}
			if (!Integer)
			{
				String += ".";
				for (int I2 = 0; I2 < Length; I2++)
				{
					String += Numbers[Random.Next(0, Numbers.Length)];
				}
			}
			return String;
		}

		private static string GenerateHexadecimal(int Length = 5)
		{
			string String = "0x";
			for (int I = 0; I < Length; I++)
			{
				String += Hexadecimal[Random.Next(0, Hexadecimal.Length)];
			}
			return String;
		}

		private static string GenerateString(int MaximumLength = 100)
		{
			string String = "";
			for (int I = 0; I < Random.Next(1, MaximumLength); I++)
			{
				String += Characters[Random.Next(0, Characters.Length)];
			}
			switch (Random.Next(0, 3))
			{
			case 0:
				return "\"" + String + "\"";
			case 1:
				return "'" + String + "'";
			case 2:
			{
				string Chunk = new string('=', Random.Next(0, 10));
				return "[" + Chunk + "[" + String + "]" + Chunk + "]";
			}
			default:
				return String;
			}
		}

		private static string GenerateTable(int Depth = 0)
		{
			if (Depth > 5)
			{
				return "{}";
			}
			string String = "{";
			for (int I = 0; I < Random.Next(0, 10); I++)
			{
				switch (Random.Next(0, 2))
				{
				case 0:
					String = String + GetRandomValue(Depth + 1) + ";";
					break;
				case 1:
					String = String + "[(" + GetRandomValue(Depth + 1) + ")] = " + GetRandomValue(Depth + 1) + ";";
					break;
				}
			}
			return String + "}";
		}

		private static string GetEquation(int Depth = 0)
		{
			string String = GetRandomValue(Depth + 1) + Operators[Random.Next(0, Operators.Length)] + GetRandomValue(Depth + 1);
			if (Depth <= 5)
			{
				String = String + Operators[Random.Next(0, Operators.Length)] + GetEquation(Depth + 1);
			}
			return String;
		}

		private static string GenerateFunction(int Depth = 0)
		{
			if (Depth > 5)
			{
				return "(function(...) return; end)";
			}
			int VariableCount = Variables.Count;
			string String = "(function(";
			int Parameters = Random.Next(0, 10);
			for (int I2 = 0; I2 < Parameters; I2++)
			{
				string Parameter = GenerateVariableName();
				String = String + Parameter + ", ";
				Variables.Add(Parameter);
			}
			String += "...)";
			GenerateBody(Depth + 1);
			String += "return ";
			int R = Random.Next(0, 10);
			for (int I = 0; I < R; I++)
			{
				String = String + GetRandomValue(Depth + 1) + ((I < R - 1) ? ", " : "");
			}
			Variables.RemoveRange(VariableCount, Variables.Count - VariableCount);
			return String + "; end)";
		}

		private static string GetRandomValue(int Depth = 0, dynamic Lock = null)
		{
			string String = "";
			if (ValueLock != null)
			{
				switch (Random.Next(0, 11))
				{
				case 0:
					if (Variables.Count <= 0)
					{
						goto default;
					}
					String = Variables[Random.Next(0, Variables.Count)];
					break;
				case 7:
					if (Depth >= 5)
					{
						goto default;
					}
					String = GenerateTable(Depth + 1);
					break;
				case 8:
					if (Depth >= 5)
					{
						goto default;
					}
					String = GetEquation(Depth + 1);
					break;
				case 9:
					if (Depth >= 5)
					{
						goto default;
					}
					String = GenerateFunction(Depth + 1);
					break;
				default:
					String = Alphabet[Random.Next(0, Alphabet.Length)].ToString();
					break;
				}
			}
			else if (Depth > 5)
			{
				switch (Random.Next(0, 7))
				{
				case 0:
					if (Variables.Count <= 0)
					{
						goto default;
					}
					String = Variables[Random.Next(0, Variables.Count)];
					break;
				case 1:
					String = GenerateString();
					break;
				case 2:
					String = GenerateNumber();
					break;
				case 3:
					String = GenerateHexadecimal();
					break;
				case 4:
					String = "...";
					break;
				case 5:
					String = ((Random.Next(0, 2) == 0) ? "false" : "true");
					break;
				case 6:
					String = "nil";
					break;
				default:
					String = GenerateString();
					break;
				}
			}
			else
			{
				switch (Random.Next(0, 11))
				{
				case 0:
					if (Variables.Count <= 0)
					{
						goto default;
					}
					String = Variables[Random.Next(0, Variables.Count)];
					break;
				case 1:
					String = GenerateString();
					break;
				case 2:
					String = GenerateNumber();
					break;
				case 3:
					String = GenerateHexadecimal();
					break;
				case 4:
					String = "...";
					break;
				case 5:
					String = ((Random.Next(0, 2) == 0) ? "false" : "true");
					break;
				case 6:
					String = "nil";
					break;
				case 7:
					if (Depth >= 5)
					{
						goto default;
					}
					String = GenerateTable(Depth + 1);
					break;
				case 8:
					if (Depth >= 5)
					{
						goto default;
					}
					String = GetEquation(Depth + 1);
					break;
				case 9:
					if (Depth >= 5)
					{
						goto default;
					}
					String = GenerateFunction(Depth + 1);
					break;
				default:
					String = GenerateString();
					break;
				}
			}
			if (Random.Next(0, 2) == 0)
			{
				String = "(not " + String + ")";
			}
			if (Random.Next(0, 2) == 0)
			{
				String = "#" + String;
			}
			if (Random.Next(0, 2) == 0)
			{
				String = "(-" + String + ")";
			}
			if (Random.Next(0, 2) == 0)
			{
				String = "(" + String + ")." + GenerateVariableName();
			}
			if (Random.Next(0, 2) == 0)
			{
				String = "(" + String + ")()";
			}
			return String;
		}

		private static void GenerateBody(int Depth = 1)
		{
			if (Depth > 2)
			{
				return;
			}
			int VariableCount = Variables.Count;
			for (int I = 0; (double)I < GenerationIntensity - (double)Depth; I++)
			{
				switch (Random.Next(0, 6))
				{
				case 0:
				{
					string VariableName = GenerateVariableName();
					Source = Source + "local " + VariableName + " = " + GetRandomValue(Depth + 1) + ";";
					Variables.Add(VariableName);
					break;
				}
				case 1:
					Source = Source + "if (" + GetEquation(Depth + 1) + ") then ";
					GenerateBody(Depth + 1);
					Source += " end;";
					break;
				case 2:
					Source = Source + "while (" + GetEquation(Depth + 1) + ") do ";
					GenerateBody(Depth + 1);
					Source += " end;";
					break;
				case 3:
				{
					string VariableName2 = GenerateVariableName();
					Source = Source + "for " + VariableName2 + " = " + GetEquation(Depth + 1) + ", " + GetEquation(Depth + 1) + ", " + GetEquation(Depth + 1) + " do ";
					Variables.Add(VariableName2);
					GenerateBody(Depth + 1);
					Source += " end;";
					Variables.Remove(VariableName2);
					break;
				}
				case 4:
				{
					string VariableName3 = GenerateVariableName();
					Source = Source + "local function " + VariableName3 + "(...) ";
					Variables.Add(VariableName3);
					GenerateBody(Depth + 1);
					Source += " end;";
					break;
				}
				}
			}
			Variables.RemoveRange(VariableCount, Variables.Count - VariableCount);
		}

		public static string GenerateSingleVariable()
		{
			return Random.Next(0, 4) switch
			{
				0 => "local _ = " + GenerateNumber() + ";", 
				1 => "local _ = " + GenerateString() + ";", 
				2 => "local _ = " + GenerateHexadecimal() + ";", 
				3 => "local _ = ({});", 
				_ => "local _ = " + GenerateString() + ";", 
			};
		}

		public static string GenerateRandomFile(int Iterations, int GenerationIntensity)
		{
			List<string> Chunks = new List<string>();
			Alphabet = "_";
			Alphanumeric = "_";
			DEFAULT_VARIABLE_NAME_LENGTH = 1;
			Source = "";
			for (double I = 0.0; I < (double)Iterations; I += 1.0)
			{
				Source = "";
				switch (Random.Next(0, 5))
				{
				case 0:
				{
					string VariableName = GenerateVariableName();
					Source = Source + "local " + VariableName + " = " + GetEquation(1) + ";";
					Variables.Add(VariableName);
					break;
				}
				case 1:
					Source = Source + "if (" + GetEquation(1) + ") then ";
					GenerateBody();
					Source += " end;";
					break;
				case 2:
					Source = Source + "while (" + GetEquation(1) + ") do ";
					GenerateBody();
					Source += " end;";
					break;
				case 3:
				{
					string VariableName2 = GenerateVariableName();
					Source = Source + "for " + VariableName2 + " = " + GetEquation(1) + ", " + GetEquation(1) + ", " + GetEquation(1) + " do ";
					Variables.Add(VariableName2);
					GenerateBody();
					Source += " end;";
					Variables.Remove(VariableName2);
					break;
				}
				case 4:
				{
					string VariableName3 = GenerateVariableName();
					Source = Source + "local function " + VariableName3 + "(...) ";
					Variables.Add(VariableName3);
					GenerateBody();
					Source += " end;";
					break;
				}
				}
				Source += "\n";
				Chunks.Add(Source);
			}
			Source = "";
			Source = string.Join("", Chunks);
			return Source;
		}

		public static string VarArgSpam(int Iterations, int GenerationIntensity)
		{
			ValueLock = new string[1] { "..." };
			DEFAULT_VARIABLE_NAME_LENGTH = 1;
			List<string> Chunks = new List<string>();
			Source = "";
			for (double I = 0.0; I < (double)Iterations; I += 1.0)
			{
				Source = "";
				switch (Random.Next(0, 5))
				{
				case 0:
				{
					string VariableName = GenerateVariableName();
					Source = Source + "local " + VariableName + " = " + GetEquation(1) + ";";
					Variables.Add(VariableName);
					break;
				}
				case 1:
					Source = Source + "if (" + GetEquation(1) + ") then ";
					GenerateBody();
					Source += " end;";
					break;
				case 2:
					Source = Source + "while (" + GetEquation(1) + ") do ";
					GenerateBody();
					Source += " end;";
					break;
				case 3:
				{
					string VariableName2 = GenerateVariableName();
					Source = Source + "for " + VariableName2 + " = " + GetEquation(1) + ", " + GetEquation(1) + ", " + GetEquation(1) + " do ";
					Variables.Add(VariableName2);
					GenerateBody();
					Source += " end;";
					Variables.Remove(VariableName2);
					break;
				}
				case 4:
				{
					string VariableName3 = GenerateVariableName();
					Source = Source + "local function " + VariableName3 + "(...) ";
					Variables.Add(VariableName3);
					GenerateBody();
					Source += " end;";
					break;
				}
				}
				Source += "\n";
				Chunks.Add(Source);
			}
			Source = "";
			Source = string.Join("", Chunks);
			Variables.Clear();
			return Source;
		}

		public static string OperatorSpam(int Iterations, int GenerationIntensity)
		{
			DEFAULT_VARIABLE_NAME_LENGTH = 1;
			Source = "";
			List<string> Chunks = new List<string>();
			for (double I = 0.0; I < (double)Iterations; I += 1.0)
			{
				string Chunk = "";
				Chunk = Chunk + "local " + GenerateVariableName(2) + " = " + GenerateVariableName();
				for (int K = 0; K < GenerationIntensity; K++)
				{
					Chunk = Chunk + Operators[Random.Next(0, Operators.Length)] + GenerateVariableName();
				}
				Chunk += ";\n";
				Chunks.Add(Chunk);
			}
			Source = string.Join("", Chunks);
			Variables.Clear();
			return Source;
		}
	}
}
