namespace Obfuscator.Obfuscation
{
	public class ObfuscationSettings
	{
		public const int ConstantOffset = 1024;

		public const int MaximumVirtuals = 200;

		public const int MaximumSuperOperatorLength = 50;

		public const int MaximumSuperOperators = 128;

		public bool DisableSuperOperators = false;

		public bool MaximumSecurityEnabled = false;

		public bool ControlFlowObfuscation = true;

		public bool ConstantEncryption = false;

		public bool EncryptAllStrings = false;

		public bool DisableAllMacros = false;

		public bool EnhancedOutput = false;

		public bool EnhancedConstantEncryption = false;

		public bool CompressedOutput = false;

		public bool PremiumFormat = false;

		public bool DebugMode = false;

		public string ByteCodeMode = "Default";

		public ObfuscationSettings()
		{
		}

		public ObfuscationSettings(ObfuscationSettings ObfuscationSettings)
		{
			DisableSuperOperators = ObfuscationSettings.DisableSuperOperators;
			MaximumSecurityEnabled = ObfuscationSettings.MaximumSecurityEnabled;
			ControlFlowObfuscation = ObfuscationSettings.ControlFlowObfuscation;
			ConstantEncryption = ObfuscationSettings.ConstantEncryption;
			EncryptAllStrings = ObfuscationSettings.EncryptAllStrings;
			DisableAllMacros = ObfuscationSettings.DisableAllMacros;
			EnhancedOutput = ObfuscationSettings.EnhancedOutput;
			EnhancedConstantEncryption = ObfuscationSettings.EnhancedConstantEncryption;
			CompressedOutput = ObfuscationSettings.CompressedOutput;
			PremiumFormat = ObfuscationSettings.PremiumFormat;
		}
	}
}
