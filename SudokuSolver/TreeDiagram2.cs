using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuSolver
{
    public class TreeDiagram2
    {
        private string[,] Grid; // Type=string for compatibility for 2-digit values and char 'x'
        private TempBlock[,] TempGrid; //[x, y]
        private int[] SeparateLines = { 0, 3, 6 };
        private int[] FullInts = { 1, 2, 3, 4, 5, 6, 7, 8, 9 }; // basic 9x9 grid 
        private static int SingleBlockWidth {get; set;}
        private static int FullGridWidth
        {
            get { return (SingleBlockWidth * SingleBlockWidth); }
        }

        public TreeDiagram(string[,] g, TempBlock[,] t)
        {
            Grid = g;
            TempGrid = t;
        }
        public bool TryGrid()
        {


        }
        #region Basic
        private void BasicSolve()
        {
            //fill each tempgrid block with possible values
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    //Console.Clear();
                    //foreach (int item in GetPossible(x,y))
                    //{
                    //    Console.WriteLine(item);
                    //}
                    TempGrid[x, y].Possibles.AddRange(GetPossible(x, y)); //add possible values
                }
            }

        }
        #endregion
    }
}
