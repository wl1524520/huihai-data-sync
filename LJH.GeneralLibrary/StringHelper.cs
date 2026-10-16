namespace LJH.GeneralLibrary
{
	/// <summary>
	/// 全角/半角转换。原库还提供 ToSBC（半角转全角），
	/// 采集器的输入控件只做全角转半角（ToDBC），已移除 ToSBC。
	/// </summary>
	public class StringHelper
	{
		public static string ToDBC(string input)
		{
			char[] array = input.ToCharArray();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == '　')
				{
					array[i] = ' ';
				}
				else if (array[i] > '＀' && array[i] < '｟')
				{
					array[i] = (char)(array[i] - 65248);
				}
			}
			return new string(array);
		}
	}

}
