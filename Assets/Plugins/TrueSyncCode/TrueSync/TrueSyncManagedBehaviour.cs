using System;
using System.Collections.Generic;

namespace TrueSync
{
	public class TrueSyncManagedBehaviour : ITrueSyncBehaviourGamePlay, ITrueSyncBehaviour, ITrueSyncBehaviourCallbacks
	{
		public ITrueSyncBehaviour trueSyncBehavior;

		[AddTracking]
		public bool disabled;

		public TSPlayerInfo localOwner;

		public TSPlayerInfo owner;

		public TrueSyncManagedBehaviour(ITrueSyncBehaviour trueSyncBehavior)
		{
			StateTracker.AddTracking(this);
			StateTracker.AddTracking(trueSyncBehavior);
			this.trueSyncBehavior = trueSyncBehavior;
		}

		public void OnPreSyncedUpdate()
		{
			if (trueSyncBehavior is ITrueSyncBehaviourGamePlay)
			{
				((ITrueSyncBehaviourGamePlay)trueSyncBehavior).OnPreSyncedUpdate();
			}
		}

		public void OnSyncedInput()
		{
			if (trueSyncBehavior is ITrueSyncBehaviourGamePlay)
			{
				((ITrueSyncBehaviourGamePlay)trueSyncBehavior).OnSyncedInput();
			}
		}

		public void OnSyncedUpdate()
		{
			if (trueSyncBehavior is ITrueSyncBehaviourGamePlay)
			{
				((ITrueSyncBehaviourGamePlay)trueSyncBehavior).OnSyncedUpdate();
			}
		}

		public void SetGameInfo(TSPlayerInfo localOwner, int numberOfPlayers)
		{
			trueSyncBehavior.SetGameInfo(localOwner, numberOfPlayers);
		}

		public void OnSyncedStart()
		{
			if (trueSyncBehavior is ITrueSyncBehaviourCallbacks)
			{
				((ITrueSyncBehaviourCallbacks)trueSyncBehavior).OnSyncedStart();
				if (localOwner.Id == owner.Id)
				{
					((ITrueSyncBehaviourCallbacks)trueSyncBehavior).OnSyncedStartLocalPlayer();
				}
			}
		}

		public void OnGamePaused()
		{
			if (trueSyncBehavior is ITrueSyncBehaviourCallbacks)
			{
				((ITrueSyncBehaviourCallbacks)this.trueSyncBehavior).OnGamePaused();
			}
		}

		public void OnGameUnPaused()
		{
			if (trueSyncBehavior is ITrueSyncBehaviourCallbacks)
			{
				((ITrueSyncBehaviourCallbacks)trueSyncBehavior).OnGameUnPaused();
			}
		}

		public void OnGameEnded()
		{
			if (trueSyncBehavior is ITrueSyncBehaviourCallbacks)
			{
				((ITrueSyncBehaviourCallbacks)trueSyncBehavior).OnGameEnded();
			}
		}

		public void OnPlayerDisconnection(int playerId)
		{
			if (trueSyncBehavior is ITrueSyncBehaviourCallbacks)
			{
				((ITrueSyncBehaviourCallbacks)trueSyncBehavior).OnPlayerDisconnection(playerId);
			}
		}

		public void OnSyncedStartLocalPlayer()
		{
			throw new NotImplementedException();
		}

		public static void OnPlayerDisconnection(List<TrueSyncManagedBehaviour> generalBehaviours, Dictionary<byte, List<TrueSyncManagedBehaviour>> behaviorsByPlayer, byte playerId)
		{
			int i = 0;
			int count = generalBehaviours.Count;
			while (i < count)
			{
				generalBehaviours[i].OnPlayerDisconnection((int)playerId);
				i++;
			}
			Dictionary<byte, List<TrueSyncManagedBehaviour>>.Enumerator enumerator = behaviorsByPlayer.GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<byte, List<TrueSyncManagedBehaviour>> current = enumerator.Current;
				List<TrueSyncManagedBehaviour> value = current.Value;
				int j = 0;
				int count2 = value.Count;
				while (j < count2)
				{
					value[j].OnPlayerDisconnection((int)playerId);
					j++;
				}
			}
		}

		public static void OnGameStarted(List<TrueSyncManagedBehaviour> generalBehaviours, Dictionary<byte, List<TrueSyncManagedBehaviour>> behaviorsByPlayer)
		{
			int i = 0;
			int count = generalBehaviours.Count;
			while (i < count)
			{
				generalBehaviours[i].OnSyncedStart();
				i++;
			}
			Dictionary<byte, List<TrueSyncManagedBehaviour>>.Enumerator enumerator = behaviorsByPlayer.GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<byte, List<TrueSyncManagedBehaviour>> current = enumerator.Current;
				List<TrueSyncManagedBehaviour> value = current.Value;
				int j = 0;
				int count2 = value.Count;
				while (j < count2)
				{
					value[j].OnSyncedStart();
					j++;
				}
			}
		}

		public static void OnGamePaused(List<TrueSyncManagedBehaviour> generalBehaviours, Dictionary<byte, List<TrueSyncManagedBehaviour>> behaviorsByPlayer)
		{
			int i = 0;
			int count = generalBehaviours.Count;
			while (i < count)
			{
				generalBehaviours[i].OnGamePaused();
				i++;
			}
			Dictionary<byte, List<TrueSyncManagedBehaviour>>.Enumerator enumerator = behaviorsByPlayer.GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<byte, List<TrueSyncManagedBehaviour>> current = enumerator.Current;
				List<TrueSyncManagedBehaviour> value = current.Value;
				int j = 0;
				int count2 = value.Count;
				while (j < count2)
				{
					value[j].OnGamePaused();
					j++;
				}
			}
		}

		public static void OnGameUnPaused(List<TrueSyncManagedBehaviour> generalBehaviours, Dictionary<byte, List<TrueSyncManagedBehaviour>> behaviorsByPlayer)
		{
			int i = 0;
			int count = generalBehaviours.Count;
			while (i < count)
			{
				generalBehaviours[i].OnGameUnPaused();
				i++;
			}
			Dictionary<byte, List<TrueSyncManagedBehaviour>>.Enumerator enumerator = behaviorsByPlayer.GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<byte, List<TrueSyncManagedBehaviour>> current = enumerator.Current;
				List<TrueSyncManagedBehaviour> value = current.Value;
				int j = 0;
				int count2 = value.Count;
				while (j < count2)
				{
					value[j].OnGameUnPaused();
					j++;
				}
			}
		}

		public static void OnGameEnded(List<TrueSyncManagedBehaviour> generalBehaviours, Dictionary<byte, List<TrueSyncManagedBehaviour>> behaviorsByPlayer)
		{
			int i = 0;
			int count = generalBehaviours.Count;
			while (i < count)
			{
				generalBehaviours[i].OnGameEnded();
				i++;
			}
			Dictionary<byte, List<TrueSyncManagedBehaviour>>.Enumerator enumerator = behaviorsByPlayer.GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<byte, List<TrueSyncManagedBehaviour>> current = enumerator.Current;
				List<TrueSyncManagedBehaviour> value = current.Value;
				int j = 0;
				int count2 = value.Count;
				while (j < count2)
				{
					value[j].OnGameEnded();
					j++;
				}
			}
		}
	}
}
