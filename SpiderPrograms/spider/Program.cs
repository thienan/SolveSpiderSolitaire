﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;

// copyright 2009..2018 Joseph "Beemer Biker" Stateson   All Rights Reserved   


namespace spider
{


    public class Program
    {
        static int RunAway = 100;
        private static bool keepRunning = true;
#if DEBUG
        static int XMLtoRead = 0;   // can be 0..6   if == 0 then ".xml" is read else .xml1 thru .xml6
#else
        static int XMLtoRead = 0;   
#endif
        // if == 0 then the board  in "newxml" is read in place of InitialBoard

        static cDisassemble DisAssemble;
        static cSpinAllMoves MakeAllPossibleMoves;
        static cStrategy Strategy;
        static cReduce Reduce;
        static csuitable Suitable;
        
        static cTest Test;

        static void ClearDir(string strDir)
        {
            foreach (string sFile in Directory.GetFiles(strDir, "DEAL*.*"))
            {
                File.Delete(sFile);
            }
            foreach (string sFile in Directory.GetFiles(strDir, "SUIT_*.*"))
            {
                File.Delete(sFile);
            }
            foreach (string sFile in Directory.GetFiles(strDir, "*.mov*"))
            {
                File.Delete(sFile);
            }
        }



        static void Main(string[] args)
        {
            bool bIsThere = false;
            string strSpiderBin0;
            string PathToDirectory;

            // KnownFolderFinder.EnumerateKnownFolders();   // this was used for testing purposes
            // GlobalClass.InitExceptions();
            // look for file in current director first and use that one - 5/2018

            string stTemp = System.Reflection.Assembly.GetEntryAssembly().Location; // path to executable
            strSpiderBin0 = System.IO.Path.GetDirectoryName(stTemp) + "\\Spider Solitaire.SpiderSolitaireSave-ms";
            bIsThere = File.Exists(strSpiderBin0);

            if (!bIsThere)
            {

                //Attempt to find where the saved spider file is located.  Normally at c:\user\username but the windows MOVE property might
                //have been used to relocat the file.  In addition,  OneDrive may or may not be in the path returned by SpecialFolder

                strSpiderBin0 = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                // the above is usually user\stateson\documents
                GlobalClass.strSpiderBin = strSpiderBin0.Replace("Documents", "Saved Games\\Microsoft Games\\Spider Solitaire\\Spider Solitaire.SpiderSolitaireSave-ms");
                bIsThere = File.Exists(GlobalClass.strSpiderBin);
                if (!bIsThere)
                {
                    // user has made it hard to find, try looking in the registry in case the windows 10 "MOVE" relocated Documents.
                    // we need to find the original Documents location, not the new one
                    Console.WriteLine("could not find " + GlobalClass.strSpiderBin + "\n");
                    strSpiderBin0 = KnownFolderFinder.GetFolderFromKnownFolderGUID(new Guid("{FDD39AD0-238F-46AF-ADB4-6C85480369C7}"));
                    GlobalClass.strSpiderBin = strSpiderBin0.Replace("Documents", "Saved Games\\Microsoft Games\\Spider Solitaire\\Spider Solitaire.SpiderSolitaireSave-ms");
                    bIsThere = File.Exists(GlobalClass.strSpiderBin);
                    if (!bIsThere)
                    {
                        Console.WriteLine("nothing here also " + GlobalClass.strSpiderBin + "\n");
                        Console.WriteLine("Trying local directory where this program resides\n");
                        strSpiderBin0 = System.Reflection.Assembly.GetEntryAssembly().Location;
                        strSpiderBin0 = System.IO.Path.GetDirectoryName(strSpiderBin0);
                        Console.WriteLine("Looking here: " + strSpiderBin0 + "\n");
                        strSpiderBin0 += "\\Spider Solitaire.SpiderSolitaireSave-ms";
                        if (!File.Exists(strSpiderBin0))
                        {
                            Console.WriteLine("Giving up, cannot find " + strSpiderBin0);
                            Environment.Exit(0);
                        }
                        else GlobalClass.strSpiderBin = strSpiderBin0;
                    }
                }
            }
            else GlobalClass.strSpiderBin = strSpiderBin0;
            PathToDirectory = Path.GetDirectoryName(GlobalClass.strSpiderBin);
            ClearDir(PathToDirectory);  // erases files with pattern DEAL*, SUIT*, *.mov* and adeck and those xml ones also
            cSpinControl cSC = new cSpinControl();
            DisAssemble = new cDisassemble(ref cSC);
            Suitable = new csuitable(ref cSC, ref DisAssemble);
            Strategy = new cStrategy(ref cSC, ref DisAssemble);
            Reduce = new cReduce(ref cSC, ref DisAssemble, ref Strategy);
            cSC.Strategy = Strategy;
            cSC.Suitable = Suitable;
            cSC.JoinSuited = new cJoinSuited(ref cSC);

            Test = new cTest(ref cSC, ref Strategy);

//            Test.RunTest();

           // http://stackoverflow.com/questions/177856/how-do-i-trap-ctrl-c-in-a-c-sharp-console-apphttp://stackoverflow.com/questions/177856/how-do-i-trap-ctrl-c-in-a-c-sharp-console-app
            try
            {
                SolveBoard(ref cSC, GlobalClass.strSpiderBin);
            }
            catch (Exception e)
            {
                if(e.Message != "Spider")
                {
                    throw e;
                }
                //GlobalClass.eExceptionType eID = (GlobalClass.eExceptionType) e.Data["ID"];
                string msg = (string) e.Data["MSG"];
                Console.WriteLine(msg);
                Environment.Exit(0);
            }

        }

        static void CallExit()
        {
            Debug.Assert(false);
            Environment.Exit(0);
        }


        static void SolveBoard(ref cSpinControl cSC, string strSpiderBin)
        {
            int m = GlobalClass.MAX_FILTERED_BOARDS;
            GlobalClass.StrategyType gcst = GlobalClass.StrategyType.CONTINUE_SPINNING;
            board InitialBoard = new board();
            cSC.Deck = new cBuildDeck(strSpiderBin, XMLtoRead, ref cSC);
            MakeAllPossibleMoves = new cSpinAllMoves(ref cSC);
            cSC.Deck.GetBoardFromSpiderSave(ref InitialBoard);
            InitialBoard.ShowBoard();

            cSC.cMXF = new cMergeXmlFile();

            if (cSC.bJustReadXML)
            {
                Console.WriteLine(" Did you mean to read a pre-existing XML file?\n - close window to abort\n - or press N to delete the XML file and abort\n - or press Y to continue");
                if (cSC.EventList.Count > 0)
                {
                    Console.WriteLine(" There are " + cSC.EventList.Count + " events in the event list");
                    Console.WriteLine(cSC.Deck.strEventInfoPrompt);
                }
                ConsoleKeyInfo cki = Console.ReadKey();
                Console.WriteLine("");
                if (cki.Key.ToString().ToLower() == "n")
                {
                    File.Delete(cSC.XML_Diag_filename);
                    Environment.Exit(0);
                }
                else
                {
                    string strA = cki.KeyChar.ToString();
                    int A = -1;
                    int.TryParse(strA, out A);
                    
                    if (A >= 1 && A < 7)
                    {
                        int RequiredOffset = 0;
                        int RequiredDeal = A;
                        string strCmd = Console.ReadLine().ToLower();
                        strA = strCmd.Substring(0, 1);
                        InitialBoard = null;
                        if (strA == "m")  //Format is 2m34 or 3M45
                        {
                            strCmd = strCmd.Substring(1);
                            RequiredOffset = Convert.ToInt32(strCmd);
                            bool bResult = cSC.Deck.AdvanceToPosition(RequiredDeal, RequiredOffset, out InitialBoard);
                            CallExit();
                        }
                        else
                        {
                            if (strA == "s") // format is 2s 3S , etc ..   show all best scores
                            {
                                bool bResult = cSC.Deck.ShowAllBestScores(RequiredDeal, out InitialBoard);
                                CallExit();
                            }
                            else if (strA == "t") // format is 2t45 3t9 , etc .. trace using stlookup 
                            {       // deals 2, 3, and set breakpoint in debugger at the callout for the 45, 9, etc
                                RequiredOffset = Convert.ToInt32(strCmd.Substring(1));
                                bool bResult = cSC.Deck.AddBoardsToLookup(RequiredDeal, RequiredOffset);
                                CallExit();
                            }
                            else
                            {
                                Console.WriteLine("Unknown key code.  One of M,S,m,s only");
                                CallExit();
                            }
                        }
                    }
                    else
                    {
                        bool bResult = cSC.Deck.AdvanceSaveBoard(cki.Key.ToString().ToLower(), out InitialBoard);
                        Debug.Assert(false);
                    }
                }
            }

            

            //Suitable.CheckSuitability(ref InitialBoard);
            // the above is not critical to the program as yet, just good info about unexposed cards

            //cSC.Suitable.CombineLikeSuits(ref InitialBoard);

            MakeAllPossibleMoves.AddInitialBoard(ref InitialBoard, GlobalClass.FIRST_CARD, ref Suitable);

            
            //cSC.JoinSuited.SpinSuitedJoinables(ref cSC.ThisBoardSeries);


            //if (InitialBoard.bIsCompletable)                PerformReduceSuits(ref cSC, 20);

            //Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
            //{
            //    e.Cancel = true;
            //    Program.keepRunning = false;
            //};

            while (true) // (Program.keepRunning)
            {
                RunAway--;
                if (RunAway < 0)
                {
                    Console.WriteLine("hit any key except x: TBS:" + cSC.ThisBoardSeries.Count + "  NBS:" + cSC.NextBoardSeries.Count);
                    ConsoleKeyInfo cki = Console.ReadKey();
                    if (cki.KeyChar == 'x')
                        Environment.Exit(0);

                }

                bool b1 = MakeAllPossibleMoves.RunFilter(m);    // m is max to allow after applying filter

                bool bSameSeed = cSC.PrevSeed.bSameSeed(ref cSC.ThisSeed);
                if (cSC.bOutOfSpaceInSTLOOKUP)
                {
                    Console.WriteLine("ran out of space and had to clear stlookup");
                    cSC.stlookup.Clear();
                    cSC.bOutOfSpaceInSTLOOKUP = false;
                }
                if (cSC.bSpinDidAllBoards)
                {
                    int iAny = 0;
                    for(int i = 0; i < cSC.ThisBoardSeries.Count;i++)
                    {
                        board nb = cSC.ThisBoardSeries[i];
                        if (cSC.Suitable.CombineLikeSuits(ref nb))
                        {
                            iAny++;
                        }
                    }
                    if (iAny > 0)
                    {
                        cSC.bSpinDidAllBoards = false;
                        m = cSC.ThisBoardSeries.Count;
                        Console.WriteLine("Cannot deal but found " + iAny.ToString() + "  new boards using join suited" + cSC.bSpinDidAllBoards);
                    }
                }
                if (cSC.bSpinDidAllBoards || bSameSeed || cSC.bStopThisRun)
                {
                    m = HandleDeal(ref cSC,false);
                    Console.WriteLine("Handled deal with m:" + m.ToString() + "  bSpin:" + cSC.bSpinDidAllBoards + " SameSeed:" + bSameSeed, " StopRun:" + cSC.bStopThisRun);
                    Console.WriteLine("TBS: " + cSC.ThisBoardSeries.Count + " NBS: " + cSC.NextBoardSeries.Count);
                    cSC.bTrace = true;
                }
                else
                {
                    // unable to finish all boards -or- didnt generate any boards other then the same seeds
                    if (cSC.SortedScores.Count == 0)
                    {
                        // no larger scores were found during the last run:  use ThisSeed
                        m = HandleDeal(ref cSC, true);
                        Console.WriteLine("Handled deal from SortedScores.count=0 m:" + m.ToString());
                    }
                    else
                    {
                        gcst = utils.UseThisManyBoards(ref cSC, ref m);
                        Console.WriteLine("HNS: " + m + "  type " + gcst.ToString());
                        if (gcst != GlobalClass.StrategyType.CONTINUE_SPINNING)
                        {
                            Console.WriteLine("Seem we are to reduce:" + m);
                            HandleNewStrategy(gcst, ref cSC, ref m);
                        }
                    }
                }
            }
            //if (!Program.keepRunning)
            //{
            //    cSC.BestBoard.ShowBoard();  // 11nov2012 added this ctrl-c stuff
            //}
        }

        // 11nov2012 got to identify which deal was actually used.  suggest filename of DEAL1-1, DEAL1-2, ETC
        // then when the second deal occures the true board can be displayed as
        // DEAL1-3  if the 3rd deal was used by the time we got to DEAL2.  Note that there is a possibility that
        // DEAL4 might use DEAL1-2 if that deal was better at the end game.  

        public static int HandleDeal(ref cSpinControl cSC, bool bDealThruSeed)
        {
            int m, n = cSC.ThisBoardSeries.Count; // utils.UseThisManyBoards(ref cSC);
            cDealStrategy DealStrategy = new cDealStrategy(ref cSC);            
            board tb;
            int j, nNewBoards=0;
            if (cSC.BestBoard != null)
            {
                cSC.LocalDealCounter = 0;
                cSC.BestBoard.DealCountIndex = cSC.LocalDealCounter;
                cSC.Deck.SaveBoardAtDeal(ref cSC.BestBoard,0);
                if (cSC.bSpinDidAllBoards && cSC.BestBoard.DealCounter == 5)
                {
                    // if we ran that reduce suits here it would be nice 16nov2012               }
                    GlobalClass.No_Cards_Left.Data.Add("MSG", "Out of Cards in HandleDeal");
                    throw GlobalClass.No_Cards_Left;
                }
            }
            if (bDealThruSeed)
                n = cSC.ThisSeed.Seeds.Count;            
            if (n > GlobalClass.MAX_FILTERED_BOARDS)
                n = GlobalClass.MAX_FILTERED_BOARDS;
            cSC.BestBoard = null;   // only want the one for the current deal
            for (int i = 0; i < n; i++)
            {
                if (bDealThruSeed)
                {
                    if (i >= cSC.ThisSeed.SeedIndex.Count) break;
                    j = cSC.ThisSeed.SeedIndex[i];
                }
                else j = i;
                tb = cSC.ThisBoardSeries[j];
                nNewBoards = DealStrategy.DealToBoard(ref tb, DateTime.Now,n);
                // nNewBoards mistakenly includes dead boards so dont check for n==m anymore
                cSC.LocalDealCounter++;
                tb.DealCountIndex = cSC.LocalDealCounter;
                if(i < 12)cSC.Deck.SaveBoardAtDeal(ref tb,i+1); // 5/2018 usually far less than 12 are useful
                if (nNewBoards > GlobalClass.MinInserts) break;
            }
            m = DealStrategy.GetBoards(ref cSC);
            Debug.Assert(m>0);
            if (m <= 0)
            {
                Console.WriteLine("problem with handleing deal! m:" + m + " nNewBoards:" + nNewBoards);
                Environment.Exit(0);
            }
            tb = cSC.ThisBoardSeries[0];
            utils.SetSuitableValues(ref cSC, ref tb);
            if (m > GlobalClass.MAX_FILTERED_BOARDS) m = GlobalClass.MAX_FILTERED_BOARDS; 
                                    // all will be processed, just want at most 500 results after RunFilter
            if (m < GlobalClass.MIN_FILTERED_BOARDS) m = GlobalClass.MIN_FILTERED_BOARDS;  
                                    // want to have at least this many from the filter if possible
            cSC.BestScore = 0;      // this may get new boards into SortedScores at least on the next filter run
            cSC.stlookup.Clear();
            return m;
        }
        public static void HandleNewStrategy(GlobalClass.StrategyType gcst, ref cSpinControl cSC,ref int FilterSize)
        {
            int i, n;

            switch (gcst)
            {
                case GlobalClass.StrategyType.RUN_JOINSUITS_INPLACE:
                    foreach (board tb in cSC.ThisBoardSeries)
                    {
                        board nb = tb;
                        Strategy.ExposeTop(ref nb);
                        Strategy.RunJoinables(ref nb, true);
                    }
                    break;
                case GlobalClass.StrategyType.SPIN_JOINSUITS :
                    n = cSC.ThisBoardSeries.Count;
                    for (i = 0; i < n; i++)
                    {
                        board tb = cSC.ThisBoardSeries[i];
                        Strategy.FirstStrategy(ref tb);
                    }                    
                    break;
                case GlobalClass.StrategyType.REDUCE_SUITS :
                    PerformReduceSuits(ref cSC, FilterSize);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
;
        }

        public static int PerformReduceSuits(ref cSpinControl cSC, int m)
        {
            bool bAny = true;
            int cnt = 0;
            int j, n, OCount = cSC.ThisBoardSeries.Count;
            for (int i = 0; i < OCount; i++)    // Reduce can add boards to this
            {
                board oldtb = cSC.ThisBoardSeries[i];
                board tb = new board(ref oldtb);
                if (tb.bIsCompletable)
                {
                    Suitable.PerformSuitabilityScreening(ref tb);
                }
                else
                {
                    int Any = Strategy.TryUnstackOne(ref tb, ref cnt);
                    //Strategy.RunJoinables(ref tb, true);
                    if (Any == 0) continue;
                    if (tb.bIsCompletable)
                    {
                        Suitable.PerformSuitabilityScreening(ref tb);
                    }
                    else
                    {
                        cSC.NextBoardSeries.Add(tb);   // it will not be processed because of bIsCompletable however, it
                    }                                  // will be moved back into ThisBoardSeries for another try at it
                }
            }
            bAny = (cSC.NextBoardSeries.Count > 0);
            return cSC.NextBoardSeries.Count;  // jys 16nov2012
            Debug.Assert(bAny);
            while (bAny)
            {
                bAny = Reduce.RunFilter(m);
            }
            n = cSC.ThisBoardSeries.Count;
            if (OCount < n)
            {
                j = 0;
                for (int i = OCount; i < n; i++)
                {
                    cSC.ThisBoardSeries[j] = cSC.ThisBoardSeries[OCount + j];
                    j++;
                }
                cSC.ThisBoardSeries.RemoveRange(OCount, n - OCount);                
            }
        }

    }
}

/*
 * set somethign to null then run
 *       GC.Collect();
      GC.WaitForPendingFinalizers();

 * */