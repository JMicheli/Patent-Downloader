using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using HtmlAgilityPack;

using PDL4.DataModels;

namespace PDL4.Models
{
    class PDLAppModel
    {
        #region Private Members

        private Dictionary<PatentData, PatentStatus> mPatentDictionary;

        private string mOpenFilePath = null;
        private string mOpenFileName = null;

        #endregion

        #region Public Properties

        public string OpenFilePathString { get { return mOpenFilePath; } }
        public string OpenFileNameString { get { return mOpenFileName; } }

        public string OpenFileDirectoryString { get { return Path.GetDirectoryName(mOpenFilePath) +@"\"; } } //Probably only works on Windows

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

        #endregion

        #region Public Functions

        public void DownloadSingle(PatentData patent)
        {
         
        }

        public void DownloadAll()
        {
         
        }

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
                PatentData p = GetPatentFromString(str);
                nl.Add(p);
            }
            RemoveDuplicates(ref nl);

            //Build new <PatentData, PatentStatus> Dictionary entries
            foreach (PatentData p in nl)
            {
                PatentStatus s = new PatentStatus();
                mPatentDictionary.Add(p, s);
            }
        }

        public void Reset()
        {
            //New dictionary
            mPatentDictionary = new Dictionary<PatentData, PatentStatus>();
            //Null member variables
            mOpenFilePath = null;
            mOpenFileName = null;
        }

        #endregion

        #region Constructor

        public PDLAppModel()
        {
            //Create fresh Dictionary<PatentData, PatentStatus> to hold our list
            mPatentDictionary = new Dictionary<PatentData, PatentStatus>();
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
            //We'll take the first tag (in case of multiple)
            HtmlNode n = meta_tags.First();

            //Grab the content from the appropriate node <meta content="x">
            return n.GetAttributeValue("content", "");
        }

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

        private void RemoveDuplicates(ref List<PatentData> list)
        {   
            PatentData cur = null;
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
