using Obfuscator.Bytecode.IR;

namespace Obfuscator.Obfuscation.OpCodes
{
	public class OpAddC : VOpCode
	{
		public override bool IsInstruction(Instruction Instruction)
		{
			return Instruction.OpCode == OpCode.OpAdd && !Instruction.IsConstantB && Instruction.IsConstantC;
		}

		public override string GetObfuscated(ObfuscationContext ObfuscationContext)
		{
			return "Stack[Instruction[OP_A]] = Stack[Instruction[OP_B]] + Constants[Instruction[OP_C]];";
		}

		public override void Mutate(Instruction Instruction)
		{
			Instruction.ConstantType |= InstructionConstantType.RC;
		}
	}
}
