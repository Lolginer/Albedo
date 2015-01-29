using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Albedo.Core
{
	public class Maths
	{
		public static double Clamp(int toClamp, int min = 0, int max = 1)
		{
			int step1 = Math.Min(toClamp, max);
			int step2 = Math.Max(step1, min);
			return (double)step2;
		}

		public static int Lerp(int value1, int value2, double percentage)
		{
			double step1 = (double)(value2 - value1);
			double step2 = (step1 * percentage);
			return value1 + (int)step2;
		}
	}
}
