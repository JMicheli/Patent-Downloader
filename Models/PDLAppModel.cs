using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using PDL4.DataModels;
using System.Text.RegularExpressions;
using System.Net;
using HtmlAgilityPack;

namespace PDL4.Models
{
    class PDLAppModel
    {
        #region Private Members

        private List<PatentData> mPatentList = null;

        private string mOpenFilePath = null;
        private string mOpenFileName = null;

        private List<PatentData> mSuccessfulList;
        private List<PatentData> mFailedList;

        private WebClient mClient;

        #endregion

        #region Public Properties

        public List<PatentData> PatentList { get { return mPatentList; } }

        public string OpenFilePathString { get { return mOpenFilePath; } }
        public string OpenFileNameString { get { return mOpenFileName; } }

        public string OpenFileDirectoryString { get { return Path.GetDirectoryName(mOpenFilePath) +@"\"; } } //Probably only works on Windows

        public List<PatentData> SuccessfulList { get { return PatentList; } } //TEMPORARY: We'll want to change this to just log successes later
        public List<PatentData> FailedList { get { return mFailedList; } }

        #endregion

        #region Public Functions

        public void DownloadToLocation(PatentData patent, string location)
        {
            //Don't do it if we have no file open, consider changing later
            if (!(mOpenFilePath == null))
            {
                string file_name = OpenFileDirectoryString + patent.CondensedTitle + ".pdf";
                mClient.DownloadFile(GetPatentDownloadURL(patent), file_name);
            }
        }

        public void FullListDownload(string location)
        {
            //Still check if a file is loaded to avoid unnecessary looping
            if (!(mOpenFilePath == null))
            {
                foreach (PatentData p in mPatentList)
                    DownloadToLocation(p, location);
            }
        }

        public void LoadFile(string path)
        {
            mOpenFilePath = path;
            mOpenFileName = Path.GetFileName(path);
            string[] contents = File.ReadAllLines(mOpenFilePath);

            //Build a new PatentData List object
            List<PatentData> nl = new List<PatentData>();
            foreach (string str in contents)
            {
                PatentData p = GetPatentFromString(str);
                nl.Add(p);
            }

            mPatentList = RemoveDuplicates(nl);
        }

        public void Reset()
        {
            //Null member variables
            mPatentList = null;
            mOpenFilePath = null;
            mOpenFileName = null;

            //Reset success lists
            mSuccessfulList = new List<PatentData>();
            mFailedList = new List<PatentData>();
        }

        #endregion

        #region Constructor

        public PDLAppModel()
        {
            mSuccessfulList = new List<PatentData>();
            mFailedList = new List<PatentData>();

            //Get a WebClient we'll use for downloads
            mClient = new WebClient();
        }

        #endregion

        #region Private Fns

        private PatentData GetPatentFromString(string input)
        {
            string pat_str = input;
            //Sanitize input
            pat_str = pat_str.ToUpper(); //Lowercase letters are never used
            pat_str = pat_str.Replace(";", ""); pat_str = pat_str.Replace(",", "");
            pat_str = pat_str.Replace(" ", ""); pat_str = pat_str.Replace("-", "");
            pat_str = pat_str.Replace(".", ""); pat_str = pat_str.Replace("/", "");
            pat_str = pat_str.Replace("\n", ""); pat_str = pat_str.Replace("\t", "");

            //Determine country code
            string cc = "US"; //Assume US unless there is a code in the input
            if (char.IsLetter(pat_str, 0)) //There is a country code
            {
                //Find index of the character after the country code
                int cci = 1;
                while (cci < pat_str.Length - 1)
                {
                    if (char.IsDigit(pat_str, cci))
                        break;
                    else
                        cci++;
                }

                cc = pat_str.Substring(0, cci); //Store country code
                pat_str = pat_str.Substring(cci, pat_str.Length - cci); //Grab only the non-code part
            }
            //Detect the presence of a Letter+Number at the end of the patent number e.g. A1, B2, etc.
            if ((char.IsLetter(pat_str, pat_str.Length - 2)) && (char.IsDigit(pat_str, pat_str.Length - 2)))
                pat_str = pat_str.Substring(0, pat_str.Length - 2); //Discard it

            //Remaining string should just be the grant number, convert it to int
            int gn = Int32.Parse(pat_str);

            return new PatentData(cc, gn);
        }

        private List<PatentData> RemoveDuplicates(List<PatentData> initial)
        {
            List<PatentData> p = initial;
            
            PatentData cur = null;
            int count = p.Count;
            for (int i = 0; i < count; i++)
            {
                cur = p[i];

                for (int j = i + 1; j < count; j++)
                {
                    if (p[j].FormattedTitle == cur.FormattedTitle)
                    {
                        p.RemoveAt(j);
                        count--;
                    }
                }
            }

            return p;
        }

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
            //We'll take the first tag (in case of multiple)
            HtmlNode n = meta_tags.First();
            
            //Grab the content from the appropriate node <meta content="x">
            return n.GetAttributeValue("content", "");
        }

        #endregion
    }
}
