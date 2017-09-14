using System;
using System.Reflection;

public static class TrueSyncExtensionsCore
{
	public static object GetValue(this MemberInfo memberInfo, object obj)
	{
		object result;
		if (memberInfo is PropertyInfo)
		{
			result = ((PropertyInfo)memberInfo).GetValue(obj, null);
		}
		else
		{
			if (memberInfo is FieldInfo)
			{
				result = ((FieldInfo)memberInfo).GetValue(obj);
			}
			else
			{
				result = null;
			}
		}
		return result;
	}

	public static void SetValue(this MemberInfo memberInfo, object obj, object value)
	{
		if (memberInfo is PropertyInfo)
		{
			((PropertyInfo)memberInfo).SetValue(obj, value, null);
		}
		else
		{
			if (memberInfo is FieldInfo)
			{
				((FieldInfo)memberInfo).SetValue(obj, value);
			}
		}
	}
}
