using System;
using System.Collections.Generic;
using System.Text;

namespace TrueSync
{
	public class SyncedInfo
	{
		private const int CHECKSUM_LENGTH = 32;

		public byte playerId;

		public int tick;
        //对刚体body的位置和旋转数据的一个同步校验
		public string checksum;

		public SyncedInfo()
		{
		}

		public SyncedInfo(byte playerId, int tick, string checksum)
		{
			this.tick = tick;
			this.checksum = checksum;
		}

		public static byte[] Encode(SyncedInfo info)
		{
			List<byte> list = new List<byte>();
			list.Add(info.playerId);
			if (info.checksum != null)
			{
				list.AddRange(BitConverter.GetBytes(info.tick));
				list.AddRange(Encoding.ASCII.GetBytes(info.checksum));
			}
			return list.ToArray();
		}

		public static SyncedInfo Decode(byte[] infoBytes)
		{
			SyncedInfo syncedInfo = new SyncedInfo();
			int num = 0;
			syncedInfo.playerId = infoBytes[num++];
			if (num < infoBytes.Length)
			{
				syncedInfo.tick = BitConverter.ToInt32(infoBytes, num);
				num += 4;
				syncedInfo.checksum = Encoding.ASCII.GetString(infoBytes, num, 32);
			}
			return syncedInfo;
		}
	}
}
