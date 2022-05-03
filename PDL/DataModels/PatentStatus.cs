using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDL.DataModels
{
    #region Status Enums

    public enum PatentTimeline
    {
        None,
        Succeeded,
        Failed
    }

    public enum PatentError
    {
        None,
        InvalidLine,
        NoUrlFound,
        DownloadFailed
    }

    #endregion

    class PatentStatus
    {
        #region Public Properties

        public PatentTimeline Timeline { get; set; }
        public PatentError Error { get; set; }

        public string TimelineString
        {
            get
            {
                switch(Timeline)
                {
                    case PatentTimeline.None:
                        return "None";
                    case PatentTimeline.Succeeded:
                        return "Succeeded";
                    case PatentTimeline.Failed:
                        return "Failed";
                }

                return null; //Shouldn't ever happen
            }
        }

        #endregion

        #region Constructor

        public PatentStatus()
        {
            Timeline = PatentTimeline.None;
            Error = PatentError.None;
        }

        #endregion
    }
}
