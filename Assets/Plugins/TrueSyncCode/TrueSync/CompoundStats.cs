using System;

namespace TrueSync
{
	public class CompoundStats
	{
		private const float BUFFER_LIFETIME = 2f;

		private const int BUFFER_WINDOW = 10;

		public Stats globalStats;

		public GenericBufferWindow<Stats> bufferStats;

		private float timerAcc;

		public CompoundStats()
		{
			bufferStats = new GenericBufferWindow<Stats>(10);
			globalStats = new Stats();
			timerAcc = 0f;
		}

		public void UpdateTime(float time)
		{
			timerAcc += time;
			if (timerAcc >= 2f)
			{
				bufferStats.MoveNext();
				bufferStats.Current().Clear();
				timerAcc = 0f;
			}
		}

		public void AddValue(string key, long value)
		{
			bufferStats.Current().AddValue(key, value);
			globalStats.AddValue(key, value);
		}

		public void Increment(string key)
		{
			bufferStats.Current().Increment(key);
			globalStats.Increment(key);
		}
	}
}
