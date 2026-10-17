using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HH.GeneralLibrary;
using HH.GeneralLibrary.Core.DAL;

internal static class VerifyGeneralLibrary
{
    private static readonly List<string> Lines = new List<string>();
    private static void Record(Func<object> action)
    {
        try { Lines.Add(Convert.ToString(action(), CultureInfo.InvariantCulture)); }
        catch (Exception ex) { Lines.Add("throws:" + ex.GetType().FullName); }
    }
    private static int Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        for (int i = -1; i <= 100; i++)
        {
            int value = i;
            Record(delegate { return BCDConverter.IntToBCD(value); });
        }
        for (int i = 0; i < 256; i++)
        {
            byte value = (byte)i;
            Record(delegate { return BCDConverter.BCDtoInt(value); });
        }
        foreach (int i in new[] { int.MinValue, -65536, -1, 0, 1, 255, 256, 65535, int.MaxValue })
        {
            int value = i;
            Record(delegate { return BitConverter.ToString(SEBinaryConverter.IntToBytes(value)); });
            Record(delegate { return SEBinaryConverter.BytesToInt(SEBinaryConverter.IntToBytes(value)); });
            Record(delegate { return SEBinaryConverter.BytesToUint(SEBinaryConverter.IntToBytes(value)); });
        }
        // LongToBytes 已随本轮精简移除，改为直接观察 BytesToLong 的输入输出。
        foreach (byte[] i in new[] {
            new byte[] { 0 }, new byte[] { 0xFF },
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 0x80 }, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F },
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF } })
        {
            byte[] value = i;
            Record(delegate { return SEBinaryConverter.BytesToLong(value); });
        }
        foreach (string culture in new[] { "en-US", "zh-CN", "de-DE" })
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            foreach (decimal d in new[] { 0m, -1.2300m, 10.000m, 1.2001m, 9999999.99m })
            {
                decimal value = d;
                Record(delegate { return DecimalExtention.Trim(value); });
            }
        }
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        // DSEncrypt/DTEncrypt（加密狗）已随本轮精简移除。
        foreach (string input in new[] { "", "password", "AbC123!", "中文", "A B", "１２３ＡＢＣ" })
        {
            string value = input;
            Record(delegate { return StringHelper.ToDBC(value); });
        }
        foreach (string uri in new[] { null, "", "Data Source=test.db", "SQLITE:Data Source=test.db", "MSSQL:Server=(local)", ":value" })
        {
            string value = uri;
            Record(delegate { var parsed = new SQLConnectionURI(value); return parsed.SQLType + "|" + parsed.ConnectString + "|" + parsed; });
        }
        Record(delegate { var result = new QueryResultList<string>(ResultCode.Successful, "ok", new List<string>(), 3, 2, 10); return result.PageSize + "|" + result.PageIndex + "|" + result.TotalCount; });
        File.WriteAllLines("general-library-behavior.txt", Lines.ToArray());
        Console.WriteLine("GeneralLibrary: " + Lines.Count + " observations");
        return 0;
    }
}
