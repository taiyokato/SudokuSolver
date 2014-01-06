using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace SudokuSolver
{
    partial class Solver
    {
        #region def
        /// <summary>
        /// Main grid. Didn't use char[,] due to comaptibility with grids greater than 9x9, for they have 2-digit values
        /// メイン表。2桁数値対応のため、char[,]をしようしない。
        /// </summary>
        private int[,] Grid;
        /// <summary>
        /// Temp grid for holding possible values
        /// サブ表。マスに入れる可能性の値を保管する
        /// </summary>
        private int[,][] TempGrid;
        //private TempBlock[,] TempGrid; //[x, y]
        /// <summary> 
        /// All possible values array used for filter out possible values
        /// 可能の値の配列。不可能値の排除に使う
        /// </summary>
        private int[] FullInts;
        protected int? singleblockw;
        /// <summary> 
        /// For accessing nullable int singleblockw 
        /// Nullable int singleblockwをアクセスするために。singleblockwがnullならデフォルトの3を返す
        /// </summary>
        private int SingleBlockWidth
        {
            get { return (singleblockw.HasValue) ? singleblockw.Value : 3; }
            set { singleblockw = value; }
        }
        /// <summary> 
        /// Shortcut for getting full grid width since n^2 
        /// 表のフルサイズを取得するショートカット。表のサイズはSingleBlockWidthのn^2
        /// </summary>
        private int FullGridWidth
        {
            get { return (SingleBlockWidth * SingleBlockWidth); }
        }
        public bool finished = false;
        public bool success = false;
        public int UnfilledCount;
        public Validator validator;
        #endregion

        /// <summary>
        /// Main method
        /// 入り込みポイント
        /// </summary>
        public Solver()
        { 
        //Get the size first, then initialize Grid
        SETSIZE: //label for reset grid size
            Console.WriteLine("Input full grid width: ");
            Console.WriteLine("Grid size must be greater than 9, and √size must be a whole number");
            Console.WriteLine(" Usable escape strings are available.\n The default grid size will be 9x9 if escape strings are used");
            Console.WriteLine(" Usable escape strings for default are:");
            Console.WriteLine(" {0, null, -, --}");
            ReadLength();//set the grid length

            Console.Clear(); //Clear console
            Console.WriteLine("Grid size will be: {0}x{0}", FullGridWidth);
            Console.WriteLine();
            //initialize
            Initialize();



            Console.WriteLine("Input Each line\nEnter empty values as x\nSeparate values with a space\nIf full grid width is less than 9, space is not required: ");
            Console.WriteLine("Input \"resize\" to re-set grid size");
            //Console.WriteLine("Input \"redo\" to re-enter line");
            if (ReadLines()) { Console.Clear(); goto SETSIZE; } //if ReadLines return true, reset grid size
            Console.WriteLine();
            Console.WriteLine("Input values are: ");
            PrintAll();
            Console.WriteLine("Press enter key to continue...");
            Console.Read();

            //dx = 6;
            //dy = 2;
            //AdvancedHook(9);
            UnfilledCount = (FullGridWidth * FullGridWidth);
            GetFilledCount();
            Stopwatch stop = new Stopwatch();
            if (!validator.GridReadyValidate(ref Grid, FullGridWidth)) { Console.WriteLine("Invalid at row: {0}", validator.BreakedAt); goto FINISH; }


            stop.Start();
            Basic(); //preparation
            Console.WriteLine("Basic try:");
            if ((validator.Validate2(ref Grid, UnfilledCount, out success)) && (!success)) { Console.WriteLine("Invalid at row: {0}", validator.BreakedAt); goto FINISH; }
            if (UnfilledCount <= 0) goto FINISH; //if finished at this point, jump to finish
            PrintAll();
            //Console.WriteLine(DateTime.Now.Subtract(now));
            Advanced();

            Console.WriteLine("Advanced try:");
            if ((validator.Validate2(ref Grid, UnfilledCount, out success)) && (!success)) { Console.WriteLine("Invalid at row: {0}", validator.BreakedAt); goto FINISH; }
            if (UnfilledCount <= 0) goto FINISH; //if finished at this point, jump to finish
            PrintAll();

            //Logic();
            //Console.WriteLine("Logical try:");
            //if (UnfilledCount==0) goto FINISH; //if finished at this point, jump to finish
            //PrintAll();

            //Console.WriteLine(DateTime.Now.Subtract(now));
            Console.WriteLine("Now solving...");

            finished = TreeDiagramSolve();

        FINISH:
            //if (treesuccess) { Console.WriteLine("Result:"); }
            stop.Stop();
            ClearLine();
            Console.WriteLine("Result:");
            PrintAll();
            Console.WriteLine("Time spent: {0}", TimeSpan.FromMilliseconds(stop.ElapsedMilliseconds));
            bool validated = validator.Validate2(ref Grid, UnfilledCount, out success);
            Console.WriteLine("Grid Check: {0}", (validated && success) ? "Valid" : "Invalid");
            if (!success) Console.WriteLine("Invalid at row: {0}", validator.BreakedAt);
            Console.WriteLine("Unfilled blocks: {0}", UnfilledCount);



            Console.WriteLine("[EOF]");
            Console.WriteLine("Press enter to finish");

            Console.Read();
            Console.Read();
        }

        void Basic()
        {
            FillTemp();
            FillTemp2();
            if (UnfilledCount == 0) return;
            CheckHVLeftOver();
            CheckInnerBoxLeftOver();
        }
        void Advanced()
        {
            //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed;
            int[,] backupGrid = Grid.Clone() as int[,];
            do
            {
                CheckInnerBoxLeftOver();
                CheckHVLeftOver();
                AdvancedFill(true);

                bool gridsame = GridSame(backupGrid);
                if (gridsame) break; //if advancedfill & checkleftover2 cant handle it anymore
                backupGrid = Grid.Clone() as int[,]; //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed;
            } while (UnfilledCount > 0);
        }

        private void LogicSolve()
        {
            return; //STILL NOT FULLY WORKING
            #region Horizontal
            //horizontal
            for (int x = 0; x < FullGridWidth; x+=SingleBlockWidth)
            {
                int[] left = FullInts.Except(GetFilledInRow(x, Axis.HORIZONTAL)).ToArray();
                Point[] empts = GetHVEmpty(x, Axis.HORIZONTAL);
                /*//
                 foreach num in left
                 1. Find if any horizontal blocks have filled.
                 * 2. if found, remove all possiblity
                 * 
                 * PROB SKIP STEPS 1 & 2 BECAUSE POSSIBILITIES ALREADY IN TEMPGRID?
                 * for each num not filled blocks except self, get all horizontal rows possible.
                 *      if one row left for self, num in that cell.
                //*/
                foreach (int leftover in left)
                {
                    for (int i = 0; i < FullGridWidth; i += SingleBlockWidth)
                    {
                        if (i == x) continue;
                        Point[] locs = GetSpecificPossibleLocationsInBlock(leftover, new Point(i,0));
                        Point[] filter = FilterLocs(leftover, empts).ToArray();
                        Point[] pts = SelectNonOverlappingPoints(Axis.HORIZONTAL, filter, locs).ToArray();
                        if (pts.Length == 1)
                        {
                            Grid[pts[0].x, pts[0].y] = leftover;
                            UnfilledCount--;
                            //Clean up
                            CleanTempGrid(pts[0].x, pts[0].y);

                            Point[] gxre = GetHorizontalEmpty(pts[0].x, pts[0].y, 0, true, true);
                            Point[] gyre = GetVerticalEmpty(pts[0].x, pts[0].y, 0, true, true);
                            foreach (Point p in gxre)
                            {
                                RemoveItemFromTemp(leftover, ref TempGrid[p.x, p.y]);
                            }
                            foreach (Point p in gyre)
                            {
                                RemoveItemFromTemp(leftover, ref  TempGrid[p.x, p.y]);
                            }
                            //empts = GetHREmpty(x, Axis.HORIZONTAL);
                            break;
                            //goto NEXT1;
                        }

                    }
                NEXT1:
                    continue;
                }

            }
            #endregion


            #region Vertical
            //vertical
            for (int y = 0; y < FullGridWidth; y+=SingleBlockWidth)
            {
                int[] left = FullInts.Except(GetFilledInRow(y, Axis.VERTICAL)).ToArray();
                Point[] empts = GetHVEmpty(y, Axis.VERTICAL);
                /*//
                 foreach num in left
                 1. Find if any horizontal blocks have filled.
                 * 2. if found, remove all possiblity
                 * 
                 * PROB SKIP STEPS 1 & 2 BECAUSE POSSIBILITIES ALREADY IN TEMPGRID?
                 * for each num not filled blocks except self, get all horizontal rows possible.
                 *      if one row left for self, num in that cell.
                //*/
                foreach (int leftover in left)
                {
                    for (int i = 0; i < FullGridWidth; i+=SingleBlockWidth)
                    {
                        if (i == y) continue;
                        Point[] locs = GetSpecificPossibleLocationsInBlock(leftover, new Point(0, i));
                        Point[] filter = FilterLocs(leftover, empts).ToArray();
                        Point[] pts = SelectNonOverlappingPoints(Axis.VERTICAL, filter, locs).ToArray();
                        if (pts.Length == 1)
                        {
                            Grid[pts[0].x, pts[0].y] = leftover;
                            UnfilledCount--;
                            //Clean up
                            CleanTempGrid(pts[0].x, pts[0].y);

                            Point[] gxre = GetHorizontalEmpty(pts[0].x, pts[0].y, 0, true, true);
                            Point[] gyre = GetVerticalEmpty(pts[0].x, pts[0].y, 0, true, true);
                            foreach (Point p in gxre)
                            {
                                RemoveItemFromTemp(leftover, ref  TempGrid[p.x, p.y]);
                            }
                            foreach (Point p in gyre)
                            {
                                RemoveItemFromTemp(leftover, ref  TempGrid[p.x, p.y]);
                            }
                            //empts = GetHREmpty(y, Axis.VERTICAL);
                            break;
                            //goto NEXT2;
                        }
                
                    }
                NEXT2:
                    continue;
                }
                
            }
            #endregion

            
        }

        private IEnumerable<Point> FilterLocs(int val, Point[] locs)
        {
            foreach (Point pt in locs)
            {
                if (TempGrid[pt.x, pt.y].Contains(val)) yield return pt;
            }
        }

        private IEnumerable<Point> SelectNonOverlappingPoints(Axis ax, Point[] self, Point[] target)
        {
            bool retflag = true;
            switch (ax)
            {
                case Axis.HORIZONTAL:
                    foreach (Point item in self)
                    {
                        foreach (Point pt in target)
                        {
                            if (item.y == pt.y) retflag = false;   
                        }
                        if (retflag == true) yield return item;
                        retflag = true;
                    }
                    break;
                case Axis.VERTICAL:
                    foreach (Point item in self)
                    {
                        foreach (Point pt in target)
                        {
                            if (item.x == pt.x) retflag = false;
                        }
                        if (retflag == true) yield return item;
                        retflag = true;
                    }
                    break;
            }
            

        }

        private Point[] GetSpecificPossibleLocationsInBlock(int num, Point loc)
        {
            int xpos = GetInnerRange(loc.x);
            int ypos = GetInnerRange(loc.y);

            Queue<Point> locs = new Queue<Point>();
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (TempGrid[xa, ya].Contains(num)) locs.Enqueue(new Point(xa, ya));
                }
            }
            return locs.ToArray();
        }

        /// <summary>
        /// NOT IMPLEMENTED YET.
        /// REQUIRES MORE THOUGHT EXPERIMENTS AND TESTINGS
        /// </summary>
        void Logic()
        {
            return;//prevent accidental use
            int[,] backupGrid = Grid.Clone() as int[,];
            while ((UnfilledCount > 0))
            {
                for (int x = 0; x < FullGridWidth; x += SingleBlockWidth)
                {
                    for (int y = 0; y < FullGridWidth; y += SingleBlockWidth)
                    {
                        int[] left = (FullInts.Except(GetFilledInner(x, y))).ToArray();
                        bool xnoneflag = true;
                        bool ynoneflag = true;

                        Point[] inner = GetInnerEmpty(x, y);
                        foreach (Point item in inner)
                        {
                            Point[] xrowempt = GetHorizontalEmpty(item.x, item.y);
                            Point[] yrowempt = GetVerticalEmpty(item.x, item.y);

                            foreach (int item2 in left)
                            {
                                foreach (Point item3 in xrowempt)
                                {
                                    int[] pos = TempGrid[item3.x, item3.y];//GetPossible(item3.x, item3.y);
                                    if (pos.Contains(item2))
                                    {
                                        xnoneflag = false;
                                    }
                                    xnoneflag = (xnoneflag) ? false : true;
                                }
                                foreach (Point item3 in yrowempt)
                                {
                                    int[] pos = TempGrid[item3.x, item3.y];//GetPossible(item3.x, item3.y);
                                    if (pos.Contains(item2))
                                    {
                                        ynoneflag = false;
                                    }
                                }
                                if (xnoneflag || ynoneflag)
                                {
                                    Grid[item.x, item.y] = item2;
                                    CleanTempGrid(item.x, item.y);
                                    goto NEXTLOOP;
                                }
                                xnoneflag = true;
                                ynoneflag = true;
                            }

                        }
                    NEXTLOOP:
                        continue;
                    }
                }

                bool gridsame = GridSame(backupGrid);
                if (gridsame) break; //if advancedfill & checkleftover2 cant handle it anymore
                backupGrid = Grid.Clone() as int[,]; //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed;
            }
        }
        bool TreeDiagramSolve()
        {
            //create TreeDiagram instance for easier code management
            TreeDiagram td = new TreeDiagram(Grid, SingleBlockWidth, FullInts, TempGrid);
            //Console.WriteLine(DateTime.Now.Ticks - start);
            td.Execute2(ref UnfilledCount); //probably faster and lighter than Execute()
            //td.Execute3(false); //Debugger throws StackOverflow exception at random places
            //td.Execute4();
            //Console.WriteLine("UNFILLEDCOUNT: {0}", td.UnfilledCount);

            Grid = td.Grid;
            TempGrid = td.TempGrid;
            return td.FinishFlag;
        }


        private bool GridSame(int[,] backup)
        {

            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    if (Grid[x, y] != backup[x, y]) return false;
                }
            }
            return true;

        }
        private bool TempGridSame(int[,][] backup)
        {
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    if (TempGrid[x, y].GetEnumerator().Equals(backup[x, y].GetEnumerator())) return false;
                }
            }
            return true;

        }

        #region Input values
        /// <summary>
        /// Reads the input line
        /// </summary>
        private bool ReadLines()
        {
            string line = string.Empty;
            for (int i = 0; i < FullGridWidth; i++)
            {
                line = Console.ReadLine(); //Read input line   
                #region FileRead
                if (line.StartsWith("desktop"))
                {
                    line = line.Replace(@"desktop", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
                }
                if (line.Equals("16x16")) line = @"C:\users\15110751\desktop\16x16.txt";
                if (line.Equals("9x9")) line = @"C:\users\15110751\desktop\9x9.txt";
                if (System.IO.File.Exists(line))
                {
                    List<string> lines = new List<string>();
                    System.IO.StreamReader file = new System.IO.StreamReader(line);
                    string fline = string.Empty;
                    while ((fline = file.ReadLine()) != null)
                    {
                        lines.Add(fline);
                    }
                    file.Close();

                    lines.ToList().ForEach(a => a.Trim());
                    for (int a = 0; a < FullGridWidth; a++)
                    {
                        string[] splitted = lines[a].Split(' ');
                        //splitted.ToList().ForEach(c => c.Trim());
                        int pt = 0;
                        for (int b = pt; b < splitted.Length; b++)
                        {
                            string item = splitted[b].Trim();
                            Grid[a, b] = (item.Equals("x")) ? 0 : int.Parse(splitted[b]);
                        }
                        pt = 0;

                    }
                    break;
                }
                #endregion
                if (line.Trim().ToLower().Equals("resize")) return true;
                if (line.Trim().Equals("")) { i--; ClearLine(); continue; }
                if (line.Equals("testvals")) { TestVals9x9empty(); break; } //test empty values
                if (line.Equals("testvals2")) { TestVals9x9(); break; } //test sample values
                if (line.Equals("testvals4x4")) { TestVals16x16(); break; } //test sample values
                if (!LineValid(line)) { i--; ClearLine(); continue; }//if invalid, reod
                string[] split = line.Split(' '); //split
                for (int x = 0; x < FullGridWidth; x++)
                {
                    //if 2-digit value is possible, split check
                    if ((FullGridWidth < 10) && (split[0].Trim().Length == FullGridWidth))
                    {
                        for (int z = 0; z < FullGridWidth; z++)
                        {
                            int num = (split[0].Trim()[z].Equals("x")) ? 0 : split[0].Trim()[z];
                            Grid[i, x] = num;
                        }
                    }
                }
            }
            return false;
        }
        private void ReadLength()
        {
            //if (!LineValid(line)) { i--; ClearLine(); continue; }//if invalid, reod
            string line = Console.ReadLine();
            string[] escapestrings = { "0", "null", "-", "--" };

            if (escapestrings.Any(a => a.Equals(line))) { SingleBlockWidth = 3; return; }// use default size
            bool valid = false;
            int testval = -1;

            while (!valid)
            {

                if (escapestrings.Any(a => a.Equals(line))) return;// use default size
                try
                {
                    testval = int.Parse(line);
                    //size cannot be 1x1, grids size less than 9x9 are too annoying.lol

                    //THE MOST IMPORTANT PART OF THIS METHOD
                    if ((testval >= 9) && (Math.Sqrt((double)testval) % 1 == 0)) break; //check if testval is a valid number of n^n, by seeing if self rooting results a whole number
                }
                catch (Exception)
                {
                }
                ClearLine();
                line = Console.ReadLine();
            }
            SingleBlockWidth = (int)Math.Sqrt((double)testval);
        }
        private void ClearLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }
        private bool LineValid(string values)
        {
            System.Text.RegularExpressions.MatchCollection matchcol = new System.Text.RegularExpressions.Regex(@"[0-9x]{1,2}").Matches(values);
            return (matchcol.Count == FullGridWidth);


            #region Old
            //NOT SMART
            /*
            //strip off x
            string tmp = string.Empty;
            string[] split = values.Trim().Split(' ');
            List<int> vals = new List<int>();
            int xchar_count = 0;
            foreach (string item in split)
            {
                string trimmed = item.Trim();
                if (trimmed.Equals("x")) { xchar_count++; continue; }
                if ((FullGridWidth < 10) && (item.Length == FullGridWidth)) return true;
                if ((!char.IsNumber(trimmed[0])) && (!trimmed.Equals("x"))) return false; //not number, nor "x". ONLY ALLOW X as variable
                vals.Add(int.Parse(trimmed));
                if (int.Parse(trimmed) <= 0) return false; //do not allow 0 or negative value
                if (int.Parse(trimmed) > FullGridWidth) return false; //what falls in this line MUST and ALWAYS is a number
            }
            bool lengthmatch = ((xchar_count + vals.Count) == FullGridWidth);//input values count, then compare with allowed && supposed length
            return lengthmatch;
            //return (values.Length == FullGridWidth);*/
            #endregion
            
        }
        #endregion

        private void GetFilledCount()
        {
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    if (Grid[x, y] > 0) UnfilledCount--;
                }
            }
        }

        #region Sample Values
        private void TestVals9x9empty()
        {
            for (int i = 0; i < FullGridWidth; i++)
            {
                for (int x = 0; x < FullGridWidth; x++)
                {
                    Console.Write("x");
                    Grid[i, x] = 0;
                }
                Console.Write("\n");
            }
        }
        private void TestVals9x9()
        {
            string[] lines = {
                                "xx9xxx7xx",
                                "x2xxxxx5x",
                                "8xx3x6xx2",
                                "xx2x7x1xx",
                                "xxx8x9xxx",
                                "xx8x1x5xx",
                                "1xx2x3xx8",
                                "x8xxxxx1x",
                                "xx4xxx9xx"
                             };
            for (int i = 0; i < FullGridWidth; i++)
            {
                string item = lines[i];
                for (int x = 0; x < FullGridWidth; x++)
                {
                    Console.Write(item[x]);
                    Grid[i, x] = (item[x].Equals("x")) ? 0 : int.Parse(item[x].ToString());
                }
                Console.Write("\n");
            }
        }
        private void TestVals16x16()
        {
            string[] lines = {
                                "x x x x 7 x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x ",
                                "x x x x x x x x x x x x x x x x "
                             };
            for (int i = 0; i < FullGridWidth; i++)
            {
                string[] split = lines[i].Split(' '); //split
                //split.ToList().ForEach(a => a.Trim()); //trim each item
                for (int x = 0; x < FullGridWidth; x++)
                {
                    string item = split[x];
                    Console.Write(item[x]);
                    Grid[i, x] = (item.Equals("x")) ? 0 : int.Parse(split[x]);
                }
                Console.Write("\n");
            }


        }

        #endregion

        

        #region Debug
        private int? dx = null, dy = null;
        private void HookEventHandler()
        {
            if ((dx == null) || (dy == null)) return; // do nothing if axis not specified
            if ((dx > FullGridWidth - 1) || (dy > FullGridWidth - 1)) return; // do nothing if specifid hook axis are OOB
            Print3x3(dx.Value, dy.Value);
        }
        private void AdvancedHook(params int[] filter)
        {
            if ((dx == null) || (dy == null)) return; // do nothing if axis not specified
            if ((dx > FullGridWidth - 1) || (dy > FullGridWidth - 1)) return; // do nothing if specifid hook axis are OOB

            int xpos = GetInnerRange(dx.Value);
            int ypos = GetInnerRange(dy.Value);

            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (Grid[xa, ya] != 0) continue; // no need to print out possible values for filled in
                    int[] poss = GetPossible(xa, ya);
                    if (poss.Intersect(filter).Any())
                    {
                        Console.WriteLine(@"[{0},{1}]", xa, ya);
                        foreach (int item in poss)
                        {
                            Console.Write(item + " ");
                        }
                        Console.Write("\n");
                    }
                }
            }


            Console.WriteLine();
        }
        #endregion
        #region Printout
        private void PrintHorizontalBorder(bool withnewline = false, bool headerfooter = false)
        {
            string outstr = (headerfooter) ? "-" : "|";

            bool outflag = false;

            int itemwidth = (Math.Floor(Math.Log10(Math.Abs(FullGridWidth)) + 1) > 1) ? 3 : 2; //2 char + 1 space, 1 char + 1 space
            for (int i = 0; i < FullGridWidth; i++)
            {
                for (int x = 0; x < (itemwidth); x++)
                {
                    if (!headerfooter)
                    {
                        if ((i == FullGridWidth - 1) && (x == itemwidth - 1))
                        {
                            outstr += "|";
                            continue;
                        }
                        outstr += (outflag) ? "-" : " ";
                        outflag = !outflag;
                        continue;
                    }
                    outstr += ((!headerfooter) && ((i == FullGridWidth - 1) && (x == itemwidth - 1))) ? "|" : "-";
                }
            }
            //Console.WriteLine(outstr);
            Console.WriteLine("{0}{1}", ((withnewline) ? "\n" : string.Empty), outstr);
        }
        private void PrintAll()
        {
            PrintHorizontalBorder(false, true);
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    Console.Write((y % SingleBlockWidth == 0) ? "|" : " ");
                    int block = Grid[x, y];
                    Console.Write("{0}{1}", ((FullGridWidth > 9) && (block < 10)) ? " " : string.Empty, (block == 0) ? "x" : block.ToString());
                }
                Console.Write("|");
                if ((x + 1) % SingleBlockWidth == 0)
                {
                    if (x != FullGridWidth - 1)
                    {
                        PrintHorizontalBorder(true);
                    }
                    else PrintHorizontalBorder(true, true);
                }
                else
                {
                    Console.Write("\n");
                }
            }

        }
        private void PrintSpecific(int x, int y)
        {
            Console.WriteLine("Specific {0},{1}:", x, y);
            for (int i = 0; i < TempGrid[x, y].Length; i++)
            {
                Console.Write(TempGrid[x, y][i] + " ");
            }
            Console.WriteLine();
        }
        /// <summary>
        /// Prints each empty block's possible values in specified inner block
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void Print3x3(int x, int y)
        {
            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);

            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (Grid[xa, ya] != 0) continue; // no need to print out possible values for filled in
                    int[] poss = TempGrid[xa, ya];//GetPossible(xa, ya);
                    Console.WriteLine(@"[{0},{1}]", xa, ya);
                    poss.ToList().ForEach(new Action<int>(a =>
                    {
                        Console.Write(a + " ");
                    }));
                    Console.Write("\n");
                }
            }
            Console.WriteLine();
        }

        #endregion

        #region 後始末
        private void CleanTempGrid(int x, int y)
        {
            TempGrid[x, y] = new int[0];
            //TempGrid[x, y].Possibles.Clear();
        }
        #endregion

        #region Advanced
        /// <summary>
        /// Call this method only for all Advanced section access
        /// </summary>
        private void AdvancedFill(bool clearafter = true)
        {
            //Array.Clear(TempGrid, 0, TempGrid.Length); //clear all
            //ReInitializeTemp();
            for (int x = 0; x < FullGridWidth; x += SingleBlockWidth)
            {
                for (int y = 0; y < FullGridWidth; y += SingleBlockWidth)
                {
                    AdvancedInBlockCheck(x, y, clearafter);
                }
            }
        }

        /// <summary>
        /// Gets inblock possible distinct values
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="includeself"></param>
        /// <returns></returns>
        private int[] InBlockPossible(int x, int y, bool includeself = true)
        {
            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);
            List<int> poss = new List<int>();

            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if ((!includeself) && ((xa == x) && (ya == y))) continue; //skipself
                    if (Grid[xa, ya] != 0) continue; //skip filled in
                    //poss.AddRange(TempGrid[xa, ya]);
                    poss.AddRange(GetPossible(xa, ya));
                }
            }
            return poss.Distinct().ToArray();
        }

        private void AdvancedInBlockCheck(int x, int y, bool clearafter = true)
        {
            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (Grid[xa, ya] != 0) { if (clearafter) { CleanTempGrid(xa, ya); } continue; }//skip if filled
                    List<int> inboxexceptselfpossible = InBlockPossible(xa, ya, false).ToList();
                    //var selfpossible = TempGrid[xa, ya]; //DON'T USE!!! CAUSES SERIOUS ERROR
                    var selfpossible = GetPossible(xa, ya);
                    //TempGrid[xa, ya] = selfpossible;
                    //TempGrid[xa, ya].Possibles = selfpossible.ToList();
                    var diff = selfpossible.Except(inboxexceptselfpossible).ToList();
                    if (diff.Count == 1)
                    {
                        Grid[xa, ya] = diff[0];
                        UnfilledCount--;
                        CleanTempGrid(xa, ya);
                    }
                }
            }
        }

        #endregion

        #region Leftover possible value check
        /// <summary>
        /// Checks the innerbox leftover
        /// </summary>
        private void CheckInnerBoxLeftOver()
        {
            for (int x = 0; x < FullGridWidth; x += SingleBlockWidth)
            {
                for (int y = 0; y < FullGridWidth; y += SingleBlockWidth)
                {
                    int c = CountInnerBlockEmpty(x, y);
                    if (c == 1)
                    {
                        int[] filledvals = GetFilledInner(x, y);
                        Point[] empts = GetInnerEmpty(x, y, false);
                        if (empts.Length == 0) continue;

                        int[] except = FullInts.Except(filledvals).ToArray();
                        Grid[empts[0].x, empts[0].y] = except[0];
                        RemoveItemFromTemp(except[0], ref TempGrid[empts[0].x, empts[0].y]);
                        UnfilledCount--;

                    }
                    else
                    {
                        Point[] innerunfilled = GetInnerEmpty(x, y, false);
                        int count = innerunfilled.Length;
                        int prev = 0;
                        do
                        {
                            foreach (Point item in innerunfilled)
                            {
                                int[] poss = GetPossible(item.x, item.y, true);
                                if (poss.Length == 1)
                                {
                                    Grid[item.x, item.y] = poss[0];
                                    RemoveItemFromTemp(poss[0], ref TempGrid[item.x, item.y]);
                                    count--;
                                    UnfilledCount--;
                                }
                            }
                            if (prev == count) break;
                            prev = count;
                        } while (count != 0);

                        /*
                        foreach (Point item in innerunfilled)
                        {
                            int[] poss = GetPossible(item.x, item.y, true);
                            if (poss.Length == 1)
                            {
                                Grid[item.x, item.y] = poss[0];
                                RemoveItemFromTemp(poss[0], ref TempGrid[item.x, item.y]);
                                UnfilledCount--;
                            }
                        }*/
                    }
                }
            }
        }

        /// <summary>
        /// Counts the amount of empty blocks inside the innerblock
        /// </summary>
        /// <param name="x">xpos</param>
        /// <param name="y">ypos</param>
        /// <returns>Empty count</returns>
        private int CountInnerBlockEmpty(int x, int y)
        {
            int track = 0;
            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);

            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (Grid[xa, ya] == 0) track++;
                }
            }
            return track;
        }
        private int CountRowEmpty(int pos, Axis axis)
        {
            int track = 0;
            switch (axis)
            {
                case Axis.HORIZONTAL:
                    for (int y = 0; y < FullGridWidth; y++)
                    {
                        if (Grid[pos, y] == 0) track++;
                    }
                    return track;
                case Axis.VERTICAL:
                    for (int x = 0; x < FullGridWidth; x++)
                    {
                        if (Grid[x, pos] == 0) track++;
                    }
                    return track;
            }
            return track;
        }
        private int[] GetFilledInner(int x, int y)
        {
            List<int> track = new List<int>();
            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);

            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (Grid[xa, ya] != 0) track.Add(Grid[xa, ya]);
                }
            }
            return track.ToArray();
        }
        private enum Axis
        {
            HORIZONTAL,
            VERTICAL
        }
        private int[] GetFilledInRow(int pos, Axis axis, int from = 0)
        {
            List<int> track = new List<int>();
            switch (axis)
            {
                case Axis.HORIZONTAL:
                    for (int y = from; y < FullGridWidth; y++)
                    {
                        if (Grid[pos, y] != 0) track.Add(Grid[pos, y]);
                    }
                    //track.Sort(); //maybe not required;
                    return track.ToArray();
                case Axis.VERTICAL:
                    for (int x = from; x < FullGridWidth; x++)
                    {
                        if (Grid[x, pos] != 0) track.Add(Grid[x, pos]);
                    }
                    //track.Sort(); //maybe not required;
                    return track.ToArray();
            }
            return track.ToArray(); //will never happen
        }

        private void RemoveItemFromTemp(int num, ref int[] arr)
        {
            Queue<int> queue = new Queue<int>();
            foreach (int item in arr)
            {
                if (item == num) continue;
                queue.Enqueue(item);
            }
            arr = queue.ToArray();
        }
        private void RemoveItemsFromTemp( int[] arr, IEnumerable<int> toremove)
        {
            Queue<int> queue = new Queue<int>();
            foreach (int val in toremove)
            {
                foreach (int item in arr)
                {
                    if (item == val) continue;
                    queue.Enqueue(item);
                }
            }
            arr = queue.ToArray();

        }
        private void RemoveItemFromBlock(int num, int x, int y)
        {
            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);

            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (xpos - SingleBlockWidth); ya < ypos; ya++)
                {
                    RemoveItemFromTemp(num, ref TempGrid[xa, ya]);
                }
            }
        }

        /// <summary>
        /// Checks the horizontal + vertical leftover
        /// </summary>
        /// <summary>
        /// Checks the horizontal + vertical leftover
        /// </summary>
        private void CheckHVLeftOver()
        {
            int yaxis = 0;
            Queue<HVItem> PossibleLocs = new Queue<HVItem>();

            for (int x = 0; x < FullGridWidth; x++) //= SingleBlockWidth)
            {
                for (int y = yaxis; y < FullGridWidth; y++)//= SingleBlockWidth)
                {

                    #region Xrow

                    int xrowempty = CountRowEmpty(x, Axis.HORIZONTAL);

                    int[] xfilledvals = GetFilledInRow(x, Axis.HORIZONTAL);
                    Point[] xempts = GetHorizontalEmpty(x, y, 0, true, true);
                    int[] xexcept = FullInts.Except(xfilledvals).ToArray();
                    if (xempts.Length == 0) goto YROW;
                    if (xrowempty == 1)
                    {
                        Grid[xempts[0].x, xempts[0].y] = xexcept[0];
                        //RemoveItemFromTemp(xexcept[0], TempGrid[xempts[0].x, xempts[0].y]).ToArray();
                        foreach (Point p in xempts)
                        {
                            RemoveItemFromTemp(xexcept[0], ref TempGrid[p.x, p.y]);
                        }
                        TempGrid[xempts[0].x, xempts[0].y] = new int[0];
                        UnfilledCount--;
                        //goto CONTINUE;
                        //if filled in, dont go check next
                    }
                    else
                    {
                        #region Else

                        foreach (Point item in xempts)
                        {
                            HVItem newHVItem = new HVItem(item);
                            foreach (int num in xexcept)
                            {
                                if (TempGrid[item.x, item.y].Contains(num)) newHVItem.Nums.Enqueue(num);
                            }
                            PossibleLocs.Enqueue(newHVItem); //location, 1 match
                        }

                        foreach (HVItem li in PossibleLocs)
                        {
                            if (li.Nums.Count == 1)
                            {
                                Point p = li.Point;
                                int num = li.Nums.Dequeue();
                                Grid[p.x, p.y] = num;
                                UnfilledCount--;
                                RemoveItemFromTemp(num, ref TempGrid[p.x, p.y]);
                                for (int i = 0; i < FullGridWidth; i++)
                                {
                                    RemoveItemFromTemp(num, ref TempGrid[x, i]);
                                }
                            }
                        }
                        PossibleLocs.Clear();
                        #endregion
                    }
                    #endregion
                YROW:
                    #region Yrow

                    int yrowempty = CountRowEmpty(y, Axis.VERTICAL);

                    int[] yfilledvals = GetFilledInRow(y, Axis.VERTICAL);
                    Point[] yempts = GetVerticalEmpty(x, y, 0, true, true);//GetInnerEmpty(x,y, false);
                    int[] yexcept = FullInts.Except(yfilledvals).ToArray();

                    if (yempts.Length == 0) continue;
                    if (yrowempty == 1)
                    {
                        Grid[yempts[0].x, yempts[0].y] = yexcept[0];
                        //RemoveItemFromTemp(yexcept[0], TempGrid[yempts[0].x, yempts[0].y]).ToArray();
                        foreach (Point p in yempts)
                        {
                            RemoveItemFromTemp(yexcept[0], ref  TempGrid[p.x, p.y]);
                        }
                        TempGrid[yempts[0].x, yempts[0].y] = new int[0];
                        UnfilledCount--;
                        //goto CONTINUE;
                        //if filled in, dont go check next
                    }
                    else
                    {
                        #region Else

                        foreach (Point item in yempts)
                        {
                            HVItem newHVItem = new HVItem(item);
                            foreach (int num in yexcept)
                            {
                                if (TempGrid[item.x, item.y].Contains(num)) newHVItem.Nums.Enqueue(num);
                            }
                            PossibleLocs.Enqueue(newHVItem); //location, 1 match
                        }

                        foreach (HVItem li in PossibleLocs)
                        {
                            if (li.Nums.Count == 1)
                            {
                                Point p = li.Point;
                                int num = li.Nums.Dequeue();
                                Grid[p.x, p.y] = num;
                                UnfilledCount--;
                                RemoveItemFromTemp(num, ref TempGrid[p.x, p.y]);
                                for (int i = 0; i < FullGridWidth; i++)
                                {
                                    RemoveItemFromTemp(num, ref TempGrid[i, y]);
                                }
                            }
                        }
                        PossibleLocs.Clear();

                        #endregion
                    }
                    #endregion
                }
                yaxis++;
            }
        }
        #endregion

        #region Impossibles
        private int[] GetInnerBlockRowImpossible(int x, int y, bool includeself = true)
        {
            //x, y-> 0-8
            //List<int> pos = new List<int>();
            List<string> pos = new List<string>();


            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);
            //3x3 from inner block
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                if ((!includeself) && (xa == x)) continue; //skip 
                foreach (int item in GetRowImpossible(xa, y))
                {
                    pos.Add(item.ToString());
                }
            }
            for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
            {
                if ((!includeself) && (ya == x)) continue; //skip 
                foreach (int item in GetRowImpossible(x, ya))
                {
                    pos.Add(item.ToString());
                }
            }
            //Console.Clear();
            //foreach (var item in GetPossible(x, y))
            //{
            //    Console.WriteLine(item);
            //}
            int[] ret = pos.Distinct().Select(int.Parse).ToArray().Except(GetPossible(x, y)).ToArray();
            return ret;
            //return GetPossible(x, y).Except(pos.Distinct().Select(int.Parse).ToArray()).ToArray();
            //return pos.Distinct().Select(int.Parse).ToArray();//remove duplicates, return list as int[]
        }

        /// <summary>
        /// Fills the whole TempGrid with possible values
        /// </summary>
        private void FillTemp()
        {
            //fill each tempgrid block with possible values
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    if (Grid[x, y] != 0)
                    {
                        TempGrid[x, y] = new int[0];
                        continue;
                    }
                    TempGrid[x, y] = GetPossible(x, y);

                    //TempGrid[x, y].Possibles.AddRange(GetPossible(x, y)); //add possible values

                }
            }
        }
        private void FillTemp2()
        {
            //各マスの可能性数値を取得
            //同じ3x3マス内に、同じ数値を持つマスを取得
            //自分のマスから縦横両方（自分のマス内は無視）で、空のマスを取得
            //各空のマスに、この数値は当てはまるかを拾う。もし、合計が０だった場合、この数値はこのマスに確定する。０じゃなかった場合、次の3x3内の可能性マスを試す。


            //MUST USE Clone() to prevent backupGrid getting overwritten when Grid is changed;
            int[,][] backupGrid = TempGrid.Clone() as int[,][];
            //TempBlock[,] backupGrid = TempGrid.Clone() as TempBlock[,];
            bool gridsame = false;
            while (!gridsame)
            {
                for (int x = 0; x < FullGridWidth; x++)
                {
                    for (int y = 0; y < FullGridWidth; y++)
                    {
                        if (Grid[x, y] != 0) continue; // no need to care for filled in

                        Point[] gxre = GetHorizontalEmpty(x, y, 0, true);
                        Point[] gyre = GetVerticalEmpty(x, y, 0, true);

                        if (TempGrid[x, y].Length == 1)
                        {
                            int val = TempGrid[x, y][0];
                            Grid[x, y] = val;
                            UnfilledCount--;
                            CleanTempGrid(x, y);
                            foreach (Point p in gxre)
                            {
                                RemoveItemFromTemp(val, ref  TempGrid[p.x, p.y]);
                            }
                            foreach (Point p in gyre)
                            {
                                RemoveItemFromTemp(val, ref TempGrid[p.x, p.y]);
                            }
                            CleanTempGrid(x, y);
                            continue;
                        }
                    }
                }


                gridsame = TempGridSame(backupGrid);
                if (gridsame) break; //if advancedfill & checkleftover2 cant handle it anymore
                backupGrid = Grid.Clone() as int[,][];
                //backupGrid = TempGrid.Clone() as TempBlock[,]; //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed
            }
            TempGrid = backupGrid;



        }

        private Point[] GetHVEmpty(int pos, Axis axis, int from = 0)
        {
            Queue<Point> track = new Queue<Point>();
            switch (axis)
            {
                case Axis.HORIZONTAL:
                    for (int y = from; y < FullGridWidth; y++)
                    {
                        if (Grid[pos, y] == 0) track.Enqueue(new Point(pos,y));
                    }
                    //track.Sort(); //maybe not required;
                    return track.ToArray();
                case Axis.VERTICAL:
                    for (int x = from; x < FullGridWidth; x++)
                    {
                        if (Grid[x, pos] == 0) track.Enqueue(new Point(x, pos));
                    }
                    //track.Sort(); //maybe not required;
                    return track.ToArray();
            }
            return track.ToArray(); //will never happen
        }

        private Point[] GetRowEmpty(int x, int y, bool includeinnerself = false, bool includeself = false)
        {
            Queue<Point> vals = new Queue<Point>();
            int innery = GetInnerRange(y);
            int innerx = GetInnerRange(x);
            //from x
            for (int a = 0; a < FullGridWidth; a++)
            {
                if (!includeinnerself) if ((a >= innery - SingleBlockWidth) && (a < innery) && (a < innerx)) continue;
                if ((!includeself) && ((a == y) || (a == x))) continue;
                if (Grid[x, a] == 0)
                {
                    vals.Enqueue(new Point() { x = x, y = a });
                }
                if (Grid[a, y] == 0)
                {
                    vals.Enqueue(new Point() { x = a, y = y });
                }
            }
            return vals.ToArray();
        }
        
        /// <summary>
        /// DON'T USE UNLESS FULLY UNDERSTAND WHAT ARE YOU DOING
        /// Originally GetXRowEmpty()
        /// </summary>
        /// <param name="vertical"></param>
        /// <param name="horizontal"></param>
        /// <param name="from"></param>
        /// <param name="includeinnerself"></param>
        /// <param name="includeself"></param>
        /// <returns></returns>
        private Point[] GetHorizontalEmpty(int vertical, int horizontal, int from = 0, bool includeinnerself = false, bool includeself = false)
        {
            Queue<Point> vals = new Queue<Point>();
            int innery = GetInnerRange(horizontal);
            //from x
            for (int a = from; a < FullGridWidth; a++)
            {
                if (!includeinnerself) if ((a >= innery - SingleBlockWidth) && (a < innery)) continue;
                if ((!includeself) && (a == horizontal)) continue;
                if (Grid[vertical, a] == 0)
                {
                    vals.Enqueue(new Point() { x = vertical, y = a });
                }
            }
            return vals.ToArray();
        }
        /// <summary>
        /// DON'T USE UNLESS FULLY UNDERSTAND WHAT ARE YOU DOING
        /// Originally GetYRowEmpty()
        /// </summary>
        /// <param name="vertical"></param>
        /// <param name="horizontal"></param>
        /// <param name="from"></param>
        /// <param name="includeinnerself"></param>
        /// <param name="includeself"></param>
        /// <returns></returns>
        private Point[] GetVerticalEmpty(int vertical, int horizontal, int from = 0, bool includeinnerself = false, bool includeself = false)
        {
            Queue<Point> vals = new Queue<Point>();
            int innerx = GetInnerRange(vertical);
            //from y
            for (int b = from; b < FullGridWidth; b++)
            {
                if (!includeinnerself) if ((b >= innerx - SingleBlockWidth) && (b < innerx)) continue;
                if ((!includeself) && (b == vertical)) continue;
                if (Grid[b, horizontal] == 0)
                {
                    vals.Enqueue(new Point() { x = b, y = horizontal });
                }
            }
            return vals.ToArray();

        }

        /// <summary>
        /// Gets all possible values
        /// </summary>
        /// <param name="x">x loc</param>
        /// <param name="y">y loc</param>
        /// <returns>All possible values for given (x,y) coordinate</returns>
        private int[] GetPossible(int x, int y, bool notincludeself = false)
        {
            List<int> impossiblevals = GetInnerBlock(x, y, notincludeself).Concat(GetRowImpossible(x, y)).ToList<int>();
            impossiblevals = impossiblevals.Distinct().ToList(); //remove duplicate
            return FullInts.Except(impossiblevals.ToArray()).ToArray<int>(); //returns possible values
        }
        private Point[] GetInnerEmpty(int x, int y, bool notincludeself = true)
        {
            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);
            Queue<Point> pt = new Queue<Point>();
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if ((notincludeself) && (xa == x) && (ya == y)) continue; //skip self if notincludeself
                    if (Grid[xa, ya] == 0) pt.Enqueue(new Point() { x = xa, y = ya });
                }
            }
            return pt.ToArray();
        }

        /// <summary>
        /// Get the last index of the block
        /// </summary>
        /// <param name="loc"></param>
        /// <returns></returns>
        private int GetInnerRange(int loc)
        {
            return (((loc / SingleBlockWidth) + 1) * SingleBlockWidth);
            #region Previous Code
            //code was good, but better and prob. faster replacement found

            //for (int i = SingleBlockWidth; i <= (FullGridWidth); i += SingleBlockWidth)
            //{
            //    if (loc <= i - 1)
            //    {
            //        return i;
            //    }
            //}
            //return 0;
            #endregion


            #region Previous Code
            //code below works, but code above works faster and is simplier

            //List<int[]> RangeList = new List<int[]>();
            //List<int> tmplst = new List<int>();

            //int next = 0;
            //for (int i = 0; i < SingleBlockWidth; i++)
            //{
            //    for (int x = next; x < (next + SingleBlockWidth); x++)
            //    {
            //        tmplst.Add(x);
            //    }
            //    next += SingleBlockWidth;
            //    RangeList.Add(tmplst.ToArray());
            //    tmplst.Clear();
            //}
            //int index = RangeList.FindIndex(a => a.Contains(loc)); //return the section's last item index (1~)
            //int indexcalc = ((index * SingleBlockWidth) + SingleBlockWidth);
            //return indexcalc;
            #endregion


            #region Previous code
            //Print out
            //foreach (int[] item in RangeList)
            //{
            //    foreach (int it in item)
            //    {
            //        Console.Write(it + " ");
            //    }
            //    Console.WriteLine();
            //}

            //Only supports for normal 9x9 sudoku
            //3-9, {012} {345} {678}
            //switch (loc)
            //{
            //    case 0:
            //    case 1:
            //    case 2:
            //        return 3;
            //    case 3:
            //    case 4:
            //    case 5:
            //        return 6;
            //    case 6:
            //    case 7:
            //    case 8:
            //        return 9;
            //}
            //return 0; //will never happen, but to pass compiler
            #endregion
        }

        /// <summary>
        /// Gets IMPOSSIBLE values
        /// Gets all the existing values in the specified xy range
        /// </summary>
        /// <param name="x">x axis</param>
        /// <param name="y">y axis</param>
        /// <returns>IMPOSSIBLE values</returns>
        public int[] GetInnerBlock(int x, int y, bool notincludeself = false)
        {
            //x, y-> 0-8
            //List<int> pos = new List<int>();
            //List<string> pos = new List<string>();
            int[] pos = new int[FullGridWidth];//items should be less than FullGridWidth
            int pointer = 0;


            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);
            //3x3
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if ((notincludeself) && (xa == x) && (ya == y)) continue; //skip self if notincludeself
                    if (Grid[xa, ya] != 0)
                    {
                        pos[pointer] = (Grid[xa, ya]);
                        pointer++;
                    }
                }
            }
            return pos.Distinct().ToArray();
            //return pos.Distinct().Select(int.Parse).ToArray();//remove duplicates, return list as int[]
        }

        /// <summary>
        /// Gets the IMPOSSIBLE values in the horizontal vertical line across the specified location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int[] GetRowImpossible(int x, int y)
        {
            //List<int> values = new List<int>();
            //List<string> values = new List<string>();
            int[] values = new int[FullGridWidth * 2];
            int pointer = 0;

            //from x
            for (int a = 0; a < FullGridWidth; a++)
            {
                if (Grid[x, a] != 0)
                {
                    values[pointer] = Grid[x, a];
                    pointer++;
                }
                //if (char.IsNumber((char)Grid[x, a].ToString()[0]))
                //{
                //    values.Add(Grid[x, a]);
                //    //num = int.Parse(Grid[x, a]);
                //    //char charac = (char)num;
                //    //values.Add(charac.ToString());
                //}
            }
            //from y
            for (int b = 0; b < FullGridWidth; b++)
            {
                if (Grid[b, y] != 0)
                {
                    values[pointer] = Grid[b, y];
                    pointer++;
                }
                //if (char.IsNumber((char)Grid[b, y].ToString()[0]))
                //{
                //    values.Add(Grid[b, y]);
                //    //num = int.Parse(Grid[b, y]);
                //    //char charac = (char)num;
                //    //values.Add(charac.ToString());
                //}
            }
            //int[] ret = values.Distinct().Select(int.Parse).ToArray(); //remove duplicates and return IMPOSSIBLE values
            return values.Distinct().ToArray();
        }
        #endregion

        #region Initialize
        /// <summary>
        /// Initializes Grid, TempGrid, FullInts, Separate lines size, and Tempgrid TempBlock item
        /// </summary>
        private void Initialize()
        {
            Grid = new int[FullGridWidth, FullGridWidth]; //MainGrid
            TempGrid = new int[FullGridWidth, FullGridWidth][];
            //TempGrid = new TempBlock[FullGridWidth, FullGridWidth]; //TempGrid

            //Fullints initialize
            //List<int> ints = new List<int>();

            FullInts = new int[FullGridWidth];
            for (int i = 1; i <= FullGridWidth; i++)
            {
                //ints.Add(i);
                FullInts[i - 1] = i;
            }
            //FullInts = ints;//.ToArray();
            //ints.Clear();

            //Tempgrid possible list
            //for (int x = 0; x < FullGridWidth; x++)
            //{
            //    for (int y = 0; y < FullGridWidth; y++)
            //    {
            //        TempGrid[x, y] = new TempBlock();
            //    }
            //}
            validator = new Validator(SingleBlockWidth,FullInts);
        }
        #endregion

        #region Support
        private void Clone(int[,][] obj, ref int[,][] destination)
        {
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    destination[x, y] = obj[x, y];
                }
            }
        }
        #endregion
    }

}