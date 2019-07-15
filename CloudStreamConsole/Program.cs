using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using Jint;

using c = CloudStreamConsole.CloudStream;

namespace CloudStreamConsole
{

    public class Program
    {
        static string searchedFor = "";
        static int autoC = -1;
        static int autoE = -1;
        static int autoVlc = -1;
        static string[] quitCommands = { "-exit", "-quit", "-q", "-e" };
        static string[] helpCommands = { "-help", "-h" };
        static string helpResult =
@"movie, tv, anime [PREFIX, only searches from one provider] 
[SEARCH FOR] 
-c [NUMBER, autoselects a searchresult] 
-e [NUMBER, autoselects an episode] 
-l [NONE, autoselects the latest episode], 
-vlc [NONE, autolaunch vlc with link]
-mpv [NONE, autolaunch mpv with link] (Linux Only)

Example: [ movie iron man -c 8 -vlc ] Launches iron man with vlc
Example: [ anime death note -c 2 -e 1 -mpv ] Launches first episode of death note with mpv
Example: [ the orville -c 2 -l ] Loads the links for the latest episode of the orville 
Example: [ movie iron man -vlc ] Autoplays a movie with vlc when you select the movie
";

        static void Main(string[] arg)
        {
            string totalArg = "";

            for (int i = 0; i < arg.Length; i++) {
                //Console.WriteLine(arg[i]);
                totalArg += arg[i] + " ";
            }


            //Console.WriteLine("Starting!");

            c.SearchProgressChanged += CloudStream_SearchProgressChanged;
            c.LinksAdded += CloudStream_LinksAdded;
            c.LinkProgressChanged += CloudStream_LinkProgressChanged;

            c.SearchDone += CloudStream_SearchDone;
            c.LinksDone += CloudStream_LinksDone;

            while (true) {
                string searchFor = totalArg == "" ? Console.ReadLine() : totalArg;
                bool skip = false;
                for (int i = 0; i < quitCommands.Length; i++) {
                    if (searchFor == quitCommands[i]) {
                        Environment.Exit(0);
                    }
                }
                for (int i = 0; i < helpCommands.Length; i++) {
                    if (searchFor.Contains(helpCommands[i])) {
                        if (!skip) {
                            Console.WriteLine(helpResult);
                            skip = true;
                        }
                    }
                }
                if (!skip) {


                    searchedFor = searchFor.ToLower();
                    // tv the orville -s 2 -l

                    string[] pre = { "movie", "tv", "anime" };

                    int[] res = { 0, 8, 1 };
                    //int[] res = { -1, 4, 2 };
                    for (int i = 0; i < c.settingsSearch.Length; i++) {
                        c.settingsSearch[i] = false;
                    }
                    bool any = false;
                    for (int i = 0; i < pre.Length; i++) {
                        if (searchedFor.StartsWith(pre[i])) {
                            c.settingsSearch[res[i]] = true;
                            any = true;
                            searchedFor = searchedFor.Replace(pre[i] + " ", "");
                        }
                    }

                    autoC = -1;
                    autoE = -1;

                    if (searchedFor.Contains("-c ")) {
                        string chosen = c.FindHTML(searchedFor, "-c ", " ");
                        if (chosen == "") {
                            chosen = searchedFor.Substring(searchedFor.IndexOf("-c") + 3, searchedFor.Length - searchedFor.IndexOf("-c") - 3);
                        }
                        try {
                            autoC = int.Parse(chosen);
                        }
                        catch (Exception) {

                        }
                        searchedFor = searchedFor.Replace("-c " + chosen, "");
                    }
                    if (searchedFor.Contains("-l")) {
                        autoE = -2;
                        searchedFor = searchedFor.Replace("-l", "");

                    }
                    if (searchedFor.Contains("-e ")) {
                        string chosen = c.FindHTML(searchedFor, "-e ", " ");
                        if (chosen == "") {
                            chosen = searchedFor.Substring(searchedFor.IndexOf("-e") + 3, searchedFor.Length - searchedFor.IndexOf("-e") - 3);
                        }
                        try {
                            autoE = int.Parse(chosen);
                        }
                        catch (Exception) {

                        }
                        searchedFor = searchedFor.Replace("-e " + chosen, "");
                    }

                    if (!any) {
                        for (int i = 0; i < c.settingsSearch.Length; i++) {
                            c.settingsSearch[i] = true;
                        }
                    }
                    autoVlc = -1;
                    if (searchedFor.Contains("-mpv")) {
                        searchedFor = searchedFor.Replace("-mpv", "");
                        autoVlc = 2;
                    }
                    if (searchedFor.Contains("-vlc")) {
                        searchedFor = searchedFor.Replace("-vlc", "");
                        autoVlc = 1;
                    }


                    c.Search(searchedFor);

                }
                if (totalArg != "") {
                    break;
                }
            }

            Environment.Exit(0);
        }

        private static void CloudStream_LinkProgressChanged(object sender, int e)
        {
            //Console.WriteLine("Progress: " + e);
        }

        private static void CloudStream_LinksAdded(object sender, int e)
        {
            for (int i = 0; i < e; i++) {
                int newLink = c.activeLinks.Count - e + i;
                if (newLink == 0) {
                    Console.Clear();
                }
                if (autoC == -1) {
                    Console.WriteLine(c.activeLinksNames[newLink] + " | " + c.activeLinks[newLink]);
                }
                else {
                    if (c.movieProvider[c.moveSelectedID] == 4 && c.activeLinksNames[newLink].StartsWith("Episode ")) {
                        Console.WriteLine(c.activeLinksNames[newLink] + " | " + c.activeLinks[newLink]);

                    }
                    else {

                        Console.WriteLine(c.activeLinks[newLink]);
                    }
                }
                Console.WriteLine("");
            }
        }


        private static void CloudStream_LinksDone(object sender, bool e)
        {
            if (c.movieProvider[c.moveSelectedID] == 4 && c.activeLinksNames[0].StartsWith("Episode ")) {
                bool doAuto = (autoE <= c.activeLinksNames.Count && autoE > 0) || autoE == -2;
                if (!doAuto) {
                    Console.WriteLine("Select episode, 1-" + c.activeLinksNames.Count);
                }
                int readInt = doAuto ? autoE : GetInt(c.activeLinksNames.Count, 1);
                if (readInt == -2) { readInt = c.activeLinksNames.Count; }

                Console.Clear();
                //c.activeLinks = new List<string>();
                //c.activeLinksNames = new List<string>();

                c.LoadNewSeries(readInt - 1);
            }
            else {
                if (autoE == -1 && autoC == -1) {
                    Console.WriteLine("Done!");
                }
            }

            if (autoVlc != -1 && c.activeLinks.Count > 0) {
                if (c.movieProvider[c.moveSelectedID] != 4 && c.movieProvider[c.moveSelectedID] != -1) {
                    PlayVLC(c.activeLinks[0]);
                }
                else {
                    int highestHD = 0;
                    for (int i = 0; i < c.activeLinksNames.Count; i++) {
                        if (c.activeLinksNames[i].StartsWith("HD") && !c.activeLinks[i].Contains("embed")) {
                            highestHD = i;
                        }
                    }
                    if (!c.activeLinksNames[highestHD].StartsWith("Episode ")) {
                        PlayVLC(c.activeLinks[highestHD]);
                    }

                }
            }

            if (!e) { Console.WriteLine("Link Search Incomplete, returned because of timeout"); }
        }

        static void PlayVLC(string inp)
        {
            if (System.Environment.OSVersion.Platform == PlatformID.Win32NT || System.Environment.OSVersion.Platform == PlatformID.Win32Windows) {
                if (autoVlc == 1) {
                    System.Diagnostics.Process VLC = new System.Diagnostics.Process();
                    VLC.StartInfo.FileName = "C:\\Program Files\\VideoLAN\\VLC\\vlc.exe";
                    VLC.StartInfo.Arguments = "-vvv " + inp;
                    try {
                        VLC.Start();
                    }
                    catch (Exception) {


                    }
                }
            }
            else if (System.Environment.OSVersion.Platform == PlatformID.Unix) {
                try {
                    ExecuteCommand((autoVlc == 1 ? "vlc" : "mpv") + " \"" + inp + "\"");
                }
                catch (Exception) {
                }
            }
        }

        public static void ExecuteCommand(string command)
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "/bin/bash";
            proc.StartInfo.Arguments = "-c \" " + command + " \"";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();

            while (!proc.StandardOutput.EndOfStream) {
                Console.WriteLine(proc.StandardOutput.ReadLine());
            }
        }

        static int GetInt(int max, int min = 0)
        {
            bool done = false;
            int readInt = 0;
            while (!done) {
                string line = Console.ReadLine();
                for (int i = 0; i < quitCommands.Length; i++) {
                    if (line == quitCommands[i]) {
                        Environment.Exit(0);
                    }
                }
                try {
                    readInt = int.Parse(line);
                    if (readInt >= min && readInt <= max) {
                        done = true;
                    }
                }
                catch (Exception) {

                }
            }
            return readInt;
        }

        private static void CloudStream_SearchDone(object sender, bool e)
        {
            Console.Clear();
            bool doAuto = (autoC <= c.movieTitles.Count && autoC > 0);

            if (!doAuto) {
                Console.WriteLine("0 | Go Back");
                for (int i = 0; i < c.movieTitles.Count; i++) {
                    Console.WriteLine((i + 1) + " | " + c.movieTitles[i]);
                }
                if (!e) { Console.WriteLine("Movie Search Incomplete, returned because of timeout"); }
            }
            int readInt = doAuto ? autoC : GetInt(c.movieTitles.Count);

            Console.Clear();
            if (readInt != 0) {
                if (!doAuto) {
                    Console.WriteLine("Loading links for " + c.movieTitles[readInt - 1]);
                }
                c.GetURLFromTitle(readInt - 1, epNum: autoE);
                //c.GetLinks();
            }


        }


        private static void CloudStream_SearchProgressChanged(object sender, int e)
        {
            if (autoC == -1 && autoE == -1) {
                if (e != 100) {
                    Console.Clear();
                    Console.WriteLine("Searching for: " + searchedFor);

                    int pgs = (int)Math.Round(e / 5f);
                    Console.WriteLine("[" + RepeatString("=", pgs) + RepeatString(".", 20 - pgs) + "] " + e + "%");
                }
            }
        }

        static string RepeatString(string inp, int rep)
        {
            return String.Concat(Enumerable.Repeat(inp, rep));
        }
    }

    public class CloudStream
    {
        public const bool useChromeCast = false;

        public static string searchFor = "";

        public static int thredNumber = 0;


        public static bool addProviderNames = true;
        public static string currentSearch = "";

        public static int linkProgress = 0;
        public static int searchProgress = 0;
        public static int timeout = 500; // 1000 = 10s
        static int timeoutUpdated = 500;

        /// <summary>
        /// e = progress in %
        /// </summary>
        public static event EventHandler<int> LinkProgressChanged;
        /// <summary>
        /// e = amount of new links added on call
        /// </summary>
        public static event EventHandler<int> LinksAdded;
        /// <summary>
        /// e = bool if it returned successful, default timeout of 10s
        /// </summary>
        public static event EventHandler<bool> LinksDone;
        /// <summary>
        /// e = progress in %
        /// </summary>
        public static event EventHandler<int> SearchProgressChanged;
        /// <summary>
        /// e = bool if it returned successful, default timeout of 10s
        /// </summary>
        public static event EventHandler<bool> SearchDone;


        static int lastLink = 0;

        public static void GetLinks(int titleID)
        {
            GetURLFromTitle(titleID);
        }

        static void OnProgressLinkChanged(int progress)
        {
            linkProgress = progress;
            LinkProgressChanged?.Invoke(null, progress);
            timeoutUpdated = timeout;


            if (lastLink > activeLinks.Count) { lastLink = 0; }
            LinksAdded?.Invoke(null, activeLinks.Count - lastLink);
            lastLink = activeLinks.Count;
        }

        static void OnProgressSearchChanged(int progress)
        {
            timeoutUpdated = timeout;
            searchProgress = progress;
            SearchProgressChanged?.Invoke(null, progress);
        }

        static void OnSearchDone(bool successful)
        {
            SearchDone?.Invoke(null, successful);
        }

        static void OnLinksDone(bool successful)
        {
            LinksDone?.Invoke(null, successful);
        }










        public const string SUBTITLE_PATH = "TempSubtitle.srt";
        public const bool SHOW_INFO_SUBTITLES = false;





        public static bool useSubtitles = false;

        public class Movie
        {
            public string title;
            public string year;
            public string rated;
            public string released;
            public string runtime;
            public string genre;
            public string plot;
            public string posterID;
            public string imdbRating;
            public string type;
        }

        public static Movie currentMovie = new Movie();
        public static List<string> movieTitles = new List<string>();
        public static List<string> fwordLink = new List<string>();
        public static List<string> activeLinks = new List<string>();
        public static List<Movie> moviesActive = new List<Movie>();
        public static List<bool> movieIsAnime = new List<bool>();
        public static List<int> movieProvider = new List<int>();
        public static List<string> activeLinksNames = new List<string>();
        public static List<string> activeSubtitles = new List<string>();
        public static List<string> activeSubtitlesNames = new List<string>();

        static readonly HttpClient _client = new HttpClient();
        public delegate void MethodInvoker();
        static int progress = 0;

        public delegate void DelegateWithParameters(int f, string realMoveLink);
        public delegate void DelegateWithParameters_search(string inp);
        public delegate void DelegateWithParameters_link(string sp, string ep, int cThred);
        public delegate void DelegateWithParameters_int(int i);


        static int urlServers = 10;
        static int otherServers = 1;

        static int urlsDone = 0;
        static string selectedTitle = "";
        //static int provider = 1;
        static readonly bool[] providerDubStatus = { true, false, false }; // sub/dub provider need to show status

        public static bool debug = false;
        public static bool clearNext = false;
        static bool isDub = false;
        // public static bool renderLinks = false;
        public static int REQUEST_WRITE_STORAGE = 112;






        public static string FindHTML(string all, string first, string end, int offset = 0)
        {
            if (all.IndexOf(first) == -1) {
                return "";
            }
            int x = all.IndexOf(first) + first.Length + offset;

            all = all.Substring(x, all.Length - x);
            int y = all.IndexOf(end);
            if (y == -1) {
                return "";
            }
            //  print(x + "|" + y);
            return all.Substring(0, y);
        }
        public static string FindReverseHTML(string all, string first, string end, int offset = 0)
        {
            int x = all.IndexOf(first);
            all = all.Substring(0, x);
            int y = all.LastIndexOf(end) + end.Length;
            //  print(x + "|" + y);
            return all.Substring(y, all.Length - y);

        }

        static void PraseJavaScript(string result)
        {
            realXToken = result;
        }

        static string realXToken = "";

        static async void DownloadGogo(string url)
        {
            print("Downloading gogo: " + url);

            WebClient client = new WebClient();
            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            try {
                string d = "";

                for (int i = 0; i < 5; i++) {
                    if (d == "") {
                        try {

                            d = client.DownloadString(url);

                        }
                        catch (System.Exception) {
                            print("Error gogo");
                            Thread.Sleep(1000);
                        }
                    }
                }



                if (d != "") {
                    print("Passed gogo download site");
                    tokenCode = FindHTML(d, "var tc = \'", "'");
                    _token = FindHTML(d, "_token\": \"", "\"");
                    string funct = "function _tsd_tsd_ds(" + FindHTML(d, "function _tsd_tsd_ds(", "</script>").Replace("\"", "'") + " log(_tsd_tsd_ds('" + tokenCode + "'))";
                    // print(funct);
                    if (funct == "function _tsd_tsd_ds( log(_tsd_tsd_ds(''))") {
                        print(d);
                    }
                    var engine = new Engine()
                    .SetValue("log", new Action<string>(PraseJavaScript));

                    engine.Execute(@funct);

                    GetAPI();

                }
                else {
                    print("Dident get gogo");
                    hdMovieIsDone = true;

                }

            }
            catch (System.Exception) {
                print("Error");
                hdMovieIsDone = true;
            }

        }
        static void DownloadMp4(string url)
        {
            WebClient client = new WebClient();
            string file = client.DownloadString(url);
            print(Getmp4uploadByFile(file));
        }

        static string GetgogoByFile(string _episode)
        {
            _episode = _episode.Replace("||||", "|");
            _episode = _episode.Replace("|||", "|");
            _episode = _episode.Replace("||", "|");
            _episode = _episode.Replace("||", "|");
            _episode = _episode.Replace("||", "|");

            string inter = FindHTML(_episode, "|mp4|", "|");
            /*
            if(inter.Length < 5) {
                inter = FindHTML(_episode, "|srt|", "|");

            }
            if (inter.Length < 5) {
                inter = FindHTML(_episode, "|vvad|", "|");

            }*/

            string server = "";
            string[] serverStart = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
            for (int s = 0; s < serverStart.Length; s++) {
                for (int i = 0; i < 100; i++) {
                    if (_episode.Contains("|" + serverStart[s] + i + "|")) {
                        server = serverStart[s] + i;
                    }
                }
            }

            for (int i = 0; i < 100; i++) {
                if (_episode.Contains("|www" + i + "|")) {
                    server = "www" + i;
                }
            }

            if (server == "") {
                return "Error, server not found";
            }
            if (inter == "") {
                return "Error, index not found";
            }
            if (inter.Length < 5) {
                inter = FindReverseHTML(_episode, "|" + server + "|", "|");
            }

            //https://v16.viduplayer.com/vxokfmpswoalavf4eqnivlo2355co6iwwgaawrhe7je3fble4vtvcgek2jha/v.mp4
            return "https://" + server + ".viduplayer.com/" + inter + "/v.mp4";
        }


        static string Getmp4uploadByFile(string _episode)
        {
            // print(_episode);

            _episode = _episode.Replace("||||", "|");
            _episode = _episode.Replace("|||", "|");
            _episode = _episode.Replace("||", "|");
            _episode = _episode.Replace("||", "|");
            _episode = _episode.Replace("||", "|");
            string server = "s1";
            for (int i = 0; i < 100; i++) {
                if (_episode.Contains("|s" + i + "|")) {
                    server = "s" + i;
                }
            }

            for (int i = 0; i < 100; i++) {
                if (_episode.Contains("|www" + i + "|")) {
                    server = "www" + i;
                }
            }

            int pos = _episode.IndexOf("vid|mp4|download");
            int offset = 18;

            if (pos == -1) {
                offset = 9;
                pos = _episode.IndexOf("vid|mp4");
            }
            if (pos == -1) {
                pos = _episode.IndexOf("mp4|video");
                offset = 11;
            }

            if (pos == -1) {
                return "";
                /*
                if (_episode.Contains("This video is no longer available due to a copyright claim")) {
                    break;
                }
                */
            }

            string r = "-1";
            string allEp = _episode.Substring(pos + offset - 1, _episode.Length - pos - offset + 1);
            if ((allEp.Substring(0, 30).Contains("|"))) {
                string rez = allEp.Substring(0, allEp.IndexOf("p")) + "p";
                r = rez;
                allEp = allEp.Substring(allEp.IndexOf("p") + 2, allEp.Length - allEp.IndexOf("p") - 2);
            }
            string urlLink = allEp.Substring(0, allEp.IndexOf("|"));

            allEp = allEp.Substring(urlLink.Length + 1, allEp.Length - urlLink.Length - 1);
            string typeID = allEp.Substring(0, allEp.IndexOf("|"));
            //             

            string _urlLink = FindReverseHTML(_episode, "|" + typeID + "|", "|");

            string mxLink = "https://" + server + ".mp4upload.com:" + typeID + "/d/" + _urlLink + "/video.mp4"; //  282 /d/qoxtvtduz3b4quuorgvegykwirnmt3wm3mrzjwqhae3zsw3fl7ajhcdj/video.mp4

            string addRez = "";
            if (r != "-1") {
                addRez += " | " + r;
            }

            if (typeID != "282") {
                //Error
            }
            else {

            }
            return mxLink;

        }

        static string Getmp4uploadByID(string id)
        {
            WebClient client = new WebClient();

            string realLink = "https://www.mp4upload.com/embed-" + id + ".html";
            try {
                string _episode = client.DownloadString(realLink);
                //print(_episode);
                return Getmp4uploadByFile(_episode);
            }
            catch (System.Exception) {

            }
            return "";

        }

        static string tempTitle = "";
        static string[] xtokens = { "JnVh9WbXF11289366", "Nv12Uh9WZHtUboJ0Rit2ShBX29584584" };
        static int currentXtoken = 0;
        static bool tryDownloadingTv = false;
        static void GetAPI(string _title = "")
        {
            System.Uri myUri = new System.Uri("https://gomostream.com/decoding_v3.php");
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(myUri);
            webRequest.Method = "POST";
            webRequest.Headers.Add("x-token", realXToken); //it2ShBXbtBn12198295 JnVh9WbXF11289366}

            webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
            webRequest.Headers.Add("DNT", "1");
            // webRequest.Headers.Add("Cookie", "XSRF-TOKEN=eyJpdiI6Ild0T2ZMTXByZDBvY3hCelVUdno1TXc9PSIsInZhbHVlIjoidGtFOGtFKzkxc1prWGtpWFpid29TbGJiZExYM0J6dWhTZ3JvNERBYUd3dFduTkx3S2hNYlFqUENBSTR5dFE3VzdsZDhENStwT1dBa05WTFo2XC82VTN3PT0iLCJtYWMiOiI2YTVkMjc3MmRmMjdjZTMwNzllYjdlYTMwOWFhYjlmN2Y4ZjE1MTdiYzEwNzUzNWYwYTUyZmNhY2EyNDE5YmZmIn0%3D; watchXyz_session=eyJpdiI6Iks2YlFDMnZpeTlmeFZCRFFacVROUXc9PSIsInZhbHVlIjoidFA1RWpaUmwwSWpjS0Y5NXNUdktwTnlSOWlWN2VIUzBqaDNiaWpDbytCeVFjaEhtK0M2T0VIaE1YNWJ3T2o0WHQwM1czMWdyTXI4bUtnRVg1ekZwTnc9PSIsIm1hYyI6ImIxMjMyMzQyZjc0OWNmMGJjNGNlZGRhZWM2N2U1YTliYzk1ZGJlOGNhZWQ0MTIyNjhmOGI3OTdhMTk1NTA5ZDAifQ%3D%3D");
            webRequest.Headers.Add("Cache-Control", "max-age=0, no-cache");
            webRequest.Headers.Add("TE", "Trailers");
            webRequest.Headers.Add("Pragma", "Trailers");
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), webRequest);

        }

        static void GetRequestStreamCallback(IAsyncResult callbackResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)callbackResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(callbackResult);

            string requestBody = true ? ("tokenCode=" + tokenCode + "&_token=" + _token) : "type=epis&xny=hnk&id=" + tokenCode;
            byte[] byteArray = Encoding.UTF8.GetBytes(requestBody);

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(GetResponseStreamCallback), webRequest);
        }

        static string HTMLGet(string uri, string referer, bool br = false)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            request.Method = "GET";
            request.ContentType = "text/html; charset=UTF-8";
            // webRequest.Headers.Add("Host", "trollvid.net");
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36";
            request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            request.Referer = referer;

            // webRequest.Headers.Add("Cookie", "__cfduid=dc6e854c3f07d2a427bca847e1ad5fa741562456483; _ga=GA1.2.742704858.1562456488; _gid=GA1.2.1493684150.1562456488; _maven_=popped; _pop_=popped");
            request.Headers.Add("TE", "Trailers");

            print("DA--");


            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                //  print(response.GetResponseHeader("set-cookie").ToString());


                // using (Stream stream = response.GetResponseStream())
                if (br) {
                    /*
                    using (BrotliStream bs = new BrotliStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress)) {
                        using (System.IO.MemoryStream msOutput = new System.IO.MemoryStream()) {
                            bs.CopyTo(msOutput);
                            msOutput.Seek(0, System.IO.SeekOrigin.Begin);
                            using (StreamReader reader = new StreamReader(msOutput)) {
                                string result = reader.ReadToEnd();

                                return result;

                            }
                        }
                    }
                    */
                    return "";
                }
                else {
                    using (Stream stream = response.GetResponseStream()) {
                        print("res" + response.StatusCode);
                        foreach (string e in response.Headers) {
                            print("Head: " + e);
                        }
                        print("LINK:" + response.GetResponseHeader("Set-Cookie"));
                        using (StreamReader reader = new StreamReader(stream)) {
                            string result = reader.ReadToEnd();
                            return result;
                        }
                    }
                }
            }

        }


        static void GetResponseStreamCallback(IAsyncResult callbackResult)
        {
            HttpWebRequest request = (HttpWebRequest)callbackResult.AsyncState;
            HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(callbackResult);
            using (StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream())) {
                string result = httpWebStreamReader.ReadToEnd();
                if (result != "") {
                    WebClient client = new WebClient();
                    //XbHP6duxDnD~1558891507~83.186.0.0~c5i0DlNs
                    string veryURL = FindHTML(result, "https:\\/\\/verystream.com\\/e\\/", "\"");
                    string gogoStream = FindHTML(result, "https:\\/\\/gomostream.com", "\"");
                    gogoStream = gogoStream.Replace(",,&noneemb", "").Replace("\\", "");
                    //["https:\/\/redirector.googlevideo.com\/videoplayback?id=3559ed25eabf374d&itag=22&source=picasa&begin=0&requiressl=yes&mm=30&mn=sn-4g5ednsy&ms=nxu&mv=u&pl=44&sc=yes&ei=oenhXN27O62S8gOI07awBA&susc=ph&app=fife&mime=video\/mp4&cnr=14&dur=7561.903&lmt=1557947360209526&mt=1558308859&ipbits=0&keepalive=yes&ratebypass=yes&ip=2a01:4f8:110:3447::2&expire=1558316481&sparams=ip,ipbits,expire,id,itag,source,requiressl,mm,mn,ms,mv,pl,sc,ei,susc,app,mime,cnr,dur,lmt&signature=9ABF4766E7C2573C0171F8D1C6F0761B289483F1B9704140A09090666F4EED83.25A52B55EF6070C25DB2608CFBF0994166D1CA477F85D0CD71994980976993C6&key=us0","
                    //https:\/\/gomostream.com\/vid\/?v=eyJ0eXBlIjoibW92aWUiLCJpbWQiOiJ0dDAzNzE3NDYiLCJfIjoiMTI5NjUwODg1NTE3IiwidG9rZW4iOiI5MzExNTIifQ,,&noneemb",
                    //"https:\/\/verystream.com\/e\/XbHP6duxDnD",
                    //"https:\/\/hqq.tv\/player\/embed_player.php?vid=WDc5TjcvTkxXTFpBbXRLYzFSazFMUT09&autoplay=no",
                    //"https:\/\/gounlimited.to\/embed-xjje1taiv02x.html",
                    //"https:\/\/vcstream.to\/embed\/5cb01ff305468"]

                    //https://gomostream.com/vid/?v=eyJ0eXBlIjoibW92aWUiLCJpbWQiOiJ0dDAzNzE3NDYiLCJfIjoiMTI5NjUwODg1NTE3IiwidG9rZW4iOiI5MzExNTIifQ
                    if (veryURL != "") {
                        string d = "";
                        try {
                            d = client.DownloadString("https://verystream.com/e/" + veryURL);

                        }
                        catch (Exception) {

                        }
                        if (d != "") {
                            // print(d);
                            print("-------------------- HD --------------------");
                            string url = tempTitle + "https://verystream.com/gettoken/" + FindHTML(d, "videolink\">", "<");
                            print(url);
                            if (!activeLinks.Contains(url) && url != "https://verystream.com/gettoken/") {
                                activeLinks.Add(url);
                                activeLinksNames.Add("HD Verystream");
                            }

                            print("--------------------------------------------");
                            print("");
                        }
                    }
                    else {
                        print("HD Link error (Read api)");
                        print("");
                    }
                    if (gogoStream != "") {
                        string d = client.DownloadString("https://gomostream.com" + gogoStream);
                        //print(d);
                        // print("https://gomostream.com" + gogoStream);
                        //https://v16.viduplayer.com/vxokfmpswoalavf4eqnivlo2355co6iwwgaawrhe7je3fble4vtvcgek2jha/v.mp4
                        print("-------------------- HD --------------------");
                        string url = tempTitle + GetgogoByFile(d);
                        print(url);
                        if (!activeLinks.Contains(url) && (!url.EndsWith(".viduplayer.com/urlset/v.mp4") && !url.EndsWith(".viduplayer.com/vplayer/v.mp4")) ) {
                            activeLinks.Add(url);
                            activeLinksNames.Add("HD Viduplayer");
                        }
                        print("--------------------------------------------");
                        print("");
                    }
                    else {
                        print("HD Link error (Read api)");
                        print("");

                    }
                    hdMovieIsDone = true;

                }
                else {
                    print("HD Link error (no response)");
                    hdMovieIsDone = true;

                }

            }
        }
        static string tokenCode = "";
        static string _token = "";
        static string _path;

        public delegate void DelegateWithParametersPage(int page, bool movie);
        public delegate void DelegateWithParametersLine(bool movie, string line, string searchString);

        static void DownloadAll(bool movie)
        {
            totalPages = movie ? movieLinks : tvLinks; //360 : 1938;
            string path = _path + "\\saveAllMovieData.txt";
            bool fileFound = false;
            try {
                StreamReader sr = new StreamReader(path);
                fileFound = true;
            }
            catch (System.Exception) {
            }
            if (!fileFound) {
                print("Error, file not found");

            }
            else {
                pagesDone = 0;
                for (int i = 0; i < totalPages; i++) {
                    DelegateWithParametersPage linkServer =
                       new DelegateWithParametersPage(GetPage);

                    IAsyncResult tag =
                        linkServer.BeginInvoke(i, movie, null, null);
                }
            }




        }
        static List<string> allPages = new List<string>();
        static int pagesDone = 0;
        static int totalPages = 360;
        static string __key = ""; // make your own key >:( 
        //[{"imdId":"tt0120366","slug":"traveller-1","title":"Traveller","quality":360},{"imdId":"tt9021234","slug":"the-legend-of-5-mile-cave","title":"The Legend of 5 Mile Cave","quality":720},{"imdId":"tt0318081","slug":"a-sound-of-thunder","title":"A Sound of Thunder","quality":1080},{"imdId":"tt8123728","slug":"the-guard-of-auschwitz","title":"The Guard of Auschwitz","quality":1080},{"imdId":"tt0316882","slug":"warriors-of-virtue-the-return-to-tao","title":"Warriors of Virtue: The Return to Tao","quality":360},{"imdId":"tt4975920","slug":"the-kid-1","title":"The Kid","quality":1080},{"imdId":"tt6563576","slug":"the-queen-s-corgi","title":"The Queen's Corgi","quality":1080},{"imdId":"tt9169592","slug":"wanda-sykes-not-normal","title":"Wanda Sykes: Not Normal","quality":1080},{"imdId":"tt6098380","slug":"isabelle-1","title":"Isabelle","quality":1080},{"imdId":"tt00382621","slug":"cr-nicas","title":"Cr\u00c3\u00b3nicas","quality":360},{"imdId":"tt0101252","slug":"29th-street","title":"29th Street","quality":360},{"imdId":"tt8917752","slug":"joy-1","title":"Joy","quality":1080},{"imdId":"tt10136680","slug":"after-maria","title":"After Maria","quality":1080},{"imdId":"tt9557416","slug":"lineage-of-lies","title":"Lineage of Lies","quality":720},{"imdId":"tt9366716","slug":"dagram","title":"DaGram","quality":1080},{"imdId":"tt9496886","slug":"goddesses-in-the-flames-of-war","title":"Goddesses In The Flames Of War","quality":1080},{"imdId":"tt9412268","slug":"furie","title":"Furie","quality":1080},{"imdId":"tt3810486","slug":"the-innocent-bastard","title":"The Innocent Bastard","quality":720},{"imdId":"tt0086232","slug":"sahara-3","title":"Sahara","quality":1080},{"imdId":"tt10171746","slug":"brendan-schaub-you-d-be-surprised","title":"Brendan Schaub: You'd Be Surprised","quality":1080},{"imdId":"tt0108041","slug":"sankofa","title":"Sankofa","quality":360},{"imdId":"tt10050766","slug":"bren-brown-the-call-to-courage","title":"Bren\u00c3\u00a9 Brown: The Call to Courage","quality":1080},{"imdId":"tt0039317","slug":"the-devil-thumbs-a-ride","title":"The Devil Thumbs a Ride","quality":720},{"imdId":"tt8547366","slug":"adam-carolla-not-taco-bell-material","title":"Adam Carolla: Not Taco Bell Material","quality":1080},{"imdId":"tt4960466","slug":"dark-highlands","title":"Dark Highlands","quality":720},{"imdId":"tt5037684","slug":"the-rainbow-experiment","title":"The Rainbow Experiment","quality":720},{"imdId":"tt7481192","slug":"sex-and-assassins","title":"Sex and Assassins","quality":720},{"imdId":"tt3602422","slug":"shed-of-the-dead","title":"Shed of the Dead","quality":1080},{"imdId":"tt3953420","slug":"king-of-beasts","title":"King of Beasts","quality":1080},{"imdId":"tt7399140","slug":"believe-2","title":"Believe","quality":1080},{"imdId":"tt7476122","slug":"jimbo","title":"Jimbo","quality":1080},{"imdId":"tt5157682","slug":"the-climb","title":"The Climb","quality":1080},{"imdId":"tt5952522","slug":"soul-sessions","title":"Soul Sessions","quality":1080},{"imdId":"tt5755622","slug":"nowhere-to-hide-1","title":"Nowhere to Hide","quality":1080},{"imdId":"tt2834944","slug":"hamlet-hutch","title":"Hamlet & Hutch","quality":720},{"imdId":"tt6857250","slug":"boonie-bears-entangled-worlds","title":"Boonie Bears: Entangled Worlds","quality":1080},{"imdId":"tt4465538","slug":"juveniles","title":"Juveniles","quality":1080},{"imdId":"tt7558166","slug":"trickster","title":"Trickster","quality":1080},{"imdId":"tt4082314","slug":"dark-sense","title":"Dark Sense","quality":1080},{"imdId":"tt10079698","slug":"attack-in-la","title":"Attack in LA","quality":1080},{"imdId":"tt7069740","slug":"barbie-dreamtopia-festival-of-fun","title":"Barbie Dreamtopia: Festival of Fun","quality":1080},{"imdId":"tt10244900","slug":"hailey-dean-mysteries-killer-sentence","title":"Hailey Dean Mysteries: Killer Sentence","quality":1080},{"imdId":"tt6236780","slug":"rottentail","title":"Rottentail","quality":1080},{"imdId":"tt6025356","slug":"muerte-tales-of-horror","title":"Muerte: Tales of Horror","quality":1080},{"imdId":"tt8875622","slug":"rituals-of-guilt","title":"Rituals of Guilt","quality":1080},{"imdId":"tt6597612","slug":"j-lia-ist","title":"J\u00c3\u00balia ist","quality":1080},{"imdId":"tt5929226","slug":"pure-hearts","title":"Pure Hearts","quality":720},{"imdId":"tt5859038","slug":"know-your-enemy","title":"Know Your Enemy","quality":1080},{"imdId":"tt6652828","slug":"daze-like-this","title":"Daze Like This","quality":720},{"imdId":"tt4179338","slug":"fourth-quarter","title":"Fourth Quarter","quality":1080},{"imdId":"tt7520184","slug":"swamp-zombies-2","title":"Swamp Zombies 2","quality":1080},{"imdId":"tt4876096","slug":"avenues","title":"Avenues","quality":1080},{"imdId":"tt4110388","slug":"superlopez","title":"Superlopez","quality":1080},{"imdId":"tt4942694","slug":"60-seconds-to-die","title":"60 Seconds to Die","quality":1080},{"imdId":"tt8959820","slug":"petta","title":"Petta","quality":1080},{"imdId":"tt6451260","slug":"paris-pigalle","title":"Paris Pigalle","quality":1080},{"imdId":"tt8073604","slug":"evil-bong-777","title":"Evil Bong 777","quality":1080},{"imdId":"tt7422552","slug":"funny-story","title":"Funny Story","quality":1080},{"imdId":"tt2981746","slug":"being-frank-the-chris-sievey-story","title":"Being Frank: The Chris Sievey Story","quality":360},{"imdId":"tt8836988","slug":"avengement","title":"Avengement","quality":1080},{"imdId":"tt5862166","slug":"the-poison-rose","title":"The Poison Rose","quality":1080},{"imdId":"tt0048316","slug":"love-is-a-many-splendored-thing","title":"Love Is a Many-Splendored Thing","quality":1080},{"imdId":"tt0056350","slug":"the-pirates-of-blood-river","title":"The Pirates of Blood River","quality":1080},{"imdId":"tt0064747","slug":"the-oblong-box","title":"The Oblong Box","quality":1080},{"imdId":"tt0092751","slug":"china-girl","title":"China Girl","quality":1080},{"imdId":"tt8179388","slug":"rim-of-the-world","title":"Rim of the World","quality":1080},{"imdId":"tt0069956","slug":"sacrifice-1","title":"Sacrifice!","quality":1080},{"imdId":"tt0065895","slug":"machine-gun-mccain","title":"Machine Gun McCain","quality":1080},{"imdId":"tt5238904","slug":"assimilate","title":"Assimilate","quality":1080},{"imdId":"tt1489887","slug":"booksmart","title":"Booksmart","quality":1080},{"imdId":"tt7772580","slug":"the-perfection","title":"The Perfection","quality":1080},{"imdId":"tt0050972","slug":"silk-stockings","title":"Silk Stockings","quality":1080},{"imdId":"tt1833844","slug":"berberian-sound-studio","title":"Berberian Sound Studio","quality":1080},{"imdId":"tt0228786","slug":"the-crimson-rivers","title":"The Crimson Rivers","quality":1080},{"imdId":"tt6383494","slug":"holy-lands","title":"Holy Lands","quality":1080},{"imdId":"tt7090040","slug":"roads-trees-and-honey-bees","title":"Roads, Trees and Honey Bees","quality":1080},{"imdId":"tt0092944","slug":"eat-the-rich","title":"Eat the Rich","quality":1080},{"imdId":"tt0075950","slug":"the-domino-principle","title":"The Domino Principle","quality":1080},{"imdId":"tt0203975","slug":"tart","title":"Tart","quality":1080},{"imdId":"tt0093225","slug":"housekeeping","title":"Housekeeping","quality":1080},{"imdId":"tt0283509","slug":"no-man-s-land-1","title":"No Man's Land","quality":1080},{"imdId":"tt0091752","slug":"the-phantom-empire","title":"The Phantom Empire","quality":1080},{"imdId":"tt0090203","slug":"the-trip-to-bountiful-1","title":"The Trip to Bountiful","quality":1080},{"imdId":"tt0073312","slug":"love-and-death","title":"Love and Death","quality":1080},{"imdId":"tt0459250","slug":"the-cure-trilogy","title":"The Cure: Trilogy","quality":1080},{"imdId":"tt0114489","slug":"soldier-boyz","title":"Soldier Boyz","quality":1080},{"imdId":"tt0067658","slug":"von-richthofen-and-brown","title":"Von Richthofen and Brown","quality":1080},{"imdId":"tt0029322","slug":"nothing-sacred","title":"Nothing Sacred","quality":1080},{"imdId":"tt9818000","slug":"girl-dorm","title":"Girl Dorm","quality":1080},{"imdId":"tt4219706","slug":"demon-squad","title":"Demon Squad","quality":1080},{"imdId":"tt0091830","slug":"the-green-ray","title":"The Green Ray","quality":1080},{"imdId":"tt6547786","slug":"life-like","title":"Life Like","quality":1080},{"imdId":"tt6021482","slug":"american-exit","title":"American Exit","quality":1080},{"imdId":"tt9056818","slug":"last-breath","title":"Last Breath","quality":1080},{"imdId":"tt7144200","slug":"the-siren","title":"The Siren","quality":1080},{"imdId":"tt8912932","slug":"room-37-the-mysterious-death-of-johnny-thunders","title":"Room 37: The Mysterious Death of Johnny Thunders","quality":1080},{"imdId":"tt4981292","slug":"unwritten","title":"Unwritten","quality":1080},{"imdId":"tt6534422","slug":"a-brilliant-monster","title":"A Brilliant Monster","quality":1080},{"imdId":"tt6902696","slug":"gloria-bell","title":"Gloria Bell","quality":1080},{"imdId":"tt8731138","slug":"long-way-home","title":"Long Way Home","quality":1080}]
        static void GetPage(int page, bool movie)
        {
            string download = "https://gomostream.com/user-api/" + (movie ? "movies" : "episodes") + "?key=" + __key + "&p=" + page.ToString();
            WebClient client = new WebClient();
            string d = "";
            int errors = 0;

            while (d == "") {
                d = client.DownloadString(download);
                if (d == "") {
                    errors++;
                    //print("Error: " + page);
                    System.Threading.Thread.Sleep(1000);
                };
                if (errors > 10) {
                    d = "Error";
                }
            }
            // print(d);

            while (d.Contains("{")) {


                string currt = FindHTML(d, "{", "}");
                allPages.Add(currt);
                d = d.Substring(d.IndexOf("{") + 1, d.Length - d.IndexOf("{") - 1);
            }
            pagesDone++;

            Clear();
            print("Done: " + (System.Math.Round(10000f * pagesDone / totalPages)) / 100f + "%");

            if (pagesDone == totalPages) {
                WriteData(movie);
            }
            //   1";
        }

        static void WriteData(bool movie)
        {
            print("Saving");
            string path = _path + "\\saveAll" + (movie ? "Movie" : "Series") + "Data.txt";
            File.WriteAllText(path, System.String.Empty);

            //Open the File
            StreamWriter sw = new StreamWriter(path, true, Encoding.ASCII);

            //Writeout the numbers 1 to 10 on the same line.
            for (int i = 0; i < allPages.Count; i++) {
                sw.WriteLine(allPages[i]);
            }

            allPages = new List<string>();
            print("Saved!");

            //close the file
            sw.Close();
        }

        static void ReadLine(bool movie, string line, string searchString)
        {

            if (movie) {
                string slug = ReadJson(line, "slug");
                // if (!fwordLink.Contains("https://gomostream.com/movie/" + slug)) {
                string title = ReadJson(line, "title");
                if (FuzzyMatch(title, searchString)) {
                    //string imdId = ReadJson(line, "imdId");
                    int _start = line.IndexOf("quality");
                    string qulity = line.Substring(_start + 9, line.Length - _start - 9);
                    Movie m = new Movie();
                    m.title = title;

                    moviesActive.Add(m);
                    fwordLink.Add("https://gomostream.com/movie/" + slug);
                    movieIsAnime.Add(false);
                    movieTitles.Add(title + " (" + qulity + "p)");
                    movieProvider.Add(-3);
                }
            }
            else {
                string slug = ReadJson(line, "slug");
                string title = slug.Replace("-", " ");
                if (FuzzyMatch(title, searchString)) {
                    if (!fwordLink.Contains(slug)) {
                        Movie m = new Movie();
                        m.title = ToTitle(title);

                        fwordLink.Add(slug);
                        moviesActive.Add(m);
                        movieIsAnime.Add(false);
                        movieTitles.Add(m.title);
                        movieProvider.Add(-2);
                    }
                }
            }
        }

        static void ReadFile(bool movie, string searchString)
        {




            string path = _path + "\\saveAll" + (movie ? "Movie" : "Series") + "Data.txt";
            int lines = 0;

            try {
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new StreamReader(path);

                //Read the first line of text
                string line = sr.ReadLine();

                //Continue to read until you reach end of file
                while (line != null) {
                    //write the lie to console window
                    //Console.WriteLine(line);
                    // DelegateWithParametersLine readLine = new DelegateWithParametersLine(ReadLine);
                    ReadLine(movie, line, searchString);
                    //IAsyncResult tag =
                    //    readLine.BeginInvoke(movie, line, searchString, null, null);

                    lines++;
                    // }
                    //Read the next line
                    line = sr.ReadLine();

                }



                //close the file
                sr.Close();
            }
            catch (System.Exception e) {
                //Console.WriteLine("System.Exception: " + e.Message);
            }
            finally {
                //Console.WriteLine("Executing finally block.");
            }

        }


        static List<string> seriesLinks = new List<string>();






        static void GetSeriesLink(string flink)
        {
            seriesLinks = new List<string>();
            string path = _path + "\\saveAllSeriesData.txt";
            int lines = 0;

            try {
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new StreamReader(path);

                //Read the first line of text
                string line = sr.ReadLine();

                //Continue to read until you reach end of file
                while (line != null) {
                    //write the lie to console window
                    //Console.WriteLine(line);
                    // DelegateWithParametersLine readLine = new DelegateWithParametersLine(ReadLine);
                    //https://user.gomostream.com/show/the-simpsons/30-23
                    if (line.Contains(flink)) {
                        string ep = ReadJson(line, "episodesNo");  //seasonsNo":"02","episodesNo":"13"
                        string sp = ReadJson(line, "seasonsNo");
                        seriesLinks.Add("https://user.gomostream.com/show/" + flink + "/" + sp + "-" + ep + "/");
                    }
                    //IAsyncResult tag =
                    //    readLine.BeginInvoke(movie, line, searchString, null, null);

                    lines++;
                    // }
                    //Read the next line
                    line = sr.ReadLine();

                }



                //close the file
                sr.Close();
            }
            catch (System.Exception e) {
                //Console.WriteLine("System.Exception: " + e.Message);
            }
            finally {
                //Console.WriteLine("Executing finally block.");
            }
        }

        static int movieLinks = 360;
        static int tvLinks = 1938;

        static bool onlyHD = false;
        static bool disableHD = false;

        static string ToTitle(string title)
        {
            return System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(title.Replace("/", "").Replace("-", " "));
        }






        public static void Clear()
        {
            //Console.Clear();
        }
        public static void print(string s)
        {
            if (clearNext) {
                clearNext = false;
                Clear();
            }
            // Console.WriteLine(s);

            System.Diagnostics.Debug.WriteLine(s);
        }

        public static void Search(string inp)
        {

            movieTitles = new List<string>();
            fwordLink = new List<string>();
            activeLinks = new List<string>();
            moviesActive = new List<Movie>();
            movieIsAnime = new List<bool>();
            movieProvider = new List<int>();
            /*
            DelegateWithParameters_search titles_search =
                           new DelegateWithParameters_search(GetTitles);

            IAsyncResult tag =
                titles_search.BeginInvoke(inp, null, null);
                */
            GetTitles(inp);
            //return ;
        }





        //{"Title":"Iron Man","Year":"2008","Rated":"PG-13","Released":"02 May 2008","Runtime":"126 min","Genre":"Action, Adventure, Sci-Fi","Director":"Jon Favreau","Writer":"Mark Fergus (screenplay), Hawk Ostby (screenplay), Art Marcum (screenplay), Matt Holloway (screenplay), Stan Lee (characters), Don Heck (characters), Larry Lieber (characters), Jack Kirby (characters)","Actors":"Robert Downey Jr., Terrence Howard, Jeff Bridges, Gwyneth Paltrow","Plot":"After being held captive in an Afghan cave, billionaire engineer Tony Stark creates a unique weaponized suit of armor to fight evil.","Language":"English, Persian, Urdu, Arabic, Hungarian","Country":"USA, Canada","Awards":"Nominated for 2 Oscars. Another 20 wins & 65 nominations.","Poster":"https://m.media-amazon.com/images/M/MV5BMTczNTI2ODUwOF5BMl5BanBnXkFtZTcwMTU0NTIzMw@@._V1_SX300.jpg","Ratings":[{"Source":"Internet Movie Database","Value":"7.9/10"},{"Source":"Rotten Tomatoes","Value":"93%"},{"Source":"Metacritic","Value":"79/100"}],"Metascore":"79","imdbRating":"7.9","imdbVotes":"854,168","imdbID":"tt0371746","Type":"movie","DVD":"30 Sep 2008","BoxOffice":"$318,298,180","Production":"Paramount Pictures","Website":"http://www.ironmanmovie.com/","Response":"True"}
        static string ReadJson(string all, string inp)
        {
            string newS = all.Substring(all.IndexOf(inp) + (inp.Length + 3), all.Length - all.IndexOf(inp) - (inp.Length + 3));

            string ns = newS.Substring(0, newS.IndexOf("\""));

            return ns;
        }



        static string __key2 = ""; // get your own key >:(
        async Task GetRating(string inp, bool fullplot = false)
        {
            int season = 0;
            bool wide = false;
            bool mx = false;

            for (int i = 1000; i > 0; i--) {
                if (inp.Contains(" s" + i)) {
                    season = i;
                    inp = inp.Substring(0, inp.IndexOf(" s" + i));
                }
            }
            if (inp.StartsWith("all ")) {
                wide = true;
                inp = inp.Substring(4, inp.Length - 4);
            }
            if (inp.StartsWith("mx ")) {
                mx = true;
                inp = inp.Substring(3, inp.Length - 3);
            }

            string input = inp.Replace(" ", "+");
            string url = "http://www.omdbapi.com/?t=" + input + (fullplot ? "&plot=full" : "") + "&apikey=" + __key2;

            Movie newMovie = new Movie();

            WebClient client = new WebClient();
            System.String downloadedString = client.DownloadString(url);

            if (!downloadedString.Contains("Error")) {

                newMovie.title = ReadJson(downloadedString, "Title");
                newMovie.year = ReadJson(downloadedString, "Year");
                newMovie.rated = ReadJson(downloadedString, "Rated");
                newMovie.released = ReadJson(downloadedString, "Released");
                newMovie.genre = ReadJson(downloadedString, "Genre");
                newMovie.plot = ReadJson(downloadedString, "Plot");
                newMovie.posterID = ReadJson(downloadedString, "Poster");
                newMovie.imdbRating = ReadJson(downloadedString, "imdbRating");
                newMovie.type = ReadJson(downloadedString, "Type");
                if (newMovie.year.Contains("â")) {
                    newMovie.year = newMovie.year.Replace(newMovie.year.Substring(newMovie.year.IndexOf("â"), 3), "-");
                    if (newMovie.year.Length == 5) {
                        newMovie.year += "????";
                    }
                }
                // print(downloadedString);


                Movie m = newMovie;
                if (!mx) {

                    print("Title: " + m.title);
                    print("Score: " + m.imdbRating);
                    print("Year: " + m.year);
                    print("Rated: " + m.rated);
                    print("Genre: " + m.genre);
                    print("Plot: " + m.plot);
                    print("");
                }
            }
        }
        static int mirrorCounter = 0;
        static bool haveLowLink = false;
        static void GetLinkServer(int f, string realMoveLink)
        {
            int cThred = thredNumber;
            print("Get link server " + realMoveLink);
            string jsn = _WebRequest(realMoveLink + "?server=server_" + f);
            urlsDone++;
            progress = (int)System.Math.Round((urlsDone * 100f) / (urlServers + otherServers), MidpointRounding.AwayFromZero);
            OnProgressLinkChanged(progress); // 1   //int.Parse(MathF.Floor(100 * (urlsDone / (urlServers + otherServers))).ToString());  

            //print(bar.Progress + " | " + urlsDone);
            //print(jsn);


            while (jsn.Contains("http")) {
                int _start = jsn.IndexOf("http");
                jsn = jsn.Substring(_start, jsn.Length - _start);
                int id = jsn.IndexOf("\"");
                if (id != -1) {
                    string newM = jsn.Substring(0, id);
                    newM = newM.Replace("\\", "");
                    if (!activeLinks.Contains(newM)) {
                        if (cThred != thredNumber) return;

                        mirrorCounter++;
                        activeLinks.Add(newM);
                        activeLinksNames.Add("Mirror " + mirrorCounter);

                        print(newM);
                        print("");
                    }
                }
                jsn = jsn.Substring(4, jsn.Length - 4);
            }
        }


        public static int moveSelectedID = 0;
        static bool hdMovieIsDone = false;
        static bool __done = false;

        //Subtitles::
        //    <script>window.subtitles = [{"max":["English"],"kind":"subtitles","srclang":"en","label":"English","src":"https:\/\/static.movies123.pro\/files\/tracks\/7Qb72bR9HOeBARWA7S68SsMXnWTJ29E00cnCACJ4KQZDoKR6pMigskHNAJYZYuRHJo3PJnaqW-oRQmrIp0SQrXb95zyifNPLFNLuTPyzQG0.srt"}]</script>    </block></div></div></section>
        public static string currentEpisodeName = "";
        public static int currentActiveSubtitle = -1;

        public static void GetUrlFromMovie123(int titleID, int cThred, string realMoveLink, bool isMovie = true, string episode = "-1")
        {
            print("RealMovieLink:" + realMoveLink);
            mirrorCounter = 0;
            string find = movieTitles[titleID].ToLower().Replace("b___", "").Replace(" (bookmark)", "").Replace(" (movie)", "").Replace(" (tv-series)", "").Replace(" (tv-serie)", "").Replace(" (hd tv-serie)", "").Replace(" (hd tv-series)", "");
            print("FIND GOGO:" + find);
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            find = rgx.Replace(find, "");
            find = find.Replace(" - ", "-").Replace(" ", "-");
            find = find.Replace("-season-", "/");
            for (int i = 0; i < 10; i++) {
                if (find.EndsWith("/" + i)) {
                    find = find.Replace("/" + i, "/0" + i);
                }
            }
            if (episode != "-1") {
                find += "-" + episode;
                currentEpisodeName = episode;
            }
            for (int i = 0; i < 10; i++) {
                if (find.EndsWith("/" + i)) {
                    find = find.Replace("/" + i, "/0" + i);
                }
            }
            int errors = 0;
            try {

                DelegateWithParameters_search linkServer =
                    new DelegateWithParameters_search(DownloadGogo);

                if (cThred != thredNumber) return;


                IAsyncResult tag =
                    linkServer.BeginInvoke("https://gomostream.com/" + (isMovie ? "movie" : "show") + "/" + find, null, null);


                // DownloadGogo("https://gomostream.com/movie/" + find.Replace("-movie", ""));

                /*
                string d = client.DownloadString();
                tokenCode = FindHTML(d, "var tc = \'", "'");
                _token = FindHTML(d, "_token\": \"", "\"");
                GetAPI();
                */
            }
            catch (System.Exception) {
                errors++;
            }
            hdMovieIsDone = false;
            if (errors == 1) {
                print("HD Link error");
                print("");
                hdMovieIsDone = true;

            }

            for (int f = 0; f < urlServers; f++) {
                // GetLinkServer(f, realMoveLink);

                // create the delegate
                DelegateWithParameters linkServer =
                    new DelegateWithParameters(GetLinkServer);

                if (cThred != thredNumber) return;

                IAsyncResult tag =
                    linkServer.BeginInvoke(f, realMoveLink, null, null);

                //linkServer.EndInvoke(tag);
            }
            haveLowLink = false;
            try {
                WebClient client = new WebClient();

                string d = client.DownloadString(realMoveLink);
                string lookfor = "\",\"src\":\"http";
                while (d.Contains(lookfor)) {

                    string lan = FindReverseHTML(d, lookfor, "\"");
                    string link = "http" + FindHTML(d, lookfor, "\"").Replace("\\", "");
                    activeSubtitles.Add(link);
                    activeSubtitlesNames.Add(lan);
                    print("SUBTITLE: " + lan + " | " + link);
                    d = d.Substring(d.IndexOf(lookfor) + 1, d.Length - d.IndexOf(lookfor) - 1);
                }

            }
            catch (System.Exception) {

            }


            if (cThred != thredNumber) return;

            try {
                //  GetLowLink(RemoveBloatTitle(movieTitles[titleID]));
            }
            catch (System.Exception) {
                //print("Low link error");
            }
            while (urlsDone < urlServers) {
                if (cThred != thredNumber) return;

                Thread.Sleep(10);
            }
            /*
            while (!haveLowLink) {
                if (cThred != thredNumber) return;

                Thread.Sleep(10);
            }
            */
            while (!hdMovieIsDone) {
                if (cThred != thredNumber) return;

                Thread.Sleep(10);
            }
            OnProgressLinkChanged(100);
        }


        public static void GetURLFromTitle(int titleID = -1, bool onlyEps = false, int tNum = -1, int epNum = -1)
        {
            int addToTitleEps = 0;
            string tempThred = tNum.ToString();
            int ccThrednum = int.Parse(tempThred);
            moveSelectedID = titleID;

            string inter = thredNumber.ToString();
            int cThred = int.Parse(inter);
            print("---------------------------------" + cThred + " | " + thredNumber + "--------------------------------------------");
            string realMoveLink = fwordLink[titleID];
            __done = true;
            __selSeason = 0;
            int provider = movieProvider[titleID];
            if (titleID == -1) {
                for (int i = 0; i < movieTitles.Count; i++) {
                    if (selectedTitle == movieTitles[i]) {
                        titleID = i;
                    }
                }
            }

            if (!onlyEps) {

                tempTitle = "";
                urlsDone = 0;
                currentXtoken = 0;

                activeLinks = new List<string>();
                activeLinksNames = new List<string>();
                activeSubtitles = new List<string>();
                activeSubtitlesNames = new List<string>();
                mirrorCounter = 0;
                currentActiveSubtitle = -1;

            }
            print(movieTitles[titleID] + ":::::" + movieProvider[titleID] + "||||||" + provider + "-----" + movieIsAnime[titleID]);
            if (!movieIsAnime[titleID]) {
                WebClient client = new WebClient();

                if (!onlyEps) {
                    if (provider == -1) {
                        GetUrlFromMovie123(titleID, cThred, realMoveLink);

                        print("Done!!");
                    }
                    else if (provider == -3) {
                        string d = "";
                        try {
                            d = client.DownloadString(fwordLink[titleID]);
                        }
                        catch (System.Exception) {

                        }
                        if (d != "") {
                            tokenCode = FindHTML(d, "var tc = \'", "'");
                            _token = FindHTML(d, "_token\": \"", "\"");
                            GetAPI();
                        }
                        else {
                            print("Error downloading: " + fwordLink[titleID]);
                        }
                    }
                    else if (provider == -2) {
                        //currentXtoken = 1;
                        GetSeriesLink(fwordLink[titleID]);
                        for (int i = 0; i < seriesLinks.Count; i++) {
                            if (tryDownloadingTv) {
                                string d = "";
                                try {
                                    d = client.DownloadString(seriesLinks[i]);
                                }
                                catch (System.Exception) {

                                }
                                if (d != "") {
                                    tempTitle = seriesLinks[i].Replace("https://user.gomostream.com/show/", "").Replace("-", " ").Replace("/", " ") + "| ";
                                    currentXtoken = 1;
                                    tokenCode = FindHTML(d, "id\': \'", "\'");
                                    tokenCode = FindHTML(d, "var tc = \'", "'");
                                    _token = FindHTML(d, "_token\": \"", "\"");
                                    GetAPI();
                                }
                                else {
                                    print("Error downloading: " + seriesLinks[i]);
                                }
                            }
                            else {
                                print(seriesLinks[i]);
                            }
                        }
                    }
                    else if (provider == 3) {
                        //currentXtoken = 1;
                        __series = fwordLink[titleID];
                        _GetAPI(cThred);
                        __done = false;
                    }
                    else if (provider == -5) {
                        string d = client.DownloadString(fwordLink[titleID]);
                        //https://vcstream.to/player?fid=5cea538a2cf69&page=embed
                        // string embeded = "https://vcstream.to/embed/" + FindHTML(d, "src=\"https://vcstream.to/embed/", "\"");
                        string find = FindHTML(d, "src=\"https://vcstream.to/embed/", "\"");
                        if (find != "") {
                            string embeded = "https://vcstream.to/player?fid=" + find + "&page=embed";

                            d = client.DownloadString(embeded);
                            string file = FindHTML(d, "\"file\\\":\\\"", "\\\"}").Replace("\\\\\\/", "/");
                            activeLinks.Add(file);
                            activeLinksNames.Add("PUTLOCKER");

                        }
                        else {
                            string gogoLink = "https://gomostream.com/" + FindHTML(d, "https://gomostream.com/", "\"");
                            if (gogoLink != "https://gomostream.com/") {
                                hdMovieIsDone = false;
                                DownloadGogo(gogoLink);

                                while (!hdMovieIsDone) {
                                    if (cThred != thredNumber) return;

                                    Thread.Sleep(10);
                                }
                            }
                            else {
                                string openloadLink = "https://openload.co/embed/" + FindHTML(d, "https://openload.co/embed/", "\"");
                                if (openloadLink != "https://openload.co/embed/") {
                                    activeLinks.Add(openloadLink);
                                    activeLinksNames.Add("OPENLOAD");
                                }

                            }
                        }





                    }
                }
                else {
                    if (provider == 3) {
                        __series = fwordLink[titleID];

                        _GetAPI(thredNumber, false);
                        while (!_APIDone) {
                            if (cThred != thredNumber) {
                                return;
                            }
                            Thread.Sleep(10);
                        }
                        addToTitleEps = __totalLinks;
                    }
                    else {
                        addToTitleEps = -1;
                    }
                }

                if (provider == 4) {
                    addToTitleEps = 0;
                    //<a data-ep-id="9DXf8Rsv" href="/tv-series/the-orville-season-2/gMSTqyRs/9DXf8Rsv-watch-free.html" title="Episode 09: Identity, Part 2" class="PbBgtidTnPxraqPIHruO ">
                    //    < span class="yYnGHyiHQRdFTEMmNPga">Episode 09: Identity, Part 2</span>
                    string d = "";
                    try {
                        d = client.DownloadString("https://movies123.pro" + fwordLink[titleID]);
                    }
                    catch (System.Exception) {

                    }
                    if (d != "") {
                        string lookfor = "\" href=\"" + fwordLink[titleID];

                        while (d.Contains(lookfor)) {
                            string fullUrl = fwordLink[titleID] + FindHTML(d, lookfor, "\""); // ch-free.html" title="
                            string title = FindHTML(d, fullUrl + "\" title=\"", "\"");
                            if (title != "") {
                                if (!onlyEps) {
                                    activeLinks.Add(fullUrl);
                                    activeLinksNames.Add(title);
                                }
                                else {
                                    addToTitleEps++;
                                }
                            }

                            int endIndex = d.IndexOf(title) + 1;
                            d = d.Substring(endIndex, d.Length - endIndex);
                            if (title == "") {
                                d = "";
                            }
                        }

                    }

                }

            }
            else {
                if (provider == 1) {
                    WebClient client = new WebClient();
                    print("Daa13");
                    string d = "";
                    try {
                        d = client.DownloadString("https://ww.9animes.net/" + realMoveLink.Replace(" ", "-"));
                    }
                    catch (System.Exception) {

                    }
                    print("Daa12");
                    if (d != "") {
                        print("Daa11");
                        string realLink = FindHTML(d, "<link rel=\"canonical\" href=\"", "\"");
                        try {


                            bool dub = movieTitles[titleID].Contains("Dubbed");
                            if (dub) {
                                realLink.Replace("subbed", "dubbed");
                            }
                            else {
                                realLink.Replace("dubbed", "subbed");
                            }
                            d = client.DownloadString(realLink);

                            string templateS = realLink.Replace(FindHTML(realLink, "episode", "/") + "/", "");
                            string s = "<option value=\"" + templateS;
                            while (d.Contains(s)) {
                                if (!onlyEps) {
                                    string url = templateS + FindHTML(d, s, "\"");
                                    string dUrl = client.DownloadString(url);
                                    string title = FindHTML(dUrl, "To watch <strong>", "<");
                                    // print(dUrl);
                                    string allBox = "";
                                    if (dUrl.Contains("//captched.com/embed/")) {
                                        allBox = "https://captched.com/embed/iframe.php?u=" + FindHTML(dUrl, "//captched.com/embed/", "\"");

                                        // string dAllBox = client.DownloadString(allBox);
                                        //print(dAllBox);
                                    }

                                    if (dUrl.Contains("https://www.mp4upload.com/embed-")) {
                                        string mp4URL = FindHTML(dUrl, "https://www.mp4upload.com/embed-", "\"").Replace(".html", "");
                                        string printAll = title;
                                        if (allBox != "") {
                                            printAll += " | Browser: " + allBox;
                                        }
                                        string realMp4 = Getmp4uploadByID(mp4URL);
                                        if (realMp4 != "") {
                                            if (realMp4 != "") {
                                                printAll += " | BrowserMp4: " + "https://www.mp4upload.com/embed-" + mp4URL + ".html" + " | DownloadMp4: " + realMp4;
                                                if (!activeLinks.Contains(realMp4)) {
                                                    print("---------------------------------" + cThred + " | " + thredNumber + "--------------------------------------------");

                                                    if (cThred != thredNumber) return;

                                                    activeLinks.Add(realMp4);
                                                    activeLinksNames.Add(title);
                                                    progress = 0;
                                                    OnProgressLinkChanged(progress);

                                                }
                                                print(printAll);
                                            }
                                        }
                                    }
                                    else {
                                        if (allBox != "") {
                                            print(title + " | Error, Browser: " + allBox);
                                        }
                                        else {
                                            print(title + " | Error");
                                        }
                                        //"https://vidstreaming.io/streaming.php?id="
                                    }

                                }
                                else {
                                    addToTitleEps++;
                                    print(addToTitleEps + " ||| " + movieTitles[titleID]);
                                }
                                d = d.Substring(d.IndexOf(s) + 1, d.Length - d.IndexOf(s) - 1);

                            }

                        }
                        catch (System.Exception) {

                        }
                    }
                    //  print(d);
                    //  print(d);

                    /*
                    string title = FindHTML(d,"To watch <strong>", "<");
                    string mp4URL = FindHTML(d, "https://www.mp4upload.com/embed-", "\"").Replace(".html", "");
                    print(Getmp4uploadByID(mp4URL));
                    */
                }
                if (provider == 0) {
                    // try {


                    WebClient client = new WebClient();
                    string d = "";
                    try {
                        d = client.DownloadString(realMoveLink);

                    }
                    catch (System.Exception) {


                    }
                    if (d != "") {
                        print("äää");
                        print(d);

                        List<string> downloads = new List<string>();
                        List<string> names = new List<string>();
                        List<int> nums = new List<int>();
                        // print(d);
                        while (d.Contains("text-right ep-num\'>")) {

                            try {
                                string dstring = ("https://" + FindHTML(d, "<div class=\'episode-wrap\'>", "\'", 28));
                                string name = FindHTML(d, "ep-anime-name-an\'>", "<");

                                string ep = FindHTML(d, "text-right ep-num\'>", "<").Replace("Ep. ", "");
                                if (ep == "") {
                                    ep = "0";
                                }
                                int num = int.Parse(ep);
                                bool dub = dstring.Contains("english-dub");
                                int epNumPlace = d.IndexOf("text-right ep-num\'>") + 1; // to prevent while(true)

                                d = d.Substring(epNumPlace, d.Length - epNumPlace);
                                // if (dub == isDub) {

                                print("NUM:" + num + " DUB:" + dub + " DSTRING: " + dstring);

                                nums.Add(num);
                                names.Add(name + (dub ? " (Dub)" : " (Sub)"));
                                downloads.Add(dstring);
                                addToTitleEps++;

                                //}
                            }
                            catch (System.Exception) {
                                string name = FindHTML(d, "ep-anime-name-an\'>", "<");
                                print(name + " | Error");
                                int epNumPlace = d.IndexOf("text-right ep-num\'>") + 1; // to prevent while(true)

                                d = d.Substring(epNumPlace, d.Length - epNumPlace);
                            }

                        }
                        if (!onlyEps) {

                            List<string> newDownloads = new List<string>();
                            List<string> newNames = new List<string>();
                            List<int> taken = new List<int>();
                            for (int i = 0; i < nums.Count; i++) {
                                int lowest = 10000000;
                                int rnum = 0;
                                for (int x = 0; x < nums.Count; x++) {
                                    if (nums[x] < lowest && !taken.Contains(x)) {
                                        rnum = x;
                                        lowest = nums[x];
                                    }
                                }
                                taken.Add(rnum);
                                newNames.Add(names[rnum]);
                                newDownloads.Add(downloads[rnum]);
                            }

                            for (int i = 0; i < newDownloads.Count; i++) {
                                print("NEW DOWNLOAD: " + newDownloads[i]);
                                d = client.DownloadString(newDownloads[i]);

                                string mp4Find = "mp4upload\",\"id\":\"";
                                string trollFind = "trollvid\",\"id\":\"";


                                // string ttp = FindHTML(d, mp4upload + "\",\"type\":\"", "\"");
                                string mp4upload = ""; // FindHTML(d, mp4Find, "\""); 
                                string trollvid = "";
                                // TO get corrent
                                // int ccc = 0;

                                /*
                                while (((ttp == "subbed" && newNames[i].Contains("(Dub)")) || (ttp == "dubbed" && newNames[i].Contains("(Sub)"))) && mp4upload != "" && ccc < 30) {
                                    ccc++;

                                    string _ttpFind = mp4upload + "\",\"type\":\"";
                                    int ttpFind = d.IndexOf(_ttpFind);
                                    print(ttpFind.ToString());
                                    d = d.Substring(ttpFind + 1, d.Length - ttpFind - 1);

                                    mp4upload = FindHTML(d, "mp4upload\",\"id\":\"", "\"");

                                    ttp = FindHTML(d, mp4upload + "\",\"type\":\"", "\"");
                                }
                                if (ccc == 30) {
                                    mp4upload = "";
                                }*/


                                while (d.Contains(mp4Find) || d.Contains(trollFind)) {
                                    bool mp4First = d.IndexOf(mp4Find) < d.IndexOf(trollFind);
                                    if (!d.Contains(trollFind)) { mp4First = true; }
                                    if (!d.Contains(mp4Find)) { mp4First = false; }

                                    string lookfor = mp4First ? mp4Find : trollFind;

                                    int ttpFind = d.IndexOf(lookfor);

                                    string id = FindHTML(d, lookfor, "\"");

                                    string type = FindHTML(d, lookfor + id + "\",\"type\":\"", "\"");

                                    if ((type == "dubbed" && newNames[i].Contains("(Dub)")) || (type == "subbed" && newNames[i].Contains("(Sub)"))) {
                                        if (mp4First) {
                                            mp4upload = id;
                                        }
                                        else {
                                            trollvid = id;
                                        }
                                    }
                                    d = d.Substring(ttpFind + 1, d.Length - ttpFind - 1);
                                }




                                if (mp4upload != "") {


                                    string rLink = Getmp4uploadByID(mp4upload);
                                    //<script type='text/javascript'>var episode_videos = [{"host":"trollvid","id":"ba4f06fe","type":"dubbed","date":"2015-12-20 09:32:53"},{"host":"trollvid","id":"252bbb32c682","type":"subbed","date":"2019-04-08 18:15:01"},{"host":"trollvid","id":"e0c600d9573d","type":"subbed","date":"2019-04-08 18:15:01"},
                                    //{ "host":"mp4upload","id":"93vet46b3k43","type":"subbed","date":"2015-12-20 09:32:53"},{"host":"mp4upload","id":"zjmyxp4x0zmu","type":"subbed","date":"2019-02-10 21:14:48"},{"host":"mp4upload","id":"1s5nmi90lyqo","type":"dubbed","date":"2019-04-08 17:52:06"}];</script>

                                    print(newNames[i] + " | BrowserMp4: " + "https://www.mp4upload.com/embed-" + mp4upload + ".html" + " | DownloadMp4: " + rLink);
                                    if (!activeLinks.Contains(rLink)) {
                                        print("---------------------------------" + cThred + " | " + thredNumber + "--------------------------------------------");

                                        if (cThred != thredNumber) return;

                                        activeLinks.Add(rLink);
                                        activeLinksNames.Add(newNames[i]);
                                        progress = (int)System.Math.Round((100f * i) / newDownloads.Count);
                                        OnProgressLinkChanged(progress);

                                    }
                                }
                                else if (trollvid != "") {
                                    string embeded = "https://trollvid.net/embed/" + trollvid;
                                    string result = HTMLGet(embeded, newDownloads[i]);
                                    string lookfor = "player.src(\"";
                                    print(result);

                                    string rLink = "";

                                    while (result.Contains(lookfor)) {
                                        string link = FindHTML(result, lookfor, "\"");
                                        print("Direct Link: " + link);
                                        rLink = link;
                                        result = result.Substring(result.IndexOf(lookfor) + 1, result.Length - result.IndexOf(lookfor) - 1);
                                    }

                                    lookfor = "<source src=\"";

                                    while (result.Contains(lookfor)) {
                                        string link = FindHTML(result, lookfor, "\"");
                                        print("Source: " + link);
                                        rLink = link;
                                        result = result.Substring(result.IndexOf(lookfor) + 1, result.Length - result.IndexOf(lookfor) - 1);
                                        //string newD = HTMLGet(link, embeded, false);
                                        // print(d);
                                    }
                                    if (!activeLinks.Contains(rLink) && rLink != "") {
                                        print("GOT TROLLVID: " + rLink);

                                        print("---------------------------------" + cThred + " | " + thredNumber + "--------------------------------------------");

                                        if (cThred != thredNumber) return;

                                        activeLinks.Add(rLink);
                                        activeLinksNames.Add(newNames[i]);


                                        progress = (int)System.Math.Round((100f * i) / newDownloads.Count);
                                        OnProgressLinkChanged(progress);

                                    }

                                }

                            }
                        }
                    }
                    else {
                        print("Error, 404");
                    }
                    // }
                    //catch (System.Exception) {
                    //   print("Error");
                    //}
                }
                if (provider == 2) {
                    WebClient client = new WebClient();
                    string d = "";
                    try {
                        d = client.DownloadString("https://www3.gogoanime.io/category/" + fwordLink[titleID]);

                    }
                    catch (System.Exception) {

                    }
                    if (d != "") {
                        string id = FindHTML(d, "<input type=\"hidden\" value=\"", "\"");
                        d = client.DownloadString("https://ajax.apimovie.xyz/ajax/load-list-episode?ep_start=0&ep_end=100000&id=" + id + "&default_ep=0");
                        //print(d);
                        // Console.ReadLine();
                        string fS = "<li><a href=\" ";
                        List<string> _links = new List<string>();
                        while (d.Contains(fS)) {
                            string _link = FindHTML(d, fS, "\"");

                            _links.Add(_link);
                            d = d.Substring(d.IndexOf(fS) + 1, d.Length - 1 - d.IndexOf(fS));

                            if (epNum == -2) {
                                break;
                            }
                        }
                        if (epNum > 0) {
                            string blink = "";
                            for (int i = 0; i < _links.Count; i++) {
                                if (ToTitle(_links[i]).Contains("Episode " + epNum)) {
                                    blink = _links[i];
                                }
                            }
                            if (blink != "") {

                                _links = new List<string>();
                                _links.Add(blink);
                            }
                        }
                        if (onlyEps) {
                            addToTitleEps = _links.Count;
                        }
                        else {
                            for (int i = 0; i < _links.Count; i++) {
                                string title = ToTitle(_links[i]);

                                d = client.DownloadString("https://www3.gogoanime.io" + _links[i]);
                                string mp4 = "https://www.mp4upload.com/embed-" + FindHTML(d, "data-video=\"https://www.mp4upload.com/embed-", "\"");
                                if (mp4 == "https://www.mp4upload.com/embed-") {
                                    mp4 = FindHTML(d, "data-video=\"//vidstreaming.io/streaming.php?", "\"");
                                    if (mp4 != "") {
                                        mp4 = "http://vidstreaming.io/streaming.php?" + mp4;
                                        d = client.DownloadString(mp4);
                                        string mxLink = FindHTML(d, "sources:[{file: \'", "\'");
                                        print("Backup: " + title + " | Browser: " + mp4 + " | RAW (NO ADS): " + mxLink);
                                        if (!activeLinks.Contains(mxLink)) {
                                            print("---------------------------------" + cThred + " | " + thredNumber + "--------------------------------------------");

                                            if (cThred != thredNumber) return;
                                            if (!mxLink.StartsWith("Error")) {
                                                activeLinks.Add(mxLink);
                                                activeLinksNames.Add(title);
                                                progress = (int)System.Math.Round((100f * i) / _links.Count);
                                                OnProgressLinkChanged(progress);
                                            }
                                            else {
                                                print("vidstreaming mx link starts w error");
                                            }
                                        }
                                    }
                                    else {
                                        print(title + " | Error :(");
                                    }
                                }
                                else {

                                    try {
                                        d = client.DownloadString(mp4);
                                        string mxLink = Getmp4uploadByFile(d);
                                        print(title + " | BrowserMp4: " + mp4 + " | DownloadMp4: " + mxLink);
                                        if (!activeLinks.Contains(mxLink)) {
                                            print("---------------------------------" + cThred + " | " + thredNumber + "--------------------------------------------");

                                            if (cThred != thredNumber) return;
                                            activeLinks.Add(mxLink);
                                            activeLinksNames.Add(title);
                                            progress = (int)System.Math.Round((100f * i) / _links.Count);
                                            OnProgressLinkChanged(progress);
                                        }
                                    }
                                    catch (System.Exception) {
                                        print(title + " | BrowserMp4: " + mp4);

                                    }
                                }

                            }
                        }
                    }
                }
            }

            timeoutUpdated = timeout;
            while (timeoutUpdated > 0) {
                if (!__done) {
                    Thread.Sleep(10);
                    timeoutUpdated--;
                    // print("time" + timeoutUpdated);
                }
                else {
                    break;
                }
            }
            thredNumber++;
            progress = 100;
            OnProgressLinkChanged(100);
            OnLinksDone(__done);

            // if (addToTitleEps != -1) {
        }
        static async Task GetLowLink(string serchText)
        {
            print("LOW LINK: " + serchText);

            bool mx = debug;
            string rinput = serchText.ToLower().Replace(" ", "+");

            string f6utl = "https://www6.fmovie.cc/?s=" + rinput;
            WebClient client = new WebClient();
            print("AAAAAAAA1");
            WebRequest request = WebRequest.Create(
           f6utl);
            request.Credentials = CredentialCache.DefaultCredentials;
            WebResponse response = request.GetResponse();
            using (Stream dataStream = response.GetResponseStream()) {
                // Open the stream using a StreamReader for easy access.  
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.  
                string responseFromServer = reader.ReadToEnd().ToLower();
                print("AAAAAAAA2");
                // Display the content.  
                if (responseFromServer.Contains(serchText)) {
                    print("AAAAAAAA3");
                    print(responseFromServer);
                    int endID = responseFromServer.IndexOf(">" + serchText + "</a>");
                    string startFind = "alt=\"" + serchText + "\"><a class=\"titlecover\" style=\"\" href=\"";
                    int startID = responseFromServer.IndexOf(startFind);
                    string movieUrl = responseFromServer.Substring(startID + startFind.Length, endID - startID - startFind.Length - 1);
                    string firstUrl = "https://www6.fmovie.cc/movies/";
                    string inUrl = movieUrl.Substring(firstUrl.Length, movieUrl.Length - firstUrl.Length - 1);
                    string rUrl = "https://api.123movie.cc/search.php?ep=" + inUrl + "&server_name=serverf4";
                    print("AAAAAAAA4");
                    if (!mx) {
                        print("Taken from");
                        print(movieUrl);
                        print("");

                        print("Servers: ");
                        print("");

                        string rUrl2 = "https://api.123movie.cc/search.php?ep=" + inUrl + "&server_name=openload";

                        print(rUrl + " (ADS)");
                        print(rUrl2 + " (ADS)");

                        print("");
                        print("Raw Server4");
                        print("");

                    }
                    string realURLDownload = client.DownloadString(rUrl);
                    string start = "https://serverf4.org/";
                    string end = "#poster=&caption=";
                    int indexStart = realURLDownload.IndexOf(start);
                    print("AAAAAAAA5");
                    if (indexStart != -1) {
                        string raw = realURLDownload.Substring(indexStart, -indexStart + realURLDownload.IndexOf(end));
                        if (!mx) {

                            print(raw + " (ADS)");
                        }

                        /*
                        string _realURLDownload = client.DownloadString(raw);
                        print(_realURLDownload);
                        string _start = "https://serverf4.org/";
                        string _end = "#poster=&caption=";
                        int _indexStart = realURLDownload.IndexOf(start);
                        if (_indexStart != -1) {
                            print(_realURLDownload.Substring(_indexStart, -_indexStart + _realURLDownload.IndexOf(end)));
                        }*/



                        var _values = new Dictionary<string, string>
                                   {
                                   { "Host", "serverf4.org" },
                                  // { "Accept-Language", "en-US,en;q=0.5" },
                                   { "Content-Type", "application/x-www-form-urlencoded; charset=UTF-8" },
                                   { "X-Requested-With", "XMLHttpRequest" },
                                   { "Referer", raw },
                                  // { "Content-Length", "17" },
                                 //  { "Cache-Control", "max-age=0, no-cache" },
                                  // { "TE", "Trailers" },
                                   };

                        var _content = new FormUrlEncodedContent(_values);

                        var _response = await _client.PostAsync("https://serverf4.org/api/source/" + raw.Replace("https://serverf4.org/v/", ""), _content);//   "7zo-n7-jm2v", _content);

                        var _responseString = await _response.Content.ReadAsStringAsync();

                        string _all = _responseString.ToString();
                        print("AAAAAAAA6");

                        int mxStart = _all.IndexOf("\"data\":[{\"file\":") + 17;
                        int mxEnd = _all.IndexOf("label") - 3;
                        string mxURL = _all.Substring(mxStart, mxEnd - mxStart).Replace("\\", "/").Replace("//", "/");
                        string quality = _all.Substring(mxEnd, _all.Length - mxEnd);
                        int qEnd = quality.IndexOf("p");
                        string rq = quality.Substring(11, qEnd - 10);
                        print("AAAAAAAA7");

                        if (!mx) {
                            print("");
                            print("MX-URL (" + rq + ")");
                            print("");
                        }
                        print("------------------- " + rq + " -------------------");
                        print(mxURL);
                        print("--------------------------------------------");
                        print("");
                        if (!activeLinks.Contains(mxURL)) {
                            activeLinks.Add(mxURL);

                            activeLinksNames.Add("Backup Link (" + rq + ")");

                            movieIsAnime.Add(false);
                            movieProvider.Add(-1);
                        }
                        print("Low Link 3");

                        haveLowLink = true;

                    }
                    else {
                        print("Low Link Error 2");
                        haveLowLink = true;
                    }
                }
                else {
                    print("Low link error 1");
                    haveLowLink = true;

                }
            }
            print("Low link error 5");

        }
        static string ReadDataMovie(string all, string inp)
        {
            string newS = all.Substring(all.IndexOf(inp) + (inp.Length + 2), all.Length - all.IndexOf(inp) - (inp.Length + 2));
            string ns = newS.Substring(0, newS.IndexOf("\""));

            return ns;
        }


        static void Link1()
        {

            try {


                string movies123 = "https://movies123.pro/search/" + rinput.Replace("+", " ");

                WebClient client = new WebClient();
                string mD = "";
                try {
                    mD = client.DownloadString(movies123);
                }
                catch (System.Exception) {

                }
                bool canMovie = SettingsGetChecked(0);
                bool canShow = SettingsGetChecked(8);

                while (mD.Contains("/movie/") || mD.Contains("/tv-series/")) {

                    /*

                    data - filmName = "Iron Man"
                data - year = "2008"
                data - imdb = "IMDb: 7.9"
                data - duration = "126 min"
                data - country = "United States"
                data - genre = "Action, Adventure, Sci-Fi"
                data - descript = "Tony a boss of a Technology group, after his encounter in Afghanistan, became a symbol of justice as he built High-Tech armors and suits, to act as..."
                data - star_prefix = ""
                data - key = "0"
                data - quality = "itemAbsolute_hd"
                data - rating = "4.75"
                        */
                    print("--::--");
                    int tvIndex = mD.IndexOf("/tv-series/");
                    int movieIndex = mD.IndexOf("/movie/");
                    bool isMovie = movieIndex < tvIndex;

                    if (tvIndex == -1) { isMovie = true; }
                    if (movieIndex == -1) { isMovie = false; }


                    Movie m = new Movie();
                    m.year = ReadDataMovie(mD, "data-year");
                    m.imdbRating = ReadDataMovie(mD, "data-imdb");
                    m.runtime = ReadDataMovie(mD, "data-duration");
                    m.genre = ReadDataMovie(mD, "data-genre");
                    m.plot = ReadDataMovie(mD, "data-descript");
                    m.posterID = ReadDataMovie(mD, "<img src=\"/dist/image/default_poster.jpg\" data-src");
                    m.type = isMovie ? "movie" : "tv-series";

                    if (debug) {
                        print(m.title);
                        print(m.imdbRating);
                        print(m.runtime);
                        print(m.genre);
                        print(m.plot);
                        print(m.posterID);
                    }
                    string lookfor = isMovie ? "/movie/" : "/tv-series/";

                    int mStart = mD.IndexOf(lookfor);

                    mD = mD.Substring(mStart, mD.Length - mStart);
                    mD = mD.Substring(7, mD.Length - 7);


                    string rmd = lookfor + mD;
                    //string realAPILink = mD.Substring(0, mD.IndexOf("-"));
                    string _realMoveLink = "https://movies123.pro" + rmd.Substring(0, rmd.IndexOf("\""));
                    if (!isMovie) {
                        _realMoveLink = rmd.Substring(0, rmd.IndexOf("\"")); // /tv-series/ies/the-orville-season-2/gMSTqyRs
                        _realMoveLink = _realMoveLink.Substring(11, _realMoveLink.Length - 11); //ies/the-orville-season-2/gMSTqyRs
                        string found = _realMoveLink.Substring(0, _realMoveLink.IndexOf("/"));
                        if (!found.Contains("-")) {
                            _realMoveLink = _realMoveLink.Replace(found, ""); //the-orville-season-2/gMSTqyRs
                        }
                        _realMoveLink = "/tv-series" + _realMoveLink;
                    }

                    fwordLink.Add(_realMoveLink);
                    print("::::::::::::::::::::::" + _realMoveLink + " | " + isMovie);

                    int titleStart = mD.IndexOf("title=\"");
                    string _allrmd = mD.Substring(titleStart + 7, mD.Length - titleStart - 7);
                    _allrmd = _allrmd.Substring(0, _allrmd.IndexOf("\""));
                    _allrmd = _allrmd.Replace("&amp;", "and");
                    m.title = _allrmd;


                    if ((isMovie && canMovie) || (!isMovie && canShow)) {
                        moviesActive.Add(m);
                        movieIsAnime.Add(false);
                        movieTitles.Add(_allrmd);
                        movieProvider.Add(isMovie ? -1 : 4);

                    }
                }
                linksDone++;
                //   SortMovies();

                OnProgressSearchChanged((int)System.Math.Round(linksDone * 100 / totalLinks));
            }
            catch (System.Exception) {

            }
        }
        static void Link2()
        {
            try {
                WebClient client = new WebClient();
                string gogoAnime = "https://www3.gogoanime.io//search.html?keyword=" + rinput.Replace("+", "%");

                string _d = client.DownloadString(gogoAnime);

                // <p class="name"><a href="/category/mob-psycho-100" title="Mob Psycho 100">Mob Psycho 100</a></p>
                string look = "<p class=\"name\"><a href=\"/category/";
                while (_d.Contains(look)) {
                    string ur = FindHTML(_d, look, "\"");
                    Movie m = new Movie();
                    m.title = ToTitle(ur);
                    moviesActive.Add(m);
                    movieIsAnime.Add(true);
                    movieTitles.Add(m.title);
                    movieProvider.Add(2);
                    fwordLink.Add(ur);
                    _d = _d.Substring(_d.IndexOf(look) + 1, _d.Length - _d.IndexOf(look) - 1);
                }
                linksDone++;
                //SortMovies();

                OnProgressSearchChanged((int)System.Math.Round(linksDone * 100 / totalLinks));
            }
            catch (System.Exception) {

            }
        }
        static void Link3()
        {
            try {


                string kuroani = "https://ww1.kuroani.me/search?term=" + rinput;

                WebClient client = new WebClient();

                string download = client.DownloadString(kuroani);

                // print(download);
                //</div><a href='http://ww1.kuroani.me/anime/ // link, '
                //id='epilink'> //name, <
                //id='headerA_5'><img src=' // poster, '



                string endFile = "id=\'headerA_5'><img src=\'"; // poster ID file 
                while (download.Contains(endFile)) {
                    Movie m = new Movie();
                    string fLink = "https://ww1.kuroani.me/anime/" + FindHTML(download, "</div><a href=\'http://ww1.kuroani.me/anime/", "\'");
                    if (!fwordLink.Contains(fLink)) {
                        fwordLink.Add(fLink); // link
                        m.title = FindHTML(download, "id=\'epilink\'>", "<"); // name
                        m.posterID = FindHTML(download, endFile, "\'"); // poster
                        m.type = "anime";


                        moviesActive.Add(m);
                        movieIsAnime.Add(true);
                        movieTitles.Add(m.title);
                        movieProvider.Add(0);


                    }
                    int end = download.IndexOf(endFile);
                    download = download.Substring(end + endFile.Length, download.Length - end - endFile.Length);
                }
                linksDone++;
                // SortMovies();

                OnProgressSearchChanged((int)System.Math.Round(linksDone * 100 / totalLinks));
            }
            catch (System.Exception) {

            }
        }
        static void Link4()
        {
            try {


                WebClient client = new WebClient();

                string d = client.DownloadString("https://ww.9animes.net/list-all-english-subbed-dubbed-animes/");
                List<string> matches = new List<string>();
                while (d.Contains("</option>")) {
                    string a = FindReverseHTML(d, "</option>", ">");
                    if (FuzzyMatch(a, _serchText)) {
                        matches.Add(a);
                    }
                    d = d.Substring(d.IndexOf("</option>") + 1, d.Length - d.IndexOf("</option>") - 1);
                    //  "<link rel="canonical" href=""
                }
                for (int i = 0; i < matches.Count; i++) {
                    Movie m = new Movie();
                    m.title = matches[i];
                    m.type = "anime";
                    fwordLink.Add(m.title.Replace("-", "").Replace("(English Dubbed)", "").Replace("(English Subbed)", ""));

                    moviesActive.Add(m);
                    movieIsAnime.Add(true);
                    movieTitles.Add(m.title);
                    movieProvider.Add(1);

                }
                linksDone++;
                // SortMovies();
                OnProgressSearchChanged((int)System.Math.Round(linksDone * 100 / totalLinks));
            }
            catch (System.Exception) {

            }
        }
        static void Link5()
        {
            try {


                WebClient client = new WebClient();

                string d = client.DownloadString("https://grabthebeast.com/search/?search_for=" + rinput);
                string lookfor = "\"><a href=\"/tv-series/download-";
                while (d.Contains(lookfor)) {
                    string dd = FindHTML(d, lookfor, "\"");
                    string realNum = dd.Substring(dd.IndexOf("/") + 1, dd.Length - dd.IndexOf("/") - 1);
                    d = d.Substring(d.IndexOf(lookfor) + 1, d.Length - d.IndexOf(lookfor) - 1);

                    fwordLink.Add(realNum);
                    string date = " (" + FindHTML(d, "<b>First Air Date: </b> <b>", "-") + ")";
                    if (date.Contains("<") || date.Contains("Oops! Unknown")) {
                        date = "";
                    }
                    string _title = ToTitle(dd.Replace("/", "").Replace(realNum, "")).Replace(" All Season", "") + date;

                    Movie m = new Movie();
                    m.title = _title;
                    print(_title);
                    moviesActive.Add(m);
                    movieIsAnime.Add(false);
                    movieTitles.Add(m.title);
                    movieProvider.Add(3);


                }
                for (int i = 0; i < movieTitles.Count; i++) {
                    print(movieTitles[i]);
                }
                linksDone++;
                // SortMovies();
                OnProgressSearchChanged((int)System.Math.Round(linksDone * 100 / totalLinks));

            }
            catch {

            }

        }
        static void Link6()
        {
            try {


                WebClient client = new WebClient();

                string d = client.DownloadString("https://zmovies.me/search/" + rinput);
                string lookfor = "<div class=\"name\"> <a href=\"";
                while (d.Contains(lookfor)) {
                    string link = FindHTML(d, lookfor, "\"");
                    string poster = FindHTML(d, "data-original=\"", "\"");
                    string quality = FindHTML(d, "<div class=\"status\">", " </div>").Replace(" ", "").Replace("\n", "");
                    string year = FindHTML(d, "<div class=\"tooltip hide\"></div>", " </div>").Replace(" ", "").Replace("\n", "");
                    print("____");
                    print(poster);
                    print(quality);
                    print(year);

                    d = d.Substring(d.IndexOf(lookfor) + 1, d.Length - d.IndexOf(lookfor) - 1);
                    string title = FindHTML(d, "title=\"", "\"").Replace("Watch ", "");


                    Movie m = new Movie();
                    m.title = title + " (" + quality + ")";
                    m.posterID = poster;
                    m.year = year;

                    moviesActive.Add(m);
                    movieIsAnime.Add(false);
                    movieTitles.Add(m.title);
                    movieProvider.Add(-5);
                    fwordLink.Add(link);

                }

                linksDone++;
                // SortMovies();
                OnProgressSearchChanged((int)System.Math.Round(linksDone * 100 / totalLinks));

            }
            catch {

            }

        }

        static float totalLinks = 6f;
        static string rinput = "";
        static string _serchText;
        static int linksDone = 0;


        static bool SettingsGetChecked(int inp)
        {
            return settingsSearch[inp];
        }
        static void GetTitles(string serchText)
        {
            _serchText = serchText;
            OnProgressSearchChanged(10);


            rinput = serchText.ToLower().Replace(" ", "+");
            string fmovesUrl = "https://www7.fmovies.to/search?keyword=" + rinput;
            string f8url = "http://www8.fmovies.ag/search/" + rinput + ".html";
            string f6utl = "https://www6.fmovie.cc/?s=" + rinput;
            string ksiUrl = "https://kissanime.si/Search/?s=" + (rinput.Replace("+", "%"));
            string k9Url = "https://ww.9animes.net/?s=" + rinput;
            string k8Url = "http://www8.watchanimeonline.cc/?s=" + rinput;
            string an9Url = "https://9anime.fun/search?term=" + rinput;
            string movies123 = "https://movies123.pro/search/" + rinput.Replace("+", " ");
            string aniM = "https://aniwatcher.com/search?q=" + rinput;
            string kuroani = "https://ww1.kuroani.me/search?term=" + rinput;
            string gogoAnime = "https://www3.gogoanime.io//search.html?keyword=" + rinput.Replace("+", "%");



            // if movie




            movieTitles = new List<string>();
            fwordLink = new List<string>();
            activeLinks = new List<string>();
            activeLinksNames = new List<string>();
            moviesActive = new List<Movie>();
            movieIsAnime = new List<bool>();
            movieProvider = new List<int>();

            /*
            if (!disableHD) {
                ReadFile(true, serchText);
                ReadFile(false, serchText);
            }
            */
            linksDone = 0;
            int tLinks = 0;
            MethodInvoker simpleDelegate;
            if (SettingsGetChecked(0) || SettingsGetChecked(8)) {
                tLinks++;
                simpleDelegate = new MethodInvoker(Link1);
                simpleDelegate.BeginInvoke(null, null);
            }
            if (SettingsGetChecked(1)) {
                tLinks++;
                simpleDelegate = new MethodInvoker(Link2);
                simpleDelegate.BeginInvoke(null, null);
            }
            if (SettingsGetChecked(2)) {
                tLinks++;
                simpleDelegate = new MethodInvoker(Link3);
                simpleDelegate.BeginInvoke(null, null);
            }
            if (SettingsGetChecked(3)) {
                tLinks++;
                simpleDelegate = new MethodInvoker(Link4);
                simpleDelegate.BeginInvoke(null, null);
            }
            if (SettingsGetChecked(6)) {
                tLinks++;
                simpleDelegate = new MethodInvoker(Link5);
                simpleDelegate.BeginInvoke(null, null);
            }
            if (SettingsGetChecked(7)) {
                tLinks++;
                simpleDelegate = new MethodInvoker(Link6);
                simpleDelegate.BeginInvoke(null, null);
            }
            totalLinks = tLinks;

            timeoutUpdated = timeout;
            while (timeoutUpdated > 0) {
                if (linksDone < tLinks) {
                    Thread.Sleep(10);
                    timeoutUpdated--;
                }
                else {
                    break;
                }
            }
            SortMovies();
            OnSearchDone(linksDone >= tLinks);


            // string d = client.DownloadString("https://ww.9animes.net/Ueno-san-wa-Bukiyou");
            //print(d);
            //if(provider == 1) {

            //OnProgressSearchChanged(100);

            //ax_Search.ax_search.FinishedSearch();
        }

        public static string ToRoman(int number) // not used
        {
            // if ((number < 0) || (number > 3999)) throw new System. .ArgumentOutOfRangeSystem.Exception("insert value betwheen 1 and 3999");
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900);
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            return "";
            //throw new System.ArgumentOutOfRangeSystem.Exception("something bad happened");
        }

        public static string Find(string all, string first, string end, int offF = 0, int offE = 0)
        {
            int x = all.IndexOf(first) + offF;
            int y = all.IndexOf(end) + offE;
            //  print(x + "|" + y);
            return all.Substring(x, y - x);
        }



        private static string _WebRequest(string url)
        {
            string WEBSERVICE_URL = url;
            try {
                var __webRequest = System.Net.WebRequest.Create(WEBSERVICE_URL);
                if (__webRequest != null) {
                    __webRequest.Method = "GET";
                    __webRequest.Timeout = 12000;
                    __webRequest.ContentType = "application/json";
                    __webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");

                    using (System.IO.Stream s = __webRequest.GetResponse().GetResponseStream()) {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(s)) {
                            var jsonResponse = sr.ReadToEnd();
                            return jsonResponse.ToString();
                            // Console.WriteLine(String.Format("Response: {0}", jsonResponse));
                        }
                    }
                }
            }
            catch (System.Exception ex) {
            }
            return "";
        }

        /// <summary>
        ///  0="HD Movie", 1="TV-Series", 2="Movie", 3="HD Anime", 4="Anime Backup", 5="Anime", 6="TV-Series", 7="Movie Backup", 8="HD Tv-Series"
        ///  0=-3, 1=-2, 2=-1, 3=2, 4=1, 5=0, 6=3, 7=-5, 8=4
        /// </summary>
        public static bool[] settingsSearch = { true, true, true, true, true, true, true, true, true, true, true, true };
        /// <summary>
        ///  0="HD Movie", 1="TV-Series", 2="Movie", 3="HD Anime", 4="Anime Backup", 5="Anime", 6="TV-Series", 7="Movie Backup", 8="HD Tv-Series"
        /// </summary>
        readonly static string[] allProviderNames = { "HD Movie", "TV-Series", "Movie", "HD Anime", "Anime Backup", "Anime", "TV-Series", "Movie Backup", "HD Tv-Series" };
        readonly static string[] allQualityNames = { "sd", "cam", "720p", "1080p", "360p", "480p", "hd" };

        static string RemoveBloatTitle(string title)
        {
            for (int i = 0; i < allProviderNames.Length; i++) {
                title = title.Replace(" (" + allProviderNames[i] + ")", "");
            }
            title = title.Replace("B___", "").Replace(" (Bookmark)", "");
            title = title.ToLower();
            for (int i = 0; i < allQualityNames.Length; i++) {
                title = title.Replace(" (" + allQualityNames[i] + ")", "");
            }

            return title;
        }

        static string SortName(int lastP)
        {
            string add = " (";
            //switch my ass
            if (lastP == -3) {
                add += allProviderNames[0];
            }
            else if (lastP == -2) {
                add += allProviderNames[1];
            }
            else if (lastP == -1) {
                add += allProviderNames[2];
            }
            else if (lastP == 2) {
                add += allProviderNames[3];
            }
            else if (lastP == 1) {
                add += allProviderNames[4];
            }
            else if (lastP == 0) {
                add += allProviderNames[5];
            }
            else if (lastP == 3) {
                add += allProviderNames[6];
            }
            else if (lastP == -5) {
                add += allProviderNames[7];
            }
            else if (lastP == 4) {
                add += allProviderNames[8];
            }
            add += ")";
            return add;
        }

        static Thread tempThred;

        public static void LoadNewSeries(int pos)
        {
            string link = activeLinks[pos];

            string episode = FindHTML(activeLinksNames[pos], "Episode ", ":");
            print("Episode id:" + episode);
            //  List<string> _activeLinks = new List<string>();
            // List<string> _activeLinksNames = new List<string>();
            activeSubtitles = new List<string>();
            activeSubtitlesNames = new List<string>();

            activeLinks = new List<string>();
            activeLinksNames = new List<string>();
            /*
            for (int i = 0; i < activeLinks.Count; i++) {
                if (activeLinksNames[i].StartsWith("Episode")) {
                    _activeLinksNames.Add(activeLinksNames[i]);
                    _activeLinks.Add(activeLinks[i]);
                }
            }
            activeLinks = _activeLinks;
            activeLinksNames = _activeLinksNames;
            */
            thredNumber++;

            tempThred = new Thread(
                 () =>
                 {
                     try {
                         GetUrlFromMovie123(moveSelectedID, thredNumber, "https://movies123.pro" + link, false, episode);

                         //Thread.Sleep(1000);
                     }
                     finally {
                         OnLinksDone(true);
                         tempThred.Join();
                         //invoke(onCompleted);
                     }

                 });
            tempThred.Start();


        }

        static void SortMovies()
        {

            List<string> _movieTitles = new List<string>();
            List<string> _fwordLink = new List<string>();
            List<Movie> _moviesActive = new List<Movie>();
            List<bool> _movieIsAnime = new List<bool>();
            List<int> _movieProvider = new List<int>();

            int[] order = { -3, -1, -5, 4, 3, -2, 2, 0, 1 };

            for (int i = 0; i < movieTitles.Count; i++) {
                if (!movieTitles[i].EndsWith(SortName(movieProvider[i]))) {
                    movieTitles[i] += SortName(movieProvider[i]);
                }
            }


            int[] newPos = new int[movieTitles.Count];
            int counter = 0;

            for (int j = 0; j < order.Length; j++) {
                for (int i = 0; i < movieTitles.Count; i++) {
                    if (movieProvider[i] == order[j]) {
                        newPos[i] = counter;
                        counter++;
                        try {
                            _movieTitles.Add(movieTitles[i]);
                            _moviesActive.Add(moviesActive[i]);
                            _movieIsAnime.Add(movieIsAnime[i]);
                            _movieProvider.Add(movieProvider[i]);
                            _fwordLink.Add(fwordLink[i]);
                        }
                        catch (System.Exception) {
                        }
                    }
                }
            }

            movieTitles = _movieTitles;
            fwordLink = _fwordLink;
            moviesActive = _moviesActive;
            movieIsAnime = _movieIsAnime;
            movieProvider = _movieProvider;

        }


        static string __series = "1399";
        static string __season = "1";
        public static int __totalSeasons = 0;
        static int __currentSeason = 0;
        static List<int> __epsCount = new List<int>();
        static int __totalLinks = 0;
        static int __linksDone = 0;
        static int __cThred = 0;
        public static int __selSeason = 0; //0 == all, other = season


        static int GetTotalEps()
        {
            WebClient client = new WebClient();
            string d = client.DownloadString("https://grabthebeast.com/tv-series/" + __series);
            //   print(d);

            int highest = 0;
            for (int i = 0; i < 100; i++) {
                if (d.Contains("data-season=\"" + i + "\"")) {
                    highest = i;
                }
            }
            return highest;
        }
        static int GetTotalSeasons()
        {
            WebClient client = new WebClient();
            string d = client.DownloadString("https://grabthebeast.com/tv-series/" + __series);
            //   print(d);

            int highest = 0;
            for (int i = 0; i < 100; i++) {
                if (d.Contains("data-season=\"" + i + "\"")) {
                    highest = i;
                }
            }
            return highest;
        }

        static bool _APIDone = false;
        static bool __downloadEps = false;
        static void _GetAPI(int cThred, bool downloadEps = true)
        {
            print("GETAPI TV");
            __downloadEps = downloadEps;
            __currentSeason = 0;
            __totalSeasons = GetTotalSeasons();
            __totalLinks = -1;
            _APIDone = false;
            __epsCount = new List<int>();



            for (int i = 0; i < __totalSeasons; i++) {
                __epsCount.Add(0);
            }
            print("Seasons: " + __totalSeasons);

            GetSeries(cThred);
        }

        static void GetSeries(int cThred)
        {
            //https://grabthebeast.com/stream/1399/season/1/episode/1
            __currentSeason++;
            __cThred = cThred;
            if (__currentSeason <= __totalSeasons) {
                System.Uri myUri = new System.Uri("https://grabthebeast.com/get_series.php");
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(myUri);
                webRequest.Method = "POST";
                webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
                webRequest.Headers.Add("DNT", "1");
                // webRequest.Headers.Add("Cookie", "XSRF-TOKEN=eyJpdiI6Ild0T2ZMTXByZDBvY3hCelVUdno1TXc9PSIsInZhbHVlIjoidGtFOGtFKzkxc1prWGtpWFpid29TbGJiZExYM0J6dWhTZ3JvNERBYUd3dFduTkx3S2hNYlFqUENBSTR5dFE3VzdsZDhENStwT1dBa05WTFo2XC82VTN3PT0iLCJtYWMiOiI2YTVkMjc3MmRmMjdjZTMwNzllYjdlYTMwOWFhYjlmN2Y4ZjE1MTdiYzEwNzUzNWYwYTUyZmNhY2EyNDE5YmZmIn0%3D; watchXyz_session=eyJpdiI6Iks2YlFDMnZpeTlmeFZCRFFacVROUXc9PSIsInZhbHVlIjoidFA1RWpaUmwwSWpjS0Y5NXNUdktwTnlSOWlWN2VIUzBqaDNiaWpDbytCeVFjaEhtK0M2T0VIaE1YNWJ3T2o0WHQwM1czMWdyTXI4bUtnRVg1ekZwTnc9PSIsIm1hYyI6ImIxMjMyMzQyZjc0OWNmMGJjNGNlZGRhZWM2N2U1YTliYzk1ZGJlOGNhZWQ0MTIyNjhmOGI3OTdhMTk1NTA5ZDAifQ%3D%3D");
                webRequest.Headers.Add("Cache-Control", "max-age=0, no-cache");
                webRequest.Headers.Add("TE", "Trailers");
                webRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                webRequest.BeginGetRequestStream(new AsyncCallback(_GetRequestStreamCallback), webRequest);
            }
            else {
                print("Done!!");
                __totalLinks = 0;
                for (int i = 0; i < __epsCount.Count; i++) {
                    print(__epsCount[i] + "__");
                    __totalLinks += __epsCount[i];
                }

                _APIDone = true;
                if (__downloadEps) {
                    //for (int i = 0; i < totalPages; i++) {
                    for (int s = 0; s < __epsCount.Count; s++) {
                        for (int e = 0; e < __epsCount[s]; e++) {
                            DelegateWithParameters_link linkServer =
                           new DelegateWithParameters_link(__GetLinkSeries);

                            IAsyncResult tag =
                                linkServer.BeginInvoke((s + 1).ToString(), (e + 1).ToString(), cThred, null, null);
                        }
                    }
                }
                else {
                }
                //}
            }
        }

        static void __GetLinkSeries(string sp, string ep, int cThred)
        {
            try {


                if (cThred != thredNumber) return;

                WebClient client = new WebClient();
                string d = client.DownloadString("https://grabthebeast.com/stream/" + __series + "/season/" + sp + "/episode/" + ep);
                if (cThred != thredNumber) return;

                //print(d);
                string lookfor = "<source src=\"";

                int hgstSize = 0;
                string displayLink = "";
                while (d.Contains(lookfor)) {
                    string allD = FindHTML(d, lookfor, " />");
                    string link = allD.Substring(0, allD.IndexOf("\""));
                    string size = FindHTML(allD, "size=\"", "\"");
                    // print(link);
                    // print(size);
                    int sz = int.Parse(size);
                    if (sz > hgstSize) {
                        displayLink = link;
                    }
                    if (cThred != thredNumber) return;

                    activeLinks.Add(link);
                    activeLinksNames.Add("Season " + sp + " | Episode " + ep + " (" + size + "p)");

                    d = d.Substring(d.IndexOf(lookfor) + 1, d.Length - d.IndexOf(lookfor) - 1);
                    progress = (int)System.Math.Round((__linksDone * 100f / __totalLinks));
                    try {

                        if (__selSeason == 0 || __selSeason == int.Parse(sp)) {
                            OnProgressLinkChanged(progress);

                        }
                        else {
                            OnProgressLinkChanged(progress);

                        }
                    }
                    catch (System.Exception) {

                    }
                    /*
                    for (int i = 0; i < ax_Links.ax_Links_all.Count; i++) {
                        try {
                            ax_Links.ax_Links_all[i].ChangeBar(progress);
                        }
                        catch (System.Exception) {

                        }
                    }
                    */
                }
                print(displayLink);
                //activeLinks.Add(displayLink);
                //activeLinksNames.Add(movieTitles[moveSelectedID] +  " Season " + sp + " | Episode " + ep);
                __linksDone++;
                if (__linksDone >= __totalLinks) {
                    __done = true;
                    progress = 100;
                    try {

                        if (__selSeason == 0 || __selSeason == int.Parse(sp)) {
                            OnProgressLinkChanged(progress);

                        }
                        else {
                            OnProgressLinkChanged(progress);

                        }
                    }
                    catch (System.Exception) {

                    }
                }
            }
            catch (System.Exception) {

                throw;
            }
        }


        static void _GetRequestStreamCallback(IAsyncResult callbackResult)
        {
            HttpWebRequest webRequest = (HttpWebRequest)callbackResult.AsyncState;
            Stream postStream = webRequest.EndGetRequestStream(callbackResult);

            string requestBody = "series=" + __series + "&season=" + __currentSeason;
            byte[] byteArray = Encoding.UTF8.GetBytes(requestBody);

            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            webRequest.BeginGetResponse(new AsyncCallback(_GetResponseStreamCallback), webRequest);
        }

        static void _GetResponseStreamCallback(IAsyncResult callbackResult)
        {
            HttpWebRequest request = (HttpWebRequest)callbackResult.AsyncState;
            HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(callbackResult);
            using (StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream())) {
                string result = httpWebStreamReader.ReadToEnd();
                //print(result);
                int highest = 0;
                for (int i = 0; i < 100; i++) {
                    if (result.Contains("/stream/" + __series + "/season/" + __currentSeason + "/episode/" + i)) {
                        highest = i;
                    }
                }
                try {
                    __epsCount[__currentSeason - 1] = highest;
                    print("Season: " + __currentSeason + " | TotalEps: " + highest);
                    GetSeries(__cThred);
                }
                catch (System.Exception) {

                }

            }
        }



        // LICENSE
        //
        //   This software is dual-licensed to the public domain and under the following
        //   license: you are granted a perpetual, irrevocable license to copy, modify,
        //   publish, and distribute this file as you see fit.
        /// <summary>
        /// Does a fuzzy search for a pattern within a string.
        /// </summary>
        /// <param name="stringToSearch">The string to search for the pattern in.</param>
        /// <param name="pattern">The pattern to search for in the string.</param>
        /// <returns>true if each character in pattern is found sequentially within stringToSearch; otherwise, false.</returns>
        public static bool FuzzyMatch(string stringToSearch, string pattern)
        {
            var patternIdx = 0;
            var strIdx = 0;
            var patternLength = pattern.Length;
            var strLength = stringToSearch.Length;

            while (patternIdx != patternLength && strIdx != strLength) {
                if (char.ToLower(pattern[patternIdx]) == char.ToLower(stringToSearch[strIdx]))
                    ++patternIdx;
                ++strIdx;
            }

            return patternLength != 0 && strLength != 0 && patternIdx == patternLength;
        }

        /// <summary>
        /// Does a fuzzy search for a pattern within a string, and gives the search a score on how well it matched.
        /// </summary>
        /// <param name="stringToSearch">The string to search for the pattern in.</param>
        /// <param name="pattern">The pattern to search for in the string.</param>
        /// <param name="outScore">The score which this search received, if a match was found.</param>
        /// <returns>true if each character in pattern is found sequentially within stringToSearch; otherwise, false.</returns>
        public static bool FuzzyMatch(string stringToSearch, string pattern, out int outScore)
        {
            // Score consts
            const int adjacencyBonus = 5;               // bonus for adjacent matches
            const int separatorBonus = 10;              // bonus if match occurs after a separator
            const int camelBonus = 10;                  // bonus if match is uppercase and prev is lower

            const int leadingLetterPenalty = -3;        // penalty applied for every letter in stringToSearch before the first match
            const int maxLeadingLetterPenalty = -9;     // maximum penalty for leading letters
            const int unmatchedLetterPenalty = -1;      // penalty for every letter that doesn't matter


            // Loop variables
            var score = 0;
            var patternIdx = 0;
            var patternLength = pattern.Length;
            var strIdx = 0;
            var strLength = stringToSearch.Length;
            var prevMatched = false;
            var prevLower = false;
            var prevSeparator = true;                   // true if first letter match gets separator bonus

            // Use "best" matched letter if multiple string letters match the pattern
            char? bestLetter = null;
            char? bestLower = null;
            int? bestLetterIdx = null;
            var bestLetterScore = 0;

            var matchedIndices = new List<int>();

            // Loop over strings
            while (strIdx != strLength) {
                var patternChar = patternIdx != patternLength ? pattern[patternIdx] as char? : null;
                var strChar = stringToSearch[strIdx];

                var patternLower = patternChar != null ? char.ToLower((char)patternChar) as char? : null;
                var strLower = char.ToLower(strChar);
                var strUpper = char.ToUpper(strChar);

                var nextMatch = patternChar != null && patternLower == strLower;
                var rematch = bestLetter != null && bestLower == strLower;

                var advanced = nextMatch && bestLetter != null;
                var patternRepeat = bestLetter != null && patternChar != null && bestLower == patternLower;
                if (advanced || patternRepeat) {
                    score += bestLetterScore;
                    matchedIndices.Add((int)bestLetterIdx);
                    bestLetter = null;
                    bestLower = null;
                    bestLetterIdx = null;
                    bestLetterScore = 0;
                }

                if (nextMatch || rematch) {
                    var newScore = 0;

                    // Apply penalty for each letter before the first pattern match
                    // Note: Math.Max because penalties are negative values. So max is smallest penalty.
                    if (patternIdx == 0) {
                        var penalty = System.Math.Max(strIdx * leadingLetterPenalty, maxLeadingLetterPenalty);
                        score += penalty;
                    }

                    // Apply bonus for consecutive bonuses
                    if (prevMatched)
                        newScore += adjacencyBonus;

                    // Apply bonus for matches after a separator
                    if (prevSeparator)
                        newScore += separatorBonus;

                    // Apply bonus across camel case boundaries. Includes "clever" isLetter check.
                    if (prevLower && strChar == strUpper && strLower != strUpper)
                        newScore += camelBonus;

                    // Update pattern index IF the next pattern letter was matched
                    if (nextMatch)
                        ++patternIdx;

                    // Update best letter in stringToSearch which may be for a "next" letter or a "rematch"
                    if (newScore >= bestLetterScore) {
                        // Apply penalty for now skipped letter
                        if (bestLetter != null)
                            score += unmatchedLetterPenalty;

                        bestLetter = strChar;
                        bestLower = char.ToLower((char)bestLetter);
                        bestLetterIdx = strIdx;
                        bestLetterScore = newScore;
                    }

                    prevMatched = true;
                }
                else {
                    score += unmatchedLetterPenalty;
                    prevMatched = false;
                }

                // Includes "clever" isLetter check.
                prevLower = strChar == strLower && strLower != strUpper;
                prevSeparator = strChar == '_' || strChar == ' ';

                ++strIdx;
            }

            // Apply score for last match
            if (bestLetter != null) {
                score += bestLetterScore;
                matchedIndices.Add((int)bestLetterIdx);
            }

            outScore = score;
            return patternIdx == patternLength;
        }
    }
}

