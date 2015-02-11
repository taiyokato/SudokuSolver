using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace SudokuSolver
{
    public class Solver
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

        public readonly int[] EmptyTemp = new int[0];
        #endregion

        /// <summary>
        /// Main method
        /// 入り込みポイント
        /// </summary>

        bool debug = false;
        public void Solve(bool skipprint)
        {
            //dx = 6;
            //dy = 2;
            //AdvancedHook(9);
            UnfilledCount = 0;
            GetFilledCount();
            Stopwatch stop = new Stopwatch();
            if (!Validator.GridReadyValidate(ref Grid, FullGridWidth)) { Console.WriteLine("Invalid at row: {0}", Validator.BreakedAt); goto FINISH; }


            stop.Start();
            Basic(); //preparation
#if (debug)
            if ((Validator.Validate2(ref Grid, UnfilledCount, out success)) && (!success)) { Console.WriteLine("Invalid at row: {0}", Validator.BreakedAt); goto FINISH; }
#endif
            if (UnfilledCount <= 0) goto FINISH; //if finished at this point, jump to finish
            if (!skipprint)
            {
                Console.WriteLine("Basic try:");
                PrintAll();
            }
            //Console.WriteLine(DateTime.Now.Subtract(now));



            Advanced();
#if (debug)
            if ((Validator.Validate2(ref Grid, UnfilledCount, out success)) && (!success)) { Console.WriteLine("Invalid at row: {0}", Validator.BreakedAt); goto FINISH; }
#endif
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

            bool validated = Validator.FinalValidate(ref Grid, FullGridWidth);
            //bool validated = Validator.Validate2(ref Grid, UnfilledCount, out success);
            Console.WriteLine("Grid Check: {0}", (validated) ? "Valid" : "Invalid");
            //Console.WriteLine("Grid Check: {0}", (validated && success) ? "Valid" : "Invalid");
            if (Validator.BreakedAt != -1) Console.WriteLine("Invalid at row: {0}", Validator.BreakedAt);
            
            Console.WriteLine("[EOF]");
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
            Console.Read();

            Solve(skipprint);
        }

        #region Table Copy
        /// <summary>
        /// Copies grid from source->target
        /// </summary>
        public void CopyAll(int[][] source,ref int[][] target)
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
                            CleanTempGrid(x, y);
                            ClearRelativeTemp(x, y, val);
                        }
                    }
                    //CheckHVLeftOver();
                }
                //EnvironmentScan();
                
                if (UnfilledCount <= 0) break; //early break before copying

                if (GridSame(ref BackupGrid)) break; //if advancedfill & checkleftover2 cant handle it anymore
                CopyAll(Grid, ref BackupGrid);
            } while (UnfilledCount > 0);


            if (UnfilledCount == 0) return;
            CopyAll(Grid, ref BackupGrid);
            do
            {
                CheckHVLeftOver();
                if (UnfilledCount <= 0) break; //early break before copying

                if (GridSame(ref BackupGrid)) break; //if advancedfill & checkleftover2 cant handle it anymore
                CopyAll(Grid, ref BackupGrid);
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
                        Point[] p = GetInnerEmpty(x, y);
                        for (int i = 0; i < p.Length; i++)
                        {
                            IEnumerable<int> hvleft = FullInts.Except(GetFilledInRow(p[i].y, Axis.VERTICAL).Concat(GetFilledInRow(p[i].x, Axis.HORIZONTAL)));
                            IEnumerable<int> inleft = FullInts.Except(GetFilledInner(x, y));
                            int[] intersect = inleft.Intersect(hvleft).ToArray();

                            //int[] inter = FullInts.Except(GetFilledInner(x, y)).Intersect(FullInts.Except().ToArray();
                            //int[] intersect = TempGrid[p.x][p.y].Intersect(inter).ToArray();



                            if (intersect.Length == 1)
                            {
                                Grid[p[i].x][p[i].y] = intersect[0];
                                ClearRelativeTemp(p[i].x, p[i].y, intersect[0]);
                                UnfilledCount--;
                                continue;
                            }
                        }
                    }
                }

                if (GridSame(ref BackupGrid)) break; //if advancedfill & checkleftover2 cant handle it anymore
                CopyAll(Grid, ref BackupGrid);
                //BackupGrid = Grid.Clone() as int[,]; //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed;
            } while (UnfilledCount > 0);
            
        }


        bool TreeDiagramSolve()
        {
            //create TreeDiagram instance for easier code management
            TreeDiagram td = new TreeDiagram(ref Grid, SingleBlockWidth, FullInts, ref TempGrid);
            td.UnfilledCount = UnfilledCount;
            td.Execute5(td.EntryPoint);

            //td.Execute2(ref UnfilledCount); //probably faster and lighter than Execute()
            //Execute3(ref UnfilledCount); //STILL TESTING THE CODE
            //Grid = td.Grid; // reference did the job
            //TempGrid = td.TempGrid; //reference did the job
            return td.FinishFlag;
        }


        /// <summary>
        /// Checks if Grid and BackupGrid are exactly same
        /// </summary>
        private bool GridSame(ref int[][] backup)
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
                System.Diagnostics.Debug.WriteLine(hpre);
                
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
                System.Diagnostics.Debug.WriteLine(hpre);
            }
        }
        #endregion
        #region Printout
        private void PrintHorizontalBorder(bool withnewline = false, bool headerfooter = false)
        {
            string outstr = (headerfooter) ? "-" : "|";

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
        private void PrintTempHasValue(int val)
        {
            PrintHorizontalBorder(false, true);
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    Console.Write((y % SingleBlockWidth == 0) ? "|" : " ");
                    Console.Write("{0}", TempGrid[x][y].Contains(val) ? "o" : "x");
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
            TempGrid[x][y] = EmptyTemp;      
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
#if (false)
                    PrintTempHasValue(6);
                    PrintAll();
                    AdvancedHV(x, y);
                    PrintAll();
#endif
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
                    TempGrid[xa][ya] = GetPossible(xa, ya);
                    var diff = TempGrid[xa][ya].Except(inboxexceptselfpossible).ToList();
                    if (diff.Count == 1)
                    {
                        Grid[xa][ya] = diff[0];
                        UnfilledCount--;
                        CleanTempGrid(xa, ya);
                        ClearRelativeTemp(xa, ya, diff[0]);
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
                    }
                }
            }
        }

        /// <summary>
        /// Environment check for HV Block (SingleBlockWidth size) rows
        /// </summary>
        private void AdvancedHV(int x, int y)
        {
            //PrintTempHasValue(5);
            //refer to snapshot

            /*
             * BIG PROBLEM NOW!
             * NUMBERS ARE RANDOMLY FILLED IN!
             */ 


            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);

            List<List<Point>> conflict = new List<List<Point>>();
            List<int> except = FullInts.Except(GetFilledInner(x, y)).ToList();
            foreach (int item in except)
            {
                
                Point[] block = GetCriticalPointsFromValue(x, y, item);
                if (block.Length == 0) return;
                List<Point> external = new List<Point>();

                for (int i = 0; i < FullGridWidth; i += SingleBlockWidth)
                {
                    external.AddRange(GetCriticalPointsFromValue(x, i, item));
                    external.AddRange(GetCriticalPointsFromValue(i, y, item));
                }

                //Count all horizontal points 
                //100 / Count -> probability for each
                //seclude points that have higher probability
                //sum the vertical points
                //seclude the points that have least probability
                //intersect the two secluded and they are the answers
                HashSet<int>
                    h_count = new HashSet<int>(),
                    v_count = new HashSet<int>();
                for (int i = 0; i < external.Count; i++)
                {
                    h_count.Add(external[i].x);
                    v_count.Add(external[i].y);
                }

                double[][] matrix = new double[h_count.Count][];
                int[] h_arr = h_count.ToArray();
                int[] v_arr = v_count.ToArray();

                for (int i = 0; i < h_count.Count; i++)
                {
                    matrix[i] = new double[v_count.Count];
                    int incount = 0;
                    for (int ii = 0; ii < v_count.Count; ii++)
                    {
                        if (external.Contains(new Point(h_arr[i], v_arr[ii])))
                        {
                            incount++;
                        }
                    }
                    for (int ii = 0; ii < v_count.Count; ii++)
                    {
                        if (external.Contains(new Point(h_arr[i], v_arr[ii])))
                        {
                            matrix[i][ii] = 100 / incount;
                        }

                    }
                }
                double[] sum = new double[v_arr.Length];
                Point[] maxp = new Point[v_arr.Length];
                double max;
                int max_index;
                bool flip = false;
                double prev;
                for (int i = 0; i < v_arr.Length; i++)
                {
                    max = 0.0;
                    max_index = 0;
                    flip = false;
                    prev = -1;
                    for (int ii = 0; ii < h_arr.Length; ii++)
                    {
                        if (prev == matrix[ii][i]) flip = true;
                        prev = matrix[ii][i];
                        sum[i] += matrix[ii][i];
                        if (max == matrix[ii][i]) flip = true;
                        if (max < matrix[ii][i])
                        {
                            max_index = ii;
                            max = matrix[ii][i];
                        }
                    }
                    if (!flip) maxp[i] = new Point(h_arr[max_index], v_arr[i]);
                }
                external = new List<Point>();
                double min = sum[0]; //since we don't know the max, we just set it
                for (int i = 0; i < sum.Length; i++)
                {
                    if (sum[i] < min) min = sum[i];
                }
                for (int i = 0; i < sum.Length; i++)
                {
                    if (sum[i] == min)
                    {
                        external.Add(maxp[i]);
                    }
                }
                for (int i = external.Count - 1; i >= 1; i--)
                {
                    for (int ii = i - 1; ii >= 0; ii--)
                    {
                        if (SameInner(external[i],external[ii]))
                        {
                            external[i] = external[ii] = Point.Null; //disable them
                        }
                    }
                }
                List<Point> cnf = new List<Point>();
                for (int i = 0; i < external.Count; i++)
                {
                    Point p = external[i];
                    if (p == Point.Null) continue;
                    if (Grid[p.x][p.y] != 0) continue;
                    cnf.Add(p);
                    //Grid[p.x][p.y] = item;
                    //UnfilledCount--;
                    //CleanTempGrid(p.x, p.y);
                    //ClearRelativeTemp(p.x, p.y, item);
                }
                conflict.Add(cnf);
            }
            for (int outer = 0; outer < conflict.Count; outer++)
            {
                for (int inner = 0; inner < conflict[outer].Count; inner++)
                {
                    for (int outer2 = outer; outer2 < conflict.Count; outer2++)
                    {
                        for (int inner2 = inner+1; inner2 < conflict[outer2].Count; inner2++)
                        {
                            if (SameInner(conflict[outer][inner], conflict[outer2][inner2]))
                            {
                                conflict[outer][inner] = conflict[outer2][inner2] = Point.Null;
                            }
                        }
                    }
                }
                
            }
            for (int i = 0; i < conflict.Count; i++)
            {
                for (int ii = 0; ii < conflict[i].Count; ii++)
                {
                    Point p = conflict[i][ii];
                if (p == Point.Null) continue;
                if (Grid[p.x][p.y] != 0) continue;
                if (except[i] == 1)
                {
                    System.Diagnostics.Debugger.Break();
                    PrintTempHasValue(1);
                }
                if (GetFilledInner(p.x, p.y, true).Contains(except[i])) continue;
                Grid[p.x][p.y] = except[i];
                UnfilledCount--;
                CleanTempGrid(p.x, p.y);
                ClearRelativeTemp(p.x, p.y, i);
                }
            }
        }
        private bool SameInner(Point p1, Point p2)
        {
            return (GetInnerRange(p1.x) == GetInnerRange(p2.x)) && (GetInnerRange(p1.y) == GetInnerRange(p2.y));
        }
        private Point[] GetCriticalPointsFromValue(int x, int y, int val)
        {
            Point[] track = new Point[FullGridWidth];
            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);
            int pt = 0;
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (TempGrid[xa][ya].Contains(val)) track[pt++] = new Point(xa, ya);
                }
            }
            Point.TrimEndPt(ref track);
            return track;

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
                                CleanTempGrid(p.x, p.y);
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
            int c = 0; //end index counter
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
            return Point.TrimAt(ref track, c);
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
            Point.TrimEndPt(ref pt);
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
            //LinkedGrid = new LinkedPoint[FullGridWidth][];

            //Fullints initialize

            FullInts = new int[FullGridWidth];
            //SearchGrid = new bool[FullGridWidth][];
            for (int i = 0; i < FullGridWidth; i++)
            {
                FullInts[i] = i + 1;
                Grid[i] = new int[FullGridWidth];
                BackupGrid[i] = new int[FullGridWidth];
                TempGrid[i] = new int[FullGridWidth][];
                //LinkedGrid[i] = new LinkedPoint[FullGridWidth];
                //SearchGrid[i] = new bool[FullGridWidth];
            }

            //LinkedGrid test
            //for (int x = 0; x < FullGridWidth; x++)
            //{
            //    for (int y = 0; y < FullGridWidth; y++)
            //    {
            //        LinkedGrid[x][y] = new LinkedPoint(new Point(x, y));
            //    }
            //}

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
            Validator.Init(SingleBlockWidth, FullInts);
        }
        #endregion

        #region Test
        public void EnvironmentScan()
        {
            PrintAll();
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    for (int xa = 0; xa < FullGridWidth; xa++)
                    {
                        for (int ya = 0; ya < FullGridWidth; ya++)
                        {
                            if (Grid[x][y] != 0)
                                LinkedGrid[x][y] = LinkedPoint.NullPoint;
                            if (Grid[x][y]!= 0 && LinkedGrid[x][y].Contains(new Point(xa, ya)))
                            {
                                RemoveItemFromTemp(Grid[x][y], ref TempGrid[x][y]);
                                LinkedGrid[xa][ya].Remove(new Point(xa, ya));
                            }
                            if (LinkedGrid[xa][ya].IsSolvable)
                            {
                                Grid[xa][ya] = TempGrid[xa][ya][0];
                                CleanTempGrid(xa, ya);
                                ClearRelativeTemp(xa, ya, Grid[xa][ya]);
                                LinkedGrid[xa][ya] = LinkedPoint.NullPoint;
                            }
                            PrintAll();
                        }
                    }
                }
            }
        }
        public LinkedPoint[][] LinkedGrid;
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