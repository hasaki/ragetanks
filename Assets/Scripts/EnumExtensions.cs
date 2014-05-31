using System;

namespace RageTanks
{
	static class EnumExtensions
	{
		public static bool HasFlag(this Enum variable, Enum value)
		{
			if (variable == null)
				return false;

			ulong num = Convert.ToUInt64(value);
			return ((Convert.ToUInt64(variable) & num) > 0);
		}
	}
}
