﻿using YAF.Lucene.Net.Diagnostics;
using YAF.Lucene.Net.Store;
using YAF.Lucene.Net.Support.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace YAF.Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// On-disk sorting of byte arrays. Each byte array (entry) is a composed of the following
    /// fields:
    /// <list type="bullet">
    ///   <item><description>(two bytes) length of the following byte array,</description></item>
    ///   <item><description>exactly the above count of bytes for the sequence to be sorted.</description></item>
    /// </list>
    /// </summary>
    public sealed class OfflineSorter
    {
        /// <summary>
        /// Convenience constant for megabytes </summary>
        public const long MB = 1024 * 1024;
        /// <summary>
        /// Convenience constant for gigabytes </summary>
        public const long GB = MB * 1024;

        /// <summary>
        /// Minimum recommended buffer size for sorting.
        /// </summary>
        public const long MIN_BUFFER_SIZE_MB = 32;

        /// <summary>
        /// Absolute minimum required buffer size for sorting.
        /// </summary>
        public const long ABSOLUTE_MIN_SORT_BUFFER_SIZE = MB / 2;
        private const string MIN_BUFFER_SIZE_MSG = "At least 0.5MB RAM buffer is needed";

        /// <summary>
        /// Maximum number of temporary files before doing an intermediate merge.
        /// </summary>
        public const int MAX_TEMPFILES = 128;

        /// <summary>
        /// A bit more descriptive unit for constructors.
        /// </summary>
        /// <seealso cref="Automatic()"/>
        /// <seealso cref="Megabytes(long)"/>
        public sealed class BufferSize
        {
            internal readonly int bytes;

            private BufferSize(long bytes)
            {
                if (bytes > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(bytes), "Buffer too large for .NET (" + (int.MaxValue / MB) + "mb max): " + bytes); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }

                if (bytes < ABSOLUTE_MIN_SORT_BUFFER_SIZE)
                {
                    throw new ArgumentOutOfRangeException(nameof(bytes), MIN_BUFFER_SIZE_MSG + ": " + bytes); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }

                this.bytes = (int)bytes;
            }

            /// <summary>
            /// Creates a <see cref="BufferSize"/> in MB. The given
            /// values must be &gt; 0 and &lt; 2048.
            /// </summary>
            public static BufferSize Megabytes(long mb)
            {
                return new BufferSize(mb * MB);
            }

            /// <summary>
            /// Approximately half of the currently available free heap, but no less
            /// than <see cref="ABSOLUTE_MIN_SORT_BUFFER_SIZE"/>. However if current heap allocation
            /// is insufficient or if there is a large portion of unallocated heap-space available
            /// for sorting consult with max allowed heap size.
            /// </summary>
            public static BufferSize Automatic()
            {
                long max, total, free;
                using (var proc = Process.GetCurrentProcess())
                {
                    // take sizes in "conservative" order
                    max = proc.PeakVirtualMemorySize64; // max allocated; java has it as Runtime.maxMemory();
                    total = proc.VirtualMemorySize64; // currently allocated; java has it as Runtime.totalMemory();
                    free = proc.PrivateMemorySize64; // unused portion of currently allocated; java has it as Runtime.freeMemory();
                }
                long totalAvailableBytes = max - total + free;

                // by free mem (attempting to not grow the heap for this)
                long sortBufferByteSize = free / 2;
                long minBufferSizeBytes = MIN_BUFFER_SIZE_MB * MB;
                if (sortBufferByteSize < minBufferSizeBytes || totalAvailableBytes > 10 * minBufferSizeBytes) // lets see if we need/should to grow the heap
                {
                    if (totalAvailableBytes / 2 > minBufferSizeBytes) // there is enough mem for a reasonable buffer
                    {
                        sortBufferByteSize = totalAvailableBytes / 2; // grow the heap
                    }
                    else
                    {
                        //heap seems smallish lets be conservative fall back to the free/2
                        sortBufferByteSize = Math.Max(ABSOLUTE_MIN_SORT_BUFFER_SIZE, sortBufferByteSize);
                    }
                }
                return new BufferSize(Math.Min((long)int.MaxValue, sortBufferByteSize));
            }
        }

        /// <summary>
        /// Sort info (debugging mostly).
        /// </summary>
        public class SortInfo
        {
            /// <summary>
            /// Number of temporary files created when merging partitions </summary>
            public int TempMergeFiles { get; set; }
            /// <summary>
            /// Number of partition merges </summary>
            public int MergeRounds { get; set; }
            /// <summary>
            /// Number of lines of data read </summary>
            public int Lines { get; set; }
            /// <summary>
            /// Time spent merging sorted partitions (in milliseconds) </summary>
            public long MergeTime { get; set; }
            /// <summary>
            /// Time spent sorting data (in milliseconds) </summary>
            public long SortTime { get; set; }
            /// <summary>
            /// Total time spent (in milliseconds) </summary>
            public long TotalTime { get; set; }
            /// <summary>
            /// Time spent in i/o read (in milliseconds) </summary>
            public long ReadTime { get; set; }
            /// <summary>
            /// Read buffer size (in bytes) </summary>
            public long BufferSize { get; private set; }

            /// <summary>
            /// Create a new <see cref="SortInfo"/> (with empty statistics) for debugging. </summary>
            public SortInfo(OfflineSorter offlineSorter)
            {
                BufferSize = offlineSorter.ramBufferSize.bytes;
            }

            /// <summary>
            /// Returns a string representation of this object.
            /// </summary>
            public override string ToString()
            {
                return string.Format(CultureInfo.InvariantCulture, 
                    "time={0:0.00} sec. total ({1:0.00} reading, {2:0.00} sorting, {3:0.00} merging), lines={4}, temp files={5}, merges={6}, soft ram limit={7:0.00} MB", 
                    TotalTime / 1000.0d, ReadTime / 1000.0d, SortTime / 1000.0d, MergeTime / 1000.0d, 
                    Lines, TempMergeFiles, MergeRounds, 
                    (double)BufferSize / MB);
            }
        }

        private readonly BufferSize ramBufferSize;

        private readonly Counter bufferBytesUsed = Counter.NewCounter();
        private readonly BytesRefArray buffer;
        private SortInfo sortInfo;
        private readonly int maxTempFiles;
        private readonly IComparer<BytesRef> comparer;

        /// <summary>
        /// Default comparer: sorts in binary (codepoint) order </summary>
        public static readonly IComparer<BytesRef> DEFAULT_COMPARER = Utf8SortedAsUnicodeComparer.Instance;

        /// <summary>
        /// Defaults constructor.
        /// </summary>
        /// <seealso cref="DefaultTempDir()"/>
        /// <seealso cref="BufferSize.Automatic()"/>
        public OfflineSorter()
            : this(DEFAULT_COMPARER, BufferSize.Automatic(), DefaultTempDir(), MAX_TEMPFILES)
        {
        }

        /// <summary>
        /// Defaults constructor with a custom comparer.
        /// </summary>
        /// <seealso cref="DefaultTempDir()"/>
        /// <seealso cref="BufferSize.Automatic()"/>
        public OfflineSorter(IComparer<BytesRef> comparer)
            : this(comparer, BufferSize.Automatic(), DefaultTempDir(), MAX_TEMPFILES)
        {
        }

        /// <summary>
        /// All-details constructor.
        /// </summary>
#pragma warning disable IDE0060 // Remove unused parameter
        public OfflineSorter(IComparer<BytesRef> comparer, BufferSize ramBufferSize, DirectoryInfo tempDirectory, int maxTempfiles)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            buffer = new BytesRefArray(bufferBytesUsed);
            if (ramBufferSize.bytes < ABSOLUTE_MIN_SORT_BUFFER_SIZE)
            {
                throw new ArgumentException(MIN_BUFFER_SIZE_MSG + ": " + ramBufferSize.bytes);
            }

            if (maxTempfiles < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTempfiles), "maxTempFiles must be >= 2"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            this.ramBufferSize = ramBufferSize;
            this.maxTempFiles = maxTempfiles;
            this.comparer = comparer;
        }

        /// <summary>
        /// Sort input to output, explicit hint for the buffer size. The amount of allocated
        /// memory may deviate from the hint (may be smaller or larger).
        /// </summary>
        public SortInfo Sort(FileInfo input, FileInfo output)
        {
            sortInfo = new SortInfo(this) { TotalTime = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond }; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

            output.Delete();

            var merges = new JCG.List<FileInfo>();
            bool success2 = false;
            try
            {
                var inputStream = new ByteSequencesReader(input);
                bool success = false;
                try
                {
                    int lines = 0;
                    while ((lines = ReadPartition(inputStream)) > 0)
                    {
                        merges.Add(SortPartition(/*lines*/)); // LUCENENET specific - removed unused parameter
                        sortInfo.TempMergeFiles++;
                        sortInfo.Lines += lines;

                        // Handle intermediate merges.
                        if (merges.Count == maxTempFiles)
                        {
                            var intermediate = new FileInfo(Path.GetTempFileName());
                            try
                            {
                                MergePartitions(merges, intermediate);
                            }
                            finally
                            {
                                foreach (var file in merges)
                                {
                                    file.Delete();
                                }
                                merges.Clear();
                                merges.Add(intermediate);
                            }
                            sortInfo.TempMergeFiles++;
                        }
                    }
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Dispose(inputStream);
                    }
                    else
                    {
                        IOUtils.DisposeWhileHandlingException(inputStream);
                    }
                }

                // One partition, try to rename or copy if unsuccessful.
                if (merges.Count == 1)
                {
                    FileInfo single = merges[0];
                    Copy(single, output);
                    try
                    {
                        File.Delete(single.FullName);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch
                    {
                        // ignored
                    }
#pragma warning restore CA1031 // Do not catch general exception types
                }
                else
                {
                    // otherwise merge the partitions with a priority queue.
                    MergePartitions(merges, output);
                }
                success2 = true;
            }
            finally
            {
                foreach (FileInfo file in merges)
                {
                    file.Delete();
                }
                if (!success2)
                {
                    output.Delete();
                }
            }

            sortInfo.TotalTime = ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - sortInfo.TotalTime); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            return sortInfo;
        }

        /// <summary>
        /// Returns the default temporary directory. By default, the System's temp folder. If not accessible
        /// or not available, an <see cref="IOException"/> is thrown.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DirectoryInfo DefaultTempDir()
        {
            return new DirectoryInfo(Path.GetTempPath());
        }

        /// <summary>
        /// Copies one file to another.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Copy(FileInfo file, FileInfo output)
        {
            using Stream inputStream = file.OpenRead();
            using Stream outputStream = output.OpenWrite();
            inputStream.CopyTo(outputStream);
        }

        /// <summary>
        /// Sort a single partition in-memory. </summary>
        private FileInfo SortPartition(/*int len*/) // LUCENENET NOTE: made private, since protected is not valid in a sealed class. Also eliminated unused parameter.
        {
            var data = this.buffer;
            FileInfo tempFile = FileSupport.CreateTempFile("sort", "partition", DefaultTempDir());

            long start = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            sortInfo.SortTime += ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - start); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

            using (var @out = new ByteSequencesWriter(tempFile))
            {
                IBytesRefEnumerator iter = buffer.GetEnumerator(comparer);
                while (iter.MoveNext())
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(iter.Current.Length <= ushort.MaxValue);
                    @out.Write(iter.Current);
                }
            }

            // Clean up the buffer for the next partition.
            data.Clear();
            return tempFile;
        }

        /// <summary>
        /// Merge a list of sorted temporary files (partitions) into an output file. </summary>
        internal void MergePartitions(IList<FileInfo> merges, FileInfo outputFile)
        {
            long start = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

            var @out = new ByteSequencesWriter(outputFile);

            PriorityQueue<FileAndTop> queue = new PriorityQueueAnonymousClass(this, merges.Count);

            var streams = new ByteSequencesReader[merges.Count];
            try
            {
                // Open streams and read the top for each file
                for (int i = 0; i < merges.Count; i++)
                {
                    streams[i] = new ByteSequencesReader(merges[i]);
                    byte[] line = streams[i].Read();
                    if (line != null)
                    {
                        queue.InsertWithOverflow(new FileAndTop(i, line));
                    }
                }

                // Unix utility sort() uses ordered array of files to pick the next line from, updating
                // it as it reads new lines. The PQ used here is a more elegant solution and has
                // a nicer theoretical complexity bound :) The entire sorting process is I/O bound anyway
                // so it shouldn't make much of a difference (didn't check).
                FileAndTop top;
                while ((top = queue.Top) != null)
                {
                    @out.Write(top.Current);
                    if (!streams[top.Fd].Read(top.Current))
                    {
                        queue.Pop();
                    }
                    else
                    {
                        queue.UpdateTop();
                    }
                }

                sortInfo.MergeTime += (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - start; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                sortInfo.MergeRounds++;
            }
            finally
            {
                // The logic below is: if an exception occurs in closing out, it has a priority over exceptions
                // happening in closing streams.
                try
                {
                    IOUtils.Dispose(streams);
                }
                finally
                {
                    IOUtils.Dispose(@out);
                }
            }
        }

        private class PriorityQueueAnonymousClass : PriorityQueue<FileAndTop>
        {
            private readonly OfflineSorter outerInstance;

            public PriorityQueueAnonymousClass(OfflineSorter outerInstance, int size)
                : base(size)
            {
                this.outerInstance = outerInstance;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected internal override bool LessThan(FileAndTop a, FileAndTop b)
            {
                return outerInstance.comparer.Compare(a.Current, b.Current) < 0;
            }
        }

        /// <summary>
        /// Read in a single partition of data. </summary>
        internal int ReadPartition(ByteSequencesReader reader)
        {
            long start = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            var scratch = new BytesRef();
            while ((scratch.Bytes = reader.Read()) != null)
            {
                scratch.Length = scratch.Bytes.Length;
                buffer.Append(scratch);
                // Account for the created objects.
                // (buffer slots do not account to buffer size.)
                if (ramBufferSize.bytes < bufferBytesUsed)
                {
                    break;
                }
            }
            sortInfo.ReadTime += ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - start); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            return buffer.Length;
        }

        internal class FileAndTop
        {
            internal int Fd { get; private set; }
            internal BytesRef Current { get; private set; }

            internal FileAndTop(int fd, byte[] firstLine)
            {
                this.Fd = fd;
                this.Current = new BytesRef(firstLine);
            }
        }


        /// <summary>
        /// Utility class to emit length-prefixed <see cref="T:byte[]"/> entries to an output stream for sorting.
        /// Complementary to <see cref="ByteSequencesReader"/>.
        /// </summary>
        public class ByteSequencesWriter : IDisposable
        {
            private readonly DataOutput os;

            /// <summary>
            /// Constructs a <see cref="ByteSequencesWriter"/> to the provided <see cref="FileInfo"/>. </summary>
            public ByteSequencesWriter(FileInfo file)
                : this(NewBinaryWriterDataOutput(file))
            {
            }

            /// <summary>
            /// Constructs a <see cref="ByteSequencesWriter"/> to the provided <see cref="DataOutput"/>. </summary>
            public ByteSequencesWriter(DataOutput os)
            {
                this.os = os;
            }

            /// <summary>
            /// LUCENENET specific - ensures the file has been created with no BOM
            /// if it doesn't already exist and opens the file for writing.
            /// Java doesn't use a BOM by default.
            /// </summary>
            private static BinaryWriterDataOutput NewBinaryWriterDataOutput(FileInfo file)
            {
                string fileName = file.FullName;
                // Create the file (without BOM) if it doesn't already exist
                if (!File.Exists(fileName))
                {
                    // Create the file
                    File.WriteAllText(fileName, string.Empty, new UTF8Encoding(false) /* No BOM */);
                }

                return new BinaryWriterDataOutput(new BinaryWriter(new FileStream(fileName, FileMode.Open, FileAccess.Write)));
            }

            /// <summary>
            /// Writes a <see cref="BytesRef"/>. </summary>
            /// <seealso cref="Write(byte[], int, int)"/>
            public virtual void Write(BytesRef @ref)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(@ref != null);
                Write(@ref.Bytes, @ref.Offset, @ref.Length);
            }

            /// <summary>
            /// Writes a byte array. </summary>
            /// <seealso cref="Write(byte[], int, int)"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void Write(byte[] bytes)
            {
                Write(bytes, 0, bytes.Length);
            }

            /// <summary>
            /// Writes a byte array.
            /// <para/>
            /// The length is written as a <see cref="short"/>, followed
            /// by the bytes.
            /// </summary>
            public virtual void Write(byte[] bytes, int off, int len)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(bytes != null);
                    Debugging.Assert(off >= 0 && off + len <= bytes.Length);
                    Debugging.Assert(len >= 0);
                }
                os.WriteInt16((short)len);
                os.WriteBytes(bytes, off, len); // LUCENENET NOTE: We call WriteBytes, since there is no Write() on Lucene's version of DataOutput
            }

            /// <summary>
            /// Disposes the provided <see cref="DataOutput"/> if it is <see cref="IDisposable"/>.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Disposes the provided <see cref="DataOutput"/> if it is <see cref="IDisposable"/>.
            /// </summary>
            protected virtual void Dispose(bool disposing) // LUCENENET specific - implemented proper dispose pattern
            {
                if (disposing && this.os is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        /// <summary>
        /// Utility class to read length-prefixed <see cref="T:byte[]"/> entries from an input.
        /// Complementary to <see cref="ByteSequencesWriter"/>.
        /// </summary>
        public class ByteSequencesReader : IDisposable
        {
            private readonly DataInput inputStream;

            /// <summary>
            /// Constructs a <see cref="ByteSequencesReader"/> from the provided <see cref="FileInfo"/>. </summary>
            public ByteSequencesReader(FileInfo file)
                : this(new BinaryReaderDataInput(new BinaryReader(new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))))
            {
            }

            /// <summary>
            /// Constructs a <see cref="ByteSequencesReader"/> from the provided <see cref="DataInput"/>. </summary>
            public ByteSequencesReader(DataInput inputStream)
            {
                this.inputStream = inputStream;
            }

            /// <summary>
            /// Reads the next entry into the provided <see cref="BytesRef"/>. The internal
            /// storage is resized if needed.
            /// </summary>
            /// <returns> Returns <c>false</c> if EOF occurred when trying to read
            /// the header of the next sequence. Returns <c>true</c> otherwise. </returns>
            /// <exception cref="EndOfStreamException"> If the file ends before the full sequence is read. </exception>
            public virtual bool Read(BytesRef @ref)
            {
                ushort length;
                try
                {
                    length = (ushort)inputStream.ReadInt16();
                }
                catch (Exception e) when (e.IsEOFException())
                {
                    return false;
                }

                @ref.Grow(length);
                @ref.Offset = 0;
                @ref.Length = length;
                inputStream.ReadBytes(@ref.Bytes, 0, length);
                return true;
            }

            /// <summary>
            /// Reads the next entry and returns it if successful.
            /// </summary>
            /// <seealso cref="Read(BytesRef)"/>
            /// <returns> Returns <c>null</c> if EOF occurred before the next entry
            /// could be read. </returns>
            /// <exception cref="EndOfStreamException"> If the file ends before the full sequence is read. </exception>
            public virtual byte[] Read()
            {
                ushort length;
                try
                {
                    length = (ushort)inputStream.ReadInt16();
                }
                catch (Exception e) when (e.IsEOFException())
                {
                    return null;
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(length >= 0, "Sanity: sequence length < 0: {0}", length);
                byte[] result = new byte[length];
                inputStream.ReadBytes(result, 0, length);
                return result;
            }

            /// <summary>
            /// Disposes the provided <see cref="DataInput"/> if it is <see cref="IDisposable"/>.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing) // LUCENENET specific - implemented proper dispose pattern
            {
                if (disposing)
                {
                    if (inputStream is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Returns the comparer in use to sort entries </summary>
        public IComparer<BytesRef> Comparer => comparer;
    }
}