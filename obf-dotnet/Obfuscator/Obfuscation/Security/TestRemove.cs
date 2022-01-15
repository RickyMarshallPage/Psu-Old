using System;
using System.Collections.Generic;
using System.Linq;
using Obfuscator.Bytecode.IR;
using Obfuscator.Obfuscation.OpCodes;
using Obfuscator.Utility;

namespace Obfuscator.Obfuscation.Security
{
	public class TestRemove
	{
		private Random Random = new Random();

		private Chunk Chunk;

		private List<Instruction> Instructions;

		private List<VOpCode> Virtuals;

		private ObfuscationContext ObfuscationContext;

		private int Start;

		private int End;

		public TestRemove(ObfuscationContext ObfuscationContext, Chunk Chunk, List<Instruction> Instructions, List<VOpCode> Virtuals, int Start, int End)
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
			int TrueIndex;
			Constant True = Utility.Utility.GetOrAddConstant(Chunk, ConstantType.Boolean, true, out TrueIndex);
			int FalseIndex;
			Constant False = Utility.Utility.GetOrAddConstant(Chunk, ConstantType.Boolean, false, out FalseIndex);
			foreach (BasicBlock Block2 in BasicBlocks)
			{
				for (int InstructionPoint = 0; InstructionPoint < Block2.Instructions.Count; InstructionPoint++)
				{
					Instruction Instruction2 = Block2.Instructions[InstructionPoint];
					if (Instruction2.OpCode == OpCode.OpTest)
					{
						if (Instruction2.C == 1)
						{
							Instruction L2 = Instruction2.References[2];
							Instruction R2 = Instruction2.JumpTo;
							L2.BackReferences.Remove(Instruction2);
							R2.BackReferences.Remove(Instruction2);
							Instruction2.OpCode = OpCode.OpNot;
							Instruction2.B = Instruction2.A;
							Instruction2.A = Chunk.StackSize + 1;
							Instruction2.C = 0;
							Instruction2.IsJump = false;
							Instruction2.JumpTo = null;
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 1, new Instruction(Chunk, OpCode.OpNot)
							{
								A = Chunk.StackSize + 1,
								B = Chunk.StackSize + 1
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 2, new Instruction(Chunk, OpCode.OpNewTable)
							{
								A = Chunk.StackSize + 2
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 3, new Instruction(Chunk, OpCode.OpLoadJump, L2)
							{
								A = Chunk.StackSize + 3
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 4, new Instruction(Chunk, OpCode.OpLoadJump, R2)
							{
								A = Chunk.StackSize + 4
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 5, new Instruction(Chunk, OpCode.OpSetTable, True)
							{
								A = Chunk.StackSize + 2,
								B = TrueIndex,
								C = Chunk.StackSize + 3,
								IsConstantB = true
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 6, new Instruction(Chunk, OpCode.OpSetTable, False)
							{
								A = Chunk.StackSize + 2,
								B = FalseIndex,
								C = Chunk.StackSize + 4,
								IsConstantB = true
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 7, new Instruction(Chunk, OpCode.OpGetTable)
							{
								A = Chunk.StackSize + 1,
								B = Chunk.StackSize + 2,
								C = Chunk.StackSize + 1
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 8, new Instruction(Chunk, OpCode.OpDynamicJump)
							{
								A = Chunk.StackSize + 1
							});
							InstructionPoint += 8;
						}
						else
						{
							Instruction L = Instruction2.References[2];
							Instruction R = Instruction2.JumpTo;
							L.BackReferences.Remove(Instruction2);
							R.BackReferences.Remove(Instruction2);
							Instruction2.OpCode = OpCode.OpNot;
							Instruction2.B = Instruction2.A;
							Instruction2.A = Chunk.StackSize + 1;
							Instruction2.C = 0;
							Instruction2.IsJump = false;
							Instruction2.JumpTo = null;
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 1, new Instruction(Chunk, OpCode.OpNot)
							{
								A = Chunk.StackSize + 1,
								B = Chunk.StackSize + 1
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 2, new Instruction(Chunk, OpCode.OpNewTable)
							{
								A = Chunk.StackSize + 2
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 3, new Instruction(Chunk, OpCode.OpLoadJump, L)
							{
								A = Chunk.StackSize + 3
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 4, new Instruction(Chunk, OpCode.OpLoadJump, R)
							{
								A = Chunk.StackSize + 4
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 5, new Instruction(Chunk, OpCode.OpSetTable, False)
							{
								A = Chunk.StackSize + 2,
								B = FalseIndex,
								C = Chunk.StackSize + 3,
								IsConstantB = true
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 6, new Instruction(Chunk, OpCode.OpSetTable, True)
							{
								A = Chunk.StackSize + 2,
								B = TrueIndex,
								C = Chunk.StackSize + 4,
								IsConstantB = true
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 7, new Instruction(Chunk, OpCode.OpGetTable)
							{
								A = Chunk.StackSize + 1,
								B = Chunk.StackSize + 2,
								C = Chunk.StackSize + 1
							});
							Block2.Instructions.Insert(Block2.Instructions.IndexOf(Instruction2) + 8, new Instruction(Chunk, OpCode.OpDynamicJump)
							{
								A = Chunk.StackSize + 1
							});
							InstructionPoint += 8;
						}
					}
				}
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
		}
	}
}
