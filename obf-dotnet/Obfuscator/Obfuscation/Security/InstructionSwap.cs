using System;
using System.Collections.Generic;
using System.Linq;
using Obfuscator.Bytecode.IR;
using Obfuscator.Extensions;

namespace Obfuscator.Obfuscation.Security
{
	public class InstructionSwap
	{
		private Random Random = new Random();

		public ObfuscationContext ObfuscationContext;

		public Chunk HeadChunk;

		public InstructionSwap(ObfuscationContext ObfuscationContext, Chunk HeadChunk)
		{
			this.ObfuscationContext = ObfuscationContext;
			this.HeadChunk = HeadChunk;
		}

		public void DoChunk(Chunk Chunk)
		{
			Chunk.UpdateMappings();
			foreach (Chunk SubChunk in Chunk.Chunks)
			{
				DoChunk(SubChunk);
			}
			List<Instruction> Instructions = Chunk.Instructions.ToList();
			int InstructionPoint = 0;
			List<Instruction> IgnoredInstructions = new List<Instruction>();
			while (InstructionPoint < Instructions.Count)
			{
				Instruction Instruction = Instructions[InstructionPoint];
				InstructionPoint++;
				OpCode opCode = Instruction.OpCode;
				OpCode opCode2 = opCode;
				if ((uint)(opCode2 - 22) <= 5u || (uint)(opCode2 - 31) <= 2u)
				{
					IgnoredInstructions.Add(Instruction);
				}
				else if (Instruction.IsJump || Instruction.BackReferences.Count > 0 || InstructionPoint == 1 || InstructionPoint == Chunk.Instructions.Count)
				{
					IgnoredInstructions.Add(Instruction);
				}
			}
			List<Instruction> Swapped = new List<Instruction>();
			foreach (Instruction Instruction3 in Instructions)
			{
				if (!IgnoredInstructions.Contains(Instruction3))
				{
					Instruction PreviousInstruction = Chunk.Instructions[Chunk.InstructionMap[Instruction3] - 1];
					Instruction NextInstruction = Chunk.Instructions[Chunk.InstructionMap[Instruction3] + 1];
					if (!IgnoredInstructions.Contains(PreviousInstruction) && !IgnoredInstructions.Contains(NextInstruction))
					{
						PreviousInstruction.IsJump = true;
						PreviousInstruction.JumpTo = Instruction3;
						Instruction3.BackReferences.Add(PreviousInstruction);
						Instruction3.IsJump = true;
						Instruction3.JumpTo = NextInstruction;
						NextInstruction.BackReferences.Add(Instruction3);
						Swapped.Add(Instruction3);
						IgnoredInstructions.Add(Instruction3);
						IgnoredInstructions.Add(PreviousInstruction);
						IgnoredInstructions.Add(NextInstruction);
					}
				}
			}
			Swapped.Shuffle();
			foreach (Instruction Instruction2 in Swapped)
			{
				Chunk.Instructions.Remove(Instruction2);
			}
			Chunk.Instructions.AddRange(Swapped);
			Chunk.UpdateMappings();
		}

		public void DoChunks()
		{
			DoChunk(HeadChunk);
		}
	}
}
