using Obfuscator.Bytecode.IR;

namespace Obfuscator.Obfuscation.OpCodes
{
	public class OpNeC : VOpCode
	{
		public override bool IsInstruction(Instruction Instruction)
		{
			return Instruction.OpCode == OpCode.OpEq && Instruction.A != 0 && !Instruction.IsConstantB && Instruction.IsConstantC;
		}

		public override string GetObfuscated(ObfuscationContext ObfuscationContext)
		{
			return "if (Stack[Instruction[OP_A]] == Constants[Instruction[OP_C]]) then InstructionPoint = Instruction[OP_B]; end;";
		}

		public override void Mutate(Instruction Instruction)
		{
			Instruction.IsConstantA = false;
			Instruction.IsConstantB = false;
			Instruction.IsConstantC = true;
			Instruction.A = Instruction.B;
			Instruction.B = Instruction.Chunk.InstructionMap[Instruction.References[2]];
			Instruction.InstructionType = InstructionType.AsBxC;
			Instruction.ConstantType |= InstructionConstantType.RC;
		}
	}
}
