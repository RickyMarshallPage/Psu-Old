using System;
using System.Collections.Generic;
using System.Linq;
using Obfuscator.Bytecode.IR;
using Obfuscator.Extensions;
using Obfuscator.Obfuscation.Generation;
using Obfuscator.Obfuscation.OpCodes;
using Obfuscator.Utility;

namespace Obfuscator.Obfuscation
{
	public class ObfuscationContext
	{
		public Chunk HeadChunk;

		public Dictionary<OpCode, VOpCode> InstructionMapping;

		public ChunkStep[] ChunkSteps;

		public InstructionStep[] InstructionStepsABC;

		public InstructionStep[] InstructionStepsABx;

		public InstructionStep[] InstructionStepsAsBx;

		public InstructionStep[] InstructionStepsAsBxC;

		public InstructionStep[] InstructionSteps;

		public int[] ConstantMapping;

		public int PrimaryXORKey;

		public int InitialPrimaryXORKey;

		public int[] XORKeys;

		public Obfuscator Obfuscator;

		private Random Random = new Random();

		public InstructionMap Instruction = default(InstructionMap);

		public ChunkMap Chunk = default(ChunkMap);

		public List<DeserializerInstructionStep> DeserializerInstructionSteps;

		public List<string> PrimaryIndicies;

		public string ByteCode;

		public string FormatTable;

		public Dictionary<long, NumberEquation> NumberEquations;

		public ObfuscationContext(Chunk HeadChunk)
		{
			this.HeadChunk = HeadChunk;
			InstructionMapping = new Dictionary<OpCode, VOpCode>();
			ChunkSteps = (from I in Enumerable.Range(0, 5)
				select (ChunkStep)I).ToArray();
			ChunkSteps.Shuffle();
			InstructionSteps = (from I in Enumerable.Range(0, 4)
				select (InstructionStep)I).ToArray();
			InstructionSteps.Shuffle();
			InstructionStepsABC = InstructionSteps.ToArray().Shuffle().ToArray();
			InstructionStepsABx = InstructionSteps.ToArray().Shuffle().ToArray();
			InstructionStepsAsBx = InstructionSteps.ToArray().Shuffle().ToArray();
			InstructionStepsAsBxC = InstructionSteps.ToArray().Shuffle().ToArray();
			PrimaryXORKey = Random.Next(0, 256);
			InitialPrimaryXORKey = PrimaryXORKey;
			XORKeys = new int[10]
			{
				Random.Next(0, 256),
				Random.Next(0, 256),
				Random.Next(0, 256),
				Random.Next(0, 256),
				Random.Next(0, 256),
				Random.Next(0, 256),
				Random.Next(0, 256),
				Random.Next(0, 256),
				Random.Next(0, 256),
				Random.Next(0, 256)
			};
			Instruction.A = global::Obfuscator.Utility.Utility.GetIndexListNoBrackets();
			Instruction.B = global::Obfuscator.Utility.Utility.GetIndexListNoBrackets();
			Instruction.C = global::Obfuscator.Utility.Utility.GetIndexListNoBrackets();
			Instruction.D = global::Obfuscator.Utility.Utility.GetIndexListNoBrackets();
			Instruction.E = global::Obfuscator.Utility.Utility.GetIndexListNoBrackets();
			Instruction.Enum = global::Obfuscator.Utility.Utility.GetIndexListNoBrackets();
			Instruction.Data = global::Obfuscator.Utility.Utility.GetIndexListNoBrackets();
			Chunk.ParameterCount = global::Obfuscator.Utility.Utility.GetIndexListNoBrackets();
			Chunk.Instructions = global::Obfuscator.Utility.Utility.GetIndexListNoBrackets();
			Chunk.Chunks = global::Obfuscator.Utility.Utility.GetIndexListNoBrackets();
			Chunk.Constants = global::Obfuscator.Utility.Utility.GetIndexListNoBrackets();
			Chunk.StackSize = global::Obfuscator.Utility.Utility.GetIndexListNoBrackets();
			Chunk.InstructionPoint = global::Obfuscator.Utility.Utility.GetIndexListNoBrackets();
			DeserializerInstructionSteps = new List<DeserializerInstructionStep>();
			PrimaryIndicies = new List<string> { "A", "B", "C" }.Shuffle().ToList();
			NumberEquations = new Dictionary<long, NumberEquation>();
			int Count = Random.Next(5, 15);
			for (int I2 = 0; I2 < Count; I2++)
			{
				NumberEquations.Add(Random.Next(0, 1000000000), new NumberEquation(Random.Next(3, 6)));
			}
			int _ = Random.Next(0, 16);
			int[] obj = new int[5] { _, 0, 0, 0, 0 };
			_ = (obj[4] = (obj[3] = (obj[2] = (obj[1] = _ + Random.Next(1, 16)) + Random.Next(1, 16)) + Random.Next(1, 16)) + Random.Next(1, 16));
			ConstantMapping = obj;
			ConstantMapping.Shuffle();
		}
	}
}
