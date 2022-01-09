using System;
using System.Collections.Generic;
using System.Linq;
using Obfuscator.Bytecode.IR;
using Obfuscator.Extensions;
using Obfuscator.Obfuscation.OpCodes;

namespace Obfuscator.Obfuscation.Security
{
	public class TestSpam
	{
		private Random Random = new Random();

		private Chunk Chunk;

		private List<Instruction> Instructions;

		private List<VOpCode> Virtuals;

		private ObfuscationContext ObfuscationContext;

		private int Start;

		private int End;

		public TestSpam(ObfuscationContext ObfuscationContext, Chunk Chunk, List<Instruction> Instructions, List<VOpCode> Virtuals, int Start, int End)
		{
			this.ObfuscationContext = ObfuscationContext;
			this.Chunk = Chunk;
			this.Instructions = Instructions;
			this.Virtuals = Virtuals;
			this.Start = Start;
			this.End = End;
		}

		public void DoInstructions()
		{
			List<BasicBlock> BasicBlocks = new BasicBlock().GenerateBasicBlocksFromInstructions(Chunk, Instructions);
			List<Instruction> Additions = new List<Instruction>();
			foreach (BasicBlock Block3 in BasicBlocks)
			{
				Instruction Last = Block3.Instructions.Last();
				OpCode opCode = Last.OpCode;
				OpCode opCode2 = opCode;
				if ((uint)(opCode2 - 23) <= 3u)
				{
					Instruction PreviousTrue = Last;
					Instruction PreviousFalse = Last;
					Instruction T = Last.JumpTo;
					Instruction F = Last.References[2];
					for (int I2 = 0; I2 < Random.Next(5, 10); I2++)
					{
						Instruction IsTrue = new Instruction(PreviousTrue);
						Instruction IsFalse = new Instruction(PreviousFalse);
						PreviousTrue.JumpTo = IsTrue;
						PreviousTrue.IsJump = true;
						Additions.Add(IsTrue);
						PreviousTrue = IsTrue;
						IsTrue.IsJump = true;
						IsTrue.JumpTo = T;
						IsTrue.References[2] = IsTrue;
						PreviousFalse.References[2] = IsFalse;
						Additions.Add(IsFalse);
						PreviousFalse = IsFalse;
						IsFalse.IsJump = true;
						IsFalse.JumpTo = IsFalse;
						IsFalse.References[2] = F;
					}
					Instruction JumpTrue = (PreviousTrue.JumpTo = new Instruction(Chunk, OpCode.OpJump, T));
					PreviousTrue.IsJump = true;
					Additions.Add(JumpTrue);
					Instruction JumpFalse = new Instruction(Chunk, OpCode.OpJump, F);
					PreviousFalse.References[2] = JumpFalse;
					Additions.Add(JumpFalse);
					for (int I = 0; I < Random.Next(5, 10); I++)
					{
						Instruction NextJumpTrue = new Instruction(Chunk, OpCode.OpJump, T);
						Instruction NextJumpFalse = new Instruction(Chunk, OpCode.OpJump, F);
						JumpTrue.References[0] = NextJumpTrue;
						JumpFalse.References[0] = NextJumpFalse;
						JumpTrue = NextJumpTrue;
						JumpFalse = NextJumpFalse;
						Additions.Add(JumpTrue);
						Additions.Add(JumpFalse);
					}
				}
			}
			List<BasicBlock> AllowedBlocks = new List<BasicBlock>();
			foreach (BasicBlock Block2 in BasicBlocks)
			{
				OpCode opCode3 = Block2.Instructions.Last().OpCode;
				OpCode opCode4 = opCode3;
				if ((uint)(opCode4 - 22) <= 5u || (uint)(opCode4 - 30) <= 3u)
				{
					AllowedBlocks.Add(Block2);
				}
			}
			Additions.Shuffle();
			foreach (Instruction Instruction2 in Additions)
			{
				AllowedBlocks.Random().Instructions.Add(Instruction2);
			}
			List<Instruction> Before = Chunk.Instructions.Take(Start).ToList();
			List<Instruction> After = Chunk.Instructions.Skip(End).ToList();
			Chunk.Instructions.Clear();
			Chunk.Instructions.AddRange(Before);
			foreach (BasicBlock Block in BasicBlocks)
			{
				foreach (Instruction Instruction in Block.Instructions)
				{
					Chunk.Instructions.Add(Instruction);
				}
			}
			Chunk.Instructions.AddRange(After);
			Chunk.UpdateMappings();
			End = Chunk.InstructionMap[BasicBlocks.Last().Instructions.Last()];
			List<Instruction> IList = Chunk.Instructions.Skip(Start).Take(End - Start).ToList();
		}
	}
}
