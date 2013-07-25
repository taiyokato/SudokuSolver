using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace SudokuSolver
{
    partial class Solver
    {
        private string[,] Grid; // Type=string for compatibility for 2-digit values and char 'x'
        private TempBlock[,] TempGrid; //[x, y]
        private int[] SeparateLines = { 0, 3, 6 };
        private int[] FullInts = { 1, 2, 3, 4, 5, 6, 7, 8, 9 }; // basic 9x9 grid 
        protected int? singleblockw;
        private int SingleBlockWidth
        {
            get { return (singleblockw.HasValue) ? singleblockw.Value : 3; }
            set { singleblockw = value; }
        }
        private int FullGridWidth
        {
            get { return (SingleBlockWidth * SingleBlockWidth); }
        }
        private bool treesuccess = true;

        public Solver()
        {

            //Get the size first, then initialize Grid
            SETSIZE:
            Console.WriteLine("Input grid width: ");
            Console.WriteLine(" Usable escape strings are available.\n The default grid size will be 9x9 if escape strings are used");
            Console.WriteLine(" Usable escape strings for default are:");
            Console.WriteLine(" {0, null, -, --}");
            ReadLength();//set the grid length

            Console.Clear(); //Clear console
            Console.WriteLine("Grid size will be: {0}x{0}", FullGridWidth);
            Console.WriteLine();
            //initialize
            Initialize();


            Console.WriteLine("Input Each line\n Enter empty values as x\nSeparate values with a space: ");
            Console.WriteLine("Input \"redo\" to re-set grid size");
            if (ReadLines()) { Console.Clear();  goto SETSIZE; }

            Console.WriteLine();
            Console.WriteLine("Input values are: ");
            PrintAll();
            Console.WriteLine("Press any key to continue...");
            Console.Read();
            
            DateTime now = DateTime.Now;
            Basic(); //preparation
            Console.WriteLine("Basic try:");
            if (!CheckFailure()) goto FINISH; //if finished at this point, jump to finish
            PrintAll();
            
            Advanced();
            Console.WriteLine("Advanced try:");
            if (!CheckFailure()) goto FINISH; //if finished at this point, jump to finish
            PrintAll();

            Console.WriteLine("Now solving...");

            treesuccess = TreeDiagramSolve();
            //PrintTempStack();

            FINISH:
            if (treesuccess) Console.WriteLine("Result:");
            PrintAll();
            TimeSpan diff = DateTime.Now.Subtract(now);
            Console.WriteLine("Time spent: {0}", diff.ToString());

            Console.WriteLine("[EOF]");
            Console.WriteLine("Press enter to finish");

            Console.Read();
            Console.Read();
        }


        void Basic()
        {
            FillTemp();
            BasicSolve();
        }
        void Advanced()
        {
            int loop = 30;
            
            int trackloop = 1;
            //Auto loop until all complete. Manual break available
            string[,] backupGrid = Grid.Clone() as string[,]; //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed;
            while (!CheckFinish())
            {
                //if (trackloop == loop)
                //{ // for manual break
                //    //Console.WriteLine("Guess limit reached. Manual break."); 
                //    //break; 
                //}
                //Console.WriteLine("Advanced Solve {0}:", trackloop);
                AdvancedFill(true);
                CheckLeftOver2();
                bool gridsame = GridSame(backupGrid);
                if (gridsame) break; //if advancedfill & checkleftover2 cant handle it anymore
                backupGrid = Grid.Clone() as string[,]; //MUST USE Clone() as string[,] to prevent backupGrid getting overwritten when Grid is changed;
                trackloop++;
            }
        }
        bool TreeDiagramSolve()
        {
            TreeDiagram td = new TreeDiagram(Grid, SingleBlockWidth);
            td.ExecuteTry();
            if (!td.FinishFlag)
            {
                Console.WriteLine(td.Error.Message);
                return false;
            }
            else
            {
                Grid = td.Grid;
                return true;
            }
        }
        void PrintTempStack()
        {
            Console.WriteLine();
            Console.WriteLine("TempGrid Possibility CountStack: ");
            PrintAllTempStack();
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
            //PrintAll();
            //string[,] temp = Grid;
            //Grid = backup;
            //PrintAll();
            //Grid = temp;
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
                if (line.Equals("16x16")) line = @"C:\users\15110751\desktop\16x16.txt";
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
                if (line.Trim().ToLower().Equals("redo")) return true;
                if (line.Trim().Equals("")) { i--; ClearLine(); continue; }
                if (line.Equals("testvals")) { TestVals9x9empty(); break; } //test empty values
                if (line.Equals("testvals2")) { TestVals9x9(); break; } //test sample values
                if (line.Equals("testvals4x4")) { TestVals16x16(); break; } //test sample values
                if (!LineValid(line)) { i--; ClearLine(); continue; }//if invalid, reod
                string[] split = line.Split(' '); //split
                split.ToList().ForEach(a => a.Trim()); //trim each item
                for (int x = 0; x < FullGridWidth; x++)
                {
                    if ((FullGridWidth < 10) && (split[0].Length == FullGridWidth))
                    {
                        for (int z = 0; z < FullGridWidth; z++)
                        {
                            Grid[i, x] = split[0][z].ToString();
                        }
                    }
                    else
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
                    if (Math.Sqrt((double)testval) % 1 == 0) break; //check if testval is a valid number of n^n, by seeing if self rooting results a whole number
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

        /// <summary>
        /// Checks each block whether it is filled or not.
        /// </summary>
        /// <returns>True if all is NOT x, False if even one is x</returns>
        private bool CheckFinish()
        {
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    if (Grid[x, y].Equals("x")) return false;
                }
            }
            return true;
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
        private void PrintAllTempStack()
        {
            PrintHorizontalBorder(false);
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
                    int count = TempGrid[i, x].Possibles.Count;
                    Console.Write("{0}{1}",((FullGridWidth>9)&&(count.ToString().Length==1)) ? " " : string.Empty, count);

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
            //Console.Clear();
            Console.WriteLine("Specific {0},{1}:", x, y);
            TempGrid[x, y].Possibles.ForEach(new Action<int>(a =>
            {
                Console.Write(a + " ");
            }));
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
        #endregion

        #region 後始末
        private void CleanTempGrid(int x, int y)
        {
            TempGrid[x, y].Possibles.Clear();
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
                    TempGrid[xa, ya].Possibles = selfpossible.ToList();
                    var diff = selfpossible.Except(inboxexceptselfpossible).ToList();
                    if (diff.Count == 1)
                    {
                        Grid[xa, ya] = diff[0].ToString();
                        CleanTempGrid(xa, ya);
                    }
                }
            }
        }
        
        #endregion

        private void BasicSolve()
        {
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    //HookEventHandler();
                    if (!Grid[x, y].Equals("x")) continue; //DONT DO ANYTHING IF THE BLOCK IS ALREADY FILLED WITH NUMBER
                    if (TempGrid[x, y].Possibles.Count == 1) { Grid[x, y] = TempGrid[x, y].Possibles[0].ToString(); CleanTempGrid(x, y); }
                }
            }
        }

        #region Leftover possible value check
        private void CheckLeftOver2()
        {
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    if (TempGrid[x, y].Possibles.Count == 1)
                    {
                        Grid[x, y] = TempGrid[x, y].Possibles[0].ToString();
                        CleanTempGrid(x, y);
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

        private void FillTemp()
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
        /// <summary>
        /// Gets all possible values
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int[] GetPossible(int x, int y, bool notincludeself = false)
        {
            List<int> impossiblevals = GetInnerBlock(x, y, notincludeself).Concat(GetRowImpossible(x, y)).ToList<int>();
            impossiblevals = impossiblevals.Distinct().ToList(); //remove duplicate
            return FullInts.Except(impossiblevals.ToArray()).ToArray<int>(); //returns possible values
        }

        private int GetInnerRange(int loc)
        {
            List<int[]> RangeList = new List<int[]>();
            List<int> tmplst = new List<int>();

            int next = 0;
            for (int i = 0; i < SingleBlockWidth; i++)
            {
                for (int x = next; x < (next + SingleBlockWidth); x++)
                {
                    tmplst.Add(x);
                }
                next += SingleBlockWidth;
                RangeList.Add(tmplst.ToArray());
                tmplst.Clear();
            }
            int index = RangeList.FindIndex(a => a.Contains(loc)); //return the section's last item index (1~)
            int indexcalc = ((index * SingleBlockWidth) + SingleBlockWidth);
            return indexcalc;

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
        private int[] GetInnerBlock(int x, int y, bool notincludeself = false)
        {
            //x, y-> 0-8
            //List<int> pos = new List<int>();
            List<string> pos = new List<string>();
            int num = -1;


            int xpos = GetInnerRange(x);
            int ypos = GetInnerRange(y);
            //3x3
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if ((notincludeself) && (xa == x) && (ya == y)) continue; //skip self if notincludeself
                    if (char.IsNumber((char)Grid[xa, ya].ToString()[0]))
                    {
                        pos.Add(Grid[xa, ya]);
                        //num = int.Parse(Grid[xa, ya]);
                        //char charac = (char)num;
                        //pos.Add(charac.ToString());
                    }
                }
            }
            return pos.Distinct().Select(int.Parse).ToArray();//remove duplicates, return list as int[]
        }
        
        /// <summary>
        /// Gets the IMPOSSIBLE values in the horizontal vertical line across the specified location
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int[] GetRowImpossible(int x, int y)
        {
            //List<int> values = new List<int>();
            List<string> values = new List<string>();
            int num = -1;

            //from x
            for (int a = 0; a < FullGridWidth; a++)
            {
                if (char.IsNumber((char)Grid[x, a].ToString()[0]))
                {
                    values.Add(Grid[x, a]);
                    //num = int.Parse(Grid[x, a]);
                    //char charac = (char)num;
                    //values.Add(charac.ToString());
                }
            }
            //from y
            for (int b = 0; b < FullGridWidth; b++)
            {
                if (char.IsNumber((char)Grid[b, y].ToString()[0]))
                {
                    values.Add(Grid[b, y]);
                    //num = int.Parse(Grid[b, y]);
                    //char charac = (char)num;
                    //values.Add(charac.ToString());
                }
            }
            int[] ret = values.Distinct().Select(int.Parse).ToArray(); //remove duplicates and return IMPOSSIBLE values
            return ret;

        }
        #endregion

        #region Initialize
        /// <summary>
        /// Initializes Grid, TempGrid, FullInts, Separate lines size, and Tempgrid TempBlock item
        /// </summary>
        private void Initialize()
        {
            Grid = new string[FullGridWidth,FullGridWidth]; //MainGrid
            TempGrid = new TempBlock[FullGridWidth, FullGridWidth]; //TempGrid

            //Fullints initialize
            List<int> ints = new List<int>();
            for (int i = 1; i <= FullGridWidth; i++)
            {
                ints.Add(i);
            }
            FullInts = ints.ToArray();
            ints.Clear();
            
            //Separate lines initialize
            for (int i = 0; i < FullGridWidth; i+=SingleBlockWidth)
            {
                ints.Add(i);
            }
            SeparateLines = ints.ToArray();

            //Tempgrid possible list
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    TempGrid[x, y] = new TempBlock();
                }
            }
        }
        /// <summary>
        /// Creates a new TempBlock[x,y] list and returns it.
        /// Initializes the TempBlock[x,y] list
        /// </summary>
        /// <param name="xsize">Height. 0-8</param>
        /// <param name="ysize">Width. 0-8</param>
        /// <returns>Initialized empty TempBloc[x,y]</returns>
        private TempBlock[,] CreateCustomTemp(int xsize, int ysize)
        {
            TempBlock[,] tmp = new TempBlock[xsize, ysize];
            for (int x = 0; x < xsize; x++)
            {
                for (int y = 0; y < ysize; y++)
                {
                   tmp[x,y] = new TempBlock();
                }
            }
            return tmp;
        }
        #endregion

        #region GetNext
        private int PossibleCounter = 0;
        /// <summary>
        /// Gets next possible empty in innerblock
        /// </summary>
        /// <param name="startx">InnerRange x</param>
        /// <param name="starty">InnerRange y</param>
        /// <returns>Exact Point location</returns>
        private Point GetNextPossible(int startx = 0, int starty = 0)
        {
            int xpos = GetInnerRange(startx);
            int ypos = GetInnerRange(starty);
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (TempGrid[xa, ya].Possibles.Count != 0)
                    {
                        PossibleCounter = 0;
                        return new Point() { x = xa, y = ya };
                    }
                }
            }

            return new Point();


        }
        private int least_count = 0;
        private Point GetLeastEmptyBlock(int startx = 0, int starty = 0, int startfrom = -1)
        {
            //<KeyvaluePair<Bigblock axis, Possible location count>, possible count>
            List<KeyValuePair<KeyValuePair<Point, int>, int>> tracker = new List<KeyValuePair<KeyValuePair<Point, int>, int>>();
            //List<KeyValuePair<Point, int>> tracker = new List<KeyValuePair<Point, int>>();
            int count = 0;
            int leastcount = FullGridWidth;
            int possiblesum = 0;

            int xpos = -1, ypos = -1;

            for (int x = startx; x < FullGridWidth; x += SingleBlockWidth)
            {
                for (int y = starty; y < FullGridWidth; y += SingleBlockWidth)
                {
                    xpos = GetInnerRange(x);
                    ypos = GetInnerRange(y);
                    for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
                    {
                        for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                        {
                            if (TempGrid[xa, ya].Possibles.Count > 0) { count++; possiblesum += TempGrid[xa, ya].Possibles.Count; }
                        }
                    }
                    if (count <= leastcount)
                    {
                        if (startfrom > 0)
                        {
                            if (count >= startfrom) leastcount = count;
                        }
                        else
                        {
                            leastcount = count;
                        }

                    }
                    count = 0;//reset

                    if (possiblesum != 0) //avoid returning all-filled innerblock
                    {
                        KeyValuePair<Point, int> pipair = new KeyValuePair<Point, int>(new Point() { x = x, y = y }, leastcount);
                        KeyValuePair<KeyValuePair<Point, int>, int> pairintpair = new KeyValuePair<KeyValuePair<Point, int>, int>(pipair, possiblesum);
                        tracker.Add(pairintpair);
                    }
                    //Console.WriteLine("{0},{1}: {2}, {3}", x, y, leastcount, possiblesum);
                    possiblesum = 0;
                }
                starty = 0;//reset y value to head of line
            }
            least_count = leastcount;
            Point nextmin = (tracker.Count == 0) ? new Point()  : tracker.Reverse<KeyValuePair<KeyValuePair<Point, int>, int>>().Aggregate((p, n) => ((p.Key.Value < n.Key.Value) && (p.Value < n.Value)) ? p : n).Key.Key;
            return nextmin;
            //Point nextmin = tracker.Reverse<KeyValuePair<Point, int>>().Aggregate((p, n) => p.Value < n.Value ? p : n).Key;
            //Aggregate function searches all the way until last item, and we are looking for the first item that matches.
            //Therefore we need to reverse the list first so that the last item it gets is what we want
            //the formula: http://stackoverflow.com/questions/2805703/good-way-to-get-the-key-of-the-highest-value-of-a-dictionary-in-c-sharp

        }
        private Point GetNextLeastEmptyInInnerBlock(Point innerblock)
        {
            int xpos = GetInnerRange(innerblock.x);
            int ypos = GetInnerRange(innerblock.y);
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (TempGrid[xa, ya].Possibles.Count > 0) return new Point() { x = xa, y = ya };
                }
            }
            return new Point() { x = -1, y = -1 };
        }
        
        #endregion

        /// <summary>
        /// Checks if the filled grid is failure
        /// </summary>
        /// <returns>True if failed, False if succes</returns>
        private bool CheckFailure()
        {
            for (int x = 0; x < FullGridWidth; x += 4)
            {
                for (int y = 0; y < FullGridWidth; y += 4)
                {
                    int[] pos = FullInts.Except(GetInnerBlockRowImpossible(x, y)).ToArray();
                    if (pos.Length != 0) return true;
                }
            }
            return false;
        }
    }
    
}
