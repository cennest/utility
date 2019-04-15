using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Playground.Base
{
    public class FileWriter : IDisposable
    {
        private bool disposed = false;
        private StreamWriter writer = null;

        public void Dispose()
        {
            if (!disposed)
            {
                if (this.writer != null)
                {
                    this.writer.Dispose();
                }
                disposed = true;
            }
        }

        ~FileWriter()
        {
            Dispose();
        }

        private void CreateFileIFNotExist(string filename)
        {
            if(!File.Exists(filename)) File.Create(filename).Dispose();
        }

        public FileWriter(string path, string fileName, bool append = true)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filePath = Path.Combine(path, fileName);
            CreateFileIFNotExist(filePath);
            this.writer = new StreamWriter(filePath, append);
        }

        public FileWriter(string filePath, bool append = true)
        {
            CreateFileIFNotExist(filePath);
            this.writer = new StreamWriter(filePath, append);
        }

        public void WriteText(IEnumerable<string> textArray, bool lineEndingEnabled = true)
        {
            foreach (var text in textArray)
            {
                WriteText(text, lineEndingEnabled);
            }
        }


        public void WriteText(string text, bool lineEndingEnabled = true)
        {
            if (lineEndingEnabled)
            {
                writer.WriteLine(text);
            }
            else
            {
                writer.Write(text);
            }
        }
    }
}
