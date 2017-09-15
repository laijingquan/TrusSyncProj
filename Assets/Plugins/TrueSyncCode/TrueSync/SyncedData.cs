using System;
using System.Collections.Generic;

namespace TrueSync
{
	[Serializable]
	public class SyncedData : ResourcePoolItem//ResourcePoolItem接口实现cleanup方法
	{
		internal static ResourcePoolSyncedData pool = new ResourcePoolSyncedData();//SyncedData

		internal static ResourcePoolListSyncedData poolList = new ResourcePoolListSyncedData();//List<SyncedData>

		public InputDataBase inputData;//操作输入，里面用字典的方式输入数据，还包括了序列化和反序列化

		public int tick;

		[NonSerialized]
		public bool fake;

		[NonSerialized]
		public bool dirty;

		[NonSerialized]
		public bool dropPlayer;//回退玩家？

		[NonSerialized]
		public byte dropFromPlayerId;

		private static List<byte> bytesToEncode = new List<byte>();

		public SyncedData()
		{
			this.inputData = AbstractLockstep.instance.InputDataProvider();
		}
        /// <summary>
        /// 初始化输入数据属于哪个玩家,和是哪个tick时间的数据
        /// </summary>
        /// <param name="ownerID"></param>
        /// <param name="tick"></param>
		public void Init(byte ownerID, int tick)
		{
			this.inputData.ownerID = ownerID;
			this.tick = tick;
			this.fake = false;
			this.dirty = false;
		}

        /// <summary>
        /// 序列化头部： 将tick ownerID dropFormPlayerId dropPlayer加入到List<Byte>
        /// </summary>
        /// <param name="bytes"></param>
		public void GetEncodedHeader(List<byte> bytes)
		{
			Utils.GetBytes(tick, bytes);
			bytes.Add(inputData.ownerID);
			bytes.Add(dropFromPlayerId);
			bytes.Add((byte)(dropPlayer ? 1 : 0));
		}

        /// <summary>
        /// 序列化 “操作”（其实就是序列化字典）
        /// </summary>
        /// <param name="bytes"></param>
		public void GetEncodedActions(List<byte> bytes)
		{
			this.inputData.Serialize(bytes);
		}

		public static List<SyncedData> Decode(byte[] data)
		{
			List<SyncedData> @new = poolList.GetNew();
			@new.Clear();
			int i = 0;
			int num = BitConverter.ToInt32(data, i);
			i += 4;
			byte ownerID = data[i++];
			byte b = data[i++];
			bool flag = data[i++] == 1;
			int num2 = num;
			while (i < data.Length)
			{
				SyncedData new2 = SyncedData.pool.GetNew();
				new2.Init(ownerID, num2--);
				new2.inputData.Deserialize(data, ref i);
				@new.Add(new2);
			}
			bool flag2 = @new.Count > 0;
			if (flag2)
			{
				@new[0].dropPlayer = flag;
				@new[0].dropFromPlayerId = b;
			}
			return @new;
		}

        /// <summary>
        /// 序列化一个tick里面多个操作
        /// </summary>
        /// <param name="syncedData"></param>
        /// <returns></returns>
		public static byte[] Encode(SyncedData[] syncedData)
		{
			SyncedData.bytesToEncode.Clear();
			if (syncedData.Length != 0)
			{
				syncedData[0].GetEncodedHeader(SyncedData.bytesToEncode);//tick 玩家id
				for (int i = 0; i < syncedData.Length; i++)
				{
					syncedData[i].GetEncodedActions(SyncedData.bytesToEncode);//该tick下的所有操作
				}
			}
            //分配一个新的字节数组array返回
			byte[] array = new byte[SyncedData.bytesToEncode.Count];
			int j = 0;
			int num = array.Length;
			while (j < num)
			{
				array[j] = SyncedData.bytesToEncode[j];
				j++;
			}
			return array;
		}

		public SyncedData clone()
		{
			SyncedData @new = SyncedData.pool.GetNew();
			@new.Init(this.inputData.ownerID, this.tick);
			@new.inputData.CopyFrom(this.inputData);
			return @new;
		}

		public bool EqualsData(SyncedData other)
		{
			return this.inputData.EqualsData(other.inputData);
		}

		public void CleanUp()
		{
			this.inputData.CleanUp();
		}
	}
}
