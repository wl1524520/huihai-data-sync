using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HH.TiYu.Device;
using HH.TiYu.Device.Wifi;

class VerifyDevice
{
    static readonly List<string> output = new List<string>();
    static readonly Assembly device = typeof(PhysicalRecord).Assembly;
    static readonly BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static string Value(object v) {
        if(v==null)return "null";
        if(v is byte[])return BitConverter.ToString((byte[])v);
        if(v is DateTime)return ((DateTime)v).ToString("o",CultureInfo.InvariantCulture);
        if(v is decimal)return ((decimal)v).ToString(CultureInfo.InvariantCulture);
        return v.ToString().Replace("\0","\\0").Replace("\r","\\r").Replace("\n","\\n");
    }
    static void Observe(string name, object v){output.Add(name+"="+Value(v));}
    static void Properties(string prefix, object obj) {
        if(obj==null){Observe(prefix,null);return;}
        foreach(var p in obj.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance).OrderBy(p=>p.Name,StringComparer.Ordinal)){
            try{Observe(prefix+"."+p.Name,p.GetValue(obj,null));}
            catch(TargetInvocationException ex){Observe(prefix+"."+p.Name,ex.InnerException.GetType().Name);}
        }
    }
    static object New(string name,byte[] bytes) {return Activator.CreateInstance(device.GetType("HH.TiYu.Device.Wifi."+name),All,null,new object[]{bytes},CultureInfo.InvariantCulture);}
    static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,All).Invoke(o,args);}
    static byte Bcd(int v){return (byte)((v/10)*16+v%10);}
    static int Main(string[] args) {
      try{
        System.Threading.Thread.CurrentThread.CurrentCulture=CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture=CultureInfo.InvariantCulture;
        if(new Crc16Ccitt().CalCRC(Encoding.ASCII.GetBytes("123456789"))!=0x29B1)throw new Exception("CRC known vector");
        var random=new Random(971);
        foreach(InitialCrcValue initial in Enum.GetValues(typeof(InitialCrcValue))) {
          var crc=new Crc16Ccitt(initial);
          for(int n=0;n<=1024;n+=17){var b=new byte[n];random.NextBytes(b);Observe("CRC."+initial+"."+n,crc.CalCRC(b));Observe("CRC.bytes."+initial+"."+n,crc.CalCRCBytes(b));}
        }
        int k=0;
        foreach(int len in new[]{62,64})
        foreach(byte item in new byte[]{1,6,9,15,16,81,100})
        foreach(byte sex in new byte[]{0,1,0x30,0x31,0x41,0xff})
        foreach(int mode in new[]{0,1,2,3,4,5,6}) {
          byte[] b=new byte[len];int off=len==62?8:10;
          b[4]=7;b[6]=1;b[7]=9;b[8]=2;b[9]=3;
          Encoding.ASCII.GetBytes(mode==6?"12ABCDEF":"x000123").CopyTo(b,off);
          b[off+20]=len==62?Bcd(item):item;
          byte[] score=Encoding.ASCII.GetBytes("12.34");
          if(mode==1||mode==2)score=new byte[]{(byte)(mode==1?224:225),0,0,0x30,0x39};
          if(mode>=3&&mode<=5)score=new byte[]{(byte)(157+mode),0,0,0,0};
          score.CopyTo(b,off+21);
          new byte[]{0x26,(byte)(mode==6?0:0x10),0x16,0x09,0x35,sex}.CopyTo(b,off+26);
          Encoding.GetEncoding("GB2312").GetBytes("测试学生").CopyTo(b,off+32);
          object packet=New("DataResponse",b);
          object record=Call(packet,"GetData");
          Properties("record."+(k++),record);
          b[len-1]^=0xff;Observe("corrupt."+k,Call(New("DataResponse",b),"get_IsValid"));
        }
        foreach(int len in new[]{18,24}) {
          var b=new byte[len];for(int i=0;i<len;i++)b[i]=(byte)i;b[len==18?15:21]=7;
          Properties("header."+len,New("HeadResponse",b));
        }
        var ps=new HostParameters();
        int pn=0;foreach(var p in typeof(HostParameters).GetProperties())if(p.CanWrite)p.SetValue(ps,p.PropertyType==typeof(byte)?(object)(byte)(++pn):Convert.ChangeType(++pn,p.PropertyType,CultureInfo.InvariantCulture),null);
        var parameterType=device.GetType("HH.TiYu.Device.Wifi.GetParameterResponse");
        var parameterBytes=(byte[])parameterType.GetMethod("GetBytes",All).Invoke(null,new object[]{ps});
        Observe("parameter.bytes",parameterBytes);
        Properties("parameter.values",ps);
        var downloader=StudentDownloader.CreateInstance(7,"127.0.0.1",null); // Constructor only; never Open/Send.
        var students=new[]{
          new HostStudent{StudentID="1234567890123456789012345",Name="测试学生",Sex="男",Grade=(byte)(args.Contains("--require-empty-grade")?5:0),ClassName="一年级一班",CardID="987654321012345"},
          new HostStudent{StudentID="AB123",Name="",Sex="0",Grade=0,ClassName=null,CardID=null}
        };
        foreach(string method in new[]{"CreateDownloadStudentsCmd","CreateDownloadTestStudentsCmd"}) {
          object cmd=Call(downloader,method,(byte)7,students);
          byte[] data=(byte[])cmd.GetType().GetProperty("data",All).GetValue(cmd,null);
          if(data.Length!=15+72*students.Length+2 || data[12]!=72)throw new Exception(method+": student record length changed");
          for(int i=0;i<students.Length;i++)
            if(data[15+72*i+41]!=0)throw new Exception(method+": grade must be empty");
          if(args.Contains("--require-empty-grade") && students[0].Grade!=5)throw new Exception(method+": input grade was modified");
          foreach(var f in cmd.GetType().GetFields(All).OrderBy(f=>f.Name))Observe(method+"."+f.Name,f.GetValue(cmd));
        }
        foreach(string method in new[]{"CreateCompleteDownloadCmd","CreateClearStudentsCmd"}){
          object cmd=Call(downloader,method,(byte)7);
          foreach(var f in cmd.GetType().GetFields(All).OrderBy(f=>f.Name))Observe(method+"."+f.Name,f.GetValue(cmd));
        }
        var search=typeof(WifiSearcher).GetMethod("GetData",All);
        foreach(string s in new[]{"192.168.1.2,x,H7_P6_SN123,x","bad","a,b,x,y","a,b,H255_P100_SNABC,d","a,b,H999_P999_SN0,d"}){
           Properties("search."+s,search.Invoke(null,new object[]{s}));
        }
        var buffer=new PhysicalRecordBuffer();Observe("buffer.empty",buffer.Read()==null);
        var records=new[]{new PhysicalRecord{SN=2},new PhysicalRecord{SN=3}};
        buffer.Add(records[0]);buffer.AddRange(records);Observe("buffer.order",string.Join(",",buffer.Read().Select(r=>r.SN)));Observe("buffer.drained",buffer.Read()==null);
        File.WriteAllLines("device-behavior.txt",output,Encoding.UTF8);
        Console.WriteLine("Device offline observations: "+output.Count);
        return 0;
      }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
