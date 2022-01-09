using System;
using System.Collections.Generic;
using System.Linq;

namespace Obfuscator.Bytecode.IR
{
	public class BasicBlock
	{
		public List<Instruction> Instructions = new List<Instruction>();

		public List<BasicBlock> References = new List<BasicBlock>();

		public List<BasicBlock> BackReferences = new List<BasicBlock>();

		public List<BasicBlock> GenerateBasicBlocks(Chunk Chunk)
		{
			Random Random = new Random();
			List<BasicBlock> BasicBlocks = new List<BasicBlock>();
			int InstructionPoint = 0;
			BasicBlock BasicBlock = null;
			Dictionary<int, BasicBlock> BlockMap = new Dictionary<int, BasicBlock>();
			for (; InstructionPoint < Chunk.Instructions.Count; InstructionPoint++)
			{
				Instruction Instruction = Chunk.Instructions[InstructionPoint];
				if (Instruction.BackReferences.Count > 0)
				{
					BasicBlock = null;
				}
				if (BasicBlock == null)
				{
					BasicBlock = new BasicBlock();
					BasicBlocks.Add(BasicBlock);
				}
				BasicBlock.Instructions.Add(Instruction);
				BlockMap[InstructionPoint] = BasicBlock;
				OpCode opCode = Instruction.OpCode;
				OpCode opCode2 = opCode;
				if ((uint)(opCode2 - 22) <= 5u || (uint)(opCode2 - 30) <= 3u)
				{
					BasicBlock = null;
				}
				if (Instruction.IsJump)
				{
					BasicBlock = null;
				}
			}
			BasicBlocks.First().BackReferences.Add(new BasicBlock());
			foreach (BasicBlock Block in BasicBlocks)
			{
				if (Block.Instructions.Count == 0)
				{
					continue;
				}
				Instruction Instruction2 = Block.Instructions.Last();
				switch (Instruction2.OpCode)
				{
				case OpCode.OpForLoop:
				case OpCode.OpForPrep:
					Block.References.Add(BlockMap[Chunk.InstructionMap[Instruction2] + 1]);
					Block.References.Add(BlockMap[Chunk.InstructionMap[Instruction2.References[0]]]);
					break;
				case OpCode.OpJump:
					Block.References.Add(BlockMap[Chunk.InstructionMap[Instruction2.References[0]]]);
					break;
				case OpCode.OpEq:
				case OpCode.OpLt:
				case OpCode.OpLe:
				case OpCode.OpTest:
				case OpCode.OpTestSet:
				case OpCode.OpTForLoop:
					Block.References.Add(BlockMap[Chunk.InstructionMap[Instruction2] + 1]);
					Block.References.Add(BlockMap[Chunk.InstructionMap[Instruction2.References[2]]]);
					break;
				default:
					Block.References.Add(BlockMap[Chunk.InstructionMap[Instruction2] + 1]);
					break;
				case OpCode.OpReturn:
					break;
				}
				foreach (BasicBlock Reference in Block.References)
				{
					Reference.BackReferences.Add(Block);
				}
			}
			return BasicBlocks;
		}

		public List<BasicBlock> GenerateBasicBlocksFromInstructions(Chunk Chunk, List<Instruction> Instructions)
		{
			Random Random = new Random();
			List<BasicBlock> BasicBlocks = new List<BasicBlock>();
			int InstructionPoint = 0;
			BasicBlock BasicBlock = null;
			Dictionary<int, BasicBlock> BlockMap = new Dictionary<int, BasicBlock>();
			for (; InstructionPoint < Instructions.Count; InstructionPoint++)
			{
				Instruction Instruction = Instructions[InstructionPoint];
				if (Instruction.BackReferences.Count > 0)
				{
					BasicBlock = null;
				}
				if (BasicBlock == null)
				{
					BasicBlock = new BasicBlock();
					BasicBlocks.Add(BasicBlock);
				}
				BasicBlock.Instructions.Add(Instruction);
				BlockMap[InstructionPoint] = BasicBlock;
				OpCode opCode = Instruction.OpCode;
				OpCode opCode2 = opCode;
				if ((uint)(opCode2 - 22) <= 5u || (uint)(opCode2 - 30) <= 3u)
				{
					BasicBlock = null;
				}
				if (Instruction.IsJump)
				{
					BasicBlock = null;
				}
			}
			foreach (BasicBlock Block in BasicBlocks)
			{
				if (Block.Instructions.Count == 0)
				{
					continue;
				}
				Instruction Instruction2 = Block.Instructions.Last();
				switch (Instruction2.OpCode)
				{
				case OpCode.OpForLoop:
				case OpCode.OpForPrep:
					if (BlockMap.ContainsKey(Chunk.InstructionMap[Instruction2] + 1))
					{
						Block.References.Add(BlockMap[Chunk.InstructionMap[Instruction2] + 1]);
					}
					if (BlockMap.ContainsKey(Chunk.InstructionMap[Instruction2.References[0]]))
					{
						Block.References.Add(BlockMap[Chunk.InstructionMap[Instruction2.References[0]]]);
					}
					break;
				case OpCode.OpJump:
					if (BlockMap.ContainsKey(Chunk.InstructionMap[Instruction2.References[0]]))
					{
						Block.References.Add(BlockMap[Chunk.InstructionMap[Instruction2.References[0]]]);
					}
					break;
				case OpCode.OpEq:
				case OpCode.OpLt:
				case OpCode.OpLe:
				case OpCode.OpTest:
				case OpCode.OpTestSet:
				case OpCode.OpTForLoop:
					if (BlockMap.ContainsKey(Chunk.InstructionMap[Instruction2] + 1))
					{
						Block.References.Add(BlockMap[Chunk.InstructionMap[Instruction2] + 1]);
					}
					if (BlockMap.ContainsKey(Chunk.InstructionMap[Instruction2.References[2]]))
					{
						Block.References.Add(BlockMap[Chunk.InstructionMap[Instruction2.References[2]]]);
					}
					break;
				default:
					if (BlockMap.ContainsKey(Chunk.InstructionMap[Instruction2] + 1))
					{
						Block.References.Add(BlockMap[Chunk.InstructionMap[Instruction2] + 1]);
					}
					break;
				case OpCode.OpReturn:
					break;
				}
				foreach (BasicBlock Reference in Block.References)
				{
					Reference.BackReferences.Add(Block);
				}
			}
			return BasicBlocks;
		}
	}
}
