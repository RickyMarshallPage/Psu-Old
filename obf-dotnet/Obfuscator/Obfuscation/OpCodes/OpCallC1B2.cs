using Obfuscator.Bytecode.IR;

namespace Obfuscator.Obfuscation.OpCodes
{
	public class OpCallC1B2 : VOpCode
	{
		public override bool IsInstruction(Instruction Instruction)
		{
			return Instruction.OpCode == OpCode.OpCall && Instruction.B == 2 && Instruction.C == 1;
		}

		public override string GetObfuscated(ObfuscationContext ObfuscationContext)
		{
			return "local A = Instruction[OP_A]; Stack[A](Stack[A + 1]); for I = A, StackSize do Stack[I] = nil; end;";
		}

		public override void Mutate(Instruction Instruction)
		{
			Instruction.B = 0;
			Instruction.C = 0;
		}
	}
}
