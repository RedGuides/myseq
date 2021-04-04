using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using myseq.Properties;
using Structures;

namespace myseq
{
    // This is the "model" part - no UI related things in here, only hard EQ data.

    public class EQData
    {
        private static readonly SPAWNINFO sPAWNINFO = new SPAWNINFO();

        // player details
        public SPAWNINFO gamerInfo = sPAWNINFO;

        // Map details
        public string longname = "";

        public string shortname = "";
        private static readonly ArrayList maplinearray = new ArrayList();

        // Map data
        public ArrayList lines = maplinearray;//MapLine[MAX_LINES]

        private static ArrayList maptextarray = new ArrayList();
        public ArrayList texts = maptextarray;//MapText[50]

        private readonly ArrayList mobtrails = new ArrayList();//MobTrailPoint[1000]

        // Max + Min map coordinates - define the bounds of the zone

        public float minx = -1000;

        public float maxx = 1000;

        public float miny = -1000;

        public float maxy = 1000;

        public float minz = -1000;

        public float maxz = 1000;

        // Mobs

        private readonly List<GroundItem> itemcollection = new List<GroundItem>();          // Hold the items that are on the ground

        private readonly Hashtable mobsHashTable = new Hashtable();           // Holds the details of the mobs in the current zone.

        public MobsTimers mobsTimers = new MobsTimers();   // Manages the timers

        public int selectedID = 99999;

        public float SpawnX = -1;

        public float SpawnY = -1;

        private int EQSelectedID = 0;

        public DateTime gametime = new DateTime();

        private readonly Random rnd = new Random();

        // Mobs / UI Lists

        public ArrayList newSpawns = new ArrayList();

        public ArrayList newGroundItems = new ArrayList();

        // Items List by ID and Description loaded from file

        //        public Hashtable itemList = new Hashtable();
        public List<ListItem> GroundSpawn = new List<ListItem>();

        // Guild List by ID and Description loaded from file

        //        public Hashtable guildList = new Hashtable();

        // Mobs / Filters

        // Used to improve packet processing speed

        private bool PrefixStars = true;

        private bool AffixStars = true;

        private bool CorpseAlerts = true;

        private bool MatchFullTextH;

        private bool MatchFullTextC;

        private bool MatchFullTextD;

        private bool MatchFullTextA;

        private string HuntPrefix = "";

        private string CautionPrefix = "";

        private string DangerPrefix = "";

        private string AlertPrefix = "";

        public int GreenRange { get; set; }

        public int CyanRange { get; set; }

        public int GreyRange { get; set; }
        public int YellowRange { get; set; } = 3;

        //        private readonly Structures.ColorConverter ColorChart = new Structures.ColorConverter();

        public bool Zoning { get; set; }
        public string[] Classes { get; private set; }
        public string[] Races { get; private set; }
        public string GConBaseName { get; set; } = "";
        public SolidBrush[] ConColors { get; set; } = new SolidBrush[500];

        private const int ditchGone = 2;
        private const string dflt = "Mob Search";
        private string search0 = "";
        private string search1 = "";
        private string search2 = "";
        private string search3 = "";
        private string search4 = "";

        private bool filter0;
        private bool filter1;
        private bool filter2;
        private bool filter3;
        private bool filter4;

        public void MarkLookups(string name, bool filterMob = false)
        {
            if (name.Length > 2 && name.Substring(2) == dflt) { name = name.Substring(0, 2); }
            if (name.Substring(0, 2) == "0:")
            {
                if (name.Length > 2)
                {
                    search0 = name.Substring(2);
                    filter0 = filterMob;
                }
                else
                {
                    search0 = "";
                    filter0 = false;
                }
            }
            else if (name.Substring(0, 2) == "1:")
            {
                if (name.Length > 2)
                {
                    search1 = name.Substring(2);
                    filter1 = filterMob;
                }
                else
                {
                    search1 = "";
                    filter1 = false;
                }
            }
            else if (name.Substring(0, 2) == "2:")
            {
                if (name.Length > 2)
                {
                    search2 = name.Substring(2);
                    filter2 = filterMob;
                }
                else
                {
                    search2 = "";
                    filter2 = false;
                }
            }
            else if (name.Substring(0, 2) == "3:")
            {
                if (name.Length > 2)
                {
                    search3 = name.Substring(2);
                    filter3 = filterMob;
                }
                else
                {
                    search3 = "";
                    filter3 = false;
                }
            }
            else if (name.Substring(0, 2) == "4:")
            {
                if (name.Length > 2)
                {
                    search4 = name.Substring(2);
                    filter4 = filterMob;
                }
                else
                {
                    search4 = "";
                    filter4 = false;
                }
            }

            foreach (SPAWNINFO sp in mobsHashTable.Values)
            {
                sp.isLookup = false;
                sp.lookupNumber = "";
                if (search0.Length > 1)
                {
                    SubLookup(sp, search0, filter0, "1");
                }
                if (search1.Length > 1)
                {
                    SubLookup(sp, search1, filter1, "2");
                }
                if (search2.Length > 1)
                {
                    SubLookup(sp, search2, filter2, "3");
                }
                if (search3.Length > 1)
                {
                    SubLookup(sp, search3, filter3, "4");
                }
                if (search4.Length > 1)
                {
                    SubLookup(sp, search4, filter4, "5");
                }
            }
        }

        private void SubLookup(SPAWNINFO sp, string search, bool filter, string ln)
        {
            var levelCheck = false;
            if (search.Length > 2 && string.Equals(search.Substring(0, 2), "L:", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(search.Substring(2), out var searchLevel);
                if (searchLevel != 0 && (sp.Level == searchLevel))
                {
                    levelCheck = true;
                }
            }
            if (levelCheck || RegexHelper.GetRegex(search).Match(sp.Name).Success)
            {
                sp.isLookup = true;
                sp.lookupNumber = ln;
                sp.hidden = false;
                if (filter) { sp.hidden = true; }
            }
        }

        public Hashtable GetMobsReadonly() => mobsHashTable;

        public ArrayList GetMobTrailsReadonly() => mobtrails;

        public ArrayList GetLinesReadonly() => lines;

        public ArrayList GetTextsReadonly() => texts;

        public List<GroundItem> GetItemsReadonly() => itemcollection;

        public bool SelectTimer(float delta, float x, float y)
        {
            SPAWNTIMER st = FindTimer(delta, x, y);

            if (st != null)
            {
                if (Settings.Default.AutoSelectSpawnList && st.itmSpawnTimerList != null)
                {
                    st.itmSpawnTimerList.EnsureVisible();
                    st.itmSpawnTimerList.Selected = true;
                }
                SPAWNINFO sp = FindMobTimer(st.SpawnLoc);

                selectedID = sp == null ? 99999 : sp.SpawnID;

                SpawnX = st.X;

                SpawnY = st.Y;

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SelectGroundItem(float delta, float x, float y)
        {
            GroundItem gi = FindGroundItem(delta, x, y);

            if (gi != null)
            {
                if (Settings.Default.AutoSelectSpawnList)
                {
                    gi.listitem.EnsureVisible();

                    gi.listitem.Selected = true;
                }

                selectedID = 99999;

                SpawnX = gi.X;

                SpawnY = gi.Y;

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SelectMob(float delta, float x, float y)
        {
            SPAWNINFO sp = FindMobNoPet(delta, x, y) ?? FindMob(delta, x, y);

            if (sp != null)
            {
                if (Settings.Default.AutoSelectSpawnList)
                {
                    sp.listitem.EnsureVisible();

                    sp.listitem.Selected = true;
                }

                selectedID = sp.SpawnID;

                SpawnX = -1.0f;

                SpawnY = -1.0f;

                return true;
            }
            else
            {
                return false;
            }
        }

        public SPAWNINFO FindMobNoPet(float delta, float x, float y)
        {
            try
            {
                foreach (SPAWNINFO sp in mobsHashTable.Values)
                {
                    var dely = sp.Y < y + delta && sp.Y > y - delta;
                    var delx = sp.X < x + delta && sp.X > x - delta;
                    if (!sp.filtered && HiddenFamPet(sp) && delx && dely)
                    {
                        return sp;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogLib.WriteLine("Error in FindMobNoPet(): ", ex);

                return null;
            }
        }

        public SPAWNINFO FindMobNoPetNoPlayerNoCorpse(float delta, float x, float y)
        {
            try
            {
                foreach (SPAWNINFO sp in mobsHashTable.Values)
                {
                    var dely = sp.Y < y + delta && sp.Y > y - delta;
                    var delx = sp.X < x + delta && sp.X > x - delta;

                    if (HiddenFamPet(sp) && !sp.m_isPlayer && !sp.isCorpse && delx && dely)
                    {
                        return sp;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                LogLib.WriteLine("Error in FindMobNoPetNoPlayerNoCorpse(): ", ex);

                return null;
            }
        }

        public SPAWNINFO FindMobNoPetNoPlayer(float delta, float x, float y)
        {
            try
            {
                foreach (SPAWNINFO sp in mobsHashTable.Values)
                {
                    if (HiddenFamPet(sp) && !sp.isCorpse && sp.X < x + delta && sp.X > x - delta && sp.Y < y + delta && sp.Y > y - delta)
                    {
                        return sp;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogLib.WriteLine("Error in FindMobNoPetNoPlayer(): ", ex);

                return null;
            }
        }

        private static bool HiddenFamPet(SPAWNINFO sp) => !sp.hidden && !sp.isFamiliar && !sp.isPet && !sp.isMerc;

        public SPAWNINFO FindMob(float delta, float x, float y)
        {
            try
            {
                foreach (SPAWNINFO sp in mobsHashTable.Values)
                {
                    if (!sp.hidden && !sp.filtered && sp.X < x + delta && sp.X > x - delta && sp.Y < y + delta && sp.Y > y - delta)
                    {
                        return sp;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogLib.WriteLine("Error in FindMob(): ", ex);

                return null;
            }
        }

        public SPAWNINFO FindMobTimer(string spawnLoc)
        {
            try
            {
                foreach (SPAWNINFO sp in mobsHashTable.Values)
                {
                    if ((sp.SpawnLoc == spawnLoc) && (sp.Type == 1))
                    {
                        return sp;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogLib.WriteLine("Error in FindMobTimer(): ", ex);

                return null;
            }
        }

        public SPAWNTIMER FindListViewTimer(ListViewItem listItem)
        {
            try
            {
                // This returns mobsTimer2
                foreach (SPAWNTIMER st in mobsTimers.GetRespawned().Values)
                {
                    if (st.itmSpawnTimerList == listItem)
                    {
                        return st;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                LogLib.WriteLine("Error in SPAWNTIMER FindTimer(): ", ex);

                return null;
            }
        }

        public SPAWNTIMER FindTimer(float delta, float x, float y)
        {
            try
            {
                // This returns mobsTimer2
                foreach (SPAWNTIMER st in mobsTimers.GetRespawned().Values)
                {
                    if (st.X < x + delta && st.X > x - delta && st.Y < y + delta && st.Y > y - delta)
                    {
                        return st;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogLib.WriteLine("Error in SPAWNTIMER FindTimer(): ", ex);

                return null;
            }
        }

        public GroundItem FindGroundItem(float delta, float x, float y)
        {
            foreach (GroundItem gi in itemcollection)
            {
                if (!gi.filtered && gi.X < x + delta && gi.X > x - delta && gi.Y < y + delta && gi.Y > y - delta)
                {
                    return gi;
                }
            }

            return null;
        }

        public GroundItem FindGroundItemNoFilter(float delta, float x, float y)
        {
            foreach (GroundItem gi in itemcollection)
            {
                if (gi.X < x + delta && gi.X > x - delta && gi.Y < y + delta && gi.Y > y - delta)
                {
                    return gi;
                }
            }

            return null;
        }

        public SPAWNINFO GetSelectedMob() => (SPAWNINFO)mobsHashTable[(uint)selectedID];

        public void InitLookups()
        {
            Classes = GetStrArrayFromTextFile(Path.Combine(Settings.Default.CfgDir, "Classes.txt"));

            Races = GetStrArrayFromTextFile(Path.Combine(Settings.Default.CfgDir, "Races.txt"));

            GroundSpawn.Clear();

            ReadItemList(Path.Combine(Settings.Default.CfgDir, "GroundItems.ini"));

            //guildList.Clear();

            //ReadGuildList(Path.Combine(Settings.Default.CfgDir, "Guilds.txt"));

            //            ColorChart.Initialise(Path.Combine(Settings.Default.CfgDir, "RGB.txt"));
        }

        public bool LoadLoYMapInternal(string filename) //ingame EQ format
        {
            IFormatProvider NumFormat = new CultureInfo("en-US");
            var numtexts = 0;
            var numlines = 0;
            var curLine = 0;
            var lineCount = 0;

            if (!File.Exists(filename))
            {
                LogLib.WriteLine($"File not found loading {filename} in loadLoYMap");
                return false;
            }

            LogLib.WriteLine($"Loading Zone Map (LoY): {filename}");

            foreach (var line in File.ReadAllLines(filename))
            {
                if (line.StartsWith("L") || line.StartsWith("P"))
                {
                    ParseLP(filename, NumFormat, line, ref numtexts, ref numlines, curLine);
                }
                else
                {
                    LogLib.WriteLine($"Warning - Line {curLine} of map '{filename}' has an invalid format and will be ignored.", LogLevel.Warning);
                }

                LogLib.WriteLine($"{curLine} lines processed.", LogLevel.Debug);
                LogLib.WriteLine($"Loaded {lines.Count} lines", LogLevel.Debug);

                if (numtexts > 0 || lineCount > 0)
                {
                    shortname = Path.GetFileNameWithoutExtension(filename);

                    if (shortname.IndexOf("_") > 0)
                    {
                        shortname = shortname.Substring(0, shortname.Length - 2);
                    }

                    longname = shortname;

                    CalcExtents();

                    return true;
                }
            }
            return false; // LOY / EQ folder maps
        }

        private void ParseLP(string filename, IFormatProvider NumFormat, string line, ref int numtexts, ref int numlines, int curLine)
        {
            if (line.StartsWith("L"))
            {
                MapLine work = new MapLine();

                MapPoint point1 = new MapPoint();

                MapPoint point2 = new MapPoint();

                var parsedLine = line.Remove(0, 1).Split(",".ToCharArray());

                if (parsedLine.Length == 9)
                {
                    point1.x = -(int)float.Parse(parsedLine[0], NumFormat);
                    point1.y = -(int)float.Parse(parsedLine[1], NumFormat);
                    point1.z = (int)float.Parse(parsedLine[2], NumFormat);

                    point2.x = -(int)float.Parse(parsedLine[3], NumFormat);
                    point2.y = -(int)float.Parse(parsedLine[4], NumFormat);
                    point2.z = -(int)float.Parse(parsedLine[5], NumFormat);

                    var r = int.Parse(parsedLine[6].PadRight(4).Substring(0, 3));
                    var g = int.Parse(parsedLine[7].PadRight(4).Substring(0, 3));
                    var b = int.Parse(parsedLine[8].PadRight(4).Substring(0, 3));
                    work.color = new Pen(new SolidBrush(Color.FromArgb(r, g, b)));

                    work.aPoints.Add(point1);

                    work.aPoints.Add(point2);

                    work.linePoints = new PointF[2];

                    work.linePoints[0] = new PointF(point1.x, point1.y);

                    work.linePoints[1] = new PointF(point2.x, point2.y);

                    lines.Add(work);

                    numlines++;
                }
                else
                {
                    LogLib.WriteLine($"Warning - Line {curLine} of map '{filename}' has an invalid format and will be ignored.", LogLevel.Warning);
                }
            }
            else if (line.StartsWith("P"))
            {// string format "P 175.5915{0}, 894.8506{1}, 148.1645{2},  240{3}, 240{4}, 240{5},  2{6},  Tower{7}"
                MapText work = new MapText();
                var dataRecord = line.Remove(0, 1);
                var parsedline = dataRecord.Split(",".ToCharArray());

                if (parsedline.Length >= 7)
                {
                    work.x = -(int)float.Parse(parsedline[0], NumFormat);
                    work.y = -(int)float.Parse(parsedline[1], NumFormat);
                    work.z = (int)float.Parse(parsedline[2], NumFormat);
                    var r = int.Parse(parsedline[3], NumFormat);
                    var g = int.Parse(parsedline[4], NumFormat);
                    var b = int.Parse(parsedline[5], NumFormat);
                    work.color = new SolidBrush(Color.FromArgb(r, g, b));
                    work.size = int.Parse(parsedline[6], NumFormat);
                    for (var i = 7; i < parsedline.Length; i++)
                    {
                        work.label = parsedline[i];
                    }
                }
                else
                {
                    LogLib.WriteLine($"Warning - Line {curLine} of map '{filename}' has an invalid format and will be ignored.", LogLevel.Warning);
                }
                texts.Add(work);
                numtexts++;
            }
        }

        public void AddMapText(MapText work)
        {
            texts.Add(work);
        }

        public void DeleteMapText(MapText work)
        {
            texts.Remove(work);
        }

        private string[] GetStrArrayFromTextFile(string filePath)
        {
            ArrayList arList = new ArrayList();
            if (File.Exists(filePath))
            {
                foreach (var line in File.ReadAllLines(filePath))
                {
                    if (line != null)
                    {
                        line.Trim();
                        if (line != "" && line.Substring(0, 1) != "#")
                        {
                            arList.Add(line);
                        }
                    }
                }
            }

            return (string[])arList.ToArray(typeof(string));
        }

        private void ReadItemList(string filePath)
        {
            //            IFormatProvider NumFormat = new CultureInfo("en-US"); //no commaseparation on numbers, so this is pointless.


            if (!File.Exists(filePath))
            {
                LogLib.WriteLine("GroundItems.ini file not found", LogLevel.Warning);
                return;
            }

            foreach (var line in File.ReadAllLines(filePath).ToList())
            {
                //sample:  IT0_ACTORDEF = Generic
                var entries = line.Split('=');
                var tmp = entries[0].Split('_');
                ListItem newGround = new ListItem
                {
                    ID = int.Parse(tmp[0].Remove(0, 2)),/* NumFormat)*/ //0
                    ActorDef = entries[0], //IT0_ACTORDEF
                    Name = entries[1] //NAME
                };
                GroundSpawn.Add(newGround);
            }
        }

        //private void ReadGuildList(string filePath)
        //{
        //    string line = "";

        //    IFormatProvider NumFormat = new CultureInfo("en-US");

        //    if (!File.Exists(filePath))
        //    {
        //        // we did not find the Guild file
        //        LogLib.WriteLine("Guild file not found", LogLevel.Warning);
        //        return;
        //    }

        //    FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        //    StreamReader sr = new StreamReader(fs);

        //    while (line != null)
        //    {
        //        line = sr.ReadLine();

        //        if (line != null)
        //        {
        //            line = line.Trim();

        //            if (line.Length > 0 && (!line.StartsWith("[") && !line.StartsWith("#")))
        //            {
        //                ListItem thisitem = new ListItem();

        //                string tok;
        //                if ((tok = Getnexttoken(ref line, '=')) != null)
        //                {
        //                    thisitem.ActorDef = tok.ToUpper();

        //                    if ((tok = Getnexttoken(ref line, ',')) != null)
        //                    {
        //                        thisitem.Name = tok;

        //                        if ((tok = Getnexttoken(ref thisitem.ActorDef, '_')) != null)
        //                        {
        //                            thisitem.ID = int.Parse(tok, NumFormat);

        //                            // We got this far, so we have a valid item to add

        //                            if (!guildList.ContainsKey(thisitem.ID))
        //                            {
        //                                try { guildList.Add(thisitem.ID, thisitem); }
        //                                catch (Exception ex) { LogLib.WriteLine("Error adding " + thisitem.ID + " to Guilds hashtable: ", ex); }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    sr.Close();
        //    fs.Close();
        //}

        public string GetItemDescription(string ActorDef)
        {//sample:  IT0_ACTORDEF
            var lookupid = int.Parse(ActorDef.Remove(0, 2).Split('_')[0]);

            for (var i = 0; i < GroundSpawn.Count; i++)
            {
                if (GroundSpawn[i].ID.Equals(lookupid))
                {
                    return GroundSpawn[i].Name;
                }
            }
            return ActorDef;
        }

        //public string GetGuildDescription(string guildDef)
        //{
        //    // I know - ## NEEDS CLEANUP
        //    // Get description from list made using Guildlist.txt
        //    string tok;
        //    return (tok = Getnexttoken(ref guildDef, '_')) != null
        //        && guildList.ContainsKey(int.Parse(tok, new CultureInfo("en-US")))
        //        ? ((ListItem)guildList[int.Parse(tok, new CultureInfo("en-US"))]).Name
        //        : guildDef;
        //}

        private string ArrayIndextoStr(string[] source, int index) => index < source.GetLowerBound(0) || index > source.GetUpperBound(0) ? $"{index}: Unknown" : source[index];

        public void ClearMapStructures()
        {
            lines.Clear();
            texts.Clear();
            CalcExtents();
        }

        public void CalcExtents()
        {
            if (longname != "" && lines.Count > 0)
            {
                maxx = minx = ((MapLine)lines[0]).Point(0).x;

                maxy = miny = ((MapLine)lines[0]).Point(0).y;

                maxz = minz = ((MapLine)lines[0]).Point(0).z;

                foreach (MapLine mapLine in lines)
                {
                    foreach (MapPoint mapPoint in mapLine.aPoints)
                    {
                        if (mapPoint.x > maxx)
                        {
                            maxx = mapPoint.x;
                        }
                        else if (mapPoint.x < minx)
                        {
                            minx = mapPoint.x;
                        }

                        if (mapPoint.y > maxy)
                        {
                            maxy = mapPoint.y;
                        }
                        else if (mapPoint.y < miny)
                        {
                            miny = mapPoint.y;
                        }

                        if (mapPoint.z > maxz)
                        {
                            maxz = mapPoint.z;
                        }
                        else if (mapPoint.z < minz)
                        {
                            minz = mapPoint.z;
                        }
                    }
                }
            }
            else
            {
                minx = -1000;

                maxx = 1000;

                miny = -1000;

                maxy = 1000;

                minz = -1000;

                maxz = 1000;
            }
        }

        public void CheckMobs(ListViewPanel SpawnList, ListViewPanel GroundItemList)
        {
            ArrayList deletedItems = new ArrayList();

            ArrayList delListItems = new ArrayList();

            // Increment the remove timers on all the ground spawns

            foreach (GroundItem sp in itemcollection)
            {
                if (sp.gone >= ditchGone)
                {
                    deletedItems.Add(sp);
                }
                else
                {
                    sp.gone++;
                }
            }

            // Remove any that have been marked for deletion
            if (deletedItems.Count > 0)
            {
                //if (Zoning || deletedItems.Count > 5)
                //{
                //    GroundItemList.listView.BeginUpdate();
                //}

                foreach (GroundItem gi in deletedItems)
                {
                    GroundItemList.listView.Items.Remove(gi.listitem);

                    gi.listitem = null;

                    itemcollection.Remove(gi);
                }

                //if (Zoning || deletedItems.Count > 5)
                //{
                //    GroundItemList.listView.EndUpdate();
                //}
            }
            deletedItems.Clear();

            // Increment the remove timers on all the mobs

            foreach (SPAWNINFO sp in mobsHashTable.Values)
            {
                if (sp.delFromList)
                {
                    sp.delFromList = false;

                    delListItems.Add(sp);
                }
                else if (sp.gone >= ditchGone)
                {
                    deletedItems.Add(sp);
                }
                else
                {
                    sp.gone++;
                }
            }

            // Remove any that have been marked for deletion

            if (deletedItems.Count > 0 || delListItems.Count > 0)
            {
                if (Zoning || deletedItems.Count > 5 || delListItems.Count > 5)
                {
                    SpawnList.listView.BeginUpdate();
                }

                foreach (SPAWNINFO sp in deletedItems)
                {
                    SpawnList.listView.Items.Remove(sp.listitem);

                    sp.listitem = null;

                    mobsHashTable.Remove(sp.SpawnID);
                }

                foreach (SPAWNINFO sp in delListItems)
                {
                    SpawnList.listView.Items.Remove(sp.listitem);
                }

                if (Zoning || deletedItems.Count > 5 || delListItems.Count > 5)
                {
                    SpawnList.listView.EndUpdate();
                }

                delListItems.Clear();

                deletedItems.Clear();
            }
        }

        public void ProcessGroundItems(SPAWNINFO si, Filters filters)//, ListViewPanel GroundItemList)
        {
            try
            {
                var found = false;
                foreach (GroundItem gi in itemcollection)
                {
                    if (gi.Name == si.Name && gi.X == si.X && gi.Y == si.Y && gi.Z == si.Z)
                    {
                        found = true;
                        gi.gone = 0;
                        break;
                    }
                }

                if (!found)
                {
                    GroundItem gi = new GroundItem
                    {
                        X = si.X,
                        Y = si.Y,
                        Z = si.Z,
                        Name = si.Name,
                        Desc = GetItemDescription(si.Name)
                    };

                    var itemname = gi.Desc.ToLower();

                    /* ************************************* *
                    * ************* ALERTS **************** *
                    * ************************************* */

                    // [hunt]

                    if (filters.Hunt.Count > 0 && FindMatches(filters.Hunt, itemname, Settings.Default.NoneOnHunt,

                            Settings.Default.TalkOnHunt, "Ground Item",

                            Settings.Default.PlayOnHunt, Settings.Default.HuntAudioFile,

                            Settings.Default.BeepOnHunt, MatchFullTextH))
                    {
                        gi.isHunt = true;
                    }

                    if (filters.GlobalHunt.Count > 0 && FindMatches(filters.GlobalHunt, itemname, Settings.Default.NoneOnHunt,

                            Settings.Default.TalkOnHunt, "Ground Item",

                            Settings.Default.PlayOnHunt, Settings.Default.HuntAudioFile,

                            Settings.Default.BeepOnHunt, MatchFullTextH))
                    {
                        gi.isHunt = true;
                    }

                    // [caution]

                    if (filters.Caution.Count > 0 && FindMatches(filters.Caution, itemname, Settings.Default.NoneOnCaution,

                            Settings.Default.TalkOnCaution, "Ground Item",

                            Settings.Default.PlayOnCaution, Settings.Default.CautionAudioFile,

                            Settings.Default.BeepOnCaution, MatchFullTextC))
                    {
                        gi.isCaution = true;
                    }

                    if (filters.GlobalCaution.Count > 0 && FindMatches(filters.GlobalCaution, itemname, Settings.Default.NoneOnCaution,

                            Settings.Default.TalkOnCaution, "Ground Item",

                            Settings.Default.PlayOnCaution, Settings.Default.CautionAudioFile,

                            Settings.Default.BeepOnCaution, MatchFullTextC))
                    {
                        gi.isCaution = true;
                    }

                    // [danger]

                    if (filters.Danger.Count > 0 && FindMatches(filters.Danger, itemname, Settings.Default.NoneOnDanger,

                            Settings.Default.TalkOnDanger, "Ground Item",

                            Settings.Default.PlayOnDanger, Settings.Default.DangerAudioFile,

                            Settings.Default.BeepOnDanger, MatchFullTextD))
                    {
                        gi.isDanger = true;
                    }

                    if (filters.GlobalDanger.Count > 0 && FindMatches(filters.GlobalDanger, itemname, Settings.Default.NoneOnDanger,

                            Settings.Default.TalkOnDanger, "Ground Item",

                            Settings.Default.PlayOnDanger, Settings.Default.DangerAudioFile,

                            Settings.Default.BeepOnDanger, MatchFullTextD))
                    {
                        gi.isDanger = true;
                    }

                    // [rare]

                    if (filters.Alert.Count > 0 && FindMatches(filters.Alert, itemname, Settings.Default.NoneOnAlert,

                            Settings.Default.TalkOnAlert, "Ground Item",

                            Settings.Default.PlayOnAlert, Settings.Default.AlertAudioFile,

                            Settings.Default.BeepOnAlert, MatchFullTextA))
                    {
                        gi.isAlert = true;
                    }

                    if (filters.GlobalAlert.Count > 0 && FindMatches(filters.GlobalAlert, itemname, Settings.Default.NoneOnAlert,

                            Settings.Default.TalkOnAlert, "Ground Item",

                            Settings.Default.PlayOnAlert, Settings.Default.AlertAudioFile,

                            Settings.Default.BeepOnAlert, MatchFullTextA))
                    {
                        gi.isAlert = true;
                    }

                    ListViewItem item1 = new ListViewItem(gi.Desc);

                    item1.SubItems.Add(si.Name);

                    DateTime dt = DateTime.Now;

                    item1.SubItems.Add(dt.ToLongTimeString());

                    item1.SubItems.Add(si.X.ToString("#.000"));

                    item1.SubItems.Add(si.Y.ToString("#.000"));

                    item1.SubItems.Add(si.Z.ToString("#.000"));

                    gi.listitem = item1;

                    itemcollection.Add(gi);

                    // Add it to the ground item list
                    newGroundItems.Add(item1);
                }
            }
            catch (Exception ex) { LogLib.WriteLine("Error in ProcessGroundItems(): ", ex); }
        }

        public void ProcessTarget(SPAWNINFO si)
        {
            try
            {
                if (Settings.Default.AutoSelectEQTarget && EQSelectedID != si.SpawnID)
                {
                    EQSelectedID = selectedID = si.SpawnID;

                    SpawnX = -1.0f;

                    SpawnY = -1.0f;

                    foreach (SPAWNINFO sp in mobsHashTable.Values)
                    {
                        if (sp.SpawnID == EQSelectedID)
                        {
                            if (Settings.Default.AutoSelectSpawnList)
                            {
                                sp.listitem.EnsureVisible();

                                sp.listitem.Selected = true;
                            }

                            break;
                        }
                    }
                }
            }
            catch (Exception ex) { LogLib.WriteLine("Error in ProcessTarget(): ", ex); }
        }

        public void ProcessWorld(SPAWNINFO si) => gametime = new DateTime(si.Race, si.Hide, si.Level, si.Type - 1, si.Class, 0);

        /*  yy/mm/dd/hh/min
         * gameYear = si.Race
         * gameMonth = si.Hide
         * gameDay = si.Levelafk
         * gameHour = si.Type - 1
         * gameMin = si.Class
        */

        public void ProcessSpawns(SPAWNINFO si, MainForm f1, ListViewPanel SpawnList, Filters filters, MapPane mapPane, bool update_hidden)
        {
            CorpseAlerts = Settings.Default.CorpseAlerts;
            if (si.Name.Contains("a_tainted_egg"))
            {
                si.Class = 1;
            }

            try
            {
                var listReAdd = false;

                var found = false;

                SPAWNINFO mob;

                // Converted mob collection to a hashtable so we can do

                // a fast lookup to see if a mob already exists

                if (mobsHashTable.ContainsKey(si.SpawnID))
                {
                    found = true;

                    mob = (SPAWNINFO)mobsHashTable[si.SpawnID];

                    mob.gone = 0;

                    if (update_hidden)
                    {
                        mob.refresh = 100;
                    }

                    // some of these should not change often, so only check every 10 times through
                    if (mob.refresh > 10)
                    {
                        // Update Hidden flags
                        if (update_hidden)
                        {
                            listReAdd = UpdateHidden(si, listReAdd, mob);
                        } // end update_hidden

                        // Update mob types
                        if (mob.Type != si.Type)
                        {
                            UpdateMobTypes(si, SpawnList, mob);
                        }

                        // check if the mob name has changed - eg when a mob dies.

                        if ((si.Name.Length > 0) && (string.Compare(mob.Name, si.Name) != 0))
                        {
                            NameChngOrDead(si, mob);
                        }

                        MobLevelSetColor(si, SpawnList, mob);

                        if (mob.Class != si.Class)
                        {
                            mob.Class = si.Class;
                            mob.listitem.SubItems[2].Text = GetClass(si.Class);
                        }

                        if (mob.Primary != si.Primary)
                        {
                            mob.Primary = si.Primary;
                            mob.listitem.SubItems[3].Text = si.Primary > 0 ? ItemNumToString(si.Primary) : "";
                        }

                        if (mob.Offhand != si.Offhand)
                        {
                            mob.Offhand = si.Offhand;
                            mob.listitem.SubItems[4].Text = si.Offhand > 0 ? ItemNumToString(si.Offhand) : "";
                        }

                        if (mob.Race != si.Race)
                        {
                            mob.Race = si.Race;
                            mob.listitem.SubItems[5].Text = GetRace(si.Race);
                        }

                        if (mob.OwnerID != si.OwnerID)
                        {
                            mob.OwnerID = si.OwnerID;
                            if (mob.OwnerID == 0)
                            {
                                mob.listitem.SubItems[6].Text = "";
                                mob.isPet = false;
                            }
                            else if (mobsHashTable.ContainsKey(mob.OwnerID))
                            {
                                SPAWNINFO owner = (SPAWNINFO)mobsHashTable[mob.OwnerID];

                                if (owner.IsPlayer)
                                {
                                    mob.isPet = true;
                                    mob.listitem.ForeColor = Color.Gray;
                                }
                                else
                                {
                                    mob.isPet = false;
                                }
                                mob.listitem.SubItems[6].Text = RegexHelper.FixMobName(owner.Name);
                            }
                            else
                            {
                                mob.listitem.SubItems[6].Text = mob.OwnerID.ToString();
                                mob.isPet = false;
                            }
                        }

                        if (mob.Hide != si.Hide)
                        {
                            mob.Hide = si.Hide;
                            mob.listitem.SubItems[9].Text = PrettyNames.GetHideStatus(si.Hide);
                        }

                        //if (mob.Guild != si.Guild)
                        //{
                        //    mob.Guild = si.Guild;

                        //    if (si.Guild > 0)
                        //        mob.listitem.SubItems[17].Text = GuildNumToString(si.Guild);
                        //    else
                        //        mob.listitem.SubItems[17].Text = "";
                        //}

                        mob.refresh = 0;
                    } // end refresh > 10

                    mob.refresh++;

                    // Set variables we dont want to trigger list update

                    if (selectedID != mob.SpawnID)
                    {
                        if (mob.X != si.X)
                        {
                            // ensure that map is big enough to show all spawns.

                            CheckBigMap(si, mapPane);

                            mob.X = si.X;

                            mob.Y = si.Y;
                        }
                        else if (mob.Y != si.Y)
                        {
                            mob.Y = si.Y;
                        }

                        // update these for all but selected mob, so they do not refresh for all mobs
                        mob.Z = si.Z;
                    }

                    mob.Heading = si.Heading;

                    mob.SpeedRun = si.SpeedRun;

                    if (mob.SpeedRun != si.SpeedRun)
                    {
                        mob.SpeedRun = si.SpeedRun;
                        mob.listitem.SubItems[10].Text = si.SpeedRun.ToString();
                    }

                    if ((mob.X != si.X) || (mob.Y != si.Y) || (mob.Z != si.Z))
                    {
                        // this should be the selected id
                        // ensure that map is big enough to show all spawns.

                        AdjustMapForSpawns(si, f1, mapPane, mob);
                    }

                    if (listReAdd)
                    {
                        newSpawns.Add(mob.listitem);
                    }
                } // end of if found

                // If it's not already in there, add it

                if (!found && si.Name.Length > 0)
                {
                    //                    bool alert = false;

                    // ensure that map is big enough to show all spawns.

                    CheckBigMap(si, mapPane);

                    // Set mob type info

                    if (si.Type == 0)
                    {
                        // Players

                        si.m_isPlayer = true;

                        if (!Settings.Default.ShowPlayers)
                        {
                            si.hidden = true;
                        }
                    }
                    else if (si.Type == 2 || si.Type == 3)
                    {
                        // Corpses
                        HandleCorpses(si);
                    }
                    else
                    {
                        // non-corpse, non-player spawn (aka NPC)

                        HandleNPCs(si);
                    }

                    mobsTimers.Spawn(si);

                    if (si.Name.Length > 0)
                    {
                        IsSpawnInFilterLists(si, f1, SpawnList, filters);//, alert);
                    }
                }
            }
            catch (Exception ex) { LogLib.WriteLine("Error in ProcessSpawns(): ", ex); }
        }

        private void MobLevelSetColor(SPAWNINFO si, ListViewPanel SpawnList, SPAWNINFO mob)
        {
            if (mob.Level != si.Level)
            {
                mob.Level = si.Level;
                mob.listitem.SubItems[1].Text = si.Level.ToString();

                // update forecolor
                if (mob.Type == 2 || mob.Type == 3 || mob.isLDONObject)
                {
                    mob.listitem.ForeColor = Color.Gray;
                }
                else if (mob.isEventController)
                {
                    mob.listitem.ForeColor = Color.DarkOrchid;
                }
                else
                {
                    // a duplicate codeblock was found, extracted as common method.
                    SetListColors(si, SpawnList, mob);
                }
            }
        }

        private void CheckBigMap(SPAWNINFO si, MapPane mapPane)
        {
            if (mapPane?.scale.Value == 100M && Settings.Default.AutoExpand)
            {
                if ((minx > si.X) && (si.X > -20000))
                {
                    minx = si.X;
                }

                if ((maxx < si.X) && (si.X < 20000))
                {
                    maxx = si.X;
                }

                if ((miny > si.Y) && (si.Y > -20000))
                {
                    miny = si.Y;
                }

                if ((maxy < si.Y) && (si.Y < 20000))
                {
                    maxy = si.Y;
                }
            }
        }

        private void HandleNPCs(SPAWNINFO si)
        {
            if (!Settings.Default.ShowNPCs)
            {
                si.hidden = true;
            }

            if (si.OwnerID > 0)
            {
                SPAWNINFO owner;

                if (mobsHashTable.ContainsKey(si.OwnerID))
                {
                    owner = (SPAWNINFO)mobsHashTable[si.OwnerID];
                    if (owner.IsPlayer)
                    {
                        si.isPet = true;
                        if (!Settings.Default.ShowPets)
                        {
                            si.hidden = true;
                        }
                    }
                }
                else
                {
                    // we didnt find owner, so set to 0
                    // so we can check again next update
                    si.OwnerID = 0;
                }
            }

            if ((si.Race == 127) && ((si.Name.IndexOf("_") == 0) || (si.Level < 2) || (si.Class == 62))) // Invisible Man Race
            {
                si.isEventController = true;
                if (!Settings.Default.ShowInvis)
                {
                    si.hidden = true;
                }
            }
            else if (si.Class == 62)
            {
                si.isLDONObject = true;
            }

            // Mercenary Identification - Only do it once now

            if (!string.IsNullOrEmpty(si.Lastname))
            {
                if (RegexHelper.IsMerc(si.Lastname))
                {
                    si.isMerc = true;
                }
            }
            else if (RegexHelper.IsMount(si.Name)) // Mounts
            {
                si.isMount = true;

                if (!Settings.Default.ShowMounts)
                {
                    si.hidden = true;
                }
            }
            else if (RegexHelper.IsFamiliar(si.Name))
            {
                // reset these, if match a familiar
                si.isPet = false;
                si.hidden = false;

                si.isFamiliar = true;

                if (!Settings.Default.ShowFamiliars)
                {
                    si.hidden = true;
                }
            }
        }

        private void HandleCorpses(SPAWNINFO si)
        {
            si.isCorpse = true;

            if (!CorpseAlerts)
            {
                si.isHunt = false;

                si.isCaution = false;

                si.isDanger = false;

                si.isAlert = false;
            }

            if ((si.Name.IndexOf("_") == -1) && (si.Name.IndexOf("a ") != 0) && (si.Name.IndexOf("an ") != 0))
            {
                si.m_isPlayer = true;

                if (!Settings.Default.ShowPCCorpses)
                {
                    si.hidden = true;
                }

                if (si.Name.Length > 0 && CheckMyCorpse(si.Name))
                {
                    si.m_isMyCorpse = true;

                    si.hidden = !Settings.Default.ShowMyCorpse;
                }
            }
            else if (!Settings.Default.ShowCorpses)
            {
                si.hidden = true;
            }
        }

        private void AdjustMapForSpawns(SPAWNINFO si, MainForm f1, MapPane mapPane, SPAWNINFO mob)
        {
            CheckBigMap(si, mapPane);

            mob.X = si.X;
            mob.listitem.SubItems[14].Text = si.Y.ToString();

            mob.Y = si.Y;
            mob.listitem.SubItems[13].Text = si.X.ToString();

            mob.Z = si.Z;
            mob.listitem.SubItems[15].Text = si.Z.ToString();

            var sd = SpawnDistance(si);

            if (Settings.Default.FollowOption == FollowOption.Target)
            {
                f1.ReAdjust();
            }

            mob.listitem.SubItems[16].Text = sd.ToString("#.000");
        }

        private void UpdateMobTypes(SPAWNINFO si, ListViewPanel SpawnList, SPAWNINFO mob)
        {
            mob.Type = si.Type;
            mob.listitem.SubItems[8].Text = PrettyNames.GetSpawnType(si.Type);

            if (si.Type == 2 || si.Type == 3)
            {
                mob.listitem.ForeColor = Color.Gray;

                mob.isCorpse = true;

                if (!CorpseAlerts)
                {
                    mob.isHunt = false;

                    mob.isCaution = false;

                    mob.isDanger = false;

                    mob.isAlert = false;
                }
            }
            else if ((si.Race == 127) && ((si.Name.IndexOf("_") == 0) || (si.Level < 2) || (si.Class == 62))) // Invisible Man Race
            {
                mob.listitem.ForeColor = Color.DarkOrchid;
                si.isEventController = true;
            }
            else if (si.Class == 62)
            {
                mob.listitem.ForeColor = Color.Gray;
                si.isLDONObject = true;
            }
            else
            {
                SetListColors(si, SpawnList, mob);
            }
        }

        private void SetListColors(SPAWNINFO si, ListViewPanel SpawnList, SPAWNINFO mob)
        {
            mob.listitem.ForeColor = ConColors[si.Level].Color;

            if (mob.listitem.ForeColor == Color.Maroon)
            {
                mob.listitem.ForeColor = Color.Red;
            }
            else if (SpawnList.listView.BackColor == Color.White)
            {
                // Change the colors to be more visible on white if the background is white

                if (mob.listitem.ForeColor == Color.White)
                {
                    mob.listitem.ForeColor = Color.Black;
                }
                else if (mob.listitem.ForeColor == Color.Yellow)
                {
                    mob.listitem.ForeColor = Color.Goldenrod;
                }
            }
        }

        private static bool UpdateHidden(SPAWNINFO si, bool listReAdd, SPAWNINFO mob)
        {
            if (mob.isCorpse)
            {
                if (mob.m_isPlayer)
                {
                    // My Corpse

                    if (mob.m_isMyCorpse)
                    {
                        si.hidden = !Settings.Default.ShowMyCorpse;
                    }
                    else
                    {
                        // Other Players Corpses

                        si.hidden = !Settings.Default.ShowPCCorpses;
                    }
                }
                else
                {
                    si.hidden = !Settings.Default.ShowCorpses;
                }
            }
            else if (mob.m_isPlayer)
            {
                si.hidden = !Settings.Default.ShowPlayers;
            }
            else
            {
                // non-corpse, non-player spawn (aka NPC)

                if (!Settings.Default.ShowNPCs) // hides all NPCs
                {
                    si.hidden = true;
                }
                else
                {
                    si.hidden = false;

                    if (si.isEventController && !Settings.Default.ShowInvis) // Invis Men
                    {
                        si.hidden = true;
                    }
                    else if (mob.isMount && !Settings.Default.ShowMounts) // Mounts
                    {
                        si.hidden = true;
                    }
                    else if (mob.isPet && !Settings.Default.ShowPets) // Pets
                    {
                        si.hidden = true;
                    }
                    else if (mob.isFamiliar && !Settings.Default.ShowFamiliars) // Familiars
                    {
                        si.hidden = true;
                    }
                }
            }

            if (si.hidden && !mob.hidden)
            {
                mob.delFromList = true;
            }

            if (!si.hidden && mob.hidden)
            {
                listReAdd = true;
            }

            mob.hidden = si.hidden;
            return listReAdd;
        }

        private void IsSpawnInFilterLists(SPAWNINFO si, MainForm f1, ListViewPanel SpawnList, Filters filters)//, bool alert)
        {
            var mobname = si.isMerc ? RegexHelper.FixMobNameMatch(si.Name) : RegexHelper.FixMobName(si.Name);

            var matchmobname = RegexHelper.FixMobNameMatch(mobname);
            var alert = false;
            if (matchmobname.Length < 2)
            {
                matchmobname = mobname;
            }

            var mobnameWithInfo = mobname;

            var primaryName = "";

            if (si.Primary > 0 || si.Offhand > 0)
            {
                primaryName = ItemNumToString(si.Primary);
            }

            var offhandName = "";

            if (si.Offhand > 0)
            {
                offhandName = ItemNumToString(si.Offhand);
            }

            // Don't do alert matches for controllers, Ldon objects, pets, mercs, mounts, or familiars
            if (!(si.isLDONObject || si.isEventController || si.isFamiliar || si.isMount || (si.isMerc && si.OwnerID != 0)))
            {
                /* ************************************* *
                * ************* ALERTS **************** *
                * ************************************* */

                // [hunt]

                var corpse = !si.isCorpse || CorpseAlerts;
                if (filters.Hunt.Count > 0 && corpse && FindMatches(filters.Hunt, matchmobname, Settings.Default.NoneOnHunt,
                    Settings.Default.TalkOnHunt, "Hunt Mob",
                    Settings.Default.PlayOnHunt, Settings.Default.HuntAudioFile,
                    Settings.Default.BeepOnHunt, MatchFullTextH))
                {
                    alert = true;
                    mobnameWithInfo = PrefixAffixLabel(mobnameWithInfo, HuntPrefix);

                    si.isHunt = true;
                }
                if (filters.GlobalHunt.Count > 0 && !alert && corpse && FindMatches(filters.GlobalHunt, matchmobname, Settings.Default.NoneOnHunt,

                        Settings.Default.TalkOnHunt, "Hunt Mob",
                        Settings.Default.PlayOnHunt, Settings.Default.HuntAudioFile,
                        Settings.Default.BeepOnHunt, MatchFullTextH))
                {
                    alert = true;
                    mobnameWithInfo = PrefixAffixLabel(mobnameWithInfo, HuntPrefix);
                    si.isHunt = true;
                }

                // [caution]
                if (filters.Caution.Count > 0 && !alert && corpse && FindMatches(filters.Caution, matchmobname, Settings.Default.NoneOnCaution,

                        Settings.Default.TalkOnCaution, "Caution Mob",
                        Settings.Default.PlayOnCaution, Settings.Default.CautionAudioFile,
                        Settings.Default.BeepOnCaution, MatchFullTextC))
                {
                    alert = true;
                    si.isCaution = true;

                    mobnameWithInfo = PrefixAffixLabel(mobnameWithInfo, CautionPrefix);

                    si.isCaution = true;
                }
                if (filters.GlobalCaution.Count > 0 && !alert && corpse && FindMatches(filters.GlobalCaution, matchmobname, Settings.Default.NoneOnCaution,

                        Settings.Default.TalkOnCaution, "Caution Mob",
                        Settings.Default.PlayOnCaution, Settings.Default.CautionAudioFile,
                        Settings.Default.BeepOnCaution, MatchFullTextC))
                {
                    alert = true;

                    si.isCaution = true;

                    mobnameWithInfo = PrefixAffixLabel(mobnameWithInfo, CautionPrefix);
                }

                // [danger]
                if (filters.Danger.Count > 0 && !alert && corpse && FindMatches(filters.Danger, matchmobname, Settings.Default.NoneOnDanger,

                        Settings.Default.TalkOnDanger, "Danger Mob",

                        Settings.Default.PlayOnDanger, Settings.Default.DangerAudioFile,

                        Settings.Default.BeepOnDanger, MatchFullTextD))
                {
                    alert = true;

                    mobnameWithInfo = PrefixAffixLabel(mobnameWithInfo, DangerPrefix);

                    si.isDanger = true;
                }
                if (filters.GlobalDanger.Count > 0 && !alert && corpse && FindMatches(filters.GlobalDanger, matchmobname, Settings.Default.NoneOnDanger,
                        Settings.Default.TalkOnDanger, "Danger Mob",
                        Settings.Default.PlayOnDanger, Settings.Default.DangerAudioFile,
                        Settings.Default.BeepOnDanger, MatchFullTextD))
                {
                    alert = true;

                    mobnameWithInfo = PrefixAffixLabel(mobnameWithInfo, DangerPrefix);

                    si.isDanger = true;
                }

                // [rare]
                if (filters.Alert.Count > 0 && !alert && corpse && FindMatches(filters.Alert, matchmobname, Settings.Default.NoneOnAlert,
                        Settings.Default.TalkOnAlert, "Rare Mob",
                        Settings.Default.PlayOnAlert, Settings.Default.AlertAudioFile,
                        Settings.Default.BeepOnAlert, MatchFullTextA))
                {
                    alert = true;

                    si.isAlert = true;

                    mobnameWithInfo = PrefixAffixLabel(mobnameWithInfo, AlertPrefix);
                }
                if (filters.GlobalAlert.Count > 0 && !alert && corpse && FindMatches(filters.GlobalAlert, matchmobname, Settings.Default.NoneOnAlert,
                        Settings.Default.TalkOnAlert, "Rare Mob",
                        Settings.Default.PlayOnAlert, Settings.Default.AlertAudioFile,
                        Settings.Default.BeepOnAlert, MatchFullTextA))
                {
                   mobnameWithInfo = PrefixAffixLabel(mobnameWithInfo, AlertPrefix);

                    si.isAlert = true;
                }
                //// [Email]
                //if (filters.EmailAlert.Count > 0 && !si.isCorpse && FindMatches(filters.EmailAlert, matchmobname, false, false, "", false, "", !si.isAlert && !si.isCaution && !si.isDanger && !si.isHunt, true))
                //{
                //    // Flag on map as an alert mob
                //    si.isAlert = true;
                //}

                // [Wielded Items]
                // Acts like a hunt mob.
                if (filters.WieldedItems.Count > 0 && corpse && FindMatches(filters.WieldedItems, primaryName, Settings.Default.NoneOnHunt,
                        Settings.Default.TalkOnHunt, "Hunt Mob Wielded",
                        Settings.Default.PlayOnHunt, Settings.Default.HuntAudioFile,
                        Settings.Default.BeepOnHunt, MatchFullTextH))
                {
                    mobnameWithInfo = PrefixAffixLabel(mobnameWithInfo, HuntPrefix);

                    si.isHunt = true;
                }

                // [Offhand]
                // Acts like a hunt mob.
                if (filters.WieldedItems.Count > 0 && corpse && FindMatches(filters.WieldedItems, offhandName,
                    Settings.Default.NoneOnHunt,
                        Settings.Default.TalkOnHunt, "Hunt Mob Wielded",
                        Settings.Default.PlayOnHunt, Settings.Default.HuntAudioFile,
                        Settings.Default.BeepOnHunt, MatchFullTextH))
                {
                    mobnameWithInfo = PrefixAffixLabel(mobnameWithInfo, HuntPrefix);
                    si.isAlert = true;
                }

                LookupBoxMatch(si, f1);
            }

            ListViewItem item1 = AddDetailsToList(si, SpawnList, mobnameWithInfo);
            try { mobsHashTable.Add(si.SpawnID, si); }
            catch (Exception ex) { LogLib.WriteLine($"Error adding {si.Name} to mobs hashtable: ", ex); }

            // Add it to the spawn list if it's not supposed to be hidden
            if (!si.hidden)
            {
                newSpawns.Add(item1);
            }


        }
        private string PrefixAffixLabel(string mname, string prefix)
        {
            if (PrefixStars)
            {
                mname = prefix + " " + mname;
            }

            if (AffixStars)
            {
                mname += " " + prefix;
            }

            return mname;
        }

        private ListViewItem AddDetailsToList(SPAWNINFO si, ListViewPanel SpawnList, string mobnameWithInfo)
        {
            ListViewItem item1 = new ListViewItem(mobnameWithInfo);

            item1.SubItems.Add(si.Level.ToString());

            item1.SubItems.Add(GetClass(si.Class));

            if (si.Primary > 0)
            {
                item1.SubItems.Add(ItemNumToString(si.Primary));
            }
            else
            {
                item1.SubItems.Add("");
            }

            if (si.Offhand > 0)
            {
                item1.SubItems.Add(ItemNumToString(si.Offhand));
            }
            else
            {
                item1.SubItems.Add("");
            }

            item1.SubItems.Add(GetRace(si.Race));

            OwnerFlag(si, item1);

            item1.SubItems.Add(si.Lastname);

            item1.SubItems.Add(PrettyNames.GetSpawnType(si.Type));

            item1.SubItems.Add(PrettyNames.GetHideStatus(si.Hide));

            item1.SubItems.Add(si.SpeedRun.ToString());

            item1.SubItems.Add(si.SpawnID.ToString());

            item1.SubItems.Add(DateTime.Now.ToLongTimeString());

            item1.SubItems.Add(si.X.ToString("#.00"));

            item1.SubItems.Add(si.Y.ToString("#.00"));

            item1.SubItems.Add(si.Z.ToString("#.00"));

            item1.SubItems.Add(SpawnDistance(si).ToString("#.00"));

            //            item1.SubItems.Add(GuildNumToString(si.Guild));

            item1.SubItems.Add(RegexHelper.FixMobName(si.Name));

            if (si.Type == 2 || si.Type == 3 || si.isLDONObject)
            {
                item1.ForeColor = Color.Gray;
            }
            else if (si.isEventController)
            {
                item1.ForeColor = Color.DarkOrchid;
            }
            else
            {
                item1.ForeColor = ConColors[si.Level].Color;

                if (item1.ForeColor == Color.Maroon)
                {
                    item1.ForeColor = Color.Red;
                }

                // Change the colors to be more visible on white if the background is white

                if (SpawnList.listView.BackColor == Color.White)
                {
                    if (item1.ForeColor == Color.White)
                    {
                        item1.ForeColor = Color.Black;
                    }
                    else if (item1.ForeColor == Color.Yellow)
                    {
                        item1.ForeColor = Color.Goldenrod;
                    }
                }
            }

            //if (alert)
            //    item1.Font = Settings.Default.ListFont;

            si.gone = 0;

            si.refresh = rnd.Next(0, 10);

            si.listitem = item1;
            return item1;
        }

        private float SpawnDistance(SPAWNINFO si)
        => (float)Math.Sqrt(((si.X - gamerInfo.X) * (si.X - gamerInfo.X)) + ((si.Y - gamerInfo.Y) * (si.Y - gamerInfo.Y)) + ((si.Z - gamerInfo.Z) * (si.Z - gamerInfo.Z)));

        private void NameChngOrDead(SPAWNINFO si, SPAWNINFO mob)
        {
            var newname = RegexHelper.FixMobName(si.Name);

            var oldname = RegexHelper.FixMobName(mob.Name);
            mob.listitem.Text = mob.listitem.Text.Replace(oldname, newname);

            if (!si.IsPlayer && (si.Type == 2 || si.Type == 3))
            {
                // Corpses - lose alerts on map

                si.isCorpse = true;

                si.isHunt = false;

                si.isCaution = false;

                si.isDanger = false;

                si.isAlert = false;

                // moved the type change before this.  So now only trigger kills
                // for name changes of corpses.
                mobsTimers.Kill(mob);
            }

            mob.Name = si.Name;
        }

        private static void LookupBoxMatch(SPAWNINFO si, MainForm f1)
        {
            si.isLookup = false;
            if (f1.toolStripLookupBox.Text.Length > 1
                && f1.toolStripLookupBox.Text != dflt
                && RegexHelper.GetRegex(f1.toolStripLookupBox.Text).Match(si.Name).Success)
            {
                si.isLookup = true;
            }

            if (f1.toolStripLookupBox1.Text.Length > 1
                && f1.toolStripLookupBox1.Text != dflt
                && RegexHelper.GetRegex(f1.toolStripLookupBox1.Text).Match(si.Name).Success)
            {
                si.isLookup = true;
            }

            if (f1.toolStripLookupBox2.Text.Length > 1
                && f1.toolStripLookupBox2.Text != dflt
                && RegexHelper.GetRegex(f1.toolStripLookupBox2.Text).Match(si.Name).Success)
            {
                si.isLookup = true;
            }

            if (f1.toolStripLookupBox3.Text.Length > 1
                && f1.toolStripLookupBox3.Text != dflt
                && RegexHelper.GetRegex(f1.toolStripLookupBox3.Text).Match(si.Name).Success)
            {
                si.isLookup = true;
            }

            if (f1.toolStripLookupBox4.Text.Length > 1
                && f1.toolStripLookupBox4.Text != dflt
                && RegexHelper.GetRegex(f1.toolStripLookupBox4.Text).Match(si.Name).Success)
            {
                si.isLookup = true;
            }
        }

        private void OwnerFlag(SPAWNINFO si, ListViewItem item1)
        {
            if (si.OwnerID > 0)
            {
                if (mobsHashTable.ContainsKey(si.OwnerID))
                {
                    SPAWNINFO owner = (SPAWNINFO)mobsHashTable[si.OwnerID];
                    item1.SubItems.Add(RegexHelper.FixMobName(owner.Name));
                }
                else
                {
                    item1.SubItems.Add(si.OwnerID.ToString());
                }
            }
            else
            {
                item1.SubItems.Add("");
            }
        }

        public void UpdateMobListColors()
        {
            if (mobsHashTable != null)
            {
                foreach (SPAWNINFO si in mobsHashTable.Values)
                {
                    if (si.listitem != null)
                    {
                        if (si.Type == 2 || si.Type == 3 || si.isLDONObject)
                        {
                            si.listitem.ForeColor = Color.Gray;
                        }
                        else if (si.isEventController)
                        {
                            si.listitem.ForeColor = Color.DarkOrchid;
                        }
                        else
                        {
                            si.listitem.ForeColor = ConColors[si.Level].Color;

                            if (si.listitem.ForeColor == Color.Maroon)
                            {
                                si.listitem.ForeColor = Color.Red;
                            }

                            // Change the colors to be more visible on white if the background is white

                            if (Settings.Default.ListBackColor == Color.White)
                            {
                                if (si.listitem.ForeColor == Color.White)
                                {
                                    si.listitem.ForeColor = Color.Black;
                                }
                                else if (si.listitem.ForeColor == Color.Yellow)
                                {
                                    si.listitem.ForeColor = Color.Goldenrod;
                                }
                            }

                            if (Settings.Default.ListBackColor == Color.Black && si.listitem.ForeColor == Color.Black)
                            {
                                si.listitem.ForeColor = Color.White;
                            }
                        }
                    }
                }
            }
        }

        private bool FindMatches(ArrayList exps, string mobname, bool NoneOnMatch,
             bool TalkOnMatch, string TalkDescr, bool PlayOnMatch, string AudioFile,
             bool BeepOnMatch, bool MatchFullText)
        {
            var alert = false;
            MainForm f1 = null;
            foreach (string str in exps)
            {
                var matched = false;

                // if "match full text" is ON...

                if (MatchFullText)
                {
                    if (string.Compare(mobname, str, true) == 0)
                    {
                        matched = true;
                    }
                }
                else if (RegexHelper.IsSubstring(mobname, str))
                {
                    matched = true;
                }
                // if item has been matched...

                if (matched)
                {
                    if (!NoneOnMatch && f1.playAlerts)
                    {
                        AudioMatch(mobname, TalkOnMatch, TalkDescr, PlayOnMatch, AudioFile, BeepOnMatch);
                    }

                    alert = true;

                    break;
                }
            }

            return alert;
        }

        private static void AudioMatch(string mobname, bool TalkOnMatch, string TalkDescr, bool PlayOnMatch, string AudioFile, bool BeepOnMatch)
        {
            if (TalkOnMatch)
            {
                ThreadStart threadDelegate = new ThreadStart(new Talker
                {
                    SpeakingText = $"{TalkDescr}, {RegexHelper.FixMobNameMatch(mobname)}, is up."
                }.SpeakText);

                new Thread(threadDelegate).Start();
            }
            else if (PlayOnMatch)
            {
                SAudio.Play(AudioFile.Replace("\\", "\\\\"));
            }
            else if (BeepOnMatch)
            {
                SafeNativeMethods.Beep(300, 100);
            }
        }

        public bool CheckMyCorpse(string mobname) => (mobname.Length < (gamerInfo.Name.Length + 14)) && (mobname.IndexOf(gamerInfo.Name) == 0);

        public void SaveMobs()
        {
            DateTime dt = DateTime.Now;

            var filename = $"{shortname} - {dt.Month}-{dt.Day}-{dt.Year} {dt.Hour}.txt";

            StreamWriter sw = new StreamWriter(filename, false);

            sw.Write("Name\tLevel\t Class\tRace\tLastname\tType\tInvis\tRun\tSpeed\tSpawnID\tX\tY\tZ\tHeading");

            foreach (SPAWNINFO si in mobsHashTable.Values)
            {
                sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}",
                             si.Name,
                             si.Level,
                             GetClass(si.Class),
                             GetRace(si.Race),
                             si.Lastname,
                             PrettyNames.GetSpawnType(si.Type),
                             PrettyNames.GetHideStatus(si.Hide),
                             si.SpeedRun,
                             si.SpawnID,
                             si.Y,
                             si.X,
                             si.Z,
                             CalcRealHeading(si));
            }

            sw.Close();
        }

        public void SetSelectedID(int id)
        {
            selectedID = id;
            SpawnX = -1.0f;
            SpawnY = -1.0f;
        }

        public void SetSelectedTimer(float x, float y)
        {
            SPAWNTIMER st = FindTimer(1.0f, x, y);

            if (st != null)
            {
                SPAWNINFO sp = FindMobTimer(st.SpawnLoc);

                selectedID = sp == null ? 99999 : sp.SpawnID;

                SpawnX = st.X;

                SpawnY = st.Y;
            }
        }

        public void SetSelectedGroundItem(float x, float y)
        {
            GroundItem gi = FindGroundItemNoFilter(1.0f, x, y);

            if (gi != null)
            {
                selectedID = 99999;

                SpawnX = gi.X;

                SpawnY = gi.Y;
            }
        }

        public string GetClass(int num) => ArrayIndextoStr(Classes, num);

        public string ItemNumToString(int num)
        {
            foreach (ListItem item in GroundSpawn)
            {
                if (item.ID.Equals(num))
                {
                    return item.Name;
                }
            }
            return num.ToString();
        }

        //        public string GuildNumToString(int num) => guildList.ContainsKey(num) ? ((ListItem)guildList[num]).Name : num.ToString();

        public string GetRace(int num) => num == 2250 ? "Interactive Object" : ArrayIndextoStr(Races, num);

        public void BeginProcessPacket()
        {
            newSpawns.Clear();
            newGroundItems.Clear();
        }

        public void ProcessSpawnList(ListViewPanel SpawnList)
        {
            try
            {
                if (newSpawns.Count > 0)
                {
                    if (Zoning)
                    {
                        SpawnList.listView.BeginUpdate();
                    }

                    ListViewItem[] items = new ListViewItem[newSpawns.Count];

                    var d = 0;

                    foreach (ListViewItem i in newSpawns)
                    {
                        items[d++] = i;
                    }

                    SpawnList.listView.Items.AddRange(items);
                    newSpawns.Clear();
                    if (Zoning)
                    {
                        SpawnList.listView.EndUpdate();
                    }
                }
            }
            catch (Exception ex) { LogLib.WriteLine("Error in ProcessSpawnList(): ", ex); }
        }

        public void ProcessGroundItemList(ListViewPanel GroundItemList)
        {
            try
            {
                if (newGroundItems.Count > 0)
                {
                    ListViewItem[] items = new ListViewItem[newGroundItems.Count];

                    var d = 0;

                    foreach (ListViewItem i in newGroundItems)
                    {
                        items[d++] = i;
                    }

                    GroundItemList.listView.Items.AddRange(items);
                    //GroundItemList.listView.Items.Add((string)items.GetValue(1));

                    newGroundItems.Clear();
                }
            }
            catch (Exception ex) { LogLib.WriteLine("Error in ProcessGroundItemList(): ", ex); }
        }

        public float CalcRealHeading(SPAWNINFO sp) => sp.Heading >= 0 && sp.Heading < 512 ? sp.Heading / 512 * 360 : 0;

        public void LoadSpawnInfo()
        {
            // Used to improve packet processing speed

            PrefixStars = Settings.Default.PrefixStars;

            AffixStars = Settings.Default.AffixStars;

            CorpseAlerts = Settings.Default.CorpseAlerts;

            MatchFullTextH = Settings.Default.MatchFullTextH;

            MatchFullTextC = Settings.Default.MatchFullTextC;

            MatchFullTextD = Settings.Default.MatchFullTextD;

            MatchFullTextA = Settings.Default.MatchFullTextA;

            HuntPrefix = Settings.Default.HuntPrefix;

            CautionPrefix = Settings.Default.CautionPrefix;

            DangerPrefix = Settings.Default.DangerPrefix;

            AlertPrefix = Settings.Default.AlertPrefix;
        }

        #region ProcessGamer

        private int gLastconLevel = -1;
        private int gconLevel;

        public void ProcessGamer(SPAWNINFO si, MainForm f1)//FrmMain f1)
        {
            try
            {
                gamerInfo.SpawnID = si.SpawnID;

                gamerInfo.Name = si.Name;
                f1.SetCharSelection(gamerInfo.Name);
                f1.SetTitle();

                gamerInfo.Lastname = si.Lastname;

                gamerInfo.X = si.X;
                gamerInfo.Y = si.Y;

                if (Settings.Default.FollowOption == FollowOption.Player)
                {
                    f1.ReAdjust();
                }

                gamerInfo.Z = si.Z;

                gamerInfo.Heading = si.Heading;

                gamerInfo.Hide = si.Hide;

                gamerInfo.SpeedRun = si.SpeedRun;
                gconLevel = Settings.Default.LevelOverride;
                if (gamerInfo.Level != si.Level)
                {
                    if (GConBaseName.Length > 0)
                    {
                        if (si.Level > gamerInfo.Level)
                        {
                            gconLevel += si.Level - gamerInfo.Level;
                        }
                        else
                        {
                            gconLevel -= gamerInfo.Level - si.Level;
                        }
                        if (gconLevel > 115)
                        {
                            gconLevel = 115;
                        }

                        if (gconLevel < 1)
                        {
                            gconLevel = -1;
                        }

                        gLastconLevel = gconLevel;
                        Settings.Default.LevelOverride = gconLevel;
                    }
                    gamerInfo.Level = si.Level;
                    FillConColors(f1);

                    // update mob list con colors

                    UpdateMobListColors();
                }
                if (gLastconLevel != gconLevel)
                {
                    gLastconLevel = gconLevel;
                    FillConColors(f1);
                    UpdateMobListColors();
                }
            }
            catch (Exception ex) { LogLib.WriteLine("Error in ProcessPlayer(): ", ex); }
        }

        #endregion ProcessGamer

        public void Clear()
        {
            mobsHashTable.Clear();
            itemcollection.Clear();
            mobtrails.Clear();
        }

        public void CollectMobTrails()
        {
            // Collect Mob Trails

            foreach (SPAWNINFO sp in GetMobsReadonly().Values)
            {
                if (sp.Type == 1)
                {
                    // Setup NPCs Trails

                    //add point to mobtrails arraylist if not already there

                    MobTrailPoint work = new MobTrailPoint
                    {
                        x = (int)sp.X,

                        y = (int)sp.Y
                    };

                    if (!mobtrails.Contains(work))
                    {
                        mobtrails.Add(work);
                    }
                }
            }
        }

        #region ColorOperations

        public void FillConColors(MainForm f1)//FrmMain f1)
        {
            int level;
            if (Settings.Default.LevelOverride == -1)
            {
                f1.toolStripLevel.Text = "Auto";
                level = gamerInfo.Level;
            }
            else
            {
                level = Settings.Default.LevelOverride;
                f1.toolStripLevel.Text = level.ToString();
            }
            YellowRange = 3;

            CyanRange = -5;

            GreenRange = (-1) * level;

            GreyRange = (-1) * level;

            // If using SoD/Titanium Con Colors
            VersionColorVariation(level);

            int c;
            // Set the Grey Cons
            for (c = 0; c < (GreyRange + level); c++)
            {
                ConColors[c] = new SolidBrush(Color.Gray);
            }

            // Set the Green Cons
            for (; c < (GreenRange + level); c++)
            {
                ConColors[c] = new SolidBrush(Color.Lime);
            }

            // Set the Light Blue Cons
            for (; c < (CyanRange + level); c++)
            {
                ConColors[c] = new SolidBrush(Color.Aqua);
            }

            // Set the Dark Blue Cons
            for (; c < level; c++)
            {
                ConColors[c] = new SolidBrush(Color.Blue);
            }

            // Set the Same Level Con
            ConColors[c++] = new SolidBrush(Color.White);

            // Yellow Cons
            for (; c < (level + YellowRange + 1); c++)
            {
                ConColors[c] = new SolidBrush(Color.Yellow);
            }

            // 4 levels of bright red
            ConColors[c++] = new SolidBrush(Color.Red);
            ConColors[c++] = new SolidBrush(Color.Red);
            ConColors[c++] = new SolidBrush(Color.Red);
            ConColors[c++] = new SolidBrush(Color.Red);

            // Set the remaining levels to dark red
            for (; c < 500; c++)
            {
                ConColors[c] = new SolidBrush(Color.Maroon);
            }
        }

        private void VersionColorVariation(int level)
        // Check for SoD, SoF or Real EQ con levels in use
        {
            var ConColorsFile = Path.Combine(Settings.Default.CfgDir, "ConLevels.Ini");
            if (Settings.Default.SoDCon)
            {
                SoDCon(level);
            }

            // If using SoF Con Colors
            else if (Settings.Default.SoFCon)
            {
                SoFCon(level);
            }
            else if (File.Exists(ConColorsFile))
            {
                ConLevelFile(level, ConColorsFile);
            }
            else if (Settings.Default.DefaultCon)
            {
                // Using Default Con Colors
                DefaultCon(level);
            }
        }

        private void ConLevelFile(int level, string ConColorsFile)
        {
            IniFile Ini = new IniFile(ConColorsFile);

            var sIniValue = Ini.ReadValue("Con Levels", level.ToString(), "0/0/0");
            var yellowLevels = Ini.ReadValue("Con Levels", "0", "3");
            var ConLevels = sIniValue.Split('/');

            GreyRange = Convert.ToInt32(ConLevels[0]) - level + 1;

            GreenRange = Convert.ToInt32(ConLevels[1]) - level + 1;

            CyanRange = Convert.ToInt32(ConLevels[2]) - level + 1;

            YellowRange = Convert.ToInt32(yellowLevels);
        }

        private void DefaultCon(int level)
        {
            CyanRange = -5;

            if (level < 16) // verified
            {
                GreyRange = -5;

                GreenRange = -5;
            }
            else if (level < 19) // verified
            {
                GreyRange = -6;

                GreenRange = -5;
            }
            else if (level < 21) // verified
            {
                GreyRange = -7;

                GreenRange = -5;
            }
            else if (level < 22) // verified
            {
                GreyRange = -7;

                GreenRange = -6;
            }
            else if (level < 25) // verified
            {
                GreyRange = -8;

                GreenRange = -6;
            }
            else if (level < 28) // verified
            {
                GreyRange = -9;

                GreenRange = -7;
            }
            else if (level < 29) // verified
            {
                GreyRange = -10;

                GreenRange = -7;
            }
            else if (level < 31) // verified
            {
                GreyRange = -10;

                GreenRange = -8;
            }
            else if (level < 33) // verified
            {
                GreyRange = -11;

                GreenRange = -8;
            }
            else if (level < 34) // verified
            {
                GreyRange = -11;

                GreenRange = -9;
            }
            else if (level < 37) // verified
            {
                GreyRange = -12;

                GreenRange = -9;
            }
            else if (level < 40) // verified
            {
                GreyRange = -13;

                GreenRange = -10;
            }
            else if (level < 41) // Verified
            {
                GreyRange = -14;

                GreenRange = -10;
            }
            else if (level < 43) // Verified
            {
                GreyRange = -14;

                GreenRange = -11;
            }
            else if (level < 45)  // Verified
            {
                GreyRange = -15;

                GreenRange = -11;
            }
            else if (level < 46)  // Verified
            {
                GreyRange = -15;

                GreenRange = -12;
            }
            else if (level < 49)  // Verified
            {
                GreyRange = -16;

                GreenRange = -12;
            }
            else if (level < 51) // Verified at 50
            {
                GreyRange = -17;

                GreenRange = -13;
            }
            else if (level < 53)
            {
                GreyRange = -18;

                GreenRange = -14;
            }
            else if (level < 57)
            {
                GreyRange = -20;

                GreenRange = -15;
            }
            else
            {
                GreyRange = -21;

                GreenRange = -16;
            }
        }

        private void SoFCon(int level)
        {
            YellowRange = 3;

            CyanRange = -5;

            if (level < 9)
            {
                GreyRange = -3;

                GreenRange = -7;
            }
            else if (level < 10)
            {
                GreyRange = -4;

                GreenRange = -3;
            }
            else if (level < 13)
            {
                GreyRange = -5;

                GreenRange = -3;
            }
            else if (level < 17)
            {
                GreyRange = -6;

                GreenRange = -4;
            }
            else if (level < 21)
            {
                GreyRange = -7;

                GreenRange = -5;
            }
            else if (level < 25)
            {
                GreyRange = -8;

                GreenRange = -6;
            }
            else if (level < 29)
            {
                GreyRange = -9;

                GreenRange = -7;
            }
            else if (level < 31)
            {
                GreyRange = -10;

                GreenRange = -8;
            }
            else if (level < 33)
            {
                GreyRange = -11;

                GreenRange = -8;
            }
            else if (level < 37)
            {
                GreyRange = -12;

                GreenRange = -9;
            }
            else if (level < 41)
            {
                GreyRange = -13;

                GreenRange = -10;
            }
            else if (level < 45)
            {
                GreyRange = -15;

                GreenRange = -11;
            }
            else if (level < 49)
            {
                GreyRange = -16;

                GreenRange = -12;
            }
            else if (level < 53)
            {
                GreyRange = -17;

                GreenRange = -13;
            }
            else if (level < 55)
            {
                GreyRange = -18;

                GreenRange = -14;
            }
            else if (level < 57)
            {
                GreyRange = -19;

                GreenRange = -14;
            }
            else
            {
                GreyRange = -20;

                GreenRange = -15;
            }
        }

        private void SoDCon(int level)
        {
            YellowRange = 2;

            GreyRange = (-1) * level;

            if (level < 9)
            {
                GreenRange = -3;

                CyanRange = -7;
            }
            else if (level < 13)
            {
                GreenRange = -5;

                CyanRange = -3;
            }
            else if (level < 17)
            {
                GreenRange = -6;

                CyanRange = -4;
            }
            else if (level < 21)
            {
                GreenRange = -7;

                CyanRange = -5;
            }
            else if (level < 25)
            {
                GreenRange = -8;

                CyanRange = -6;
            }
            else if (level < 29)
            {
                GreenRange = -9;

                CyanRange = -7;
            }
            else if (level < 31)
            {
                GreenRange = -10;

                CyanRange = -8;
            }
            else if (level < 33)
            {
                GreenRange = -11;

                CyanRange = -8;
            }
            else if (level < 37)
            {
                GreenRange = -12;

                CyanRange = -9;
            }
            else if (level < 41)
            {
                GreenRange = -13;

                CyanRange = -10;
            }
            else if (level < 45)
            {
                GreenRange = -15;

                CyanRange = -11;
            }
            else if (level < 49)
            {
                GreenRange = -16;

                CyanRange = -12;
            }
            else if (level < 53)
            {
                GreenRange = -17;

                CyanRange = -13;
            }
            else if (level < 55)
            {
                GreenRange = -18;

                CyanRange = -14;
            }
            else if (level < 57)
            {
                GreenRange = -19;

                CyanRange = -14;
            }
            else
            {
                GreenRange = -20;

                CyanRange = -15;
            }
        }

        public void CalculateMapLinePens()
        {
            if (lines != null)
            {
                Pen darkpen = new Pen(Color.Black);
                var alpha = Settings.Default.FadedLines * 255 / 100;

                foreach (MapLine mapline in lines)
                {
                    if (Settings.Default.ForceDistinct)
                    {
                        mapline.draw_color = GetDistinctColor(darkpen);
                        mapline.fade_color = new Pen(Color.FromArgb(alpha, mapline.draw_color.Color));
                    }
                    else
                    {
                        mapline.draw_color = GetDistinctColor(new Pen(mapline.color.Color));
                        mapline.fade_color = new Pen(Color.FromArgb(alpha, mapline.draw_color.Color));
                    }
                }
                SolidBrush distinctbrush = new SolidBrush(Color.Black);
                foreach (MapText maptxt in texts)
                {
                    maptxt.draw_color = Settings.Default.ForceDistinctText ? GetDistinctColor(distinctbrush) : GetDistinctColor(maptxt.color);
                    maptxt.draw_pen = new Pen(maptxt.draw_color.Color);
                }
            }
        }
        public SolidBrush GetDistinctColor(SolidBrush curBrush)
        {
            curBrush.Color = GetDistinctColor(curBrush.Color, Settings.Default.BackColor);

            return curBrush;
        }

        public Color GetDistinctColor(Color foreColor, Color backColor)
        {
            // make sure the fore + back color can be distinguished.

            const int ColorThreshold = 55;

            if (GetColorDiff(foreColor, backColor) >= ColorThreshold)
            {
                return foreColor;
            }
            else
            {
                Color inverseColor = GetInverseColor(foreColor);

                if (GetColorDiff(inverseColor, backColor) > ColorThreshold)
                {
                    return inverseColor;
                }
                else //' if we have grey rgb(127,127,127) the inverse is the same so return black...
                {
                    return Color.Black;
                }
            }
        }
        public Pen GetDistinctColor(Pen curPen)
        {
            curPen.Color = GetDistinctColor(curPen.Color, Settings.Default.BackColor);
            return curPen;
        }
        public Color GetDistinctColor(Color curColor) => GetDistinctColor(curColor, Settings.Default.BackColor);

        private int GetColorDiff(Color foreColor, Color backColor)
        {
            int lTmp;
            var lColDiff = 0;

            lTmp = Math.Abs(backColor.R - foreColor.R);

            lColDiff = Math.Max(lColDiff, lTmp);

            lTmp = Math.Abs(backColor.G - foreColor.G);

            lColDiff = Math.Max(lColDiff, lTmp);

            lTmp = Math.Abs(backColor.B - foreColor.B);

            return Math.Max(lColDiff, lTmp);
        }

        private Color GetInverseColor(Color foreColor) => Color.FromArgb((int)(192 - (foreColor.R * 0.75)), (int)(192 - (foreColor.G * 0.75)), (int)(192 - (foreColor.B * 0.75)));
        #endregion ColorOperations
    }
}