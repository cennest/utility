using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playground.Base
{
    public struct Segment : ICloneable
    {
        public int Ln { get; set; }
        public long Start { get; set; }
        public long Current { get; set; }

        public Segment(int line, long start, long current)
        {
            Ln = line;
            Start = start;
            Current = current;
        }

        public object Clone()
        {
            Segment seg = new Segment(this.Ln, this.Start, this.Current);
            return seg;
        }
    }

    public sealed class StreamLineReader : IDisposable
    {
        const int BufferLength = 1024;

        Stream _Base;
        int _Read = 0, _Index = 0, _offset = 0;
        byte[] _Bff = new byte[BufferLength];

        Segment _segment = new Segment();


        /// <summary>
        /// Segment
        /// </summary>
        public Segment Segment { get { return (Segment)_segment.Clone(); } }

        /// <summary>
        /// CurrentLine number
        /// </summary>
        public long CurrentPosition { get { return _segment.Current; } }

        /// <summary>
        /// CurrentLine Start Postion
        /// </summary>
        public long CurrentLineStartPostion { get { return _segment.Start; } }

        /// <summary>
        /// CurrentLine End Postion
        /// </summary>
        public long CurrentLineEndPostion { get { return _segment.Current; } }

        /// <summary>
        /// CurrentLine number
        /// </summary>
        public int CurrentLine { get { return _segment.Ln; } }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stream">Stream</param>
        public StreamLineReader(Stream stream, int offset = 0) { _Base = stream; _offset = offset; }
        /// <summary>
        /// Count lines and goto line number
        /// </summary>
        /// <param name="goToLine">Goto Line number</param>
        /// <returns>Return true if goTo sucessfully</returns>
        public bool GoToLine(int goToLine) { return GetCount(goToLine, true) == goToLine; }
        /// <summary>
        /// Count lines and goto line number
        /// </summary>
        /// <param name="goToLine">Goto Line number</param>
        /// <returns>Return the Count of lines</returns>
        public int GetCount(int goToLine) { return GetCount(goToLine, false); }
        /// <summary>
        /// Internal method for goto&Count
        /// </summary>
        /// <param name="goToLine">Goto Line number</param>
        /// <param name="stopWhenLine">Stop when found the selected line number</param>
        /// <returns>Return the Count of lines</returns>
        int GetCount(int goToLine, bool stopWhenLine)
        {
            _Base.Seek(_offset, SeekOrigin.Begin);
            _segment.Current = _offset;
            _segment.Ln = 0;
            _Index = 0;
            _Read = 0;

            long savePosition = _Base.Length;

            do
            {
                if (_segment.Ln == goToLine)
                {
                    savePosition = _segment.Current;
                    if (stopWhenLine) return _segment.Ln;
                }
            }
            while (ReadLine() != null);

            // GoToPosition

            int count = _segment.Ln;

            _segment.Ln = goToLine;
            _Base.Seek(savePosition, SeekOrigin.Begin);

            return count;
        }

        /// <summary>
        /// Re-position the stream to the segment
        /// </summary>
        /// <param name="segment"></param>
        public void GoToSegment(Segment segment)
        {
            _segment = segment;
            _Index = 0;
            _Read = 0;
            _Base.Seek(_segment.Start, SeekOrigin.Begin);
        }

        /// <summary>
        /// Read Line
        /// </summary>
        /// <returns></returns>
        public string ReadLine()
        {
            bool found = false;

            long savePoint = _segment.Current;

            StringBuilder sb = new StringBuilder();
            while (!found)
            {
                if (_Read <= 0)
                {
                    // Read next block
                    _Index = 0;
                    _Read = _Base.Read(_Bff, 0, BufferLength);
                    if (_Read == 0)
                    {
                        if (sb.Length > 0) break;
                        return null;
                    }
                }

                for (int max = _Index + _Read; _Index < max;)
                {
                    char ch = (char)_Bff[_Index];
                    _Read--; _Index++;
                    _segment.Current++;

                    if (ch == '\0' || ch == '\n')
                    {
                        found = true;
                        break;
                    }
                    else if (ch == '\r') continue;
                    else sb.Append(ch);
                }
            }


            if (savePoint != _segment.Current)
            {
                _segment.Start = savePoint;
            }

            _segment.Ln++;
            return sb.ToString();
        }



        /// <summary>
        /// Free resources
        /// </summary>
        public void Dispose()
        {
            if (_Base != null)
            {
                _Base.Close();
                _Base.Dispose();
                _Base = null;
            }
        }
    }
}

