using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using HtmlAgilityPack;

using PDL4.DataModels;

namespace PDL4.Models
{
    class PDLDownloader
    {
        #region Private Classes

        private class DownloadRequest
        {
            public readonly List<PatentData> Patents;
            public readonly string Directory;

            public DownloadRequest(List<PatentData> patents, string directory)
            {
                Patents = patents;
                Directory = directory;
            }
        }

        #endregion

        #region Delegates

        public delegate void PatentDownloadFinished(PatentData patent, PatentTimeline time);
        public delegate void BasicCallback();

        #endregion

        #region Private Members

        private BackgroundWorker mBackgroundWorker;

        private List<PatentData> mResumeList;
        private string mResumeDirectory;

        #endregion

        #region Public Properties

        public int DownloadProgressPercentage { get; private set; } = 0;

        public PatentDownloadFinished DownloadProgressedCallback { get; set; }
        public BasicCallback DownloadHaltedCallback { get; set; }
        public BasicCallback DownloadFinishedCallback { get; set; }


        #endregion

        #region Public Functions

        public void Download(List<PatentData> patents, string directory)
        {
            //Encapsulate BackgroundWorker's data
            var args = new DownloadRequest(patents, directory);
            //Zero progress and set it running
            DownloadProgressPercentage = 0;
            mBackgroundWorker.RunWorkerAsync(args);
        }

        public void Download(PatentData patent, string directory)
        {
            var patents = new List<PatentData>();
            patents.Add(patent);
            Download(patents, directory);
        }

        public void Halt()
        {
            if (mBackgroundWorker.IsBusy)
                mBackgroundWorker.CancelAsync();
        }

        public void Resume()
        {
            Download(mResumeList, mResumeDirectory);
        }

        #endregion

        #region Constructor

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

        private void worker_DownloadWork(object sender, DoWorkEventArgs e)
        {
            //Catch defunct start requests
            var download_request = e.Argument as DownloadRequest;
            if (download_request != null)
            {
                //Set up variables prior to loop
                BackgroundWorker worker = sender as BackgroundWorker;
                WebClient client = new WebClient();
                int total = download_request.Patents.Count;

                //Process each patent in turn (can we make this have multiple simultaneous downloads
                int i;
                for (i = 0; i < total; i++)
                {
                    //Check for cancellation
                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        break;
                    }
                    else
                    {
                        PatentData patent = download_request.Patents[i];

                        string url = GetPatentDownloadURL(patent);
                        string fname = download_request.Directory + patent.CondensedTitle + ".pdf";
                        int progress = Convert.ToInt32(100 * (float)(i + 1) / (float)total);

                        //Catch url failures
                        if (url == null)
                        {
                            DownloadProgressedCallback(patent, PatentTimeline.Failed);
                            worker.ReportProgress(progress);
                            continue;
                        }

                        client.DownloadFile(url, fname);
                        DownloadProgressedCallback(patent, PatentTimeline.Succeeded);
                        worker.ReportProgress(progress);
                    }
                }

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

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            DownloadProgressPercentage = e.ProgressPercentage;
        }

        #endregion
    }




}
