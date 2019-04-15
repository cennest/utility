using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Playground.Base;
using System.Text.RegularExpressions;

namespace FormatParser.CSVFormater
{

    public class CSVReader : IDisposable
    {

        bool disposed = false;
        char delimiter = ',';
        int headerSpan;
        List<string> headers = new List<string>();
        public string HeaderLine { get; private set; }
        public StreamLineReader LineReader { get; private set; }


        public void Dispose()
        {
            if (!disposed)
            {
                if (this.LineReader != null)
                {
                    this.LineReader.Dispose();
                    headers = null;
                }
                disposed = true;
            }
        }

        ~CSVReader()
        {
            Dispose();
        }

        public CSVReader(string path, char delimiter, int headerSpan = 1)
        {
            int bomOffset = GetFileBOMOffset(path);

            var fileStream = File.OpenRead(path);
            this.LineReader = new StreamLineReader(fileStream, bomOffset);
            this.delimiter = delimiter;
            this.headerSpan = headerSpan;
            this.headers = GetHeaders(headerSpan);
        }

        /// <summary>
        /// Used for calculating BOM (Byte Order Marks) offset value.
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns></returns>
        private int GetFileBOMOffset(string path)
        {
            var encodings = Encoding.GetEncodings()
                .Select(e => e.GetEncoding())
                .Select(e => new { Encoding = e, Preamble = e.GetPreamble() })
                .Where(e => e.Preamble.Any())
                .ToArray();

            var maxPrembleLength = encodings.Max(e => e.Preamble.Length);
            byte[] buffer = new byte[maxPrembleLength];

            using (var stream = File.OpenRead(path))
            {
                stream.Read(buffer, 0, (int)Math.Min(maxPrembleLength, stream.Length));
            }

            var encoding = encodings
                .Where(enc => enc.Preamble.SequenceEqual(buffer.Take(enc.Preamble.Length)))
                .Select(enc => enc)
                .FirstOrDefault();

            int bomOffset = 0;
            if (encoding != null)
            {
                bomOffset = encoding.Preamble.Length;
            }

            return bomOffset;
        }

        private List<string> GetHeaders(int headerSpan)
        {
            this.LineReader.GoToLine(0);

            SafeObject<int, string> csvRow = new SafeObject<int, string>();

            int counter = 1;

            string line = string.Empty;
            while ((line = this.LineReader.ReadLine()) != null)
            {
                var rows = ReadRow(line);
                for (int i = 0; i < rows.Count; i++)
                {
                    string value = rows[i];

                    if (value.StartsWith("\"") && value.EndsWith("\""))
                        value = value.Replace("\"", "");

                    csvRow[i] += value;
                }

                if (counter == headerSpan) break;
                counter++;
            }

            this.HeaderLine = string.Join(this.delimiter.ToString(), csvRow.Values.ToArray());
            return csvRow.Values.ToList();
        }

        private List<string> ReadRow(string rowData)
        {
            //List<string> csvRow = rowData.Split(this.delimiter).ToList();

            Regex csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            List <string> csvRow = csvParser.Split(rowData).ToList();
            if(rowData.Contains("Q4120"))
            {
                int a = 0;
            }

            csvRow = csvRow.Select(h => h.Trim()).ToList();
            return csvRow;
        }

        private List<Dictionary<string, object>> ReadRows(long limit)
        {
            var rows = new List<Dictionary<string, object>>();
            long counter = limit == 0 ? long.MaxValue : limit;

            string line = string.Empty;
            while (counter > 0 && (line = this.LineReader.ReadLine()) != null)
            {
                List<string> csvRow = ReadRow(line);
                var row = CreateRow(headers, csvRow);
                rows.Add(row);
                counter--;
            }
            return rows;
        }

        private Dictionary<string, object> CreateRow(List<string> header, List<string> row)
        {
            /*
            T outRow = new T();
            Type rowType = outRow.GetType();
            for (int index = 0; index < header.Count; index++)
            {
                PropertyInfo propertyInfo = rowType.GetProperty(header[index]);
                if (propertyInfo != null) propertyInfo.SetValue(outRow, row[index], null);
            }
            */

            Dictionary<string, object> dict = new Dictionary<string, object>();
            for (int index = 0; index < header.Count; index++)
            {
                dict.Add(header[index], row[index]);
            }
            //string json = JsonConvert.SerializeObject(dict);
            //T outRow = JsonConvert.DeserializeObject<T>(json);
            return dict;
        }

        public void Reset()
        {
            this.LineReader.GoToLine(headerSpan);
        }

        public void GoToSegment(Segment segment)
        {
            this.LineReader.GoToSegment(segment);
        }

        public List<Dictionary<string, object>> GetNextRows(long limit = 1)
        {
            var nextRows = ReadRows(limit);
            return nextRows;
        }

        public string MakeRow(Dictionary<string, object> row)
        {
            List<string> orders = new List<string>();

            foreach (var header in this.headers)
            {
                string value = row.ContainsKey(header) ? row[header] as string : string.Empty;
                orders.Add(value);
            }

            string rowData = string.Join(this.delimiter.ToString(), orders);
            return rowData;
        }
    }

}
