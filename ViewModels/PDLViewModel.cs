using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;

using PDL4.DataModels;
using PDL4.Models;

namespace PDL4.ViewModels
{
    /// <summary>
    /// A ViewModel linking the WPF UI to the PDL App classes
    /// </summary>
    class PDLViewModel : BaseViewModel
    {
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
        /// Binding for controling the enabled state of the load button
        /// </summary>
        public bool LoadEnabled { get { return !(mAppModel.State == PDLAppState.Downloading); } }
        /// <summary>
        /// Binding for controling the enabled state of the start button
        /// </summary>
        public bool StartEnabled { get { return ((mAppModel.State == PDLAppState.Loaded) || (mAppModel.State == PDLAppState.Stopped)); } }
        /// <summary>
        /// Binding for controling the enabled state of the reset button
        /// </summary>
        public bool ResetEnabled { get { return !((mAppModel.State == PDLAppState.Initial) || (mAppModel.State == PDLAppState.Downloading)); } }
        /// <summary>
        /// Binding for controling the enabled state of the stop button
        /// </summary>
        public bool StopEnabled { get { return mAppModel.State == PDLAppState.Downloading; } }

        //And the text for the start button
        /// <summary>
        /// Binding for what text is displayed in the sart button
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

        //List Button state calculations (check for both length of list and that it exists)
        /// <summary>
        /// Binding for controling the enabled state of the Export Successful button
        /// </summary>
        public bool SuccessfulExportEnabled { get { return (mAppModel.SuccessfulList.Count > 0) && (mAppModel.State != PDLAppState.Downloading); } }
        /// <summary>
        /// Binding for controling the enabled state of the Export Failed button
        /// </summary>
        public bool FailedExportEnabled { get { return (mAppModel.FailedList.Count > 0) && (mAppModel.State != PDLAppState.Downloading); } }
        /// <summary>
        /// Binding for controling the enabled state of the Export All button
        /// </summary>
        public bool ExportAllEnabled { get { return (mAppModel.SuccessfulList.Count > 0 || mAppModel.FailedList.Count > 0) && (mAppModel.State != PDLAppState.Downloading); } }

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

        public ICommand ExportSuccessfulCommand { get; set; }
        public ICommand ExportFailedCommand     { get; set; }
        public ICommand ExportAllCommand        { get; set; }

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

            ExportSuccessfulCommand = new RelayCommand(() => { Export_Click(mAppModel.SuccessfulList, "Successful.txt"); });
            ExportFailedCommand     = new RelayCommand(() => { Export_Click(mAppModel.FailedList, "Failed.txt"); });
            ExportAllCommand        = new RelayCommand(() => { Export_Click(mAppModel.PatentList, "All.txt"); });
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
            //load_diag.Multiselect = true;

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
        /// Relay point for an arbitrary list export
        /// </summary>
        /// <param name="patents">List of patents to be exported</param>
        /// <param name="default_fname">The default filename for the export</param>
        private void Export_Click(List<PatentData> patents, string default_fname)
        {
            SaveFileDialog save_dialog = new SaveFileDialog();
            save_dialog.DefaultExt = ".txt";
            save_dialog.Title = "Save a list of results";
            save_dialog.FileName = default_fname;
            save_dialog.Filter = "Text documents (.txt)|*.txt";
            save_dialog.OverwritePrompt = true;
            save_dialog.ValidateNames = true;

            //Exit upon cancellation
            if (save_dialog.ShowDialog() != true)
                return;

            //Prepare file contents
            string[] lines = new string[patents.Count];
            for (int i = 0; i < patents.Count; i++)
            {
                PatentData patent = patents[i];

                lines[i] = patent.CondensedTitle;
                PatentStatus status = mAppModel.GetPatentStatus(patent);
                if (status.Timeline == PatentTimeline.None)
                    lines[i] += " was not processed";
                else if (status.Timeline == PatentTimeline.Succeeded)
                    lines[i] += " was successfully downloaded";
                else if (status.Timeline == PatentTimeline.Failed)
                    lines[i] += " failed to download";
                else
                    lines[i] += " had an unknown error";
            }

            //Write contents to file
            File.WriteAllLines(save_dialog.FileName, lines);
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
            //List Buttons
            OnPropertyChanged(nameof(SuccessfulExportEnabled));
            OnPropertyChanged(nameof(FailedExportEnabled));
            OnPropertyChanged(nameof(ExportAllEnabled));
            //Control Buttons
            OnPropertyChanged(nameof(LoadEnabled));
            OnPropertyChanged(nameof(StartEnabled));
            OnPropertyChanged(nameof(ResetEnabled));
            OnPropertyChanged(nameof(StopEnabled));
            OnPropertyChanged(nameof(StartButtonText));
            //Progress bar
            OnPropertyChanged(nameof(ProgressBarPercentage));
            OnPropertyChanged(nameof(StatusText));
        }

        #endregion
    }
}
