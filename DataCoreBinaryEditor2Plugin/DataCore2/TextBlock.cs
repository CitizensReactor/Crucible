using Binary;
using DataCore2;
using DataCore2.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2
{
    internal class TextBlock
    {
        public int Size => StringData.Count;
        private List<byte> StringData;
        private Dictionary<int, string> OffsetToString = new Dictionary<int, string>();
        private Dictionary<string, int> StringToOffset = new Dictionary<string, int>();
        private volatile bool FinishedProcessing = false;

        Task iterateblocktask = null;
        public void WaitForInitialization()
        {
            if (!FinishedProcessing)
            {
                iterateblocktask.Wait();
            }
        }

        public TextBlock(BinaryBlobReader reader, int size)
        {
            StringData = reader.Read<byte>(size).ToList();

            iterateblocktask = new TaskFactory().StartNew(_PopulateStrings);
        }

        public TextBlock()
        {
            StringData = new List<byte>();
            FinishedProcessing = true;
        }

        public int AddString(string str)
        {
            WaitForInitialization();

            if(StringToOffset.ContainsKey(str))
            {
                return StringToOffset[str];
            }

            //TODO: Maybe check if string exists?

            var data = Encoding.UTF8.GetBytes(str);

            var offset = StringData.Count;

            StringData.AddRange(data);
            StringData.Add(0);

            OffsetToString[offset] = str;
            StringToOffset[str] = offset;

            return offset;
        }

        public RawStringReference AddStringToReference(string str)
        {
            var offset = AddString(str);
            return new RawStringReference { NameOffset = offset };
        }

        public string GetString(int offset)
        {
            if (!FinishedProcessing)
            {
                return _GetString(offset);
            }
            else
            {
                return OffsetToString[offset];
            }
        }

        public string GetString(RawStringReference strRef)
        {
            return GetString(strRef.NameOffset);
        }

        public string GetString(RawLocaleReference strRef)
        {
            return GetString(strRef.NameOffset);
        }

        private unsafe void _PopulateStrings()
        {
            var stringData = StringData.ToArray();
            int max_length = stringData.Length;
            fixed (byte* start_pos = stringData)
            {
                byte* current_pos = start_pos;
                var pos = current_pos - start_pos;
                while (pos < max_length)
                {
                    int length = 0;
                    while (current_pos[length] != 0)
                    {
                        length++;
                    }

                    var str = Encoding.UTF8.GetString(current_pos, length);
                    OffsetToString[(int)pos] = str;
                    StringToOffset[str] = (int)pos;

                    current_pos += length;
                    current_pos++; // null terminator
                    pos = current_pos - start_pos;
                }
            }
            FinishedProcessing = true;
        }

        private unsafe string _GetString(int offset)
        {
            var stringData = StringData.ToArray();
            int max_length = stringData.Length;
            fixed (byte* _StringDataPtr = stringData)
            {
                var src = _StringDataPtr + offset;

                int length = 0;
                while (src[length] != 0)
                {
                    length++;
#if DEBUG
                    if (length >= max_length)
                    {
                        throw new Exception("Couldn't read string");
                    }
#endif
                }

                return Encoding.UTF8.GetString(src, length);

            }
        }

        internal int AddString(DataCoreString dataCoreString)
        {
            return AddString(dataCoreString.String);
        }

        internal int AddString(DataCoreLocale dataCoreString)
        {
            return AddString(dataCoreString.String);
        }
    }
}
