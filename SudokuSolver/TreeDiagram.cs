using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SudokuSolver
{
    public class TreeDiagram
    {
        public string[,] Grid {get; private set;}
        public TempBlock[,] TempGrid {get; private set;}
        public TempBlock[,] ImpossibleGrid { get; private set; }//Throw in all impossible vals
        public string[,] mainbackupGrid { get; set; }
        private int[] SeparateLines = { 0, 3, 6 };
        private int[] FullInts = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
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
        private Point Next;
        private List<Tuple<Point, int, bool, bool>> Logger;
        private Debug debug;

        public TreeDiagram(string[,] g, int sbw)
        {
            debug = new Debug();
            Grid = g;
            SingleBlockWidth = sbw;
            Logger = new List<Tuple<Point, int, bool, bool>>();
            mainbackupGrid = Grid;
            Initialize();
        }
        private void Initialize()
        {
            //Fullints initialize
            List<int> ints = new List<int>();
            for (int i = 1; i <= FullGridWidth; i++)
            {
                ints.Add(i);
            }
            FullInts = ints.ToArray();
            ints.Clear();

            //Separate lines initialize
            for (int i = 0; i < FullGridWidth; i += SingleBlockWidth)
            {
                ints.Add(i);
            }
            SeparateLines = ints.ToArray();

            TempGrid = new TempBlock[FullGridWidth, FullGridWidth];
            ImpossibleGrid = new TempBlock[FullGridWidth, FullGridWidth]; 
            //Tempgrid possible list
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    TempGrid[x, y] = new TempBlock();
                    ImpossibleGrid[x, y] = new TempBlock();
                }
            }

            //fill each tempgrid block with possible values
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    if (Grid[x,y].Equals("x")) TempGrid[x, y].Possibles.AddRange(GetPossible(x, y)); //add possible values
                }
            }
            ImpossibleGrid = TempGrid;
        }
        /// <summary>
        /// Point - location
        /// int - value
        /// bool - CheckFinish()
        /// bool - Leftovercount == 0
        /// </summary>
        

        public void ExecuteTry()
        {
            string[,] backupGrid = Grid;
            TempBlock[,] backupTemp = TempGrid;

            TempBlock[,] copyTemp = TempGrid;
            int EmptyCount = GetTotalLeftOver();
            bool finish = CheckFinish();

            while (!finish)
            {
            STEP1:
                Next = GetNextEmpty();
            if (Next.x == -1)
            {
                bool fail = CheckFailure();
                if (fail)
                {
                    Grid = backupGrid;
                    Next = GetNextEmpty();
                }
                else return;
            }
                finish = CheckFinish();
            
        STEP2:
        if (!finish)
                {
                //true
                STEP23:
                    TempGrid[Next.x, Next.y].Possibles = GetPossible(Next.x, Next.y,true).ToList();
                STEP3:
                    PrintAll();
                    PrintAllTempStack();
                    Console.Write(Next.ToString()+ ": ");
                    TempGrid[Next.x, Next.y].Possibles.ToList().ForEach(a => Console.Write(a + " "));
                    Console.Write("-----------\n");
                    Logger.ForEach(a => debug.LoggerWrite(a.ToString()));
                    debug.LoggerWrite("----------");
                    if (TempGrid[Next.x, Next.y].Possibles.Count == 0)
                    {
                    //true  
                        if (TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Possibles.Count == 0)
                        {
                            Logger.RemoveAt(Logger.Count - 1);
                            goto STEP3;        
                        }
                        else
                        {
                            TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Possibles.Remove(Logger.Last().Item2);
                            Next = Logger.Last().Item1;//rollback one
                            Grid[Next.x, Next.y] = "x";
                            debug.Write(string.Format("Removed: {0}, {1}", Logger.Last().Item1, Logger.Last().Item2));
                            string debugout = String.Empty;
                            TempGrid[Next.x, Next.y].Possibles.ToList().ForEach(a=> debugout += (a.ToString()+ " "));
                            debug.Write(debugout);
                            Logger.RemoveAt(Logger.Count - 1);
                            goto STEP3;
                        }
                    //Next = GetNextEmpty();
                    
                    }
                    else
                    {
                        //false
                        Grid[Next.x, Next.y] = TempGrid[Next.x, Next.y].Possibles[0].ToString();
                        Logger.Add(new Tuple<Point, int, bool, bool>(Next, TempGrid[Next.x, Next.y].Possibles[0], CheckFinish(), GetTotalLeftOver() == 0));
                        debug.Write(string.Format("Logged: {0}, {1}", Logger.Last().Item1, Logger.Last().Item2));
                        Next = GetNextEmpty();
                        goto STEP2;

                    }
                }
                else
                {
                    return;
                }
            }
            


            //if (finish) return;
            
            //while (!finish)
            //{
            //    PrintAll();
            //    PrintAllTempStack();

            //STEP11:
            //    finish = CheckFinish();
            //    if (finish) return;
            //STEP2:
            //    Next = GetNextEmpty();//target;
            //if (Next.x == -1) return;
            //    if (!CheckFinish())
            //    {
            //    STEP3:
            //        if ((TempGrid[Next.x, Next.y].Possibles.Count == 0) && (Logger.Count != 0))
            //        {
            //        STEP5:
            //            if (TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Possibles.Count == 0)
            //            {
            //                TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Possibles.Add(Logger.Last().Item2); //add back
            //                Grid[Logger.Last().Item1.x, Logger.Last().Item1.y] = "x";
            //                Logger.RemoveAt(Logger.Count - 1);
                            
            //                goto STEP5;
            //            }
            //            else
            //            {
            //                TempGrid[Logger.Last().Item1.x, Logger.Last().Item1.y].Possibles.Remove(Logger.Last().Item2);
            //                Next = Logger.Last().Item1;
            //                goto STEP3;
            //            }
            //        }
            //        else
            //        {
            //            Grid[Next.x, Next.y] = TempGrid[Next.x, Next.y].Possibles[0].ToString();
            //            Logger.Add(new Tuple<Point, int, bool, bool>(Next, TempGrid[Next.x, Next.y].Possibles[0], CheckFinish(), GetTotalLeftOver() == 0));
            //            goto STEP11;   
            //        }
            //    }
            //    else return;
                
            //}
            //return;
        }
        private void FillCorrectTemp()
        {
            //foreach (Tuple<Point,int,bool,bool> item in Logger)
            //{
            //    TempGrid[item.Item1.x, item.Item1.y].Possibles
            //}
        }
        private void RemoveStartFailure()
        {
            Tuple<Point, int, bool, bool> first = Logger.FirstOrDefault();
            if (first == null) return;
            ImpossibleGrid[first.Item1.x, first.Item1.y].Possibles.Add(first.Item2);
            TempGrid[first.Item1.x, first.Item1.y].Possibles.Remove(first.Item2);
            RegenTempGrid();
            Grid = mainbackupGrid;
            PrintAll();
        }
        private void WithValueTry()
        {
            Next = new Point();
            
            while (Next.x != -1)
            {
                Next = GetNextEmpty(Next.x, Next.y, true);
                RemoveStartFailure();
                //if (TryFailure(Next)) { break; }
                if (TempGrid[Next.x, Next.y].Possibles.Count == 0) { RemoveStartFailure(); continue; }
                Grid[Next.x, Next.y] = TempGrid[Next.x, Next.y].Possibles[0].ToString();
                Logger.Add(new Tuple<Point, int, bool, bool>(Next, TempGrid[Next.x, Next.y].Possibles[0], CheckFinish(), GetTotalLeftOver() == 0));
                TempGrid[Next.x, Next.y].Possibles.RemoveAt(0);
                
                PrintAll();
            }
        }
        private bool TryFailure(Point pt)
        {
            return ((Grid[pt.x, pt.y].Equals("x")) && (TempGrid[pt.x, pt.y].Possibles.Count != 0));
        }

        public bool ExecuteTry2()
        {
            Basic();

            string[,] backupGrid = Grid; //for reverting back all
            string[,] loopGrid = Grid;  //for testing each
            int loopcount = 0;
            while (!CheckFinish())
            {
                Grid[Next.x, Next.y] = TempGrid[Next.x, Next.y].Possibles[0].ToString();
                
                if (CheckFinish())
                {
                    return true;
                }
                else
                {
                    ImpossibleGrid[Next.x, Next.y].Possibles.Add(TempGrid[Next.x, Next.y].Possibles[0]); //blacklist the item
                    Next = GetNextPossible(GetInnerRange(Next.x), GetInnerRange(Next.y));

                }

                if (loopcount >= 3)
                {
                    loopcount = 0;//reset

                }
                if (Grid == loopGrid) loopcount++;
                loopGrid = Grid;

            }


            int loop = 20;
            int trackloop = 1;
            int tracker = 0;
            int count = TempGrid[Next.x, Next.y].Possibles.Count;
            while (tracker < count)
	        {
                Grid[Next.x, Next.y] = TempGrid[Next.x, Next.y].Possibles[tracker].ToString();
                while (!CheckFinish())
                {
                    if (trackloop == loop) { break; }// for manual break
                    AdvancedFill();
                    CheckLeftOver2();
                    trackloop++;
                    Point nextpoint = GetNextLeastEmptyInInnerBlock(GetLeastEmptyBlock(Next.x, Next.y));
                    TreeDiagram td = new TreeDiagram(Grid,  SingleBlockWidth);
                    td.ExecuteTry();
                }
                tracker++;
                trackloop = 0;
                count = TempGrid[Next.x, Next.y].Possibles.Count;
	        }

            
            return (CheckFinish());
        }
        private void CheckLeftOver2()
        {
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    if (TempGrid[x, y].Possibles.Count == 1)
                    {
                        List<int> poss = TempGrid[x, y].Possibles;
                        Grid[x, y] = poss[0].ToString();
                        CleanTempGrid(x, y);
                    }
                }
            }
        }

        private void RegenTempGrid()
        {
            for (int x = 0; x < FullGridWidth; x++)
            {
                for (int y = 0; y < FullGridWidth; y++)
                {
                    if (Grid[x, y].Equals("x")) TempGrid[x, y].Possibles = GetPossible(x, y).ToList();
                }
            }
        }

        
        private void Basic()
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
        private void CleanTempGrid(int x, int y)
        {
            TempGrid[x, y].Possibles.Clear();
        }
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
                    if ((pos != null)||(pos.Length!=0)) return true;
                }
            }
            return false;
        }

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

            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
                {
                    if ((!includeself) && ((xa == x) && (ya == y))) continue; //skipself
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
            for (int xa = (xpos - SingleBlockWidth); xa < xpos; xa++)
            {
                for (int ya = (ypos - SingleBlockWidth); ya < ypos; ya++)
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

        public void FillTemp()
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
        private int PossibleCounter = 0;
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
        #endregion



        #region Printout
        private  void PrintHorizontalBorder(bool withnewline = false)
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
        private  void PrintAllTempStack()
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
                    Console.Write("{0}{1}", ((FullGridWidth > 9) && (count.ToString().Length == 1)) ? " " : string.Empty, count);

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
        private  void PrintAll()
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
        private  void PrintSpecific(int x, int y)
        {
            //Console.Clear();
            Console.WriteLine("Specific {0},{1}:", x, y);
            TempGrid[x, y].Possibles.ForEach(new Action<int>(a =>
            {
                Console.Write(a + " ");
            }));
        }
        private  void Print3x3(int x, int y)
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
       
    }
}
