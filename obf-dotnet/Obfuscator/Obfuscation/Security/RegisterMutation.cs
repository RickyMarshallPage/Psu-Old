using System;
using System.Collections.Generic;
using System.Linq;
using Obfuscator.Bytecode.IR;
using Obfuscator.Extensions;
using Obfuscator.Obfuscation.OpCodes;
using Obfuscator.Utility;

namespace Obfuscator.Obfuscation.Security
{
	public class RegisterMutation
	{
		private Random Random = new Random();

		private Chunk Chunk;

		private List<Instruction> Instructions;

		private List<VOpCode> Virtuals;

		private ObfuscationContext ObfuscationContext;

		private int Start;

		private int End;

		public RegisterMutation(ObfuscationContext ObfuscationContext, Chunk Chunk, List<Instruction> Instructions, List<VOpCode> Virtuals, int Start, int End)
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
			int Enum = Random.Next(0, 255);
			int A = Random.Next(0, 255);
			int B = Random.Next(0, 255);
			int C = Random.Next(0, 255);
			long nA = ObfuscationContext.NumberEquations.Keys.ToList().Random();
			long nB = ObfuscationContext.NumberEquations.Keys.ToList().Random();
			long nC = ObfuscationContext.NumberEquations.Keys.ToList().Random();
			int iA = Random.Next(0, 2);
			int iB = Random.Next(0, 2);
			int iC = Random.Next(0, 2);
			OpCustom Virtual = new OpCustom();
			Virtuals.Add(Virtual);
			foreach (BasicBlock Block2 in BasicBlocks)
			{
				List<Instruction> InstructionList = Block2.Instructions.ToList();
				foreach (Instruction Instruction2 in InstructionList)
				{
					bool References = false;
					foreach (dynamic Reference in Instruction2.References)
					{
						if (Reference != null)
						{
							References = true;
						}
					}
					if (!References && !Instruction2.IsConstantA && !Instruction2.IsConstantB && !Instruction2.IsConstantC && !Instruction2.IgnoreInstruction && Instruction2 != Block2.Instructions.First() && Instruction2.RegisterHandler == null && Random.Next(1, 5) == 0)
					{
						Instruction2.IgnoreInstruction = true;
						Instruction rInstruction = new Instruction(Chunk, OpCode.Custom);
						rInstruction.IgnoreInstruction = true;
						rInstruction.CustomInstructionData.OpCode = Virtual;
						rInstruction.References[1] = Instruction2;
						rInstruction.RequiresCustomData = true;
						rInstruction.InstructionType = InstructionType.ABx;
						Block2.Instructions.Insert(1, rInstruction);
						rInstruction.RegisterHandler = delegate
						{
							rInstruction.B = Chunk.InstructionMap[Instruction2];
							ObfuscationContext.InstructionMapping.TryGetValue(OpCode.None, out var value);
							rInstruction.RequiresCustomData = true;
							rInstruction.CustomData = new List<int>
							{
								Instruction2.CustomInstructionData.OpCode.VIndex ^ Enum,
								(int)((iA == 0) ? (Instruction2.A ^ A) : ObfuscationContext.NumberEquations[nA].ComputeExpression(Instruction2.A)),
								(int)((iB == 0) ? (Instruction2.B ^ B) : ObfuscationContext.NumberEquations[nB].ComputeExpression(Instruction2.B)),
								(int)((iC == 0) ? (Instruction2.C ^ C) : ObfuscationContext.NumberEquations[nC].ComputeExpression(Instruction2.C))
							};
							Virtual.Obfuscated = "\r\n\r\n\t\t\t\t\t\tlocal oInstruction = Instructions[Instruction[OP_B]];\r\n\t\t\t\t\t\tlocal D = Instruction[OP_D];\r\n\r\n\t\t\t\t\t\t" + string.Join("\n", new List<string>
							{
								"oInstruction[OP_ENUM] = BitXOR(D[" + Utility.Utility.IntegerToString(1, 2) + "], " + Utility.Utility.IntegerToString(Enum, 2) + ");",
								"oInstruction[OP_A] = " + ((iA == 0) ? ("BitXOR(D[" + Utility.Utility.IntegerToString(2, 2) + "], " + Utility.Utility.IntegerToString(A, 2) + ");") : $"CalculateVM({nA}, D[{Utility.Utility.IntegerToString(2, 2)}])"),
								"oInstruction[OP_B] = " + ((iB == 0) ? ("BitXOR(D[" + Utility.Utility.IntegerToString(3, 2) + "], " + Utility.Utility.IntegerToString(B, 2) + ");") : $"CalculateVM({nB}, D[{Utility.Utility.IntegerToString(3, 2)}])"),
								"oInstruction[OP_C] = " + ((iC == 0) ? ("BitXOR(D[" + Utility.Utility.IntegerToString(4, 2) + "], " + Utility.Utility.IntegerToString(C, 2) + ");") : $"CalculateVM({nC}, D[{Utility.Utility.IntegerToString(4, 2)}])")
							}.Shuffle()) + "\r\n\r\n\t\t\t\t\t\tInstruction[OP_ENUM] = (" + Utility.Utility.IntegerToString(value.VIndex) + ");\r\n\r\n\t\t\t\t\t\t";
							rInstruction.InstructionType = InstructionType.ABx;
							return rInstruction;
						};
						Instruction2.RegisterHandler = delegate
						{
							Instruction2.WrittenVIndex = 0;
							Instruction2.A = 0;
							Instruction2.B = 0;
							Instruction2.C = 0;
							return Instruction2;
						};
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
			new IMutateCFlow(ObfuscationContext, Chunk, IList, Virtuals, Start, End).DoInstructions();
		}
	}
}
