using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace LJH.GeneralLibrary.Core.UI
{
	/// <summary>
	/// 成绩表导出。原库还提供 Import 及其专用的 GetValue 单元格读取，
	/// 采集器只导出不上传 Excel（学生导入走同步接口），已移除。
	/// </summary>
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
