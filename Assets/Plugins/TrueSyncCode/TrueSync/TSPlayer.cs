using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrueSync
{
	[Serializable]
	public class TSPlayer
	{
		[SerializeField]
		public TSPlayerInfo playerInfo;

		[NonSerialized]
		public int dropCount;

		[NonSerialized]
		public bool dropped;

		[NonSerialized]
		public bool sentSyncedStart;

		[SerializeField]
		internal SerializableDictionaryIntSyncedData controls;

		private int lastTick;

		public byte ID
		{
			get
			{
				return this.playerInfo.id;
			}
		}

		internal TSPlayer(byte id, string name)
		{
			this.playerInfo = new TSPlayerInfo(id, name);
			this.dropCount = 0;
			this.dropped = false;
			this.controls = new SerializableDictionaryIntSyncedData();
		}

		public bool IsDataReady(int tick)
		{
			return this.controls.ContainsKey(tick) && !this.controls[tick].fake;
		}

		public bool IsDataDirty(int tick)
		{
			return controls.ContainsKey(tick) && controls[tick].dirty;
		}

		public SyncedData GetData(int tick)
		{
			SyncedData result;
			if (!controls.ContainsKey(tick))
			{
				SyncedData syncedData;
				if (controls.ContainsKey(tick - 1))
				{
					syncedData = this.controls[tick - 1].clone();
					syncedData.tick = tick;
				}
				else
				{
					syncedData = SyncedData.pool.GetNew();
					syncedData.Init(this.ID, tick);
				}
				syncedData.fake = true;
				this.controls[tick] = syncedData;
				result = syncedData;
			}
			else
			{
				result = this.controls[tick];
			}
			return result;
		}

		public void AddData(SyncedData data)
		{
			int tick = data.tick;
			if (controls.ContainsKey(tick))
			{
				SyncedData.pool.GiveBack(data);
			}
			else
			{
				controls[tick] = data;
				lastTick = tick;
			}
		}

		public void AddData(List<SyncedData> data)
		{
			for (int i = 0; i < data.Count; i++)
			{
				AddData(data[i]);
			}
		}

		public void RemoveData(int refTick)
		{
			if (controls.ContainsKey(refTick))
			{
				SyncedData.pool.GiveBack(this.controls[refTick]);
				controls.Remove(refTick);
			}
		}

		public void AddDataProjected(int refTick, int window)
		{
			SyncedData syncedData = GetData(refTick);
			for (int i = 1; i <= window; i++)
			{
				SyncedData data = GetData(refTick + i);
				bool fake = data.fake;
				if (fake)
				{
					SyncedData syncedData2 = syncedData.clone();
					syncedData2.fake = true;
					syncedData2.tick = refTick + i;
					if (controls.ContainsKey(syncedData2.tick))
					{
						SyncedData.pool.GiveBack(controls[syncedData2.tick]);
					}
					controls[syncedData2.tick] = syncedData2;
				}
				else
				{
					bool dirty = data.dirty;
					if (dirty)
					{
						data.dirty = false;
						syncedData = data;
					}
				}
			}
		}

		public void AddDataRollback(List<SyncedData> data)
		{
			for (int i = 0; i < data.Count; i++)
			{
				SyncedData data2 = this.GetData(data[i].tick);
				bool fake = data2.fake;
				if (fake)
				{
					if (!data2.EqualsData(data[i]))
					{
						data[i].dirty = true;
						SyncedData.pool.GiveBack(controls[data[i].tick]);
						controls[data[i].tick] = data[i];
						break;
					}
					data2.fake = false;
					data2.dirty = false;
				}
				SyncedData.pool.GiveBack(data[i]);
			}
		}

		public bool GetSendDataForDrop(byte fromPlayerId, SyncedData[] sendWindowArray)
		{
			bool result;
			if (controls.Count == 0)
			{
				result = false;
			}
			else
			{
				GetDataFromTick(lastTick, sendWindowArray);
				sendWindowArray[0] = sendWindowArray[0].clone();
				sendWindowArray[0].dropFromPlayerId = fromPlayerId;
				sendWindowArray[0].dropPlayer = true;
				result = true;
			}
			return result;
		}

		public void GetSendData(int tick, SyncedData[] sendWindowArray)
		{
			GetDataFromTick(tick, sendWindowArray);
		}

		private void GetDataFromTick(int tick, SyncedData[] sendWindowArray)
		{
			for (int i = 0; i < sendWindowArray.Length; i++)
			{
				sendWindowArray[i] = GetData(tick - i);
			}
		}
	}
}
