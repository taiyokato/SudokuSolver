using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuSolver
{
    public class Reader
    {
        public static int[,] ProcessedGrid;
        public static string RawInput;
        public static int SingleSize;
        public static int GridSize
        {
            get { return (SingleSize * 3); }
            set { SingleSize = (int)Math.Sqrt(value); }
        }
        public static string ReadFromFile(string path)
        {
            System.IO.StreamReader sr = new System.IO.StreamReader(path);
            RawInput = sr.ReadToEnd();
            sr.Close();

            return RawInput;
        }

        public static int[,] ProcessRaw()
        {
            if (string.IsNullOrWhiteSpace(RawInput.Trim())) throw new NullReferenceException("No input");

            string[] split = RawInput.Split(',');
            int num = -1;
            bool s = int.TryParse(split[0],out num);
            if (!s) throw new NullReferenceException("Invalid input");
            int[,] grid = new int[num,num];
            GridSize = num;

            int t = 1;

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    if (split[t].Trim().Equals(""))
                    {
                        num = 0;
                        goto INPUT;
                    }
                    s = int.TryParse(split[t], out num);
                    if (!s) throw new NullReferenceException("Invalid input");
                INPUT:
                    grid[y, x] = num;
                t++;
                }
            }
            ProcessedGrid = grid;

            return grid;

        }
    }
}
