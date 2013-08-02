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
        /// Temporary grid used for holding possible values 
        /// サブ表
        /// </summary>
        public TempBlock[,] TempGrid { get; private set; }
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
        /// Debugger. Logs data into a txt file. Not used now 
        /// デバッガという名だけどただPointと値をtxtに書き出すだけのもの。使用しない
        /// </summary>
        //private Debug debug;
        #endregion
        
        public TreeDiagram(string[,] g, int sbw)
        {
            Grid = g; 
            SingleBlockWidth = sbw;
            Initialize();

            //Start from least unfilled block's least possibles
            //全ブロックから残りマスが一番少ない最初のブロックを選択し、そのブロック内の最初の一番残り可能性が少ないマスから始める
            Next = GetNextLeastEmptyInInnerBlock(GetLeastEmptyBlock(0, 0)); 
            //Next = GetNextEmpty();

            //if Next is {-1,-1}, return
            if (Next.x == -1)
            {
                FinishFlag = false; //if fail == true, finishflag means incomplete, 
                return; //return regardless of succeed or failure
            }
            //Preparation before recursion
            //ループ前に準備
            TempGrid[Next.x, Next.y].Possibles = GetPossible(Next.x, Next.y, true).ToList();
        }
        private void Initialize()
        {
            Logger = new List<Tuple<Point, int>>();

            //Fullints initialize
            FullInts = Enumerable.Range(1, FullGridWidth).ToArray();
            TempGrid = new TempBlock[FullGridWidth, FullGridWidth];
            //Tempgrid possible list
            //fill each tempgrid block with possible values
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    TempGrid[x, y] = new TempBlock(); //initialize
                    if (Grid[x, y].Equals("x")) TempGrid[x, y].Possibles.AddRange(GetPossible(x, y)); //add possible values
                }
            }
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
                    FinishFlag = false;
                    return; //return regardless of succeed or failure
                }
                TempGrid[Next.x, Next.y].Possibles = GetPossible(Next.x, Next.y, true).ToList();
            STEP1:
                if (TempGrid[Next.x, Next.y].Possibles.Count == 0)
                {
                    if (TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Possibles.Count == 0)
                    {
                        Logger.Remove(Logger.Last());
                        goto STEP1;
                    }
                    else
                    {
                        TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Possibles.Remove(Logger.Last().Item2);
                        Next = Logger.Last().Item1;//rollback one
                        Grid[Next.x, Next.y] = "x";
                        Logger.Remove(Logger.Last());
                        goto STEP1;
                    }
                }
                else
                {
                    Grid[Next.x, Next.y] = TempGrid[Next.x, Next.y].Possibles[0].ToString();
                    Logger.Add(new Tuple<Point, int>(Next, TempGrid[Next.x, Next.y].Possibles[0]));
                    Next = GetNextEmpty();
                    goto STEP0;
                }
            }
            catch (Exception)
            {
                FinishFlag = false;
                return;
            }
        }
        
        
        #endregion
        


        private bool CheckFinish()
        {
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    bool gridcheck = Grid[x, y].Equals("x");
                    bool tempcheck = TempGrid[x, y].Possibles.Count != 0;

                    if ((gridcheck || tempcheck) || (gridcheck && tempcheck)) return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Checks if the filled grid is failure
        /// </summary>
        /// <returns>True if failed, False if succes</returns>
        private bool CheckFailure()
        {
            for (int x = 0; x < FullGridWidth; x+=4)
            {
                for (int y = 0; y < FullGridWidth; y+=4)
                {
                    int[] pos = FullInts.Except(GetInnerBlockRowImpossible(x, y)).ToArray();
                    if (pos.Length!=0) return true;
                }
            }
            return false;
        }


        #region Impossibles
        public int[] GetInnerBlockRowImpossible(int x, int y, bool includeself = true)
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
        /// Gets all possible values
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int[] GetPossible(int x, int y, bool notincludeself = false)
        {
            List<int> impossiblevals = GetInnerBlock(x, y, notincludeself).Concat(GetRowImpossible(x, y)).ToList<int>();
            impossiblevals = impossiblevals.Distinct().ToList(); //remove duplicate
            return FullInts.Except(impossiblevals.ToArray()).ToArray<int>(); //returns possible values
        }

        public int GetInnerRange(int loc)
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
        public int[] GetInnerBlock(int x, int y, bool notincludeself = false)
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
        public int[] GetRowImpossible(int x, int y)
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
        #region Get
        private int PossibleCounter;
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
        private Point GetNextEmpty(int startx = 0, int starty = 0, bool tryloop = false)
        {
            for (int i = 0; i < ((tryloop) ? 3 : 1); i++)
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
        private int GetTotalLeftOver()
        {
            int track = 0;
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    if (Grid[x, y].Equals("x")) track++;
                }
            }
            return track;
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
            Point nextmin = (tracker.Count == 0) ? new Point() : tracker.Reverse<KeyValuePair<KeyValuePair<Point, int>, int>>().Aggregate((p, n) => ((p.Key.Value < n.Key.Value) && (p.Value < n.Value)) ? p : n).Key.Key;
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
    }
}
