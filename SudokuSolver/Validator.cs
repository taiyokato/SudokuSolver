using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SudokuSolver
{
    public static class Validator
    {
        #region def
        private static int? singleblockw;
        /// <summary> 
        /// For accessing nullable int singleblockw 
        /// Nullable int singleblockwをアクセスするために。singleblockwがnullならデフォルトの3を返す
        /// </summary>
        private static int SingleBlockWidth
        {
            get { return (singleblockw.HasValue) ? singleblockw.Value : 3; }
            set { singleblockw = value; }
        }
        /// <summary> 
        /// Shortcut for getting full grid width since n^2 
        /// 表のフルサイズを取得するショートカット。表のサイズはSingleBlockWidthのn^2
        /// </summary>
        private static int FullGridWidth
        {
            get { return (SingleBlockWidth * SingleBlockWidth); }
        }
        private static int[] FullInts;
        private static bool? _success;
        public static bool Success
        {
            get {return (_success.HasValue)? _success.Value : false;}
            set { _success = value; }
        }
        /// <summary>
        /// Shadow of BreakedAt
        /// </summary>
        private static int? _breakedat;
        /// <summary>
        /// Breaked at which horizontal axis
        /// </summary>
        public static int BreakedAt
        {
            get { return (_breakedat.HasValue) ? _breakedat.Value : -1; }
            set { _breakedat = value; }
        }
        private static int sum;

        #endregion

        public static void Init(int sbw, int[] fullints)
        {
            SingleBlockWidth = sbw;
            BreakedAt = -1;
            sum = 0;
            FullInts = fullints;
            Sum(ref sum);
        }

        /// <summary>
        /// Checks the grid before starting to solve
        /// </summary>
        /// <param name="grid">ref Grid</param>
        /// <param name="fgw">FullGridWidth</param>
        /// <returns>Grid is valid or not</returns>
        public static bool GridReadyValidate(ref int[][] grid, int fgw)
        {
            int[] counts = new int[fgw];
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    int item = grid[x][y];
                    if (item == 0) { continue; }
                    counts[item-1]++;
                }
                for (int i = 0; i < FullGridWidth; i++)
                {
                    if (counts[i] > 1)
                    {
                        BreakedAt = x;
                        return false;
                    }
                    counts[i] = 0;
                }
                //counts = new int[fgw];
            }
            for (int x = 0; x < FullGridWidth; x += SingleBlockWidth)
            {
                for (int y = 0; y < FullGridWidth; y += SingleBlockWidth)
                {
                    if (OverlappingInner(ref grid, x, y))
                    {
                        BreakedAt = x;
                        return false;
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// USE THIS NOW! ENSURES NO DUPLICATE
        /// Works same as Validate(), but not using
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="unfilled"></param>
        /// <returns></returns>
        public static bool Validate2(ref int[][] grid, int unfilled, out bool result)
        {
            result = true;
            if (unfilled > 0) return true;
            HashSet<int> InRow = new HashSet<int>();
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    if (grid[x][y] == 0) continue;
                    InRow.Add(grid[x][y]);
                }
                if (InRow.Count != FullGridWidth)
                {
                    BreakedAt = x;
                    goto END;
                }
                InRow.Clear();//resetB
            }
            Success = true;
        END:
            for (int x = 0; x < FullGridWidth; x+=SingleBlockWidth)
            {
                for (int y = 0; y < FullGridWidth; y+=SingleBlockWidth)
                {
                    if (OverlappingInner(ref grid, x, y)) return false;
                }
            }
            result = Success;
            return true;
        }


        /// <summary>
        /// Checks no same value exists in the same block
        /// </summary>
        private static bool OverlappingInner(ref int[][] grid, int startx, int starty)
        {
            HashSet<int> InBlock = new HashSet<int>();

            int xmax = startx + SingleBlockWidth;
            int ymax = starty + SingleBlockWidth;

            for (int xa = startx; xa < xmax; xa++)
            {
                for (int ya = starty; ya < ymax; ya++)
                {
                    if (grid[xa][ya] == 0) continue;
                    if (!InBlock.Add(grid[xa][ya]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Final validation. Checks for all values filled in once
        /// </summary>
        /// <param name="grid">Grid</param>
        /// <returns>Success or fail</returns>
        public static bool FinalValidate(ref int[][] grid, int fgw)
        {
            ushort[] row = new ushort[fgw];

            for (int l = 0; l < fgw; l++)
            {
                for (int a = 0; a < fgw; a++)
                {
                    if (grid[l][a] == 0) continue;
                    row[grid[l][a]-1]++;
                }
                for (int i = 0; i < row.Length; i++)
                {
                    if (row[i] != 1) return false;//more than 1, less than 1 -> false
                    row[i] = 0; //reset as well
                }
            }
            return true;
        }

        /// <summary>
        /// Obtain the total sum of one line for the grid
        /// </summary>
        /// <param name="sum">ref int sum. Put in the referenced int variable</param>
        private static void Sum(ref int sum)
        {
            for (int i = 1; i <= FullGridWidth; i++)
            {
                  sum += i;
            }
        }

    }
}
