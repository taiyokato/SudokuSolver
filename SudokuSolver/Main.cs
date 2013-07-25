using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/*
 * BY TAIYO KATO
 * LAST BACKUP TIME: 1:50 AM 7/15/2013 -> dropbox, skydrive, SD card
 * 
 * TODO:
 * WHEN THERE ARE STILL UNSOLVABLE BLOCKS, USE TREEDIAGRAM TO SEARCH FOR POSSIBLE ANSWERS.
 * 
 * 
 * FINISHED: 12:59 AM 7/14/2013
 * 
 * EDIT FINISH: 12:48 AM 7/15/2013
 * Edit point:
 * 1. Compatibility for 4x4, 9x9, 16x16... so on sudokus
 * 2. Checks if input value is invalid
 * 
 * EDIT FINISH: 1:33 AM 7/15/2013
 * Edit point:
 * 1. Bug fix for compatibility for 4x4, 9x9, 16x16... so on
 * 2. Allow 'x' only for unknown values
 * 3. Separate each value with a space. Compatibility for higher than 9x9 ones. (since values will be 1-16, which is double digit. Cannot use foreach(char) anymore)
 * 
 * 
 * EDIT FINISH: 2:23 AM 7/25/2013
 * Edit point:
 * 1. Newly added TreeDiagram solve
 * 2. Grid size reset allowed
 * 
 * EDIT FINISH: 9:08 PM 7/25/2013
 * Edit point:
 * 1. Try catch invalid grids
 * 
 */
namespace SudokuSolver
{
    public class SudokuSolver
    {
        [STAThread]
        static void Main(string[] args)
        {
            Solver solver = new Solver();
        }
    }
    #region Structs
    /// <summary>
    /// Item for TempGrid
    /// </summary>
    public class TempBlock
    {
        /// <summary>
        /// Container for possible values in this block
        /// </summary>
        public List<int> Possibles { get; set; }
        /// <summary>
        /// Initialize Possibles list
        /// </summary>
        public TempBlock()
        {
            Possibles = new List<int>();
        }
    }
    /// <summary>
    /// Object that can hold x & y location
    /// </summary>
    public struct Point
    {
        public int x { get; set; }
        public int y { get; set; }
        /// <summary>
        /// For debug purposes, overrided the ToString() method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("[{0},{1}]", x, y);
        }
        /// <summary>
        /// Unsure if the default Equals() method works properly
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Equals(Point obj)
        {
            return ((x.Equals(obj.x)) && (y.Equals(obj.y)));
        }
    }
    #endregion

    
    public class InvalidGridException : Exception
    {
      public InvalidGridException() { }
      public InvalidGridException( string message ) : base( message ) { }
      public InvalidGridException( string message, Exception inner ) : base( message, inner ) { }
      protected InvalidGridException( 
	    System.Runtime.Serialization.SerializationInfo info, 
	    System.Runtime.Serialization.StreamingContext context ) : base( info, context ) { }
    }
}
