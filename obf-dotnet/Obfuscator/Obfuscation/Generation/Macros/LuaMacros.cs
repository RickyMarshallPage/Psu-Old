using System.Collections.Generic;
using Obfuscator.Bytecode.IR;
using Obfuscator.Obfuscation.OpCodes;
using Obfuscator.Utility;

namespace Obfuscator.Obfuscation.Generation.Macros
{
	public class LuaMacros
	{
		private static List<string> Macros = new List<string> { "PSU_GETSTACK" };

		private Chunk HeadChunk;

		private List<VOpCode> Virtuals;

		public LuaMacros(Chunk HeadChunk, List<VOpCode> Virtuals)
		{
			this.HeadChunk = HeadChunk;
			this.Virtuals = Virtuals;
		}

		private void DoChunk(Chunk Chunk)
		{
			foreach (Chunk SubChunk in Chunk.Chunks)
			{
				DoChunk(SubChunk);
			}
			for (int InstructionPoint = 0; InstructionPoint < Chunk.Instructions.Count; InstructionPoint++)
			{
				Instruction Instruction = Chunk.Instructions[InstructionPoint];
				if (!((Instruction.OpCode == OpCode.OpGetGlobal && Macros.Contains(Instruction.References[0].Data)) ? true : false))
				{
					continue;
				}
				int A = Instruction.A;
				if (Chunk.Instructions[InstructionPoint + 1].OpCode == OpCode.OpCall && Chunk.Instructions[InstructionPoint + 1].A == Instruction.A)
				{
					object obj = Instruction.References[0].Data;
					object obj2 = obj;
					if (obj2 is string text && text == "PSU_GETSTACK")
					{
						global::Obfuscator.Utility.Utility.VoidInstruction(Chunk.Instructions[InstructionPoint + 1]);
						Chunk.Instructions.RemoveAt(InstructionPoint + 1);
						global::Obfuscator.Utility.Utility.VoidInstruction(Instruction);
						Instruction.OpCode = OpCode.OpGetStack;
						Instruction.A = A;
					}
				}
			}
			Chunk.UpdateMappings();
		}

		public void DoChunks()
		{
			DoChunk(HeadChunk);
		}
	}
}
