using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.Utils
{
    public class OutLogWriter
    {
        private string _filePath;

        public OutLogWriter(string filePath)
        {
            _filePath = filePath;
        }

        private StreamWriter _writer;

        public void Log(string text)
        {
            if (_writer == null)
            {
                _writer = new StreamWriter(_filePath);
            }
            _writer.WriteLine(text);
            _writer.Flush();
        }

        public void Close()
        {
            _writer?.Close();
        }

        public void Start()
        {
            _writer = new StreamWriter(_filePath);
        }
    }
}