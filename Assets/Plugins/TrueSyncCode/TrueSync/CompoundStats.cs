using System;

namespace TrueSync
{
    /// <summary>
    /// compoundStats:混合状态
    /// 该类是收集各种状态要显示在界面上的数据类
    /// Stats:单个状态
    /// </summary>
	public class CompoundStats
	{
		private const float BUFFER_LIFETIME = 2f;

		private const int BUFFER_WINDOW = 10;

		public Stats globalStats;

		public GenericBufferWindow<Stats> bufferStats;

		private float timerAcc;

		public CompoundStats()
		{
			bufferStats = new GenericBufferWindow<Stats>(BUFFER_WINDOW);
			globalStats = new Stats();
			timerAcc = 0f;
		}
        /// <summary>
        /// 相当于俩秒更新一次界面状态,在俩秒时间内,都添加到bufferStats.Current()中
        /// </summary>
        /// <param name="time"></param>
		public void UpdateTime(float time)
		{
			timerAcc += time;
			if (timerAcc >= BUFFER_LIFETIME)
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
