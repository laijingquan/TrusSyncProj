using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace TrueSync
{
	public class Utils
	{
		public static List<MemberInfo> GetMembersInfo(Type type)
		{
			List<MemberInfo> list = new List<MemberInfo>();
			list.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
			list.AddRange(type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
			return list;
		}

        /// <summary>
        /// int转成底尾端字节流（小端）
        /// 11 22 33 44
        ///     尾端：44
        ///     高尾端（高地址在尾端 | 大端）： 11 22 33 44
        ///                                                      0   1   2   3
        ///                                                     低地址——》高地址
        ///     低尾端（地地址在尾端 | 小端）: 44 33 22 11
        ///                                                    0   1   2   3
        ///                                                    低地址——》高地址
        ///   int a=1234; byte b = (int)a; b:4 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bytes"></param>
		public static void GetBytes(int value, List<byte> bytes)
		{
			bytes.Add((byte)value);
			bytes.Add((byte)(value >> 8));
			bytes.Add((byte)(value >> 16));
			bytes.Add((byte)(value >> 24));
		}

		public static void GetBytes(long value, List<byte> bytes)
		{
			bytes.Add((byte)value);
			bytes.Add((byte)(value >> 8));
			bytes.Add((byte)(value >> 16));
			bytes.Add((byte)(value >> 24));
			bytes.Add((byte)(value >> 32));
			bytes.Add((byte)(value >> 40));
			bytes.Add((byte)(value >> 48));
			bytes.Add((byte)(value >> 56));
        }

        /// <summary>
        /// md5编码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string GetMd5Sum(string str)
		{
			Encoder encoder = Encoding.Unicode.GetEncoder();
			byte[] array = new byte[str.Length * 2];
			encoder.GetBytes(str.ToCharArray(), 0, str.Length, array, 0, true);
			MD5 mD = new MD5CryptoServiceProvider();
			byte[] array2 = mD.ComputeHash(array);
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < array2.Length; i++)
			{
				stringBuilder.Append(array2[i].ToString("X2"));//ToString("X2") 为C#中的字符串格式控制符|X为     十六进制 |2为 每次都是两位数
            }
			return stringBuilder.ToString();
		}
	}
}
