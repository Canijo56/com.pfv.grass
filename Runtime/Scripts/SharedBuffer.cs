using System.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace PFV.Grass
{
    public struct SubBufferData<T>
    {
        SharedBuffer<T> parent;
        private uint _startOffset;
        public uint startOffset => _startOffset;
        public uint bytesStartOffset => startOffset * (uint)parent.stride;
        public int size;
        public T[] resetArgs;
        public SubBufferData(uint offsetPosition, SharedBuffer<T> parent)
        {
            this.parent = parent;
            _startOffset = offsetPosition;
            size = 0;
            resetArgs = new T[0];
        }

        public void SetDefaultData(T[] args)
        {
            resetArgs = new T[args.Length];
            Array.Copy(args, resetArgs, args.Length);
        }
    }
    public class SharedBuffer<T> : IDisposable
    {
        public ComputeBuffer buffer { get; private set; }

        public int size { get; private set; }
        private Dictionary<Enum, SubBufferData<T>> _subBufferData = new Dictionary<Enum, SubBufferData<T>>();
        public int stride { get; private set; }

        public SubBufferData<T> this[Enum subBufferID]
        {
            get
            {
                if (_subBufferData.TryGetValue(subBufferID, out SubBufferData<T> data))
                    return data;
                return default;
            }
        }

        public SharedBuffer(int stride)
        {
            this.size = 0;
            this.stride = stride;
        }

        public SharedBuffer()
        {

            this.size = 0;
            this.stride = Marshal.SizeOf<T>();
        }

        public void Allocate(ComputeBufferType type)
        {
            buffer?.Dispose();
            buffer = new ComputeBuffer(size, stride, type);
        }

        public void ResetToDefault(CommandBuffer cmd)
        {
            if (buffer != null)
            {
                T[] data = _subBufferData.SelectMany(v => v.Value.resetArgs).ToArray();
                cmd.SetBufferData(buffer, data);
            }
        }

        public int AddSubBuffer<TEnum>(TEnum id, int size) where TEnum : Enum
        {
            int offset = this.size;
            this.size += size;

            _subBufferData.Add(id, new SubBufferData<T>((uint)offset, this) { size = size });
            return 0;
        }

        public bool SetData<TEnum>(TEnum subBufferID, T[] data) where TEnum : Enum
        {
            if (_subBufferData.TryGetValue(subBufferID, out SubBufferData<T> subBufferData))
            {
                if (data.Length > subBufferData.size)
                {
                    Debug.LogError("Trying to set more data into subBuffer than it fits");
                    return false;
                }
                subBufferData.SetDefaultData(data);
                _subBufferData[subBufferID] = subBufferData;
                buffer.SetData(data, 0, (int)subBufferData.startOffset, data.Length);
                return true;
            }
            Debug.LogError($"Couldnt set data for ID: {subBufferID}");
            return false;
        }

        public void Dispose()
        {
            buffer?.Release();
            buffer = null;
        }

        public void Release()
        {
            Dispose();
            
        }
    }
}