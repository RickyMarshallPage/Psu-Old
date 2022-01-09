using Obfuscator.Bytecode.IR;

namespace Obfuscator.Obfuscation.OpCodes
{
	public class OpLoadJump : VOpCode
	{
		public override bool IsInstruction(Instruction Instruction)
		{
			return Instruction.OpCode == OpCode.OpLoadJump;
		}

		public override string GetObfuscated(ObfuscationContext ObfuscationContext)
		{
			return "Stack[Instruction[OP_A]] = Instruction[OP_B];";
		}

		public override void Mutate(Instruction Instruction)
		{
			Instruction.B = Instruction.Chunk.InstructionMap[Instruction.References[0]];
			Instruction.InstructionType = InstructionType.AsBx;
		}
	}
}
