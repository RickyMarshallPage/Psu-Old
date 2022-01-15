using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GParse.Collections;
using GParse.Lexing;
using Loretta;
using Loretta.Lexing;
using Loretta.Parsing;
using Loretta.Parsing.AST;
using Loretta.Parsing.Visitor;
using LuaGeneration;
using Obfuscator.Bytecode;
using Obfuscator.Bytecode.IR;
using Obfuscator.Extensions;
using Obfuscator.Obfuscation.OpCodes;
using Obfuscator.Utility;

namespace Obfuscator.Obfuscation.Generation
{
	public class ScriptBuilder
	{
		public class Expression
		{
			public dynamic Data;

			public List<string> Indicies = new List<string>();
		}

		private List<string> extraStrings;

		private static Random Random = new Random();

		public static Encoding LuaEncoding = Encoding.GetEncoding(28591);

		private Chunk HeadChunk;

		private ObfuscationContext ObfuscationContext;

		private ObfuscationSettings ObfuscationSettings;

		private List<VOpCode> Virtuals;

		private string Variables;

		private string Functions;

		private string Deserializer;

		private string VM;

		private List<Expression> Expressions = new List<Expression>();

		private Dictionary<dynamic, Expression> ExpressionMap = new Dictionary<object, Expression>();

		private List<Expression> NumberExpressions = new List<Expression>();

		private Dictionary<long, Expression> NumberExpressionMap = new Dictionary<long, Expression>();

		private List<dynamic> UsedIndicies = new List<object>();

		private Dictionary<long, NumberEquation> NumberEquations = new Dictionary<long, NumberEquation>();

		private List<string> GenerateIndicies()
		{
			if (Random.Next(0, 2) == 0)
			{
				long Index2 = Random.Next(0, 1000000000);
				while (UsedIndicies.Contains(Index2))
				{
					Index2 = Random.Next(0, 1000000000);
				}
				UsedIndicies.Add("[" + Index2 + "]");
				return new List<string> { "[" + Index2 + "]" };
			}
			List<string> Indicies = Utility.Utility.GetIndexList();
			while (UsedIndicies.Contains(Indicies.First()))
			{
				Indicies = Utility.Utility.GetIndexList();
			}
			foreach (string Index in Indicies)
			{
				UsedIndicies.Add(Indicies.First());
			}
			return Indicies;
		}

		private long GenerateNumericIndex()
		{
			long Index = Random.Next(0, 1000000000);
			while (UsedIndicies.Contains(Index))
			{
				Index = Random.Next(0, 1000000000);
			}
			UsedIndicies.Add("[" + Index + "]");
			return Index;
		}

		private Expression AddExpression(dynamic Value)
		{
			if (((Dictionary<object, Expression>)ExpressionMap).ContainsKey(Value))
			{
				return ((Dictionary<object, Expression>)ExpressionMap)[Value];
			}
			Expression Expression = new Expression();
			Expression.Data = Value;
			Expression.Indicies = GenerateIndicies();
			if (Value is long)
			{
				NumberExpressions.Add(Expression);
				NumberExpressionMap[Value] = Expression;
			}
			Expressions.Add(Expression);
			((Dictionary<object, Expression>)ExpressionMap)[Value] = Expression;
			return Expression;
		}

		private string ToExpression(dynamic Value, string Type)
		{
			switch (Type)
			{
			case "String":
			{
				byte[] Bytes = LuaEncoding.GetBytes(Value);
				string String = "\"";
				bool IsString = true;
				byte[] array = Bytes;
				for (int i = 0; i < array.Length; i++)
				{
					byte Byte = array[i];
					if (Random.Next(0, 2) == 0)
					{
						string Chunk = "\\" + Byte;
						if (!IsString)
						{
							String = String + "..\"" + Chunk;
							IsString = true;
						}
						else
						{
							String += Chunk;
							IsString = true;
						}
						continue;
					}
					string Escape = "\\" + Byte;
					string Chunk2 = "T" + AddExpression("\"" + Escape + "\"").Indicies.Random();
					if (IsString)
					{
						String = String + "\".." + Chunk2;
						IsString = false;
					}
					else
					{
						String = String + ".." + Chunk2;
						IsString = false;
					}
				}
				return String + (IsString ? "\"" : "");
			}
			case "Number":
				return "T" + ((List<string>)AddExpression(Value).Indicies).Random();
			case "Raw String":
				return "T" + ((List<string>)AddExpression(Value).Indicies).Random();
			default:
				return "";
			}
		}

		private string GetLuaGeneration()
		{
			if (!ObfuscationSettings.EnhancedOutput)
			{
				return "";
			}
			return "PSU_LUA_GENERATION";
		}

		public string BasicRandomizeNumberStatement(long Number)
		{
			string Replacement = "";
			if (Random.Next(0, 2) == 0)
			{
				int XOR2 = Random.Next(0, 1000000000);
				Number ^= XOR2;
				return $"BitXOR({XOR2}, {Number})";
			}
			if (NumberExpressions.Count > 0 && Random.Next(0, 2) == 0)
			{
				Expression Expression2 = NumberExpressions.Random();
				Number ^= (long)Expression2.Data;
				return $"BitXOR({Number}, T{Expression2.Indicies.Random()})";
			}
			long XOR = Random.Next(0, 1000000000);
			Expression Expression = AddExpression(XOR);
			Number ^= XOR;
			return $"BitXOR({Number}, T{Expression.Indicies.Random()})";
		}

		public string RandomizeNumberStatement(long Number)
		{
			string Replacement = Number.ToString();
			if (ObfuscationSettings.MaximumSecurityEnabled)
			{
				switch (Random.Next(0, 7))
				{
				case 0:
				{
					long Index = NumberEquations.Keys.ToList().Random();
					NumberEquation NumberEquation = NumberEquations[Index];
					Replacement = $"Calculate({Index}, {NumberEquation.ComputeExpression(Number)})";
					break;
				}
				case 1:
				{
					long Index2 = NumberEquations.Keys.ToList().Random();
					NumberEquation NumberEquation2 = NumberEquations[Index2];
					Replacement = $"Calculate({BasicRandomizeNumberStatement(Index2)}, {NumberEquation2.ComputeExpression(Number)})";
					break;
				}
				case 2:
				{
					long Index3 = NumberEquations.Keys.ToList().Random();
					NumberEquation NumberEquation3 = NumberEquations[Index3];
					Replacement = "Calculate(" + BasicRandomizeNumberStatement(Index3) + ", " + BasicRandomizeNumberStatement(NumberEquation3.ComputeExpression(Number)) + ")";
					break;
				}
				case 3:
				{
					NumberEquation NumberEquation4 = new NumberEquation(Random.Next(3, 6));
					long Index4 = GenerateNumericIndex();
					Replacement = $"((Storage[{Index4}]) or (" + $"(function(Value) Storage[{Index4}] = {NumberEquation4.WriteStatement()}; return (Storage[{Index4}]); end)({BasicRandomizeNumberStatement(NumberEquation4.ComputeExpression(Number))})))";
					break;
				}
				case 4:
					Replacement = BasicRandomizeNumberStatement(Number);
					break;
				case 5:
				{
					NumberEquation NumberEquation5 = new NumberEquation(Random.Next(3, 6));
					long Index5 = GenerateNumericIndex();
					string Function = "(function(Value, BitXOR, Storage, Index) Storage[Index] = " + NumberEquation5.WriteStatement() + "; return (Storage[Index]); end)";
					Replacement = $"((Storage[{Index5}]) or (" + ToExpression(Function, "Raw String") + $"({BasicRandomizeNumberStatement(NumberEquation5.ComputeExpression(Number))}, BitXOR, Storage, {Index5})))";
					break;
				}
				}
			}
			return Replacement;
		}

		public string ExpandNumberStatements(string Source)
		{
			int SearchPosition = 0;
			while (SearchPosition < Source.Length)
			{
				string Substring = Source.Substring(SearchPosition);
				Match Match = Regex.Match(Substring, "[^\\\\0-9a-zA-Z\\.\"'](\\d+)[^0-9a-zA-Z\\.\"']");
				if (!Match.Success)
				{
					break;
				}
				if (!int.TryParse(Match.Groups[1].Value, out var Number))
				{
					SearchPosition += Match.Index + Match.Length;
					continue;
				}
				string Replacement = "(" + Utility.Utility.IntegerToString(Number) + ")";
				Source = Source.Substring(0, SearchPosition + Match.Index + 1) + Replacement + Source.Substring(SearchPosition + Match.Index + Match.Length - 1);
				SearchPosition += Match.Index + Replacement.Length;
			}
			return Source;
		}

		public string ReplaceNumbers(string Source)
		{
			List<int> Variables = new List<int>();
			int SearchPosition = 0;
			while (SearchPosition < Source.Length)
			{
				string Substring = Source.Substring(SearchPosition);
				Match Match = Regex.Match(Substring, "[^\\\\0-9a-zA-Z_.](\\d+)[^0-9a-zA-Z_.]");
				if (!Match.Success)
				{
					break;
				}
				if (!int.TryParse(Match.Groups[1].Value, out var Number))
				{
					SearchPosition = Match.Index + Match.Length;
					continue;
				}
				string Replacement = ToExpression(Number, "Number");
				if (!Variables.Contains(Number))
				{
					if (Variables.Count > 32)
					{
						SearchPosition = SearchPosition + Match.Index + Match.Length;
						continue;
					}
					Variables.Add(Number);
				}
				Source = Source.Substring(0, SearchPosition + Match.Index + 1) + $"V{Number}" + Source.Substring(SearchPosition + Match.Index + Match.Length - 1);
				SearchPosition = Match.Index + Replacement.Length;
			}
			Variables.Shuffle();
			foreach (int Number2 in Variables)
			{
				Source = $"local V{Number2} = T{ExpressionMap[Number2].Indicies.Random()}; \n" + Source;
			}
			return Source;
		}

		public ScriptBuilder(Chunk HeadChunk, ObfuscationContext ObfuscationContext, ObfuscationSettings ObfuscationSettings, List<VOpCode> Virtuals)
		{
			this.HeadChunk = HeadChunk;
			this.ObfuscationSettings = ObfuscationSettings;
			this.ObfuscationContext = ObfuscationContext;
			this.Virtuals = Virtuals;
			int Count = Random.Next(5, 15);
			for (int I = 0; I < Count; I++)
			{
				NumberEquations.Add(Random.Next(0, 1000000000), new NumberEquation(Random.Next(3, 6)));
			}
		}

		private void GenerateDeserializer()
		{
			Deserializer = "\r\n\r\n" + GetLuaGeneration() + "\r\n\r\n" + (ObfuscationSettings.MaximumSecurityEnabled ? "PSU_MAX_SECURITY_START()" : "") + "\r\n\r\nlocal function Deserialize(...) \r\n\t\t\t\t\t\r\n" + string.Join("\n", new List<string> { "\tlocal Instructions = ({});", "\tlocal Constants = ({});", "\tlocal Functions = ({});" }.Shuffle()) + "  \r\n\r\n\t\t\t";
			string InlinedGetBits17 = "";
			string InlinedGetBits16 = "";
			string InlinedGetBits18 = "";
			ChunkStep[] chunkSteps = ObfuscationContext.ChunkSteps;
			for (int i = 0; i < chunkSteps.Length; i++)
			{
				switch (chunkSteps[i])
				{
				case ChunkStep.StackSize:
					Deserializer = Deserializer + "\n\t" + GetInlinedOrDefault("local StackSize = gBits16(PrimaryXORKey);", "local StackSize = Value;", InlinedGetBits16, "PrimaryXORKey") + "\n";
					break;
				case ChunkStep.ParameterCount:
					Deserializer = Deserializer + "\n\t" + GetInlinedOrDefault("local ParameterCount = gBits8(PrimaryXORKey);", "local ParameterCount = Value;", InlinedGetBits18, "PrimaryXORKey") + "\n";
					break;
				case ChunkStep.Chunks:
					Deserializer = Deserializer + "\n\t" + GetInlinedOrDefault("for Index = 0, gBits32(PrimaryXORKey) - 1, 1 do", "for Index = 0, Value - 1, 1 do", InlinedGetBits17, "PrimaryXORKey") + " Functions[Index] = Deserialize(); end;\n";
					break;
				case ChunkStep.Instructions:
				{
					Deserializer += string.Format(" \r\n\r\n\t\t\t\t\t\t\t{0}\r\n\t\t\t\t\t\t\t\t{1}\r\n\t\t\t\t\t\t\t\t\r\n\t\t\t\t\t\t\t\tif (Type == {2}) then\r\n\t\t\t\t\t\t\t\t\t\r\n\t\t\t\t\t\t\t\t\t{3}\r\n\t\t\t\t\t\t\t\t\tConstants[Index] = (Bool ~= 0);\r\n\r\n\t\t\t\t\t\t\t\telseif (Type == {4}) then\r\n\r\n\t\t\t\t\t\t\t\t\twhile (true) do\r\n\t\t\t\t\t\t\t\t\t\t{5}\r\n\t\t\t\t\t\t\t\t\t\t{6}                                   \r\n\t\t\t\t\t\t\t\t\t\tlocal IsNormal = 1;\r\n\t\t\t\t\t\t\t\t\t\tlocal Mantissa = (gBit(Right, 1, 20) * (2 ^ 32)) + Left;\r\n\t\t\t\t\t\t\t\t\t\tlocal Exponent = gBit(Right, 21, 31);\r\n\t\t\t\t\t\t\t\t\t\tlocal Sign = ((-1) ^ gBit(Right, 32));\r\n\t\t\t\t\t\t\t\t\t\tif (Exponent == 0) then\r\n\t\t\t\t\t\t\t\t\t\t\tif (Mantissa == 0) then\r\n\t\t\t\t\t\t\t\t\t\t\t\tConstants[Index] = (Sign * 0);\r\n\t\t\t\t\t\t\t\t\t\t\t\tbreak;\r\n\t\t\t\t\t\t\t\t\t\t\telse\r\n\t\t\t\t\t\t\t\t\t\t\t\tExponent = 1;\r\n\t\t\t\t\t\t\t\t\t\t\t\tIsNormal = 0;\r\n\t\t\t\t\t\t\t\t\t\t\tend;\r\n\t\t\t\t\t\t\t\t\t\telseif(Exponent == 2047) then\r\n\t\t\t\t\t\t\t\t\t\t\tConstants[Index] = (Mantissa == 0) and (Sign * (1 / 0)) or (Sign * (0 / 0));\r\n\t\t\t\t\t\t\t\t\t\t\tbreak;\r\n\t\t\t\t\t\t\t\t\t\tend;\r\n\t\t\t\t\t\t\t\t\t\tConstants[Index] = LDExp(Sign, Exponent - 1023) * (IsNormal + (Mantissa / (2 ^ 52)));\r\n\t\t\t\t\t\t\t\t\t\tbreak;\r\n\t\t\t\t\t\t\t\t\tend;\r\n\r\n\t\t\t\t\t\t\t\telseif (Type == {7}) then\r\n\t\t\t\t\t\t\t\t   \r\n\t\t\t\t\t\t\t\t\twhile (true) do\r\n\t\t\t\t\t\t\t\t\t\t{8}\t\t\t                        \r\n\t\t\t\t\t\t\t\t\t\tif (Length == 0) then Constants[Index] = (''); break; end;\r\n\t\t\t\t\t\t\t\t\t   \r\n\t\t\t\t\t\t\t\t\t\tif (Length > 5000) then\r\n\t\t\t\t\t\t\t\t\t\t\tlocal Constant, ByteString = (''), (SubString(ByteString, Position, Position + Length - 1));\r\n\t\t\t\t\t\t\t\t\t\t\tPosition = Position + Length;\r\n\t\t\t\t\t\t\t\t\t\t\tfor Index = 1, #ByteString, 1 do local Byte = BitXOR(Byte(SubString(ByteString, Index, Index)), PrimaryXORKey); PrimaryXORKey = Byte % 256; Constant = Constant .. Dictionary[Byte]; end;\r\n\t\t\t\t\t\t\t\t\t\t\tConstants[Index] = Constant;\r\n\t\t\t\t\t\t\t\t\t\telse\r\n\t\t\t\t\t\t\t\t\t\t\tlocal Constant, Bytes = (''), ({{Byte(ByteString, Position, Position + Length - 1)}});\r\n\t\t\t\t\t\t\t\t\t\t\tPosition = Position + Length;        \r\n\t\t\t\t\t\t\t\t\t\t\tfor Index, Byte in Pairs(Bytes) do local Byte = BitXOR(Byte, PrimaryXORKey); PrimaryXORKey = Byte % 256; Constant = Constant .. Dictionary[Byte]; end;\t\t\t\t                        \r\n\t\t\t\t\t\t\t\t\t\t\tConstants[Index] = Constant;\r\n\t\t\t\t\t\t\t\t\t\tend;\r\n\r\n\t\t\t\t\t\t\t\t\t\tbreak;\r\n\t\t\t\t\t\t\t\t\tend;\r\n\r\n\t\t\t\t\t\t\t\telse\r\n\r\n\t\t\t\t\t\t\t\t   Constants[Index] = (nil);\r\n\r\n\t\t\t\t\t\t\t\tend;\r\n\t\t\t\t\t\t\tend;", GetInlinedOrDefault("for Index = 0, gBits32(PrimaryXORKey) - 1, 1 do", "for Index = 0, Value - 1, 1 do", InlinedGetBits17, "PrimaryXORKey"), GetInlinedOrDefault("local Type = gBits8(PrimaryXORKey);", "local Type = Value;", InlinedGetBits18, "PrimaryXORKey"), ObfuscationContext.ConstantMapping[1], GetInlinedOrDefault("local Bool = gBits8(PrimaryXORKey);", "local Bool = Value;", InlinedGetBits18, "PrimaryXORKey"), ObfuscationContext.ConstantMapping[2], GetInlinedOrDefault("local Left = gBits32(PrimaryXORKey);", "local Left = Value;", InlinedGetBits17, "PrimaryXORKey"), GetInlinedOrDefault("local Right = gBits32(PrimaryXORKey);", "local Right = Value;", InlinedGetBits17, "PrimaryXORKey"), ObfuscationContext.ConstantMapping[3], GetInlinedOrDefault("local Length = gBits32(PrimaryXORKey);", "local Length = Value;", InlinedGetBits17, "PrimaryXORKey"));
					Deserializer = Deserializer + "\r\n\r\n\t\t\t\t\t\t\t" + GetInlinedOrDefault("local Count = gBits32(PrimaryXORKey);", "local Count = Value;", InlinedGetBits17, "PrimaryXORKey") + " \r\n\t\t\t\t\t\t\tfor Index = 0, Count - 1, 1 do Instructions[Index] = ({}); end;\r\n\r\n\t\t\t\t\t\t\tfor Index = 0, Count - 1, 1 do\r\n\t\t\t\t\t\t\t\t" + GetInlinedOrDefault("local InstructionData = gBits8(PrimaryXORKey);", "local InstructionData = Value;", InlinedGetBits18, "PrimaryXORKey") + " \r\n\t\t\t\t\t\t\t\tif (InstructionData ~= 0) then \r\n\t\t\t\t\t\t\t\t\tInstructionData = InstructionData - 1;\r\n\t\t\t\t\t\t\t\t\tlocal " + string.Join(", ", new List<string> { "Enum", "A", "B", "C", "D", "E" }.Shuffle()) + " = 0, 0, 0, 0, 0, 0;\r\n\t\t\t\t\t\t\t\t\tlocal InstructionType = gBit(InstructionData, 1, 3);\r\n\t\t\r\n\t\t\t\t\t\t\t";
					List<InstructionType> InstructionTypes = new List<InstructionType>
					{
						InstructionType.ABC,
						InstructionType.ABx,
						InstructionType.AsBx,
						InstructionType.AsBxC,
						InstructionType.Closure,
						InstructionType.Compressed
					}.Shuffle().ToList();
					foreach (InstructionType InstructionType in InstructionTypes)
					{
						Deserializer += $"if (InstructionType == {(int)InstructionType}) then ";
						switch (InstructionType)
						{
						case InstructionType.ABC:
						{
							InstructionStep[] instructionSteps3 = ObfuscationContext.InstructionSteps;
							for (int l = 0; l < instructionSteps3.Length; l++)
							{
								switch (instructionSteps3[l])
								{
								case InstructionStep.Enum:
									Deserializer += GetInlinedOrDefault(" Enum = (gBits8(PrimaryXORKey));", " Enum = (Value);", InlinedGetBits18, "PrimaryXORKey");
									break;
								case InstructionStep.A:
									Deserializer += GetInlinedOrDefault(" A = (gBits16(PrimaryXORKey));", " A = (Value);", InlinedGetBits16, "PrimaryXORKey");
									break;
								case InstructionStep.B:
									Deserializer += GetInlinedOrDefault(" B = (gBits16(PrimaryXORKey));", " B = (Value);", InlinedGetBits16, "PrimaryXORKey");
									break;
								case InstructionStep.C:
									Deserializer += GetInlinedOrDefault(" C = (gBits16(PrimaryXORKey));", " C = (Value);", InlinedGetBits16, "PrimaryXORKey");
									break;
								}
							}
							break;
						}
						case InstructionType.ABx:
						{
							InstructionStep[] instructionSteps5 = ObfuscationContext.InstructionSteps;
							for (int n = 0; n < instructionSteps5.Length; n++)
							{
								switch (instructionSteps5[n])
								{
								case InstructionStep.Enum:
									Deserializer += GetInlinedOrDefault(" Enum = (gBits8(PrimaryXORKey));", " Enum = (Value);", InlinedGetBits18, "PrimaryXORKey");
									break;
								case InstructionStep.A:
									Deserializer += GetInlinedOrDefault(" A = (gBits16(PrimaryXORKey));", " A = (Value);", InlinedGetBits16, "PrimaryXORKey");
									break;
								case InstructionStep.B:
									Deserializer += GetInlinedOrDefault(" B = (gBits32(PrimaryXORKey));", " B = (Value);", InlinedGetBits17, "PrimaryXORKey");
									break;
								}
							}
							break;
						}
						case InstructionType.AsBx:
						{
							InstructionStep[] instructionSteps2 = ObfuscationContext.InstructionSteps;
							for (int k = 0; k < instructionSteps2.Length; k++)
							{
								switch (instructionSteps2[k])
								{
								case InstructionStep.Enum:
									Deserializer += GetInlinedOrDefault(" Enum = (gBits8(PrimaryXORKey));", " Enum = (Value);", InlinedGetBits18, "PrimaryXORKey");
									break;
								case InstructionStep.A:
									Deserializer += GetInlinedOrDefault(" A = (gBits16(PrimaryXORKey));", " A = (Value);", InlinedGetBits16, "PrimaryXORKey");
									break;
								case InstructionStep.B:
									Deserializer += GetInlinedOrDefault(" B = Instructions[(gBits32(PrimaryXORKey))];", " B = (Value);", InlinedGetBits17, "PrimaryXORKey");
									break;
								}
							}
							break;
						}
						case InstructionType.AsBxC:
						{
							InstructionStep[] instructionSteps4 = ObfuscationContext.InstructionSteps;
							for (int m = 0; m < instructionSteps4.Length; m++)
							{
								switch (instructionSteps4[m])
								{
								case InstructionStep.Enum:
									Deserializer += GetInlinedOrDefault(" Enum = (gBits8(PrimaryXORKey));", " Enum = (Value);", InlinedGetBits18, "PrimaryXORKey");
									break;
								case InstructionStep.A:
									Deserializer += GetInlinedOrDefault(" A = (gBits16(PrimaryXORKey));", " A = (Value);", InlinedGetBits16, "PrimaryXORKey");
									break;
								case InstructionStep.B:
									Deserializer += GetInlinedOrDefault(" B = Instructions[(gBits32(PrimaryXORKey))];", " B = Instructions[(Value)];", InlinedGetBits17, "PrimaryXORKey");
									break;
								case InstructionStep.C:
									Deserializer += GetInlinedOrDefault(" C = (gBits16(PrimaryXORKey));", " C = (Value);", InlinedGetBits16, "PrimaryXORKey");
									break;
								}
							}
							break;
						}
						case InstructionType.Closure:
						{
							InstructionStep[] instructionSteps = ObfuscationContext.InstructionSteps;
							for (int j = 0; j < instructionSteps.Length; j++)
							{
								switch (instructionSteps[j])
								{
								case InstructionStep.Enum:
									Deserializer += GetInlinedOrDefault(" Enum = (gBits8(PrimaryXORKey));", " Enum = (Value);", InlinedGetBits18, "PrimaryXORKey");
									break;
								case InstructionStep.A:
									Deserializer += GetInlinedOrDefault(" A = (gBits16(PrimaryXORKey));", " A = (Value);", InlinedGetBits16, "PrimaryXORKey");
									break;
								case InstructionStep.B:
									Deserializer += GetInlinedOrDefault(" B = (gBits32(PrimaryXORKey));", " B = (Value);", InlinedGetBits17, "PrimaryXORKey");
									break;
								case InstructionStep.C:
									Deserializer += GetInlinedOrDefault(" C = (gBits16(PrimaryXORKey));", " C = (Value);", InlinedGetBits16, "PrimaryXORKey");
									break;
								}
							}
							Deserializer += " D = ({}); for Index = 1, C, 1 do D[Index] = ({[0] = gBits8(PrimaryXORKey), [1] = gBits16(PrimaryXORKey)}); end; ";
							break;
						}
						}
						if (InstructionType != InstructionTypes.Last())
						{
							Deserializer += " else";
						}
					}
					Deserializer = Deserializer + " end; \r\n\r\n\t\t\t\t\t\t\t" + string.Join(" ", new List<string> { "if (gBit(InstructionData, 4, 4) == 1) then A = Constants[A]; end;", "if (gBit(InstructionData, 5, 5) == 1) then B = Constants[B]; end;", "if (gBit(InstructionData, 6, 6) == 1) then C = Constants[C]; end;", "if (gBit(InstructionData, 8, 8) == 1) then E = Instructions[gBits32(PrimaryXORKey)]; else E = Instructions[Index + 1]; end;" }.Shuffle()) + "\r\n\r\n\t\t\t\t\t\t\tif (gBit(InstructionData, 7, 7) == 1) then D = ({}); for Index = 1, gBits8(), 1 do D[Index] = gBits32(); end; end;\r\n\r\n\t\t\t\t\t\t\tlocal Instruction = Instructions[Index];\r\n\r\n\t\t\t\t\t\t\t" + string.Join(" ", new List<string>
					{
						"Instruction[" + ObfuscationContext.Instruction.Enum.Random() + "] = Enum;",
						"Instruction[" + ObfuscationContext.Instruction.A.Random() + "] = A;",
						"Instruction[" + ObfuscationContext.Instruction.B.Random() + "] = B;",
						"Instruction[" + ObfuscationContext.Instruction.C.Random() + "] = C;",
						"Instruction[" + ObfuscationContext.Instruction.D.Random() + "] = D;",
						"Instruction[" + ObfuscationContext.Instruction.E.Random() + "] = E;"
					}.Shuffle()) + " end; end;";
					break;
				}
				}
			}
			Deserializer = Deserializer + "\r\n\r\n\treturn ({\r\n\r\n" + string.Join("\n", new List<string>
			{
				"\t[" + ObfuscationContext.Chunk.InstructionPoint.Random() + "] = 0;",
				"\t[" + ObfuscationContext.Chunk.Instructions.Random() + "] = Instructions;",
				"\t[" + ObfuscationContext.Chunk.Constants.Random() + "] = Constants;",
				"\t[" + ObfuscationContext.Chunk.Chunks.Random() + "] = Functions;",
				"\t[" + ObfuscationContext.Chunk.StackSize.Random() + "] = StackSize;",
				"\t[" + ObfuscationContext.Chunk.ParameterCount.Random() + "] = ParameterCount;"
			}.Shuffle()) + "\r\n\r\n\t}); \r\n\r\nend; \r\n\r\n" + (ObfuscationSettings.MaximumSecurityEnabled ? "PSU_MAX_SECURITY_END()" : "") + "\n";
			static string GetInlinedOrDefault(string ToInline, string Inlined, string Function, dynamic XORKey)
			{
				if (Random.Next(1, 2) == 0)
				{
					return (Function + "\n" + Inlined).Replace("XOR_KEY", XORKey.ToString());
				}
				return ToInline;
			}
		}

		private void GenerateVM()
		{
			VM = "\r\n\r\n" + GetLuaGeneration() + "\r\n\r\nlocal function Wrap(Chunk, UpValues, Environment, ...)\r\n\t\t\t\t\r\n\t" + string.Join("\n", new List<string>
			{
				"\tlocal Instructions = Chunk[" + ObfuscationContext.Chunk.Instructions.Random() + "];",
				"\tlocal Functions = Chunk[" + ObfuscationContext.Chunk.Chunks.Random() + "];",
				"\tlocal ParameterCount = Chunk[" + ObfuscationContext.Chunk.ParameterCount.Random() + "];",
				"\tlocal Constants = Chunk[" + ObfuscationContext.Chunk.Constants.Random() + "];",
				"\tlocal InitialInstructionPoint = 0;",
				"\tlocal StackSize = Chunk[" + ObfuscationContext.Chunk.StackSize.Random() + "];"
			}.Shuffle()) + "\r\n\t\r\n\treturn (function(...)\r\n\r\n\t\t" + string.Join("\n", new List<string>
			{
				"\t\tlocal OP_A = " + ObfuscationContext.Instruction.A.Random() + ";",
				"\t\tlocal OP_B = " + ObfuscationContext.Instruction.B.Random() + ";",
				"\t\tlocal OP_C = " + ObfuscationContext.Instruction.C.Random() + ";",
				"\t\tlocal OP_D = " + ObfuscationContext.Instruction.D.Random() + ";",
				"\t\tlocal OP_E = " + ObfuscationContext.Instruction.E.Random() + ";",
				"\t\tlocal OP_ENUM = " + ObfuscationContext.Instruction.Enum.Random() + ";",
				"\t\tlocal Stack = {};",
				"\t\tlocal Top = -(1);",
				"\t\tlocal VarArg = {};",
				"\t\tlocal Arguments = {...};",
				"\t\tlocal PCount = (Select(Mode, ...) - 1);",
				"\t\tlocal InstructionPoint = Instructions[InitialInstructionPoint];",
				"\t\tlocal lUpValues = ({});",
				$"\t\tlocal VMKey = ({Random.Next(0, 1000000000)});",
				"\t\tlocal DecryptConstants = (true);"
			}.Shuffle()) + "\r\n\r\n\t\tfor Index = 0, PCount, 1 do\r\n\t\t\tif (Index >= ParameterCount) then\r\n\t\t\t\tVarArg[Index - ParameterCount] = Arguments[Index + 1];\r\n\t\t\telse\r\n\t\t\t\tStack[Index] = Arguments[Index + 1];\r\n\t\t\tend;\r\n\t\tend;\r\n\r\n\t\tlocal VarArgs = PCount - ParameterCount + 1;\r\n\r\n\t\twhile (true) do\r\n\t\t\tlocal Instruction = InstructionPoint;\t\r\n\t\t\tlocal Enum = Instruction[OP_ENUM];\r\n\t\t\tInstructionPoint = Instruction[OP_E];";
			VM += GetString(Enumerable.Range(0, Virtuals.Count).ToList());
			VM = VM + "\r\n\r\n\t\t\t\t\tend;\r\n\t\t\t\tend);\r\n\t\t\tend;\t\r\n\r\n\t\t\t" + GetLuaGeneration() + "\r\n\r\n\t\t\treturn Wrap(Deserialize(), {}, GetFEnv())(...);";
			string FormatEnum(int Enum)
			{
				return RandomizeNumberStatement(Enum);
			}
			string FormatVMHandle(VOpCode Virtual)
			{
				string Obfuscated = Virtual.GetObfuscated(ObfuscationContext);
				if (!ObfuscationSettings.ConstantEncryption || ObfuscationSettings.EnhancedConstantEncryption)
				{
					Obfuscated = Obfuscated.Replace("Constants[Instruction[OP_A]]", "Instruction[OP_A]");
					Obfuscated = Obfuscated.Replace("Constants[Instruction[OP_B]]", "Instruction[OP_B]");
					Obfuscated = Obfuscated.Replace("Constants[Instruction[OP_C]]", "Instruction[OP_C]");
				}
				return Obfuscated;
			}
			string GetString(List<int> OpCodes)
			{
				string String = "";
				if (OpCodes.Count == 1)
				{
					string Obfuscated2 = FormatVMHandle(Virtuals[OpCodes[0]]);
					String += Obfuscated2;
				}
				else if (OpCodes.Count == 2)
				{
					switch (Random.Next(0, 2))
					{
					case 0:
						String = String + "if (Enum > " + FormatEnum(Virtuals[OpCodes[0]].VIndex) + ") then\n" + FormatVMHandle(Virtuals[OpCodes[1]]);
						String = String + "elseif (Enum < " + FormatEnum(Virtuals[OpCodes[1]].VIndex) + ") then\n\n" + FormatVMHandle(Virtuals[OpCodes[0]]);
						String += "end;";
						break;
					case 1:
						String = String + "if (Enum == " + FormatEnum(Virtuals[OpCodes[0]].VIndex) + ") then\n" + FormatVMHandle(Virtuals[OpCodes[0]]);
						String = String + "elseif (Enum <= " + FormatEnum(Virtuals[OpCodes[1]].VIndex) + ") then\n" + FormatVMHandle(Virtuals[OpCodes[1]]);
						String += "end;";
						break;
					}
				}
				else
				{
					List<int> Ordered = OpCodes.OrderBy((int OpCode) => OpCode).ToList();
					List<int>[] Sorted = new List<int>[2]
					{
						Ordered.Take(Ordered.Count / 2).ToList(),
						Ordered.Skip(Ordered.Count / 2).ToList()
					};
					String = String + "if (Enum <= " + FormatEnum(Sorted[0].Last()) + ") then ";
					String += GetString(Sorted[0]);
					String += "else";
					String += GetString(Sorted[1]);
				}
				return String;
			}
		}

		private void GenerateHeader()
		{
			List<byte> Bytes = new Serializer(ObfuscationContext, ObfuscationSettings).Serialize(HeadChunk);
			string ByteString = Compression.CompressedToString(Compression.Compress(Bytes.ToArray()), ObfuscationSettings);
			string ByteCodeFormattingTable = "{";
			Dictionary<char, string> Replacements = new Dictionary<char, string>();
			string Base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
			string Pattern = "";
			if (ObfuscationSettings.ByteCodeMode == "Chinese")
			{
				string Format8 = "\ud847\udfc2\ud847\udfc3\ud847\udfc4\ud847\udfc5\ud847\udfc6\ud847\udfc7\ud847\udfc8\ud847\udfc9\ud847\udfca\ud847\udfcb\ud847\udfcc\ud847\udfcd\ud847\udfce\ud847\udfcf\ud847\udfd0\ud847\udfd1\ud847\udfd2\ud847\udfd3\ud847\udfd4\ud847\udfd5\ud847\udfd6\ud847\udfd7\ud847\udfd8\ud847\udfd9\ud847\udfda\ud847\udfdb\ud847\udfdc\ud847\udfdd\ud847\udfde\ud847\udfdf\ud847\udfe0\ud847\udfe1\ud847\udfe2\ud847\udfe3\ud847\udfe4\ud847\udfe5\ud845\udcf7\ud845\udcf8\ud845\udcf9\ud845\udcfa\ud845\udcfb\ud845\udcfc\ud845\udcfd\ud845\udcfe\ud845\udcff\ud845\udd00\ud845\udd01\ud845\udd02\ud845\udd03\ud845\udd04\ud845\udd05\ud845\udd06\ud845\udd07\ud845\udd08\ud845\udd09\ud845\udd0a\ud845\udd0b\ud845\udd0c\ud845\udd0d\ud845\udd0e\ud845\udd0f\ud845\udd10\ud845\udd11\ud845\udd12\ud845\udd13\ud845\udd14\ud845\udd15\ud845\udd16\ud845\udd17\ud845\udd18\ud845\udd19\ud845\udd1a";
				Pattern = "....";
				List<string> Characters8 = new List<string>();
				for (int I16 = 0; I16 < Format8.Length; I16 += 2)
				{
					string Section8 = Format8.Substring(I16, 2);
					Characters8.Add(Section8);
				}
				for (int I15 = 0; I15 < Base36.Length; I15++)
				{
					ByteString = ByteString.Replace(Base36[I15].ToString(), Characters8[I15]);
					Replacements[Base36[I15]] = Characters8[I15];
					ByteCodeFormattingTable += $"[\"{Characters8[I15]}\"]=\"{Base36[I15]}\";";
				}
				ByteString = "[=[PSU|" + ByteString + "]=]";
			}
			else if (ObfuscationSettings.ByteCodeMode == "Arabic")
			{
				string Format7 = "ٶٷٸٺٻټٽپٿڀځڂڃڄڅچڇڈډڊڋڌڍڎڏڐڑڒݐݑݒݓݔݕݖݗݘݙݚݛݜݝݞݟݠݡݢݣݤݥݦݧݨݩݪݫݬݭݮݯݰݱݲݳݴݵݶݷݸݹݺݻݼݽݾݿ";
				Pattern = "..";
				List<string> Characters7 = new List<string>();
				for (int I14 = 0; I14 < Format7.Length; I14++)
				{
					string Section7 = Format7.Substring(I14, 1);
					Characters7.Add(Section7);
				}
				for (int I13 = 0; I13 < Base36.Length; I13++)
				{
					ByteString = ByteString.Replace(Base36[I13].ToString(), Characters7[I13]);
					Replacements[Base36[I13]] = Characters7[I13];
					ByteCodeFormattingTable += $"[\"{Characters7[I13]}\"]=\"{Base36[I13]}\";";
				}
				ByteString = "[=[PSU|" + ByteString + "]=]";
			}
			else if (ObfuscationSettings.ByteCodeMode == "Symbols1")
			{
				string Format6 = "ꀀꀁꀂꀃꀄꀅꀆꀇꀈꀉꀊꀋꀌꀍꀎꀏꀐꀑꀒꀓꀔꀕꀖꀗꀘꀙꀚꀛꀜꀝꀞꀟꀠꀡꀢꀣꀤꀥꀦꀧꀨꀩꀪꀫꀬꀭꀮꀯꀰꀱꀲꀳꀴꀵꀶꀷꀸꀹꀺꀻꀼꀽꀾꀿꁀꁁꁂꁃꁄꁅꁆꁇꁈꁉꁊꁋꁌꁍꁎꁏꁐꁑꁒꁓꁔꁕꁖꁗꁘꁙꁚꁛ";
				Pattern = "...";
				List<string> Characters6 = new List<string>();
				for (int I12 = 0; I12 < Format6.Length; I12++)
				{
					string Section6 = Format6.Substring(I12, 1);
					Characters6.Add(Section6);
				}
				for (int I11 = 0; I11 < Base36.Length; I11++)
				{
					ByteString = ByteString.Replace(Base36[I11].ToString(), Characters6[I11]);
					Replacements[Base36[I11]] = Characters6[I11];
					ByteCodeFormattingTable += $"[\"{Characters6[I11]}\"]=\"{Base36[I11]}\";";
				}
				ByteString = "[=[PSU|" + ByteString + "]=]";
			}
			else if (ObfuscationSettings.ByteCodeMode == "Korean")
			{
				string Format5 = "뗬뗭뗮뗯뗰뗱뗲뗳뗴뗵뗶뗷뗸뗹뗺뗻뗼뗽뗾뗿똀똁똂똃똄똅똆똇똈똉똊똋똌똍똎똏또똑똒똓똔똕똖똗똘똙똚똛똜똝똞똟똠똡똢똣똤똥똦똧똨똩똪똫똬똭똮똯똰똱똲똳똴똵똶똷똸똹똺똻";
				Pattern = "...";
				List<string> Characters5 = new List<string>();
				for (int I10 = 0; I10 < Format5.Length; I10++)
				{
					string Section5 = Format5.Substring(I10, 1);
					Characters5.Add(Section5);
				}
				for (int I9 = 0; I9 < Base36.Length; I9++)
				{
					ByteString = ByteString.Replace(Base36[I9].ToString(), Characters5[I9]);
					Replacements[Base36[I9]] = Characters5[I9];
					ByteCodeFormattingTable += $"[\"{Characters5[I9]}\"]=\"{Base36[I9]}\";";
				}
				ByteString = "[=[PSU|" + ByteString + "]=]";
			}
			else if (ObfuscationSettings.ByteCodeMode == "Symbols2")
			{
				string Format4 = "\ud808\ude67\ud808\ude68\ud808\ude69\ud808\ude6a\ud808\ude6b\ud808\ude6c\ud808\ude6d\ud808\ude6e\ud808\ude6f\ud808\ude70\ud808\ude71\ud808\ude72\ud808\ude73\ud808\ude74\ud808\ude75\ud808\ude76\ud808\ude77\ud808\ude78\ud808\ude79\ud808\ude7a\ud808\ude7b\ud808\ude7c\ud808\ude7d\ud808\ude7e\ud808\ude7f\ud808\ude80\ud808\ude81\ud808\ude82\ud808\ude83\ud808\ude84\ud808\ude85\ud808\ude86\ud808\ude87\ud808\ude88\ud808\ude89\ud808\ude8a\ud808\ude8b\ud808\ude8c\ud808\ude8d\ud808\ude8e\ud808\ude8f\ud808\ude90\ud808\ude91\ud808\ude92\ud808\ude93\ud808\ude94\ud808\ude95\ud808\ude96\ud808\ude97\ud808\ude98\ud808\ude99\ud808\ude9a\ud808\ude9b\ud808\ude9c\ud808\ude9d\ud808\ude9e\ud808\ude9f\ud808\udea0\ud808\udea1\ud808\udea2\ud808\udea3\ud808\udea4\ud808\udea5\ud808\udea6\ud808\udea7\ud808\udea8\ud808\udea9\ud808\udeaa\ud808\udeab\ud808\udeac\ud808\udead\ud808\udeae\ud808\udeaf\ud808\udeb0\ud808\udeb1\ud808\udeb2\ud808\udeb3\ud808\udeb4\ud808\udeb5\ud808\udeb6\ud808\udeb7\ud808\udeb8\ud808\udeb9\ud808\udeba\ud808\udebb\ud808\udebc\ud808\udebd\ud808\udebe\ud808\udebf\ud808\udec0\ud808\udec1\ud808\udec2\ud808\udec3\ud808\udec4\ud808\udec5\ud808\udec6\ud808\udec7";
				Pattern = "....";
				List<string> Characters4 = new List<string>();
				for (int I8 = 0; I8 < Format4.Length; I8 += 2)
				{
					string Section4 = Format4.Substring(I8, 2);
					Characters4.Add(Section4);
				}
				for (int I7 = 0; I7 < Base36.Length; I7++)
				{
					ByteString = ByteString.Replace(Base36[I7].ToString(), Characters4[I7]);
					Replacements[Base36[I7]] = Characters4[I7];
					ByteCodeFormattingTable += $"[\"{Characters4[I7]}\"]=\"{Base36[I7]}\";";
				}
				ByteString = "[=[PSU|" + ByteString + "]=]";
			}
			else if (ObfuscationSettings.ByteCodeMode == "Symbols3")
			{
				string Format3 = "\ud809\udc0b\ud809\udc0c\ud809\udc0d\ud809\udc0e\ud809\udc0f\ud809\udc10\ud809\udc11\ud809\udc12\ud809\udc13\ud809\udc14\ud809\udc15\ud809\udc16\ud809\udc17\ud809\udc18\ud809\udc19\ud809\udc1a\ud809\udc1b\ud809\udc1c\ud809\udc1d\ud809\udc1e\ud809\udc1f\ud809\udc20\ud809\udc21\ud809\udc22\ud809\udc23\ud809\udc24\ud809\udc25\ud809\udc26\ud809\udc27\ud809\udc28\ud809\udc29\ud809\udc2a\ud809\udc2b\ud809\udc2c\ud809\udc2d\ud809\udc2e\ud809\udc2f\ud809\udc30\ud809\udc31\ud809\udc32\ud809\udc33\ud809\udc34\ud809\udc35\ud809\udc36\ud809\udc37\ud809\udc38\ud809\udc39\ud809\udc3a\ud809\udc3b\ud809\udc3c\ud809\udc3d\ud809\udc3e\ud809\udc3f\ud809\udc40\ud809\udc41\ud809\udc42\ud809\udc43\ud809\udc44\ud809\udc45\ud809\udc46\ud809\udc47\ud809\udc48\ud809\udc49\ud809\udc4a\ud809\udc4b\ud809\udc4c\ud809\udc4d\ud809\udc4e\ud809\udc4f\ud809\udc50\ud809\udc51\ud809\udc52\ud809\udc53\ud809\udc54\ud809\udc55\ud809\udc56\ud809\udc57\ud809\udc58\ud809\udc59\ud809\udc5a\ud809\udc5b\ud809\udc5c\ud809\udc5d\ud809\udc5e\ud809\udc5f\ud809\udc60\ud809\udc61\ud809\udc62\ud809\udc63\ud809\udc64\ud809\udc65\ud809\udc66\ud809\udc67\ud809\udc68\ud809\udc69\ud809\udc6a";
				Pattern = "....";
				List<string> Characters3 = new List<string>();
				for (int I6 = 0; I6 < Format3.Length; I6 += 2)
				{
					string Section3 = Format3.Substring(I6, 2);
					Characters3.Add(Section3);
				}
				for (int I5 = 0; I5 < Base36.Length; I5++)
				{
					ByteString = ByteString.Replace(Base36[I5].ToString(), Characters3[I5]);
					Replacements[Base36[I5]] = Characters3[I5];
					ByteCodeFormattingTable += $"[\"{Characters3[I5]}\"]=\"{Base36[I5]}\";";
				}
				ByteString = "[=[PSU|" + ByteString + "]=]";
			}
			else if (ObfuscationSettings.ByteCodeMode == "Emoji")
			{
				string Format2 = "\ud83c\udf45\ud83c\udf46\ud83c\udf47\ud83c\udf48\ud83c\udf49\ud83c\udf4a\ud83c\udf4b\ud83c\udf4c\ud83c\udf4d\ud83c\udf4e\ud83c\udf4f\ud83c\udf50\ud83c\udf51\ud83c\udf52\ud83c\udf53\ud83c\udf54\ud83c\udf55\ud83c\udf56\ud83c\udf57\ud83c\udf58\ud83d\udd0f\ud83d\udd10\ud83d\udd11\ud83d\udd12\ud83d\udd13\ud83d\udd14\ud83d\ude00\ud83d\ude01\ud83d\ude02\ud83d\ude03\ud83d\ude04\ud83d\ude05\ud83d\ude06\ud83d\ude07\ud83d\ude08\ud83d\ude09\ud83d\ude0a\ud83d\ude0b\ud83d\ude0c\ud83d\ude0d\ud83d\ude0e\ud83d\ude0f\ud83d\ude10\ud83d\ude11\ud83d\ude12\ud83d\ude13\ud83d\ude14\ud83d\ude15\ud83d\ude16\ud83d\ude17\ud83d\ude18\ud83d\ude19\ud83d\ude1a\ud83d\ude1b\ud83d\ude1c\ud83d\ude1d\ud83d\ude1e\ud83d\ude1f\ud83d\ude20\ud83d\ude21\ud83d\ude22\ud83d\ude23\ud83d\ude24\ud83d\ude25\ud83d\ude26\ud83d\ude27\ud83d\ude28\ud83d\ude29\ud83d\ude2a\ud83d\ude2b\ud83d\ude2c\ud83d\ude2d\ud83d\ude2e\ud83d\ude2f\ud83d\ude30\ud83d\ude31";
				Pattern = "....";
				List<string> Characters2 = new List<string>();
				for (int I4 = 0; I4 < Format2.Length; I4 += 2)
				{
					string Section2 = Format2.Substring(I4, 2);
					Characters2.Add(Section2);
				}
				for (int I3 = 0; I3 < Base36.Length; I3++)
				{
					ByteString = ByteString.Replace(Base36[I3].ToString(), Characters2[I3]);
					Replacements[Base36[I3]] = Characters2[I3];
					ByteCodeFormattingTable += $"[\"{Characters2[I3]}\"]=\"{Base36[I3]}\";";
				}
				ByteString = "[=[PSU|" + ByteString + "]=]";
			}
			else if (!(ObfuscationSettings.ByteCodeMode == "Greek"))
			{
				ByteString = ((!(ObfuscationSettings.ByteCodeMode == "Default")) ? ("\"PSU|" + ByteString + "\"") : ("\"PSU|" + ByteString + "\""));
			}
			else
			{
				string Format = "\ud835\udf44\ud835\udf45\ud835\udf46\ud835\udf47\ud835\udf48\ud835\udf49\ud835\udf4a\ud835\udf4b\ud835\udf4c\ud835\udf4d\ud835\udf4e\ud835\udf4f\ud835\udf50\ud835\udf51\ud835\udf52\ud835\udf53\ud835\udf54\ud835\udf55\ud835\udf56\ud835\udf57\ud835\udf58\ud835\udf59\ud835\udf5a\ud835\udf5b\ud835\udf5c\ud835\udf5d\ud835\udf5e\ud835\udf5f\ud835\udf60\ud835\udf61\ud835\udf62\ud835\udf63\ud835\udf64\ud835\udf65\ud835\udf66\ud835\udf67\ud835\udf68\ud835\udf69\ud835\udf6a\ud835\udf6b\ud835\udf6c\ud835\udf6d\ud835\udf6e\ud835\udf6f\ud835\udf70\ud835\udf71\ud835\udf72\ud835\udf73\ud835\udf74\ud835\udf75\ud835\udf76\ud835\udf77\ud835\udf78\ud835\udf79\ud835\udf7a\ud835\udf7b\ud835\udf7c\ud835\udf7d\ud835\udf7e\ud835\udf7f\ud835\udf80\ud835\udf81\ud835\udf82\ud835\udf83\ud835\udf84\ud835\udf85\ud835\udf86";
				Pattern = "....";
				List<string> Characters = new List<string>();
				for (int I2 = 0; I2 < Format.Length; I2 += 2)
				{
					string Section = Format.Substring(I2, 2);
					Characters.Add(Section);
				}
				for (int I = 0; I < Base36.Length; I++)
				{
					ByteString = ByteString.Replace(Base36[I].ToString(), Characters[I]);
					Replacements[Base36[I]] = Characters[I];
					ByteCodeFormattingTable += $"[\"{Characters[I]}\"]=\"{Base36[I]}\";";
				}
				ByteString = "[=[PSU|" + ByteString + "]=]";
			}
			ObfuscationContext.ByteCode = ByteString;
			ByteCodeFormattingTable += "}";
			if (ObfuscationSettings.ByteCodeMode != "Default")
			{
				ObfuscationContext.FormatTable = ByteCodeFormattingTable;
				ByteCodeFormattingTable = "ByteString = GSub(ByteString, \"" + Pattern + "\", PSU_FORMAT_TABLE);";
			}
			else
			{
				ByteCodeFormattingTable = "";
			}
			Variables = string.Format("\r\n\r\nlocal GetFEnv = ((getfenv) or (function(...) return (_ENV); end));\r\nlocal Storage, _, Environment = ({{}}), (\"\"), (GetFEnv(1));\r\n\r\nlocal bit32 = ((Environment[{0}]) or (Environment[{1}]) or ({{}})); \r\nlocal BitXOR = (((bit32) and (bit32[{2}])) or (function(A, B) local P, C = 1, 0; while ((A > 0) and (B > 0)) do local X, Y = A % 2, B % 2; if X ~= Y then C = C + P; end; A, B, P = (A - X) / 2, (B - Y) / 2, P * 2; end; if A < B then A = B; end; while A > 0 do local X = A % 2; if X > 0 then C = C + P; end; A, P =(A - X) / 2, P * 2; end; return (C); end));\r\n\r\nlocal MOD = (2 ^ 32);\r\nlocal MODM = (MOD - 1);\r\nlocal BitSHL, BitSHR, BitAND;\r\n\r\n{3}\r\n\r\n{4}\r\n\r\n{5}\r\n\r\n{6}\r\n\r\n{7}\r\n\r\n{8}\r\n\r\nif ((not (Environment[{9}])) and (not (Environment[{10}]))) then\r\n\r\n{11}\r\n\r\nend;\r\n\r\n{12}\r\n\r\n{13} \r\n\r\nEnvironment[{14}] = bit32;\r\n\r\nlocal PrimaryXORKey = ({15});\r\n\r\n{16}\r\n\r\nlocal F = (256); local G, Dictionary = ({{}}), ({{}}); for H = 0, F - 1 do local Value = Character(H); G[H] = Value; Dictionary[H] = Value; Dictionary[Value] = H; end;\r\nlocal ByteString, Position = (function(ByteString) local X, Y, Z = 0,0,248 if ((X + Y + Z) ~= 248) then PrimaryXORKey = PrimaryXORKey + {17}; F = F + {18}; end; ByteString = SubString(ByteString, 5); {19} local C, D, E = (\"\"), (\"\"), ({{}}); local I = 1; local function K() local L = ToNumber(SubString(ByteString, I, I), 36); I = I + 1; local M = ToNumber(SubString(ByteString, I, I + L - 1), 36); I = I + L; return (M); end; C = Dictionary[K()]; E[1] = C; while (I < #ByteString) do local N = K(); if G[N] then D = G[N]; else D = C .. SubString(C, 1, 1); end; G[F] = C .. SubString(D, 1, 1); E[#E + 1], C, F = D, D, F + 1; end; return (Concatenate(E)); end)(PSU_BYTECODE), (1);", ToExpression("bit32", "String"), ToExpression("bit", "String"), ToExpression("bxor", "String"), GetLuaGeneration(), string.Join("\n", new List<string>
			{
				"local Byte = (_[" + ToExpression("byte", "String") + "]);",
				"local Character = (_[" + ToExpression("char", "String") + "]);",
				"local SubString = (_[" + ToExpression("sub", "String") + "]);",
				"local GSub = (_[" + ToExpression("gsub", "String") + "]);"
			}.Shuffle()), GetLuaGeneration(), string.Join("\n", new List<string>
			{
				"local RawSet = (Environment[" + ToExpression("rawset", "String") + "]);",
				"local Pairs = (Environment[" + ToExpression("pairs", "String") + "]);",
				"local ToNumber = (Environment[" + ToExpression("tonumber", "String") + "]);",
				"local SetMetaTable = (Environment[" + ToExpression("setmetatable", "String") + "]);",
				"local Select = (Environment[" + ToExpression("select", "String") + "]);",
				"local Type = (Environment[" + ToExpression("type", "String") + "]);",
				"local UnPack = ((Environment[" + ToExpression("unpack", "String") + "]) or (Environment[" + ToExpression("table", "String") + "][" + ToExpression("unpack", "String") + "]));",
				"local LDExp = ((Environment[" + ToExpression("math", "String") + "][" + ToExpression("ldexp", "String") + "]) or (function(Value, Exponent, ...) return ((Value * 2) ^ Exponent); end));",
				"local Floor = (Environment[" + ToExpression("math", "String") + "][" + ToExpression("floor", "String") + "]);"
			}.Shuffle()), GetLuaGeneration(), string.Join("\n", new List<string>
			{
				"BitAND = (bit32[" + ToExpression("band", "String") + "]) or (function(A, B, ...) return (((A + B) - BitXOR(A, B)) / 2); end);",
				"local BitOR = (bit32[" + ToExpression("bor", "String") + "]) or (function(A, B, ...) return (MODM - BitAND(MODM - A, MODM - B)); end);",
				"local BitNOT = (bit32[" + ToExpression("bnot", "String") + "]) or (function(A, ...) return (MODM - A); end);",
				"BitSHL = ((bit32[" + ToExpression("lshift", "String") + "]) or (function(A, B, ...) if (B < 0) then return (BitSHR(A, -(B))); end; return ((A * 2 ^ B) % 2 ^ 32); end));",
				"BitSHR = ((bit32[" + ToExpression("rshift", "String") + "]) or (function(A, B, ...) if (B < 0) then return (BitSHL(A, -(B))); end; return (Floor(A % 2 ^ 32 / 2 ^ B)); end));"
			}.Shuffle()), ToExpression("bit32", "String"), ToExpression("bit", "String"), string.Join("\n", new List<string>
			{
				"bit32[" + ToExpression("bxor", "String") + "] = BitXOR;",
				"bit32[" + ToExpression("band", "String") + "] = BitAND;",
				"bit32[" + ToExpression("bor", "String") + "] = BitOR;",
				"bit32[" + ToExpression("bnot", "String") + "] = BitNOT;",
				"bit32[" + ToExpression("lshift", "String") + "] = BitSHL;",
				"bit32[" + ToExpression("rshift", "String") + "] = BitSHR;"
			}.Shuffle()), GetLuaGeneration(), string.Join("\n", new List<string>
			{
				"local Remove = (Environment[" + ToExpression("table", "String") + "][" + ToExpression("remove", "String") + "]);",
				"local Insert = (Environment[" + ToExpression("table", "String") + "][" + ToExpression("insert", "String") + "]);",
				"local Concatenate = (Environment[" + ToExpression("table", "String") + "][" + ToExpression("concat", "String") + "]);",
				"local Create = (((Environment[" + ToExpression("table", "String") + "][" + ToExpression("create", "String") + "])) or ((function(Size, ...) return ({ UnPack({}, 0, Size); }); end)));"
			}.Shuffle()), ToExpression("bit32", "String"), ObfuscationContext.InitialPrimaryXORKey, GetLuaGeneration(), Random.Next(0, 256), Random.Next(0, 256), ByteCodeFormattingTable);
			Functions = "\r\n\r\n" + GetLuaGeneration() + "\r\n\r\n" + string.Join("\n", new List<string> { "local function gBits32() local W, X, Y, Z = Byte(ByteString, Position, Position + 3); W = BitXOR(W, PrimaryXORKey); PrimaryXORKey = W % 256; X = BitXOR(X, PrimaryXORKey); PrimaryXORKey = X % 256; Y = BitXOR(Y, PrimaryXORKey); PrimaryXORKey = Y % 256; Z = BitXOR(Z, PrimaryXORKey); PrimaryXORKey = Z % 256; Position = Position + 4; return ((Z * 16777216) + (Y * 65536) + (X * 256) + W); end;", "local function gBits16() local W, X = Byte(ByteString, Position, Position + 2); W = BitXOR(W, PrimaryXORKey); PrimaryXORKey = W % 256; X = BitXOR(X, PrimaryXORKey); PrimaryXORKey = X % 256; Position = Position + 2; return ((X * 256) + W); end;", "local function gBits8() local F = BitXOR(Byte(ByteString, Position, Position), PrimaryXORKey); PrimaryXORKey = F % 256; Position = (Position + 1); return (F); end;", "local function gBit(Bit, Start, End) if (End) then local R = (Bit / 2 ^ (Start - 1)) % 2 ^ ((End - 1) - (Start - 1) + 1); return (R - (R % 1)); else local P = 2 ^ (Start - 1); return (((Bit % (P + P) >= P) and (1)) or(0)); end; end;" }.Shuffle()) + " \r\n\r\nlocal Mode = " + ToExpression("#", "String") + "; local function _R(...) return ({...}), Select(Mode, ...); end;";
		}

		public string BuildScript(string Location)
		{
			GenerateHeader();
			GenerateDeserializer();
			GenerateVM();
			if (!ObfuscationSettings.CompressedOutput)
			{
				Deserializer = ReplaceNumbers(Deserializer);
				Deserializer = ExpandNumberStatements(Deserializer);
				Deserializer = "local function Deserialize(...) " + Deserializer + " return (Deserialize(...)); end;";
				Variables = ReplaceNumbers(Variables);
				Variables = ExpandNumberStatements(Variables);
				if (ObfuscationSettings.MaximumSecurityEnabled)
				{
					Variables += "local function Calculate(Index, Value, ...)";
					foreach (KeyValuePair<long, NumberEquation> NumberEquationPair in NumberEquations)
					{
						Variables += $"if (Index == {NumberEquationPair.Key}) then return ({NumberEquationPair.Value.WriteStatement()});";
						if (NumberEquationPair.Key != NumberEquations.Last().Key)
						{
							Variables += "else";
						}
					}
					Variables += " end; end;";
				}
				Functions = ReplaceNumbers(Functions);
				Functions = ExpandNumberStatements(Functions);
			}
			bool flag = true;
			Variables += "local function CalculateVM(Index, Value, ...)";
			foreach (KeyValuePair<long, NumberEquation> NumberEquationPair2 in ObfuscationContext.NumberEquations)
			{
				Variables += $"if (Index == {NumberEquationPair2.Key}) then return ({NumberEquationPair2.Value.WriteStatement()});";
				if (NumberEquationPair2.Key != NumberEquations.Last().Key)
				{
					Variables += "else";
				}
			}
			Variables += " end; end;";
			string VMTable = "";
			Expressions.Shuffle();
			foreach (Expression Expression in Expressions)
			{
				string Index = Expression.Indicies.Random();
				if (Index.StartsWith("."))
				{
					Index = Index.Remove(0, 1);
				}
				VMTable += $"{Index}=({(object?)Expression.Data});";
			}
			if (!ObfuscationSettings.CompressedOutput)
			{
				VMTable = ExpandNumberStatements(VMTable);
			}
			string Source = "return (function(T, ...) local TEXT = \"This file was obfuscated using the 6 month old PSU Obfuscator\"; " + Variables + Functions + Deserializer + VM;
			Source = Source + " \nend)(({" + VMTable + "}), ...);";
			if (ObfuscationSettings.EnhancedOutput)
			{
				Match Match;
				string Replacement;
				for (int SearchPosition = 0; SearchPosition < Source.Length; SearchPosition += Match.Index + Replacement.Length)
				{
					string Substring = Source.Substring(SearchPosition);
					Match = Regex.Match(Substring, "PSU_LUA_GENERATION");
					if (Match.Success)
					{
						Replacement = GenerateCode();
						Source = Source.Substring(0, SearchPosition + Match.Index) + Replacement + Source.Substring(SearchPosition + Match.Index + Match.Length);
						continue;
					}
					break;
				}
			}
			if (ObfuscationSettings.MaximumSecurityEnabled)
			{
				LuaOptions LuaOptions = new LuaOptions(acceptBinaryNumbers: true, acceptCCommentSyntax: false, acceptCompoundAssignment: true, acceptEmptyStatements: false, acceptGModCOperators: true, acceptGoto: true, acceptHexEscapesInStrings: true, acceptHexFloatLiterals: true, acceptOctalNumbers: true, acceptShebang: false, acceptUnderlineInNumberLiterals: true, useLuaJitIdentifierRules: true, ContinueType.ContextualKeyword);
				LuaLexerBuilder LexerBuilder = new LuaLexerBuilder(LuaOptions);
				LuaParserBuilder ParserBuilder = new LuaParserBuilder(LuaOptions);
				DiagnosticList Diagnostics = new DiagnosticList();
				ILexer<LuaTokenType> Lexer = LexerBuilder.CreateLexer(Source, Diagnostics);
				TokenReader<LuaTokenType> TokenReader = new TokenReader<LuaTokenType>(Lexer);
				LuaParser Parser = ParserBuilder.CreateParser(TokenReader, Diagnostics);
				StatementList Tree = Parser.Parse();
				Source = VMFormattedLuaCodeSerializer.Format(LuaOptions.All, Tree);
			}
			File.WriteAllText(Path.Combine(Location, "VM.lua"), Source);
			Process process = new Process();
			process.StartInfo.WorkingDirectory = "Lua/lua";
			process.StartInfo.FileName = "Lua/LuaJIT.exe";
			process.StartInfo.Arguments = "LuaSrcDiet.lua --maximum --opt-entropy --opt-emptylines --opt-eols --opt-numbers --opt-whitespace --opt-locals --noopt-strings \"" + Path.GetFullPath(Path.Combine(Location, "VM.lua")) + "\" -o \"" + Path.GetFullPath(Path.Combine(Location, "Output.lua")) + "\"";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardError = !ObfuscationSettings.DebugMode;
			process.StartInfo.RedirectStandardOutput = !ObfuscationSettings.DebugMode;
			Process Process = process;
			Process.Start();
			Process.WaitForExit();
			Source = File.ReadAllText(Path.Combine(Location, "Output.lua"), LuaEncoding).Replace("\n", " ");
			Source = Source.Replace("[.", "[0.");
			Source = Utility.Utility.FinalReplaceStrings(Source);
			File.WriteAllText(Path.Combine(Location, "Output.lua"), Source.Replace("PSU_BYTECODE", ObfuscationContext.ByteCode).Replace("PSU_FORMAT_TABLE", ObfuscationContext.FormatTable));
			return Source;
			string GenerateCode()
			{
				if (!ObfuscationSettings.EnhancedOutput)
				{
					return "";
				}
				return Random.Next(0, 3) switch
				{
					1 => " do local function _(...) " + global::LuaGeneration.LuaGeneration.GenerateRandomFile(1, 1) + " end; end; ", 
					0 => " do local function _(...) " + global::LuaGeneration.LuaGeneration.VarArgSpam(1, 1) + " end; end; ", 
					2 => "", 
					_ => "", 
				};
			}
		}
	}
}
