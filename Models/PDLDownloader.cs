using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;

using HtmlAgilityPack;

using PDL4.DataModels;

namespace PDL4.Models
{
    class PDLDownloader
    {
        #region Private Classes

        private class PatentDownloadArgs
        {
            public readonly PatentData Patent;
            public readonly WebClient Client;

            public PatentDownloadArgs(PatentData patent, WebClient client)
            {
                Patent = patent;
                Client = client;
            }
        }

        #endregion

        #region Delegates

        public delegate void PatentDownloadFinished(PatentData patent, PatentTimeline time);

        #endregion

        #region Public Properties

        public int MaxDownloadClients { private get; set; }

        public PatentDownloadFinished DownloadFinished { get; set; }

        #endregion

        #region Public Functions

        //Does this need an update? I think it does.
        public async void DownloadSingle(PatentData patent, string directory)
        {
            WebClient c = new WebClient();
            string url = GetPatentDownloadURL(patent);
            string fname = directory + patent.CondensedTitle + ".pdf";

            await c.DownloadFileTaskAsync(url, fname);
        }

        public void DownloadAll(List<PatentData> patents, string directory)
        {
            //Create queue objects
            BlockingCollection<WebClient> client_queue = new BlockingCollection<WebClient>(MaxDownloadClients);
            Queue<string> url_queue = new Queue<string>();

            //Populate client_queue with appropriate number of WebClients
            for (int i = 0; i < MaxDownloadClients; i++)
            {
                var cli = new WebClient();
                //Inject code for thread to run on completion (defined below)
                cli.DownloadFileCompleted += PatentDownloadCompleted;
                client_queue.Add(cli);
            }

            //Perform download operation
            for (int i = 0; i < patents.Count; i++)
            {
                //Grab a webclient off the queue (blocks if unavailable)
                WebClient cli = client_queue.Take();

                //Get download parameters
                PatentData patent = patents[i];
                string url = GetPatentDownloadURL(patent);
                string fname = directory + patent.CondensedTitle + ".pdf";

                if (url == null)
                {
                    DownloadFinished(patent, PatentTimeline.Failed);
                    client_queue.Add(cli);
                }
                else
                    cli.DownloadFileAsync(new Uri(url), fname, new PatentDownloadArgs(patent, cli));
            }

            //Internal method called when a WebClient completes a download
            void PatentDownloadCompleted(object sender, AsyncCompletedEventArgs e)
            {
                PatentDownloadArgs args = (PatentDownloadArgs)e.UserState;

                //Download finished callback
                if (e.Error == null)
                    DownloadFinished(args.Patent, PatentTimeline.Succeeded);
                else
                    DownloadFinished(args.Patent, PatentTimeline.Failed);

                client_queue.Add(args.Client);
            }

        }

        #endregion

        #region Constructor

        public PDLDownloader(int max_clients)
        {
            MaxDownloadClients = max_clients;
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

    }
}
