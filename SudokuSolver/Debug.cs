using System;
using System.IO;

namespace SudokuSolver
{
    public class Debug
    {
        private StreamWriter writer;
        private StreamWriter logwriter;
        public Debug()
        {
            writer = new StreamWriter(String.Format(@"{0}\Log.txt", Environment.CurrentDirectory));
            FileStream fs = new FileStream(String.Format(@"{0}\Logger.txt", Environment.CurrentDirectory), FileMode.Create, FileAccess.Write);
            logwriter = new StreamWriter(fs);
            
        }
        public void Write(string text)
        {
            writer.WriteLine(text);
            writer.Flush();
        }
        public void Finish()
        {
            writer.Close();
            logwriter.Close();
        }
        public void LoggerWrite(string text)
        {
            logwriter.WriteLine(text);
            logwriter.Flush();
        }
    }
}
