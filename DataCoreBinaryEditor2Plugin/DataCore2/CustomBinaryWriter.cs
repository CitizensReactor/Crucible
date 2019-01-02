using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCore2
{
    internal class CustomBinaryWriter
    {
        Stream Output = null;
        byte[] OldData;

        private CustomBinaryWriter(Stream output, byte[] oldData)
        {
            Output = output;
            OldData = oldData;
        }

        public CustomBinaryWriter(Stream output)
        {
            Output = output;
        }


        protected CustomBinaryWriter() { }

        private void Write(byte[] data)
        {
            var current_pos = Output.Position;

            Output.Write(data, 0, data.Length);
        }

#if DEBUG
        internal enum ComparisonResult
        {
            Good,
            IncorrectData,
            WrittenTooFar
        }

        internal ComparisonResult CompareBytes(byte[] data)
        {
            var current_pos = Output.Position;

            if (OldData != null)
            {
                for (long i = 0; i < data.Length; i++)
                {
                    long old_data_pos = current_pos + i;
                    if (old_data_pos > OldData.LongLength)
                    {
                        return ComparisonResult.WrittenTooFar;
                    }

                    if (OldData[old_data_pos] != data[i])
                    {
                        return ComparisonResult.IncorrectData;
                    }
                }
            }

            return ComparisonResult.Good;
        }

        internal unsafe T GetOriginalValue<T>() where T : unmanaged
        {
            T oldValue;
            fixed (byte* oldDataPtr = OldData)
            {
                T* oldValuePtr = (T*)(oldDataPtr + Output.Position);
                oldValue = *oldValuePtr;
            }
            return oldValue;
        }

        internal unsafe void PerformComparison<T>(T value, byte[] buffer) where T : unmanaged
        {
            var comparisonResult = CompareBytes(buffer);
            switch (comparisonResult)
            {
                case ComparisonResult.IncorrectData:
                case ComparisonResult.WrittenTooFar:

                    T newValue = value;
                    T oldValue = GetOriginalValue<T>();

                    throw new Exception($"Invalid write operation {comparisonResult.ToString()}");
                case ComparisonResult.Good:
                    break;
            }
        }

        internal unsafe bool IsValid<T>(T value) where T : unmanaged
        {
            T* _src = &value;
            byte* src = (byte*)_src;
            byte[] buffer = new byte[sizeof(T)];
            fixed (byte* buffer_ptr = buffer)
            {
                Buffer.MemoryCopy(src, buffer_ptr, buffer.LongLength, buffer.LongLength);
            }

            var comparisonResult = CompareBytes(buffer);
            switch (comparisonResult)
            {
                case ComparisonResult.IncorrectData:
                case ComparisonResult.WrittenTooFar:
                    return false;
                case ComparisonResult.Good:
                default:
                    return true;
            }
        }

        internal unsafe bool IsValid<T>(T[] array) where T : unmanaged
        {
            bool valid = true;

            // required for comparison debugging
            foreach (var value in array)
            {
                valid &= IsValid(value);
            }

            return valid;
        }

        public unsafe void DebugNoCheckWrite<T>(T value) where T : unmanaged
        {
            T* _src = &value;
            byte* src = (byte*)_src;
            byte[] buffer = new byte[sizeof(T)];
            fixed (byte* buffer_ptr = buffer)
            {
                Buffer.MemoryCopy(src, buffer_ptr, buffer.LongLength, buffer.LongLength);
            }

            Write(buffer);
        }

        public unsafe void DebugNoCheckWrite<T>(IEnumerable<T> array) where T : unmanaged
        {
            // required for comparison debugging
            foreach (var value in array)
            {
                DebugNoCheckWrite<T>(value);
            }
        }

#endif

        public unsafe void Write<T>(T value) where T : unmanaged
        {
            T* _src = &value;
            byte* src = (byte*)_src;
            byte[] buffer = new byte[sizeof(T)];
            fixed (byte* buffer_ptr = buffer)
            {
                Buffer.MemoryCopy(src, buffer_ptr, buffer.LongLength, buffer.LongLength);
            }

#if DEBUG
            PerformComparison<T>(value, buffer);
#endif

            Write(buffer);
        }

        public unsafe void Write<T>(IEnumerable<T> collection) where T : unmanaged
        {
            foreach (var value in collection)
            {
                Write<T>(value);
            }
        }

        public unsafe void Write<T>(T[] array) where T : unmanaged
        {
#if DEBUG
            // required for comparison debugging
            foreach (var value in array)
            {
                Write<T>(value);
            }
#else
            fixed (T* _src = array)
            {
                byte* src = (byte*)_src;
                byte[] buffer = new byte[sizeof(T) * array.Length];
                fixed (byte* buffer_ptr = buffer)
                {
                    Buffer.MemoryCopy(src, buffer_ptr, buffer.LongLength, buffer.LongLength);
                }

                Write(buffer);
            }
#endif
        }
    }
}
