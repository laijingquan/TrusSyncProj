using System;
using System.Collections.Generic;

namespace TrueSync
{
	public class Stats
	{
		private Dictionary<string, CountInfo> counts = new Dictionary<string, CountInfo>();

		private static CountInfo emptyInfo = new CountInfo();

		public void Clear()
		{
			this.counts.Clear();
		}

		public void Increment(string key)
		{
			if (!counts.ContainsKey(key))
			{
				counts[key] = new CountInfo();
			}
			counts[key].count += 1L;
		}

		public void AddValue(string key, long value)
		{
			Increment(key);
			counts[key].sum += value;
		}

		public CountInfo GetInfo(string key)
		{
			CountInfo result;
			if (counts.ContainsKey(key))
			{
				result = counts[key];
			}
			else
			{
				result = emptyInfo;
			}
			return result;
		}
	}
}
