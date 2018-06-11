
using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Client.Tracker;
using MonoTorrent.Common;
using MonoTorrent.Dht;
using MonoTorrent.Dht.Listeners;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;


namespace torrentconsol
{
    static class Program
    {
        static string basePaths;
        static string dhtNodeFile;
        static string basePath;
        static string downloadsPath;
        static string fastResumeFile;
        static string torrentsPath;
        static ClientEngine engine;				// The engine used for downloading
        static List<TorrentManager> torrents;	// The list where all the torrentManagers will be stored that the engine gives us
        static Top10Listener listener;
      
        private static string ftg;
     
        private static bool kjl;

        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Console.Write("Bienvenue sur le programe Pour la Compagnie" + Environment.NewLine + "nous sommes fier de vous presenter" + Environment.NewLine + " linterface avec intelligence artificiel" + Environment.NewLine + "elle peut apprendre evoluer et changer");
            Console.Write("");
            Console.Write("--------");
            Console.Write("Creer par : YAN BERGERON");
            Console.Write("--------");

            string fth = File.ReadAllText("directory.tx");
            ftg = fth;
            System.IO.Directory.SetCurrentDirectory(fth);
            try
            {
                //Set the current directory.
                Directory.SetCurrentDirectory(ftg);
                Environment.CurrentDirectory = ftg;

                Properties.Settings.Default.Save();
            }
            catch (DirectoryNotFoundException e)
            {
                Console.Write("The specified directory does not exist. {0}", e);
            }
            // Print to console the results.
            Console.Write("Root directory: {0}", Directory.GetDirectoryRoot(ftg));
            Console.Write("Current directory: {0}", Directory.GetCurrentDirectory());



            try
            {
                string myHost = System.Net.Dns.GetHostName();
                string myIP = null;

                for (int i = 0; i <= System.Net.Dns.GetHostEntry(myHost).AddressList.Length - 1; i++)
                {
                    if (System.Net.Dns.GetHostEntry(myHost).AddressList[i].IsIPv6LinkLocal == false)
                    {
                        myIP = System.Net.Dns.GetHostEntry(myHost).AddressList[i].ToString();
                    }
                }
                Console.Write("--------");
                Console.Write("local ip : " + myIP);
                var xg = Environment.SystemPageSize;
                Console.Write("--------");
                Console.Write("memory paging loaded : " + xg);
                string[] sss = Environment.GetLogicalDrives();
                Console.Write("logical drives : ");
                foreach (string bf in sss)
                {
                    Console.Write(bf);
                }
                var stt = Environment.MachineName;
                Console.Write("");
                Console.Write("--------");
                Console.Write("machinne name : " + stt);
                int tre = Environment.ProcessorCount;
                Console.Write("--------");
                Console.Write("processor count : " + tre);
           
                var sut = Environment.UserDomainName;
                Console.Write("--------");
                Console.Write("network name : " + myHost);
                var scx = Environment.UserName;
                Console.Write("--------");
                Console.Write("user name : " + scx);
                var syr = Environment.Version;
                Console.Write("--------");
                var syre = Environment.OSVersion;
                Console.Write("os ver : " + syre);
                Console.Write("--------");
                Console.Write("ver : " + syr);
              
                Console.Write("--------");

                var sfg = Environment.UserInteractive;
                Console.Write("interactif : " + sfg);
            }
            catch { Console.Write("error environement"); }

            button59_Click(null, null);
        }
        public static void StartEngine()
        {
            Console.Write("starting");
   
               Thread sgd = new Thread(phj);
            sgd.Start();
        }

        public static void phj()
        {
            int port = 8988;
            Torrent torrent = null;




            // Create the settings which the engine will use downloadsPath - this is the path where
            // we will save all the files to port - this is the port we listen for connections on
            EngineSettings engineSettings = new EngineSettings(ftg, port);
            engineSettings.PreferEncryption = false;
            engineSettings.AllowedEncryption = EncryptionTypes.All;

            //engineSettings.GlobalMaxUploadSpeed = 30 * 1024;
            //engineSettings.GlobalMaxDownloadSpeed = 100 * 1024;
            //engineSettings.MaxReadRate = 1 * 1024 * 1024;


            // Create the default settings which a torrent will have. 4 Upload slots - a good ratio
            // is one slot per 5kB of upload speed 50 open connections - should never really need to
            // be changed Unlimited download speed - valid range from 0 -> int.Max Unlimited upload
            // speed - valid range from 0 -> int.Max
            TorrentSettings torrentDefault = new TorrentSettings(14, 150, 0, 0);

            // Create an instance of the engine.
            ClientEngine engine = new ClientEngine(engineSettings);
            engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.LoopbackMask, port));
            byte[] nodes = null;
            try
            {
                nodes = File.ReadAllBytes(dhtNodeFile);
            }
            catch
            {
                Console.Write("No existing dht nodes could be loaded");
            }

            DhtListener dhtListner = new DhtListener(new IPEndPoint(IPAddress.LoopbackMask, port));
            DhtEngine dht = new DhtEngine(dhtListner);
            engine.RegisterDht(dht);
            dhtListner.Start();
            engine.DhtEngine.Start(nodes);

            // If the SavePath does not exist, we want to create it.
            if (!Directory.Exists(engine.Settings.SavePath))
                Directory.CreateDirectory(engine.Settings.SavePath);

            // If the torrentsPath does not exist, we want to create it
            if (!Directory.Exists(torrentsPath))
                Directory.CreateDirectory(torrentsPath);

            BEncodedDictionary fastResume;
            try
            {
                Console.Write("resume file");
   
                   fastResume = BEncodedValue.Decode<BEncodedDictionary>(File.ReadAllBytes(fastResumeFile));
            }
            catch
            {
                Console.Write("new file");
   
                   fastResume = new BEncodedDictionary();
            }

            // For each file in the torrents path that is a .torrent file, load it into the engine.
            foreach (string file in Directory.GetFiles(torrentsPath))
            {
                if (file.EndsWith(".torrent"))
                {
                    try
                    {
                        // Load the .torrent from the file into a Torrent instance You can use this
                        // to do preprocessing should you need to
                        torrent = Torrent.Load(file);
                        Console.Write(torrent.InfoHash.ToString()); Console.Write("load " + file);
                    }
                    catch (Exception e)
                    {
                        Console.Write("Couldn't decode {0}: ", file);
                        Console.Write(e.Message);
                        continue;
                    }
                    // When any preprocessing has been completed, you create a TorrentManager which
                    // you then register with the engine.
                    TorrentManager manager = new TorrentManager(torrent, downloadsPath, torrentDefault);
                    if (fastResume.ContainsKey(torrent.InfoHash.ToHex()))
                        manager.LoadFastResume(new FastResume((BEncodedDictionary)fastResume[torrent.InfoHash.ToHex()]));
                    engine.Register(manager);

                    // Store the torrent manager in our list so we can access it later
                    torrents.Add(manager);
                    manager.PeersFound += new EventHandler<PeersAddedEventArgs>(manager_PeersFound);
                }
            }

            // If we loaded no torrents, just exist. The user can put files in the torrents directory
            // and start the client again
            if (torrents.Count == 0)
            {
                Console.Write("No torrents found in the Torrents directory");
                Console.Write("Exiting...");
                engine.Dispose();
                return;
            }

            // For each torrent manager we loaded and stored in our list, hook into the events in the
            // torrent manager and start the engine.
            foreach (TorrentManager manager in torrents)
            {
                // Every time a piece is hashed, this is fired.
                manager.PieceHashed += delegate (object o, PieceHashedEventArgs e) {
                    lock (listener)
                        listener.WriteLine(string.Format("Piece Hashed: {0} - {1}", e.PieceIndex, e.HashPassed ? "Pass" : "Fail"));
                };

                // Every time the state changes (Stopped -> Seeding -> Downloading -> Hashing) this
                // is fired
                manager.TorrentStateChanged += delegate (object o, TorrentStateChangedEventArgs e) {
                    lock (listener)
                        listener.WriteLine("OldState: " + e.OldState.ToString() + " NewState: " + e.NewState.ToString());
                };

                // Every time the tracker's state changes, this is fired
                foreach (TrackerTier tier in manager.TrackerManager)
                {
                    foreach (MonoTorrent.Client.Tracker.Tracker t in tier)
                    {
                        t.AnnounceComplete += delegate (object sender, AnnounceResponseEventArgs e) {
                            listener.WriteLine(string.Format("{0}: {1}", e.Successful, e.Tracker.ToString()));
                        };
                    }
                }
                // Start the torrentmanager. The file will then hash (if required) and begin downloading/seeding
                manager.Start();
            }

            // While the torrents are still running, print out some stats to the screen. Details for
            // all the loaded torrent managers are shown.
            int i = 0;
            bool running = true;
            StringBuilder sb = new StringBuilder(1024);
            while (running)
            {
                if ((i++) % 10 == 0)
                {
                    sb.Remove(0, sb.Length);
                    running = torrents.Exists(delegate (TorrentManager m) { return m.State != TorrentState.Stopped; });

                    AppendFormat(sb, "Total Download Rate: {0:0.00}kB/sec", engine.TotalDownloadSpeed / 1024.0);
                    AppendFormat(sb, "Total Upload Rate:   {0:0.00}kB/sec", engine.TotalUploadSpeed / 1024.0);
                    AppendFormat(sb, "Disk Read Rate:      {0:0.00} kB/s", engine.DiskManager.ReadRate / 1024.0);
                    AppendFormat(sb, "Disk Write Rate:     {0:0.00} kB/s", engine.DiskManager.WriteRate / 1024.0);
                    AppendFormat(sb, "Total Read:         {0:0.00} kB", engine.DiskManager.TotalRead / 1024.0);
                    AppendFormat(sb, "Total Written:      {0:0.00} kB", engine.DiskManager.TotalWritten / 1024.0);
                    AppendFormat(sb, "Open Connections:    {0}", engine.ConnectionManager.OpenConnections);

                    foreach (TorrentManager manager in torrents)
                    {
                        AppendSeperator(sb);
                        AppendFormat(sb, "State:           {0}", manager.State);
                        AppendFormat(sb, "Name:            {0}", manager.Torrent == null ? "MetaDataMode" : manager.Torrent.Name);
                        AppendFormat(sb, "Progress:           {0:0.00}", manager.Progress);
                        AppendFormat(sb, "Download Speed:     {0:0.00} kB/s", manager.Monitor.DownloadSpeed / 1024.0);
                        AppendFormat(sb, "Upload Speed:       {0:0.00} kB/s", manager.Monitor.UploadSpeed / 1024.0);
                        AppendFormat(sb, "Total Downloaded:   {0:0.00} MB", manager.Monitor.DataBytesDownloaded / (1024.0 * 1024.0));
                        AppendFormat(sb, "Total Uploaded:     {0:0.00} MB", manager.Monitor.DataBytesUploaded / (1024.0 * 1024.0));
                        MonoTorrent.Client.Tracker.Tracker tracker = manager.TrackerManager.CurrentTracker;
                        //AppendFormat(sb, "Tracker Status:     {0}" , tracker == null ? "<no tracker>" : tracker.State.ToString());
                        AppendFormat(sb, "Warning Message:    {0}", tracker == null ? "<no tracker>" : tracker.WarningMessage);
                        AppendFormat(sb, "Failure Message:    {0}", tracker == null ? "<no tracker>" : tracker.FailureMessage);
                        if (manager.PieceManager != null)
                            AppendFormat(sb, "Current Requests:   {0}", manager.PieceManager);

                        foreach (PeerId p in manager.GetPeers())
                            AppendFormat(sb, "\t{2} - {1:0.00}/{3:0.00}kB/sec - {0}", p.PeerID,
                                                                                      p.Monitor.DownloadSpeed / 1024.0,
                                                                                      p.AmRequestingPiecesCount,
                                                                                      p.Monitor.UploadSpeed / 1024.0);

                        AppendFormat(sb, "", null);
                        if (manager.Torrent != null)
                            foreach (TorrentFile file in manager.Torrent.Files)
                                AppendFormat(sb, "{1:0.00}% - {0}", file.Path, file.BitField.PercentComplete);

                        AppendFormat(sb, "Total Download Rate: {0:0.00}kB/sec", engine.TotalDownloadSpeed / 1024.0);
                        AppendFormat(sb, "Total Upload Rate:   {0:0.00}kB/sec", engine.TotalUploadSpeed / 1024.0);
                    }
                    Console.Clear();
                    Console.Write(sb.ToString());
                    listener.ExportTo(Console.Out);
                }

                System.Threading.Thread.Sleep(500);
            }
        }

        public static void AppendFormat(StringBuilder sb, string str, params object[] formatting)
        {
            if (formatting != null)
                sb.AppendFormat(str, formatting);
            else
                sb.Append(str);
            sb.AppendLine();
        }
        static void manager_PeersFound(object sender, PeersAddedEventArgs e)
        {
            lock (listener)
                listener.WriteLine(string.Format("Found {0} new peers and {1} existing peers", e.NewPeers, e.ExistingPeers));//throw new Exception("The method or operation is not implemented.");
        }

        public static void AppendSeperator(StringBuilder sb)
        {
            AppendFormat(sb, "", null);
            AppendFormat(sb, "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - -", null);
            AppendFormat(sb, "", null);
        }

        public static void button59_Click(object sender, EventArgs e)
        {
            basePath = ftg;
            basePaths = ftg;	// This is the directory we are currently in
            torrentsPath = basePath;				// This is the directory we will save .torrents to
            downloadsPath = basePaths;         // This is the directory we will save downloads to
            if (!File.Exists(basePath+ "/fastresume.data"))
            {
                Console.Write("copy fastresume");
   
                   File.Copy("fastresume.data", basePath + "/fastresume.data");
            }

            fastResumeFile = torrentsPath+ "/fastresume.data";

            if (!File.Exists(basePath+ "/DhtNodes"))
            {
                Console.Write("copy node");
   
                   File.Copy("DhtNodes", basePath + "/DhtNodes");
            }

            dhtNodeFile = basePath+ "/DhtNodes";
            torrents = new List<TorrentManager>();							// This is where we will store the torrentmanagers
            listener = new Top10Listener(10);

            string[] fgt = Directory.GetFiles(basePath, "*.torrent");
            foreach (string fy in fgt)
            {
                if (fy.Contains(".torrent"))
                {
                    Console.Write("true contain");
   
                       kjl = true;
                }
                else
                {
                    Console.Write("false contain");
   
                       kjl = false;
                }
            }
            if (kjl)
            {
                StartEngine();

            }
            else
            {
                FolderBrowserDialog ljk = new FolderBrowserDialog();
                ljk.ShowDialog();
                string hfj = ljk.SelectedPath;

                string[] fgts = Directory.GetFiles(ljk.SelectedPath, "*.torrent");
                Console.Write("get to " + ljk.SelectedPath);
                foreach (string fy in fgts)
                {
                    if (fy.Contains(".torrent"))
                    {
                        kjl = true;
                        basePath = ljk.SelectedPath;
                        StartEngine();
                    }
                    else
                    {
                        kjl = false;
                        Application.DoEvents(); MessageBox.Show("error no .torrent");
                    }
                }
            }
            try
            {
                String jg = "tada.wav"; 
                Application.DoEvents();
                SoundPlayer hv = new SoundPlayer();

                hv.SoundLocation = jg;
                hv.Play();
            }
            catch { }
        }


    }
}
