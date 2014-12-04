using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SudokuSolver
{
    public class Reader
    {
        public static int[][] ProcessedGrid;
        public static string RawInput;
        public static int SingleSize;
        public static int GridSize
        {
            get { return (SingleSize * SingleSize); }
            set { SingleSize = (int)Math.Sqrt(value); }
        }
        public static bool ReadFromFile(string path)
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(path);
                RawInput = sr.ReadToEnd();
                sr.Close();

                return true;
            }
            return false;
        }

        public static int[][] ProcessRaw()
        {
            if (string.IsNullOrEmpty(RawInput.Trim())) throw new NullReferenceException("No input");

            string[] alllines = RawInput.Split(Environment.NewLine.ToCharArray(),StringSplitOptions.RemoveEmptyEntries);
            GridSize = alllines.Length;
            ProcessedGrid = new int[GridSize][];

            string[] tmp = {};
            for (int i = 0; i < alllines.Length; i++)
            {
                tmp = alllines[i].Split(new char[]{' '},StringSplitOptions.RemoveEmptyEntries);
                ProcessedGrid[i] = new int[GridSize];
                for (int x = 0; x < GridSize; x++)
                {
                    ProcessedGrid[i][x] = (tmp[x].Equals("x")) ? 0 : int.Parse(tmp[x]);
                }
            }

            return ProcessedGrid;

        }
    }
}
