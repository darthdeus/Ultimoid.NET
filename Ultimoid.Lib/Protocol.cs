﻿using System;
using System.Diagnostics;
using System.IO;

namespace Ultimoid.Lib {
    public struct Datagram {
        public UInt64 Seq;
        public UInt32 Ack;
        public byte[] Payload;
    }

    public static class Protocol {
        public const int MagicHeaderLength = 5;

        private static readonly byte[] MagicBytes = new byte[] {0xCA, 0xFF, 0xEE, 0xFF, 0xAC};

        const int SeqLength = sizeof(UInt64);
        const int AckLength = sizeof(UInt32);
        const int HeaderLength = MagicHeaderLength + SeqLength + AckLength;
        const int MaxDatagramLength = 256;

        public static Datagram Deserialize(byte[] networkData) {
            if (networkData.Length > MaxDatagramLength) {
                throw new ArgumentException($"Datagram longer than maximum allowed size (actual size: ${networkData.Length}",
                    nameof(networkData));
            }

            if (!IsMagicNumberValid(networkData)) {
                throw new ArgumentException($"Attempting to deserialize a datagram in an INVALID format",
                    nameof(networkData));
            }

            if (networkData.Length - HeaderLength < 0) {
                throw new ArgumentException(
                    $"Network data is shorter than header size (size: ${networkData.Length}, header size: {HeaderLength}",
                    nameof(networkData));
            }

            Datagram result;

            byte[] seqBytes = new byte[SeqLength];
            Array.Copy(networkData, MagicHeaderLength, seqBytes, 0, seqBytes.Length);
            seqBytes.ReverseIfLittleEndian();

            result.Seq = BitConverter.ToUInt64(seqBytes, 0);

            byte[] ackBytes = new byte[AckLength];
            Array.Copy(networkData, MagicHeaderLength + SeqLength, ackBytes, 0, ackBytes.Length);
            ackBytes.ReverseIfLittleEndian();

            result.Ack = BitConverter.ToUInt32(ackBytes, 0);

            byte[] payload = new byte[networkData.Length - HeaderLength];
            Array.Copy(networkData, HeaderLength, payload, 0, payload.Length);

            result.Payload = payload;

            return result;
        }

        public static byte[] Serialize(Datagram datagram) {
            return Serialize(datagram.Payload, datagram.Seq, datagram.Ack);
        }

        public static byte[] Serialize(byte[] payload, UInt64 seq, UInt32 ack) {
            var stream = new MemoryStream();

            // Magic header
            stream.Write(MagicBytes, 0, MagicBytes.Length);

            Debug.Assert(stream.Length == MagicHeaderLength);

            byte[] seqBytes = BitConverter.GetBytes(seq);
            seqBytes.ReverseIfLittleEndian();
            stream.Write(seqBytes, 0, seqBytes.Length);

            byte[] ackBytes = BitConverter.GetBytes(ack);
            ackBytes.ReverseIfLittleEndian();
            stream.Write(ackBytes, 0, ackBytes.Length);

            stream.Write(payload, 0, payload.Length);

            return stream.ToArray();
        }

        public static bool IsMagicNumberValid(byte[] data) {
            return data[0] == MagicBytes[0] &&
                   data[1] == MagicBytes[1] &&
                   data[2] == MagicBytes[2] &&
                   data[3] == MagicBytes[3] &&
                   data[4] == MagicBytes[4];
        }

        public static void ReverseIfLittleEndian(this Array arr) {
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(arr);
            }
        }
    }
}