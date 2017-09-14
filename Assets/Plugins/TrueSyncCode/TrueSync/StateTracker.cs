using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TrueSync
{
	public class StateTracker
	{
		internal class State
		{
			private TrackedInfo trackedInfo;

			private object value;

			private Array _auxReferenceArray;

			public void SetInfo(TrackedInfo trackedInfo)
			{
				this.trackedInfo = trackedInfo;
				SaveValue();
			}

			public void SaveValue()
			{
				object obj = trackedInfo.propInfo.GetValue(trackedInfo.relatedObj);
				if (obj != null)
				{
					bool isArray = obj.GetType().IsArray;
					if (isArray)
					{
						if (value == null)
						{
							value = Array.CreateInstance(obj.GetType().GetElementType(), ((Array)obj).Length);
							_auxReferenceArray = (Array)obj;
						}
						Array.Copy(_auxReferenceArray, (Array)value, _auxReferenceArray.Length);
					}
					else
					{
						value = obj;
					}
				}
				else
				{
					value = null;
				}
			}

			public void RestoreValue()
			{
				if (trackedInfo.relatedObj != null)
				{
					if (value is Array)
					{
						Array.Copy((Array)value, _auxReferenceArray, ((Array)value).Length);
					}
					else
					{
						trackedInfo.propInfo.SetValue(trackedInfo.relatedObj, value);
					}
				}
			}
		}

		internal class TrackedInfo
		{
			public object relatedObj;

			public MemberInfo propInfo;
		}

		private static ResourcePoolStateTrackerState resourcePool = new ResourcePoolStateTrackerState();

		private HashSet<string> trackedInfosAdded = new HashSet<string>();

		private List<TrackedInfo> trackedInfos = new List<TrackedInfo>();

		private GenericBufferWindow<List<State>> states;

		internal static StateTracker instance;

		public static void Init(int rollbackWindow)
		{
			instance = new StateTracker();
			instance.states = new GenericBufferWindow<List<State>>(rollbackWindow);
		}

		public static void CleanUp()
		{
			instance = null;
		}

		public static void AddTracking(object obj, string path)
		{
			bool flag = StateTracker.instance != null;
			if (flag)
			{
				string item = string.Format("{0}_{1}_{2}", obj.GetType().FullName, obj.GetHashCode(), path);
				bool flag2 = !StateTracker.instance.trackedInfosAdded.Contains(item);
				if (flag2)
				{
					StateTracker.TrackedInfo trackedInfo = StateTracker.GetTrackedInfo(obj, path);
					StateTracker.instance.trackedInfos.Add(trackedInfo);
					StateTracker.instance.trackedInfosAdded.Add(item);
					int i = 0;
					int size = StateTracker.instance.states.size;
					while (i < size)
					{
						StateTracker.State @new = StateTracker.resourcePool.GetNew();
						@new.SetInfo(trackedInfo);
						StateTracker.instance.states.Current().Add(@new);
						StateTracker.instance.states.MoveNext();
						i++;
					}
				}
			}
		}

		public static void AddTracking(object obj)
		{
			bool flag = StateTracker.instance != null;
			if (flag)
			{
				List<MemberInfo> membersInfo = Utils.GetMembersInfo(obj.GetType());
				int i = 0;
				int count = membersInfo.Count;
				while (i < count)
				{
					MemberInfo memberInfo = membersInfo[i];
					object[] customAttributes = memberInfo.GetCustomAttributes(true);
					bool flag2 = customAttributes != null;
					if (flag2)
					{
						int j = 0;
						int num = customAttributes.Length;
						while (j < num)
						{
							bool flag3 = customAttributes[j] is AddTracking;
							if (flag3)
							{
								StateTracker.AddTracking(obj, memberInfo.Name);
							}
							j++;
						}
					}
					i++;
				}
			}
		}

		internal void SaveState()
		{
			List<StateTracker.State> list = this.states.Current();
			int i = 0;
			int count = list.Count;
			while (i < count)
			{
				list[i].SaveValue();
				i++;
			}
			this.MoveNextState();
		}

		internal void RestoreState()
		{
			List<StateTracker.State> list = this.states.Current();
			int i = 0;
			int count = list.Count;
			while (i < count)
			{
				list[i].RestoreValue();
				i++;
			}
		}

		internal void MoveNextState()
		{
			this.states.MoveNext();
		}

		private static StateTracker.TrackedInfo GetTrackedInfo(object obj, string name)
		{
			string[] array = name.Split(new char[]
			{
				'.'
			});
			int i = 0;
			int num = array.Length;
			StateTracker.TrackedInfo result;
			while (i < num)
			{
				string name2 = array[i];
				bool flag = obj == null;
				if (flag)
				{
					result = null;
				}
				else
				{
					Type type = obj.GetType();
                    MemberInfo memberInfo = type.GetProperty(name2, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (memberInfo == null)
                        memberInfo = type.GetField(name2, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (memberInfo == null)
                        Debug.LogError("memberInfo is null");
                    bool flag2 = memberInfo == null;
					if (flag2)
					{
						result = null;
					}
					else
					{
						bool flag3 = i == num - 1;
						if (!flag3)
						{
							obj = memberInfo.GetValue(obj);
							i++;
							continue;
						}
						result = new StateTracker.TrackedInfo
						{
							relatedObj = obj,
							propInfo = memberInfo
						};
					}
				}
				return result;
			}
			result = null;
			return result;
		}
	}
}
