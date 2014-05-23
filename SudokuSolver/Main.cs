using System;
using System.Collections.Generic;


/*
 * BY TAIYO KATO
 *
 * VER 1.0.1.3
 * 
 * ALPHA VERSION FINISHED: 12:59 AM 7/14/2013
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
 * EDIT FINISH: 2:23 AM 7/25/2013
 * Edit point:
 * 1. Newly added TreeDiagram solve
 * 2. Grid size reset allowed
 * 
 * EDIT FINISH: 9:08 PM 7/25/2013
 * Edit point:
 * 1. Try catch invalid grids
 * 
 * EDIT FINISH: 8:36 AM 9/25/2013
 * Edit point:
 * 1. Basic (but hard) logical way of solving added
 * 
 * EDIT FINISH: 11:51 PM 10/1/2013
 * Edit point:
 * 1. Checks for horizontal + vertical row for one empty block leftover, and innerblock leftover
 * 
 * EDIT FINISH: 2:15 PM 10/2/2013
 * Edit point:
 * 1. Lighter and simplier modification for GetInnerRange();
 * Overall performance speed up by min. 60%
 *
 * EDIT FINISH: 9:15 PM 10/6/2013
 * Edit point: 
 * 1. Removed confirmed unused methods
 * 2. Less loopings and checked for unnecessary looping duplication
 * 
 * EDIT FINISH: 11:09 PM 11/20/2013
 * Edit point:
 * 1. Minor fix on inner block remaining value checks
 * 2. Fixed bug that FillTemp2() will fill in x with overlapping values
 * 
 * EDIT FINISH: 6:23 PM 11/21/2013
 * Edit point:
 * 1. Replaced List<Tuple<Point,int>> Logger with Stack<LogItem>, where LogItem is a custom struct.
 * 
 * EDIT FINISH: 11:21 PM 11/25/2013
 * Edit point:
 * 1. Changed TreeDiagram constructor to WITHOUT UnfilledCount
 * 2. Changed Execute2() to Execute2(ref UnfilledCount) to replace old TreeDiagram constructor
 * 3. Big improvement with location-possibility evaluation inside CheckHVLeftOver(). Faster and smarter.
 * 4. Added Validator to check grid genuineness
 */
namespace SudokuSolver
{
    public class SudokuSolver
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].ToLower().Equals("-a"))
                {
                    Console.WriteLine(string.Format("SudokuSolver - (c) {0}, by Taiyo Kato", DateTime.Today.Year));
                    Console.WriteLine("Press enter to finish...");
                    Console.Read();
                    System.Environment.Exit(0); //exit
                }
                Reader.ReadFromFile(args[0]);
                new Solver(true);
                return;
            }
            //Console.BufferWidth = Console.LargestWindowWidth;
            //Console.WindowWidth = Console.LargestWindowWidth;
            //Console.WindowHeight = Console.LargestWindowHeight;
            Console.Clear();

            Solver solver = new Solver();
        }
    }

    #region Structs
    /// <summary>
    /// Object that can hold x&y location
    /// </summary>
    public struct Point
    {
        private int? _x;
        private int? _y;
        public int x
        {
            get { return (_x.HasValue) ? _x.Value : -1; }
            set { _x = value; }
        }
        public int y
        {
            get { return (_y.HasValue) ? _y.Value : -1; }
            set { _y = value; }
        }
        
        public Point(int X, int Y) : this()
        {
            x = X;
            y = Y;
        }
        
        
        /// <summary> Custom ToString() for debug purpose </summary>
        /// <returns>[x,y]</returns>
        public override string ToString()
        {
            return string.Format("[{0},{1}]", x, y);
        }
        /// <summary> Avoid default Equals() mis-evaluate </summary>
        /// <param name="obj">Point to evaluate</param>
        /// <returns>If both x && y are equal</returns>
        public bool Equals(Point obj)
        {
            return ((x == obj.x) && (y == obj.y));
        }
        //Not realy needed, but might be useful if operators are available.
        public static bool operator ==(Point a, Point b)
        {
            return a.Equals(b);
        }
        public static bool operator !=(Point a, Point b)
        {
            return !a.Equals(b);
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public static Point Null
        {
            get { return new Point(-1, -1); }
        }
    }
    public struct LogItem
    {
        public Point Point;
        public int Value;
        public LogItem(Point p, int v)
        {
            Point = p;
            Value = v;
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Point, Value);
        }
    }
    public enum Axis
    {
        HORIZONTAL,
        VERTICAL,
        NEITHER
    }
    #endregion
}
