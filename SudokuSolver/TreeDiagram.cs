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
        public int[][] Grid { get; private set; }
        /// <summary> 
        /// Finish flag 
        /// 終了フラグ。失敗時にfalseに切り替わる
        /// </summary>
        public bool FinishFlag { get; private set; }
        /// <summary> 
        /// Temporary grid used for holding possible values 
        /// サブ表
        /// </summary>
        public int[][][] TempGrid;
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
        /// Holds the nextrent location 
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

        public Point EntryPoint;
        public TreeDiagram(int[][] g, int sbw, int[] fullints, int[][][] tempgrid)
        {
            Grid = g;
            SingleBlockWidth = sbw;
            FullInts = fullints;
            TempGrid = tempgrid;

            Initialize();
            //PrintTempGridCount();

            //debugger = new Debug();
            //Start from least unfilled block's least possibles
            //全ブロックから残りマスが一番少ない最初のブロックを選択し、そのブロック内の最初の一番残り可能性が少ないマスから始める
            //EntryPoint = Next = GetLeastEmptyBlock();//GetNextLeastEmptyInInnerBlock(); //GetLeastEmptyBlock();
            EntryPoint = Next = LeastShareBlock();

            //System.Diagnostics.Debug.WriteLine(Next);
            if (Next.x == -1)
            {
                FinishFlag = false; //if fail == true, finishflag means incomplete, 
                return; //return regardless of succeed or failure
            }
            //Preparation before renextsion
            //ループ前に準備
            TempGrid[Next.x][Next.y] = GetPossible(Next.x, Next.y, true);

        }
        private void Initialize()
        {
            Logger = new Stack<LogItem>();
        }

        public int UnfilledCount;
        
        public bool Execute5(Point next)
        {
            if (FinishFlag) return true;
            //PrintAll();
            
            HEAD:
            if (next ==Point.Null)
            {
                if (UnfilledCount == 0)
                {
                    return (FinishFlag = true);
                }
                return false;
            }
            TempGrid[next.x][next.y] = GetPossible(next.x, next.y, true);
            {

            RETURN:
                //PrintAll();
                if (TempGrid[next.x][next.y].Length == 0)
                { 
                    return false;
                }
                Grid[next.x][next.y] = TempGrid[next.x][next.y][0];
                //TempGrid[next.x, next.y] = PopFirst(TempGrid[next.x, next.y]);
                UnfilledCount--;
                Point n2 = GetNextEmpty(next.x,next.y,true);
                bool res = Execute5(n2);
                if (res)
                {
                    if (UnfilledCount == 0)
                    {
                        FinishFlag = true;
                        return true;
                    }
                    next = n2;
                    goto HEAD; 
                }
                else
                {
                    Grid[next.x][next.y] = 0;
                    TempGrid[next.x][next.y] = PopFirst(TempGrid[next.x][next.y]);
                    UnfilledCount++;
                    //return true;
                    goto RETURN;
                }
            }

        }


        #region RecursiveTry
        /// <summary>
        /// Executes tree-search
        /// </summary>
        public void Execute2(ref int UnfilledCount)
        {
            ulong count = 0;
            try
            {
            STEP0:
                if (Next.x == -1)
                {
                    FinishFlag = (UnfilledCount == 0);//CheckFinish();

                    //debugger.Finish();
                    System.Diagnostics.Debug.WriteLine("Loop:" + count);
                    return; //return regardless of succeed or failure
                }
                //PossiblesGrid[Next.x, Next.y] = GetPossible(Next.x, Next.y, true);
                TempGrid[Next.x][Next.y] = GetPossible(Next.x, Next.y, true);



            STEP1:
                count++;
                //debugger.Write(Next.ToString());
                if (TempGrid[Next.x][Next.y].Length == 0)
                //if (TempGrid[Next.x, Next.y].Possibles.Length == 0)
                {
                    Next = Logger.Peek().Point;
                    TempGrid[Next.x][Next.y] = PopFirst(TempGrid[Next.x][Next.y]);
                    //TempGrid[pt.x,pt.y] = TempGrid[pt.x,pt.y].Skip(1).ToArray();
                    //TempGrid[Logger.Last().Point.x, Logger.Last().Point.y].Possibles.Remove(Logger.Last().Item2);
                    //Next = pt;//Logger.Last().Point;//rollback one
                    Grid[Next.x][Next.y] = 0;
                    UnfilledCount++;
                    Logger.Pop();
                    goto STEP1;

                    /*
                    //Point pt = Logger.Peek().Point;
                    if (TempGrid[pt.x][pt.y].Length == 0)
                    //if (TempGrid[Logger.Last().Point.x][Logger.Last().Point.y].Possibles.Length == 0)
                    {
                        Logger.Pop();
                        goto STEP1;
                    }
                    else
                    {
                        
                    }//*/
                }
                else
                {
                    Grid[Next.x][Next.y] = TempGrid[Next.x][Next.y][0]; //[0].ToString();
                    UnfilledCount--;
                    //Grid[Next.x][Next.y] = TempGrid[Next.x][Next.y].Possibles[0].ToString();
                    Logger.Push(new LogItem(Next, TempGrid[Next.x][Next.y][0]));
                    //Logger.Add(new Tuple<Point][int>(Next][TempGrid[Next.x][Next.y].Possibles[0]));
                    Next = GetNextEmpty(Next.x, Next.y, true);
                    goto STEP0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                FinishFlag = false;
                //debugger.Finish();
                return;
            }
        }
        #region 後始末
        private void CleanTempGrid(int x, int y)
        {
            TempGrid[x][y] = new int[0];
            //TempGrid[x, y].Possibles.Clear();
        }
        private void ClearRelativeTemp(int x, int y, int value)
        {
            //cleanup
            foreach (Point p in GetHorizontalEmpty(x, y, 0, true))
            {
                RemoveItemFromTemp(value, ref  TempGrid[p.x][p.y]);
            }
            foreach (Point p in GetVerticalEmpty(x, y, 0, false))
            {
                RemoveItemFromTemp(value, ref TempGrid[p.x][p.y]);
            }
            foreach (Point p in GetInnerEmpty(x, y, false))
            {
                RemoveItemFromTemp(value, ref TempGrid[p.x][p.y]);
            }
            CleanTempGrid(x, y);
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
            Solver.TrimEndPt(ref pt);
            //return pt.ToArray();
            return pt;//pt.ToArray();
        }

        #endregion

        private int[] PopFirst(int[] l)
        {

            int[] ret = new int[l.Length - 1];
            for (int i = 1; i < l.Length; i++)
            {
                ret[i - 1] = l[i];
            }
            return ret;
        }
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
        #endregion

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
                    if (Grid[x][y] == 0) return false;
                }
            }
            return true;
        }

        #region Impossibles
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
        /// <summary>
        /// Gets all possible values
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int[] GetPossible(int x, int y, bool notincludeself = false)
        {
            var fe = FullInts.Except(GetFilledInner(x, y, notincludeself).Concat(GetRowImpossible(x, y))).ToArray(); //returns possible values
            return fe;
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


            for (int a = 0; a < FullGridWidth; a++)
            {
                //from x
                if (Grid[x][a] != 0)
                {
                    values[pointer++] = Grid[x][a];
                }
                //from y
                if (Grid[a][y] != 0)
                {
                    values[pointer++] = Grid[a][y];
                }
            }
            //int[] ret = values.Distinct().Select(int.Parse).ToArray(); //remove duplicates and return IMPOSSIBLE values
            return values.Distinct().ToArray();
        }
        #endregion
        #region Get
        private Point GetNextEmpty(int startx = 0, int starty = 0, bool tryloop = false)
        {
            int xa = startx, ya = starty;
            for (int i = 0; i < ((tryloop) ? 2 : 1); i++)
            {
                for (int x = xa; x < FullGridWidth; x++)
                {
                    for (int y = ya; y < FullGridWidth; y++)
                    {
                        if (Grid[x][y] == 0) return new Point(x, y);
                    }
                    ya = 0;
                }
                xa = 0;
            }
            return Point.Null;
        }

        #region GetLeast Empty Block
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
        #endregion

        private Point GetLeastEmptyBlock(int startx = -1, int starty = -1, int startfrom = -1)
        {
            Point ret = Point.Null; //start from null
            int max = FullGridWidth; //max is all empty
            for (int a = 0; a < FullGridWidth; a += SingleBlockWidth)
            {
                for (int b = 0; b < FullGridWidth; b += SingleBlockWidth)
                {
                    int tmp = CountInnerBlockEmpty(a, b);
                    if (tmp == 0) continue; //skip empty ones!
                    if (tmp < max)
                    {
                        max = tmp;
                        //dont use ret = new Point(a,b). takes more time initializing new object

                        ret.x = a;
                        ret.y = b;
                    }
                    //max = (tmp < max) ? tmp : max;

                }
            }

            return GetInnerEmpty(ret.x, ret.y, false)[0]; //grab the point at last. dont use this method while looping to reduce time
            //return ret;



            #region Previous
            /*

            //test code for GetLeastEmptyNext()
            Point ret = Point.NullObject, tmp = new Point();
            byte[] tracker = new byte[FullGridWidth];
            int xnext = (startx == -1) ? SingleBlockWidth : startx;
            int ynext = (starty == -1) ? SingleBlockWidth : starty;
            byte counter = 0;

            byte leastindex = 0;
            byte least = (byte)FullGridWidth;
            bool flag = false;
            for (int i = 0; i < FullGridWidth; i++)
            {

                for (int x = (xnext - (SingleBlockWidth)); x < xnext; x++)
                {
                    for (int y = (ynext - SingleBlockWidth); y < ynext; y++)
                    {
                        if (Grid[x, y] == 0)
                        {
                            counter++;
                            if (!flag) tmp = new Point(x, y);
                            flag = true;
                        }
                    }
                }

                ynext += (byte)SingleBlockWidth;
                if (ynext >= (FullGridWidth + SingleBlockWidth))
                {
                    ynext = SingleBlockWidth;
                    xnext += SingleBlockWidth;
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
            //*/
            #endregion

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

        #endregion


        #region Experimental
        private Point[] GetSpecificPossibleLocationsInBlock(int num, Point loc)
        {
            int xpos = GetInnerRange(loc.x);
            int ypos = GetInnerRange(loc.y);
            int t = 0;
            Point[] locs = new Point[FullGridWidth];
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (TempGrid[xa][ya].Contains(num)) locs[t++] = (new Point(xa, ya));
                }
            }
            return locs.ToArray();
        }
        
        public Point LeastShareBlock()
        {
            List<sLeastShare> least = new List<sLeastShare>();
            //=========
            
            for (int xa = 0; xa < FullGridWidth; xa += SingleBlockWidth)
            {
                for (int ya = 0; ya < FullGridWidth; ya += SingleBlockWidth)
                {

                    Point[] empty = GetInnerEmpty(xa, ya, false);

                    List<sLeastShare> track = new List<sLeastShare>();

                    //make new list according to each empty's possibles length where count must be greater than 1
                    //sort the list by length
                    var a =
                         from word in empty
                         group word by TempGrid[word.x][word.y].Length into g
                         where g.Count() > 1
                         orderby g.Count(), g.Key ascending
                         select new { g.Key, Count = g.Count(), g };
                    var d = a.ToArray();
                     

                    least.Add(new sLeastShare(GetSpecificPossibleTempCount(d[0].Key, new Point(xa,ya))[0],d[0].Key));


                    
                    //ypos += SingleBlockWidth;
            
                }
            }

            var sorted = least.OrderBy(a => a.val).ToArray();
            return sorted[0].p; ;
        }
        public Point[] GetSpecificPossibleTempCount(int len, Point loc)
        {
            int xpos = GetInnerRange(loc.x);
            int ypos = GetInnerRange(loc.y);
            int t = 0;
            Point[] locs = new Point[FullGridWidth];
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if (TempGrid[xa][ya].Length == len) locs[t++] = (new Point(xa, ya));
                }
            }
            return locs.ToArray();
        }
        #endregion
    }
    public struct sLeastShare
    {
        public Point p { get; set; }
        public int val { get; set; }
        public sLeastShare(Point P, int V)
            :this()
        {
            p = P;
            val = V;
        }
    }
}