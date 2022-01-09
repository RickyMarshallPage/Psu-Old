using System.Collections.Generic;
using Obfuscator.Bytecode.IR;
using Obfuscator.Obfuscation.OpCodes;

namespace Obfuscator.Obfuscation.Generation
{
	public class SuperOperators
	{
		private void ProcessInstructions(Chunk Chunk, Dictionary<Instruction, bool> IgnoredInstructions, List<VOpCode> Virtuals, int InstructionPoint = 0)
		{
			if (Virtuals.Count > 128)
			{
				return;
			}
			OpSuperOperator Virtual = new OpSuperOperator();
			Virtuals.Add(Virtual);
			while (InstructionPoint < Chunk.Instructions.Count - 1)
			{
				Instruction Instruction = Chunk.Instructions[InstructionPoint];
				if (IgnoredInstructions.ContainsKey(Instruction))
				{
					if (Virtual.Instructions.Count < 5)
					{
						Virtuals.Remove(Virtual);
					}
					while (InstructionPoint + 1 < Chunk.Instructions.Count)
					{
						InstructionPoint++;
						if (!IgnoredInstructions.ContainsKey(Chunk.Instructions[InstructionPoint]))
						{
							break;
						}
					}
					if (InstructionPoint + 2 < Chunk.Instructions.Count)
					{
						ProcessInstructions(Chunk, IgnoredInstructions, Virtuals, InstructionPoint + 1);
					}
					break;
				}
				Virtual.Instructions.Add(Instruction);
				Virtual.Virtuals.Add(Instruction.CustomInstructionData.OpCode);
				InstructionPoint++;
				if (InstructionPoint >= Chunk.Instructions.Count - 1)
				{
					break;
				}
				if (Virtual.Instructions.Count >= 50)
				{
					ProcessInstructions(Chunk, IgnoredInstructions, Virtuals, InstructionPoint);
					break;
				}
			}
		}

		private void OptimizeInstructions(Chunk Chunk, List<VOpCode> Virtuals)
		{
			int InstructionPoint = 0;
			Dictionary<Instruction, bool> IgnoredInstructions = new Dictionary<Instruction, bool>();
			for (; InstructionPoint < Chunk.Instructions.Count - 1; InstructionPoint++)
			{
				Instruction Instruction = Chunk.Instructions[InstructionPoint];
				switch (Instruction.OpCode)
				{
				case OpCode.OpLoadBool:
					if (Instruction.C == 0)
					{
						break;
					}
					goto case OpCode.OpEq;
				case OpCode.OpEq:
				case OpCode.OpLt:
				case OpCode.OpLe:
				case OpCode.OpTest:
				case OpCode.OpTestSet:
				case OpCode.OpTForLoop:
				case OpCode.OpSetList:
				case OpCode.OpClosure:
					IgnoredInstructions[Instruction] = true;
					break;
				case OpCode.OpJump:
				case OpCode.OpForLoop:
				case OpCode.OpForPrep:
					IgnoredInstructions[Instruction] = true;
					IgnoredInstructions[Instruction.References[0]] = true;
					break;
				case OpCode.OpDynamicJump:
					IgnoredInstructions[Instruction] = true;
					break;
				case OpCode.Custom:
					IgnoredInstructions[Instruction] = true;
					break;
				}
				if (Instruction.BackReferences.Count > 0)
				{
					IgnoredInstructions[Instruction] = true;
				}
				if (Instruction.IgnoreInstruction)
				{
					IgnoredInstructions[Instruction] = true;
				}
			}
			ProcessInstructions(Chunk, IgnoredInstructions, Virtuals);
		}

		public void DoChunk(Chunk Chunk, List<VOpCode> Virtuals)
		{
			foreach (Chunk SubChunk in Chunk.Chunks)
			{
				DoChunk(SubChunk, Virtuals);
			}
			OptimizeInstructions(Chunk, Virtuals);
		}
	}
}
