using System;
using System.Collections.Generic;
using System.Linq;

namespace SudokuSolver
{
    public partial class TreeDiagram
    {
        #region def
        /// <summary> 
        /// Main grid
        /// メイン表
        /// </summary>
        public string[,] Grid { get; private set; }
        /// <summary> 
        /// Finish flag 
        /// 終了フラグ。失敗時にfalseに切り替わる
        /// </summary>
        public bool FinishFlag { get; private set; }
        /// <summary>
        /// Measure used when printing out grids
        /// 表のプリントアウト時に使う測り
        /// </summary>
        public int[] SeparateLines = { 0, 3, 6 };
        /// <summary> 
        /// Temporary grid used for holding possible values 
        /// サブ表
        /// </summary>
        public int[,][] TempGrid;
        //public TempBlock[,] TempGrid { get; private set; }
        protected int? singleblockw;
        /// <summary> 
        /// All possible values array used for filter out possible values
        /// 可能の値の配列。不可能値の排除に使う
        /// </summary>
        private int[] FullInts = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
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
        /// Holds the current location 
        /// 現在地を保持
        /// </summary>
        private Point Next;
        /// <summary> 
        /// Tracks the filled in location and value while backtrack solving 
        /// バックトラック時、巻き戻しできるように記録
        /// </summary>
        private List<Tuple<Point, int>> Logger;
        /// <summary>
        /// Tracks amount of unfilled blocks in the grid. Faster than looping to check unfilled block each time Next.x == -1 (probab)
        /// </summary>
        public int UnfilledCount;
        private Debug debugger;
        #endregion
        
        public TreeDiagram(string[,] g, int sbw, int[] fullints, int[,][] tempgrid, int unfilled)
        {
            Grid = g; 
            SingleBlockWidth = sbw;
            FullInts = fullints;
            TempGrid = tempgrid;
            UnfilledCount = unfilled;
             
            Initialize();
            
            //debugger = new Debug();
            //Start from least unfilled block's least possibles
            //全ブロックから残りマスが一番少ない最初のブロックを選択し、そのブロック内の最初の一番残り可能性が少ないマスから始める
            Next = GetNextLeastEmptyInInnerBlock(GetLeastEmptyBlock(0, 0)); //GetLeastEmptyBlock();
            //PossiblesGrid[Next.x, Next.y] = GetPossible(Next.x, Next.y, true);
            //Previous = Next;
            //if Next is {-1,-1}, return
            if (Next.x == -1)
            {
                FinishFlag = false; //if fail == true, finishflag means incomplete, 
                return; //return regardless of succeed or failure
            }
            //Preparation before recursion
            //ループ前に準備
            TempGrid[Next.x, Next.y] = GetPossible(Next.x, Next.y, true);

        }
        private void Initialize()
        {
            //GetFilledCount();
            Logger = new List<Tuple<Point, int>>();
            //FullInts = new int[FullGridWidth]; //alloc size
            //Fullints initialize
            //for (int i = 0; i < FullGridWidth; i++)
            //{
            //    FullInts[i] = i+1;
            //}
            //FullInts = Enumerable.Range(1, FullGridWidth).ToArray();
            //TempGrid = new TempBlock[FullGridWidth, FullGridWidth];
            //Tempgrid possible list
            //fill each tempgrid block with possible values
            //for (int x = 0; x < FullGridWidth; x++)
            //{
            //    for (int y = 0; y < FullGridWidth; y++)
            //    {
            //        TempGrid[x, y] = new TempBlock(); //initialize

            //        if (Grid[x, y].Equals("x")) TempGrid[x, y].Possibles = GetPossible(x, y).ToList<int>(); //this is faster
            //            //TempGrid[x, y].Possibles.AddRange(GetPossible(x, y)); //add possible values
            //    }
            //}
            
        }

        #region RecursiveTry
        /// <summary>
        /// Start backtrack try
        /// </summary>
        public void Execute()
        {
             
            try
            {
            STEP0:
                if (Next.x == -1) 
                {
                    bool finished = CheckFinish();
                    FinishFlag = finished;
                    debugger.Finish();
                    return; //return regardless of succeed or failure
                }
                TempGrid[Next.x, Next.y] = GetPossible(Next.x, Next.y, true);
                //TempGrid[Next.x, Next.y].Possibles = GetPossible(Next.x, Next.y, true).ToList();
            STEP1:
                //debugger.Write(Next.ToString());
                if (TempGrid[Next.x, Next.y].Length == 0)
                {
                    if (TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Length == 0)
                    {
                        Logger.Remove(Logger.Last());
                        goto STEP1;
                    }
                    else
                    {
                        TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y] = TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Skip(0).ToArray();
                        //TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Possibles.Remove(Logger.Last().Item2);
                        Next = Logger.Last().Item1;//rollback one
                        Grid[Next.x, Next.y] = "x";
                        Logger.Remove(Logger.Last());
                        goto STEP1;
                    }
                }
                else
                {
                    Grid[Next.x, Next.y] = TempGrid[Next.x, Next.y][0].ToString();
                    Logger.Add(new Tuple<Point, int>(Next, TempGrid[Next.x, Next.y][0]));
                    Next = GetNextEmpty();
                    goto STEP0;
                }
            }
            catch (Exception ex)
            {
                FinishFlag = false;
                debugger.Finish();
                return;
            }
        }
        public void Execute2()
        {
            try
            {
            STEP0:
                if (Next.x == -1)
                {
                    bool finished = (UnfilledCount == 0);//CheckFinish();
                    FinishFlag = finished;
                    //debugger.Finish();
                    return; //return regardless of succeed or failure
                }
                //PossiblesGrid[Next.x, Next.y] = GetPossible(Next.x, Next.y, true);
                TempGrid[Next.x, Next.y] = GetPossible(Next.x, Next.y, true);
            STEP1:
                //debugger.Write(Next.ToString());
                if (TempGrid[Next.x, Next.y].Length==0)
                //if (TempGrid[Next.x, Next.y].Possibles.Count == 0)
                {
                    Point pt = Logger.Last().Item1;
                    if (TempGrid[pt.x,pt.y].Length == 0)
                    //if (TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Possibles.Count == 0)
                    {
                        Logger.Remove(Logger.Last());
                        goto STEP1;
                    }
                    else
                    {
                        TempGrid[pt.x, pt.y] = PopFirst(TempGrid[pt.x, pt.y]).ToArray();
                        //TempGrid[pt.x,pt.y] = TempGrid[pt.x,pt.y].Skip(1).ToArray();
                        //TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Possibles.Remove(Logger.Last().Item2);
                        Next = pt;//Logger.Last().Item1;//rollback one
                        Grid[Next.x, Next.y] = "x";
                        UnfilledCount++;
                        Logger.Remove(Logger.Last());
                        goto STEP1;
                    }
                }
                else
                {
                    Grid[Next.x, Next.y] = TempGrid[Next.x, Next.y][0].ToString();
                    UnfilledCount--;
                    //Grid[Next.x, Next.y] = TempGrid[Next.x, Next.y].Possibles[0].ToString();
                    Logger.Add(new Tuple<Point, int>(Next, TempGrid[Next.x, Next.y][0]));
                    //Logger.Add(new Tuple<Point, int>(Next, TempGrid[Next.x, Next.y].Possibles[0]));
                    Next = GetNextEmpty(Next.x, Next.y, true);
                    goto STEP0;
                }
            }
            catch (Exception ex)
            {
                FinishFlag = false;
                //debugger.Finish();
                return;
            }
        }
        private IEnumerable<int> PopFirst(int[] l)
        {
            for (int i = 1; i < l.Length; i++)
            {
                yield return l[i];
            }
        }
        /// <summary>
        /// 使ってない
        /// Not using anymore
        /// </summary>
        private int[,][] PossiblesGrid;
        /// <summary>
        /// Stackoverflows
        /// </summary>
        /// <param name="failed"></param>
        /// <returns></returns>
        public bool Execute3(bool failed = true)
        {
            try
            {
                if (Next.x == -1)
                {
                    bool finished = (UnfilledCount == 0);
                    FinishFlag = finished;
                    return finished; //true if all filled, false else
                }
                if (!failed) PossiblesGrid[Next.x, Next.y] = GetPossible(Next.x, Next.y, true);
                if (PossiblesGrid[Next.x, Next.y].Length == 0)
                //if (TempGrid[Next.x, Next.y].Possibles.Count == 0)
                {
                    if (PossiblesGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Length == 0)
                    //if (TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Possibles.Count == 0)
                    {
                        Logger.Remove(Logger.Last());
                        Next = Logger.Last().Item1;//rollback one
                        //Console.Clear();
                        //PrintAll();
                        return Execute3(true);
                    }
                    else
                    {
                        PossiblesGrid[Logger.Last().Item1.x, Logger.Last().Item1.y] = PossiblesGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Skip(1).ToArray();
                        //TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Possibles.Remove(Logger.Last().Item2);
                        Grid[Next.x, Next.y] = "x";
                        Next = Logger.Last().Item1;//rollback one
                        UnfilledCount++;
                        Logger.Remove(Logger.Last());
                        //PossiblesGrid[Next.x, Next.y] = GetPossible(Next.x, Next.y, true);
                        //Console.Clear();
                        //PrintAll();
                        return Execute3(true);
                    }
                }
                else
                {
                    Grid[Next.x, Next.y] = PossiblesGrid[Next.x, Next.y][0].ToString();
                    UnfilledCount--;
                    //Grid[Next.x, Next.y] = TempGrid[Next.x, Next.y].Possibles[0].ToString();
                    Logger.Add(new Tuple<Point, int>(Next, PossiblesGrid[Next.x, Next.y][0]));
                    //Logger.Add(new Tuple<Point, int>(Next, TempGrid[Next.x, Next.y].Possibles[0]));
                    Next = GetNextEmpty();
                }
                //Console.Clear();
                //PrintAll();
                
                return Execute3(false);
            }
            catch (Exception ex)
            {
                
                //Console.WriteLine(ex.Message);
                Console.Read();
            }
            return true;
        }
        /// <summary>
        /// Slow.
        /// </summary>
        public void Execute4()
		{
			bool getnewpos = true;
			while (true)
			{
				if (Next.x == -1)
				{
					bool finished = (UnfilledCount == 0);
					FinishFlag = finished;
                    break;		
				}
				if (getnewpos) PossiblesGrid[Next.x, Next.y] = GetPossible(Next.x, Next.y, true);
				if (PossiblesGrid[Next.x, Next.y].Length==0)
				{
					if (PossiblesGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Length == 0)
					{
						Logger.Remove(Logger.Last());
                        getnewpos = false;
						continue;
					}
					else
					{
						PossiblesGrid[Logger.Last().Item1.x, Logger.Last().Item1.y] = PossiblesGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Skip(1).ToArray();
						Next = Logger.Last().Item1;
						Grid[Next.x, Next.y] = "x";
						UnfilledCount++;
						Logger.Remove(Logger.Last());
                        getnewpos = false;
						continue;
					}
				}
                else
                {
                    Grid[Next.x, Next.y] = PossiblesGrid[Next.x, Next.y][0].ToString();
                    UnfilledCount--;
                    //Grid[Next.x, Next.y] = TempGrid[Next.x, Next.y].Possibles[0].ToString();
                    Logger.Add(new Tuple<Point, int>(Next, PossiblesGrid[Next.x, Next.y][0]));
                    //Logger.Add(new Tuple<Point, int>(Next, TempGrid[Next.x, Next.y].Possibles[0]));
                    Next = GetNextEmpty();
                    getnewpos = true;
                }
			}
            //return;
		}
		private void PrintHorizontalBorder(bool withnewline = false)
        {
            string outstr = string.Empty;

            // | + 532... + x x x
            int vertical = 1 + SingleBlockWidth + (SingleBlockWidth - 1);
            if ((FullGridWidth > 9)) vertical += (FullGridWidth / 4);
            //if grid is greater than 9x9, it means that possible max value is 2-digit, meaning requires double space
            //for example: 16x16 grid. each block has 4x4 values, each 2-digit. 
            //because of the 2-digit, it means we need to add double of what we have now.
            //since each loop adds in 2-values, "- ", it means we don't actually need *2
            //we only need 1/4 of the FullGridWidth

            int vertical_border = SingleBlockWidth + 1;
            int total = FullGridWidth + vertical_border;

            for (int i = 0; i < SingleBlockWidth; i++)
            {
                for (int a = 0; a < (vertical / 2); a++)
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
        #endregion
        

        /// <summary>
        /// DEPRECIATED. Checks for "x" each time. Use GetFilledCount at before looping. 
        /// </summary>
        /// <returns></returns>
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
        
        #region Impossibles
        /// <summary>
        /// Gets all possible values
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int[] GetPossible(int x, int y, bool notincludeself = false)
        {
            IEnumerable<int> impossiblevals = GetInnerBlock(x, y, notincludeself).Concat(GetRowImpossible(x, y));
            return FullInts.Except(impossiblevals).ToArray();
            //HashSet<int> impossiblevals = new HashSet<int>(GetInnerBlock(x, y, notincludeself).Concat(GetRowImpossible(x, y)));
            //return FullInts.Except(impossiblevals).ToArray(); //FullInts.Except(impossiblevals.Distinct()).ToArray(); //returns possible values
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
            int[] values = new int[FullGridWidth*2];
            int pointer = 0;
            
            
            for (int a = 0; a < FullGridWidth; a++)
            {
                //from x
                if (!Grid[x, a].Equals("x"))
                {
                    values[pointer++] = int.Parse(Grid[x, a]);
                }
                //from y
                if (!Grid[a, y].Equals("x"))
                {
                    values[pointer++] = int.Parse(Grid[a, y]);                    
                }
            }
            //int[] ret = values.Distinct().Select(int.Parse).ToArray(); //remove duplicates and return IMPOSSIBLE values
            return values.Distinct().ToArray();
        }
        #endregion
        #region Get
        private Point GetNextEmpty(int startx = 0, int starty = 0, bool tryloop = false)
        {
            for (int i = 0; i < ((tryloop) ? 2 : 1); i++)
            {
                for (int x = startx; x < FullGridWidth; x++)
                {
                    for (int y = starty; y < FullGridWidth; y++)
                    {
                        if (Grid[x, y].Equals("x")) return new Point() { x = x, y = y };
                    }
                    starty = 0;
                }
                startx = 0;
            }
            return new Point() { x = -1, y = -1 };
        }
        private Point GetLeastEmptyBlock(int startx = 0, int starty = 0, int startfrom = -1)
        {
            int least_poscount = FullGridWidth;
            //Point least_loc = new Point();// { x = -1, y = -1 };
            Point innerleast = new Point();
            Point finalinnerleast = new Point();
            int xpos, ypos, count;
            int possiblesum = (FullGridWidth*FullGridWidth);
            int prev_possiblesum = (FullGridWidth * FullGridWidth);
            bool innerflag = true;

            for (int x = startx; x < FullGridWidth; x += SingleBlockWidth)
            {
                for (int y = starty; y < FullGridWidth; y += SingleBlockWidth)
                {
                    xpos = (x + SingleBlockWidth);
                    ypos = (y + SingleBlockWidth);
                    count = 0;
                    possiblesum = 0;
                    for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
                    {
                        for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                        {
                            if (TempGrid[xa, ya].Length> 0) { count++; possiblesum += TempGrid[xa, ya].Length; }
                            if ((innerflag) && (Grid[xa, ya].Equals("x"))) { innerleast = new Point() { x = xa, y = ya }; innerflag = false; }
                        }
                    }

                    innerflag = true;
                    if (prev_possiblesum < possiblesum) continue;
                    if (least_poscount > count)
                    {
                        prev_possiblesum = possiblesum;
                        least_poscount = count;
                        finalinnerleast = innerleast;
                    }                    
                }
            }
            return finalinnerleast;
            #region Previous
            //Console.WriteLine(DateTime.Now.Ticks - start);

            /*
            //This one takes 10000 ticks
            start = DateTime.Now.Ticks;
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
            Console.WriteLine(DateTime.Now.Ticks - start);
            least_count = leastcount;
            Point nextmin = (tracker.Count == 0) ? new Point() : tracker.Reverse<KeyValuePair<KeyValuePair<Point, int>, int>>().Aggregate((p, n) => ((p.Key.Value < n.Key.Value) && (p.Value < n.Value)) ? p : n).Key.Key;
            return nextmin; //*/
            //Point nextmin = tracker.Reverse<KeyValuePair<Point, int>>().Aggregate((p, n) => p.Value < n.Value ? p : n).Key;
            //Aggregate function searches all the way until last item, and we are looking for the first item that matches.
            //Therefore we need to reverse the list first so that the last item it gets is what we want
            //the formula: http://stackoverflow.com/questions/2805703/good-way-to-get-the-key-of-the-highest-value-of-a-dictionary-in-c-sharp
            #endregion
        }
        /// <summary>
        /// DEPRECITATED. Function implemented in GetLeastEmptyBlock();
        /// </summary>
        /// <param name="innerblock"></param>
        /// <returns></returns>
        private Point GetNextLeastEmptyInInnerBlock(Point innerblock)
        {
            int xpos = GetInnerRange(innerblock.x);
            int ypos = GetInnerRange(innerblock.y);
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (TempGrid[xa, ya].Length > 0) return new Point() { x = xa, y = ya };
                }
            }
            return new Point() { x = -1, y = -1 };
        }
        #endregion
    }
}