using System;
using System.Text.RegularExpressions;

namespace PDL4.DataModels
{
    class PatentData
    {
        #region Public Properties

        public string FormattedTitle { get { return CountryCode + " " + GrantNumber.ToString("N0"); } } //Formated title, e.g. US 9,842,120
        public string CondensedTitle { get { return CountryCode + GrantNumber.ToString(); } } //Condensed title, e.g. US9842120

        public string CountryCode { get; private set; }
        public int GrantNumber { get; private set; }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return FormattedTitle;
        }

        #endregion

        #region Constructor

        public PatentData(string unparsed_string)
        {
            string pat_str = unparsed_string;
            //Sanitize input
            pat_str = pat_str.ToUpper(); //Lowercase letters are never used
            pat_str = pat_str.Replace(";", ""); pat_str = pat_str.Replace(",", "");
            pat_str = pat_str.Replace(" ", ""); pat_str = pat_str.Replace("-", "");
            pat_str = pat_str.Replace(".", ""); pat_str = pat_str.Replace("/", "");
            pat_str = pat_str.Replace("\n", ""); pat_str = pat_str.Replace("\t", "");

            // Remove unicode special characters
            pat_str = Regex.Replace(pat_str, @"[^\u0000-\u007F]+", string.Empty);

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

            CountryCode = cc;
            GrantNumber = gn;
        }

        public PatentData(string countryCode, int grantNumber)
        {
            CountryCode = countryCode;
            GrantNumber = grantNumber;
        }

        #endregion
    }
}
