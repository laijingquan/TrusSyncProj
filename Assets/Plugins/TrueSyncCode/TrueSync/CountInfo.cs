using System;

namespace TrueSync
{
	public class CountInfo
	{
		public long sum;

		public long count;

		public float average()
		{
			float result;
			if (count == 0L)
			{
				result = 0f;
			}
			else
			{
				result = (float)this.sum / (float)this.count;
			}
			return result;
		}

		public string averageFormatted()
		{
			return this.average().ToString("F2");//ToString("F2")固定输出小数点后两位
		}
	}
}
