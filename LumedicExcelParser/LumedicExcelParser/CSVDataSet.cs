using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playground.Base;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FormatParser.CSVFormater
{

    /// <summary>
    /// CSVDataSet class is T-type of a collection. It allows us to read csv (character seperated value) file.
    /// It comes up with indexing feature to optimize search based on index keys.
    /// </summary>
    /// <typeparam name="T">T: Any class or struct type</typeparam>
    public sealed class CSVDataSet<T> : IDisposable, IEnumerable<T>
    {
        CSVReader csvReader;
        bool disposed = false;
        long chunkLimit = 1;
        long? size = 0;
        IndexManager<object, Segment> indexManager;
        public string HeaderLine { get { return this.csvReader.HeaderLine; } }
        public Segment CurrentSegment { get { return this.csvReader.LineReader.Segment; } }

        public long Size { get { return size ?? this.LongCount(); } }


        public CSVDataSet(string path, char delimiter, long chunkSize = 1, int headerSpan = 1)
        {
            this.chunkLimit = chunkSize;
            this.csvReader = new CSVReader(path, delimiter, headerSpan);
            this.indexManager = new IndexManager<object, Segment>();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (this.csvReader != null)
                {
                    this.csvReader.Dispose();
                    this.indexManager = null;
                }
                disposed = true;
            }
        }

        ~CSVDataSet()
        {
            Dispose();
        }


        public T FormatToType(object row)
        {
            string json = FormatToJson(row);
            T outRow = JsonConvert.DeserializeObject<T>(json);
            return outRow;
        }

        public string FormatToJson(object row)
        {
            string json = JsonConvert.SerializeObject(row, Formatting.None);
            return json;
        }

        public string FormatToCSVRow(object row)
        {
            string json = FormatToJson(row);
            var newRow = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            string csvRow = this.csvReader.MakeRow(newRow);
            return csvRow;
        }

        /// <summary>
        /// Search results for given index key.
        /// </summary>
        /// <param name="index">Index Name</param>
        /// <param name="key">Index key</param>
        /// <param name="manager">Index manager</param>
        /// <returns>IEnumerable search results</returns>
        private IEnumerable<Dictionary<string, object>> GetIndexedEnumerable(string index, object key, IndexManager<object, Segment> manager = null)
        {
            IndexManager<object, Segment> workingIndexManager = manager != null ? manager : this.indexManager;
            IndexTable<object, Segment> table = workingIndexManager.GetMap(index, false);
            List<Segment> segments = table.GetMap(key, false);

            this.csvReader.Reset();
            List<Dictionary<string, object>> dataset = null;

            foreach (Segment segment in segments)
            {
                this.csvReader.GoToSegment(segment);
                if ((dataset = this.csvReader.GetNextRows(1))?.Count > 0)
                {
                    foreach (var row in dataset)
                    {
                        yield return row;
                    }
                }
            }
        }
        

        /// <summary>
        /// Create indexing based on user provided indexing criteria.
        /// </summary>
        /// <typeparam name="OType">Index key type</typeparam>
        /// <param name="indexName">Index Name</param>
        /// <param name="indexFunc">User provider indexing function</param>
        /// <returns>List<OType> of index keys</returns>
        public List<OType> CreateIndex<OType>(string indexName, Func<Dictionary<string, object>, OType> indexFunc)
        {
            this.indexManager.RemoveIndex(indexName);

            this.csvReader.Reset();
            List<Dictionary<string, object>> dataset = null;
            var indexTable = this.indexManager.GetMap(indexName);

            while ((dataset = this.csvReader.GetNextRows(1))?.Count > 0)
            {
                foreach (var row in dataset)
                {
                    var key = indexFunc(row);
                    Segment segment = this.csvReader.LineReader.Segment;
                    indexTable.AddMapForKey(key, segment);
                }
            }

            var keys = indexTable.GetMapKeys();
            List<OType> tKeys = keys.Select(elm => (OType)elm).ToList();
            return tKeys;
        }

        /// <summary>
        /// Read number of row from csv file from current position.
        /// </summary>
        /// <param name="chunkLimit">number of rows should be read during lookup</param>
        /// <returns>IEnumerable<T> rows</returns>
        public IEnumerable<T> GetTypedEnumerable(long chunkLimit)
        {
            this.csvReader.Reset();
            List<Dictionary<string, object>> dataset = null;

            while ((dataset = this.csvReader.GetNextRows(chunkLimit))?.Count > 0)
            {
                foreach (var row in dataset)
                {
                    T outRow = FormatToType(row);
                    yield return outRow;
                }
            }
        }

        /// <summary>
        /// Search results for given index key.
        /// </summary>
        /// <param name="index">Index Name</param>
        /// <param name="key">Index key</param>
        /// <param name="manager">Index manager</param>
        /// <returns>IEnumerable<Typed> search results</returns>
        public IEnumerable<T> GetIndexedTypedEnumerable(string index, object key, IndexManager<object, Segment> manager = null)
        {
            var innerIndexedEnumerable = GetIndexedEnumerable(index, key, manager);
            foreach (var row in innerIndexedEnumerable)
            {
                T outRow = FormatToType(row);
                yield return outRow;
            }
        }

        /// <summary>
        ///  Search results for given index key.
        /// </summary>
        /// <param name="index">Index Name</param>
        /// <param name="key">Index key</param>
        /// <returns></returns>
        public IEnumerable<T> Search(string index, object key)
        {
            var innerEnumerable = GetIndexedTypedEnumerable(index, key);
            return innerEnumerable;
        }

        public List<object> GetKeys(string index)
        {
            IndexTable<object, Segment> table = indexManager.GetMap(index, false);
            var keys = table.GetMapKeys();
            return keys;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetTypedEnumerable(this.chunkLimit).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetTypedEnumerable(this.chunkLimit).GetEnumerator();
        }
    }


    public static class CSVDataSetExtenstion
    {
        public class IndexGrouping<TKey, TElement> : IGrouping<TKey, TElement>
        {
            private TKey _key;
            private IEnumerable<TElement> _elements;

            public IndexGrouping(TKey key, IEnumerable<TElement> elements)
            {
                _key = key;
                _elements = elements;
            }

            public IEnumerator<TElement> GetEnumerator()
            {
                return _elements.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public TKey Key
            {
                get { return _key; }
            }
        }

        public static IEnumerable<IGrouping<TKey, TSource>> IndexGroupBy<TKey, TSource>(this CSVDataSet<TSource> csvDataSet, Func<TSource, TKey> keySelector)
        {
            IndexManager<object, Segment> workingIndexManager = new IndexManager<object, Segment>();
            string indexName = "TempGroup";

            var innerEnumerable = csvDataSet.GetTypedEnumerable(1);
            foreach (TSource row in innerEnumerable)
            {
                var key = keySelector(row);
                Segment segment = csvDataSet.CurrentSegment;
                workingIndexManager.AddMapForKey(indexName, key, segment);
            }


            IndexTable<object, Segment> indexTable = workingIndexManager.GetMap(indexName);

            foreach (TKey key in indexTable.GetMapKeys())
            {
                var source = csvDataSet.GetIndexedTypedEnumerable(indexName, key, workingIndexManager);
                yield return new IndexGrouping<TKey, TSource>(key, source);
            }

            workingIndexManager = null;
        }

        public static void AppendToCSV<T>(this CSVDataSet<T> csvDataSet, string targetDirectory, string targetFileName, string index, object key, Func<object, T, T> rowUpdateFunc = null)
        {
            bool fileExists = File.Exists(Path.Combine(targetDirectory, targetFileName));

            using (FileWriter writer = new FileWriter(targetDirectory, targetFileName, true))
            {
                if (fileExists == false)
                    writer.WriteText(csvDataSet.HeaderLine, true);

                var rows = csvDataSet.GetIndexedTypedEnumerable(index, key);
                bool updateFuncAvailable = rowUpdateFunc != null;
                foreach (var row in rows)
                {
                    T inRow = csvDataSet.FormatToType(row);
                    T outRow = updateFuncAvailable ? rowUpdateFunc(key, inRow) : inRow;
                    string csvRow = csvDataSet.FormatToCSVRow(outRow);
                    writer.WriteText(csvRow, true);
                }
            }
        }

    }


}
