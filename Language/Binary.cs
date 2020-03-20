﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Inu.Language
{
    static class Binary
    {
        public static void WriteByte(this Stream stream, int value)
        {
            stream.WriteByte((byte)value);
        }

        public static void WriteWord(this Stream stream, int value)
        {
            stream.WriteByte(value);
            stream.WriteByte(value >> 8);
        }

        public static void WriteString(this Stream stream, string s)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            stream.WriteWord(bytes.Length);
            foreach (byte b in bytes) {
                stream.WriteByte(b);
            }
        }

        public static int ReadWord(this Stream stream)
        {
            int l = stream.ReadByte();
            int h = stream.ReadByte();
            return l | (h << 8);
        }

        public static string ReadString(this Stream stream)
        {
            int n = stream.ReadWord();
            StringBuilder s = new StringBuilder();
            for (int i = 0; i < n; ++i) {
                char c = (char)(stream.ReadByte());
                s.Append(c);
            }
            return s.ToString();
        }
    }
}