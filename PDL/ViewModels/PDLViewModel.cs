using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;

using PDL.DataModels;
using PDL.Models;

namespace PDL.ViewModels
{
    /// <summary>
    /// A ViewModel linking the WPF UI to the PDL App classes
    /// </summary>
    class PDLViewModel : BaseViewModel
    {
        #region Private Classes

        /// <summary>
        /// A container class for parameters impacting export
        /// </summary>
        private class ExportParameters
        {
            /// <summary>
            /// Should the successful list be exported?
            /// </summary>
            public bool ExportSuccessful;
            /// <summary>
            /// Should the failed list be exported?
            /// </summary>
            public bool ExportFailed;
            /// <summary>
            /// Should the unprocessed list be exported?
            /// </summary>
            public bool ExportUnprocessed;
            /// <summary>
            /// Should the patent name include the country code?
            /// </summary>
            public bool IncludeCountryCode;
            /// <summary>
            /// Should the patent line include "was successfully downloaded" etc?
            /// </summary>
            public bool IncludeStatusSuffix;
            /// <summary>
            /// Should logging include debugging info?
            /// </summary>
            public bool VerboseLogging;

            /// <summary>
            /// The filename selected by the user
            /// </summary>
            public string FileName;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// A reference to the parent window
        /// </summary>
        private Window mWindow;

        /// <summary>
        /// A reference to the internal app model
        /// </summary>
        private PDLAppModel mAppModel;

        #endregion

        #region Public Properties

        /// <summary>
        /// UI setting for phrase preceding number of patents
        /// </summary>
        public string DistinctPatentsPrefix { get; set; } = "Number of distinct patents: ";
        /// <summary>
        /// UI setting for phrase preceding currently open filename
        /// </summary>
        public string SelectedFilePrefix { get; set; } = "Currently open: ";
        /// <summary>
        /// UI setting for filename to display when none is selected
        /// </summary>
        public string SelectedFileNullSuffix { get; set; } = "Once a file is selected it will appear here";

        /// <summary>
        /// Binding string for the selected file text
        /// </summary>
        public string SelectedFile
        {
            get
            {
                if (mAppModel.OpenFileNameString != null)
                    return SelectedFilePrefix + mAppModel.OpenFileNameString;
                else
                    return SelectedFileNullSuffix;
            }
        }

        /// <summary>
        /// Binding string for the distinct patents text
        /// </summary>
        public string DistinctPatentsString
        {
            get
            {
                if (mAppModel.PatentList.Count > 0)
                    return DistinctPatentsPrefix + mAppModel.PatentList.Count.ToString();
                else
                    return DistinctPatentsPrefix + "None loaded";

            }
        }

        /// <summary>
        /// ListView datasource for successful downloads list
        /// </summary>
        public List<PatentData> SuccessfulDownloads { get { return mAppModel.SuccessfulList; } }
        /// <summary>
        /// ListView datasource for failed downloads list
        /// </summary>
        public List<PatentData> FailedDownloads { get { return mAppModel.FailedList; } }

        //Download control Button state calculations
        /// <summary>
        /// Binding for controling the enabled state of the Load Button
        /// </summary>
        public bool LoadEnabled { get { return !(mAppModel.State == PDLAppState.Downloading); } }
        /// <summary>
        /// Binding for controling the enabled state of the Start Button
        /// </summary>
        public bool StartEnabled { get { return ((mAppModel.State == PDLAppState.Loaded) || (mAppModel.State == PDLAppState.Stopped)); } }
        /// <summary>
        /// Binding for controling the enabled state of the Reset Button
        /// </summary>
        public bool ResetEnabled { get { return !((mAppModel.State == PDLAppState.Initial) || (mAppModel.State == PDLAppState.Downloading)); } }
        /// <summary>
        /// Binding for controling the enabled state of the Stop Button
        /// </summary>
        public bool StopEnabled { get { return mAppModel.State == PDLAppState.Downloading; } }

        //And the text for the start button
        /// <summary>
        /// Binding for what text is displayed in the Start Button
        /// </summary>
        public string StartButtonText
        {
            get
            {
                if (mAppModel.State == PDLAppState.Stopped)
                    return "Resume";
                else
                    return "Start";
            }
        }

        /// <summary>
        /// Binding for controlling the enabled state of the Export Button
        /// </summary>
        public bool ExportEnabled 
        {
            get
            {
                if(mAppModel.State > PDLAppState.Downloading)
                {
                    if (ExportUnprocessedBool && (0 < mAppModel.PatentList.Count - (mAppModel.SuccessfulList.Count + mAppModel.FailedList.Count)))
                        return true;
                    if (ExportSuccessfulBool && (0 < mAppModel.SuccessfulList.Count))
                        return true;
                    if (ExportFailedBool && (0 < mAppModel.FailedList.Count))
                        return true;
                }

                return false;
            }
        }

        //Progress bar stuff
        /// <summary>
        /// Binding for the percentage download progress
        /// </summary>
        public int ProgressBarPercentage 
        {
            get
            {
                if (mAppModel.PatentList.Count <= 0)
                    return 0;
                else
                    return mAppModel.PatentsProcessedPercentage; 
            }
            set { }
        }

        //Bindings for the various CheckBoxes
        /// <summary>
        /// Checked state of the Export Successful export option
        /// </summary>
        public bool ExportSuccessfulBool { get; set; } = true;
        /// <summary>
        /// Checked state of the Export Failed export option
        /// </summary>
        public bool ExportFailedBool { get; set; } = true;
        /// <summary>
        /// Checked state of the Export Unprocessed export option
        /// </summary>
        public bool ExportUnprocessedBool { get; set; } = true;
        /// <summary>
        /// Checked state of the Include country code export option
        /// </summary>
        public bool IncludeCCBool { get; set; } = true;
        /// <summary>
        /// Checked state of the Include state suffix export option
        /// </summary>
        public bool IncludeSSBool { get; set; } = true;
        /// <summary>
        /// Checked state of the Verbose logging export option
        /// </summary>
        public bool VerboseBool { get; set; } = false;

        /// <summary>
        /// Binding for the text in the lower left corner
        /// </summary>
        public string StatusText
        {
            get
            {
                if (mAppModel.State == PDLAppState.Initial)
                    return "Select File";
                else if (mAppModel.State == PDLAppState.Loaded)
                    return "Not Started";
                else if (mAppModel.State == PDLAppState.Stopped)
                    return "Stopped";
                else
                {
                    int numer = mAppModel.SuccessfulList.Count + mAppModel.FailedList.Count;
                    int denom = mAppModel.PatentList.Count;

                    return numer.ToString() + "/" + denom.ToString();
                }
            }
        }

        #endregion

        #region Public Functions

        /// <summary>
        /// External function for the codebehind to use when a file is dropped
        /// onto the window.
        /// </summary>
        /// <param name="file">Path to the file dropped</param>
        public void DragAndDrop(string file)
        {
            //Currently only allowing .txt files
            if (Path.GetExtension(file) == ".txt")
            {
                mAppModel.LoadFile(file); //Pass path into App Model
                NotifyAll(); //Notify of changed properties
            }
        }

        #endregion

        #region Commands

        public ICommand CloseCommand { get; set; }

        public ICommand LoadFileCommand { get; set; }

        public ICommand StartCommand { get; set; }
        public ICommand ResetCommand { get; set; }
        public ICommand StopCommand  { get; set; }

        public ICommand ExportCommand        { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize a viewmodel for an input window
        /// </summary>
        /// <param name="window">Reference to the parent window</param>
        public PDLViewModel(Window window)
        {
            //Initialize private members
            mWindow = window;
            mAppModel = new PDLAppModel();

            //Set AppModel callback
            mAppModel.DownloadProgressCallback = () =>
            {
                OnPropertyChanged(nameof(SuccessfulDownloads));
                OnPropertyChanged(nameof(FailedDownloads));
                OnPropertyChanged(nameof(ProgressBarPercentage));
                OnPropertyChanged(nameof(StatusText));
            };
            mAppModel.StateChangedCallback = NotifyAll;

            //Create commands
            CloseCommand = new RelayCommand(() => mWindow.Close());

            LoadFileCommand = new RelayCommand(() => LoadFile_Click());

            StartCommand = new RelayCommand(Start_Click);
            ResetCommand = new RelayCommand(Reset_Click);
            StopCommand = new RelayCommand(Stop_Click);

            ExportCommand = new RelayCommand(() => { Export_Click(); });
        }

        #endregion

        #region Private Fns

        /// <summary>
        /// Relay point for a LoadFile command
        /// </summary>
        private void LoadFile_Click()
        {
            //Create dialog and set variables
            OpenFileDialog load_diag = new OpenFileDialog();
            load_diag.DefaultExt = ".txt";
            load_diag.Title = "Load a list of patents";
            load_diag.Filter = "Text documents (.txt)|*.txt";

            //Only continue if a file was selected
            if (load_diag.ShowDialog() == true)
            {
                mAppModel.LoadFile(load_diag.FileName); //Pass path into App Model
                NotifyAll(); //Notify of changed properties
            }
        }

        /// <summary>
        /// Relay point for a start command
        /// </summary>
        private void Start_Click()
        {
            if (mAppModel.State == PDLAppState.Stopped)
                mAppModel.Resume();
            else
                mAppModel.Download();
        }

        /// <summary>
        /// Relay point for a reset command
        /// </summary>
        private void Reset_Click() { mAppModel.Reset(); }

        /// <summary>
        /// Relay point for a stop command
        /// </summary>
        private void Stop_Click() { mAppModel.Stop(); }

        /// <summary>
        /// Relay point for starting an export operation
        /// </summary>
        private void Export_Click()
        {
            //Store ExportParameters
            ExportParameters ep = new ExportParameters();
            ep.ExportSuccessful = ExportSuccessfulBool; ep.ExportFailed = ExportFailedBool;
            ep.IncludeCountryCode = IncludeCCBool; ep.IncludeStatusSuffix = IncludeSSBool;
            ep.ExportUnprocessed = ExportUnprocessedBool; ep.VerboseLogging = VerboseBool;

            SaveFileDialog save_dialog = new SaveFileDialog();
            save_dialog.DefaultExt = ".txt";
            save_dialog.Title = "Save a list of results";
            save_dialog.FileName = "Export.txt";
            save_dialog.Filter = "Text documents (.txt)|*.txt";
            save_dialog.OverwritePrompt = true;
            save_dialog.ValidateNames = true;

            //Exit upon cancellation
            if (save_dialog.ShowDialog() != true)
                return;

            ep.FileName = save_dialog.FileName;

            Export_Perform(ep);
        }

        /// <summary>
        /// Performs an export based on input ExportParameters
        /// </summary>
        /// <param name="param">The parameters describing the export</param>
        private void Export_Perform(ExportParameters param)
        {
            List<string> lines = new List<string>();
            foreach (PatentData patent in mAppModel.PatentList)
            {
                //Skip non-applicable patents
                PatentStatus status = mAppModel.GetPatentStatus(patent);
                if (!(ExportUnprocessedBool) && (status.Timeline == PatentTimeline.None))
                    continue;
                if (!(ExportSuccessfulBool) && (status.Timeline == PatentTimeline.Succeeded))
                    continue;
                if (!(ExportFailedBool) && (status.Timeline == PatentTimeline.Failed))
                    continue;

                string line;
                //Basic patent number
                if (param.IncludeCountryCode == true)
                    line = patent.CondensedTitle;
                else
                    line = patent.GrantNumber.ToString();

                if (param.IncludeStatusSuffix)
                {
                    if (status.Timeline == PatentTimeline.None)
                        line += " was not processed";
                    else if (status.Timeline == PatentTimeline.Succeeded)
                        line += " was successfully downloaded";
                    else if (status.Timeline == PatentTimeline.Failed)
                        line += " failed to download";
                    else
                        line += " had an unknown error";
                }

                lines.Add(line);
            }

            //Write contents to file
            File.WriteAllLines(param.FileName, lines);
        }

        /// <summary>
        /// Send notification of changes to every binding property
        /// </summary>
        private void NotifyAll()
        {
            //Labels
            OnPropertyChanged(nameof(SelectedFile));
            OnPropertyChanged(nameof(DistinctPatentsString));
            //Lists
            OnPropertyChanged(nameof(SuccessfulDownloads));
            OnPropertyChanged(nameof(FailedDownloads));
            //Control Buttons
            OnPropertyChanged(nameof(LoadEnabled));
            OnPropertyChanged(nameof(StartEnabled));
            OnPropertyChanged(nameof(ResetEnabled));
            OnPropertyChanged(nameof(StopEnabled));
            OnPropertyChanged(nameof(StartButtonText));
            //Progress bar
            OnPropertyChanged(nameof(ProgressBarPercentage));
            OnPropertyChanged(nameof(StatusText));
            //Export Button
            OnPropertyChanged(nameof(ExportEnabled));
        }

        #endregion
    }
}
