using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebTvTest.RecTask
{
    public class StreamPool
    {
        public struct StreamInfo
        {
            public uint Status;
            public uint BufferLength;
            public uint FirstPos;
            public uint StreamLength;
            public ulong PacketCount;
        }
        private const uint STREAM_ENDED = 0x01;
        private const string RECTASK_STREAM_POOL_NAME = "RecTask_StreamPool_";
        private const string RECTASK_MUTEX_NAME = "_Mutex";
        private const int TS_PACKET_SIZE = 188;
        private MemoryMappedFile memoryMappedFile = null;
        private MemoryMappedViewAccessor streamInfoAccessor = null;
        private MemoryMappedViewStream tsMemoryStream = null;
        private uint taskID = 0;
        public StreamPool(uint taskID)
        {
            try
            {
                memoryMappedFile = MemoryMappedFile.OpenExisting(RECTASK_STREAM_POOL_NAME + taskID);
                streamInfoAccessor = memoryMappedFile.CreateViewAccessor(0, Marshal.SizeOf(typeof(StreamInfo)));
                tsMemoryStream = memoryMappedFile.CreateViewStream(Marshal.SizeOf(typeof(StreamInfo)), 0);
                this.taskID = taskID;
            }
            catch (Exception ex)
            {

            }
        }
        public int Read(byte[] buffer, uint bufMaxPkt, ref ulong offsetPacket)
        {
            var info = new StreamInfo();
            using (var mutex = new Mutex(true, RECTASK_STREAM_POOL_NAME + taskID + RECTASK_MUTEX_NAME))
            {
                streamInfoAccessor.Read(0, out info);
                if ((info.Status & STREAM_ENDED) != 0)
                {
                    throw new InvalidOperationException("ストリームが既に終了しています。");
                }
                if (info.FirstPos >= info.BufferLength || info.StreamLength > info.BufferLength)
                {
                    throw new InvalidOperationException("ストリームプールのヘッダが不正です。");
                }
                int read = 0;
                //uint bufMaxPkt = (uint)(buffer.Length / TS_PACKET_SIZE);
                uint copyLength = (uint)Math.Min(bufMaxPkt, info.StreamLength);
                if (copyLength <= 0) return 0;
                uint copySize = copyLength * TS_PACKET_SIZE;

                if (offsetPacket == 0)
                {
                    uint srcPos = (info.FirstPos + (info.StreamLength - copyLength)) % info.BufferLength;
                    uint size = (uint)Math.Min(copySize, (info.BufferLength - srcPos) * TS_PACKET_SIZE);
                    tsMemoryStream.Seek(srcPos * TS_PACKET_SIZE, SeekOrigin.Begin);
                    read = tsMemoryStream.Read(buffer, 0, (int)size);
                    if (size < copySize)
                    {
                        tsMemoryStream.Seek(0, SeekOrigin.Begin);
                        read += tsMemoryStream.Read(buffer, (int)size, (int)(copySize - size));
                    }
                    offsetPacket = info.PacketCount;
                    Console.Error.WriteLine("[1] OP:{0}\tSP:{1} \tS:{2}\tR:{3}", offsetPacket, srcPos, size, read);
                }
                else
                {
                    if (info.PacketCount > offsetPacket)
                    {
                        uint remainPackets = (uint)(info.PacketCount - offsetPacket);
                        uint srcPos;

                        if (remainPackets < info.StreamLength)
                        {
                            if (remainPackets < copyLength)
                            {
                                copyLength = remainPackets;
                                copySize = copyLength * TS_PACKET_SIZE;
                                //Console.Error.WriteLine("Min");
                            }
                            srcPos = (info.FirstPos + (info.StreamLength - remainPackets)) % info.BufferLength;
                        }
                        else
                        {
                            srcPos = info.FirstPos;
                            Console.Error.WriteLine("Rollback");
                        }

                        uint size = (uint)Math.Min(copySize, (info.BufferLength - srcPos) * TS_PACKET_SIZE);
                        tsMemoryStream.Seek(srcPos * TS_PACKET_SIZE, SeekOrigin.Begin);
                        read = tsMemoryStream.Read(buffer, 0, (int)size);
                        if (size < copySize)
                        {
                            tsMemoryStream.Seek(0, SeekOrigin.Begin);
                            read += tsMemoryStream.Read(buffer, (int)size, (int)(copySize - size));
                        }
                        Console.Error.WriteLine("[2] OP:{0}\tSP:{1} \tS:{2}\tR:{3}", offsetPacket, srcPos, size, read);

                        if (info.PacketCount - info.StreamLength <= offsetPacket)
                            offsetPacket += copyLength;
                        else
                            offsetPacket = info.PacketCount - (info.StreamLength - copyLength);
                    }
                }

                return read;
            }
        }
    }
}
