using System;
using System.Collections.Generic;
using Obfuscator.Obfuscation;

namespace Obfuscator.Bytecode.IR
{
	[Serializable]
	public class Instruction
	{
		public OpCode OpCode;

		public InstructionType InstructionType;

		public InstructionConstantType ConstantType;

		public List<dynamic> References;

		public List<Instruction> BackReferences;

		public Chunk Chunk;

		public int A;

		public int B;

		public int C;

		public int D;

		public int PC;

		public int Data;

		public int Line;

		public bool IsConstantA = false;

		public bool IsConstantB = false;

		public bool IsConstantC = false;

		public bool RequiresCustomData = false;

		public dynamic CustomData;

		public BasicBlock Block;

		public bool IsJump;

		public Instruction JumpTo;

		public bool IgnoreInstruction = false;

		public int? WrittenVIndex = null;

		public int? WrittenA = null;

		public int? WrittenB = null;

		public int? WrittenC = null;

		public Func<Instruction, Instruction> RegisterHandler = null;

		public CustomInstructionData CustomInstructionData = new CustomInstructionData();

		public Instruction(Instruction Instruction)
		{
			OpCode = Instruction.OpCode;
			InstructionType = Instruction.InstructionType;
			ConstantType = Instruction.ConstantType;
			References = new List<object>(Instruction.References);
			BackReferences = new List<Instruction>(Instruction.BackReferences);
			Chunk = Instruction.Chunk;
			A = Instruction.A;
			B = Instruction.B;
			C = Instruction.C;
			Data = Instruction.Data;
			Line = Instruction.Line;
			IsJump = Instruction.IsJump;
			JumpTo = Instruction.JumpTo;
			IsConstantA = Instruction.IsConstantA;
			IsConstantB = Instruction.IsConstantB;
			IsConstantC = Instruction.IsConstantC;
		}

		public Instruction(Chunk Chunk, OpCode OpCode, params object[] References)
		{
			this.OpCode = OpCode;
			if (Deserializer.InstructionMappings.TryGetValue(OpCode, out var Type))
			{
				InstructionType = Type;
			}
			else
			{
				InstructionType = InstructionType.ABC;
			}
			ConstantType = InstructionConstantType.NK;
			this.References = new List<object> { null, null, null, null, null };
			BackReferences = new List<Instruction>();
			this.Chunk = Chunk;
			A = 0;
			B = 0;
			C = 0;
			Data = 0;
			Line = 0;
			IsConstantA = false;
			IsConstantB = false;
			IsConstantC = false;
			for (int I = 0; I < References.Length; I++)
			{
				object Reference = References[I];
				this.References[I] = Reference;
				if (Reference is Instruction Instruction)
				{
					Instruction.BackReferences.Add(this);
				}
			}
		}

		public void UpdateRegisters()
		{
			if (InstructionType == InstructionType.Data)
			{
				return;
			}
			PC = Chunk.InstructionMap[this];
			switch (OpCode)
			{
			case OpCode.OpLoadK:
			case OpCode.OpGetGlobal:
			case OpCode.OpSetGlobal:
				IsConstantB = true;
				B = Chunk.ConstantMap[(Constant)References[0]];
				break;
			case OpCode.OpJump:
			case OpCode.OpForLoop:
			case OpCode.OpForPrep:
			case OpCode.OpLoadJump:
				B = Chunk.InstructionMap[(Instruction)References[0]];
				break;
			case OpCode.OpClosure:
				B = Chunk.ChunkMap[(Chunk)References[0]];
				break;
			case OpCode.OpGetTable:
			case OpCode.OpSetTable:
			case OpCode.OpSelf:
			case OpCode.OpAdd:
			case OpCode.OpSub:
			case OpCode.OpMul:
			case OpCode.OpDiv:
			case OpCode.OpMod:
			case OpCode.OpPow:
			case OpCode.OpEq:
			case OpCode.OpLt:
			case OpCode.OpLe:
				if (References[0] is Constant ConstantB2)
				{
					IsConstantB = true;
					B = Chunk.ConstantMap[ConstantB2];
				}
				else
				{
					IsConstantB = false;
				}
				if (References[1] is Constant ConstantC)
				{
					IsConstantC = true;
					C = Chunk.ConstantMap[ConstantC];
				}
				else
				{
					IsConstantC = false;
				}
				break;
			case OpCode.Custom:
				if (References[0] is Constant ConstantA)
				{
					IsConstantA = true;
					A = Chunk.ConstantMap[ConstantA];
				}
				else
				{
					IsConstantA = false;
				}
				if (References[1] is Constant ConstantB)
				{
					IsConstantB = true;
					B = Chunk.ConstantMap[ConstantB];
				}
				else
				{
					IsConstantB = false;
				}
				if (References[2] is Constant ConstantC2)
				{
					IsConstantC = true;
					C = Chunk.ConstantMap[ConstantC2];
				}
				else
				{
					IsConstantC = false;
				}
				if (References[0] is Instruction InstructionA)
				{
					A = Chunk.InstructionMap[InstructionA];
				}
				if (References[1] is Instruction InstructionB)
				{
					B = Chunk.InstructionMap[InstructionB];
				}
				if (References[2] is Instruction InstructionC)
				{
					C = Chunk.InstructionMap[InstructionC];
				}
				break;
			}
		}

		public void SetupReferences()
		{
			switch (OpCode)
			{
			case OpCode.OpLoadK:
			case OpCode.OpGetGlobal:
			case OpCode.OpSetGlobal:
			{
				Constant Reference = Chunk.Constants[B];
				References[0] = Reference;
				Reference.BackReferences.Add(this);
				break;
			}
			case OpCode.OpJump:
			case OpCode.OpForLoop:
			case OpCode.OpForPrep:
			{
				Instruction Reference2 = Chunk.Instructions[Chunk.InstructionMap[this] + B + 1];
				References[0] = Reference2;
				Reference2.BackReferences.Add(this);
				break;
			}
			case OpCode.OpClosure:
				References[0] = Chunk.Chunks[B];
				break;
			case OpCode.OpEq:
			case OpCode.OpLt:
			case OpCode.OpLe:
			{
				if (B > 255)
				{
					IsConstantB = true;
					References[0] = Chunk.Constants[B -= 256];
					References[0].BackReferences.Add(this);
				}
				if (C > 255)
				{
					IsConstantC = true;
					References[1] = Chunk.Constants[C -= 256];
					References[1].BackReferences.Add(this);
				}
				Instruction Reference3 = Chunk.Instructions[Chunk.InstructionMap[this] + Chunk.Instructions[Chunk.InstructionMap[this] + 1].B + 1 + 1];
				References[2] = Reference3;
				Reference3.BackReferences.Add(this);
				break;
			}
			case OpCode.OpTest:
			case OpCode.OpTestSet:
			case OpCode.OpTForLoop:
			{
				Instruction Reference4 = Chunk.Instructions[Chunk.InstructionMap[this] + Chunk.Instructions[Chunk.InstructionMap[this] + 1].B + 1 + 1];
				References[2] = Reference4;
				Reference4.BackReferences.Add(this);
				break;
			}
			case OpCode.OpGetTable:
			case OpCode.OpSetTable:
			case OpCode.OpSelf:
			case OpCode.OpAdd:
			case OpCode.OpSub:
			case OpCode.OpMul:
			case OpCode.OpDiv:
			case OpCode.OpMod:
			case OpCode.OpPow:
				if (B > 255)
				{
					IsConstantB = true;
					References[0] = Chunk.Constants[B -= 256];
					References[0].BackReferences.Add(this);
				}
				if (C > 255)
				{
					IsConstantC = true;
					References[1] = Chunk.Constants[C -= 256];
					References[1].BackReferences.Add(this);
				}
				break;
			case OpCode.OpLoadBool:
			case OpCode.OpLoadNil:
			case OpCode.OpGetUpValue:
			case OpCode.OpSetUpValue:
			case OpCode.OpNewTable:
			case OpCode.OpUnm:
			case OpCode.OpNot:
			case OpCode.OpLen:
			case OpCode.OpConcat:
			case OpCode.OpCall:
			case OpCode.OpTailCall:
			case OpCode.OpReturn:
			case OpCode.OpSetList:
			case OpCode.OpClose:
				break;
			}
		}
	}
}
