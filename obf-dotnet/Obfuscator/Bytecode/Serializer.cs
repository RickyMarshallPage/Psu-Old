using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Obfuscator.Bytecode.IR;
using Obfuscator.Extensions;
using Obfuscator.Obfuscation;
using Obfuscator.Obfuscation.OpCodes;

namespace Obfuscator.Bytecode
{
	public class Serializer
	{
		private ObfuscationContext ObfuscationContext;

		private ObfuscationSettings ObfuscationSettings;

		private Random Random = new Random();

		private Encoding LuaEncoding = Encoding.GetEncoding(28591);

		public List<string> Types = new List<string>();

		public Serializer(ObfuscationContext ObfuscationContext, ObfuscationSettings ObfuscationSettings)
		{
			this.ObfuscationContext = ObfuscationContext;
			this.ObfuscationSettings = ObfuscationSettings;
		}

		public void SerializeLChunk(Chunk Chunk, List<byte> Bytes)
		{
			Chunk.Constants.Shuffle();
			Chunk.UpdateMappings();
			foreach (Instruction Instruction3 in Chunk.Instructions)
			{
				Instruction3.UpdateRegisters();
				if (Instruction3.CustomInstructionData.Serialize && Instruction3.InstructionType != InstructionType.Data)
				{
					CustomInstructionData CustomInstructionData = Instruction3.CustomInstructionData;
					VOpCode VirtualOpcode = CustomInstructionData.OpCode;
					if (!CustomInstructionData.Mutated)
					{
						VirtualOpcode?.Mutate(Instruction3);
					}
					CustomInstructionData.Mutated = true;
				}
			}
			ChunkStep[] chunkSteps = ObfuscationContext.ChunkSteps;
			for (int i = 0; i < chunkSteps.Length; i++)
			{
				switch (chunkSteps[i])
				{
				case ChunkStep.StackSize:
					WriteInt16(Chunk.StackSize);
					break;
				case ChunkStep.ParameterCount:
					WriteByte(Chunk.ParameterCount);
					break;
				case ChunkStep.Instructions:
					WriteInt32(Chunk.Constants.Count);
					foreach (Constant Constant in Chunk.Constants)
					{
						WriteByte((byte)ObfuscationContext.ConstantMapping[(int)Constant.Type]);
						switch (Constant.Type)
						{
						case ConstantType.Int32:
							WriteInt32((short)(Constant.Data + 65536));
							break;
						case ConstantType.Int16:
							WriteInt16((short)(Constant.Data + 256));
							break;
						case ConstantType.Boolean:
							WriteBool(Constant.Data);
							break;
						case ConstantType.Number:
							WriteNumber(Constant.Data);
							break;
						case ConstantType.String:
							WriteString(Constant.Data);
							break;
						}
					}
					WriteInt32(Chunk.Instructions.Count);
					foreach (Instruction Instruction2 in Chunk.Instructions)
					{
						SerializeInstruction(Instruction2);
					}
					break;
				case ChunkStep.Chunks:
					WriteInt32(Chunk.Chunks.Count);
					foreach (Chunk SubChunk in Chunk.Chunks)
					{
						SerializeLChunk(SubChunk, Bytes);
					}
					break;
				}
			}
			void SerializeInstruction(Instruction Instruction)
			{
				if (!Instruction.CustomInstructionData.Serialize || Instruction.InstructionType == InstructionType.Data)
				{
					WriteByte(0);
				}
				else
				{
					CustomInstructionData CustomInstructionData2 = Instruction.CustomInstructionData;
					int OpCode = (int)Instruction.OpCode;
					VOpCode VirtualOpcode2 = CustomInstructionData2.OpCode;
					if (!CustomInstructionData2.Mutated)
					{
						VirtualOpcode2?.Mutate(Instruction);
					}
					CustomInstructionData2.Mutated = true;
					if (Instruction.RegisterHandler != null)
					{
						Instruction = Instruction.RegisterHandler(Instruction);
					}
					int A = Instruction.A;
					int B = Instruction.B;
					int C = Instruction.C;
					bool KA = Instruction.IsConstantA;
					bool KB = Instruction.IsConstantB;
					bool KC = Instruction.IsConstantC;
					int InstructionData = (int)(Instruction.InstructionType + 1);
					if (!ObfuscationSettings.ConstantEncryption)
					{
						InstructionData |= (Instruction.IsConstantA ? 8 : 0);
						InstructionData |= (Instruction.IsConstantB ? 16 : 0);
						InstructionData |= (Instruction.IsConstantC ? 32 : 0);
					}
					if (Instruction.RequiresCustomData)
					{
						InstructionData |= 0x40;
					}
					if (Instruction.IsJump)
					{
						InstructionData |= 0x80;
					}
					OpCode = Instruction.WrittenVIndex ?? VirtualOpcode2.VIndex;
					Instruction.A = Instruction.WrittenA ?? Instruction.A;
					Instruction.B = Instruction.WrittenB ?? Instruction.B;
					Instruction.C = Instruction.WrittenC ?? Instruction.C;
					A = Instruction.A;
					B = Instruction.B;
					C = Instruction.C;
					WriteByte((byte)InstructionData);
					InstructionStep[] instructionSteps = ObfuscationContext.InstructionSteps;
					for (int j = 0; j < instructionSteps.Length; j++)
					{
						switch (instructionSteps[j])
						{
						case InstructionStep.Enum:
							WriteByte((byte)OpCode);
							break;
						case InstructionStep.A:
							switch (Instruction.InstructionType)
							{
							case InstructionType.ABC:
								WriteInt16((short)Instruction.A);
								break;
							case InstructionType.ABx:
								WriteInt16((short)Instruction.A);
								break;
							case InstructionType.AsBx:
								WriteInt16((short)Instruction.A);
								break;
							case InstructionType.AsBxC:
								WriteInt16((short)Instruction.A);
								break;
							case InstructionType.Closure:
								WriteInt16((short)Instruction.A);
								break;
							}
							break;
						case InstructionStep.B:
							switch (Instruction.InstructionType)
							{
							case InstructionType.ABC:
								WriteInt16((short)Instruction.B);
								break;
							case InstructionType.ABx:
								WriteInt32(Instruction.B);
								break;
							case InstructionType.AsBx:
								WriteInt32(Instruction.B);
								break;
							case InstructionType.AsBxC:
								WriteInt32(Instruction.B);
								break;
							case InstructionType.Closure:
								WriteInt32(Instruction.B);
								break;
							}
							break;
						case InstructionStep.C:
							switch (Instruction.InstructionType)
							{
							case InstructionType.ABC:
								WriteInt16((short)Instruction.C);
								break;
							case InstructionType.AsBxC:
								WriteInt16((short)Instruction.C);
								break;
							case InstructionType.Closure:
								WriteInt16((short)Instruction.C);
								break;
							}
							break;
						}
					}
					if (Instruction.InstructionType == InstructionType.Closure)
					{
						foreach (object? customDatum in Instruction.CustomData)
						{
							int[] UpValue = (int[])(dynamic)customDatum;
							WriteByte((byte)UpValue[0]);
							WriteInt16((short)UpValue[1]);
						}
					}
					if (Instruction.IsJump)
					{
						WriteInt32(Chunk.InstructionMap[Instruction.JumpTo]);
					}
					if (Instruction.RequiresCustomData)
					{
						WriteByte((byte)((List<int>)Instruction.CustomData).Count);
						foreach (int Value in (List<int>)Instruction.CustomData)
						{
							WriteInt32(Value);
						}
					}
				}
			}
			void Write(byte[] ToWrite, bool CheckEndian)
			{
				if (!BitConverter.IsLittleEndian && CheckEndian)
				{
					ToWrite = ToWrite.Reverse().ToArray();
				}
				Bytes.AddRange(ToWrite.Select(delegate(byte Byte)
				{
					byte result2 = (byte)(Byte ^ ObfuscationContext.PrimaryXORKey);
					ObfuscationContext.PrimaryXORKey = (int)Byte % 256;
					return result2;
				}));
			}
			void WriteBool(bool Bool)
			{
				Write(BitConverter.GetBytes(Bool), CheckEndian: true);
			}
			void WriteByte(byte Byte)
			{
				Bytes.Add((byte)(Byte ^ ObfuscationContext.PrimaryXORKey));
				ObfuscationContext.PrimaryXORKey = (int)Byte % 256;
			}
			void WriteInt16(short Int16)
			{
				Write(BitConverter.GetBytes(Int16), CheckEndian: true);
			}
			void WriteInt32(int Int32)
			{
				Write(BitConverter.GetBytes(Int32), CheckEndian: true);
			}
			void WriteNumber(double Number)
			{
				Write(BitConverter.GetBytes(Number), CheckEndian: true);
			}
			void WriteString(string String)
			{
				byte[] sBytes = LuaEncoding.GetBytes(String);
				WriteInt32(sBytes.Length);
				Bytes.AddRange(sBytes.Select(delegate(byte Byte)
				{
					byte result = (byte)(Byte ^ ObfuscationContext.PrimaryXORKey);
					ObfuscationContext.PrimaryXORKey = (int)Byte % 256;
					return result;
				}));
			}
		}

		public List<byte> Serialize(Chunk HeadChunk)
		{
			List<byte> Bytes = new List<byte>();
			SerializeLChunk(HeadChunk, Bytes);
			return Bytes;
		}
	}
}
