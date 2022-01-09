using System;
using Obfuscator.Bytecode.IR;

namespace Obfuscator.Obfuscation.OpCodes
{
	[Serializable]
	public abstract class VOpCode
	{
		public int VIndex;

		public abstract bool IsInstruction(Instruction Instruction);

		public abstract string GetObfuscated(ObfuscationContext ObfuscationContext);

		public virtual void Mutate(Instruction Instruction)
		{
		}
	}
}
