using System.IO;
using System.Linq;
using System.Collections.Generic;

using PDL4.DataModels;

namespace PDL4.Models
{
    /// <summary>
    /// Enum representing the current state of the application
    /// </summary>
    public enum PDLAppState
    {
        Initial,
        Loaded,
        Downloading,
        Stopped,
        Finished
    }

    /// <summary>
    /// Model reflecting the internal state of the application
    /// </summary>
    class PDLAppModel
    {
        #region Delegates

        /// <summary>
        /// A callback with no return or parameters
        /// </summary>
        public delegate void BasicCallback();

        #endregion

        #region Private Members

        /// <summary>
        /// The current state of the application
        /// </summary>
        private PDLAppState mState = PDLAppState.Initial;

        /// <summary>
        /// A dictionary containing each patent loaded and its download status
        /// </summary>
        private Dictionary<PatentData, PatentStatus> mPatentDictionary;

        /// <summary>
        /// Path to the currently opened file
        /// </summary>
        private string mOpenFilePath = null;
        /// <summary>
        /// Filename of the currently open file
        /// </summary>
        private string mOpenFileName = null;

        /// <summary>
        /// Reference to this AppModel's Downloader
        /// </summary>
        private readonly PDLDownloader mPatentDownloader;

        #endregion

        #region Public Properties

        /// <summary>
        /// The current state of the AppModel
        /// </summary>
        public PDLAppState State
        {
            get
            {
                return mState;
            }

            set
            {
                mState = value;
                StateChangedCallback();
            }
        }

        /// <summary>
        /// The currently open file's path, returns null if none open
        /// </summary>
        public string OpenFilePathString { get { return mOpenFilePath; } }
        /// <summary>
        /// The currently open file's name, returns null if none open
        /// </summary>
        public string OpenFileNameString { get { return mOpenFileName; } }

        /// <summary>
        /// The currently open file's directory, returns null if none open
        /// </summary>
        public string OpenFileDirectoryString
        {
            get
            {
                if (mOpenFilePath != null)
                    return Path.GetDirectoryName(mOpenFilePath) + @"\";
                else
                    return null;
            }
        }

        /// <summary>
        /// A list of patents not yet processed
        /// </summary>
        public List<PatentData> UnprocessedList { get { return GetPatentsByTimeline(PatentTimeline.None); } }
        /// <summary>
        /// A list of patents successfully downloaded
        /// </summary>
        public List<PatentData> SuccessfulList { get { return GetPatentsByTimeline(PatentTimeline.Succeeded); } }
        /// <summary>
        /// A list of patents which failed to download
        /// </summary>
        public List<PatentData> FailedList { get { return GetPatentsByTimeline(PatentTimeline.Failed); } }

        /// <summary>
        /// A full list of patents loaded
        /// </summary>
        public List<PatentData> PatentList
        {
            get
            {
                return mPatentDictionary.Select(pair => pair.Key).ToList();
            }
        }

        /// <summary>
        /// A relay for the downloader's BackgroundWorker progress percentage
        /// </summary>
        public int PatentsProcessedPercentage { get { return mPatentDownloader.DownloadProgressPercentage; } }

        /// <summary>
        /// A callback issued when the AppModel State changes
        /// </summary>
        public BasicCallback StateChangedCallback { get; set; }
        /// <summary>
        /// A callback issued when the download progress increments
        /// </summary>
        public BasicCallback DownloadProgressCallback { get; set; }

        #endregion

        #region Public Functions

        //Exposed download commands
        /// <summary>
        /// Download a patent
        /// </summary>
        /// <param name="patent">The patent to be downloaded</param>
        public void Download(PatentData patent) { mPatentDownloader.Download(patent, OpenFileDirectoryString); State = PDLAppState.Downloading; }
        /// <summary>
        /// Download a list of patents
        /// </summary>
        /// <param name="patents">The list of patents to be downloaded</param>
        public void Download(List<PatentData> patents) { mPatentDownloader.Download(patents, OpenFileDirectoryString); State = PDLAppState.Downloading; }
        /// <summary>
        /// Download all patents currently loaded
        /// </summary>
        public void Download() { mPatentDownloader.Download(PatentList, OpenFileDirectoryString); State = PDLAppState.Downloading; }

        /// <summary>
        /// Intake a file and load the contained patents
        /// </summary>
        /// <param name="path">The path to the file to load</param>
        public void LoadFile(string path)
        {
            mOpenFilePath = path;
            mOpenFileName = Path.GetFileName(path);
            string[] contents = File.ReadAllLines(mOpenFilePath);

            //New dictionary
            mPatentDictionary = new Dictionary<PatentData, PatentStatus>();

            //Create a List<PatentData> to hold our patents and reduce
            List<PatentData> nl = new List<PatentData>();
            foreach (string str in contents)
            {
                PatentData p = new PatentData(str);
                nl.Add(p);
            }
            RemoveDuplicates(ref nl);

            //Build new <PatentData, PatentStatus> Dictionary entries
            foreach (PatentData p in nl)
            {
                PatentStatus s = new PatentStatus();
                mPatentDictionary.Add(p, s);
            }

            State = PDLAppState.Loaded;
        }

        /// <summary>
        /// Checks the status of an input patent, returns null if patent not found
        /// </summary>
        /// <param name="patent">The patent to check</param>
        /// <returns>The status of the input patent</returns>
        public PatentStatus GetPatentStatus(PatentData patent)
        {
            if (mPatentDictionary.ContainsKey(patent))
                return mPatentDictionary[patent];
            else
                return null;
        }

        /// <summary>
        /// Reset the AppModel to its initial state
        /// </summary>
        public void Reset()
        {
            //New dictionary
            mPatentDictionary = new Dictionary<PatentData, PatentStatus>();
            //Null member variables
            mOpenFilePath = null;
            mOpenFileName = null;
            //Set state back to initial
            State = PDLAppState.Initial;
        }

        /// <summary>
        /// Stop an ongoing download
        /// </summary>
        public void Stop()
        {
            //Note: AppState set via callback
            mPatentDownloader.Halt();
        }

        /// <summary>
        /// Resume a stopped download
        /// </summary>
        public void Resume()
        {
            mPatentDownloader.Resume();
            State = PDLAppState.Downloading;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Standard constructor
        /// </summary>
        public PDLAppModel()
        {
            //Create fresh Dictionary<PatentData, PatentStatus> to hold our list
            mPatentDictionary = new Dictionary<PatentData, PatentStatus>();

            //Grab a copy of the downloader to manage downloads, issue callbacks
            mPatentDownloader = new PDLDownloader();
            mPatentDownloader.DownloadProgressedCallback = UpdatePatent;
            mPatentDownloader.DownloadHaltedCallback = () => { State = PDLAppState.Stopped; };
            mPatentDownloader.DownloadFinishedCallback = () => { State = PDLAppState.Finished; };
        }

        #endregion

        #region Private Fns

        /// <summary>
        /// Remove duplicates from a list of patents
        /// </summary>
        /// <param name="list">The list of patents (passed by reference)</param>
        private void RemoveDuplicates(ref List<PatentData> list)
        {
            PatentData cur;
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                cur = list[i];

                for (int j = i + 1; j < count; j++)
                {
                    if (list[j].FormattedTitle == cur.FormattedTitle)
                    {
                        list.RemoveAt(j);
                        count--;
                    }
                }
            }
        }

        /// <summary>
        /// Tag an input patent with the input timeline outcome
        /// </summary>
        /// <param name="patent">The patent to tag</param>
        /// <param name="time">The new timeline state</param>
        private void UpdatePatent(PatentData patent, PatentTimeline time)
        {
            mPatentDictionary[patent].Timeline = time;
            DownloadProgressCallback();
        }

        /// <summary>
        /// Get a list of all loaded patents which are at the input timeline state
        /// </summary>
        /// <param name="t">The timeline state to match</param>
        /// <returns>A list of matching patents</returns>
        private List<PatentData> GetPatentsByTimeline(PatentTimeline t)
        {
            //New list
            List<PatentData> ol = new List<PatentData>();

            foreach (KeyValuePair<PatentData, PatentStatus> kvp in mPatentDictionary)
            {
                if (kvp.Value.Timeline == t)
                    ol.Add(kvp.Key);
            }

            return ol;
        }

        #endregion
    }
}
