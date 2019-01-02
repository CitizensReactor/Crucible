using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binary
{
    public class BinaryBlobReader
    {
        public int Position = 0;
        private byte[] Data;
        public int Length => Data?.Length ?? 0;

        public BinaryBlobReader(byte[] data, int pos)
        {
            this.Data = data;
            this.Position = pos;
        }

        public BinaryBlobReader Fracture()
        {
            return new BinaryBlobReader(Data, Position);
        }

        public BinaryBlobReader Fracture(int new_position)
        {
            return new BinaryBlobReader(Data, new_position);
        }

        public unsafe object Read(Type type)
        {
            var readMethod = typeof(BinaryBlobReader).GetMethod("Read", new Type[] { });
            var readMethodType = readMethod?.MakeGenericMethod(new Type[] { type });

#if DEBUG
            if (readMethodType == null)
            {
                throw new Exception("Unsupported read by type");
            }
#endif 

            return readMethodType.Invoke(this, new object[] { });
        }

        public unsafe object Read(Type type, int count)
        {
            var readMethod = typeof(BinaryBlobReader).GetMethod("Read", new Type[] { typeof(int) });
            var readMethodType = readMethod?.MakeGenericMethod(new Type[] { type });

#if DEBUG
            if (readMethodType == null)
            {
                throw new Exception("Unsupported read by type");
            }
#endif 

            return readMethodType.Invoke(this, new object[] { count });
        }

        public unsafe T Read<T>() where T : unmanaged
        {
            fixed (byte* data_ptr = Data)
            {
                T* raw_ptr = (T*)(data_ptr + Position);
                this.Position += sizeof(T);
                return *raw_ptr;
            }
        }

        public unsafe T[] Read<T>(int count) where T : unmanaged
        {
            var results = new T[count];

            fixed (byte* src = Data)
            {
                fixed (T* dest = results)
                {
                    var bytes_copied = sizeof(T) * count;
                    Buffer.MemoryCopy(src + Position, dest, bytes_copied, bytes_copied);
                    Position += bytes_copied;
                }
            }

            return results;
        }

        public bool ReadBoolean() { return Read<Int32>() != 0; }
        public bool ReadBoolean8() { return Read<byte>() != 0; }
        public double ReadDouble() { return Read<double>(); }
        public sbyte ReadSByte() { return Read<SByte>(); }
        public byte ReadByte() { return Read<Byte>(); }
        public short ReadInt16() { return Read<Int16>(); }
        public int ReadInt32() { return Read<Int32>(); }
        public long ReadInt64() { return Read<Int64>(); }
        public float ReadSingle() { return Read<Single>(); }
        public ushort ReadUInt16() { return Read<UInt16>(); }
        public uint ReadUInt32() { return Read<UInt32>(); }
        public ulong ReadUInt64() { return Read<UInt64>(); }
        public Guid ReadGuid() { return Read<Guid>(); }

        public static unsafe T[] FastCop2<T>(T[] src, int offset, int count) where T : unmanaged
        {
            var results = new T[count];

            fixed (T* _src = src)
            {
                fixed (T* dest = results)
                {
                    Buffer.MemoryCopy(_src + offset, dest, sizeof(T) * count, sizeof(T) * count);
                }
            }

            return results;
        }

        public static unsafe T[] FastCopySafe<T>(T[] src, int offset, int count) where T : unmanaged
        {
            if(count == -1)
            {
                return new T[0];
            }

            var results = new T[count];

            fixed (T* _src = src)
            {
                fixed (T* dest = results)
                {
                    Buffer.MemoryCopy(_src + offset, dest, sizeof(T) * count, sizeof(T) * count);
                }
            }

            return results;
        }

    }
}
