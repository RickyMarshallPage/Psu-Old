using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Obfuscator.Bytecode.IR;
using Obfuscator.Extensions;
using Obfuscator.Utility;

namespace Obfuscator.Bytecode
{
	public class Deserializer
	{
		private MemoryStream MemoryStream;

		private bool Endian;

		private byte SizeNumber;

		private byte SizeSizeT;

		private bool ExpectingSetListData;

		private const bool BYTECODE_OPTIMIZATIONS = false;

		private static Encoding LuaEncoding = Encoding.GetEncoding(28591);

		public static Dictionary<OpCode, InstructionType> InstructionMappings = new Dictionary<OpCode, InstructionType>
		{
			{
				OpCode.OpMove,
				InstructionType.ABC
			},
			{
				OpCode.OpLoadK,
				InstructionType.ABx
			},
			{
				OpCode.OpLoadBool,
				InstructionType.ABC
			},
			{
				OpCode.OpLoadNil,
				InstructionType.ABC
			},
			{
				OpCode.OpGetUpValue,
				InstructionType.ABC
			},
			{
				OpCode.OpGetGlobal,
				InstructionType.ABx
			},
			{
				OpCode.OpGetTable,
				InstructionType.ABC
			},
			{
				OpCode.OpSetGlobal,
				InstructionType.ABx
			},
			{
				OpCode.OpSetUpValue,
				InstructionType.ABC
			},
			{
				OpCode.OpSetTable,
				InstructionType.ABC
			},
			{
				OpCode.OpNewTable,
				InstructionType.ABC
			},
			{
				OpCode.OpSelf,
				InstructionType.ABC
			},
			{
				OpCode.OpAdd,
				InstructionType.ABC
			},
			{
				OpCode.OpSub,
				InstructionType.ABC
			},
			{
				OpCode.OpMul,
				InstructionType.ABC
			},
			{
				OpCode.OpDiv,
				InstructionType.ABC
			},
			{
				OpCode.OpMod,
				InstructionType.ABC
			},
			{
				OpCode.OpPow,
				InstructionType.ABC
			},
			{
				OpCode.OpUnm,
				InstructionType.ABC
			},
			{
				OpCode.OpNot,
				InstructionType.ABC
			},
			{
				OpCode.OpLen,
				InstructionType.ABC
			},
			{
				OpCode.OpConcat,
				InstructionType.ABC
			},
			{
				OpCode.OpJump,
				InstructionType.AsBx
			},
			{
				OpCode.OpEq,
				InstructionType.ABC
			},
			{
				OpCode.OpLt,
				InstructionType.ABC
			},
			{
				OpCode.OpLe,
				InstructionType.ABC
			},
			{
				OpCode.OpTest,
				InstructionType.ABC
			},
			{
				OpCode.OpTestSet,
				InstructionType.ABC
			},
			{
				OpCode.OpCall,
				InstructionType.ABC
			},
			{
				OpCode.OpTailCall,
				InstructionType.ABC
			},
			{
				OpCode.OpReturn,
				InstructionType.ABC
			},
			{
				OpCode.OpForLoop,
				InstructionType.AsBx
			},
			{
				OpCode.OpForPrep,
				InstructionType.AsBx
			},
			{
				OpCode.OpTForLoop,
				InstructionType.ABC
			},
			{
				OpCode.OpSetList,
				InstructionType.ABC
			},
			{
				OpCode.OpClose,
				InstructionType.ABC
			},
			{
				OpCode.OpClosure,
				InstructionType.ABx
			},
			{
				OpCode.OpVarArg,
				InstructionType.ABC
			}
		};

		public Deserializer(byte[] Input)
		{
			MemoryStream = new MemoryStream(Input);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte[] Read(int Size, bool FactorEndianness = true)
		{
			byte[] Bytes = new byte[Size];
			MemoryStream.Read(Bytes, 0, Size);
			if (FactorEndianness && Endian == BitConverter.IsLittleEndian)
			{
				Bytes = Bytes.Reverse().ToArray();
			}
			return Bytes;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public long ReadSizeT()
		{
			return (SizeSizeT == 4) ? ReadInt32() : ReadInt64();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public long ReadInt64()
		{
			return BitConverter.ToInt64(Read(8), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int ReadInt32(bool FactorEndianness = true)
		{
			return BitConverter.ToInt32(Read(4, FactorEndianness), 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte ReadByte()
		{
			return Read(1)[0];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ReadString()
		{
			int Count = (int)ReadSizeT();
			if (Count == 0)
			{
				return "";
			}
			byte[] Value = Read(Count, FactorEndianness: false);
			return LuaEncoding.GetString(Value, 0, Count - 1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double ReadDouble()
		{
			return BitConverter.ToDouble(Read(SizeNumber), 0);
		}

		public Instruction DecodeInstruction(Chunk Chunk)
		{
			int Code = ReadInt32();
			Instruction Instruction = new Instruction(Chunk, (OpCode)(Code & 0x3F));
			Instruction.Data = Code;
			if (ExpectingSetListData)
			{
				ExpectingSetListData = false;
				Instruction.InstructionType = InstructionType.Data;
				return Instruction;
			}
			Instruction.A = (Code >> 6) & 0xFF;
			switch (Instruction.InstructionType)
			{
			case InstructionType.ABC:
				Instruction.B = (Code >> 23) & 0x1FF;
				Instruction.C = (Code >> 14) & 0x1FF;
				break;
			case InstructionType.ABx:
				Instruction.B = (Code >> 14) & 0x3FFFF;
				Instruction.C = 0;
				break;
			case InstructionType.AsBx:
				Instruction.B = ((Code >> 14) & 0x3FFFF) - 131071;
				Instruction.C = 0;
				break;
			}
			if (Instruction.OpCode == OpCode.OpSetList && Instruction.C == 0)
			{
				ExpectingSetListData = true;
			}
			return Instruction;
		}

		public void DecodeInstructions(Chunk Chunk)
		{
			List<Instruction> Instructions = new List<Instruction>();
			Dictionary<Instruction, int> InstructionMap = new Dictionary<Instruction, int>();
			int InstructionCount = ReadInt32();
			for (int I = 0; I < InstructionCount; I++)
			{
				Instruction Instruction = DecodeInstruction(Chunk);
				Instructions.Add(Instruction);
				InstructionMap.Add(Instruction, I);
			}
			Chunk.Instructions = Instructions;
			Chunk.InstructionMap = InstructionMap;
		}

		public Constant DecodeConstant()
		{
			Constant Constant = new Constant();
			switch (ReadByte())
			{
			case 0:
				Constant.Type = ConstantType.Nil;
				Constant.Data = null;
				break;
			case 1:
				Constant.Type = ConstantType.Boolean;
				Constant.Data = ReadByte() != 0;
				break;
			case 3:
				Constant.Type = ConstantType.Number;
				Constant.Data = ReadDouble();
				break;
			case 4:
				Constant.Type = ConstantType.String;
				Constant.Data = ReadString();
				break;
			}
			return Constant;
		}

		public void DecodeConstants(Chunk Chunk)
		{
			List<Constant> Constants = new List<Constant>();
			Dictionary<Constant, int> ConstantMap = new Dictionary<Constant, int>();
			int ConstantCount = ReadInt32();
			for (int I = 0; I < ConstantCount; I++)
			{
				Constant Constant = DecodeConstant();
				Constants.Add(Constant);
				ConstantMap.Add(Constant, I);
			}
			Chunk.Constants = Constants;
			Chunk.ConstantMap = ConstantMap;
		}

		public Chunk DecodeChunk()
		{
			Chunk Chunk = new Chunk
			{
				Name = ReadString(),
				Line = ReadInt32(),
				LastLine = ReadInt32(),
				UpValueCount = ReadByte(),
				ParameterCount = ReadByte(),
				VarArgFlag = ReadByte(),
				StackSize = ReadByte(),
				UpValues = new List<string>()
			};
			DecodeInstructions(Chunk);
			DecodeConstants(Chunk);
			DecodeChunks(Chunk);
			int Count = ReadInt32();
			for (int I3 = 0; I3 < Count; I3++)
			{
				Chunk.Instructions[I3].Line = ReadInt32();
			}
			Count = ReadInt32();
			for (int I2 = 0; I2 < Count; I2++)
			{
				ReadString();
				ReadInt32();
				ReadInt32();
			}
			Count = ReadInt32();
			for (int I = 0; I < Count; I++)
			{
				Chunk.UpValues.Add(ReadString());
			}
			foreach (Instruction Instruction in Chunk.Instructions)
			{
				Instruction.SetupReferences();
			}
			return Chunk;
		}

		public void DecodeChunks(Chunk Chunk)
		{
			List<Chunk> Chunks = new List<Chunk>();
			Dictionary<Chunk, int> ChunkMap = new Dictionary<Chunk, int>();
			int ChunkCount = ReadInt32();
			for (int I = 0; I < ChunkCount; I++)
			{
				Chunk SubChunk = DecodeChunk();
				Chunks.Add(SubChunk);
				ChunkMap.Add(SubChunk, I);
			}
			Chunk.Chunks = Chunks;
			Chunk.ChunkMap = ChunkMap;
		}

		public Chunk DecodeFile()
		{
			int Header = ReadInt32();
			if (Header != 457995617 && Header != 1635077147)
			{
				throw new Exception("Obfuscation Error: Invalid LuaC File.");
			}
			if (ReadByte() != 81)
			{
				throw new Exception("Obfuscation Error: Only Lua 5.1 is Supported.");
			}
			ReadByte();
			Endian = ReadByte() == 0;
			ReadByte();
			SizeSizeT = ReadByte();
			ReadByte();
			SizeNumber = ReadByte();
			ReadByte();
			Chunk HeadChunk = DecodeChunk();
			RemoveJumps(HeadChunk);
			EditClosure(HeadChunk);
			UpdateChunk(HeadChunk);
			FixJumps(HeadChunk);
			UpdateChunk(HeadChunk);
			return HeadChunk;
			static void EditClosure(Chunk Chunk)
			{
				for (int InstructionPoint2 = 0; InstructionPoint2 < Chunk.Instructions.Count; InstructionPoint2++)
				{
					Instruction Instruction3 = Chunk.Instructions[InstructionPoint2];
					if (Instruction3.OpCode == OpCode.OpClosure && Instruction3.References[0].UpValueCount > 0)
					{
						Instruction3.CustomData = new List<int[]>();
						for (int I2 = 1; I2 <= Instruction3.References[0].UpValueCount; I2++)
						{
							Instruction UpValue = Chunk.Instructions[InstructionPoint2 + I2];
							Instruction3.CustomData.Add(new int[2]
							{
								(UpValue.OpCode != 0) ? 1 : 0,
								UpValue.B
							});
						}
						for (int I = 1; I <= Instruction3.References[0].UpValueCount; I++)
						{
							Chunk.Instructions.RemoveAt(InstructionPoint2 + 1);
						}
					}
				}
				foreach (Chunk SubChunk3 in Chunk.Chunks)
				{
					EditClosure(SubChunk3);
				}
				Chunk.UpdateMappings();
			}
			static void FixJumps(Chunk Chunk)
			{
				for (int InstructionPoint = 0; InstructionPoint < Chunk.Instructions.Count; InstructionPoint++)
				{
					Instruction Instruction = Chunk.Instructions[InstructionPoint];
					switch (Instruction.OpCode)
					{
					case OpCode.OpEq:
					case OpCode.OpLt:
					case OpCode.OpLe:
					case OpCode.OpTest:
					case OpCode.OpTestSet:
					case OpCode.OpForLoop:
					case OpCode.OpForPrep:
					case OpCode.OpTForLoop:
						Instruction.IsJump = true;
						Instruction.JumpTo = Chunk.Instructions[InstructionPoint + 1];
						Instruction.JumpTo.BackReferences.Add(Instruction);
						break;
					case OpCode.OpLoadBool:
						if (Instruction.C == 1)
						{
							Instruction.IsJump = true;
							Instruction.JumpTo = Chunk.Instructions[InstructionPoint + 2];
							Instruction.JumpTo.BackReferences.Add(Instruction);
						}
						break;
					}
				}
				foreach (Chunk SubChunk in Chunk.Chunks)
				{
					FixJumps(SubChunk);
				}
				Chunk.UpdateMappings();
			}
			static void RemoveJumps(Chunk Chunk)
			{
				for (int InstructionPoint3 = 0; InstructionPoint3 < Chunk.Instructions.Count; InstructionPoint3++)
				{
					Instruction Instruction4 = Chunk.Instructions[InstructionPoint3];
					OpCode opCode = Instruction4.OpCode;
					OpCode opCode2 = opCode;
					if ((uint)(opCode2 - 23) <= 4u || opCode2 == OpCode.OpTForLoop)
					{
						Utility.Utility.VoidInstruction(Chunk.Instructions[InstructionPoint3 + 1]);
						Chunk.Instructions.RemoveAt(InstructionPoint3 + 1);
					}
				}
				foreach (Chunk SubChunk4 in Chunk.Chunks)
				{
					RemoveJumps(SubChunk4);
				}
				Chunk.UpdateMappings();
			}
			static void UpdateChunk(Chunk Chunk)
			{
				foreach (Chunk SubChunk2 in Chunk.Chunks)
				{
					UpdateChunk(SubChunk2);
				}
				Chunk.Constants.Shuffle();
				Chunk.UpdateMappings();
				foreach (Instruction Instruction2 in Chunk.Instructions)
				{
					Instruction2.UpdateRegisters();
				}
			}
		}
	}
}
