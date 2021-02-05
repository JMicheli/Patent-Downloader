namespace PDL4.DataModels
{
    class PatentData
    {
        #region Public Properties

        public string FormattedTitle { get { return CountryCode + " " + GrantNumber.ToString("N0"); } }

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

        public PatentData(string countryCode, int grantNumber)
        {
            CountryCode = countryCode;
            GrantNumber = grantNumber;
        }

        #endregion
    }
}
