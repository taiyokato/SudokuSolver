﻿using System;
using System.Collections.Generic;


/*
 * BY TAIYO KATO
 *
 * VER 1.0.1.5
 * 
 * Release log ommitted.
 */
namespace SudokuSolver
{
    public class SudokuSolver
    {
        [STAThread]
        static void Main(string[] args)
        {

            bool noprint = false;
            Console.Clear();
            if (args.Length > 0)
            {
                if (args[0].ToLower().Equals("-a"))
                {
                    Console.WriteLine(string.Format("SudokuSolver - (c) {0}, by Taiyo Kato", DateTime.Today.Year));
                    Console.WriteLine("Press enter to finish...");
                    Console.Read();
                    System.Environment.Exit(0); //exit
                }
                if (!Reader.ReadFromFile(args[0])) return;
                new Solver(true,skipprint: noprint);
                return;
            }
            new Solver(skipprint: noprint);
        }
    }

    #region Structs
    /// <summary>
    /// Object that can hold x and y location
    /// </summary>
    public struct Point : IComparer<Point>
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
        public Point(Point p)
            : this()
        {
            x = p.x;
            y = p.y;
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

        public int Compare(Point p1, Point p2)
        {
            if (p1.x == p2.x)
            {
                return p1.y - p2.y;
            }
            return p1.x - p2.y;
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
