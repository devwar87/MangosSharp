﻿// 
// Copyright (C) 2013-2020 getMaNGOS <https://getmangos.eu>
// 
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
// 

using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using Mangos.Cluster.Server;
using Mangos.Common.Enums.Global;
using Mangos.Common.Globals;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Mangos.Cluster.Globals
{
    public class Packets
    {
        public void DumpPacket(byte[] data, [Optional, DefaultParameterValue(null)] WC_Network.ClientClass client)
        {
            // #If DEBUG Then
            int j;
            string buffer = "";
            try
            {
                buffer = client is null ? buffer + string.Format("DEBUG: Packet Dump{0}", Constants.vbCrLf) : buffer + string.Format("[{0}:{1}] DEBUG: Packet Dump - Length={2}{3}", client.IP, client.Port, data.Length, Constants.vbCrLf);
                if (data.Length % 16 == 0)
                {
                    var loopTo = data.Length - 1;
                    for (j = 0; j <= loopTo; j += 16)
                    {
                        buffer += "|  " + BitConverter.ToString(data, j, 16).Replace("-", " ");
                        buffer += " |  " + System.Text.Encoding.ASCII.GetString(data, j, 16).Replace(Constants.vbTab, "?").Replace(Constants.vbBack, "?").Replace(Constants.vbCr, "?").Replace(Constants.vbFormFeed, "?").Replace(Constants.vbLf, "?") + " |" + Constants.vbCrLf;
                    }
                }
                else
                {
                    var loopTo1 = data.Length - 1 - 16;
                    for (j = 0; j <= loopTo1; j += 16)
                    {
                        buffer += "|  " + BitConverter.ToString(data, j, 16).Replace("-", " ");
                        buffer += " |  " + System.Text.Encoding.ASCII.GetString(data, j, 16).Replace(Constants.vbTab, "?").Replace(Constants.vbBack, "?").Replace(Constants.vbCr, "?").Replace(Constants.vbFormFeed, "?").Replace(Constants.vbLf, "?") + " |" + Constants.vbCrLf;
                    }

                    buffer += "|  " + BitConverter.ToString(data, j, data.Length % 16).Replace("-", " ");
                    buffer += new string(' ', (16 - data.Length % 16) * 3);
                    buffer += " |  " + System.Text.Encoding.ASCII.GetString(data, j, data.Length % 16).Replace(Constants.vbTab, "?").Replace(Constants.vbBack, "?").Replace(Constants.vbCr, "?").Replace(Constants.vbFormFeed, "?").Replace(Constants.vbLf, "?");
                    buffer += new string(' ', 16 - data.Length % 16);
                    buffer += " |" + Constants.vbCrLf;
                }

                ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DEBUG, buffer, default);
            }
            // #End If
            catch (Exception e)
            {
                ClusterServiceLocator._WorldCluster.Log.WriteLine(LogType.FAILED, "Error dumping packet: {0}{1}", Constants.vbCrLf, e.ToString());
            }
        }

        public void LogPacket(byte[] data, bool Server, [Optional, DefaultParameterValue(null)] WC_Network.ClientClass client)
        {
            int j;
            string buffer = "";
            try
            {
                OPCODES opcode = (OPCODES)BitConverter.ToInt16(data, 2);
                if (IgnorePacket(opcode))
                    return;
                int StartAt = 6;
                if (Server)
                    StartAt = 4;
                string TypeStr = "IN";
                if (Server)
                    TypeStr = "OUT";
                if (client is null)
                {
                    buffer += string.Format("{4} Packet: (0x{0:X4}) {1} PacketSize = {2}{3}", opcode, opcode, data.Length - StartAt, Constants.vbCrLf, TypeStr);
                }
                else
                {
                    buffer += string.Format("[{0}:{1}] {6} Packet: (0x{2:X4}) {3} PacketSize = {4}{5}", client.IP, client.Port, opcode, opcode, data.Length - StartAt, Constants.vbCrLf, TypeStr);
                }

                buffer += "|------------------------------------------------|----------------|" + Constants.vbCrLf;
                buffer += "|00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F |0123456789ABCDEF|" + Constants.vbCrLf;
                buffer += "|------------------------------------------------|----------------|" + Constants.vbCrLf;
                var loopTo = data.Length - 1;
                for (j = StartAt; j <= loopTo; j += 16)
                {
                    if (j + 16 > data.Length)
                    {
                        buffer += "|" + BitConverter.ToString(data, j, data.Length - j).Replace("-", " ");
                        buffer += new string(' ', (j + 16 - data.Length) * 3);
                        buffer += " |" + FormatPacketStr(System.Text.Encoding.ASCII.GetString(data, j, data.Length - j));
                        buffer += new string(' ', j + 16 - data.Length);
                    }
                    else
                    {
                        buffer += "|" + BitConverter.ToString(data, j, 16).Replace("-", " ");
                        buffer += " |" + FormatPacketStr(System.Text.Encoding.ASCII.GetString(data, j, 16));
                    }

                    buffer += "|" + Constants.vbCrLf;
                }

                buffer += "-------------------------------------------------------------------" + Constants.vbCrLf + Constants.vbCrLf;
                File.AppendAllText("packets.log", buffer);
            }
            catch (Exception)
            {
            }
        }

        private bool IgnorePacket(OPCODES opcode)
        {
            if (string.Format("{0}", opcode).StartsWith("MSG_MOVE"))
                return true;
            switch (opcode)
            {
                case var @case when @case == OPCODES.SMSG_MONSTER_MOVE:
                case var case1 when case1 == OPCODES.SMSG_UPDATE_OBJECT:
                    {
                        return true;
                    }

                default:
                    {
                        return false;
                    }
            }
        }

        private string FormatPacketStr(string str)
        {
            for (int i = 0, loopTo = str.ToCharArray().Length - 1; i <= loopTo; i++)
            {
                if (str.ToCharArray()[i] < 'A' || str.ToCharArray()[i] > 'z')
                {
                    str.ToCharArray()[i] = '.';
                }
            }

            return Conversions.ToString(str.ToCharArray());
        }

        public class PacketClass : IDisposable
        {
            public byte[] Data;
            public int Offset = 4;

            public int Length
            {
                get
                {
                    return Data[1] + Data[0] * 256;
                }
            }

            public OPCODES OpCode
            {
                get
                {
                    if (Information.UBound(Data) > 2)
                    {
                        return (OPCODES)(Data[2] + Data[3] * 256);
                    }
                    else
                    {
                        // If it's a dodgy packet, change it to a null packet
                        return 0;
                    }
                }
            }

            public PacketClass(OPCODES opcode)
            {
                Array.Resize(ref Data, 4);
                Data[0] = 0;
                Data[1] = 2;
                Data[2] = (byte)(Conversions.ToShort(opcode) % 256);
                Data[3] = (byte)(Conversions.ToShort(opcode) / 256);
            }

            public PacketClass(byte[] rawdata)
            {
                Data = rawdata;
            }

            // Public Sub AddBitArray(ByVal buffer As BitArray, ByVal Len As Integer)
            // ReDim Preserve Data(Data.Length - 1 + Len)
            // Data(0) = (Data.Length - 2) \ 256
            // Data(1) = (Data.Length - 2) Mod 256

            // Dim bufferarray(CType((buffer.Length + 8) / 8, Byte)) As Byte

            // buffer.CopyTo(bufferarray, 0)
            // Array.Copy(bufferarray, 0, Data, Data.Length - Len, Len)
            // End Sub

            public void AddInt8(byte buffer)
            {
                Array.Resize(ref Data, Data.Length + 1);
                Data[0] = (byte)((Data.Length - 2) / 256);
                Data[1] = (byte)((Data.Length - 2) % 256);
                Data[Data.Length - 1] = buffer;
            }

            public void AddInt16(short buffer)
            {
                Array.Resize(ref Data, Data.Length + 1 + 1);
                Data[0] = (byte)((Data.Length - 2) / 256);
                Data[1] = (byte)((Data.Length - 2) % 256);
                Data[Data.Length - 2] = (byte)(buffer & 255);
                Data[Data.Length - 1] = (byte)(buffer >> 8 & 255);
            }

            public void AddInt32(int buffer, int position = 0)
            {
                if (position <= 0 || position > Data.Length - 3)
                {
                    position = Data.Length;
                    Array.Resize(ref Data, Data.Length + 3 + 1);
                    Data[0] = (byte)((Data.Length - 2) / 256);
                    Data[1] = (byte)((Data.Length - 2) % 256);
                }

                Data[position] = (byte)(buffer & 255);
                Data[position + 1] = (byte)(buffer >> 8 & 255);
                Data[position + 2] = (byte)(buffer >> 16 & 255);
                Data[position + 3] = (byte)(buffer >> 24 & 255);
            }

            public void AddInt64(long buffer)
            {
                Array.Resize(ref Data, Data.Length + 7 + 1);
                Data[0] = (byte)((Data.Length - 2) / 256);
                Data[1] = (byte)((Data.Length - 2) % 256);
                Data[Data.Length - 8] = (byte)(buffer & 255L);
                Data[Data.Length - 7] = (byte)(buffer >> 8 & 255L);
                Data[Data.Length - 6] = (byte)(buffer >> 16 & 255L);
                Data[Data.Length - 5] = (byte)(buffer >> 24 & 255L);
                Data[Data.Length - 4] = (byte)(buffer >> 32 & 255L);
                Data[Data.Length - 3] = (byte)(buffer >> 40 & 255L);
                Data[Data.Length - 2] = (byte)(buffer >> 48 & 255L);
                Data[Data.Length - 1] = (byte)(buffer >> 56 & 255L);
            }

            public void AddString(string buffer)
            {
                if (Information.IsDBNull(buffer) | string.IsNullOrEmpty(buffer))
                {
                    AddInt8(0);
                }
                else
                {
                    var Bytes = System.Text.Encoding.UTF8.GetBytes(buffer.ToCharArray());
                    Array.Resize(ref Data, Data.Length + Bytes.Length + 1);
                    Data[0] = (byte)((Data.Length - 2) / 256);
                    Data[1] = (byte)((Data.Length - 2) % 256);
                    for (int i = 0, loopTo = Bytes.Length - 1; i <= loopTo; i++)
                        Data[Data.Length - 1 - Bytes.Length + i] = Bytes[i];
                    Data[Data.Length - 1] = 0;
                }
            }

            public void AddDouble(double buffer2)
            {
                var buffer1 = BitConverter.GetBytes(buffer2);
                Array.Resize(ref Data, Data.Length + buffer1.Length);
                Buffer.BlockCopy(buffer1, 0, Data, Data.Length - buffer1.Length, buffer1.Length);
                Data[0] = (byte)((Data.Length - 2) / 256);
                Data[1] = (byte)((Data.Length - 2) % 256);
            }

            public void AddSingle(float buffer2)
            {
                var buffer1 = BitConverter.GetBytes(buffer2);
                Array.Resize(ref Data, Data.Length + buffer1.Length);
                Buffer.BlockCopy(buffer1, 0, Data, Data.Length - buffer1.Length, buffer1.Length);
                Data[0] = (byte)((Data.Length - 2) / 256);
                Data[1] = (byte)((Data.Length - 2) % 256);
            }

            public void AddByteArray(byte[] buffer)
            {
                int tmp = Data.Length;
                Array.Resize(ref Data, Data.Length + buffer.Length);
                Array.Copy(buffer, 0, Data, tmp, buffer.Length);
                Data[0] = (byte)((Data.Length - 2) / 256);
                Data[1] = (byte)((Data.Length - 2) % 256);
            }

            public void AddPackGUID(ulong buffer)
            {
                var GUID = BitConverter.GetBytes(buffer);
                var flags = new BitArray(8);
                int offsetStart = Data.Length;
                int offsetNewSize = offsetStart;
                for (byte i = 0; i <= 7; i++)
                {
                    flags[i] = GUID[i] != 0;
                    if (flags[i])
                        offsetNewSize += 1;
                }

                Array.Resize(ref Data, offsetNewSize + 1);
                flags.CopyTo(Data, offsetStart);
                offsetStart += 1;
                for (byte i = 0; i <= 7; i++)
                {
                    if (flags[i])
                    {
                        Data[offsetStart] = GUID[i];
                        offsetStart += 1;
                    }
                }
            }

            // Public Sub AddUInt8(ByVal buffer As Byte)
            // ReDim Preserve Data(Data.Length + 1)
            // 
            // Data(Data.Length - 1) = CType(((buffer >> 8) And 255), Byte)
            // End Sub

            public ushort GetUInt8()
            {
                ushort num1 = (ushort)(Data.Length + 1);
                Offset += 1;
                return num1;
            }

            public void AddUInt16(ushort buffer)
            {
                Array.Resize(ref Data, Data.Length + 1 + 1);
                Data[0] = (byte)((Data.Length - 2) / 256);
                Data[1] = (byte)((Data.Length - 2) % 256);
                Data[Data.Length - 2] = (byte)(buffer & 255);
                Data[Data.Length - 1] = (byte)(buffer >> 8 & 255);
            }

            public void AddUInt32(uint buffer)
            {
                Array.Resize(ref Data, Data.Length + 3 + 1);
                Data[0] = (byte)((Data.Length - 2) / 256);
                Data[1] = (byte)((Data.Length - 2) % 256);
                Data[Data.Length - 4] = (byte)(buffer & 255L);
                Data[Data.Length - 3] = (byte)(buffer >> 8 & 255L);
                Data[Data.Length - 2] = (byte)(buffer >> 16 & 255L);
                Data[Data.Length - 1] = (byte)(buffer >> 24 & 255L);
            }

            public void AddUInt64(ulong buffer)
            {
                Array.Resize(ref Data, Data.Length + 7 + 1);
                Data[0] = (byte)((Data.Length - 2) / 256);
                Data[1] = (byte)((Data.Length - 2) % 256);
                Data[Data.Length - 8] = (byte)((long)buffer & 255L);
                Data[Data.Length - 7] = (byte)((long)(buffer >> 8) & 255L);
                Data[Data.Length - 6] = (byte)((long)(buffer >> 16) & 255L);
                Data[Data.Length - 5] = (byte)((long)(buffer >> 24) & 255L);
                Data[Data.Length - 4] = (byte)((long)(buffer >> 32) & 255L);
                Data[Data.Length - 3] = (byte)((long)(buffer >> 40) & 255L);
                Data[Data.Length - 2] = (byte)((long)(buffer >> 48) & 255L);
                Data[Data.Length - 1] = (byte)((long)(buffer >> 56) & 255L);
            }

            public byte GetInt8()
            {
                Offset += 1;
                return Data[Offset - 1];
            }

            // Public Function GetInt8(ByVal Offset As Integer) As Byte
            // Offset = Offset + 1
            // Return Data(Offset - 1)
            // End Function

            public short GetInt16()
            {
                short num1 = BitConverter.ToInt16(Data, Offset);
                Offset += 2;
                return num1;
            }

            // Public Function GetInt16(ByVal Offset As Integer) As Short
            // Dim num1 As Short = BitConverter.ToInt16(Data, Offset)
            // Offset = (Offset + 2)
            // Return num1
            // End Function

            public int GetInt32()
            {
                int num1 = BitConverter.ToInt32(Data, Offset);
                Offset += 4;
                return num1;
            }

            // Public Function GetInt32(ByVal Offset As Integer) As Integer
            // Dim num1 As Integer = BitConverter.ToInt32(Data, Offset)
            // Offset = (Offset + 4)
            // Return num1
            // End Function

            public long GetInt64()
            {
                long num1 = BitConverter.ToInt64(Data, Offset);
                Offset += 8;
                return num1;
            }

            // Public Function GetInt64(ByVal Offset As Integer) As Long
            // Dim num1 As Long = BitConverter.ToInt64(Data, Offset)
            // Offset = (Offset + 8)
            // Return num1
            // End Function

            public float GetFloat()
            {
                float single1 = BitConverter.ToSingle(Data, Offset);
                Offset += 4;
                return single1;
            }

            // Public Function GetFloat(ByVal Offset_ As Integer) As Single
            // Dim single1 As Single = BitConverter.ToSingle(Data, Offset)
            // Offset = (Offset_ + 4)
            // Return single1
            // End Function

            public double GetDouble()
            {
                double num1 = BitConverter.ToDouble(Data, Offset);
                Offset += 8;
                return num1;
            }

            // Public Function GetDouble(ByVal Offset As Integer) As Double
            // Dim num1 As Double = BitConverter.ToDouble(Data, Offset)
            // Offset = (Offset + 8)
            // Return num1
            // End Function

            public string GetString()
            {
                int start = Offset;
                int i = 0;
                while (Data[start + i] != 0)
                {
                    i += 1;
                    Offset += 1;
                }

                Offset += 1;
                return System.Text.Encoding.UTF8.GetString(Data, start, i);
            }

            // Public Function GetString(ByVal Offset As Integer) As String
            // Dim i As Integer = Offset
            // Dim tmpString As String = ""
            // While Data(i) <> 0
            // tmpString = tmpString + Chr(Data(i))
            // i = i + 1
            // Offset = Offset + 1
            // End While
            // Offset = Offset + 1
            // Return tmpString
            // End Function

            public ushort GetUInt16()
            {
                ushort num1 = BitConverter.ToUInt16(Data, Offset);
                Offset += 2;
                return num1;
            }

            // Public Function GetUInt16(ByVal Offset As Integer) As UShort
            // Dim num1 As UShort = BitConverter.ToUInt16(Data, Offset)
            // Offset = (Offset + 2)
            // Return num1
            // End Function

            public uint GetUInt32()
            {
                uint num1 = BitConverter.ToUInt32(Data, Offset);
                Offset += 4;
                return num1;
            }

            // Public Function GetUInt32(ByVal Offset As Integer) As UInteger
            // Dim num1 As UInteger = BitConverter.ToUInt32(Data, Offset)
            // Offset = (Offset + 4)
            // Return num1
            // End Function

            public ulong GetUInt64()
            {
                ulong num1 = BitConverter.ToUInt64(Data, Offset);
                Offset += 8;
                return num1;
            }

            // Public Function GetUInt64(ByVal Offset As Integer) As ULong
            // Dim num1 As ULong = BitConverter.ToUInt64(Data, Offset)
            // Offset = (Offset + 8)
            // Return num1
            // End Function

            // Public Function GetPackGUID() As ULong
            // Dim flags As Byte = Data(Offset)
            // Dim GUID() As Byte = {0, 0, 0, 0, 0, 0, 0, 0}
            // Offset += 1

            // If (flags And 1) = 1 Then
            // GUID(0) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 2) = 2 Then
            // GUID(1) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 4) = 4 Then
            // GUID(2) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 8) = 8 Then
            // GUID(3) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 16) = 16 Then
            // GUID(4) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 32) = 32 Then
            // GUID(5) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 64) = 64 Then
            // GUID(6) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 128) = 128 Then
            // GUID(7) = Data(Offset)
            // Offset += 1
            // End If

            // Return CType(BitConverter.ToUInt64(GUID, 0), ULong)
            // End Function

            // Public Function GetPackGUID(ByVal Offset As Integer) As ULong
            // Dim flags As Byte = Data(Offset)
            // Dim GUID() As Byte = {0, 0, 0, 0, 0, 0, 0, 0}
            // Offset += 1

            // If (flags And 1) = 1 Then
            // GUID(0) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 2) = 2 Then
            // GUID(1) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 4) = 4 Then
            // GUID(2) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 8) = 8 Then
            // GUID(3) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 16) = 16 Then
            // GUID(4) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 32) = 32 Then
            // GUID(5) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 64) = 64 Then
            // GUID(6) = Data(Offset)
            // Offset += 1
            // End If
            // If (flags And 128) = 128 Then
            // GUID(7) = Data(Offset)
            // Offset += 1
            // End If

            // Return CType(BitConverter.ToUInt64(GUID, 0), ULong)
            // End Function

            /* TODO ERROR: Skipped RegionDirectiveTrivia */
            private bool _disposedValue; // To detect redundant calls

            // IDisposable
            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    // TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                    // TODO: set large fields to null.
                }

                _disposedValue = true;
            }

            // This code added by Visual Basic to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
        }
    }
}