using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace AppCore
{
    public static class EaxUnifiedExtractor
    {
        public static bool TryExtract(byte[] installerData, out byte[] eaxData)
        {
            eaxData = null;
            if (installerData == null || installerData.Length == 0) return false;

            int cabOffset = FindCabinet(installerData);
            if (cabOffset < 0) return false;

            if (!new OuterCabinet(installerData, cabOffset).Extract(out byte[] headerData, out byte[] cabinetData)) return false;
            return TryExtractInstallShield(headerData, cabinetData, out eaxData);
        }

        private static int FindCabinet(byte[] data)
        {
            for (int i = 0; i <= data.Length - 4; i++)
            {
                if (data[i] == 'M' && data[i + 1] == 'S' && data[i + 2] == 'C' && data[i + 3] == 'F')
                {
                    if (i > data.Length - 36) continue;
                    uint cabinetSize = ReadUInt32(data, i + 8);
                    uint filesOffset = ReadUInt32(data, i + 16);
                    ushort folderCount = ReadUInt16(data, i + 26);
                    ushort fileCount = ReadUInt16(data, i + 28);
                    if (cabinetSize >= 36 && cabinetSize <= data.Length - i && folderCount > 0 && folderCount < 256 &&
                        fileCount > 0 && fileCount < 10000 && filesOffset >= 36 && filesOffset < cabinetSize)
                        return i;
                }
            }
            return -1;
        }

        private static bool TryExtractInstallShield(byte[] headerData, byte[] cabinetData, out byte[] eaxData)
        {
            eaxData = null;
            if (headerData == null || cabinetData == null || headerData.Length < 20) return false;
            if (Encoding.ASCII.GetString(headerData, 0, 4) != "ISc(") return false;

            uint version = ReadUInt32(headerData, 4);
            int majorVersion = (int)(version & 0xFF);
            uint descriptorOffset = ReadUInt32(headerData, 12);
            if (descriptorOffset >= headerData.Length) return false;
            if (majorVersion == 0) majorVersion = (int)((version >> 24) & 0xFF);

            int descriptor = checked((int)descriptorOffset);
            if (descriptor + 52 > headerData.Length) return false;
            uint fileTableOffset = ReadUInt32(headerData, descriptor + 12);
            ushort directoryCount = ReadUInt16(headerData, descriptor + 28);
            uint fileCount = ReadUInt32(headerData, descriptor + 44);
            uint fileTableOffset2 = ReadUInt32(headerData, descriptor + 48);
            if (fileCount == 0 || fileCount > 100000) return false;

            int offsetTableCount = majorVersion <= 5 ? checked((int)directoryCount + checked((int)fileCount)) : 0;
            int offsetTable = checked(descriptor + (int)fileTableOffset);
            if (offsetTable < 0 || offsetTable > headerData.Length - offsetTableCount * 4) return false;

            for (uint index = 0; index < fileCount; index++)
            {
                int fileDescriptor;
                if (majorVersion <= 5)
                {
                    uint relative = ReadUInt32(headerData, offsetTable + checked((int)(directoryCount + index)) * 4);
                    fileDescriptor = checked(offsetTable + (int)relative);
                }
                else
                {
                    fileDescriptor = checked(offsetTable + (int)fileTableOffset2 + checked((int)index) * 0x57);
                }

                if (fileDescriptor < 0 || fileDescriptor >= headerData.Length) return false;
                ulong expandedSize;
                ulong compressedSize;
                ulong dataOffset;
                ushort flags;
                uint nameOffset;

                if (majorVersion <= 5)
                {
                    if (fileDescriptor > headerData.Length - 0x2C) return false;
                    nameOffset = ReadUInt32(headerData, fileDescriptor);
                    flags = ReadUInt16(headerData, fileDescriptor + 8);
                    expandedSize = ReadUInt32(headerData, fileDescriptor + 10);
                    compressedSize = ReadUInt32(headerData, fileDescriptor + 14);
                    dataOffset = ReadUInt32(headerData, fileDescriptor + 0x26);
                }
                else
                {
                    if (fileDescriptor > headerData.Length - 0x57) return false;
                    flags = ReadUInt16(headerData, fileDescriptor);
                    expandedSize = ReadUInt64(headerData, fileDescriptor + 2);
                    compressedSize = ReadUInt64(headerData, fileDescriptor + 10);
                    dataOffset = ReadUInt64(headerData, fileDescriptor + 18);
                    nameOffset = ReadUInt32(headerData, fileDescriptor + 0x38);
                }

                string name = ReadString(headerData, checked(offsetTable + (int)nameOffset), majorVersion >= 17);
                if (!string.Equals(Path.GetFileName(name), "eax.dll", StringComparison.OrdinalIgnoreCase)) continue;
                if (dataOffset > (ulong)cabinetData.Length) return false;

                bool isCompressed = (flags & 4) != 0;
                ulong packedSize = isCompressed ? compressedSize : expandedSize;
                if (packedSize > (ulong)cabinetData.Length - dataOffset || expandedSize > int.MaxValue) return false;

                byte[] packed = new byte[checked((int)packedSize)];
                Buffer.BlockCopy(cabinetData, checked((int)dataOffset), packed, 0, packed.Length);
                if ((flags & 8) != 0) Deobfuscate(packed);

                if (!isCompressed)
                {
                    eaxData = packed;
                    return true;
                }

                using var output = new MemoryStream(checked((int)expandedSize));
                int inputOffset = 0;
                while (inputOffset < packed.Length)
                {
                    if (packed.Length - inputOffset < 2) return false;
                    int blockSize = ReadUInt16(packed, inputOffset);
                    inputOffset += 2;
                    if (blockSize > packed.Length - inputOffset) return false;
                    using var input = new MemoryStream(packed, inputOffset, blockSize, false);
                    using var deflate = new DeflateStream(input, CompressionMode.Decompress);
                    deflate.CopyTo(output);
                    inputOffset += blockSize;
                }

                if (output.Length != (long)expandedSize) return false;
                eaxData = output.GetBuffer();
                return true;
            }

            return false;
        }

        private static void Deobfuscate(byte[] data)
        {
            for (uint i = 0; i < data.Length; i++)
                data[i] = (byte)((((data[i] ^ 0xD5) >> 2) | ((data[i] ^ 0xD5) << 6) & 0xFF) - (i % 0x47));
        }

        private static string ReadString(byte[] data, int offset, bool unicode)
        {
            if (offset < 0 || offset >= data.Length) return string.Empty;
            if (!unicode)
            {
                int end = offset;
                while (end < data.Length && data[end] != 0) end++;
                return Encoding.ASCII.GetString(data, offset, end - offset);
            }

            int unicodeEnd = offset;
            while (unicodeEnd + 1 < data.Length && (data[unicodeEnd] != 0 || data[unicodeEnd + 1] != 0)) unicodeEnd += 2;
            return Encoding.Unicode.GetString(data, offset, unicodeEnd - offset);
        }

        private static ushort ReadUInt16(byte[] data, int offset) => BitConverter.ToUInt16(data, offset);
        private static uint ReadUInt32(byte[] data, int offset) => BitConverter.ToUInt32(data, offset);
        private static ulong ReadUInt64(byte[] data, int offset) => BitConverter.ToUInt64(data, offset);

        private sealed class OuterCabinet
        {
            private readonly byte[] _data;
            private readonly int _offset;

            public OuterCabinet(byte[] data, int offset)
            {
                _data = data;
                _offset = offset;
            }

            public bool Extract(out byte[] header, out byte[] cabinet)
            {
                header = null;
                cabinet = null;
                if (_data.Length - _offset < 36 || Encoding.ASCII.GetString(_data, _offset, 4) != "MSCF") return false;

                int baseOffset = _offset;
                uint filesOffset = ReadUInt32(_data, baseOffset + 16);
                ushort folderCount = ReadUInt16(_data, baseOffset + 26);
                ushort fileCount = ReadUInt16(_data, baseOffset + 28);
                ushort flags = ReadUInt16(_data, baseOffset + 30);
                int headerSize = 36;
                if ((flags & 4) != 0)
                {
                    if (baseOffset + headerSize + 4 > _data.Length) return false;
                    ushort headerReserve = ReadUInt16(_data, baseOffset + headerSize);
                    headerSize += 4 + headerReserve;
                }
                if ((flags & 1) != 0) return false;
                int folderTable = baseOffset + headerSize;
                if (folderCount == 0 || folderTable > _data.Length - folderCount * 8) return false;
                uint folderDataOffset = ReadUInt32(_data, folderTable);
                ushort blockCount = ReadUInt16(_data, folderTable + 4);
                ushort compression = ReadUInt16(_data, folderTable + 6);
                if ((compression & 0x000F) != 1 || blockCount == 0) return false;

                int fileTable = baseOffset + checked((int)filesOffset);
                if (fileTable < baseOffset || fileTable > _data.Length - fileCount * 16) return false;
                uint outputHeaderSize = 0, outputHeaderOffset = 0, outputCabinetSize = 0, outputCabinetOffset = 0;
                int fileEntry = fileTable;
                for (int index = 0; index < fileCount; index++)
                {
                    if (fileEntry > _data.Length - 16) return false;
                    uint size = ReadUInt32(_data, fileEntry);
                    uint offset = ReadUInt32(_data, fileEntry + 4);
                    ushort folder = ReadUInt16(_data, fileEntry + 8);
                    int nameStart = fileEntry + 16;
                    int nameEnd = nameStart;
                    while (nameEnd < _data.Length && _data[nameEnd] != 0) nameEnd++;
                    if (nameEnd == _data.Length) return false;
                    if (folder == 0)
                    {
                        if (IsCabinetFile(nameStart, nameEnd, "data1.hdr"))
                        {
                            outputHeaderSize = size;
                            outputHeaderOffset = offset;
                        }
                        else if (IsCabinetFile(nameStart, nameEnd, "data1.cab"))
                        {
                            outputCabinetSize = size;
                            outputCabinetOffset = offset;
                        }
                    }
                    fileEntry = nameEnd + 1;
                }

                int block = baseOffset + checked((int)folderDataOffset);
                using var folderOutput = new MemoryStream();
                for (int index = 0; index < blockCount; index++)
                {
                    if (block > _data.Length - 8) return false;
                    ushort compressedSize = ReadUInt16(_data, block + 4);
                    ushort expandedSize = ReadUInt16(_data, block + 6);
                    if (compressedSize < 2 || block + 8 + compressedSize > _data.Length) return false;
                    if (_data[block + 8] != 'C' || _data[block + 9] != 'K') return false;
                    if (!folderOutput.TryGetBuffer(out ArraySegment<byte> folderBuffer)) return false;
                    int outputLength = checked((int)folderOutput.Length);
                    int dictionaryLength = Math.Min(outputLength, 32768);
                    byte[] streamData = new byte[5 + dictionaryLength + compressedSize - 2];
                    streamData[0] = 0;
                    streamData[1] = (byte)dictionaryLength;
                    streamData[2] = (byte)(dictionaryLength >> 8);
                    ushort inverseLength = unchecked((ushort)~dictionaryLength);
                    streamData[3] = (byte)inverseLength;
                    streamData[4] = (byte)(inverseLength >> 8);
                    Buffer.BlockCopy(folderBuffer.Array, folderBuffer.Offset + outputLength - dictionaryLength, streamData, 5, dictionaryLength);
                    Buffer.BlockCopy(_data, block + 10, streamData, 5 + dictionaryLength, compressedSize - 2);
                    using var compressed = new MemoryStream(streamData, writable: false);
                    using var deflate = new DeflateStream(compressed, CompressionMode.Decompress);
                    using var blockOutput = new MemoryStream(dictionaryLength + expandedSize);
                    deflate.CopyTo(blockOutput);
                    if (!blockOutput.TryGetBuffer(out ArraySegment<byte> expanded) ||
                        expanded.Count < dictionaryLength || expanded.Count - dictionaryLength != expandedSize) return false;
                    folderOutput.Write(expanded.Array, expanded.Offset + dictionaryLength, expandedSize);
                    block += 8 + compressedSize;
                }

                if (outputHeaderSize == 0 || outputCabinetSize == 0 || !folderOutput.TryGetBuffer(out ArraySegment<byte> targetBuffer)) return false;
                if (!CopyFile(targetBuffer, folderOutput.Length, outputHeaderOffset, outputHeaderSize, out header) ||
                    !CopyFile(targetBuffer, folderOutput.Length, outputCabinetOffset, outputCabinetSize, out cabinet)) return false;
                return true;
            }

            private bool IsCabinetFile(int start, int end, string expected)
            {
                if (end - start == expected.Length) return string.Equals(Encoding.ASCII.GetString(_data, start, expected.Length), expected, StringComparison.OrdinalIgnoreCase);
                return end - start == expected.Length + 1 && _data[start] == '\\' && string.Equals(Encoding.ASCII.GetString(_data, start + 1, expected.Length), expected, StringComparison.OrdinalIgnoreCase);
            }

            private static bool CopyFile(ArraySegment<byte> source, long sourceLength, uint offset, uint size, out byte[] result)
            {
                result = null;
                if (offset > (ulong)sourceLength || size > (ulong)sourceLength - offset || size > int.MaxValue) return false;
                result = new byte[size];
                Buffer.BlockCopy(source.Array, source.Offset + checked((int)offset), result, 0, result.Length);
                return true;
            }
        }

    }
}
