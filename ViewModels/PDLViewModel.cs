using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PDL4.DataModels;
using PDL4.Models;

namespace PDL4.ViewModels
{
    class PDLViewModel : BaseViewModel
    {
        #region Private Members

        // A reference to the window
        private Window mWindow;

        //A reference to the PDL App Model
        private PDLAppModel mAppModel;

        #endregion

        #region Public Properties

        public string DistinctPatentsPrefix { get; set; } = "Number of distinct patents: ";
        public string SelectedFilePrefix { get; set; } = "Currently open: ";
        public string SelectedFileNullSuffix { get; set; } = "Once a file is selected it will appear here";

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

        public List<PatentData> SuccessfulDownloads { get { return mAppModel.SuccessfulList; } }

        public List<PatentData> FailedDownloads { get { return mAppModel.FailedList; } }

        //Download control Button state calculations
        public bool LoadEnabled { get { return !(mAppModel.State == PDLAppState.Downloading); } }
        public bool StartEnabled { get { return ((mAppModel.State == PDLAppState.Loaded) || (mAppModel.State == PDLAppState.Stopped)); } }
        public bool ResetEnabled { get { return !((mAppModel.State == PDLAppState.Initial) || (mAppModel.State == PDLAppState.Downloading)); } }
        public bool StopEnabled { get { return mAppModel.State == PDLAppState.Downloading; } }
        //And the text for the start button
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
        public bool SuccessfulExportEnabled { get { return (mAppModel.SuccessfulList.Count > 0) && (mAppModel.State != PDLAppState.Downloading); } }
        public bool FailedExportEnabled { get { return (mAppModel.FailedList.Count > 0) && (mAppModel.State != PDLAppState.Downloading); } }
        public bool ExportAllEnabled { get { return (mAppModel.SuccessfulList.Count > 0 || mAppModel.FailedList.Count > 0) && (mAppModel.State != PDLAppState.Downloading); } }

        //Progress bar stuff
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

        #endregion

        #region Commands

        public ICommand LoadFileCommand { get; set; }

        public ICommand StartCommand { get; set; }
        public ICommand ResetCommand { get; set; }
        public ICommand StopCommand  { get; set; }

        public ICommand ExportSuccessfulCommand { get; set; }
        public ICommand ExportFailedCommand     { get; set; }
        public ICommand ExportAllCommand        { get; set; }

        #endregion

        #region Constructor

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
            };
            mAppModel.StateChangedCallback = NotifyAll;

            //Create commands
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

        private void Start_Click()
        {
            if (mAppModel.State == PDLAppState.Stopped)
                mAppModel.Resume();
            else
                mAppModel.Download();
        }

        //Reset button to App Model reset
        private void Reset_Click() { mAppModel.Reset(); }

        //Stop button to App Model stop
        private void Stop_Click() { mAppModel.Stop(); }

        private void Export_Click(List<PatentData> patents, string default_fname)
        {
            SaveFileDialog save_dialog = new SaveFileDialog();
            save_dialog.DefaultExt = ".txt";
            save_dialog.Title = "Save a list of results";
            save_dialog.FileName = default_fname;
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
        }

        #endregion
    }
}
