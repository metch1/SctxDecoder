namespace SctxDecoder;

class DataReader
{
    private readonly byte[] _data;
    private int _pos;

    public DataReader(byte[] data) => _data = data;

    public long Position => _pos;
    public void Skip(int size) => _pos += size;
    public bool ReadBool() => ReadUChar() >= 1;
    public sbyte ReadChar() => (sbyte)_data[_pos++];
    public byte ReadUChar() => _data[_pos++];
    public short ReadShort() => BitConverter.ToInt16(ReadBytes(2), 0);
    public ushort ReadUShort() => BitConverter.ToUInt16(ReadBytes(2), 0);
    public int ReadInt() => BitConverter.ToInt32(ReadBytes(4), 0);
    public uint ReadUInt() => BitConverter.ToUInt32(ReadBytes(4), 0);
    public string ReadAscii()
    {
        byte size = ReadUChar();
        if (size != 0xFF)
            return Encoding.UTF8.GetString(_data, _pos, size);
        _pos += size;   // actually skip 0xFF case? In Python reads None.
        return null;
    }
    public float ReadTwip() => ReadInt() / 20.0f;

    public byte[] ReadBytes(int count)
    {
        var result = new byte[count];
        Array.Copy(_data, _pos, result, 0, count);
        _pos += count;
        return result;
    }

    public bool CanRead(int count) => _pos + count <= _data.Length;
}
