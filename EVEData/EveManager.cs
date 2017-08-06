﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Web;
using System.Diagnostics;
using System.Text;

namespace SMT.EVEData
{
    public class EveManager
    {

        public const string CLIENT_ID = "ace68fde71fc4749bb27f33e8aad0b70";
        public const string SECRET_KEY = "kT7fsRg8WiRb9lujedQVyKEPgaJr40hevUdTdKaF";



        static EveManager s_Instance;

        public static void SetInstance(EveManager instance)
        {
                s_Instance = instance;
        }



        public static EveManager GetInstance()
        {
            if (s_Instance == null)
            {
                s_Instance = new EveManager();
            }

            return s_Instance;
        }



        /// <summary>
        /// Master List of Regions
        /// </summary>
        public List<RegionData> Regions;

        /// <summary>
        /// Lookup from Internal ID to Name
        /// </summary>
        public SerializableDictionary<String, String> SystemIDToName;


        /// <summary>
        /// List of Jumb bridoges
        /// </summary>


        /// <summary>
        /// Folder to cache dotland svg's etc to
        /// </summary>
        [XmlIgnoreAttribute]
        public string DataCacheFolder;


        #region Characters
        [XmlIgnoreAttribute]
        public ObservableCollection<Character> LocalCharacters;

        public void LoadCharacters()
        {
            string dataFilename = AppDomain.CurrentDomain.BaseDirectory + @"\Characters.dat";
            if(!File.Exists(dataFilename))
            {
                return;
            }

            try
            {
                ObservableCollection<Character> loadList;
                XmlSerializer xms = new XmlSerializer(typeof(ObservableCollection<Character>));

                FileStream fs = new FileStream(dataFilename, FileMode.Open);
                XmlReader xmlr = XmlReader.Create(fs);

                loadList = (ObservableCollection<Character>)xms.Deserialize(xmlr);

                foreach (Character c in loadList)
                {
                    c.ESIAccessToken = "";
                    c.ESIAccessTokenExpiry = DateTime.MinValue;
                    c.LocalChatFile = "";
                    c.Location = "";

                    c.Update();

                    LocalCharacters.Add(c);
                }
            }
            catch { }
        }

        public void SaveCharacters()
        {
            // save off only the ESI authenticated Characters so create a new copy to serialise from..

            ObservableCollection<Character> saveList = new ObservableCollection<Character>();

            foreach(Character c in LocalCharacters)
            {
                if(c.ESIRefreshToken != "")
                {
                    saveList.Add(c);
                }
            }

            XmlSerializer xms = new XmlSerializer(typeof(ObservableCollection<Character>));
            string dataFilename = AppDomain.CurrentDomain.BaseDirectory + @"\Characters.dat";

            using (TextWriter tw = new StreamWriter(dataFilename))
            {
                xms.Serialize(tw, saveList);
            }


        }

        #endregion



        #region JumpBridges
        [XmlIgnoreAttribute]
        public List<JumpBridge> JumpBridges;

        /// <summary>
        /// Load the jump bridge data from disk
        /// </summary>
        public void LoadJumpBridgeData()
        {
            JumpBridges = new List<JumpBridge>();
            string JumpBridgeData = AppDomain.CurrentDomain.BaseDirectory + @"\JumpBridges.txt";

            bool Friendly = true;

            if (File.Exists(JumpBridgeData))
            {
                StreamReader file = new StreamReader(JumpBridgeData);
                string line;
                while ((line = file.ReadLine()) != null)
                {

                    if (line.StartsWith("---HOSTILE---"))
                    {
                        Friendly = false;
                    }

                    // Skip comments 
                    if (line.StartsWith("#"))
                    {
                        continue;
                    }
                    string[] jbbits = line.Split(' ');

                    // malformed line
                    if (jbbits.Length < 5)
                    {
                        continue;
                    }

                    // in the form :
                    // FromSystem FromPlanetMoon <-> ToSystem ToPlanetMoon

                    string From = jbbits[0];
                    string FromData = jbbits[1];

                    string To = jbbits[3];
                    string ToData = jbbits[4];

                    if (DoesSystemExist(From) && DoesSystemExist(To))
                    {
                        JumpBridges.Add(new JumpBridge(From, FromData, To, ToData, Friendly));
                    }
                }
            }

        }

        #endregion


        #region IntelData
        [XmlIgnoreAttribute]
        private FileSystemWatcher IntelFileWatcher;

        [XmlIgnoreAttribute]
        private Dictionary<string, int> IntelFileReadPos;

        [XmlIgnoreAttribute]
        public BindingList<EVEData.IntelData> IntelDataList;

        [XmlIgnoreAttribute]
        public List<string> IntelFilters;


        public void SetupIntelWatcher()
        {
            IntelFilters = new List<string>();
            IntelDataList = new BindingList<IntelData>();
            string IntelFileFilter = AppDomain.CurrentDomain.BaseDirectory + @"\IntelChannels.txt";
            if (File.Exists(IntelFileFilter))
            {
                StreamReader file = new StreamReader(IntelFileFilter);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    line.Trim();
                    if(line != "")
                    {
                        IntelFilters.Add(line);
                    }
                }
            }


            IntelFileReadPos = new Dictionary<string, int>();

            string EveLogFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\EVE\logs\Chatlogs\";

            if (Directory.Exists(EveLogFolder))
            {
                IntelFileWatcher = new FileSystemWatcher(EveLogFolder);
                IntelFileWatcher.Filter = "*.txt";
                IntelFileWatcher.EnableRaisingEvents = true;
                IntelFileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                IntelFileWatcher.Changed += IntelFileWatcher_Changed;
            }


            /// -----------------------------------------------------------------
            /// SUPER HACK WARNING....
            /// 
            /// Start up a thread which just reads the text files in the eve log folder
            /// by opening and closing them it updates the sytem meta files which 
            /// causes the file watcher to operate correctly otherwise this data
            /// doesnt get updated until something other than the eve client reads these files
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                // loop forever
                while (true)
                {

                    DirectoryInfo di = new DirectoryInfo(EveLogFolder);
                    FileInfo[] Files = di.GetFiles("*.txt");
                    foreach (FileInfo file in Files)
                    {
                        bool readFile = false;
                        foreach (string IntelFilterStr in IntelFilters)
                        {
                            if (file.Name.Contains(IntelFilterStr))
                            {
                                readFile = true;
                                break;
                            }
                        }

                        // local files
                        if (file.Name.Contains("Local_"))
                        {
                            readFile = true;
                        }



                        // only read files from the last day
                        if (file.CreationTime > DateTime.Now.AddDays(-1) && readFile)
                        {
                            FileStream ifs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            ifs.Seek(0, SeekOrigin.End);
                            Thread.Sleep(100);
                            ifs.Close();
                            Thread.Sleep(200);
                        }
                    }
                    Thread.Sleep(1000);
                }
            }).Start();
            /// END SUPERHACK
            /// -----------------------------------------------------------------


        }

        private void IntelFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            string ChangedFile = e.FullPath;

            bool ProcessFile = false;
            bool LocalChat = false;

            foreach (string IntelFilterStr in IntelFilters)
            {
                if (ChangedFile.Contains(IntelFilterStr))
                {
                    ProcessFile = true;
                    break;
                }
            }

            if (ChangedFile.Contains("Local_"))
            {
                LocalChat = true;
                ProcessFile = true;
            }


            if (ProcessFile)
            {
                FileStream ifs = new FileStream(ChangedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                StreamReader file = new StreamReader(ifs);

                int FileReadFrom = 0;

                // have we seen this file before
                if (IntelFileReadPos.Keys.Contains<string>(ChangedFile))
                {
                    FileReadFrom = IntelFileReadPos[ChangedFile];
                }
                else
                {
                    if (LocalChat)
                    {
                        string System = "";
                        string CharacterName = "";


                        // read the iniital block
                        while (!file.EndOfStream)
                        {
                            string l = file.ReadLine();
                            FileReadFrom++;

                            if (l.Contains("Channel ID"))
                            {
                                string temp = l.Split(',')[1].Split(')')[0].Trim();
                                if (SystemIDToName.Keys.Contains(temp))
                                {
                                    System = SystemIDToName[temp];
                                }

                                // now can read the next line
                                l = file.ReadLine(); // should be the "Channel Name : Local"
                                l = file.ReadLine();

                                CharacterName = l.Split(':')[1].Trim();

                                bool AddChar = true;
                                foreach (EVEData.Character c in LocalCharacters)
                                {
                                    if (CharacterName == c.Name)
                                    {
                                        c.Location = System;
                                        c.LocalChatFile = ChangedFile;
                                        AddChar = false;
                                    }
                                }
                                if (AddChar)
                                {
                                    Application.Current.Dispatcher.Invoke((Action)(() =>
                                    {
                                        LocalCharacters.Add(new EVEData.Character(CharacterName, ChangedFile, System));
                                    }));

                                }

                                break;
                            }
                        }

                    }

                    while (file.ReadLine() != null)
                    {
                        FileReadFrom++;
                    }
                    FileReadFrom--;
                    file.BaseStream.Seek(0, SeekOrigin.Begin);
                }


                for (int i = 0; i < FileReadFrom; i++)
                {
                    file.ReadLine();
                }

                string line = file.ReadLine();
                while (line != null)
                {
                    FileReadFrom++;

                    if (LocalChat)
                    {
                        if (line.StartsWith("[") && line.Contains("EVE System > Channel changed to Local"))
                        {
                            string System = line.Split(':').Last().Trim();
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                foreach (EVEData.Character c in LocalCharacters)
                                {
                                    if (c.LocalChatFile == ChangedFile)
                                    {
                                        c.Location = System;
                                    }
                                }
                            }));
                        }

                    }
                    else
                    {
                        if (line.StartsWith("["))
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                // check if it is in the intel list already (ie if you have multiple clients running)
                                bool addToIntel = true;
                                foreach(EVEData.IntelData idl in IntelDataList)
                                {
                                    if(idl.RawIntelString == line)
                                    {
                                        addToIntel = false;
                                        break;
                                    }
                                }

                                if(addToIntel)
                                {
                                    EVEData.IntelData id = new EVEData.IntelData(line);

                                    foreach (string s in id.IntelString.Split(' '))
                                    {
                                        if (DoesSystemExist(s))
                                        {
                                            id.Systems.Add(s);
                                        }
                                    }
                                    IntelDataList.Insert(0, id);
                                }

                            }));
                        }
                    }

                    line = file.ReadLine();
                }

                IntelFileReadPos[ChangedFile] = FileReadFrom;
            }

        }


        #endregion


        #region Dotlan Init Stuff
        /// <summary>
        /// Scrape the maps from dotlan and initialise the region data from dotland
        /// </summary>
        public void InitFromDotLAN()
        {

            Regions = new List<RegionData>();

            Regions.Add(new RegionData("Aridia"));
            Regions.Add(new RegionData("Black Rise"));
            Regions.Add(new RegionData("The Bleak Lands"));
            Regions.Add(new RegionData("Branch"));
            Regions.Add(new RegionData("Cache"));
            Regions.Add(new RegionData("Catch"));
            Regions.Add(new RegionData("The Citadel"));
            Regions.Add(new RegionData("Cloud Ring"));
            Regions.Add(new RegionData("Cobalt Edge"));
            Regions.Add(new RegionData("Curse"));
            Regions.Add(new RegionData("Deklein"));
            Regions.Add(new RegionData("Delve"));
            Regions.Add(new RegionData("Derelik"));
            Regions.Add(new RegionData("Detorid"));
            Regions.Add(new RegionData("Devoid"));
            Regions.Add(new RegionData("Domain"));
            Regions.Add(new RegionData("Esoteria"));
            Regions.Add(new RegionData("Essence"));
            Regions.Add(new RegionData("Etherium Reach"));
            Regions.Add(new RegionData("Everyshore"));
            Regions.Add(new RegionData("Fade"));
            Regions.Add(new RegionData("Feythabolis"));
            Regions.Add(new RegionData("The Forge"));
            Regions.Add(new RegionData("Fountain"));
            Regions.Add(new RegionData("Geminate"));
            Regions.Add(new RegionData("Genesis"));
            Regions.Add(new RegionData("Great Wildlands"));
            Regions.Add(new RegionData("Heimatar"));
            Regions.Add(new RegionData("Immensea"));
            Regions.Add(new RegionData("Impass"));
            Regions.Add(new RegionData("Insmother"));
            Regions.Add(new RegionData("Kador"));
            Regions.Add(new RegionData("The Kalevala Expanse"));
            Regions.Add(new RegionData("Khanid"));
            Regions.Add(new RegionData("Kor-Azor"));
            Regions.Add(new RegionData("Lonetrek"));
            Regions.Add(new RegionData("Malpais"));
            Regions.Add(new RegionData("Metropolis"));
            Regions.Add(new RegionData("Molden Heath"));
            Regions.Add(new RegionData("Oasa"));
            Regions.Add(new RegionData("Omist"));
            Regions.Add(new RegionData("Outer Passage"));
            Regions.Add(new RegionData("Outer Ring"));
            Regions.Add(new RegionData("Paragon Soul"));
            Regions.Add(new RegionData("Period Basis"));
            Regions.Add(new RegionData("Perrigen Falls"));
            Regions.Add(new RegionData("Placid"));
            Regions.Add(new RegionData("Providence"));
            Regions.Add(new RegionData("Pure Blind"));
            Regions.Add(new RegionData("Querious"));
            Regions.Add(new RegionData("Scalding Pass"));
            Regions.Add(new RegionData("Sinq Laison"));
            Regions.Add(new RegionData("Solitude"));
            Regions.Add(new RegionData("The Spire"));
            Regions.Add(new RegionData("Stain"));
            Regions.Add(new RegionData("Syndicate"));
            Regions.Add(new RegionData("Tash-Murkon"));
            Regions.Add(new RegionData("Tenal"));
            Regions.Add(new RegionData("Tenerifis"));
            Regions.Add(new RegionData("Tribute"));
            Regions.Add(new RegionData("Vale of the Silent"));
            Regions.Add(new RegionData("Venal"));
            Regions.Add(new RegionData("Verge Vendor"));
            Regions.Add(new RegionData("Wicked Creek"));

            SystemIDToName = new SerializableDictionary<string, string>();

            // create folder cache
            WebClient webClient = new WebClient();



            // update the region cache
            foreach (RegionData rd in Regions)
            {
                string localSVG = DataCacheFolder + @"\" + rd.DotLanRef + ".svg";
                string RemoteSVG = @"http://evemaps.dotlan.net/svg/" + rd.DotLanRef + ".svg";

                bool NeedsDownload = true;

                if (File.Exists(localSVG))
                {
                    NeedsDownload = false;
                }

                if (NeedsDownload)
                {
                    webClient.DownloadFile(RemoteSVG, localSVG);

                    // throttle so we dont hammer the server
                    Thread.Sleep(100);
                }


                // parse the svg as xml
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.XmlResolver = null;
                FileStream fs = new FileStream(localSVG, FileMode.Open, FileAccess.Read);
                xmldoc.Load(fs);


                // get the svg/g/g sys use child nodes
                string systemsXpath = @"//*[@id='sysuse']";
                XmlNodeList xn = xmldoc.SelectNodes(systemsXpath);

                XmlNode sysUseNode = xn[0];
                foreach (XmlNode system in sysUseNode.ChildNodes)
                {
                    // extact the base from info from the g
                    string SystemID = system.Attributes["id"].Value.Substring(3);
                    float X = float.Parse(system.Attributes["x"].Value) + (float.Parse(system.Attributes["width"].Value) / 2.0f);
                    float Y = float.Parse(system.Attributes["y"].Value) + (float.Parse(system.Attributes["height"].Value) / 2.0f);


                    string systemnodepath = @"//*[@id='def" + SystemID + "']";
                    XmlNodeList snl = xmldoc.SelectNodes(systemnodepath);
                    XmlNode sysdefNode = snl[0];

                    XmlNode aNode = sysdefNode.ChildNodes[0];


                    string name;
                    bool HasStation = false;


                    // scan for <polygon> nodes as these are the npc station nodes
                    foreach( XmlNode sxn in sysdefNode.ChildNodes)
                    {
                        if(sxn.Name == "polygon")
                        {
                            HasStation = true;
                            break;
                        }
                    }


                    // SS Nodes for system nodes
                    XmlNodeList ssNodes = aNode.SelectNodes(@".//*[@class='ss']");
                    if (ssNodes[0] != null)
                    {
                        name = ssNodes[0].InnerText;
                        rd.Systems[name] = new System(name, SystemID, X, Y, rd.Name, HasStation);

                        SystemIDToName[SystemID] = name;
                    }
                    else
                    {
                        // er / es nodes are region and constellation links
                        XmlNodeList esNodes = aNode.SelectNodes(@".//*[@class='es']");
                        XmlNodeList erNodes = aNode.SelectNodes(@".//*[@class='er']");

                        if (esNodes[0] != null && erNodes[0] != null)
                        {

                            name = esNodes[0].InnerText;
                            string regionLinkName = erNodes[0].InnerText;

                            rd.Systems[name] = new System(name, SystemID, X, Y, regionLinkName, HasStation);

                            SystemIDToName[SystemID] = name;

                        }
                    }
                }

                // now catch the links - j
                string jXpath = @"//*[@class='j']";
                xn = xmldoc.SelectNodes(jXpath);

                foreach (XmlNode jump in xn)
                {
                    // will be in the format j-XXXXXXXX-YYYYYYYY
                    string id = jump.Attributes["id"].Value.Substring(2);

                    string systemOneID = id.Substring(0, 8);
                    string systemTwoID = id.Substring(9);

                    rd.Jumps.Add(new Link(SystemIDToName[systemOneID], SystemIDToName[systemTwoID], false));
                }


                // now catch the constellation links - jc
                string jcXpath = @"//*[@class='jc']";
                xn = xmldoc.SelectNodes(jcXpath);

                foreach (XmlNode jump in xn)
                {
                    // will be in the format j-XXXXXXXX-YYYYYYYY
                    string id = jump.Attributes["id"].Value.Substring(2);

                    string systemOneID = id.Substring(0, 8);
                    string systemTwoID = id.Substring(9);

                    rd.Jumps.Add(new Link(SystemIDToName[systemOneID], SystemIDToName[systemTwoID], true));
                }


                // now catch the region links - jr
                string jrXpath = @"//*[@class='jr']";
                xn = xmldoc.SelectNodes(jrXpath);

                foreach (XmlNode jump in xn)
                {
                    // will be in the format j-XXXXXXXX-YYYYYYYY
                    string id = jump.Attributes["id"].Value.Substring(2);

                    string systemOneID = id.Substring(0, 8);
                    string systemTwoID = id.Substring(9);

                    rd.Jumps.Add(new Link(SystemIDToName[systemOneID], SystemIDToName[systemTwoID], true));
                }
            }

            // now open up the eve static data export and extract some info from it
            string EveStaticDataSolarSystemFile = AppDomain.CurrentDomain.BaseDirectory + @"\mapSolarSystems.csv";
            if (File.Exists(EveStaticDataSolarSystemFile))
            {
                StreamReader file = new StreamReader(EveStaticDataSolarSystemFile);
                // read the headers..
                string line;
                line = file.ReadLine();
                while ((line = file.ReadLine()) != null)
                {

                    string[] bits = line.Split(',');



                    string SystemID = bits[2];
                    string SystemName =  bits[3]; // SystemIDToName[SystemID];

                    double X = Convert.ToDouble(bits[4]);
                    double Y = Convert.ToDouble(bits[5]);
                    double Z = Convert.ToDouble(bits[6]);
                    double Security = Convert.ToDouble(bits[21]);

                    System s = GetEveSystem(SystemName);
                    if(s != null)
                    {
                        s.ActualX = X;
                        s.ActualY = Y;
                        s.ActualZ = Z;
                        s.Security = Security;
                    }
                    else
                    {
                        Console.WriteLine("Failed to Find System {0}", SystemName);
                    }
                }

                // patch up the out of region data
                foreach (RegionData rr in Regions)
                {
                    foreach(System ss in rr.Systems.Values.ToList())
                    {
                        if(ss.Name!= rr.Name)
                        {
                            System actual = GetEveSystem(ss.Name);
                            ss.Security = actual.Security;
                        }
                    }
                }

            }


                // now serialise the class to disk
            XmlSerializer xms = new XmlSerializer(typeof(EveManager));
            string dataFilename = AppDomain.CurrentDomain.BaseDirectory + @"\RegionInfo.dat";

            using (TextWriter tw = new StreamWriter(dataFilename))
            {
                xms.Serialize(tw, this);
            }
        }

        #endregion



        /// <summary>
        /// Does the System Exist ?
        /// </summary>
        /// <param name="name">Name (not ID) of the system</param>
        public bool DoesSystemExist(string name)
        {
            return (GetEveSystem(name) != null);
        }


        /// <summary>
        /// Get a System object from the name, note : for regions which have other region systems in it wont return 
        /// them.. eg TR07-s is on the esoteria map, but the object corresponding to the feythabolis map will be returned
        /// </summary>
        /// <param name="name">Name (not ID) of the system</param>
        public System GetEveSystem(string name)
        {
            foreach (RegionData reg in Regions)
            {
                if (reg.DoesSystemExist(name))
                {
                    return reg.Systems[name];
                }
            }

            return null ;
        }

        public RegionData GetRegion(string name)
        {
            foreach (RegionData reg in Regions)
            {
                if (reg.Name == name)
                {
                    return reg;
                }
            }
            return null;
        }


        #region ESI Data



        public string GetESILogonURL()
        {
            UriBuilder esiLogonBuilder = new UriBuilder("https://login.eveonline.com/oauth/authorize/");

            var esiQuery = HttpUtility.ParseQueryString(esiLogonBuilder.Query);
            esiQuery["response_type"] = "code";
            esiQuery["client_id"] = CLIENT_ID;
            esiQuery["redirect_uri"] = @"eveauth-smt://callback";
            esiQuery["scope"] = "publicData characterLocationRead characterNavigationWrite remoteClientUI esi-location.read_location.v1 esi-location.read_ship_type.v1 esi-ui.write_waypoint.v1 esi-characters.read_standings.v1 esi-location.read_online.v1";


            esiQuery["state"] = Process.GetCurrentProcess().Id.ToString();

            esiLogonBuilder.Query = esiQuery.ToString();
            // old way... Process.Start();

            return esiLogonBuilder.ToString();


        }

        public bool HandleEveAuthSMTUri(Uri uri)
        {
            // parse the uri
            var Query = HttpUtility.ParseQueryString(uri.Query);

            if (Query["state"] == null || Int32.Parse(Query["state"]) != Process.GetCurrentProcess().Id)
            {
                // this query isnt for us..
                return false;
            }

            if (Query["code"] == null)
            {
                // we're missing a query code
                return false;
            }


            // now we have the initial uri call back we can verify the auth code

            string url = @"https://login.eveonline.com/oauth/token";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Post;
            request.Timeout = 20000;
            request.Proxy = null;

            string code = Query["code"];
            string authHeader = CLIENT_ID + ":" + SECRET_KEY;
            string authHeader_64 = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader));

            request.Headers[HttpRequestHeader.Authorization] = authHeader_64;

            var httpData = HttpUtility.ParseQueryString(string.Empty); ;
            httpData["grant_type"] = "authorization_code";
            httpData["code"] = code;

            string httpDataStr = httpData.ToString();
            byte[] data = UTF8Encoding.UTF8.GetBytes(httpDataStr);
            request.ContentLength = data.Length;
            request.ContentType = "application/x-www-form-urlencoded";

            var stream = request.GetRequestStream();
            stream.Write(data, 0, data.Length);


            request.BeginGetResponse(new AsyncCallback(ESIValidateAuthCodeCallback), request);

            return true;
        }

        // since we dont know what character these tokens belong too until we check just store them..
        private string PendingAccessToken;
        private string PendingTokenType;
        private string PendingExpiresIn;
        private string PendingRefreshToken;

        private void ESIValidateAuthCodeCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult))
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        //Need to return this response 
                        string strContent = sr.ReadToEnd();

                        JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));
                        while (jsr.Read())
                        {
                            if (jsr.TokenType == JsonToken.StartObject)
                            {
                                JObject obj = JObject.Load(jsr);
                                PendingAccessToken = obj["access_token"].ToString();
                                PendingTokenType = obj["token_type"].ToString();
                                PendingExpiresIn = obj["expires_in"].ToString();
                                PendingRefreshToken = obj["refresh_token"].ToString();

                                // now requests the character information

                                string url = @"https://login.eveonline.com/oauth/verify";
                                HttpWebRequest verifyRequest = (HttpWebRequest)WebRequest.Create(url);
                                verifyRequest.ContentType = 
                                verifyRequest.Method = WebRequestMethods.Http.Get;
                                verifyRequest.Timeout = 20000;
                                verifyRequest.Proxy = null;
                                string authHeader = "Bearer " + PendingAccessToken;

                                verifyRequest.Headers[HttpRequestHeader.Authorization] = authHeader;


                                verifyRequest.BeginGetResponse(new AsyncCallback(ESIVerifyAccessCodeCallback), verifyRequest);

                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                /// ....
            }
        }
        private void ESIVerifyAccessCodeCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult))
                {

                    Stream verifyStream = response.GetResponseStream();
                    using (StreamReader srr = new StreamReader(verifyStream))
                    {
                        string verifyResult = srr.ReadToEnd();

                        JsonTextReader jsr = new JsonTextReader(new StringReader(verifyResult));
                        while (jsr.Read())
                        {
                            if (jsr.TokenType == JsonToken.StartObject)
                            {
                                JObject obj = JObject.Load(jsr);
                                string CharacterID = obj["CharacterID"].ToString();
                                string CharacterName = obj["CharacterName"].ToString();
                                string TokenType = obj["TokenType"].ToString();
                                string CharacterOwnerHash = obj["CharacterOwnerHash"].ToString();
                                string ExpiresOn = obj["ExpiresOn"].ToString();

                                // now find the matching character and update..
                                Character esiChar = null;
                                foreach(Character c in LocalCharacters)
                                {
                                    if(c.Name == CharacterName)
                                    {
                                        esiChar = c;
                                    }
                                }

                                if(esiChar == null)
                                {
                                    esiChar = new Character(CharacterName, "", "");


                                    Application.Current.Dispatcher.Invoke((Action)(() =>
                                    {
                                        LocalCharacters.Add(esiChar);
                                    }));

                                }

                                esiChar.ESIRefreshToken = PendingRefreshToken;
                                esiChar.ESILinked = true;
                                esiChar.ESIAccessToken = PendingAccessToken;
                                esiChar.ESIAccessTokenExpiry = DateTime.Parse(ExpiresOn);
                                esiChar.ID = CharacterID;

                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                /// ....
            }

        }
        



        /// <summary>
        /// Start the ESI download for the kill info
        /// </summary>
        public void StartUpdateKillsFromESI()
        {
            string url = @"https://esi.tech.ccp.is/latest/universe/system_kills/?datasource=tranquility";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            request.BeginGetResponse(new AsyncCallback(ESIKillsReadCallback), request);

        }


        /// <summary>
        /// ESI Result Response 
        /// </summary>
        /// <param name="asyncResult"></param>
        private void ESIKillsReadCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult))
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        //Need to return this response 
                        string strContent = sr.ReadToEnd();

                        JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));

                        // JSON feed is now in the format : [{ "system_id": 30035042, "ship_kills": 103, "npc_kills": 103, "pod_kills": 0},
                        while (jsr.Read())
                        {
                            if (jsr.TokenType == JsonToken.StartObject)
                            {
                                JObject obj = JObject.Load(jsr);
                                string SystemID = obj["system_id"].ToString();
                                string ShipKills = obj["ship_kills"].ToString();
                                string NPCKills = obj["npc_kills"].ToString();
                                string PodKills = obj["pod_kills"].ToString();


                                if(SystemIDToName[SystemID] != null)
                                {
                                    System es = GetEveSystem(SystemIDToName[SystemID]);
                                    if(es != null)
                                    {
                                        es.ShipKillsLastHour = int.Parse(ShipKills);
                                        es.PodKillsLastHour = int.Parse(PodKills);
                                        es.NPCKillsLastHour = int.Parse(NPCKills);

                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                /// ....
            }
        }


        /// <summary>
        /// Start the ESI download for the Jump info
        /// </summary>
        public void StartUpdateJumpsFromESI()
        {
            string url = @"https://esi.tech.ccp.is/latest/universe/system_jumps/?datasource=tranquility";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;

            request.BeginGetResponse(new AsyncCallback(ESIJumpsReadCallback), request);

        }


        /// <summary>
        /// ESI Result Response 
        /// </summary>
        /// <param name="asyncResult"></param>
        private void ESIJumpsReadCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult))
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        //Need to return this response 
                        string strContent = sr.ReadToEnd();

                        JsonTextReader jsr = new JsonTextReader(new StringReader(strContent));

                        // JSON feed is now in the format : [{ "system_id": 30035042, "ship_jumps": 103},
                        while (jsr.Read())
                        {
                            if (jsr.TokenType == JsonToken.StartObject)
                            {
                                JObject obj = JObject.Load(jsr);
                                string SystemID = obj["system_id"].ToString();
                                string ship_jumps = obj["ship_jumps"].ToString();


                                if (SystemIDToName[SystemID] != null)
                                {
                                    System es = GetEveSystem(SystemIDToName[SystemID]);
                                    if (es != null)
                                    {
                                        es.JumpsLastHour = int.Parse(ship_jumps);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                /// ....
            }
        }


        #endregion


        public void StartUpdateCharacterThread()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                // loop forever
                while (true)
                {

                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        foreach (Character c in LocalCharacters)
                        {
                            c.Update();
                        }
                    }));
                    Thread.Sleep(5000);
                }
            }).Start();
        }



        public EveManager()
        {
            LocalCharacters = new ObservableCollection<Character>();
            LoadCharacters();

            // ensure we have the cache folder setup
            DataCacheFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\SMTCache";
            if (!Directory.Exists(DataCacheFolder))
            {
                Directory.CreateDirectory(DataCacheFolder);
            }
            string WebCacheFoilder = DataCacheFolder + "\\WebCache"; 
            if (!Directory.Exists(WebCacheFoilder))
            {
                Directory.CreateDirectory(WebCacheFoilder);
            }

            string PortraitCacheFoilder = DataCacheFolder + "\\Portraits";
            if (!Directory.Exists(PortraitCacheFoilder))
            {
                Directory.CreateDirectory(PortraitCacheFoilder);
            }

            string LogosFoilder = DataCacheFolder + "\\Logos";
            if (!Directory.Exists(LogosFoilder))
            {
                Directory.CreateDirectory(LogosFoilder);
            }




            // start the character update thread
            StartUpdateCharacterThread();
        }

        

    }
}
