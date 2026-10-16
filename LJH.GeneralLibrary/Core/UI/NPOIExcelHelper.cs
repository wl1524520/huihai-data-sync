using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.LOG;
using LJH.GeneralLibrary.Net;
using LJH.GeneralLibrary.SoftDog;
using LJH.GeneralLibrary.WinformControl;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
namespace LJH.GeneralLibrary.Core.UI
{
	public class NPOIExcelHelper
	{
		private static DataTable GetData(DataGridView Dgv, bool forText)
		{
			DataTable dataTable = new DataTable();
			List<string> list = Columns(Dgv);
			for (int i = 0; i < list.Count; i++)
			{
				dataTable.Columns.Add(Dgv.Columns[list[i]].HeaderText);
			}
			foreach (DataGridViewRow item in (IEnumerable)Dgv.Rows)
			{
				if (item.Visible)
				{
					DataRow dataRow = dataTable.NewRow();
					for (int j = 0; j < list.Count; j++)
					{
						dataRow[j] = (forText ? item.Cells[list[j]].FormattedValue : item.Cells[list[j]].Value);
					}
					dataTable.Rows.Add(dataRow);
				}
			}
			return dataTable;
		}

		private static List<string> Columns(DataGridView view)
		{
			List<DataGridViewColumn> list = new List<DataGridViewColumn>();
			foreach (DataGridViewColumn column in view.Columns)
			{
				if (column.Visible && !string.IsNullOrEmpty(column.HeaderText))
				{
					list.Add(column);
				}
			}
			return (from col in list
				where col.Visible
				orderby col.DisplayIndex
				select col.Name).ToList();
		}

		private static object GetValue(ICell cell)
		{
			if (cell == null)
			{
				return null;
			}
			switch (cell.CellType)
			{
			case CellType.Blank:
				return null;
			case CellType.Boolean:
				return cell.BooleanCellValue;
			case CellType.Numeric:
				if (DateUtil.IsCellDateFormatted(cell))
				{
					return cell.DateCellValue;
				}
				return cell.NumericCellValue;
			case CellType.String:
				return cell.StringCellValue;
			case CellType.Error:
				return cell.ErrorCellValue;
			default:
				return "=" + cell.CellFormula;
			}
		}

		public static DataTable Import(string strFileName)
		{
			DataTable dataTable = new DataTable();
			string text = Path.GetExtension(strFileName.ToLower()).Trim();
			using (FileStream inputStream = new FileStream(strFileName, FileMode.Open, FileAccess.Read))
			{
				IWorkbook workbook = WorkbookFactory.Create((Stream)inputStream);
				ISheet sheetAt = workbook.GetSheetAt(0);
				IRow row = sheetAt.GetRow(sheetAt.FirstRowNum);
				List<int> list = new List<int>();
				for (int i = 0; i < row.LastCellNum; i++)
				{
					object value = GetValue(row.GetCell(i));
					if (value == null || value.ToString() == string.Empty)
					{
						dataTable.Columns.Add(new DataColumn(i.ToString(), typeof(string)));
					}
					else
					{
						dataTable.Columns.Add(new DataColumn(value.ToString()));
					}
					list.Add(i);
				}
				for (int j = sheetAt.FirstRowNum + 1; j <= sheetAt.LastRowNum; j++)
				{
					IRow row2 = sheetAt.GetRow(j);
					try
					{
						if (row2 == null)
						{
							continue;
						}
						DataRow dataRow = dataTable.NewRow();
						bool flag = false;
						foreach (int item in list)
						{
							dataRow[item] = GetValue(row2.GetCell(item));
							if (dataRow[item] != null && dataRow[item].ToString() != string.Empty)
							{
								flag = true;
							}
						}
						if (flag)
						{
							dataTable.Rows.Add(dataRow);
						}
					}
					catch (Exception ex)
					{
						string message = string.Format("数据读出出错:{0} 行号:{1}", ex.Message, j + 1);
						throw new Exception(message, ex);
					}
				}
			}
			return dataTable;
		}

		public static void Export(DataTable dt, string fileName)
		{
			string text = Path.GetExtension(fileName.ToLower()).Trim();
			IWorkbook workbook = null;
			workbook = ((!(text == ".xls")) ? ((IWorkbook)new XSSFWorkbook()) : ((IWorkbook)new HSSFWorkbook()));
			ISheet sheet = workbook.CreateSheet("Sheet1");
			sheet.DefaultColumnWidth = 15;
			IRow row = sheet.CreateRow(0);
			for (int i = 0; i < dt.Columns.Count; i++)
			{
				ICell cell = row.CreateCell(i);
				cell.SetCellValue(dt.Columns[i].ColumnName);
			}
			for (int j = 0; j < dt.Rows.Count; j++)
			{
				IRow row2 = sheet.CreateRow(j + 1);
				for (int k = 0; k < dt.Columns.Count; k++)
				{
					ICell cell2 = row2.CreateCell(k);
					object obj = dt.Rows[j][k];
					if (obj != null)
					{
						if (obj is int || obj is decimal || obj is long || obj is short || obj is byte || obj is double || obj is float)
						{
							cell2.SetCellValue((double)obj);
						}
						else
						{
							cell2.SetCellValue(obj.ToString());
						}
					}
				}
			}
			MemoryStream memoryStream = new MemoryStream();
			workbook.Write(memoryStream);
			byte[] array = memoryStream.ToArray();
			using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
			{
				fileStream.Write(array, 0, array.Length);
				fileStream.Flush();
			}
		}

		public static void Export(DataGridView myDgv, string fileName, bool forText = false)
		{
			DataTable data = GetData(myDgv, forText);
			Export(data, fileName);
		}

		public static void Export(DataGridView myDgv, bool forText = false)
		{
			try
			{
				SaveFileDialog saveFileDialog = new SaveFileDialog();
				saveFileDialog.Filter = "Excel文档|*.xls;*.xlsx|所有文件(*.*)|*.*";
				saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				if (saveFileDialog.ShowDialog() == DialogResult.OK)
				{
					string fileName = saveFileDialog.FileName;
					Export(myDgv, fileName, forText);
					MessageBox.Show("导出成功");
				}
			}
			catch (Exception)
			{
				MessageBox.Show("保存到电子表格时出现错误!");
			}
		}
	}

}
