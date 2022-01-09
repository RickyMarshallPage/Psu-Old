using System;
using System.Collections.Generic;

namespace Obfuscator.Obfuscation.Generation
{
	public class NumberEquation
	{
		public List<int> Steps = new List<int>();

		public List<long> Values = new List<long>();

		private Random Random = new Random();

		public NumberEquation(int StepCount)
		{
			for (int I = 0; I < StepCount; I++)
			{
				int Step = Random.Next(0, 2);
				Steps.Add(Step);
				switch (Step)
				{
				case 0:
					Values.Add(Random.Next(0, 1000000));
					break;
				case 1:
					Values.Add(Random.Next(0, 1000000));
					break;
				}
			}
		}

		public long ComputeExpression(long Value)
		{
			int Index = 0;
			using (List<int>.Enumerator enumerator = Steps.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					switch (enumerator.Current)
					{
					case 0:
						Value ^= Values[Index];
						break;
					case 1:
						Value += Values[Index];
						break;
					}
					Index++;
				}
			}
			return Value;
		}

		public string WriteStatement()
		{
			string WrittenExpression = "Value";
			int Index = Steps.Count - 1;
			for (int I = Steps.Count - 1; I >= 0; I--)
			{
				switch (Steps[I])
				{
				case 0:
					WrittenExpression = $"BitXOR({WrittenExpression}, {Values[Index]})";
					break;
				case 1:
					WrittenExpression = $"({WrittenExpression}) - {Values[Index]}";
					break;
				}
				Index--;
			}
			return WrittenExpression;
		}
	}
}
