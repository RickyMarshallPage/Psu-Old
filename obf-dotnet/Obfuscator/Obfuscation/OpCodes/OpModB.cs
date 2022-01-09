using Obfuscator.Bytecode.IR;

namespace Obfuscator.Obfuscation.OpCodes
{
	public class OpModB : VOpCode
	{
		public override bool IsInstruction(Instruction Instruction)
		{
			return Instruction.OpCode == OpCode.OpMod && Instruction.IsConstantB && !Instruction.IsConstantC;
		}

		public override string GetObfuscated(ObfuscationContext ObfuscationContext)
		{
			return "Stack[Instruction[OP_A]] = Constants[Instruction[OP_B]] % Stack[Instruction[OP_C]];";
		}

		public override void Mutate(Instruction Instruction)
		{
			Instruction.ConstantType |= InstructionConstantType.RB;
		}
	}
}
