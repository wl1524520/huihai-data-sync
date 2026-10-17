using System;
using System.Text;

namespace HH.GeneralLibrary
{
	/// <summary>
	/// 十六进制字符串转换。原库还提供 StringToHex/HexEquals，
	/// 采集器只用 HexToString 做协议日志与报文比对输出，已移除其余两个。
	/// </summary>
	public class HexStringConverter
	{
		public static string HexToString(byte[] hexs, string split)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < hexs.Length; i++)
			{
				byte b = hexs[i];
				if (!string.IsNullOrEmpty(split))
				{
					stringBuilder.Append(b.ToString("X2") + split);
				}
				else
				{
					stringBuilder.Append(b.ToString("X2"));
				}
			}
			return stringBuilder.ToString();
		}
	}

}
