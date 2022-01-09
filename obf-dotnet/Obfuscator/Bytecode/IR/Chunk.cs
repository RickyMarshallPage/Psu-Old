using System;
using System.Collections.Generic;

namespace Obfuscator.Bytecode.IR
{
	[Serializable]
	public class Chunk
	{
		public string Name = "";

		public byte ParameterCount;

		public byte VarArgFlag;

		public byte StackSize;

		public int Line;

		public int LastLine;

		public int CurrentOffset;

		public int CurrentParameterOffset;

		public byte UpValueCount;

		public List<string> UpValues;

		public List<Instruction> Instructions;

		public Dictionary<Instruction, int> InstructionMap;

		public List<Constant> Constants;

		public Dictionary<Constant, int> ConstantMap;

		public List<Chunk> Chunks;

		public Dictionary<Chunk, int> ChunkMap;

		public int XORKey = new Random().Next(0, 256);

		public void UpdateMappings()
		{
			InstructionMap.Clear();
			ConstantMap.Clear();
			ChunkMap.Clear();
			for (int I3 = 0; I3 < Instructions.Count; I3++)
			{
				InstructionMap.Add(Instructions[I3], I3);
			}
			for (int I2 = 0; I2 < Constants.Count; I2++)
			{
				ConstantMap.Add(Constants[I2], I2);
			}
			for (int I = 0; I < Chunks.Count; I++)
			{
				ChunkMap.Add(Chunks[I], I);
			}
		}
	}
}
