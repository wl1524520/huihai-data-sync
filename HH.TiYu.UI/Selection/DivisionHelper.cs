using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using HH.TiYu.BLL;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.TiYu.Model.Security;
using HH.TiYu.UI.Controls;
using HH.TiYu.UI.UserControl;
using HH.Zhongkao.Device;
using HH.Zhongkao.Device.Wifi;
using HH.GeneralLibrary;
using HH.GeneralLibrary.Core.DAL;
using HH.GeneralLibrary.Core.UI;
using HH.GeneralLibrary.ExceptionHandling;
using HH.GeneralLibrary.LOG;
using HH.GeneralLibrary.Net;
using HH.GeneralLibrary.WinformControl;
namespace HH.TiYu.UI
{
	public class DivisionHelper
	{
		private List<Division> _Divisions = null;

		private List<Facility> _Facilities = null;

		private bool IsDescendantOf(Division d1, Division d2, List<Division> allDivisions)
		{
			bool result = false;
			if (d1.ID == d2.ID)
			{
				return true;
			}
			if (string.IsNullOrEmpty(d1.Parent))
			{
				return false;
			}
			if (d1.Parent == d2.ID)
			{
				return true;
			}
			Division division = allDivisions.SingleOrDefault((Division it) => it.ID == d1.Parent);
			if (division != null)
			{
				return IsDescendantOf(division, d2, allDivisions);
			}
			return result;
		}

		public void Init()
		{
			_Divisions = PhysicalProject.Current.AllDivisions;
			_Facilities = PhysicalProject.Current.AllFacilities;
		}

		public List<Facility> GetFacilitysOf(Division division)
		{
			List<Facility> list = null;
			if (division != null)
			{
				List<Division> chs = new List<Division>();
				if (_Divisions != null && _Divisions.Count > 0)
				{
					foreach (Division division2 in _Divisions)
					{
						if (IsDescendantOf(division2, division, _Divisions))
						{
							chs.Add(division2);
						}
					}
				}
				list = new List<Facility>();
				list.AddRange(_Facilities.Where((Facility it) => chs.Exists((Division d) => d.ID == it.DivisionID)));
			}
			else
			{
				list = new List<Facility>();
				list.AddRange(_Facilities);
			}
			return list;
		}
	}

}
