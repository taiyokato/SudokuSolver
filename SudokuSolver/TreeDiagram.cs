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
        public int[,] Grid { get; private set; }
        /// <summary> 
        /// Finish flag 
        /// 終了フラグ。失敗時にfalseに切り替わる
        /// </summary>
        public bool FinishFlag { get; private set; }
        /// <summary> 
        /// Temporary grid used for holding possible values 
        /// サブ表
        /// </summary>
        public int[,][] TempGrid;
        protected int? singleblockw;
        /// <summary> 
        /// All possible values array used for filter out possible values
        /// 可能の値の配列。不可能値の排除に使う
        /// </summary>
        private int[] FullInts;
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
        private Stack<LogItem> Logger;
        /// <summary>
        /// A logger that writes Point + value to execute path txt
        /// </summary>
        //private Debug debugger;
        #endregion
        
        public TreeDiagram(int[,] g, int sbw, int[] fullints, int[,][] tempgrid)
        {
            Grid = g; 
            SingleBlockWidth = sbw;
            FullInts = fullints;
            TempGrid = tempgrid;
            
            Initialize();
            
            //debugger = new Debug();
            //Start from least unfilled block's least possibles
            //全ブロックから残りマスが一番少ない最初のブロックを選択し、そのブロック内の最初の一番残り可能性が少ないマスから始める
            Next = GetLeastEmptyBlock();//GetNextLeastEmptyInInnerBlock(); //GetLeastEmptyBlock();
            System.Diagnostics.Debug.WriteLine(Next);
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
            Logger = new Stack<LogItem>(); 
        }

        #region RecursiveTry
        /// <summary>
        /// Executes tree-search
        /// </summary>
        public void Execute2(ref int UnfilledCount)
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
                //if (TempGrid[Next.x, Next.y].Possibles.Length == 0)
                {
                    Point pt = Logger.Peek().Point;
                    if (TempGrid[pt.x,pt.y].Length == 0)
                    //if (TempGrid[Logger.Last().Point.x, Logger.Last().Point.y].Possibles.Length == 0)
                    {
                        Logger.Pop();
                        goto STEP1;
                    }
                    else
                    {
                        TempGrid[pt.x, pt.y] = PopFirst(TempGrid[pt.x, pt.y]).ToArray();
                        //TempGrid[pt.x,pt.y] = TempGrid[pt.x,pt.y].Skip(1).ToArray();
                        //TempGrid[Logger.Last().Point.x, Logger.Last().Point.y].Possibles.Remove(Logger.Last().Item2);
                        Next = pt;//Logger.Last().Point;//rollback one
                        Grid[Next.x, Next.y] = 0;
                        UnfilledCount++;
                        Logger.Pop();
                        goto STEP1;
                    }
                }
                else
                {
                    Grid[Next.x, Next.y] = TempGrid[Next.x, Next.y][0]; //[0].ToString();
                    UnfilledCount--;
                    //Grid[Next.x, Next.y] = TempGrid[Next.x, Next.y].Possibles[0].ToString();
                    Logger.Push(new LogItem(Next, TempGrid[Next.x, Next.y][0]));
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
                    int block = Grid[i, x];
                    Console.Write("{0}{1}", ((FullGridWidth > 9) && (block >= 10)) ? " " : string.Empty, block);

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
                    if (Grid[x, y]==0) return false;
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
                    if (Grid[xa, ya]!=0)
                    {
                        pos[pointer] = Grid[xa, ya];
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
                if (Grid[x, a]!=0)
                {
                    values[pointer++] = Grid[x, a];
                }
                //from y
                if (Grid[a, y]!=0)
                {
                    values[pointer++] = Grid[a, y];
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
                        if (Grid[x, y]==0) return new Point() { x = x, y = y };
                    }
                    starty = 0;
                }
                startx = 0;
            }
            return new Point() { x = -1, y = -1 };
        }
        private Point GetLeastEmptyBlock(int startx = -1, int starty = -1, int startfrom = -1)
        {
            //test code for GetLeastEmptyNext()
            Point ret = Point.NullObject, tmp = new Point();
            byte[] tracker = new byte[FullGridWidth];
            int xcur = (startx == -1) ? SingleBlockWidth : startx;
            int ycur = (starty == -1) ? SingleBlockWidth : starty;
            byte counter = 0;

            byte leastindex = 0;
            byte least = (byte)FullGridWidth;
            bool flag = false;
            for (int i = 0 ; i < FullGridWidth; i++)
            {
                
	            for (int x = (xcur - (SingleBlockWidth)); x < xcur; x++)
	            {
		            for (int y = (ycur - SingleBlockWidth); y < ycur; y++)
		            {
                        if (Grid[x, y] == 0)
                        {
                            counter++;
                            if (!flag)tmp = new Point(x, y);
                            flag = true;
                        }
		            }
	            }

                ycur += (byte)SingleBlockWidth;
                if (ycur >= (FullGridWidth + SingleBlockWidth))
                {
                    ycur = SingleBlockWidth;
                    xcur += SingleBlockWidth;
                }
                //i= 0->9

                //3x3 each
                
                //y+= 3

                //when y is 9, y is 0, x += 3
	            tracker[i] = counter;
	            if (tracker[i] < least)
	            {
		            least = tracker[i];
		            leastindex = (byte)i;
                    ret = tmp;
	            }
	            counter = 0;
                flag = false;
            }


            return ret;
            #region Previous
            /*
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
                            if (TempGrid[xa, ya].Length > 0) { count++; possiblesum += TempGrid[xa, ya].Length; }
                            if ((innerflag) && (Grid[xa, ya]==0)) { innerleast = new Point() { x = xa, y = ya }; innerflag = false; }
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
            Console.WriteLine(finalinnerleast);
            return finalinnerleast; //*/
            #endregion
            
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
                            if (TempGrid[xa, ya].Possibles.Length > 0) { count++; possiblesum += TempGrid[xa, ya].Possibles.Length; }
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
            Point nextmin = (tracker.Length == 0) ? new Point() : tracker.Reverse<KeyValuePair<KeyValuePair<Point, int>, int>>().Aggregate((p, n) => ((p.Key.Value < n.Key.Value) && (p.Value < n.Value)) ? p : n).Key.Key;
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
                    if (Grid[xa, ya].Equals("x")) return new Point() { x = xa, y = ya };
                    //if (TempGrid[xa, ya].Length > 0) return new Point() { x = xa, y = ya };
                }
            }
            return new Point() { x = -1, y = -1 };
        }
        #endregion
    }
}