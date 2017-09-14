using System;

namespace TrueSync
{
	public class GenericBufferWindow<T>
	{
		public delegate T NewInstance();

		public T[] buffer;

		public int size;

		public int currentIndex;

		public GenericBufferWindow(int size)
		{
			this.size = size;
			currentIndex = 0;
			buffer = new T[size];
			for (int i = 0; i < size; i++)
			{
				this.buffer[i] = Activator.CreateInstance<T>();
			}
		}

		public GenericBufferWindow(int size, GenericBufferWindow<T>.NewInstance NewInstance)
		{
			this.size = size;
			currentIndex = 0;
			buffer = new T[size];
			for (int i = 0; i < size; i++)
			{
				buffer[i] = NewInstance();
			}
		}

		public void Resize(int newSize)
		{
			if (newSize != size)
			{
				T[] array = new T[newSize];
				int num = newSize - size;

				if (newSize > size)
				{
					for (int i = 0; i < size; i++)
					{
						if (i < currentIndex)
						{
							array[i] = buffer[i];
						}
						else
						{
							array[i + num] = buffer[i];
						}
					}
					for (int j = 0; j < num; j++)
					{
						array[currentIndex + j] = Activator.CreateInstance<T>();
					}
				}
				else
				{
					for (int k = 0; k < newSize; k++)
					{
						if (k < currentIndex)
						{
							array[k] = buffer[k];
						}
						else
						{
							array[k] = buffer[k - num];
						}
					}
					currentIndex %= newSize;
				}
				buffer = array;
				size = newSize;
			}
		}

		public void Set(T instance)
		{
			this.buffer[this.currentIndex] = instance;
		}

		public T Previous()
		{
			int num = this.currentIndex - 1;
			bool flag = num < 0;
			if (flag)
			{
				num = this.size - 1;
			}
			return this.buffer[num];
		}

		public T Current()
		{
			return this.buffer[this.currentIndex];
		}

		public void MoveNext()
		{
			this.currentIndex = (this.currentIndex + 1) % this.size;
		}
	}
}
