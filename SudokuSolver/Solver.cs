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
        private string[,] Grid;
        /// <summary>
        /// Temp grid for holding possible values
        /// サブ表。マスに入れる可能性の値を保管する
        /// </summary>
        private int[,][] TempGrid;
        //private TempBlock[,] TempGrid; //[x, y]
        /// <summary>
        /// Measure used when printing out grids
        /// 表のプリントアウト時に使う測り
        /// </summary>
        private int[] SeparateLines = { 0, 3, 6 };
        /// <summary> 
        /// All possible values array used for filter out possible values
        /// 可能の値の配列。不可能値の排除に使う
        /// </summary>
        private int[] FullInts = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
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
        /// <summary> 
        /// Switch for changing printout 
        /// 最終プリントアウトのスイッチ
        /// </summary>
        private bool treesuccess = true;
        public bool finished = false;
        public int UnfilledCount;
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
            if (ReadLines()) { Console.Clear();  goto SETSIZE; } //if ReadLines return true, reset grid size
            Console.WriteLine();
            Console.WriteLine("Input values are: ");
            PrintAll();
            Console.WriteLine("Press enter key to continue...");
            Console.Read();

            //dx = 6;
            //dy = 2;
            //AdvancedHook(9);
            GetFilledCount();
            Stopwatch stop = new Stopwatch();
            stop.Start();
            Basic(); //preparation
            Console.WriteLine("Basic try:");
            if (UnfilledCount==0) goto FINISH; //if finished at this point, jump to finish
            PrintAll();
            //Console.WriteLine(DateTime.Now.Subtract(now));
            Advanced();
            
            Console.WriteLine("Advanced try:");
            if (UnfilledCount==0) goto FINISH; //if finished at this point, jump to finish
            PrintAll();

            //Logic();
            //Console.WriteLine("Logical try:");
            //if (UnfilledCount==0) goto FINISH; //if finished at this point, jump to finish
            //PrintAll();

            //Console.WriteLine(DateTime.Now.Subtract(now));
            Console.WriteLine("Now solving...");

            treesuccess = TreeDiagramSolve();
            
            FINISH:
            if (treesuccess) { finished = true; Console.WriteLine("Result:"); }
            PrintAll();
            stop.Stop();


            Console.WriteLine("Time spent: {0}", TimeSpan.FromMilliseconds(stop.ElapsedMilliseconds));

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
            InnerHVLeftOverCheck();
            
        }
        void Advanced()
        {
            //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed;
            string[,] backupGrid = Grid.Clone() as string[,]; 
            while ((UnfilledCount!=0))
            {
                CheckInnerBoxLeftOver();
                CheckHVLeftOver();
                AdvancedFill(true);
                
                bool gridsame = GridSame(backupGrid);
                if (gridsame) break; //if advancedfill & checkleftover2 cant handle it anymore
                backupGrid = Grid.Clone() as string[,]; //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed;
            }
            //InnerHVLeftOverCheck();
        }
        void Logic()
        {
            string[,] backupGrid = Grid.Clone() as string[,];
            while ((UnfilledCount!=0))
            {
                for (int x = 0; x < FullGridWidth; x+=SingleBlockWidth)
                {
                    for (int y = 0; y < FullGridWidth; y+=SingleBlockWidth)
                    {
                        Print3x3(x, y);
                        int[] left = (FullInts.Except(GetFilledInner(x, y))).ToArray();
                        bool xnoneflag = true;
                        bool ynoneflag = true;

                        Point[] inner = GetInnerEmpty(x, y);
                        foreach (Point item in inner)
                        {
                            Point[] xrowempt = GetXRowEmpty(item.x, item.y);
                            Point[] yrowempt = GetYRowEmpty(item.x, item.y);
                            
                            foreach (int item2 in left)
                            {
                                foreach (Point item3 in xrowempt)
                                {
                                    int[] pos = GetPossible(item3.x, item3.y);
                                    if (pos.Contains(item2))
                                    {
                                        xnoneflag = false;
                                    }
                                    xnoneflag = (xnoneflag) ? false : true;
                                }
                                foreach (Point item3 in yrowempt)
                                {
                                    int[] pos = GetPossible(item3.x, item3.y);
                                    if (pos.Contains(item2))
                                    {
                                        ynoneflag = false;
                                    }
                                }
                                if (xnoneflag||ynoneflag)
                                {
                                    Grid[item.x, item.y] = item2.ToString();
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
                backupGrid = Grid.Clone() as string[,]; //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed;
            }
        }
        bool TreeDiagramSolve()
        {
            //create TreeDiagram instance for easier code management
            TreeDiagram td = new TreeDiagram(Grid, SingleBlockWidth,FullInts,TempGrid,UnfilledCount);
            //Console.WriteLine(DateTime.Now.Ticks - start);
            td.Execute2(); //probably faster and lighter than Execute()
            //td.Execute3(false); //Debugger throws StackOverflow exception at random places
            //td.Execute4();
            //Console.WriteLine("UNFILLEDCOUNT: {0}", td.UnfilledCount);
            
            if (!td.FinishFlag)
            {
                Console.WriteLine("Grid invalid");
                Console.WriteLine(td.UnfilledCount);
            }
            else
            {
                Grid = td.Grid;
            }
            return td.FinishFlag;
        }

        /// <summary>
        /// Inner horizontal + vertical leftover check
        /// </summary>
        private void InnerHVLeftOverCheck()
        {
            string[,] backupGrid = Grid.Clone() as string[,];
            bool gridsame = false;
            while (UnfilledCount!=0)
            {
                backupGrid = Grid.Clone() as string[,]; //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed;
                CheckHVLeftOver();
                CheckInnerBoxLeftOver();


                gridsame = GridSame(backupGrid);
                if (gridsame) break; //if advancedfill & checkleftover2 cant handle it anymore
            }
        }

        private bool GridSame(string[,] backup)
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
                        splitted.ToList().ForEach(c => c.Trim());
                        int pt = 0;
                        for (int b = pt; b < splitted.Length; b++)
                        {
                            Grid[a, b] = splitted[b];
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
                split.ToList().ForEach(a => a.Trim()); //trim each item
                for (int x = 0; x < FullGridWidth; x++)
                {
                    //if 2-digit value is possible, split check
                    if ((FullGridWidth < 10) && (split[0].Length == FullGridWidth))
                    {
                        for (int z = 0; z < FullGridWidth; z++)
                        {
                            Grid[i, x] = split[0][z].ToString();
                        }
                    }
                    else //else don't care about having space in between
                    {
                        Grid[i, x] = split[x];
                    }
                }
            }
            return false;
        }
        private void ReadLength()
        {
            //if (!LineValid(line)) { i--; ClearLine(); continue; }//if invalid, reod
            string line = Console.ReadLine();
            string[] escapestrings = {"0","null","-","--"};

            if (escapestrings.Any(a => a.Equals(line))) { SingleBlockWidth = 3; return; }// use default size
            bool valid = false;
            int testval = -1;

            while (!valid)
            {

                if (escapestrings.Any(a => a.Equals(line))) return;// use default size
                try
                {
                    testval = int.Parse(line);
                    //size cannot be 1x1, grids size less than 9x9 are too annoying Lol
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
            //strip off x
            string tmp = string.Empty;
            string[] split = values.Trim().Split(' ');
            List<int> vals = new List<int>();
            int xchar_count = 0;
            foreach (string item in split)
            {
                string trimmed = item.Trim();
                if (trimmed.Equals("x")) {xchar_count++; continue;}
                if ((FullGridWidth < 10) && (item.Length == FullGridWidth)) return true;
                if ((!char.IsNumber(trimmed[0])) && (!trimmed.Equals("x"))) return false; //not number, nor "x". ONLY ALLOW X as variable
                vals.Add(int.Parse(trimmed));
                if (int.Parse(trimmed) <= 0) return false; //do not allow 0 or negative value
                if (int.Parse(trimmed) > FullGridWidth) return false; //what falls in this line MUST and ALWAYS is a number
            }
            bool lengthmatch = ((xchar_count + vals.Count) == FullGridWidth);//input values count, then compare with allowed && supposed length
            return lengthmatch;
            //return (values.Length == FullGridWidth);
        }
        #endregion

        private void GetFilledCount()
        {
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    if (Grid[x, y].Equals("x")) UnfilledCount++;
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
                    Grid[i, x] = "x";
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
                for (int x = 0; x < FullGridWidth; x++)
                {
                    Console.Write(lines[i][x]);
                    Grid[i, x] = lines[i][x].ToString();
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
                split.ToList().ForEach(a => a.Trim()); //trim each item
                for (int x = 0; x < FullGridWidth; x++)
                {
                    Console.Write(lines[i][x]);
                    Grid[i, x] = split[x];
                }
                Console.Write("\n");
            }


        }

        #endregion
        
        #region Debug
        private int? dx = null, dy = null;
        private void HookEventHandler()
        {
            if ((dx==null)||(dy==null)) return; // do nothing if axis not specified
            if ((dx > FullGridWidth -1 ) || (dy > FullGridWidth -1)) return; // do nothing if specifid hook axis are OOB
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
                    if (!Grid[xa, ya].Equals("x")) continue; // no need to print out possible values for filled in
                    int[] poss = GetPossible(xa, ya);
                    if (poss.Intersect(filter).Any())
                    {
                        Console.WriteLine(@"[{0},{1}]", xa, ya);
                        poss.ToList().ForEach(new Action<int>(a =>
                        {
                            Console.Write(a + " ");
                        }));
                        Console.Write("\n");
                    }
                }
            }
            

            Console.WriteLine();
        }
        #endregion
        #region Printout
        private void PrintHorizontalBorder(bool withnewline = false)
        {
            string outstr = string.Empty;

            // | + 532... + x x x
            int vertical = 1 + SingleBlockWidth + (SingleBlockWidth - 1);
            if ((FullGridWidth > 9)) vertical += (FullGridWidth/4);
            //if grid is greater than 9x9, it means that possible max value is 2-digit, meaning requires double space
            //for example: 16x16 grid. each block has 4x4 values, each 2-digit. 
            //because of the 2-digit, it means we need to add double of what we have now.
            //since each loop adds in 2-values, "- ", it means we don't actually need *2
            //we only need 1/4 of the FullGridWidth
            
            int vertical_border = SingleBlockWidth + 1;
            int total = FullGridWidth + vertical_border;

            for (int i = 0; i < SingleBlockWidth; i++)
            {
                for (int a = 0; a < (vertical/2); a++)
                {
                    outstr += "- ";
                }
            }
            outstr += "-";
            Console.WriteLine("{0}{1}", (withnewline) ? "\n" : string.Empty, outstr);
        }
        private void PrintAll()
        {
            //Console.WriteLine("- - - - - - - - - -");
            PrintHorizontalBorder();
            for (int i = 0; i < FullGridWidth; i++)
            {
                for (int x = 0; x < FullGridWidth; x++)
                {
                    if (x % SingleBlockWidth == 0)
                    {
                        Console.Write("|");
                    }
                    else
                    {
                        Console.Write(" ");
                    }
                    string block = Grid[i, x];
                    Console.Write("{0}{1}", ((FullGridWidth > 9) && (block.Length == 1)) ? " " : string.Empty, block);

                }
                Console.Write("|");//EOF border
                if (((i + 1) % SingleBlockWidth == 0))
                {
                    PrintHorizontalBorder(true);
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
                Console.Write(TempGrid[x,y][i] + " ");
            }
            //TempGrid[x, y].Possibles.ForEach(new Action<int>(a =>
            //{
            //    Console.Write(a + " ");
            //}));
        }
        private void Print3x3(int x, int y)
        {
            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);

            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (!Grid[xa, ya].Equals("x")) continue; // no need to print out possible values for filled in
                    int[] poss = GetPossible(xa, ya);
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
            TempGrid[x,y] = new int[0];
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
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    //TempGrid[x, y].Possibles.Clear(); //clear and fill new
                    //int[] poss = GetPossible(x, y);
                    //TempGrid[x, y].Possibles.AddRange(poss); //add possible values
                    if ((SeparateLines.Contains(x)) && (SeparateLines.Contains(y))) //dont repeat in each innerblock
                    {
                        AdvancedInBlockCheck(x, y, clearafter);
                        //AdvancedInBlockCheck2(x, y);
                        //AdvancedInBlockCheck(x, y);
                    }
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

            for (int xa = (xpos- SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos- SingleBlockWidth); ya < ypos; ya++)
                {
                    if ((!includeself)&&((xa == x) && (ya == y))) continue; //skipself
                    if (!Grid[xa, ya].Equals("x")) continue; //skip filled in
                    poss.AddRange(GetPossible(xa, ya));
                }
            }
            return poss.Distinct().ToArray();
        }

        private void AdvancedInBlockCheck(int x, int y, bool clearafter = true)
        {
            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);
            for (int xa = (xpos- SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos- SingleBlockWidth); ya < ypos; ya++)
                {
                    if (!Grid[xa, ya].Equals("x")) { if (clearafter) { CleanTempGrid(xa, ya); } continue; }//skip if filled
                    List<int> inboxexceptselfpossible = InBlockPossible(xa, ya, false).ToList();
                    var selfpossible = GetPossible(xa, ya);
                    TempGrid[xa, ya] = selfpossible;
                    //TempGrid[xa, ya].Possibles = selfpossible.ToList();
                    var diff = selfpossible.Except(inboxexceptselfpossible).ToList();
                    if (diff.Count == 1)
                    {
                        Grid[xa, ya] = diff[0].ToString();
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
                for (int y = 0; y < FullGridWidth; y+= SingleBlockWidth)
                {
                    int c = CountInnerBlockEmpty(x, y);
                    if (c == 1)
                    {
                        int[] filledvals = GetFilledInner(x, y);
                        Point[] empts = GetInnerEmpty(x, y, false);
                        if (empts.Length == 0) continue;
                        Grid[empts[0].x, empts[0].y] = FullInts.Except(filledvals).ToArray()[0].ToString();
                        UnfilledCount--;
                        
                    }
                    else
                    {
                        foreach (Point item in GetInnerEmpty(x, y, false))
                        {
                            int[] poss = GetPossible(item.x, item.y, true);
                            if (poss.Length == 1)
                            {
                                Grid[item.x, item.y] = poss[0].ToString();
                                UnfilledCount--;
                            }
                        }
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
                    if (Grid[xa, ya].Equals("x")) track++;
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
                        if (Grid[pos, y].Equals("x")) track++;
                    }
                    return track;
                case Axis.VERTICAL:
                    for (int x = 0; x < FullGridWidth; x++)
                    {
                        if (Grid[x, pos].Equals("x")) track++;
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
                    if (!Grid[xa, ya].Equals("x")) track.Add(int.Parse(Grid[xa, ya]));
                }
            }
            return track.ToArray();
        }
        private enum Axis
	    {
	            HORIZONTAL,
                VERTICAL
	    }
        private int[] GetFilledInRow(int pos, Axis axis)
        {
            List<int> track = new List<int>();
            switch (axis)
            {
                case Axis.HORIZONTAL:
                    for (int y = 0; y < FullGridWidth; y++)
                    {
                        if (!Grid[pos, y].Equals("x")) track.Add(int.Parse(Grid[pos, y]));
                    }
                    track.Sort(); //maybe not required;
                    return track.ToArray();
                case Axis.VERTICAL:
                    for (int x = 0; x < FullGridWidth; x++)
                    {
                        if (!Grid[x, pos].Equals("x")) track.Add(int.Parse(Grid[x, pos]));
                    }
                    track.Sort(); //maybe not required;
                    return track.ToArray();
            }
            return track.ToArray(); //will never happen
        }
        /// <summary>
        /// Checks the horizontal + vertical leftover
        /// </summary>
        private void CheckHVLeftOver()
        {
            for (int x = 0; x < FullGridWidth; x += SingleBlockWidth)
            {
                for (int y = 0; y < FullGridWidth; y += SingleBlockWidth)
                {
                    
                    int xrowempty = CountRowEmpty(x, Axis.HORIZONTAL);
                    if (xrowempty == 1)
                    {
                        int[] xfilledvals = GetFilledInRow(x, Axis.HORIZONTAL);
                        Point[] empts = GetXRowEmpty(x, y, true, true);
                        Grid[empts[0].x, empts[0].y] = FullInts.Except(xfilledvals).ToList()[0].ToString();
                        UnfilledCount--;
                        continue;//if filled in, dont go check next
                    }
                    int yrowempty = CountRowEmpty(y, Axis.VERTICAL);
                    if (yrowempty == 1)
                    {
                        int[] yfilledvals = GetFilledInRow(y, Axis.VERTICAL);
                        Point[] empts = GetYRowEmpty(x, y, true, true);//GetInnerEmpty(x,y, false);
                        Grid[empts[0].x, empts[0].y] = FullInts.Except(yfilledvals).ToList()[0].ToString();
                        UnfilledCount--;
                    }
                }
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
                        if (!Grid[x, y].Equals("x")) continue; // no need to print out possible values for filled in

                        if (TempGrid[x, y].Length == 1) { Grid[x, y] = TempGrid[x, y][0].ToString(); CleanTempGrid(x, y); UnfilledCount--; continue; }
                            
                        Point[] gxre = GetXRowEmpty(x, y,true,true);
                        Point[] gyre = GetYRowEmpty(x, y,true,true);
                        
                        //List<Tuple<Point, int[]>> mergedxy = new List<Tuple<Point, int[]>>();
                        List<int> ints = new List<int>();

                        foreach (Point item in gxre)
                        {
                            int[] poss = GetPossible(item.x, item.y);
                            //mergedxy.Add(new Tuple<Point, int[]>(item, poss));
                            ints.AddRange(poss);
                        }

                        //Console.WriteLine();
                        foreach (Point item in gyre)
                        {
                            int[] poss = GetPossible(item.x, item.y);
                            //mergedxy.Add(new Tuple<Point, int[]>(item, poss));
                            ints.AddRange(poss);
                        }

                        if (x == 9 && y == 11)
                        {
                            int a = 0;

                        }
                        TempGrid[x, y] = TempGrid[x, y].Except(ints).ToArray();
                        //TempGrid[x, y].Possibles = TempGrid[x, y].Possibles.Except(ints).ToList();

                        if (TempGrid[x, y].Length ==1)
                        {
                            //int[] tmp = TempGrid[x, y].Except(GetInnerBlock(x, y)).ToArray();
                            //tmp[0].ToString();
                            Grid[x, y] = TempGrid[x, y][0].ToString();
                            UnfilledCount--;
                            CleanTempGrid(x, y);
                        }
                        //if (TempGrid[x, y].Possibles.Count == 1)
                        //{
                        //    Grid[x, y] = TempGrid[x, y].Possibles[0].ToString();
                        //    UnfilledCount--;
                        //    TempGrid[x, y].Possibles.Clear();
                        //}
                        //mergedxy.Clear();
                    }
                }


                gridsame = TempGridSame(backupGrid);
                if (gridsame) break; //if advancedfill & checkleftover2 cant handle it anymore
                backupGrid = TempGrid.Clone() as int[,][];
                //backupGrid = TempGrid.Clone() as TempBlock[,]; //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed
            }
            TempGrid = backupGrid;

            

        }

        private Point[] GetRowEmpty(int x, int y, bool includeinnerself = false, bool includeself = false)
        {
            List<Point> vals = new List<Point>();
            int innery = GetInnerRange(y);
            int innerx = GetInnerRange(x);
            //from x
            for (int a = 0; a < FullGridWidth; a++)
            {
                if (!includeinnerself) if ((a >= innery - SingleBlockWidth) && (a < innery) && (a < innerx)) continue;
                if ((!includeself) && ((a == y)||(a==x))) continue;
                if (Grid[x, a][0] == 'x')
                {
                    vals.Add(new Point() { x = x, y = a });
                }
                if (Grid[a, y][0] == 'x')
                {
                    vals.Add(new Point() { x = a, y = y });
                }
            }
            return vals.ToArray();
        }
        private Point[] GetXRowEmpty(int x, int y, bool includeinnerself = false, bool includeself = false)
        {
            List<Point> vals = new List<Point>();
            int innery = GetInnerRange(y);
            //from x
            for (int a = 0; a < FullGridWidth; a++)
            {
                if (!includeinnerself) if ((a>=innery-SingleBlockWidth)&&(a<innery)) continue;
                if ((!includeself) && (a == y)) continue;
                if (Grid[x, a][0] == 'x')
                {
                    vals.Add(new Point() { x = x, y = a });
                }
            }
            return vals.ToArray();
        }

        private Point[] GetYRowEmpty(int x, int y, bool includeinnerself = false, bool includeself = false)
        {
            List<Point> vals = new List<Point>();
            int innerx = GetInnerRange(x);
            //from y
            for (int b = 0; b < FullGridWidth; b++)
            {
                if (!includeinnerself) if ((b >= innerx - SingleBlockWidth) && (b < innerx)) continue;
                if ((!includeself) && (b == x)) continue;
                if (Grid[b,y][0] == 'x')
                {
                    vals.Add(new Point() { x = b, y = y });
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
            List<Point> pt = new List<Point>();
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if ((notincludeself)&&(xa == x) && (ya == y)) continue; //skip self if notincludeself
                    if (Grid[xa, ya].Equals("x")) pt.Add(new Point() { x=xa, y = ya});
                }
            }
            return pt.ToArray();
        }

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
            int num = -1;
            int pointer = 0;


            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);
            //3x3
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if ((notincludeself) && (xa == x) && (ya == y)) continue; //skip self if notincludeself
                    if (!Grid[xa, ya].Equals("x"))
                    {
                        pos[pointer] = int.Parse(Grid[xa, ya]);
                        pointer++;
                    }
                    //if (char.IsNumber((char)Grid[xa, ya].ToString()[0]))
                    //{
                    //    pos.Add(Grid[xa, ya]);
                    //    //num = int.Parse(Grid[xa, ya]);
                    //    //char charac = (char)num;
                    //    //pos.Add(charac.ToString());
                    //}
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
                if (!Grid[x, a].Equals("x"))
                {
                    values[pointer] = int.Parse(Grid[x, a]);
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
                if (!Grid[b, y].Equals("x"))
                {
                    values[pointer] = int.Parse(Grid[b, y]);
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
            Grid = new string[FullGridWidth,FullGridWidth]; //MainGrid
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

            int[] ints = new int[FullGridWidth];
            //Separate lines initialize
            for (int i = 0; i < FullGridWidth; i+=SingleBlockWidth)
            {
                //ints.Add(i);
                ints[i] = i;
            }
            SeparateLines = ints;//.ToArray();

            //Tempgrid possible list
            //for (int x = 0; x < FullGridWidth; x++)
            //{
            //    for (int y = 0; y < FullGridWidth; y++)
            //    {
            //        TempGrid[x, y] = new TempBlock();
            //    }
            //}

        }
        #endregion

        /// <summary>
        /// Checks if the filled grid is failure
        /// </summary>
        /// <returns>True if failed, False if succes</returns>
        private bool CheckFailure()
        {
            for (int x = 0; x < FullGridWidth; x += (SingleBlockWidth+1))
            {
                for (int y = 0; y < FullGridWidth; y += (SingleBlockWidth + 1))
                {
                    int[] pos = FullInts.Except(GetInnerBlockRowImpossible(x, y)).ToArray();
                    if (pos.Length != 0) return true;
                }
            }
            return false;
        }

        #region Support
        private void Clone (int[,][] obj, ref int[,][] destination)
        {
            for (int x = 0; x < FullGridWidth; x++)
			{
                for (int y = 0; y < FullGridWidth; y++)
                {
			        destination[x,y] = obj[x,y];
                }
			}
        }
        #endregion
    }
    
}