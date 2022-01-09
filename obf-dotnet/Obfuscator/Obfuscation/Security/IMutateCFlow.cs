using System;
using System.Collections.Generic;
using System.Linq;
using Obfuscator.Bytecode.IR;
using Obfuscator.Obfuscation.OpCodes;
using Obfuscator.Utility;

namespace Obfuscator.Obfuscation.Security
{
	public class IMutateCFlow
	{
		private Random Random = new Random();

		private Chunk Chunk;

		private List<Instruction> Instructions;

		private List<VOpCode> Virtuals;

		private ObfuscationContext ObfuscationContext;

		private int Start;

		private int End;

		public IMutateCFlow(ObfuscationContext ObfuscationContext, Chunk Chunk, List<Instruction> Instructions, List<VOpCode> Virtuals, int Start, int End)
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
			OpCustom SetVMKey = new OpCustom();
			SetVMKey.Obfuscated = "VMKey = Instruction[OP_A];";
			Virtuals.Add(SetVMKey);
			OpCustom Virtual = new OpCustom();
			Virtuals.Add(Virtual);
			List<BasicBlock> BasicBlocks = new BasicBlock().GenerateBasicBlocksFromInstructions(Chunk, Instructions);
			List<BasicBlock> ProcessedBlocks = new List<BasicBlock>();
			foreach (BasicBlock Block2 in BasicBlocks)
			{
				if (Block2.References.Count <= 0)
				{
					continue;
				}
				bool Continue = true;
				foreach (BasicBlock Reference2 in Block2.References)
				{
					if (Reference2.BackReferences.Count != 1)
					{
						Continue = false;
					}
				}
				if (!Continue)
				{
					continue;
				}
				int Key = Random.Next(0, 256);
				Block2.Instructions.Insert(Block2.Instructions.Count - 1, new Instruction(Chunk, OpCode.Custom)
				{
					A = Key,
					CustomInstructionData = new CustomInstructionData
					{
						OpCode = SetVMKey
					}
				});
				foreach (BasicBlock Reference in Block2.References)
				{
					List<Instruction> InstructionList = Reference.Instructions.ToList();
					foreach (Instruction Instruction2 in InstructionList)
					{
						bool References = false;
						foreach (dynamic iReference in Instruction2.References)
						{
							if (iReference != null)
							{
								References = true;
							}
						}
						if (!References && !Instruction2.IsConstantA && !Instruction2.IsConstantB && !Instruction2.IsConstantC && !Instruction2.IgnoreInstruction && Instruction2 != Reference.Instructions.First() && Instruction2.RegisterHandler == null)
						{
							Instruction2.IgnoreInstruction = true;
							Instruction rInstruction = new Instruction(Chunk, OpCode.Custom);
							rInstruction.IgnoreInstruction = true;
							rInstruction.CustomInstructionData.OpCode = Virtual;
							rInstruction.References[1] = Instruction2;
							rInstruction.InstructionType = InstructionType.ABx;
							Reference.Instructions.Insert(1, rInstruction);
							rInstruction.RegisterHandler = delegate
							{
								rInstruction.B = Chunk.InstructionMap[Instruction2];
								ObfuscationContext.InstructionMapping.TryGetValue(OpCode.None, out var value);
								Virtual.Obfuscated = "local oInstruction = Instructions[Instruction[OP_B]]; oInstruction[OP_ENUM] = BitXOR(oInstruction[OP_ENUM], VMKey); Instruction[OP_ENUM] = (" + global::Obfuscator.Utility.Utility.IntegerToString(value.VIndex) + ");";
								rInstruction.InstructionType = InstructionType.ABx;
								return rInstruction;
							};
							Instruction2.RegisterHandler = delegate
							{
								Instruction2.WrittenVIndex = Instruction2.CustomInstructionData.OpCode.VIndex ^ Key;
								return Instruction2;
							};
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
			End = Chunk.InstructionMap[BasicBlocks.Last().Instructions.Last()];
			List<Instruction> IList = Chunk.Instructions.Skip(Start).Take(End - Start).ToList();
		}
	}
}
