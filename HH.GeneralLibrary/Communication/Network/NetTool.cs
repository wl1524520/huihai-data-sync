using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using HH.GeneralLibrary.ExceptionHandling;

namespace HH.GeneralLibrary.Net
{
	/// <summary>
	/// 本机网络信息。原库还提供 GetLocalIP/GetLocalMAC/GetHostName，
	/// 采集器只用 GetLocalIPS 做主机发现，其余三个在本目录内无调用方，已移除
	/// （GetLocalMAC 是原库中 System.Management 的唯一使用者，引用随之删除）。
	/// </summary>
	public class NetTool
	{
		public static IPAddress[] GetLocalIPS()
		{
			try
			{
				IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
				if (hostEntry.AddressList != null)
				{
					return hostEntry.AddressList.Where((IPAddress it) => it.AddressFamily == AddressFamily.InterNetwork).ToArray();
				}
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
			}
			return null;
		}
	}

}
