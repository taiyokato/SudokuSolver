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
        public int[][] Grid;
        /// <summary>
        /// Backup grid for while looping
        /// </summary>
        public int[][] BackupGrid;
        /// <summary>
        /// Temp grid for holding possible values
        /// サブ表。マスに入れる可能性の値を保管する
        /// </summary>
        public int[][][] TempGrid;
        //private TempBlock[,] TempGrid; //[x, y]
        /// <summary> 
        /// All possible values array used for filter out possible values
        /// 可能の値の配列。不可能値の排除に使う
        /// </summary>
        public int[] FullInts;
        protected int? _singleblockw;
        /// <summary> 
        /// For accessing nullable int singleblockw 
        /// Nullable int singleblockwをアクセスするために。singleblockwがnullならデフォルトの3を返す
        /// </summary>
        public int SingleBlockWidth
        {
            get { return (_singleblockw.HasValue) ? _singleblockw.Value : 3; }
            set { _singleblockw = value; }
        }
        /// <summary> 
        /// Shortcut for getting full grid width since n^2 
        /// 表のフルサイズを取得するショートカット。表のサイズはSingleBlockWidthのn^2
        /// </summary>
        public int FullGridWidth
        {
            get { return (SingleBlockWidth * SingleBlockWidth); }
        }
        /// <summary>
        /// Total number of blocks in the grid
        /// </summary>
        public int FullGridCount
        {
            get { return FullGridWidth * FullGridWidth; }
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

        public void Solve(bool skipprint)
        {
            //dx = 6;
            //dy = 2;
            //AdvancedHook(9);
            UnfilledCount = 0;
            GetFilledCount();
            Stopwatch stop = new Stopwatch();
            if (!validator.GridReadyValidate(ref Grid, FullGridWidth)) { Console.WriteLine("Invalid at row: {0}", validator.BreakedAt); goto FINISH; }


            stop.Start();
            Basic(); //preparation

            if ((validator.Validate2(ref Grid, UnfilledCount, out success)) && (!success)) { Console.WriteLine("Invalid at row: {0}", validator.BreakedAt); goto FINISH; }
            if (UnfilledCount <= 0) goto FINISH; //if finished at this point, jump to finish
            if (!skipprint)
            {
                Console.WriteLine("Basic try:");
                PrintAll();
            }
            //Console.WriteLine(DateTime.Now.Subtract(now));



            Advanced();
            if ((validator.Validate2(ref Grid, UnfilledCount, out success)) && (!success)) { Console.WriteLine("Invalid at row: {0}", validator.BreakedAt); goto FINISH; }
            if (UnfilledCount <= 0) goto FINISH; //if finished at this point, jump to finish
            if (!skipprint)
            {
                Console.WriteLine("Advanced try:");
                PrintAll();
            }

            //Logic();
            //Console.WriteLine("Logical try:");
            //if (UnfilledCount==0) goto FINISH; //if finished at this point, jump to finish
            //PrintAll();

            //Console.WriteLine(DateTime.Now.Subtract(now));
            Console.WriteLine("Backtrack solving...");

            finished = TreeDiagramSolve();

        FINISH:
            //if (treesuccess) { Console.WriteLine("Result:"); }
            stop.Stop();
            //ClearLine();
            Console.WriteLine("Result:");
            PrintAll();
            Console.WriteLine("Time spent: {0}", TimeSpan.FromMilliseconds(stop.ElapsedMilliseconds));

            bool validated = validator.FinalValidate(ref Grid, FullGridWidth);
            //bool validated = validator.Validate2(ref Grid, UnfilledCount, out success);
            Console.WriteLine("Grid Check: {0}", (validated) ? "Valid" : "Invalid");
            //Console.WriteLine("Grid Check: {0}", (validated && success) ? "Valid" : "Invalid");
            if (!success) Console.WriteLine("Invalid at row: {0}", validator.BreakedAt);
            
            Console.WriteLine("[EOF]");
            Console.WriteLine("Press enter to finish");


            Console.Read();
            Console.Read();

        }
        public Solver(bool fileread = false, bool skipprint = false)
        {
            if (fileread)
            {
                Reader.ProcessRaw();
                SingleBlockWidth = Reader.SingleSize;
                Initialize();
                Grid = Reader.ProcessedGrid;
                Solve(skipprint);
                return;
            }
        //Get the size first, then initialize Grid
        SETSIZE: //label for reset grid size
            Console.WriteLine("Input full grid width: ");
            Console.WriteLine("Grid size must be greater than 9, and √size must be a whole number");            
            ReadLength();//set the grid length

            Console.Clear(); //Clear console
            Console.WriteLine("Grid size will be: {0}x{0}", FullGridWidth);
            Console.WriteLine();
            //initialize
            Initialize();



            Console.WriteLine("Input Each line\nEnter empty values as x\nSeparate values with a space: ");
            Console.WriteLine("Input \"resize\" to re-set grid size");
            //Console.WriteLine("Input \"redo\" to re-enter line");
            if (ReadLines()) { Console.Clear(); goto SETSIZE; } //if ReadLines return true, reset grid size
            Console.WriteLine();
            Console.WriteLine("Input values are: ");
            PrintAll();
            Console.WriteLine("Press enter key to continue...");
            Console.Read();

            Solve(skipprint);
        }

        #region Table Copy
        /// <summary>
        /// Copies grid from source->target
        /// </summary>
        public void CopyAll(int[][] source,int[][] target)
        {
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int i = 0; i < FullGridWidth; i++)
                {
                    target[x][i] = source[x][i];
                }
            }
        }

        #endregion

        void Basic()
        {
            FillTemp();
            

            //PatternInitialize();
            //PatternMatch();
            //CopyAll(Grid, BackupGrid);
            do
            {
                for (int x = 0; x < FullGridWidth; x++)
                {
                    for (int y = 0; y < FullGridWidth; y++)
                    {
                        if (Grid[x][y] != 0) continue; // no need to care for filled in
                    
                        if (TempGrid[x][y].Length == 1)
                        {
                            int val = TempGrid[x][y][0];
                            Grid[x][y] = val;
                            UnfilledCount--;
                            //CleanTempGrid(x, y);
                            ClearRelativeTemp(x, y, val);
                        }
                    }
                }
                
                if (UnfilledCount <= 0) break; //early break before copying

                if (GridSame(BackupGrid)) break; //if advancedfill & checkleftover2 cant handle it anymore
                CopyAll(Grid, BackupGrid);
            } while (UnfilledCount > 0);


            if (UnfilledCount == 0) return;
            CopyAll(Grid, BackupGrid);
            do
            {
                CheckHVLeftOver();
                if (UnfilledCount <= 0) break; //early break before copying

                if (GridSame(BackupGrid)) break; //if advancedfill & checkleftover2 cant handle it anymore
                CopyAll(Grid, BackupGrid);
            } while (UnfilledCount > 0);

            //UNDER EXPERIMENT 
            /*
            while (false)
            {
                //NOT WORKING AS FULLY EXPECTED
                Deductive();
                if (backupgrid == Grid) break;
                backupgrid = Grid;
            }//*/

            //PrintAll();


        }
        void Advanced()
        {
            //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed;
            //int[,] backupGrid = Grid.Clone() as int[,];
            do
            {
                AdvancedFill(true);
                CheckHVLeftOver();
                //CheckInnerBoxLeftOver();
                for (int x = 0; x < FullGridWidth; x+=SingleBlockWidth)
                {
                    for (int y = 0; y < FullGridWidth; y+=SingleBlockWidth)
                    {
                        foreach (Point p in GetInnerEmpty(x, y))
                        {
                            IEnumerable<int> hvleft = FullInts.Except(GetFilledInRow(p.y, Axis.VERTICAL).Concat(GetFilledInRow(p.x,Axis.HORIZONTAL)));
                            IEnumerable<int> inleft = FullInts.Except(GetFilledInner(x, y));
                            int[] intersect = inleft.Intersect(hvleft).ToArray();
                            
                            //int[] inter = FullInts.Except(GetFilledInner(x, y)).Intersect(FullInts.Except().ToArray();
                            //int[] intersect = TempGrid[p.x][p.y].Intersect(inter).ToArray();



                            if (intersect.Length == 1)
                            {
                                Grid[p.x][p.y] = intersect[0];
                                ClearRelativeTemp(p.x, p.y, intersect[0]);
                                UnfilledCount--;
                                continue;
                            }
                        }
                    }
                }


                if (GridSame(BackupGrid)) break; //if advancedfill & checkleftover2 cant handle it anymore
                CopyAll(Grid, BackupGrid);
                //BackupGrid = Grid.Clone() as int[,]; //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed;
            } while (UnfilledCount > 0);
            
        }


        bool TreeDiagramSolve()
        {
            //create TreeDiagram instance for easier code management
            TreeDiagram td = new TreeDiagram(Grid, SingleBlockWidth, FullInts, TempGrid);
            td.UnfilledCount = UnfilledCount;
            td.Execute5(td.EntryPoint);

            //td.Execute2(ref UnfilledCount); //probably faster and lighter than Execute()
            //Execute3(ref UnfilledCount); //STILL TESTING THE CODE
            Grid = td.Grid;
            TempGrid = td.TempGrid;
            return td.FinishFlag;
        }


        /// <summary>
        /// Checks if Grid and BackupGrid are exactly same
        /// </summary>
        private bool GridSame(int[][] backup)
        {
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    if ((Grid[x][y] != backup[x][y]) ) return false;
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
                    List<string> lines = new List<string>(); //must use. too dynamic
                    //string[] lines = new string[SingleBlockWidth];
                    System.IO.StreamReader file = new System.IO.StreamReader(line);
                    string fline = string.Empty;
                    //int cnt = 0;
                    while ((fline = file.ReadLine()) != null)
                    {
                        lines.Add(fline.Trim());
                        //lines[cnt] = fline.Trim();
                        //cnt++;
                    }
                    file.Close();



                    for (int a = 0; a < FullGridWidth; a++)
                    {
                        string[] splitted = lines[a].Split(' ');
                        //splitted.ToList().ForEach(c => c.Trim());
                        int pt = 0;
                        for (int b = pt; b < splitted.Length; b++)
                        {
                            string item = splitted[b].Trim();
                            Grid[a][b] = (item.Equals("x")) ? 0 : int.Parse(splitted[b]);
                        }
                        pt = 0;

                    }
                    break;
                }
                #endregion
                if (line.Trim().ToLower().Equals("resize")) return true;
                if (line.Trim().Equals("")) { i--; ClearLine(); continue; }
                if (!LineValid(line)) { i--; ClearLine(); continue; }//if invalid, reod
                string[] split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); //split input line into pieces
                for (int x = 0; x < FullGridWidth; x++)
                {
                    

                    Grid[i][x] = (split[x].Equals("x")) ? 0 : int.Parse(split[x].Trim());
                }
            }
            return false;
        }
        private void ReadLength()
        {
            //if (!LineValid(line)) { i--; ClearLine(); continue; }//if invalid, reod
            string line = Console.ReadLine();
            double testval = -1;

            while (true)
            {

                try
                {
                    testval = Math.Sqrt(double.Parse(line));
                    //size cannot be 1x1, grids size less than 9x9 are too annoying

                    //THE MOST IMPORTANT PART OF THIS METHOD
                    if ((testval >= 3) && (testval % 1 == 0)) break; //check if testval is a valid number of n^n, by seeing if self rooting results a whole number
                }
                catch (Exception)
                {
                }
                ClearLine();
                line = Console.ReadLine();
            }
            SingleBlockWidth = (int)testval;
        }
        private void ClearLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            int nextrentLinenextsor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, nextrentLinenextsor);
        }
        private bool LineValid(string values)
        {
            //leave it this way until find better pattern matching
            System.Text.RegularExpressions.MatchCollection matchcol = new System.Text.RegularExpressions.Regex(@"(?:[0-9]{1,2}|x{1})").Matches(values);
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
                    if (Grid[x][y] == 0) UnfilledCount++;
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
                    Grid[i][x] = 0;
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
                    Grid[i][x] = (item[x].Equals("x")) ? 0 : int.Parse(item[x].ToString());
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
                    Grid[i][x] = (item.Equals("x")) ? 0 : int.Parse(split[x]);
                }
                Console.Write("\n");
            }


        }

        #endregion



        #region Debug
        private int? dx = null, dy = null;
        private void HookEventHandler(int cx, int cy)
        {
            if ((dx == null) || (dy == null)) return; // do nothing if axis not specified
            if ((dx > FullGridWidth - 1) || (dy > FullGridWidth - 1)) return; // do nothing if specifid hook axis are OOB
            if (!(cx == dx.Value) || !(cy == dy.Value)) return;
            Print3x3(dx.Value, dy.Value);
            try
            {
                throw new HookedPointReachedException(string.Format("[{0},{1}] hooked", dx, dy));
            }
            catch (HookedPointReachedException hpre)
            {
                PrintAll();
                System.Diagnostics.Debug.WriteLine("");
                
            }
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
                    if (Grid[xa][ya] != 0) continue; // no need to print out possible values for filled in
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
            try
            {
                throw new HookedPointReachedException(string.Format("[{0},{1}] hooked", dx,dy));
            }
            catch (HookedPointReachedException hpre)
            {
                System.Diagnostics.Debug.WriteLine("");
            }
        }
        #endregion
        #region Printout
        private void PrintHorizontalBorder(bool withnewline = false, bool headerfooter = false)
        {
            string outstr = (headerfooter) ? "-" : "|";

            bool outflag = false;

            int itemwidth = (Math.Floor(Math.Log10(Math.Abs(FullGridWidth)) + 1) > 1) ? 3 : 2; //2 char + 1 space, 1 char + 1 space

            for (int a = 0; a < SingleBlockWidth; a++) //all segments
            {
                for (int b = 0; b < itemwidth; b++) //each segment
                {
                    for (int c = 0; c < SingleBlockWidth - 1; c++) //segment switch
                    {
                        outstr += '-';
                    }
                    outstr += (a == SingleBlockWidth - 1 && b == itemwidth - 1) ? "" : (headerfooter) ? "-" : (b == itemwidth - 1) ? "+" : "-"; //if last segment, add +
                }
            }
            outstr += (!headerfooter) ? '|' : '-';
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
                    int block = Grid[x][y];
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
            for (int i = 0; i < TempGrid[x][y].Length; i++)
            {
                Console.Write(TempGrid[x][y][i] + " ");
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
                    if (Grid[xa][ya] != 0) continue; // no need to print out possible values for filled in
                    int[] poss = TempGrid[xa][ya];//GetPossible(xa, ya);
                    System.Diagnostics.Debug.Write(string.Format(@"[{0},{1}]", xa, ya));
                    //Console.WriteLine(@"[{0},{1}]", xa, ya);
                    poss.ToList().ForEach(new Action<int>(a =>
                    {
                        System.Diagnostics.Debug.Write(a + " ");
                        //Console.Write(a + " ");
                    }));
                    System.Diagnostics.Debug.WriteLine("\n");
                    //Console.Write("\n");
                }
            }
            System.Diagnostics.Debug.WriteLine("");
            //Console.WriteLine();
        }
        private void PrintTempGridCount()
        {
            PrintHorizontalBorder(false, true);
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    Console.Write((y % SingleBlockWidth == 0) ? "|" : " ");
                    int block = TempGrid[x][y].Length;
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
        #endregion

        #region 後始末
        private void CleanTempGrid(int x, int y)
        {
            TempGrid[x][y] = new int[0];
            //TempGrid[x, y].Possibles.Clear();
        }
        private void ClearRelativeTemp(int x, int y, int value, bool horizontal = true, bool vertical = true, bool inner = true)
        {
            //cleanup
            if (!horizontal) goto VERT;
            foreach (Point p in GetHorizontalEmpty(x, y, 0, true))
            {
                RemoveItemFromTemp(value, ref  TempGrid[p.x][p.y]);
            }
        VERT:
            if (!vertical) goto INNER;
            foreach (Point p in GetVerticalEmpty(x, y, 0, false))
            {
                RemoveItemFromTemp(value, ref TempGrid[p.x][p.y]);
            }
        INNER:
            if (!inner) goto SELF;
            foreach (Point p in GetInnerEmpty(x, y, false))
            {
                RemoveItemFromTemp(value, ref TempGrid[p.x][p.y]);
            }
        SELF:
            CleanTempGrid(x, y);
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
        private int[] InBlockPossible(int x, int y, bool includeself = true, bool forceupdate = false)
        {
            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);
            List<int> poss = new List<int>();


            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if ((!includeself) & ((xa == x) & (ya == y))) continue; //skipself
                    if (Grid[xa][ya] != 0) continue; //skip filled in
                    //poss.AddRange(TempGrid[xa, ya]);
                    poss.AddRange((forceupdate) ? GetPossible(xa, ya) : TempGrid[xa][ya]);
                }
            }
            //int[] ret = poss.ToArray();
            // Distinct(ref ret);
            return  poss.Distinct().ToArray();
        }

        private void AdvancedInBlockCheck(int x, int y, bool clearafter = true)
        {
            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (Grid[xa][ya] != 0) { if (clearafter) { CleanTempGrid(xa, ya); } continue; }//skip if filled
                    int[] inboxexceptselfpossible = InBlockPossible(xa, ya, false);
                    //var selfpossible = TempGrid[xa, ya]; //DON'T USE!!! CAUSES  ERROR
                    TempGrid[xa][ya] = GetPossible(xa, ya);
                    //TempGrid[xa, ya].Possibles = selfpossible.ToList();
                    var diff = TempGrid[xa][ya].Except(inboxexceptselfpossible).ToList();
                    if (diff.Count == 1)
                    {
                        Grid[xa][ya] = diff[0];
                        UnfilledCount--;
                        CleanTempGrid(xa, ya);
                    }
                }
            }
        }

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
                        Grid[empts[0].x][empts[0].y] = except[0];
                        ClearRelativeTemp(empts[0].x, empts[0].y, except[0]);
                        UnfilledCount--;

                    }
                    else
                    {
                        //goto SKIP;

                        #region Else
                        int[] except = FullInts.Except(GetFilledInner(x, y)).ToArray();
                        Queue<Point> remainpts = new Queue<Point>(GetInnerEmpty(x, y, false));

                        byte lpct = 0;

                        while (remainpts.Count > 0)
                        {
                            Point p = remainpts.Dequeue();
                            IEnumerable<int> inters = TempGrid[p.x][p.y].Intersect(except);

                            foreach (Point stack in remainpts)
                            {
                                //filters out each possibility and leaves with the only possible ones
                                inters = inters.Except(TempGrid[stack.x][stack.y]);
                            }

                            int[] intersect = inters.ToArray();


                            bool hasonly = intersect.Length == 1;

                            if (hasonly)
                            {
                                Grid[p.x][p.y] = intersect[0];
                                UnfilledCount--;

                                ClearRelativeTemp(p.x, p.y, intersect[0]);

                                int index = 0;
                                for (int i = 0; i < except.Length; i++)
                                {
                                    if (except[i] == intersect[0])
                                    {
                                        index = i;
                                        break;
                                    }
                                }



                                //remove value from yexcept
                                Queue<int> removeold = new Queue<int>(except);
                                while (true)
                                {
                                    int tmp = removeold.Dequeue();
                                    if (tmp == intersect[0]) break;
                                    else removeold.Enqueue(tmp);
                                }
                                except = removeold.ToArray();



                            }
                            else
                            {
                                remainpts.Enqueue(p); //throw it back the queue

                            }
                            if (lpct > 2)
                            {
                                lpct = 0;
                                break;
                            }
                            lpct++;

                        }



                        #endregion

                        continue;//skip the rest for now
                    SKIP:
                        #region Old

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
                                    Grid[item.x][item.y] = poss[0];
                                    RemoveItemFromTemp(poss[0], ref TempGrid[item.x][item.y]);
                                    count--;
                                    UnfilledCount--;
                                }
                            }
                            if (prev == count) break;
                            prev = count;
                        } while (count != 0);
                        //*/
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
                        #endregion
                    }
                }
            }
        }



        #endregion

        #region Leftover possible value check

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
                    if (Grid[xa][ya] == 0) track++;
                }
            }
            return track;
        }
        /// <summary>
        /// Gets IMPOSSIBLE values
        /// Gets all the existing values in the specified xy range
        /// </summary>
        /// <param name="x">x axis</param>
        /// <param name="y">y axis</param>
        /// <returns>IMPOSSIBLE values</returns>
        private int[] GetFilledInner(int x, int y, bool notincludeself = false)
        {
            int[] track = new int[FullGridWidth - CountInnerBlockEmpty(x, y)]; //Fullgridwidth -> SingleBlock * SingleBlock - Empty = filled

            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);
            int c = 0;

            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if ((notincludeself) && (xa == x) && (ya == y)) continue; //skip self if notincludeself
                    if (Grid[xa][ya] != 0) track[c++] = (Grid[xa][ya]);
                }
            }
            return track;
        }

        private int[] GetFilledInRow(int pos, Axis axis, int from = 0)
        {
            Queue<int> track = new Queue<int>();
            switch (axis)
            {
                case Axis.HORIZONTAL:
                    for (int y = from; y < FullGridWidth; y++)
                    {
                        if (Grid[pos][y] != 0) track.Enqueue(Grid[pos][y]);
                    }
                    //track.Sort(); //maybe not required;
                    return track.ToArray();
                case Axis.VERTICAL:
                    for (int x = from; x < FullGridWidth; x++)
                    {
                        if (Grid[x][pos] != 0) track.Enqueue(Grid[x][pos]);
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
                if (item == num) continue; //if the value is num, skip it
                queue.Enqueue(item);
            }
            arr = queue.ToArray(); //replace old arr with new arr
        }

        private int[] BlockVerticalLeftIntersect(int x, int y)
        {
            return FullInts.Except(GetFilledInner(x, y)).Intersect(FullInts.Except(GetFilledInRow(y,Axis.VERTICAL))).ToArray();
        }
        private int[] BlockHorizontalLeftIntersect(int x, int y)
        {
            return FullInts.Except(GetFilledInner(x, y)).Intersect(FullInts.Except(GetFilledInRow(x,Axis.HORIZONTAL))).ToArray();
        }

        /// <summary>
        /// Checks the horizontal + vertical leftover
        /// </summary>
        private void CheckHVLeftOver()
        {
            int yaxis = 0;

            for (int x = 0; x < FullGridWidth; x++) //= SingleBlockWidth)
            {
                for (int y = yaxis; y < FullGridWidth; y++)//= SingleBlockWidth)
                {

                    #region Xrow

                    //int xrowempty = CountRowEmpty(x, Axis.HORIZONTAL); // don't need to waste loops here. (fgw - xfillevals len) would do
                    Point[] xempts = GetHVEmpty(x, Axis.HORIZONTAL, Point.Null);
                    xempts = GetHorizontalEmpty(x, y, 0, true, true);
                    if (xempts.Length == 0) goto YROW; //priority jump

                    int[] xfilledvals = GetFilledInRow(x, Axis.HORIZONTAL);
                    int[] xexcept = FullInts.Except(xfilledvals).ToArray();


                    if ((FullGridWidth - xfilledvals.Length) == 1)
                    {
                        Grid[xempts[0].x][xempts[0].y] = xexcept[0];
                        //RemoveItemFromTemp(xexcept[0], TempGrid[xempts[0].x, xempts[0].y]).ToArray();

                        //USE THIS! DONT USE CLEARRELATIVE BECAUSE CLEARRELATIVE LOOPS AGAIN TO FIND LOCATIONS!
                        //LOCATION DATA IS ALREADY IN YEMPTS
                        for (int i = 0; i < xempts.Length; i++)
                        {
                            RemoveItemFromTemp(xexcept[0], ref  TempGrid[xempts[i].x][xempts[i].y]);
                        }
                        CleanTempGrid(xempts[0].x, xempts[0].y);
                        UnfilledCount--;
                        continue;
                        //goto CONTINUE;
                        //if filled in, dont go check next
                    }
                    else
                    {
                        #region VER 1
                        /*
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
                                    RemoveItemFromTemp(num, ref TempGrid[i, y]);
                                }
                            }
                        }
                        PossibleLocs.Clear();
                        //*/
                        #endregion

                        #region Else

                        Queue<Point> remainpts = new Queue<Point>(xempts);

                        byte lpct = 0;
                        bool foundany = false;
                        Point first = remainpts.Peek(); //OK because yempts is never null
                        while (remainpts.Count > 0)
                        {
                            Point p = remainpts.Dequeue();
                            IEnumerable<int> inters = TempGrid[p.x][p.y].Intersect(xexcept);

                            //need advanced search here. refer to logic #8 problem 

                            foreach (Point stack in remainpts)
                            {
                                inters = inters.Except(TempGrid[stack.x][stack.y]);
                            }

                            int[] intersect = inters.ToArray();



                            if (intersect.Length == 1)
                            {
                                Grid[p.x][p.y] = intersect[0];
                                UnfilledCount--;

                                ClearRelativeTemp(p.x, p.y, intersect[0], true, true, false);

                                //remove value from yexcept
                                //remove value from yexcept.
                                //IF NO VALUE IS FOUND, IT WILL GO ON INFINITE LOOP.
                                Queue<int> removeold = new Queue<int>(xexcept);
                                ushort i = 0;
                                while (i < removeold.Count)
                                {
                                    int tmp = removeold.Dequeue();
                                    if (tmp == intersect[0]) break;
                                    else removeold.Enqueue(tmp);
                                    i++;
                                }
                                xexcept = removeold.ToArray();
                                foundany = true;

                            }
                            else
                            {
                                remainpts.Enqueue(p); //throw it back the queue
                                if (remainpts.Peek() == first && !foundany) break; //break if nothing found in first loop                                
                            }
                            if (lpct > (xempts.Length * 2))
                            {
                                break;
                            }
                            lpct++;
                            foundany = false;

                        }

                        /*
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

                        //*/
                        #endregion

                    }
                    #endregion
                YROW:
                    #region Yrow
                    //int xrowempty = CountRowEmpty(x, Axis.HORIZONTAL); // don't need to waste loops here. (fgw - xfillevals len) would do
                    Point[] yempts = GetVerticalEmpty(x, y, 0, true, true);
                    if (yempts.Length == 0) continue;


                    int[] yfilledvals = GetFilledInRow(y, Axis.VERTICAL);
                    int[] yexcept = FullInts.Except(yfilledvals).ToArray();


                    if ((FullGridWidth - yfilledvals.Length) == 1)
                    {
                        Grid[yempts[0].x][yempts[0].y] = yexcept[0];
                        //RemoveItemFromTemp(yexcept[0], TempGrid[yempts[0].x, yempts[0].y]).ToArray();

                        //USE THIS! DONT USE CLEARRELATIVE BECAUSE CLEARRELATIVE LOOPS AGAIN TO FIND LOCATIONS!
                        //LOCATION DATA IS ALREADY IN YEMPTS
                        for (int i = 0; i < yempts.Length; i++)
                        {
                            RemoveItemFromTemp(yexcept[0], ref  TempGrid[yempts[i].x][yempts[i].y]);
                        }
                        CleanTempGrid(yempts[0].x, yempts[0].y);
                        UnfilledCount--;
                        //goto CONTINUE;
                        //if filled in, dont go check next
                    }
                    else
                    {
                        #region VER 1
                        /*
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
                        //*/
                        #endregion

                        #region Else
                        Queue<Point> remainpts = new Queue<Point>(yempts);


                        byte lpct = 0;
                        bool foundany = false;
                        Point first = remainpts.Peek(); //OK because yempts is never null
                        while (remainpts.Count > 0)
                        {
                            Point p = remainpts.Dequeue();
                            IEnumerable<int> inters = TempGrid[p.x][p.y].Intersect(yexcept);

                            foreach (Point stack in remainpts)
                            {
                                //filters out each possibility and leaves with the only possible ones
                                inters = inters.Except(TempGrid[stack.x][stack.y]);
                            }
                            int[] intersect = inters.ToArray();

                            if (intersect.Length == 1)
                            {
                                Grid[p.x][p.y] = intersect[0];
                                UnfilledCount--;

                                ClearRelativeTemp(p.x, p.y, intersect[0], true, true, false);

                                //remove value from yexcept.
                                //IF NO VALUE IS FOUND, IT WILL GO ON INFINITE LOOP.
                                Queue<int> removeold = new Queue<int>(yexcept);
                                ushort i = 0;
                                while (i++ < (removeold.Count + 1)) //count + 1 because ushort 1, and i++ in while.
                                {
                                    int tmp = removeold.Dequeue(); //take first value
                                    if (tmp == intersect[0]) break;
                                    else removeold.Enqueue(tmp); //send to back, shift the next
                                }
                                yexcept = removeold.ToArray();
                                foundany = true;
                            }
                            else
                            {
                                remainpts.Enqueue(p); //throw it back the queue
                                if (remainpts.Peek() == first && !foundany) break; //break if nothing found in first loop
                            }
                            //loop TWICE at most
                            if (lpct > (yempts.Length * 2))
                            {
                                break;
                            }
                            lpct++;
                            foundany = false;
                        }

                        #endregion
                    }
                    #endregion


                }
                //yaxis++; //skips a lot of cells
            }
        }
        #endregion

        #region Impossibles

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
                    if (Grid[x][y] != 0)
                    {
                        TempGrid[x][y] = new int[0];
                        continue;
                    }
                    TempGrid[x][y] = GetPossible(x, y);

                    //TempGrid[x, y].Possibles.AddRange(GetPossible(x, y)); //add possible values

                }
            }
        }
        
        /// <summary>
        /// Gets all empty blocks in the direction.
        /// Use HORIZONTAL with x value and VERTICAL with y value
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="axis"></param>
        /// <param name="from"></param>
        /// <returns></returns>
        private Point[] GetHVEmpty(int pos, Axis axis, Point notinclude, int from = 0)
        {
            Point[] track = new Point[FullGridWidth]; //maximum available
            uint c = 0; //end index counter
            switch (axis)
            {
                case Axis.HORIZONTAL:
                    for (int y = from; y < FullGridWidth; y++)
                    {
                        if (notinclude != Point.Null)
                        {
                            if (notinclude == new Point(pos, y)) continue;
                        }
                        if (Grid[pos][y] == 0) track[c++] = new Point(pos, y);
                    }
                    break;
                    //track.Sort(); //maybe not required;
                case Axis.VERTICAL:
                    for (int x = from; x < FullGridWidth; x++)
                    {
                        if (notinclude != Point.Null)
                        {
                            if (notinclude == new Point(x, pos)) continue;
                        }
                        if (Grid[x][pos] == 0) track[c++] = new Point(x, pos);
                    }
                    break;
                    //track.Sort(); //maybe not required;
            }
            return TrimAt(track, c);
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
                if (Grid[vertical][a] == 0)
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
                if (Grid[b][horizontal] == 0)
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
            //List<int> li = new List<int>(FullInts);
            //li.RemoveAll(e => GetFilledInner(x, y, notincludeself).Concat(GetRowImpossible(x, y)).Contains(e));
            //return li.ToArray();
            var fe =  FullInts.Except(GetFilledInner(x, y, notincludeself).Concat(GetRowImpossible(x, y))).ToArray(); //returns possible values
            return fe;
        }
        private Point[] GetInnerEmpty(int x, int y, bool notincludeself = true)
        {
            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);
            Point[] pt = new Point[FullGridWidth];
            //Queue<Point> pt = new Queue<Point>();
            int ptr = 0;
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if ((notincludeself) && (xa == x) && (ya == y) && (Grid[xa][ya] == 0)) continue; //skip self if notincludeself
                    if (Grid[xa][ya] == 0) pt[ptr++] = new Point(xa, ya);
                        //pt.Enqueue(new Point() { x = xa, y = ya });
                }
            }
            TrimEndPt(ref pt);
            //return pt.ToArray();
            return pt;//pt.ToArray();
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
                if (Grid[x][a] != 0)
                {
                    values[pointer] = Grid[x][a];
                    pointer++;
                }
                if (Grid[a][y] != 0)
                {
                    values[pointer] = Grid[a][y];
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
            
            //int[] ret = values.Distinct().Select(int.Parse).ToArray(); //remove duplicates and return IMPOSSIBLE values
            return values;
        }
        #endregion

        #region Initialize
        /// <summary>
        /// Initializes Grid, TempGrid, FullInts, Separate lines size, and Tempgrid TempBlock item
        /// </summary>
        private void Initialize()
        {
            Grid = new int[FullGridWidth][]; //MainGrid
            BackupGrid = new int[FullGridWidth][]; //MainGrid
            TempGrid = new int[FullGridWidth][][];
            //SearchGrid = new bool[FullGridWidth, FullGridWidth]; //default all true, so use false as notifier

            //Fullints initialize

            FullInts = new int[FullGridWidth];
            //SearchGrid = new bool[FullGridWidth][];
            for (int i = 0; i < FullGridWidth; i++)
            {
                FullInts[i] = i + 1;
                Grid[i] = new int[FullGridWidth];
                BackupGrid[i] = new int[FullGridWidth];
                TempGrid[i] = new int[FullGridWidth][];
                //SearchGrid[i] = new bool[FullGridWidth];
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
            validator = new Validator(SingleBlockWidth, FullInts);

            dx = 1;
            dy = 4;
            
        }
        #endregion

        #region Experimental
        /*
         * 
         * Result:
         * int[] mrg = Merge(a, b);
            Array.Sort(mrg);
            Distinct(ref mrg);
         * has the fastest performance
         * 
         */
        private static void Swap(ref int x, ref int y)
        {
            var tmp = x;
            x = y;
            y = tmp;
        }

        private static void xorSwap(ref int x,ref  int y)
        {
            if (x != y)
            {
                x ^= y;
                y ^= x;
                x ^= y;
                //*/
            }
        }

        public static void TestExperimental()
        {
            //swap test;

            int e = 5, j = 6;
            Swap(ref e,ref j);
            System.Diagnostics.Debug.WriteLine(e + " " + j);



            int[] a = { 5, 2, 4,4, 1, 6 };

            int[] b = { 2, 5, 4, 7 };

            

            var stp = new Stopwatch();
            stp.Start();
            //int[] mrg = CombineSort(a, b);
            int[] final = BlockSwapSort(ref a, ref b);
            Distinct(ref final);
            stp.Stop();
            System.Diagnostics.Debug.WriteLine(stp.ElapsedMilliseconds);
            System.Diagnostics.Debug.WriteLine(stp.ElapsedTicks);
            stp.Reset();
            stp.Start();
            int[] res = a.Intersect(b).ToArray();
            stp.Stop();
            System.Diagnostics.Debug.WriteLine(stp.ElapsedMilliseconds);
            System.Diagnostics.Debug.WriteLine(stp.ElapsedTicks);
            stp.Reset();
            stp.Start();
            int[] f2 = a.Concat(b).Distinct().OrderBy(x=>x).ToArray();
            stp.Stop();
            System.Diagnostics.Debug.WriteLine(stp.ElapsedMilliseconds);
            System.Diagnostics.Debug.WriteLine(stp.ElapsedTicks);

            stp.Reset();
            stp.Start();
            int[] c = BitonicSort(true, Merge(a,b) );
            
            
            Distinct(ref c);
            stp.Stop();
            System.Diagnostics.Debug.WriteLine(stp.ElapsedMilliseconds);
            System.Diagnostics.Debug.WriteLine(stp.ElapsedTicks);

            stp.Reset();
            stp.Start();
            HashSet<int> hs = new HashSet<int>(BitonicSort(true,Merge(a, b)));
            stp.Stop();
            System.Diagnostics.Debug.WriteLine(stp.ElapsedMilliseconds);
            System.Diagnostics.Debug.WriteLine(stp.ElapsedTicks);

            stp.Reset();
            stp.Start();
            int[] mrg = Merge(a, b);
            Array.Sort(mrg);
            Distinct(ref mrg);
            //mrg = mrg.Distinct().ToArray();
            
            stp.Stop();
            System.Diagnostics.Debug.WriteLine(stp.ElapsedMilliseconds);
            System.Diagnostics.Debug.WriteLine(stp.ElapsedTicks);
            

            System.Environment.Exit(0);
        }

        static int[] BlockSwapSort(ref int[] a, ref int[] b)
        {
            int[] c = Merge(a, b);
            TrimArray(ref c);
            int[] first = Split(c, c.Length / 2, 0);
            int[] second = Split(c, c.Length - (c.Length / 2), c.Length / 2);
            //TrimArray(ref first);
            //TrimArray(ref second);
            int size = Math.Max(first.Length, second.Length);
            FormatArr(ref first, ref second, ref size); //equalize size

            bool flag, res = false;
            HEAD:
            do
            {
                flag = false;


                for (int i = 0; i < size; i++)
                {
                    //Console.WriteLine("{0} - {1}", first[i],second[i]);
                    /*
                    if (first[i] == second[i])
                    {
                        second[i] = 0;
                        flag = true;
                        continue;
                    }//*/
                    if (((first[i]==0|second[i]==0)&(first[i]==0^second[i]==0)))
                    {
                        first[i] = (second[i] == 0) ? first[i] : second[i];
                        second[i] = 0;
                        flag = true;
                        continue;
                    }
                    if (first[i] > second[i])
                    {
                        Swap(ref first[i], ref second[i]);
                        flag = true;
                    }
                }
                for (int i = 0; i < size - 1; i++)
                {
                    if (first[i + 1] < second[i])
                    {
                        Swap(ref second[i], ref first[i + 1]);
                    }
                    if (first[i] > second[i])
                    {
                        Swap(ref first[i], ref second[i]);
                        flag = true;
                    }
                }
                int[] final = Merge(first, second, true);
                if (res = finalcheck(ref final)) return final;

                c = Merge(first, second);
                TrimArray(ref c);
                //printarr(ref c);
                first = Split(c, c.Length / 2, 0);
                second = Split(c, c.Length - (c.Length / 2), c.Length / 2);
                //TrimArray(ref first);
                //TrimArray(ref second);
                size = Math.Max(first.Length, second.Length);
                FormatArr(ref first, ref second, ref size); //equalize size
                //printarr(ref c);

            } while (flag);

            if (res = finalcheck(ref c)) return c;
            if (!(res | flag))
            {
                flag = false;

                int ptr = 0;
                int[] final = new int[size*2];
                for (int i = 0; i < size; i++)
                {
                    final[ptr++] = first[i];
                    final[ptr++] = second[i];
                }
                if (res = finalcheck(ref final)) return final;
                
                //b[i] with a[i+1] comp until max -1;
                //go back to original

                for (int i = 0; i < size - 1; i++)
                {
                    if (second[i] > first[i+1])
                    {
                        Swap(ref second[i], ref first[i + 1]);
                        flag = true;
                    }
                }
                if (flag) goto HEAD;
            }
            c = Merge(first, second);
            //TrimArray(ref c);
            printarr(ref c);
            return c;
        }

        static bool finalcheck(ref int[] a)
        {
            for (int i = 0; i < a.Length - 1; i++)
            {
                if (a[i] > a[i + 1]) return false;
            }
            return true;
        }

        static void printarr(ref int[] a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                Console.WriteLine(a[i]);

            }
            Console.WriteLine("-----");

        }
        
        static void TrimArray(ref int[] a)
        {
            int tcount = 0;
            for (int i = 0; i < a.Length; i++)
                if (a[i] == 0) tcount++;

            int[] ns = new int[a.Length - tcount];
            tcount = 0;
            for (int i = 0; i < a.Length; i++)
            {
               if (a[i] != 0)
               {
                   ns[tcount++] = a[i];
               }
            }
            a = ns;
        }

        public static void TrimEndPt(ref Point[] a)
        {
            int loc = 0;
            for (int i = a.Length - 1; i >= 0; i--)
            {
                if (a[i]!= Point.Null)
                {
                    loc = i + 1;
                    break;
                }
            }
            Point[] ret = new Point[loc];
            for (int i = 0; i < loc; i++)
            {
                ret[i] = a[i];
            }
            a = ret;
            
        }

        static void FormatArr(ref int[] a, ref int[] b, ref int size)
        {
            if (a.Length == b.Length) return;
            int[] ns = new int[size];
            if (size == a.Length)
            {
                //a is longer
                for (int i = 0; i < size; i++)
                {
                    if (i >= b.Length) ns[i] = 0;
                    else ns[i] = b[i];
                }
                b = ns;
            }
            else
            {
                //b is longer
                for (int i = 0; i < size; i++)
                {
                    if (i >= a.Length) ns[i] = 0;
                    else ns[i] = a[i];
                }
                a = ns;
            }

        }

        
        private static int[] CombineSort(int[] a, int[] b)
        {
            //objectives:
            //merge
            //sort while loop. doesnt need to be perfectly sorted
            //remove duplicates

            int[] mrg = new int[a.Length + b.Length];
            int ptr = 0;
            int t1, t2;
            for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
            {
                t1 = (i >= a.Length) ? -1 : a[i];
                t2 = (i >= b.Length) ? -1 : b[i];
                int max;
                sbyte res = Cmp(t1, t2, out max);

                //put nums into mrg with sort
                switch (res)
                {
                    case 1:
                        mrg[ptr++] = t2;
                        goto case 0;
                    case 2:
                        mrg[ptr++] = t1;
                        goto case 0;
                    case 0:
                        mrg[ptr++] = max;
                        break;
                }
            }
            return mrg;


        }

        /// <summary>
        /// Compares two values
        /// </summary>
        /// <param name="res">Max value</param>
        /// <returns>Return -1 Fail 0 Equal 1 Max</returns>
        private static sbyte Cmp(int a, int b, out int res)
        {
            if (a == b)
            {
                if ((a <= 0))
                {
                    res = -1;
                    return -1;
                }
                res = a;
                return 0;
            }
            if (a > b)
            {
                res = a;
                return (b <= 0) ? (sbyte)0 : (sbyte)1;
            }
            if (b > a)
            {
                res = b;
                return (a <= 0) ? (sbyte)0 : (sbyte)2;
            }
            res = -1;
            return -1;
        }

        #region Bitonic Sort
        
        private static int[] BitonicSort(bool top, int[] arr)
        {
            if (arr.Length <= 1) return arr;
            int len = arr.Length / 2;
            var first = BitonicSort(true, Split(arr, len, 0));
            var second = BitonicSort(false, Split(arr, arr.Length - len, len));
            int[] merged = Merge(first, second);
            return BitonicMerge(top, merged);
        }

        private static int[] BitonicMerge(bool top, int[] arr)
        {
            if (arr.Length == 1) return arr;
            BitonicCompare(top, arr);
            int len = arr.Length / 2;
            var first = BitonicMerge(top, Split(arr, len, 0));
            var second = BitonicMerge(top, Split(arr, arr.Length - len, len));
            return Merge(first, second);

        }

        private static void BitonicCompare(bool top, int[] arr)
        {
            var dist = arr.Length / 2;

            for (int i = 0; i < dist; i++)
            {
                if ((arr[i] > arr[i + dist]) == top)
                {
                    Swap(ref arr[i],ref  arr[i + dist]);
                    continue;
                    var tmp = arr[i];
                    arr[i] = arr[i + dist];
                    arr[i + dist] = tmp;
                }
            }

        }

        private static int[] Split(int[] arr, int count, int start)
        {
            int[] ret = new int[count];
            int ptr = start;
            for (int i = 0; i < count; i++)
            {
                ret[i] = arr[i + start];
            }
            return ret; 
        }

        private static int[] Merge(int[] a, int[] b, bool serial = false)
        {
            int[] ret;

            switch (serial)
            {
                case true:
                    int size = Math.Max(a.Length, b.Length);
                    ret = new int[size * 2];
                    int ptr = 0;
                    int[] final = new int[size * 2];
                    for (int i = 0; i < size; i++)
                    {
                        final[ptr++] = a[i];
                        final[ptr++] = b[i];
                    }
                    return ret;
                case false:
                    ret = new int[a.Length + b.Length];
                    for (int i = 0; i < (a.Length); i++)
                    {
                        ret[i] = a[i];
                    }
                    for (int i = 0; i < (b.Length); i++)
                    {
                        ret[a.Length + i] = b[i];
                    }
                    return ret;
            }
            return (new int[0]);
        }
        #endregion

        public static void Distinct(ref int[] a)
        {
            int[] ns = new int[a.Length];
            int end = a.Length, ptr = 0;

            for (int i = 0; i < end; i++)
            {
                for (int c = 0; c < ns.Length; c++)
                {
                    if (ns[c] == a[i]) goto NEXT;
                }
                ns[ptr++] = a[i];
            NEXT:
                continue;
            }
            a = ns;
        }
        #endregion

        public static Point[] TrimAt(Point[] arr, uint index)
        {
            Point[] fin = new Point[index];
            for (int i = 0; i < index; i++)
            {
                fin[i] = arr[i];
            }
            return fin;
        }

        #region Labs
        public bool[][] SearchGrid;
        public void reinitSearch()
        {
            for (int i = 0; i < FullGridWidth; i++)
            {
                Array.Clear(SearchGrid[i], 0, FullGridWidth);
            }
        }

        public Point[] GetInfluenced(Point[] ps, int val)
        {
            if (ps.Length == 1) return ps;
            HashSet<Point> collection = new HashSet<Point>();
            foreach (Point item in ps)
            {
                collection.UnionWith(RowHasSamePossible(item.x, item.y, val));
            }
            return GetInfluenced(collection.ToArray(), val);
        }

        public Point[] RowHasSamePossible(int x, int y,int val)
        {
            HashSet<Point> ret = new HashSet<Point>();
            for (int i = 0; i < FullGridWidth; i++)
            {
                if (TempGrid[x][i].Contains(val)) ret.Add(new Point(x, i));
                if (TempGrid[i][y].Contains(val)) ret.Add(new Point(i, y));
            }
            ret.Remove(new Point(x, y));//remove self
            return ret.ToArray();
        }
        #endregion
    }

    public class HookedPointReachedException : Exception
    {
        public HookedPointReachedException()
        {
        }

        public HookedPointReachedException(string message)
            : base(message)
        {
        }

        public HookedPointReachedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}