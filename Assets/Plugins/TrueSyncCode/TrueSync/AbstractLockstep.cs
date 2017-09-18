using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrueSync
{
	public abstract class AbstractLockstep
	{
		private enum SimulationState
		{
			NOT_STARTED,
			WAITING_PLAYERS,
			RUNNING,
			PAUSED,
			ENDED
		}

		private const int INITIAL_PLAYERS_CAPACITY = 4;

		private const byte SYNCED_GAME_START_CODE = 196;

		private const byte SIMULATION_CODE = 197;

		private const byte CHECKSUM_CODE = 198;

		private const byte SEND_CODE = 199;

		private const byte SIMULATION_EVENT_PAUSE = 0;

		private const byte SIMULATION_EVENT_RUN = 1;

		private const byte SIMULATION_EVENT_END = 3;

		private const int MAX_PANIC_BEFORE_END_GAME = 5;

		private const int SYNCED_INFO_BUFFER_WINDOW = 3;

        /// <summary>
        /// 游戏中所有玩家List
        /// </summary>
		internal Dictionary<byte, TSPlayer> players;

        /// <summary>
        /// 游戏中所有玩家Dict
        /// </summary>
		internal List<TSPlayer> activePlayers;

		internal List<SyncedData> auxPlayersSyncedData;

		internal List<InputDataBase> auxPlayersInputData;

		internal int[] auxActivePlayersIds;

		internal TSPlayer localPlayer;

		protected TrueSyncUpdateCallback StepUpdate;

		private TrueSyncInputCallback GetLocalData;

		internal TrueSyncInputDataProvider InputDataProvider;

		private TrueSyncEventCallback OnGameStarted;

		private TrueSyncEventCallback OnGamePaused;

		private TrueSyncEventCallback OnGameUnPaused;

		private TrueSyncEventCallback OnGameEnded;

		private TrueSyncPlayerDisconnectionCallback OnPlayerDisconnection;

		public TrueSyncIsReady GameIsReady;

		protected int ticks;
        /// <summary>
        /// maximum amount of missed frames/ticks before a remote player is removed from simulation due to being unresponsive(not sending input values anymore)
        /// 就是超过panicWindow还没有收到远程玩家的input那么久将之移除（他太卡了）
        /// </summary>
		private int panicWindow;

        /// <summary>
        /// this is the size of the input queue;就是缓存输入数据的窗口大小
        /// Lets say a game client has a ping (round trip time) of 60ms to Photon Cloud servers, and we're using the default locked step time of 0.02 (20ms time per frame).

        //In a lockstep game, we need the input queue to compensate that lag from the remote players by adding an equally big latency to local input.

        //This means that the sync window size shall be at least 3, so we add 60ms(3 frames X 20ms per frame) to all input
        //就是本地的输入等待远程的输入，为了更好的同步
        /// </summary>
		protected int syncWindow;

		private int elapsedPanicTicks;

		private AbstractLockstep.SimulationState simulationState;
        /// <summary>
        /// this is the maximum amount of frames that TrueSync is allowed to advance the simulation with predicted input values;//允许优先预测玩家输入的窗口大小
        /// </summary>
		internal int rollbackWindow;

		internal ICommunicator communicator;

		protected IPhysicsManagerBase physicsManager;

		private GenericBufferWindow<SyncedInfo> bufferSyncedInfo;

		protected int totalWindow;

		public bool checksumOk;

		public CompoundStats compoundStats;

		public float deltaTime;

		public int _lastSafeTick = 0;

		protected Dictionary<int, List<IBody>> bodiesToDestroy;

		protected Dictionary<int, List<byte>> playersDisconnect;

		private ReplayMode replayMode;

		private ReplayRecord replayRecord;

		internal static AbstractLockstep instance;

		private List<int> playersIdsAux = new List<int>();

		private SyncedData[] _syncedDataCacheDrop = new SyncedData[1];

		private SyncedData[] _syncedDataCacheUpdateData = new SyncedData[1];

		public List<TSPlayer> ActivePlayers
		{
			get
			{
				return this.activePlayers;
			}
		}

		public IDictionary<byte, TSPlayer> Players
		{
			get
			{
				return this.players;
			}
		}

		public TSPlayer LocalPlayer
		{
			get
			{
				return this.localPlayer;
			}
		}

		public int Ticks
		{
			get
			{
				return this.GetSimulatedTick(this.GetSyncedDataTick()) - 1;
			}
		}

		public int LastSafeTick
		{
			get
			{
				//bool flag = this._lastSafeTick < 0;
				int result;
				if (this._lastSafeTick < 0)
				{
					result = -1;
				}
				else
				{
					result = this._lastSafeTick - 1;
				}
				return result;
			}
		}

		private ReplayMode ReplayMode
		{
			set
			{
				replayMode = value;
				if (replayMode == ReplayMode.RECORD_REPLAY)
				{
					replayRecord = new ReplayRecord();
				}
			}
		}

		public ReplayRecord ReplayRecord
		{
			set
			{
				replayRecord = value;
				if (replayRecord != null)
				{
					replayMode = ReplayMode.LOAD_REPLAY;
					replayRecord.ApplyRecord(this);
				}
			}
		}

		public static AbstractLockstep NewInstance(float deltaTime, ICommunicator communicator, IPhysicsManagerBase physicsManager, int syncWindow, int panicWindow, int rollbackWindow, TrueSyncEventCallback OnGameStarted, TrueSyncEventCallback OnGamePaused, TrueSyncEventCallback OnGameUnPaused, TrueSyncEventCallback OnGameEnded, TrueSyncPlayerDisconnectionCallback OnPlayerDisconnection, TrueSyncUpdateCallback OnStepUpdate, TrueSyncInputCallback GetLocalData, TrueSyncInputDataProvider InputDataProvider)
		{
			bool flag = rollbackWindow <= 0 || communicator == null;
			AbstractLockstep result;
			if (flag)
			{
				result = new DefaultLockstep(deltaTime, communicator, physicsManager, syncWindow, panicWindow, rollbackWindow, OnGameStarted, OnGamePaused, OnGameUnPaused, OnGameEnded, OnPlayerDisconnection, OnStepUpdate, GetLocalData, InputDataProvider);
			}
			else
			{
				result = new RollbackLockstep(deltaTime, communicator, physicsManager, syncWindow, panicWindow, rollbackWindow, OnGameStarted, OnGamePaused, OnGameUnPaused, OnGameEnded, OnPlayerDisconnection, OnStepUpdate, GetLocalData, InputDataProvider);
			}
			return result;
		}

		public AbstractLockstep(float deltaTime, ICommunicator communicator, IPhysicsManagerBase physicsManager, int syncWindow, int panicWindow, int rollbackWindow, TrueSyncEventCallback OnGameStarted, TrueSyncEventCallback OnGamePaused, TrueSyncEventCallback OnGameUnPaused, TrueSyncEventCallback OnGameEnded, TrueSyncPlayerDisconnectionCallback OnPlayerDisconnection, TrueSyncUpdateCallback OnStepUpdate, TrueSyncInputCallback GetLocalData, TrueSyncInputDataProvider InputDataProvider)
		{
			AbstractLockstep.instance = this;
			this.deltaTime = deltaTime;
			this.syncWindow = syncWindow;
			this.panicWindow = panicWindow;
			this.rollbackWindow = rollbackWindow;
			this.totalWindow = syncWindow + rollbackWindow;
			this.StepUpdate = OnStepUpdate;
			this.OnGameStarted = OnGameStarted;
			this.OnGamePaused = OnGamePaused;
			this.OnGameUnPaused = OnGameUnPaused;
			this.OnGameEnded = OnGameEnded;
			this.OnPlayerDisconnection = OnPlayerDisconnection;
			this.GetLocalData = GetLocalData;
			this.InputDataProvider = InputDataProvider;
			this.ticks = 0;
			this.players = new Dictionary<byte, TSPlayer>(4);
			this.activePlayers = new List<TSPlayer>(4);
			this.auxPlayersSyncedData = new List<SyncedData>(4);
			this.auxPlayersInputData = new List<InputDataBase>(4);
			this.communicator = communicator;
			bool flag = communicator != null;
			if (flag)
			{
				this.communicator.AddEventListener(new OnEventReceived(this.OnEventDataReceived));
			}
			this.physicsManager = physicsManager;
			this.compoundStats = new CompoundStats();
			this.bufferSyncedInfo = new GenericBufferWindow<SyncedInfo>(3);
			this.checksumOk = true;
			this.simulationState = AbstractLockstep.SimulationState.NOT_STARTED;
			this.bodiesToDestroy = new Dictionary<int, List<IBody>>();
			this.playersDisconnect = new Dictionary<int, List<byte>>();
			this.ReplayMode = ReplayRecord.replayMode;
		}

		protected int GetSyncedDataTick()
		{
			return this.ticks - this.syncWindow;
		}

		protected abstract int GetRefTick(int syncedDataTick);

		protected virtual void BeforeStepUpdate(int syncedDataTick, int referenceTick)
		{
            Debug.Log("BeforeStepUpdate");
		}

		protected virtual void AfterStepUpdate(int syncedDataTick, int referenceTick)
		{
            Debug.Log("AfterStepUpdate");
            int i = 0;
			int count = activePlayers.Count;
			while (i < count)
			{
				activePlayers[i].RemoveData(referenceTick);
				i++;
			}
		}

		protected abstract bool IsStepReady(int syncedDataTick);

		protected abstract void OnSyncedDataReceived(TSPlayer player, List<SyncedData> data);

		protected abstract string GetChecksumForSyncedInfo();

		protected abstract int GetSimulatedTick(int syncedDataTick);

		private void Run()
		{
            Debug.Log("Run");
            if (simulationState == SimulationState.NOT_STARTED)
			{
				simulationState = SimulationState.WAITING_PLAYERS;
			}
			else
			{
				if (simulationState == SimulationState.WAITING_PLAYERS || simulationState == SimulationState.PAUSED)
				{
					if (simulationState == SimulationState.WAITING_PLAYERS)
					{
						OnGameStarted();
					}
					else
					{
						OnGameUnPaused();
					}
					simulationState = SimulationState.RUNNING;
				}
			}
		}

		private void Pause()
		{
            Debug.Log("Pause");
            if (simulationState == SimulationState.RUNNING)
			{
				OnGamePaused();
				simulationState = SimulationState.PAUSED;
			}
		}

		private void End()
		{
            Debug.Log("End");
            if (simulationState != SimulationState.ENDED)
			{
				OnGameEnded();
				if (replayMode == ReplayMode.RECORD_REPLAY)
				{
					ReplayRecord.SaveRecord(replayRecord);
				}
				simulationState = SimulationState.ENDED;
			}
		}

		public void Update()
		{
            //当本地玩家已经处于等待其他玩家的状态,那么久应该去检测其他玩家是否已经准备好了
			if (simulationState == SimulationState.WAITING_PLAYERS)
			{
				CheckGameStart();
			}
			else
			{
				if (simulationState == SimulationState.RUNNING)
				{
					compoundStats.UpdateTime(deltaTime);
					if (communicator != null)
					{
						compoundStats.AddValue("ping", communicator.RoundTripTime());
					}

					if (syncWindow == 0)
					{
						UpdateData();
					}
					int i = 0;
					int num = this.activePlayers.Count;
                    //对每个玩家进行检测掉线
					while (i < num)
					{
                        //是否掉线
						if (CheckDrop(this.activePlayers[i]))
						{
                            //如果是掉线,那么该玩家会从activePlayers列表中删除,为了能够正确的检测其他玩家,必须i--,num--
							i--;
							num--;
						}
						i++;
					}
					int syncedDataTick = GetSyncedDataTick();
                    //执行游戏仿真（或者叫响应玩家输入）
					if (CheckGameIsReady() && this.IsStepReady(syncedDataTick))
					{
						compoundStats.Increment("simulated_frames");//simulate_frames++;
						UpdateData();//收集本地输入，通过服务器发送给其他玩家
                        elapsedPanicTicks = 0;
						int refTick = GetRefTick(syncedDataTick);//对于defaultLookStep,直接返回syncedDataTick
                        //每100tick做一次刚体同步校验
                        if (refTick > 1 && refTick % 100 == 0)
						{
							SendInfoChecksum(refTick);
						}
						_lastSafeTick = refTick;
						BeforeStepUpdate(syncedDataTick, refTick);
						List<SyncedData> tickData = GetTickData(syncedDataTick);//获取所有玩家在该tick下的输入数据
                        ExecutePhysicsStep(tickData, syncedDataTick);//输入数据影响物理世界的输出(TrueSyncManager.OnStepUpdate|OnSyncedUpdate)
						if (replayMode == ReplayMode.RECORD_REPLAY)
						{
							replayRecord.AddSyncedData(GetTickData(refTick));
						}
						AfterStepUpdate(syncedDataTick, refTick);//从TSPlayer.controls清除该refTick的数据
                        ticks++;
					}
                    //不仿真，只收集数据之类的
					else
					{
						if (ticks >= this.totalWindow)
						{
							if (replayMode == ReplayMode.LOAD_REPLAY)
							{
								End();
							}
							else
							{
								compoundStats.Increment("missed_frames");//missed_frames++;
								elapsedPanicTicks++;
								if (elapsedPanicTicks > panicWindow)
								{
									compoundStats.Increment("panic");
                                    //超过五次出现了落后panicWindow:100没有数据传来 那么游戏结束
									if (compoundStats.globalStats.GetInfo("panic").count >= 5L)
									{
										End();
									}
                                    //出现一次没有数据 就要通知其他玩家一次
									else
									{
										elapsedPanicTicks = 0;
										DropLagPlayers();
									}
								}
							}
						}
						else
						{
							compoundStats.Increment("simulated_frames");//simulated_frames++;
							physicsManager.UpdateStep();
							UpdateData();
							ticks++;
						}
					}
				}
			}
		}

		private bool CheckGameIsReady()
		{
            Debug.Log("CheckGameIsReady");
            bool result;
			if (GameIsReady != null)
			{
				Delegate[] invocationList = GameIsReady.GetInvocationList();
				for (int i = 0; i < invocationList.Length; i++)
				{
					Delegate @delegate = invocationList[i];
					bool flag2 = (bool)@delegate.DynamicInvoke(new object[0]);
					bool flag3 = !flag2;
					if (flag3)
					{
						result = false;
						return result;
					}
				}
			}
			result = true;
			return result;
		}

		protected void ExecutePhysicsStep(List<SyncedData> data, int syncedDataTick)
		{
            //Debug.Log("ExecutePhysicsStep");
            ExecuteDelegates(syncedDataTick);
			SyncedArrayToInputArray(data);
			StepUpdate(auxPlayersInputData);
			physicsManager.UpdateStep();
		}

		private void ExecuteDelegates(int syncedDataTick)
		{
            Debug.Log("ExecuteDelegates");
            syncedDataTick++;
			if (playersDisconnect.ContainsKey(syncedDataTick))
			{
				List<byte> list = playersDisconnect[syncedDataTick];
				int i = 0;
				int count = list.Count;
				while (i < count)
				{
					OnPlayerDisconnection(list[i]);
					i++;
				}
			}
		}

		internal void UpdateActivePlayers()
		{
            Debug.Log("UpdateActivePlayers");
            playersIdsAux.Clear();
			int i = 0;
			int count = activePlayers.Count;
			while (i < count)
			{
				//bool flag = localPlayer == null || localPlayer.ID != activePlayers[i].ID;
                //过滤了本地玩家,那么playersIdsAus存的都是除本地玩家之外的所有玩家
				if (localPlayer == null || localPlayer.ID != activePlayers[i].ID)
				{
					playersIdsAux.Add((int)activePlayers[i].ID);//List
				}
				i++;
			}
			auxActivePlayersIds = playersIdsAux.ToArray();//转成int[]
		}

		private void CheckGameStart()
		{
            Debug.Log("CheckGameStart");
            if (replayMode == ReplayMode.LOAD_REPLAY)
			{
				RunSimulation(false);
			}
			else
			{
                //检测所有玩家是否已经准备好了
				bool flag = true;
				int i = 0;
				int count = activePlayers.Count;
				while (i < count)
				{
					flag &= activePlayers[i].sentSyncedStart;
					i++;
				}


                //所有玩家已经准备好了
				if (flag)
				{
					RunSimulation(false);
					SyncedData.pool.FillStack(activePlayers.Count * (syncWindow + rollbackWindow));
				}
                //其他玩家还没有准备好,发送196事件
				else
				{
					RaiseEvent(SYNCED_GAME_START_CODE, SyncedInfo.Encode(new SyncedInfo
					{
						playerId = this.localPlayer.ID
					}));
				}
			}
		}

		protected void SyncedArrayToInputArray(List<SyncedData> data)
		{
            Debug.Log("SyncedArrayToInputArray");
            auxPlayersInputData.Clear();
			int i = 0;
			int count = data.Count;
			while (i < count)
			{
				auxPlayersInputData.Add(data[i].inputData);
				i++;
			}
		}

		public void PauseSimulation()
		{
            Debug.Log("PauseSimulation");
            Pause();
			RaiseEvent(SIMULATION_CODE, new byte[1], true, auxActivePlayersIds);
		}

		public void RunSimulation(bool firstRun)
		{
            Debug.LogFormat("RunSimulation bool is {0}",firstRun);
            Run();
			//bool flag = !firstRun;
            //firstRun=true的时候 不给其他玩家发送消息,auxActivePlayersIds存的都是除了本地玩家之外的所有玩家id
			if (!firstRun)
			{
				RaiseEvent(SIMULATION_CODE, new byte[]
				{
					1
				}, true, auxActivePlayersIds);
			}
		}

		public void EndSimulation()
		{
            Debug.Log("EndSimulation");
            End();
			RaiseEvent(SIMULATION_CODE, new byte[]
			{
				3
			}, true, auxActivePlayersIds);
		}

		public void Destroy(IBody rigidBody)
		{
            Debug.Log("Destroy");
            rigidBody.TSDisabled = true;
			int key = GetSimulatedTick(GetSyncedDataTick()) + 1;
			if (!bodiesToDestroy.ContainsKey(key))
			{
				bodiesToDestroy[key] = new List<IBody>();
			}
			bodiesToDestroy[key].Add(rigidBody);
		}

		protected void CheckSafeRemotion(int refTick)
		{
            Debug.Log("CheckSafeRemotion");
            if (bodiesToDestroy.ContainsKey(refTick))
			{
				List<IBody> list = bodiesToDestroy[refTick];
				foreach (IBody current in list)
				{
					bool tSDisabled = current.TSDisabled;
					if (tSDisabled)
					{
						physicsManager.RemoveBody(current);
					}
				}
				bodiesToDestroy.Remove(refTick);
			}

			if (playersDisconnect.ContainsKey(refTick))
			{
				playersDisconnect.Remove(refTick);
			}
		}

        /// <summary>
        /// Lag:落后， 延迟
        /// </summary>
		private void DropLagPlayers()
		{
            Debug.Log("DropLagPlayers");
            List<TSPlayer> list = new List<TSPlayer>();
			int refTick = GetRefTick(GetSyncedDataTick());
			if (refTick >= 0)
			{
				int i = 0;
				int count = activePlayers.Count;
				while (i < count)
				{
					TSPlayer tSPlayer = activePlayers[i];
					if (!tSPlayer.IsDataReady(refTick))
					{
						tSPlayer.dropCount++;
						list.Add(tSPlayer);
					}
					i++;
				}
			}
			int j = 0;
			int count2 = list.Count;
			while (j < count2)
			{
				TSPlayer p = list[j];
				CheckDrop(p);
				bool sendDataForDrop = list[j].GetSendDataForDrop(localPlayer.ID, _syncedDataCacheDrop);
				if (sendDataForDrop)
				{
					communicator.OpRaiseEvent(SEND_CODE, SyncedData.Encode(_syncedDataCacheDrop), true, null);
					SyncedData.pool.GiveBack(_syncedDataCacheDrop[0]);
				}
				j++;
			}
		}

        /// <summary>
        /// 收集本地输入，通过服务器发送给其他玩家
        /// </summary>
        /// <returns></returns>
		private SyncedData UpdateData()
		{
            Debug.Log("UpdateData");
            SyncedData result;
			if (replayMode == ReplayMode.LOAD_REPLAY)
			{
				result = null;
			}
			else
			{
				SyncedData @new = SyncedData.pool.GetNew();
				@new.Init(localPlayer.ID, ticks);
				GetLocalData(@new.inputData); //调用OnSyncedInput();输入数据给到@new.inputData里面
                localPlayer.AddData(@new);//数据塞给TSPlayer的controls
				if (communicator != null)
				{
					localPlayer.GetSendData(ticks, _syncedDataCacheUpdateData);//从TSplayer的controls里取数据
					communicator.OpRaiseEvent(SEND_CODE, SyncedData.Encode(_syncedDataCacheUpdateData), true, auxActivePlayersIds);//将自己的输入数据发给其他玩家
				}
				result = @new;
			}
			return result;
		}

		public InputDataBase GetInputData(int playerId)
		{
            Debug.Log("GetInputData");
            return players[(byte)playerId].GetData(GetSyncedDataTick()).inputData;
		}
        /// <summary>
        /// 对刚体body的位置和旋转数据的一个同步校验
        /// </summary>
        /// <param name="tick"></param>
		private void SendInfoChecksum(int tick)
		{
            Debug.Log("SendInfoChecksum");
            if (replayMode != ReplayMode.LOAD_REPLAY)
			{
				SyncedInfo syncedInfo = bufferSyncedInfo.Current();
				syncedInfo.playerId = localPlayer.ID;
				syncedInfo.tick = tick;
				syncedInfo.checksum = GetChecksumForSyncedInfo();
				bufferSyncedInfo.MoveNext();
				RaiseEvent(CHECKSUM_CODE, SyncedInfo.Encode(syncedInfo));
			}
		}

		private void RaiseEvent(byte eventCode, object message)
		{
			this.RaiseEvent(eventCode, message, true, null);
		}

		private void RaiseEvent(byte eventCode, object message, bool reliable, int[] toPlayers)
		{
			if (communicator != null)
			{
				communicator.OpRaiseEvent(eventCode, message, reliable, toPlayers);
			}
		}

		private void OnEventDataReceived(byte eventCode, object content)
		{
            Debug.LogFormat("OnEventDataReceived eventCode is {0}",eventCode);
            if (eventCode == SEND_CODE)
            {
                byte[] data = content as byte[];
                List<SyncedData> list = SyncedData.Decode(data);//只有list[0]是携带玩家ownerID数据,剩余的list都是该玩家的数据
                if (list.Count > 0)
                {
                    TSPlayer tSPlayer = players[list[0].inputData.ownerID];
                    if (!tSPlayer.dropped)
                    {
                        OnSyncedDataReceived(tSPlayer, list);//调用TSPlayer.addData添加数据
                        //满足三个条件,dropCount才可以增加
                        //网络数据的dropPlayer必须为true,网络过来的玩家不是本地玩家，提取网络数据中的dropFromPlayerId，查找players,其对应的dropped为false
                        //这里有dropPlayer dropped dropFromPlayerId 需要理解区分
                        if (list[0].dropPlayer && tSPlayer.ID != localPlayer.ID && !players[list[0].dropFromPlayerId].dropped)
                        {
                            tSPlayer.dropCount++;
                        }
                    }
                    else
                    {
                        int i = 0;
                        int count = list.Count;
                        while (i < count)
                        {
                            SyncedData.pool.GiveBack(list[i]);
                            i++;
                        }
                    }
                    SyncedData.poolList.GiveBack(list);
                }
            }
            else
            {
                if (eventCode == CHECKSUM_CODE)
                {
                    byte[] infoBytes = content as byte[];
                    this.OnChecksumReceived(SyncedInfo.Decode(infoBytes));
                }
                else
                {
                    if (eventCode == SIMULATION_CODE)
                    {
                        byte[] array = content as byte[];
                        if (array.Length != 0)
                        {
                            if (array[0] == 0)
                            {
                                Pause();
                            }
                            else
                            {
                                if (array[0] == 1)
                                {
                                    Run();
                                }
                                else
                                {
                                    if (array[0] == 3)
                                    {
                                        End();
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (eventCode == SYNCED_GAME_START_CODE)
                        {
                            byte[] infoBytes2 = content as byte[];
                            SyncedInfo syncedInfo = SyncedInfo.Decode(infoBytes2);
                            players[syncedInfo.playerId].sentSyncedStart = true;
                        }
                    }
                }
            }
        }

		private void OnChecksumReceived(SyncedInfo syncedInfo)
		{
            Debug.Log("OnChecksumReceived");
			bool dropped = this.players[syncedInfo.playerId].dropped;
			if (!dropped)
			{
				checksumOk = true;
				SyncedInfo[] buffer = bufferSyncedInfo.buffer;
				for (int i = 0; i < buffer.Length; i++)
				{
					SyncedInfo syncedInfo2 = buffer[i];
					if (syncedInfo2.tick == syncedInfo.tick && syncedInfo2.checksum != syncedInfo.checksum)
					{
						checksumOk = false;
						break;
					}
				}
			}
		}

        /// <summary>
        /// 获取所有玩家在该tick下的输入数据
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
		protected List<SyncedData> GetTickData(int tick)
		{
            Debug.Log("GetTickData");
            auxPlayersSyncedData.Clear();
			int i = 0;
			int count = activePlayers.Count;
			while (i < count)
			{
				auxPlayersSyncedData.Add(activePlayers[i].GetData(tick));
				i++;
			}
			return auxPlayersSyncedData;
		}

		public void AddPlayer(byte playerId, string playerName, bool isLocal)
		{
            Debug.Log("AddPlayer");
            TSPlayer tSPlayer = new TSPlayer(playerId, playerName);
			players.Add(tSPlayer.ID, tSPlayer);
			activePlayers.Add(tSPlayer);
			if (isLocal)
			{
				localPlayer = tSPlayer;
				localPlayer.sentSyncedStart = true;
			}
			UpdateActivePlayers();
			if (replayMode == ReplayMode.RECORD_REPLAY)
			{
				replayRecord.AddPlayer(tSPlayer);
			}
		}

        /// <summary>
        /// 检查是否有掉线玩家
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
		private bool CheckDrop(TSPlayer p)
		{
            Debug.Log("CheckDrop");
            bool result;
			if (p != localPlayer && !p.dropped && p.dropCount > 0)
			{
				int num = activePlayers.Count - 1;
                //如果dropCount>=玩家数量,那么就认为该玩家掉线了,
				if (p.dropCount >= num)
				{
					compoundStats.globalStats.GetInfo("panic").count = 0L;
					p.dropped = true;
					activePlayers.Remove(p);
					UpdateActivePlayers();
					Debug.Log("Player dropped (stopped sending input)");
					int key = GetSyncedDataTick() + 1;//下一帧为掉线玩家
					if (!playersDisconnect.ContainsKey(key))
					{
						playersDisconnect[key] = new List<byte>();
					}
					playersDisconnect[key].Add(p.ID);
					result = true;
					return result;
				}
			}
			result = false;
			return result;
		}
	}
}
