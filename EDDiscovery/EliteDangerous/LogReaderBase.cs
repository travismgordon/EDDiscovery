﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using EDDiscovery2.DB;

namespace EDDiscovery
{
    public class LogReaderBase
    {
        // File buffer
        protected byte[] buffer;
        protected long fileptr;
        protected int bufferpos;
        protected int bufferlen;

        protected Stream stream;

        // File Information
        public string FileName { get { return Path.Combine(TravelLogUnit.Path, TravelLogUnit.Name); } }
        public long filePos { get { return TravelLogUnit.Size; } }
        public TravelLogUnit TravelLogUnit { get; protected set; }

        public LogReaderBase(string filename)
        {
            FileInfo fi = new FileInfo(filename);

            this.TravelLogUnit = new TravelLogUnit
            {
                Name = fi.Name,
                Path = fi.DirectoryName,
                Size = 0
            };

            this.stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public LogReaderBase(TravelLogUnit tlu)
        {
            this.TravelLogUnit = tlu;
            this.stream = File.Open(Path.Combine(tlu.Path, tlu.Name), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public bool ReadLine(out string line)
        {
            // Initialize buffer if not yet allocated
            if (buffer == null)
            {
                buffer = new byte[16384];
                bufferpos = 0;
                bufferlen = 0;
                fileptr = TravelLogUnit.Size;
            }

            // Loop while no end-of-line is found
            while (true)
            {
                int endlinepos = -1;

                // Only look for an end-of-line if not already at end of buffer
                if (bufferpos < bufferlen)
                {
                    // Find the next end-of-line
                    endlinepos = Array.IndexOf(buffer, (byte)'\n', bufferpos, bufferlen - bufferpos) - bufferpos;

                    // End-of-line found
                    if (endlinepos >= 0)
                    {
                        // Include the end-of-line in the line length
                        int linelen = endlinepos + 1;

                        // Trim any trailing carriage-return
                        if (endlinepos > 0 && buffer[bufferpos + endlinepos - 1] == '\r')
                        {
                            endlinepos--;
                        }

                        // Return the trimmed string
                        byte[] buf = new byte[endlinepos];
                        Buffer.BlockCopy(buffer, bufferpos, buf, 0, endlinepos);
                        bufferpos += linelen;
                        TravelLogUnit.Size += linelen;
                        line = new String(buf.Select(c => (char)c).ToArray());

                        return true;
                    }
                }

                // No end-of-line found
                // Move remaining data to start of buffer
                if (bufferpos != 0)
                {
                    Buffer.BlockCopy(buffer, bufferpos, buffer, 0, bufferlen - bufferpos);
                    bufferlen -= bufferpos;
                    bufferpos = 0;
                }

                // Expand the buffer if buffer is full
                if (bufferlen == buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length * 2);
                }

                // Read the data into the buffer
                stream.Seek(fileptr, SeekOrigin.Begin);
                int bytesread = stream.Read(buffer, bufferlen, buffer.Length - bufferlen);

                // Return false if end-of-file is encountered
                if (bytesread == 0)
                {
                    // Free the buffer if the buffer was also exhausted
                    if (bufferpos == bufferlen)
                    {
                        buffer = null;
                    }

                    line = null;
                    return false;
                }

                // Update the buffer length and next read pointer
                bufferlen += bytesread;
                fileptr += bytesread;

                // No end-of-line encountered - try reading a line again
            }
        }
    }
}
