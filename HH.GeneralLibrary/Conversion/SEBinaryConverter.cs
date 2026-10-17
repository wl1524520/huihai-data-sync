using System;

namespace HH.GeneralLibrary
{
	/// <summary>
	/// 字节序转换。原库还提供 LongToBytes/UintToBytes/BytesToShort，
	/// 采集器（含设备协议层）未使用，已移除。
	/// </summary>
	public class SEBinaryConverter
	{
		public static byte[] IntToBytes(int t)
		{
			byte[] array = new byte[4];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (byte)((t >> i * 8) & 0xFF);
			}
			return array;
		}

		public static byte[] ShortToBytes(short t)
		{
			byte[] array = new byte[2];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (byte)((t >> i * 8) & 0xFF);
			}
			return array;
		}

		public static long BytesToLong(byte[] bytes)
		{
			long num = 0L;
			if (bytes.Length <= 8)
			{
				for (int i = 0; i < bytes.Length; i++)
				{
					num += (long)((ulong)bytes[i] << i * 8);
				}
				return num;
			}
			throw new InvalidCastException("转换成长整形数最多只能是8个字节");
		}

		public static int BytesToInt(byte[] bytes)
		{
			int num = 0;
			if (bytes.Length <= 4)
			{
				for (int i = 0; i < bytes.Length; i++)
				{
					num += bytes[i] << i * 8;
				}
				return num;
			}
			throw new InvalidCastException("转换成整数最多只能是四个字节的数组");
		}

		public static uint BytesToUint(byte[] bytes)
		{
			uint num = 0u;
			if (bytes.Length <= 4)
			{
				for (int i = 0; i < bytes.Length; i++)
				{
					num += (uint)(bytes[i] << i * 8);
				}
				return num;
			}
			throw new InvalidCastException("转换成无符号整数最多只能是四个字节的数组");
		}
	}

}
