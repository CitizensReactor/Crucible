using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Crucible
{
    public class CrucibleUtil
    {
        public static String BytesToString(long byteCount)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

            long bytes = Math.Abs(byteCount);
            var suffixIndex = Math.Max(0, (int)(Math.Log((double)bytes) / Math.Log(1024.0)));
            var suffix = suffixes[suffixIndex];
            double number = Math.Round(bytes / Math.Pow(1024, suffixIndex), 1);
            var result = (Math.Sign(byteCount) * number).ToString() + suffix;

            return result;
        }

        public static void WriteToXmlFile<T>(string filePath, T objectToWrite, bool append = false) where T : class, new()
        {
            TextWriter writer = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                writer = new StreamWriter(filePath, append);
                serializer.Serialize(writer, objectToWrite);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        public static T ReadFromXmlFile<T>(string filePath, bool createDefault = false) where T : class, new()
        {
            TextReader reader = null;
            if (File.Exists(filePath))
            {
                try
                {
                    var byteData = File.ReadAllBytes(filePath);
                    using (MemoryStream ms = new MemoryStream(byteData))
                    {
                        var serializer = new XmlSerializer(typeof(T));
                        reader = new StreamReader(ms);
                        return (T)serializer.Deserialize(reader);
                    }
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
            }
            if (createDefault)
            {
                return Activator.CreateInstance<T>();
            }
            else
            {
                return null;
            }
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = null;

            switch (child)
            {
                case ContextMenu contextMenu:
                    parentObject = contextMenu.PlacementTarget;
                    break;
                default:
                    parentObject = VisualTreeHelper.GetParent(child);
                    break;
            }

            if (parentObject == null)
            {
                return null;
            }

            T parent = parentObject as T;
            if (parent != null)
            {
                return (parent);
            }
            else
            {
                return FindParent<T>(parentObject);
            } 
        }

        public static dynamic Cast(dynamic obj, Type castTo)
        {
            if(castTo.IsEnum)
            {
                return Enum.ToObject(castTo, obj);
            }
            return Convert.ChangeType(obj, castTo);
        }

        public static dynamic Cast<T>(dynamic obj)
        {
            if (typeof(T).IsEnum)
            {
                return Enum.ToObject(typeof(T), obj);
            }
            return Convert.ChangeType(obj, typeof(T));
        }

        [DllImport("kernel32.dll")]
        internal static extern bool DosDateTimeToFileTime(ushort wFatDate, ushort wFatTime, out UFILETIME lpFileTime);

        internal struct UFILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        internal static DateTime ConvertDosDateTime(ushort DosDate, ushort DosTime)
        {
            UFILETIME filetime = new UFILETIME();
            DosDateTimeToFileTime(DosDate, DosTime, out filetime);
            long longfiletime = (long)(((ulong)filetime.dwHighDateTime << 32) +  (ulong)filetime.dwLowDateTime);
            return DateTime.FromFileTimeUtc(longfiletime);
        }
    }
}
