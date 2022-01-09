using System;
using System.Collections.Generic;
using System.Text;

namespace Obfuscator.Obfuscation.Generation
{
	public static class Compression
	{
		public static Encoding LuaEncoding = Encoding.GetEncoding(28591);

		public const string UpperCaseBase36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

		public const string LowerCaseBase36 = "0123456789abcdefghijklmnopqrstuvwxyz";

		public static string ToBase36(ulong Value)
		{
			Random Random = new Random();
			StringBuilder StringBuilder = new StringBuilder(13);
			do
			{
				if (Random.Next(0, 2) == 0)
				{
					StringBuilder.Insert(0, "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[(byte)(Value % 36uL)]);
				}
				else
				{
					StringBuilder.Insert(0, "0123456789abcdefghijklmnopqrstuvwxyz"[(byte)(Value % 36uL)]);
				}
				Value /= 36uL;
			}
			while (Value != 0);
			return StringBuilder.ToString();
		}

		public static string CompressedToString(List<int> Compressed, ObfuscationSettings ObfuscationSettings)
		{
			StringBuilder StringBuilder = new StringBuilder();
			foreach (int Integer in Compressed)
			{
				string String = ToBase36((ulong)Integer);
				String = ToBase36((ulong)String.Length) + String;
				byte[] Bytes = LuaEncoding.GetBytes(String);
				String = "";
				for (int I = 0; I < Bytes.Length; I++)
				{
					String += LuaEncoding.GetString(new byte[1] { Bytes[I] });
				}
				StringBuilder.Append(String);
			}
			return StringBuilder.ToString();
		}

		public static List<int> Compress(byte[] Bytes)
		{
			Dictionary<string, int> Dictionary = new Dictionary<string, int>();
			for (int Integer = 0; Integer < 256; Integer++)
			{
				Dictionary.Add(((char)Integer).ToString(), Integer);
			}
			string String = string.Empty;
			List<int> Compressed = new List<int>();
			foreach (byte Byte in Bytes)
			{
				string text = String;
				char c = (char)Byte;
				string W = text + c;
				if (Dictionary.ContainsKey(W))
				{
					String = W;
					continue;
				}
				Compressed.Add(Dictionary[String]);
				Dictionary.Add(W, Dictionary.Count);
				c = (char)Byte;
				String = c.ToString();
			}
			if (!string.IsNullOrEmpty(String))
			{
				Compressed.Add(Dictionary[String]);
			}
			return Compressed;
		}
	}
}
