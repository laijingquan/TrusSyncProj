using System;
using System.Collections.Generic;

namespace TrueSync
{
	public class ArrayResourcePool<T>
	{
		private Stack<T[]> stack = new Stack<T[]>();

		private int arrayLength;

		public int Count
		{
			get
			{
				return stack.Count;
			}
		}

		public ArrayResourcePool(int arrayLength)
		{
			this.arrayLength = arrayLength;
		}

		public void ResetResourcePool()
		{
			Stack<T[]> obj = stack;
			lock (obj)
			{
				stack.Clear();
			}
		}

		public void GiveBack(T[] obj)
		{
			Stack<T[]> obj2 = stack;
			lock (obj2)
			{
				stack.Push(obj);
			}
		}

		public T[] GetNew()
		{
			Stack<T[]> obj = stack;
			T[] array;
			lock (obj)
			{
				if (stack.Count == 0)
				{
					array = new T[this.arrayLength];
					stack.Push(array);
				}
				array = stack.Pop();
			}
			return array;
		}
	}
}
