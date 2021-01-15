using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum EndianType
    {
        Big,
        Little,
        Native
    }

public sealed class EndianIO
    {
        private EndianType _endianness;

        private bool _requiresReverse;

        private readonly Stream _stream;

        private readonly bool _publicMemoryStream;

        private readonly byte[] _buffer = new byte[8];

        private static readonly char[] NullCharacters;

        public Stream BaseStream
        {
            get
            {
                return this._stream;
            }
        }

        public EndianType Endianness
        {
            get
            {
                return _endianness;
            }
            set
            {
                _endianness = value;
                switch (value)
                {
                    case EndianType.Native:
                        _requiresReverse = false;
                        break;
                    case EndianType.Little:
                        _requiresReverse = !BitConverter.IsLittleEndian;
                        break;
                    case EndianType.Big:
                        _requiresReverse = BitConverter.IsLittleEndian;
                        break;
                }
            }
        }

        public bool Eof
        {
            get
            {
                return this.Position == this.Length;
            }
        }

        public long Length
        {
            get
            {
                return this._stream.Length;
            }
        }

        public long Position
        {
            get
            {
                return _stream.Position;
            }
            set
            {
                _stream.Position = value;
            }
        }

        public EndianIO(Stream stream, EndianType endianType = EndianType.Native)
        {
            _stream = stream;
            Endianness = endianType;
        }

        public EndianIO(EndianType endianType = EndianType.Native)
            : this(new MemoryStream(), endianType)
        {
            _publicMemoryStream = true;
        }

        public EndianIO(int maxSize, EndianType endianType = EndianType.Native)
            : this(new MemoryStream(maxSize), endianType)
        {
            _publicMemoryStream = true;
        }

        public EndianIO(byte[] buffer, EndianType endianType = EndianType.Native)
            : this(new MemoryStream(buffer), endianType)
        {
        }

        public EndianIO(string fileName, EndianType endianType = EndianType.Native, FileMode fileMode = FileMode.Open, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.Read, int bufferSize = 4096, bool isAsync = false)
            : this(new FileStream(fileName, fileMode, fileAccess, fileShare, bufferSize, isAsync), endianType)
        {
        }

        public void Seek(long position, SeekOrigin origin)
        {
            _stream.Seek(position, origin);
        }

        public void Flush()
        {
            _stream.Flush();
        }

        public void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public Stream GetStream()
        {
            return _stream;
        }

        public void Close()
        {
            _stream.Close();
        }

        public byte[] ToArray(bool preferInternalReference = false)
        {
            MemoryStream memoryStream = _stream as MemoryStream;
            if (memoryStream == null)
            {
                throw new NotSupportedException("ToArray() is only supported by memory streams.");
            }
            return (preferInternalReference && _publicMemoryStream) ? memoryStream.GetBuffer() : memoryStream.ToArray();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            return await _stream.ReadAsync(buffer, offset, count);
        }

        public byte[] ReadByteArray(int count)
        {
            byte[] array = new byte[count];
            Read(array, 0, count);
            return array;
        }

        public async Task<byte[]> ReadByteArrayAsync(int count)
        {
            byte[] buffer = new byte[count];
            await ReadAsync(buffer, 0, count);
            return buffer;
        }

        public byte[] ReadByteArray(long count)
        {
            byte[] array = new byte[count];
            Read(array, 0, array.Length);
            return array;
        }

        public byte[] ReadToEnd()
        {
            using (MemoryStream memoryStream = new MemoryStream(81920))
            {
                _stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public async Task<byte[]> ReadToEndAsync()
        {
            using (MemoryStream ms = new MemoryStream(81920))
            {
                await _stream.CopyToAsync(ms);
                return ms.ToArray();
            }
        }

        public sbyte ReadSByte()
        {
            Read(_buffer, 0, 1);
            return (sbyte)_buffer[0];
        }

        public byte ReadByte()
        {
            Read(_buffer, 0, 1);
            return _buffer[0];
        }

        public bool ReadBoolean()
        {
            return ReadByte() != 0;
        }

        public ushort ReadUInt16()
        {
            return (ushort)ReadInt16();
        }

        public unsafe short ReadInt16()
        {
            Read(_buffer, 0, 2);
            if (_requiresReverse)
            {
                return (short)(_buffer[1] | (_buffer[0] << 8));
            }
            fixed (byte* ptr = &_buffer[0])
            {
                return *(short*)ptr;
            }
        }

        public uint ReadUInt32()
        {
            return (uint)ReadInt32();
        }

        public unsafe int ReadInt32()
        {
            Read(_buffer, 0, 4);
            if (_requiresReverse)
            {
                return _buffer[3] | (_buffer[2] << 8) | (_buffer[1] << 16) | (_buffer[0] << 24);
            }
            fixed (byte* ptr = &_buffer[0])
            {
                return *(int*)ptr;
            }
        }

        public async Task<int> ReadInt32Async()
        {
            await ReadAsync(_buffer, 0, 4);
            if (_requiresReverse)
            {
                return _buffer[3] | (_buffer[2] << 8) | (_buffer[1] << 16) | (_buffer[0] << 24);
            }
            return BitConverter.ToInt32(_buffer, 0);
        }

        public ulong ReadUInt64()
        {
            return (ulong)ReadInt64();
        }

        public unsafe long ReadInt64()
        {
            Read(_buffer, 0, 8);
            if (_requiresReverse)
            {
                long num = (_buffer[3] | (_buffer[2] << 8) | (_buffer[1] << 16) | (_buffer[0] << 24)) & uint.MaxValue;
                long num2 = (_buffer[7] | (_buffer[6] << 8) | (_buffer[5] << 16) | (_buffer[4] << 24)) & uint.MaxValue;
                return num2 | (num << 32);
            }
            fixed (byte* ptr = &_buffer[0])
            {
                return *(long*)ptr;
            }
        }

        public uint ReadUInt24()
        {
            Read(_buffer, 0, 3);
            if (!_requiresReverse)
            {
                return (uint)(_buffer[0] | (_buffer[1] << 8) | (_buffer[2] << 16));
            }
            return (uint)(_buffer[2] | (_buffer[1] << 8) | (_buffer[0] << 16));
        }

        public unsafe float ReadSingle()
        {
            int num = ReadInt32();
            return *(float*)(&num);
        }

        public unsafe double ReadDouble()
        {
            long num = ReadInt64();
            return *(double*)(&num);
        }

        public string ReadString(Encoding encoding, int lengthInBytes)
        {
            return encoding.GetString(ReadByteArray(lengthInBytes));
        }

        public async Task<string> ReadStringAsync(Encoding encoding, int lengthInBytes)
        {
            return encoding.GetString(await ReadByteArrayAsync(lengthInBytes));
        }

        public string ReadNullTerminatedString(Encoding encoding)
{
	StringBuilder stringBuilder = new StringBuilder();
	StreamReader streamReader = new StreamReader(this._stream, encoding, false, 16, true);
	int num;
	while ((num = streamReader.Read()) != -1 && num != 0)
	{
		stringBuilder.Append((char)num);
	}
	streamReader.Close();
	return stringBuilder.ToString();
}

        public void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        public void Write(bool value)
        {
            Write((byte)(value ? 1 : 0));
        }

        public void Write(sbyte value)
        {
            Write((byte)value);
        }

        public void Write(byte value)
        {
            _buffer[0] = value;
            Write(_buffer, 0, 1);
        }

        public void Write(short value)
        {
            Write((ushort)value);
        }

        public void Write(ushort value)
        {
            if (_requiresReverse)
            {
                value = (ushort)((value << 8) | (value >> 8));
            }
            Write(BitConverter.GetBytes(value));
        }

        public void Write(int value)
        {
            Write((uint)value);
        }

        public void Write(uint value)
        {
            if (_requiresReverse)
            {
                value = ((value << 24) | (value >> 24) | ((value & 0xFF00) << 8) | ((value >> 8) & 0xFF00));
            }
            Write(BitConverter.GetBytes(value));
        }

        public void Write(long value)
        {
            Write((ulong)value);
        }

        public void Write(ulong value)
        {
            if (_requiresReverse)
            {
                value = ((value << 56) | (value >> 56) | ((value & 0xFF00) << 40) | ((value >> 40) & 0xFF00) | ((value & 0xFF0000) << 24) | ((value >> 24) & 0xFF0000) | ((value & 4278190080u) << 8) | ((value >> 8) & 4278190080u));
            }
            Write(BitConverter.GetBytes(value));
        }

        public unsafe void Write(float value)
        {
            Write(*(uint*)(&value));
        }

        public unsafe void Write(double value)
        {
            Write((ulong)(*(long*)(&value)));
        }

        public void WriteUInt24(uint value)
        {
            byte[] array = BitConverter.GetBytes(value);
            Array.Resize(ref array, 3);
            if (_requiresReverse)
            {
                byte b = array[0];
                array[0] = array[2];
                array[2] = b;
            }
            Write(array);
        }

        public void Write(string value, Encoding encoding)
        {
            if (value.Length != 0)
            {
                Write(encoding.GetBytes(value));
            }
        }

        public void Write(string value, Encoding encoding, int fixedLength)
        {
            if (fixedLength < 0)
            {
                throw new ArgumentOutOfRangeException("fixedLength");
            }
            if (fixedLength != 0 && value.Length != 0)
            {
                if (value.Length > fixedLength)
                {
                    value = value.Substring(0, fixedLength);
                }
                else if (value.Length < fixedLength)
                {
                    value += new string('\0', fixedLength - value.Length);
                }
                Write(encoding.GetBytes(value));
            }
        }

        public void WriteNullTerminatedString(string value, Encoding encoding)
        {
            Write(encoding.GetBytes(value));
            Write(encoding.GetBytes(NullCharacters));
        }

        static EndianIO()
        {
            char[] array = NullCharacters = new char[1];
        }
    }

