using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Utilities
{
    public class Logger
    {
        private string _currentFileName = "log.txt";
        private string _previousFileName = "log-previous.txt";

        private int _maxFileMB = 5;

        public void Log(string message, bool includeTime = true)
        {
            if (File.Exists(_currentFileName))
            {
                if (new FileInfo(_currentFileName).Length > _maxFileMB * 1000000)
                {
                    if (File.Exists(_previousFileName))
                    {
                        File.Delete(_previousFileName);
                    }

                    File.Move(_currentFileName, _previousFileName);
                }
            }

            string time = includeTime ? $"{ DateTime.Now.ToString("G") }: " : string.Empty;

            File.AppendAllText(_currentFileName, $"{time}{message}\r\n");
        }
    }
}
