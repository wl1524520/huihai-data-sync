// Recovered from lib/Model.dll with ILSpy; see README.md.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using HH.TiYu.Model.Security;
using Jurassic;
using LJH.GeneralLibrary;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.SoftDog;
using Newtonsoft.Json;

namespace HH.TiYu.Model
{
    public class StudentPhoto : IEntity<Guid>
    {
        private Image _Img;

        public Guid ID { get; set; }

        public byte[] Photo { get; set; }

        public StudentPhoto()
        {
        }

        public StudentPhoto(Guid id, string path)
        {
            ID = id;
            Photo = GetBytesFromPhoto(path);
        }

        public StudentPhoto(Guid id, byte[] photo)
        {
            ID = id;
            Photo = photo;
        }

        private Image GetImageFromBytes(byte[] photo)
        {
            try
            {
                if (photo != null)
                {
                    string text = Path.Combine(TempFolderManager.GetCurrentFolder(), string.Format("{0}_{1}.jpg", "Staff", Guid.NewGuid().ToString()));
                    using (FileStream fileStream = new FileStream(text, FileMode.Create, FileAccess.Write))
                    {
                        fileStream.Write(photo, 0, photo.Length);
                    }
                    return Image.FromFile(text);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private byte[] GetBytesFromPhoto(string path)
        {
            byte[] array = null;
            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    array = new byte[fileStream.Length];
                    fileStream.Position = 0L;
                    fileStream.Read(array, 0, (int)fileStream.Length);
                }
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
            }
            return array;
        }

        public Image GetPhoto()
        {
            if (_Img == null && Photo != null)
            {
                _Img = GetImageFromBytes(Photo);
            }
            return _Img;
        }
    }
}
