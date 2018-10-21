﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
 
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace EasyPipes
{
    public class IpcStream : IDisposable
    {
        public Stream BaseStream { get; private set; }

        private bool _disposed = false;

        public IpcStream(Stream s)
        {
            BaseStream = s;
        }

        ~IpcStream()
        {
            if (!_disposed)
                Dispose();
        }

        public byte[] ReadBytes()
        {
            int length = BaseStream.ReadByte() * 256;
            length += BaseStream.ReadByte();

            byte[] buffer = new byte[length];
            BaseStream.Read(buffer, 0, length);

            return buffer;
        }

        public void WriteBytes(byte[] buffer)
        {
            int length = buffer.Length;
            if (length > UInt16.MaxValue)
                throw new InvalidOperationException("Message is too long");

            BaseStream.WriteByte((byte)(length / 256));
            BaseStream.WriteByte((byte)(length & 255));
            BaseStream.Write(buffer, 0, length);
            BaseStream.Flush();
        }

        public IpcMessage ReadMessage()
        {
            // read the raw message
            byte[] msg = this.ReadBytes();

            // deserialize
            DataContractSerializer serializer = new DataContractSerializer(typeof(IpcMessage));
            XmlDictionaryReader rdr = XmlDictionaryReader
                .CreateBinaryReader(msg, XmlDictionaryReaderQuotas.Max);

            return serializer.ReadObject(rdr) as IpcMessage;
        }

        public void WriteMessage(IpcMessage msg)
        {
            // serialize
            DataContractSerializer serializer = new DataContractSerializer(typeof(IpcMessage));
            using (MemoryStream stream = new MemoryStream())
            {
                XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(stream);

                serializer.WriteObject(writer, msg);
                writer.Flush();

                // write the raw message
                this.WriteBytes(stream.ToArray());
            }
        }

        public void Dispose()
        {
            BaseStream.Close();
            _disposed = true;
        }
    }
}
