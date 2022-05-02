using System;
using System.Net;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;

using HtmlAgilityPack;

using PDL4.DataModels;
using System.Threading.Tasks;

namespace PDL4.Models
{
    /// <summary>
    /// A class which handles the download operations for the App,
    /// should be a private member of the AppModel.
    /// </summary>
    class PDLDownloader
    {
        #region Private Classes
        
        /// <summary>
        /// Private class encompassing a download request's data
        /// </summary>
        private class DownloadRequest
        {
            public readonly List<PatentData> Patents;
            public readonly string Directory;

            /// <summary>
            /// Default constructor
            /// </summary>
            /// <param name="patents">List of patents to be downloaded</param>
            /// <param name="directory">Directory to which they will be downloaded</param>
            public DownloadRequest(List<PatentData> patents, string directory)
            {
                Patents = patents;
                Directory = directory;
            }
        }

        #endregion

        #region Delegates

        /// <summary>
        /// A delegate for reporting a finished download
        /// </summary>
        /// <param name="patent">The patent processed</param>
        /// <param name="time">The timeline state it is placed into</param>
        public delegate void PatentDownloadFinished(PatentData patent, PatentTimeline time);
        /// <summary>
        /// A callback with no return or parameters
        /// </summary>
        public delegate void BasicCallback();

        #endregion

        #region Private Members

        /// <summary>
        /// The background worker which will handle downloading
        /// </summary>
        private BackgroundWorker mBackgroundWorker;

        /// <summary>
        /// A patent list stored to resume a stopped download
        /// </summary>
        private List<PatentData> mResumeList;
        /// <summary>
        /// A directory stored to resume a stopped download
        /// </summary>
        private string mResumeDirectory;

        #endregion

        #region Public Properties

        /// <summary>
        /// A public handle for the BackgroundWorker's download percentage
        /// </summary>
        public int DownloadProgressPercentage { get; private set; } = 0;

        /// <summary>
        /// A callback issued when a patent finishes being processed
        /// </summary>
        public PatentDownloadFinished DownloadProgressedCallback { get; set; }
        /// <summary>
        /// A callback issued when the download process is stopped
        /// </summary>
        public BasicCallback DownloadHaltedCallback { get; set; }
        /// <summary>
        /// A callback issued when the download process reaches its end
        /// </summary>
        public BasicCallback DownloadFinishedCallback { get; set; }


        #endregion

        #region Public Functions

        /// <summary>
        /// Begin downloading a list of patents to a directory
        /// </summary>
        /// <param name="patents">The list of patents to be downloaded</param>
        /// <param name="directory">The directory to download them to</param>
        public void Download(List<PatentData> patents, string directory)
        {
            //Encapsulate BackgroundWorker's data
            var args = new DownloadRequest(patents, directory);
            //Zero progress and set it running
            DownloadProgressPercentage = 0;
            mBackgroundWorker.RunWorkerAsync(args);
        }

        /// <summary>
        /// Download a single patent to a directroy
        /// </summary>
        /// <param name="patent">The patent to be downloaded</param>
        /// <param name="directory">The directory to download it to</param>
        public void Download(PatentData patent, string directory)
        {
            var patents = new List<PatentData>();
            patents.Add(patent);
            Download(patents, directory);
        }

        /// <summary>
        /// Tell the BackgroundWorker to stop as soon as possible
        /// </summary>
        public void Halt()
        {
            if (mBackgroundWorker.IsBusy)
                mBackgroundWorker.CancelAsync();
        }

        /// <summary>
        /// Restart a stopped download where it left off
        /// </summary>
        public void Resume()
        {
            //Resuming is just starting a new download with the resume variables
            Download(mResumeList, mResumeDirectory);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Standard construtor
        /// </summary>
        public PDLDownloader()
        {
            //Set up background worker
            mBackgroundWorker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            //Insert delegates
            mBackgroundWorker.DoWork += worker_DownloadWork;
            mBackgroundWorker.ProgressChanged += worker_ProgressChanged;
        }

        #endregion

        #region Private Fns

        //  NOTE: THIS IS WHAT NEEDS TO BE REPLACED WHEN DOWNLOADING BREAKS
        /// <summary>
        /// Creates a url to Google Patents' PDF scan of a given patent
        /// based on the current setup of their page.
        /// </summary>
        /// <param name="patent">The patent to collect a URL for</param>
        /// <returns>string containing url to PDF target</returns>
        private string GetPatentDownloadURL(PatentData patent)
        {
            string pg_url = "https://patents.google.com/patent/" + patent.CondensedTitle;

            //Rip Google Patents page
            var pg_doc = new HtmlWeb().Load(pg_url);
            //Grab the <meta name="citation_pdf_url"> tag where we expect to find the PDF url
            //Note: We only expect one tag but this code doesn't forbid multiple
            var meta_tags = pg_doc.DocumentNode.Descendants("meta").Where(node => node.OuterHtml.Contains("citation_pdf_url"));

            //If the patent number was invalid then the webpage won't have meta tags

            //If the Google Patents page doesn't exist then there will be no meta tags
            if (meta_tags.Count() <= 0)
                return null;
            //We'll take the first tag (in case of multiple)
            HtmlNode n = meta_tags.First();

            //Grab the content from the appropriate node <meta content="x">
            return n.GetAttributeValue("content", "");
        }

        #endregion

        #region Background Worker

        /// <summary>
        /// The work performed by the BackgroundWorker
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void worker_DownloadWork(object sender, DoWorkEventArgs e)
        {
            //Catch defunct start requests
            var download_request = e.Argument as DownloadRequest;
            if (download_request != null)
            {
                //Set up variables prior to loop
                BackgroundWorker worker = sender as BackgroundWorker;

                //Progress setup
                int i = 0; //Counting total patents done
                int total = download_request.Patents.Count;

                //Initiate parallel download operations
                Parallel.ForEach(download_request.Patents,
                    new ParallelOptions { MaxDegreeOfParallelism = 10 },
                    (patent, state) =>
                {
                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        state.Break();
                    }
                    else
                    {
                        string url = GetPatentDownloadURL(patent);
                        string fname = download_request.Directory + patent.CondensedTitle + ".pdf";

                        if (url == null)
                        {
                            DownloadProgressedCallback(patent, PatentTimeline.Failed);
                            i++; worker.ReportProgress((i * 100) / total);
                        }
                        else
                        {
                            WebClient client = new WebClient();
                            client.DownloadFile(url, fname);
                            DownloadProgressedCallback(patent, PatentTimeline.Succeeded);
                            i++; worker.ReportProgress((i * 100) / total);
                        }
                    }
                });

                //Make appropriate callback based on why we're ending
                if (worker.CancellationPending)
                {
                    //Store the remaining list for download resuming
                    mResumeList = download_request.Patents;
                    mResumeList.RemoveRange(0, i);
                    mResumeDirectory = download_request.Directory;

                    DownloadHaltedCallback();
                }
                else
                    DownloadFinishedCallback();
            }
        }

        /// <summary>
        /// A callback issued by the worker when progress is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            DownloadProgressPercentage = e.ProgressPercentage;
        }

        #endregion
    }




}
