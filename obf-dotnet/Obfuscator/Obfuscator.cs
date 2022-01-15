using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GParse;
using GParse.Collections;
using GParse.Lexing;
using Loretta;
using Loretta.Lexing;
using Loretta.Parsing;
using Loretta.Parsing.AST;
using Loretta.Parsing.Visitor;
using Obfuscator.Bytecode;
using Obfuscator.Bytecode.IR;
using Obfuscator.Extensions;
using Obfuscator.Obfuscation;
using Obfuscator.Obfuscation.Generation;
using Obfuscator.Obfuscation.Generation.Macros;
using Obfuscator.Obfuscation.OpCodes;
using Obfuscator.Obfuscation.Security;
using Obfuscator.Utility;

namespace Obfuscator
{
	public class Obfuscator
	{
		private Encoding LuaEncoding = Encoding.GetEncoding(28591);

		private Random Random = new Random();

		private ObfuscationContext ObfuscationContext;

		private ObfuscationSettings ObfuscationSettings;

		private Chunk HeadChunk;

		private string Location = "";

		public bool Obfuscating = true;

		public static string ExtraHeader = "local numberoffakes = 2000\r\nlocal fakes = {'DefaultChatSystemChatEvents','secrun','is_beta','secure_call','cache_replace','get_thread_identity','request','protect_gui','run_secure_lua','cache_invalidate','queue_on_teleport','is_cached','set_thread_identity','write_clipboard','run_secure_function','crypto','websocket','unprotect_gui','create_secure_function','crypt','syn','request','SayMessageRequest','FireServer','InvokeServer','tick','pcall','spawn','print','warn','game','GetService','getgc','getreg','getrenv','getgenv','getfenv','debug','require','ModuleScript','LocalScript','GetChildren','GetDescendants','function','settings','GameSettings','RenderSettings','string','sub','service','IsA','Parent','Name','RunService','Stepped','wait','Changed','FindFirstChild','Terrain','Lighting','Enabled','getconnections','firesignal','workspace','true','false','tostring','table','math','random','floor','Humanoid','Character','LocalPlayer','plr','Players','Player','WalkSpeed','Enum','KeyCode','_G','BreakJoints','Health','Chatted','RemoteEvent','RemoteFunction','getrawmetatable','make_writable','setreadonly','PointsService','JointsService','VRService','Ragdoll','SimulationRadiusLocaleId','gethiddenproperty','sethiddenproperty','syn','Zombies','GameId','JobId','Tool','Accessory','RightGrip','Weld','HumanoidRootPart','GuiService','CoreGui','BindableEvent','fire','BodyForce','Chat','PlayerGui','NetworkMarker','Geometry','TextService','LogService','error','LuaSettings','UserInputService','fireclickdetector','Trail','Camera','CurrentCamera','FOV','Path','InputObject','Frame','TextBox','ScreenGui','hookfunction','Debris','ReplicatedStorage','ReplicatedFirst','decompile','saveinstance','TweenService','SoundService','Teams','Tween','BasePart','Seat','Decal','Instance','new','Ray','TweenInfo','Color3','CFrame','Vector3','Vector2','UDim','UDim2','NumberRange','NumberSequence','Handle','Gravity','HopperBin','Shirt','Pants','Mouse','IntValue','StringValue','Value','VirtualUser','MouseButton1Click','Activated','FileMesh','TeleportService','Teleport','userdata','string','int','number','bool','BodyGyro','Backpack','SkateboardPlatform','FilteringEnabled','Shoot','Shell','Asset','checkifgay','create','god','BrianSucksVexu','checkifalive','getteams','getnearest','getcross','autoshoot','chatspam','changeupvalues','modifyguns','infammo','godmode','aimbot','esp','crashserver','antiaim'}\r\nlocal faked = {}\r\nfor i = 1,numberoffakes do\r\ntable.insert(faked, 'a34534345 = \\'' ..tostring(fakes[math.random(1,#fakes)])..'\\'')\r\nend\r\ntable.concat(faked,'\\n')";

		public Obfuscator(ObfuscationSettings ObfuscationSettings, string Location)
		{
			this.ObfuscationSettings = ObfuscationSettings;
			this.Location = Location;
		}

		private bool IsUsed(Chunk Chunk, VOpCode Virtual)
		{
			bool Return = false;
			foreach (Instruction Instruction in Chunk.Instructions)
			{
				if (Virtual.IsInstruction(Instruction))
				{
					if (!ObfuscationContext.InstructionMapping.ContainsKey(Instruction.OpCode))
					{
						ObfuscationContext.InstructionMapping.Add(Instruction.OpCode, Virtual);
					}
					Instruction.CustomInstructionData = new CustomInstructionData
					{
						OpCode = Virtual
					};
					Return = true;
				}
			}
			foreach (Chunk sChunk in Chunk.Chunks)
			{
				Return |= IsUsed(sChunk, Virtual);
			}
			return Return;
		}

		public string ObfuscateString(string Source)
		{
			if (!Directory.Exists(Location))
			{
				Directory.CreateDirectory(Location);
			}
			File.WriteAllText(Path.Combine(Location, "Input.lua"), Source);
			File.WriteAllText(Path.Combine(Location, "Output.lua"), "");
			Obfuscator Obfuscator = new Obfuscator(new ObfuscationSettings(ObfuscationSettings), Location);
			string Error = "";
			Obfuscator.Compile(out Error);
			Obfuscator.Deserialize(out Error);
			Obfuscator.Obfuscate(out Error);
			return File.ReadAllText(Path.Combine(Location, "Output.lua"));
		}

		public bool Compile(out string Error)
		{
			Error = "";
			if (!Directory.Exists(Location))
			{
				Error = "[Error #1] File Directory Does Not Exist!";
				return false;
			}
			if (!File.Exists(Path.Combine(Location, "Input.lua")))
			{
				Error = "[Error #2] Input File Does Not Exist!";
				return false;
			}
			string Input = Path.Combine(Location, "Input.lua");
			string ByteCode = Path.Combine(Location, "LuaC.out");
			string src = File.ReadAllText(Input);
			Utility.Utility.GetExtraStrings(src);
			File.WriteAllText(Input, ExtraHeader + "\n" + src);
			if (!ObfuscationSettings.DisableAllMacros)
			{
				LuaOptions LuaOptions = new LuaOptions(acceptBinaryNumbers: true, acceptCCommentSyntax: false, acceptCompoundAssignment: true, acceptEmptyStatements: false, acceptGModCOperators: true, acceptGoto: true, acceptHexEscapesInStrings: true, acceptHexFloatLiterals: true, acceptOctalNumbers: true, acceptShebang: false, acceptUnderlineInNumberLiterals: true, useLuaJitIdentifierRules: true, ContinueType.ContextualKeyword);
				LuaLexerBuilder LexerBuilder = new LuaLexerBuilder(LuaOptions);
				LuaParserBuilder ParserBuilder = new LuaParserBuilder(LuaOptions);
				DiagnosticList Diagnostics = new DiagnosticList();
				ILexer<LuaTokenType> Lexer = LexerBuilder.CreateLexer(File.ReadAllText(Input), Diagnostics);
				TokenReader<LuaTokenType> TokenReader = new TokenReader<LuaTokenType>(Lexer);
				LuaParser Parser = ParserBuilder.CreateParser(TokenReader, Diagnostics);
				StatementList Tree = Parser.Parse();
				if (Diagnostics.Any((Diagnostic Diagnostic) => Diagnostic.Severity == DiagnosticSeverity.Error))
				{
					Error = "[?] [Parsing] Syntax Error";
					return false;
				}
				File.WriteAllText(Input, FormattedLuaCodeSerializer.Format(LuaOptions.All, Tree, new Func<string, string>(ObfuscateString), ObfuscationSettings.EncryptAllStrings, Location, ObfuscationSettings.PremiumFormat));
			}
			Process process = new Process();
			process.StartInfo.FileName = "Lua/LuaC.exe";
			process.StartInfo.Arguments = "-o \"" + ByteCode + "\" \"" + Input + "\"";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			Process Process = process;
			Process.Start();
			Process.WaitForExit();
			if (!Obfuscating)
			{
				Error = "[?] Process Terminated.";
				return false;
			}
			if (!File.Exists(ByteCode))
			{
				Error = "[Error #3] Lua Error: Error While Compiling Script! (Syntax Error?)";
				return false;
			}
			return true;
		}

		public bool Deserialize(out string Error)
		{
			Error = "";
			Deserializer Deserializer = new Deserializer(File.ReadAllBytes(Path.Combine(Location, "LuaC.out")));
			try
			{
				HeadChunk = Deserializer.DecodeFile();
			}
			catch
			{
				Error = "[Error #4] Error While Deserializing File!";
				return false;
			}
			if (!Obfuscating)
			{
				Error = "[?] Process Terminated.";
				return false;
			}
			ObfuscationContext = new ObfuscationContext(HeadChunk);
			ObfuscationContext.Obfuscator = this;
			return true;
		}

		public bool Obfuscate(out string Error)
		{
			Error = "";
			List<VOpCode> AdditionalVirtuals = new List<VOpCode>();
			if (!ObfuscationSettings.DisableAllMacros)
			{
				new BytecodeSecurity(HeadChunk, ObfuscationSettings, ObfuscationContext, AdditionalVirtuals).DoChunks();
			}
			if (!ObfuscationSettings.DisableAllMacros)
			{
				new LuaMacros(HeadChunk, AdditionalVirtuals).DoChunks();
			}
			if (ObfuscationSettings.ControlFlowObfuscation)
			{
				ShuffleControlFlow(HeadChunk);
			}
			List<VOpCode> Virtuals = (from VOpCode T in (from T in Assembly.GetExecutingAssembly().GetTypes()
					where T.IsSubclassOf(typeof(VOpCode))
					select T).Select(Activator.CreateInstance)
				where IsUsed(HeadChunk, T)
				select T).ToList();
			foreach (VOpCode Virtual in AdditionalVirtuals)
			{
				Virtuals.Add(Virtual);
			}
			if (!ObfuscationSettings.DisableSuperOperators)
			{
				new SuperOperators().DoChunk(HeadChunk, Virtuals);
			}
			Virtuals.Shuffle();
			for (int I = 0; I < Virtuals.Count; I++)
			{
				Virtuals[I].VIndex = I;
			}
			if (ObfuscationSettings.PremiumFormat)
			{
				Utility.Utility.NoExtraString = true;
				PremiumScriptBuilder ScriptBuilder2 = new PremiumScriptBuilder(HeadChunk, ObfuscationContext, ObfuscationSettings, Virtuals);
				string Source2 = ScriptBuilder2.BuildScript(Location);
			}
			else
			{
				ScriptBuilder ScriptBuilder = new ScriptBuilder(HeadChunk, ObfuscationContext, ObfuscationSettings, Virtuals);
				string Source = ScriptBuilder.BuildScript(Location);
			}
			return true;
			static void ShuffleControlFlow(Chunk Chunk)
			{
				foreach (Chunk SubChunk in Chunk.Chunks)
				{
					ShuffleControlFlow(SubChunk);
				}
				List<BasicBlock> BasicBlocks = new BasicBlock().GenerateBasicBlocks(Chunk);
				Instruction EntryPoint = Chunk.Instructions.First();
				Dictionary<int, BasicBlock> BlockMap = new Dictionary<int, BasicBlock>();
				BasicBlocks.Shuffle();
				int InstructionPoint = 0;
				foreach (BasicBlock Block3 in BasicBlocks)
				{
					foreach (Instruction Instruction3 in Block3.Instructions)
					{
						BlockMap[InstructionPoint] = Block3;
						InstructionPoint++;
					}
				}
				foreach (BasicBlock Block2 in BasicBlocks)
				{
					if (Block2.Instructions.Count != 0)
					{
						Instruction Instruction2 = Block2.Instructions.Last();
						switch (Instruction2.OpCode)
						{
						case OpCode.OpForLoop:
						case OpCode.OpForPrep:
							Block2.Instructions.Add(new Instruction(Chunk, OpCode.OpJump, Block2.References[0].Instructions[0]));
							break;
						case OpCode.OpEq:
						case OpCode.OpLt:
						case OpCode.OpLe:
						case OpCode.OpTest:
						case OpCode.OpTestSet:
						case OpCode.OpTForLoop:
							Block2.Instructions.Add(new Instruction(Chunk, OpCode.OpJump, Block2.References[0].Instructions[0]));
							break;
						default:
							Block2.Instructions.Add(new Instruction(Chunk, OpCode.OpJump, Block2.References[0].Instructions[0]));
							break;
						case OpCode.OpJump:
						case OpCode.OpReturn:
							break;
						}
					}
				}
				Chunk.Instructions.Clear();
				Chunk.Instructions.Add(new Instruction(Chunk, OpCode.OpJump, EntryPoint));
				foreach (BasicBlock Block in BasicBlocks)
				{
					foreach (Instruction Instruction in Block.Instructions)
					{
						Chunk.Instructions.Add(Instruction);
					}
				}
				Chunk.UpdateMappings();
			}
		}
	}
}
