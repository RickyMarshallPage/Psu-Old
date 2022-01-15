using System;
using System.Collections.Generic;
using System.Linq;
using Obfuscator.Bytecode.IR;
using Obfuscator.Obfuscation.OpCodes;
using Obfuscator.Utility;

namespace Obfuscator.Obfuscation.Security
{
	public class BytecodeSecurity
	{
		private static List<string> Macros = new List<string> { "PSU_MAX_SECURITY_START", "PSU_MAX_SECURITY_END" };

		private Random Random = new Random();

		private Chunk HeadChunk;

		private ObfuscationSettings ObfuscationSettings;

		private ObfuscationContext ObfuscationContext;

		private List<VOpCode> Virtuals;

		public BytecodeSecurity(Chunk HeadChunk, ObfuscationSettings ObfuscationSettings, ObfuscationContext ObfuscationContext, List<VOpCode> Virtuals)
		{
			this.Virtuals = Virtuals;
			this.HeadChunk = HeadChunk;
			this.ObfuscationSettings = ObfuscationSettings;
			this.ObfuscationContext = ObfuscationContext;
		}

		public void DoChunk(Chunk Chunk)
		{
			foreach (Chunk SubChunk in Chunk.Chunks)
			{
				DoChunk(SubChunk);
			}
			bool ByteCodeSecurity = false;
			Instruction Begin = null;
			for (int InstructionPoint = 0; InstructionPoint < Chunk.Instructions.Count; InstructionPoint++)
			{
				Instruction Instruction = Chunk.Instructions[InstructionPoint];
				if (!((Instruction.OpCode == OpCode.OpGetGlobal && Macros.Contains(Instruction.References[0].Data)) ? true : false))
				{
					continue;
				}
				int A = Instruction.A;
				if (Chunk.Instructions[InstructionPoint + 1].OpCode != OpCode.OpCall || Chunk.Instructions[InstructionPoint + 1].A != Instruction.A)
				{
					continue;
				}
				string Global = (string)Instruction.References[0].Data;
				Utility.Utility.VoidInstruction(Chunk.Instructions[InstructionPoint + 1]);
				Utility.Utility.VoidInstruction(Instruction);
				switch (Global)
				{
				case "PSU_MAX_SECURITY_START":
					if (!ByteCodeSecurity)
					{
						Begin = Chunk.Instructions[InstructionPoint + 2];
						ByteCodeSecurity = true;
					}
					break;
				case "PSU_MAX_SECURITY_END":
					if (ByteCodeSecurity)
					{
						Chunk.UpdateMappings();
						if (Begin == Instruction)
						{
							Begin = null;
							ByteCodeSecurity = false;
							break;
						}
						Instruction = Chunk.Instructions[InstructionPoint];
						int Start = Chunk.InstructionMap[Begin];
						int End = Chunk.InstructionMap[Instruction] + 1;
						List<Instruction> InstructionList = Chunk.Instructions.Skip(Start).Take(End - Start).ToList();
						new RegisterMutation(ObfuscationContext, Chunk, InstructionList, Virtuals, Start, End).DoInstructions();
						Start = Chunk.InstructionMap[Begin];
						End = Chunk.InstructionMap[Instruction] + 1;
						InstructionList = Chunk.Instructions.Skip(Start).Take(End - Start).ToList();
						new TestSpam(ObfuscationContext, Chunk, InstructionList, Virtuals, Start, End).DoInstructions();
						Start = Chunk.InstructionMap[Begin];
						End = Chunk.InstructionMap[Instruction] + 1;
						InstructionList = Chunk.Instructions.Skip(Start).Take(End - Start).ToList();
						new TestRemove(ObfuscationContext, Chunk, InstructionList, Virtuals, Start, End).DoInstructions();
						Begin = null;
						ByteCodeSecurity = false;
					}
					break;
				}
			}
			Chunk.Instructions.Insert(0, new Instruction(Chunk, OpCode.None));
			Chunk.UpdateMappings();
		}

		public void DoChunks()
		{
			DoChunk(HeadChunk);
		}
	}
}
