using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P4KLib
{
    public class CustomBinaryReader : BinaryReader
    {
        public CustomBinaryReader(Stream input) : base(input)
        {
        }

        public CustomBinaryReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public CustomBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
        }

        public virtual string ReadString(int length)
        {
            var characters = this.ReadBytes(length);
            return Encoding.UTF8.GetString(characters);
        }
    }

    public class CustomBinaryWriter : BinaryWriter
    {
        public CustomBinaryWriter(Stream output) : base(output)
        {
        }

        public CustomBinaryWriter(Stream output, Encoding encoding) : base(output, encoding)
        {
        }

        public CustomBinaryWriter(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen)
        {
        }

        protected CustomBinaryWriter()
        {
        }

        public virtual void WriteString(string str, bool null_terminated)
        {
            var bytes = UTF8Encoding.UTF8.GetBytes(str);
            this.BaseStream.Write(bytes, 0, bytes.Length);
        }
    }
}
