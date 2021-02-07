using System.IO;
using System.Linq;
using System.Collections.Generic;

using PDL4.DataModels;
using System;

namespace PDL4.Models
{
    public enum PDLAppState
    {
        Initial,
        Loaded,
        Downloading,
        Stopped,
        Finished
    }

    class PDLAppModel
    {
        #region Delegates

        public delegate void BasicCallback();

        #endregion

        #region Private Members

        private PDLAppState mState = PDLAppState.Initial;

        private Dictionary<PatentData, PatentStatus> mPatentDictionary;

        private string mOpenFilePath = null;
        private string mOpenFileName = null;

        private readonly PDLDownloader mPatentDownloader;

        #endregion

        #region Public Properties

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

        public string OpenFilePathString { get { return mOpenFilePath; } }
        public string OpenFileNameString { get { return mOpenFileName; } }

        public string OpenFileDirectoryString { get { return Path.GetDirectoryName(mOpenFilePath) + @"\"; } } //Probably only works on Windows

        public List<PatentData> UnprocessedList { get { return GetPatentsByTimeline(PatentTimeline.None); } }
        public List<PatentData> SuccessfulList { get { return GetPatentsByTimeline(PatentTimeline.Succeeded); } }
        public List<PatentData> FailedList { get { return GetPatentsByTimeline(PatentTimeline.Failed); } }

        public List<PatentData> PatentList
        {
            get
            {
                return mPatentDictionary.Select(pair => pair.Key).ToList();
            }
        }

        public int PatentsProcessedPercentage { get { return mPatentDownloader.DownloadProgressPercentage; } }

        public BasicCallback StateChangedCallback { get; set; }
        public BasicCallback DownloadProgressCallback { get; set; }

        #endregion

        #region Public Functions

        //Exposed download commands
        public void Download(PatentData patent) { mPatentDownloader.Download(patent, OpenFileDirectoryString); State = PDLAppState.Downloading; }
        public void Download(List<PatentData> patents) { mPatentDownloader.Download(patents, OpenFileDirectoryString); State = PDLAppState.Downloading; }
        public void Download() { mPatentDownloader.Download(PatentList, OpenFileDirectoryString); State = PDLAppState.Downloading; }

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

        public void Stop()
        {
            mPatentDownloader.Halt();
        }

        public void Resume()
        {
            mPatentDownloader.Resume();
            State = PDLAppState.Downloading;
        }

        #endregion

        #region Constructor

        public PDLAppModel()
        {
            //Create fresh Dictionary<PatentData, PatentStatus> to hold our list
            mPatentDictionary = new Dictionary<PatentData, PatentStatus>();

            //Grab a copy of the downloader to manage downloads
            mPatentDownloader = new PDLDownloader();
            mPatentDownloader.DownloadProgressedCallback = UpdatePatent;
            mPatentDownloader.DownloadHaltedCallback = () => { State = PDLAppState.Stopped; };
            mPatentDownloader.DownloadFinishedCallback = () => { State = PDLAppState.Finished; };
        }

        #endregion

        #region Private Fns
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

        private void UpdatePatent(PatentData patent, PatentTimeline time)
        {
            mPatentDictionary[patent].Timeline = time;
            DownloadProgressCallback();
        }

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
