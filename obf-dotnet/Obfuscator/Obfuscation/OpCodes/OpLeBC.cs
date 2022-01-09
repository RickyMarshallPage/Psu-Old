using Obfuscator.Bytecode.IR;

namespace Obfuscator.Obfuscation.OpCodes
{
	public class OpLeBC : VOpCode
	{
		public override bool IsInstruction(Instruction Instruction)
		{
			return Instruction.OpCode == OpCode.OpLe && Instruction.A == 0 && Instruction.IsConstantB && Instruction.IsConstantC;
		}

		public override string GetObfuscated(ObfuscationContext ObfuscationContext)
		{
			return "if (Constants[Instruction[OP_A]] > Constants[Instruction[OP_C]]) then InstructionPoint = Instruction[OP_B]; end;";
		}

		public override void Mutate(Instruction Instruction)
		{
			Instruction.IsConstantA = true;
			Instruction.IsConstantB = false;
			Instruction.IsConstantC = true;
			Instruction.A = Instruction.B;
			Instruction.B = Instruction.Chunk.InstructionMap[Instruction.References[2]];
			Instruction.InstructionType = InstructionType.AsBxC;
			Instruction.ConstantType |= (InstructionConstantType)40;
		}
	}
}
