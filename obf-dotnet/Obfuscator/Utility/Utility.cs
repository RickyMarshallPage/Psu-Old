using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Obfuscator.Bytecode.IR;
using Obfuscator.Extensions;

namespace Obfuscator.Utility
{
	public static class Utility
	{
		private static List<string> VMStrings = new List<string>
		{
			""
		};

		public static Random Random = new Random();

		public static List<char> HexDecimal = "abcdefABCDEF0123456789".ToCharArray().ToList();

		public static List<char> Decimal = "0123456789".ToCharArray().ToList();

		public static List<char> Characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJLMNOPQRSTUVWXYZ".ToCharArray().ToList();

		public static List<char> AlphaNumeric = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJLMNOPQRSTUVWXYZ01234567890".ToCharArray().ToList();

		public static bool OverrideStrings = false;

		public static bool NoExtraString = false;

		public static List<string> extra = new List<string>();

		[DllImport("ObfuscatorDLL.dll")]
		private static extern double StringToDouble(string String, ref bool Success);

		public static void VoidInstruction(Instruction Instruction)
		{
			Instruction.OpCode = OpCode.None;
			Instruction.A = 0;
			Instruction.B = 0;
			Instruction.C = 0;
			Instruction.ConstantType = InstructionConstantType.NK;
			Instruction.InstructionType = InstructionType.ABC;
			foreach (object Reference in Instruction.References)
			{
				if (Reference is Instruction ReferencedInstruction)
				{
					ReferencedInstruction.BackReferences.Remove(Instruction);
				}
				else if (Reference is Constant ReferencedConstant)
				{
					ReferencedConstant.BackReferences.Remove(Instruction);
				}
			}
			Instruction.References = new List<object> { null, null, null, null, null };
		}

		public static void SwapBackReferences(Instruction Original, Instruction Instruction)
		{
			foreach (Instruction BackReference in Original.BackReferences)
			{
				Instruction.BackReferences.Add(BackReference);
				if (BackReference.JumpTo == Original)
				{
					BackReference.JumpTo = Instruction;
				}
				else
				{
					BackReference.References[BackReference.References.IndexOf(Original)] = Instruction;
				}
			}
			Original.BackReferences.Clear();
		}

		public static Constant GetOrAddConstant(Chunk Chunk, ConstantType Type, dynamic Constant, out int ConstantIndex)
		{
			Constant Current = Chunk.Constants.FirstOrDefault((Constant C) => C.Type == Type && C.Data == Constant);
			if (Current != null)
			{
				ConstantIndex = Chunk.Constants.IndexOf(Current);
				return Current;
			}
			Constant nConstant = new Constant
			{
				Type = Type,
				Data = Constant
			};
			ConstantIndex = Chunk.Constants.Count;
			Chunk.Constants.Add(nConstant);
			Chunk.ConstantMap.Add(nConstant, ConstantIndex);
			return nConstant;
		}

		public static int[] GetRegistersThatInstructionWritesTo(Instruction Instruction, int StackTop = 256)
		{
			int A = Instruction.A;
			int B = Instruction.B;
			int C = Instruction.C;
			switch (Instruction.OpCode)
			{
			case OpCode.OpMove:
				return new int[1] { A };
			case OpCode.OpLoadK:
				return new int[1] { A };
			case OpCode.OpUnm:
				return new int[1] { A };
			case OpCode.OpLoadBool:
				return new int[1] { A };
			case OpCode.OpLoadNil:
				return Enumerable.Range(A, B - A + 1).ToArray();
			case OpCode.OpLen:
				return new int[1] { A };
			case OpCode.OpNot:
				return new int[1] { A };
			case OpCode.OpGetGlobal:
				return new int[1] { A };
			case OpCode.OpGetUpValue:
				return new int[1] { A };
			case OpCode.OpGetTable:
				return new int[1] { A };
			case OpCode.OpAdd:
				return new int[1] { A };
			case OpCode.OpSub:
				return new int[1] { A };
			case OpCode.OpMul:
				return new int[1] { A };
			case OpCode.OpDiv:
				return new int[1] { A };
			case OpCode.OpMod:
				return new int[1] { A };
			case OpCode.OpPow:
				return new int[1] { A };
			case OpCode.OpConcat:
				return new int[1] { A };
			case OpCode.OpCall:
				if (C >= 2)
				{
					return Enumerable.Range(A, C + 1).ToArray();
				}
				if (C != 0)
				{
					break;
				}
				return new int[1] { A };
			case OpCode.OpVarArg:
				if (B >= 2)
				{
					return Enumerable.Range(A, B - 1).ToArray();
				}
				if (B != 0)
				{
					break;
				}
				return new int[1] { A };
			case OpCode.OpSelf:
				return new int[2]
				{
					A,
					A + 1
				};
			case OpCode.OpTestSet:
				return new int[1] { A };
			case OpCode.OpForPrep:
				return new int[1] { A };
			case OpCode.OpForLoop:
				return new int[2]
				{
					A,
					A + 3
				};
			case OpCode.OpTForLoop:
				return Enumerable.Range(A + 2, A + 2 + C - A + 1).ToArray();
			case OpCode.OpNewTable:
				return new int[1] { A };
			case OpCode.OpClosure:
				return new int[1] { A };
			case OpCode.OpClose:
				return Enumerable.Range(0, StackTop).ToArray();
			}
			return new int[0];
		}

		public static int[] GetRegistersThatInstructionReadsFrom(Instruction Instruction, int StackTop = 256)
		{
			int A = Instruction.A;
			int B = Instruction.B;
			int C = Instruction.C;
			switch (Instruction.OpCode)
			{
			case OpCode.OpMove:
				return new int[1] { B };
			case OpCode.OpUnm:
				return new int[1] { B };
			case OpCode.OpLen:
				return new int[1] { B };
			case OpCode.OpNot:
				return new int[1] { B };
			case OpCode.OpSetGlobal:
				return new int[1] { A };
			case OpCode.OpSetUpValue:
				return new int[1] { A };
			case OpCode.OpGetTable:
			{
				List<int> List = new List<int> { B };
				if (C < 1024)
				{
					List.Add(C);
				}
				return List.ToArray();
			}
			case OpCode.OpSetTable:
			{
				List<int> List4 = new List<int> { A };
				if (B < 1024)
				{
					List4.Add(B);
				}
				if (C < 1024)
				{
					List4.Add(C);
				}
				return List4.ToArray();
			}
			case OpCode.OpAdd:
			{
				List<int> List9 = new List<int>();
				if (B < 1024)
				{
					List9.Add(B);
				}
				if (C < 1024)
				{
					List9.Add(C);
				}
				return List9.ToArray();
			}
			case OpCode.OpSub:
			{
				List<int> List11 = new List<int>();
				if (B < 1024)
				{
					List11.Add(B);
				}
				if (C < 1024)
				{
					List11.Add(C);
				}
				return List11.ToArray();
			}
			case OpCode.OpMul:
			{
				List<int> List12 = new List<int>();
				if (B < 1024)
				{
					List12.Add(B);
				}
				if (C < 1024)
				{
					List12.Add(C);
				}
				return List12.ToArray();
			}
			case OpCode.OpDiv:
			{
				List<int> List10 = new List<int>();
				if (B < 1024)
				{
					List10.Add(B);
				}
				if (C < 1024)
				{
					List10.Add(C);
				}
				return List10.ToArray();
			}
			case OpCode.OpMod:
			{
				List<int> List8 = new List<int>();
				if (B < 1024)
				{
					List8.Add(B);
				}
				if (C < 1024)
				{
					List8.Add(C);
				}
				return List8.ToArray();
			}
			case OpCode.OpPow:
			{
				List<int> List6 = new List<int>();
				if (B < 1024)
				{
					List6.Add(B);
				}
				if (C < 1024)
				{
					List6.Add(C);
				}
				return List6.ToArray();
			}
			case OpCode.OpConcat:
				return Enumerable.Range(B, C - B + 1).ToArray();
			case OpCode.OpCall:
				if (B >= 2)
				{
					return Enumerable.Range(A, B).ToArray();
				}
				switch (B)
				{
				case 0:
					return Enumerable.Range(A, StackTop - A).ToArray();
				case 1:
					return new int[1] { A };
				}
				break;
			case OpCode.OpReturn:
				if (B >= 2)
				{
					return Enumerable.Range(A, B - 1).ToArray();
				}
				if (B != 0)
				{
					break;
				}
				return Enumerable.Range(A, StackTop - A).ToArray();
			case OpCode.OpTailCall:
				if (B == 0)
				{
					return Enumerable.Range(A, StackTop - A).ToArray();
				}
				if (B < 2)
				{
					break;
				}
				return Enumerable.Range(A, B).ToArray();
			case OpCode.OpSelf:
			{
				int[] List7 = new int[1] { B };
				if (C < 1024)
				{
					List7[1] = C;
				}
				return List7.ToArray();
			}
			case OpCode.OpEq:
			{
				List<int> List5 = new List<int>();
				if (B < 1024)
				{
					List5.Add(B);
				}
				if (C < 1024)
				{
					List5.Add(C);
				}
				return List5.ToArray();
			}
			case OpCode.OpLt:
			{
				List<int> List3 = new List<int>();
				if (B < 1024)
				{
					List3.Add(B);
				}
				if (C < 1024)
				{
					List3.Add(C);
				}
				return List3.ToArray();
			}
			case OpCode.OpLe:
			{
				List<int> List2 = new List<int>();
				if (B < 1024)
				{
					List2.Add(B);
				}
				if (C < 1024)
				{
					List2.Add(C);
				}
				return List2.ToArray();
			}
			case OpCode.OpTest:
				return new int[1] { A };
			case OpCode.OpTestSet:
				return new int[2] { A, B };
			case OpCode.OpForPrep:
				return new int[1] { A + 2 };
			case OpCode.OpForLoop:
				return new int[4]
				{
					A,
					A + 1,
					A + 2,
					A + 3
				};
			case OpCode.OpTForLoop:
				return new int[4]
				{
					A,
					A + 1,
					A + 2,
					A + 3
				};
			case OpCode.OpSetList:
				return Enumerable.Range(A, StackTop - A).ToArray();
			}
			return new int[0];
		}

		public static void GetExtraStrings(string source)
		{
			NoExtraString = false;
			OverrideStrings = false;
			extra.Clear();
			Match mtch = Regex.Match(source, "^\\s*--\\[(=*)\\[([\\S\\s]*?)\\]\\1\\]");
			if (!mtch.Success || mtch.Groups.Count < 3)
			{
				return;
			}
			string comment = mtch.Groups[2].Value.Trim();
			string[] lines = comment.Split('\n');
			if (lines.Length == 0)
			{
				return;
			}
			string line2 = lines[0].Trim().ToLower();
			if (!(line2 != "strings") || !(line2 != "strings-override"))
			{
				if (line2 == "strings-override")
				{
					OverrideStrings = true;
				}
				for (int i = 1; i < lines.Length; i++)
				{
					string line = lines[i].Trim();
					extra.Add(line);
				}
			}
		}

		public static int CompatLength(string str)
		{
			return Encoding.ASCII.GetString(Encoding.Default.GetBytes(str)).Length;
		}

		public static string FinalReplaceStrings(string source)
		{
			return Regex.Replace(source, "EXTRASTRING(\\d+)", (Match mtch) => extra[int.Parse(mtch.Groups[1].Value)].Replace("\"", "\\\""));
		}

		public static string IntegerToHex(int Integer)
		{
			return "0x" + Integer.ToString("X3");
		}

		public static string IntegerToString(int Integer, int Minimum = 0)
		{
			switch (Random.Next(Minimum, 4))
			{
			case 0:
				return Integer.ToString();
			case 1:
				return IntegerToHex(Integer);
			case 2:
				return IntegerToTable(Integer);
			case 3:
			{
				Random rand = new Random();
				if (extra.Count == 0 || (extra.Count == 1 && OverrideStrings) || NoExtraString)
				{
					string String3 = VMStrings.Random();
					return $"({Integer + String3.Length} - #(\"{String3}\"))";
				}
				if (OverrideStrings)
				{
					int idx = rand.Next(0, extra.Count);
					string String = extra[idx];
					return $"({Integer + CompatLength(String)} - #(\"EXTRASTRING{idx}\"))";
				}
				if (rand.Next(0, 2) == 0)
				{
					int idx2 = rand.Next(0, extra.Count);
					string String2 = extra[idx2];
					return $"({Integer + CompatLength(String2)} - #(\"EXTRASTRING{idx2}\"))";
				}
				string String4 = VMStrings.Random();
				return $"({Integer + String4.Length} - #(\"{String4}\"))";
			}
			default:
				return Integer.ToString();
			}
		}

		public static string IntegerToStringBasic(int Integer)
		{
			return Random.Next(0, 2) switch
			{
				0 => Integer.ToString(), 
				1 => IntegerToHex(Integer), 
				_ => Integer.ToString(), 
			};
		}

		public static string IntegerToTable(int Value)
		{
			string Table = "(#{";
			int Values = Random.Next(0, 5);
			Value -= Values;
			int Count = 0;
			int Indexes = 0;
			for (; Count < Values; Count++)
			{
				Table = Table + IntegerToStringBasic(Random.Next(0, 1000)) + ";";
			}
			if (Random.Next(0, 2) == 0)
			{
				int ReturnValues = Random.Next(0, 5);
				int ReturnCount = 0;
				Value -= ReturnValues;
				Table += "(function(...)return ";
				for (; ReturnCount < ReturnValues; ReturnCount++)
				{
					Table += IntegerToStringBasic(Random.Next(0, 1000));
					if (ReturnCount < ReturnValues - 1)
					{
						Table += ",";
					}
				}
				bool HasVarArg = Random.Next(0, 3) == 0;
				if (HasVarArg)
				{
					if (ReturnValues > 0)
					{
						Table += ",";
					}
					Table += "...";
				}
				Table += ";end)(";
				if (HasVarArg)
				{
					int VarArgValues = Random.Next(0, 5);
					int VarArgCount = 0;
					Value -= VarArgValues;
					for (; VarArgCount < VarArgValues; VarArgCount++)
					{
						Table += IntegerToStringBasic(Random.Next(0, 1000));
						if (VarArgCount < VarArgValues - 1)
						{
							Table += ",";
						}
					}
				}
				Table += ")";
			}
			return Table + "}" + ((Math.Sign(Value) < 0) ? " - " : " + ") + IntegerToStringBasic(Math.Abs(Value)) + ")";
		}

		public static List<string> GetIndexList()
		{
			List<string> Indicies = new List<string>();
			switch (Random.Next(0, 2))
			{
			case 0:
			{
				double Index = Random.Next(0, 1000000000);
				Indicies.Add($"[{Index}]");
				break;
			}
			case 1:
			{
				int Length = Random.Next(4, 10);
				string Index2 = Characters.Random().ToString();
				for (int I = 0; I < Length; I++)
				{
					Index2 += AlphaNumeric.Random();
				}
				Indicies.Add("." + Index2);
				Indicies.Add("[\"" + Index2 + "\"]");
				Indicies.Add("['" + Index2 + "']");
				break;
			}
			}
			return Indicies;
		}

		public static List<string> GetIndexListNoBrackets()
		{
			List<string> Indicies = new List<string>();
			switch (Random.Next(0, 2))
			{
			case 0:
			{
				double Index = 0.0;
				Index += (double)Random.Next(0, 1000000);
				if (Random.Next(0, 4) == 0)
				{
					Index += Random.NextDouble();
				}
				if (Random.Next(0, 2) == 0)
				{
					Index = 0.0 - Index;
				}
				Indicies.Add($"{Index}");
				break;
			}
			case 1:
			{
				int Length = Random.Next(2, 10);
				string Index2 = Characters.Random().ToString();
				for (int I = 0; I < Length; I++)
				{
					Index2 += AlphaNumeric.Random();
				}
				Indicies.Add("\"" + Index2 + "\"");
				Indicies.Add("'" + Index2 + "'");
				break;
			}
			}
			return Indicies;
		}
	}
}
