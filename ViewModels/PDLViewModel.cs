using System;
using System.Collections.Generic;
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

        //Button state calculations (check for both length of list and that it exists)
        public bool SuccessfulExportEnabled { get { return (mAppModel.SuccessfulList.Count > 0); } }
        public bool FailedExportEnabled { get { return (mAppModel.FailedList.Count > 0); } }
        public bool ExportAllEnabled { get { return (mAppModel.SuccessfulList.Count > 0 || mAppModel.FailedList.Count > 0); } }

        #endregion

        #region Commands

        public ICommand LoadFileCommand { get; set; }

        public ICommand StartCommand { get; set; }
        public ICommand ResetCommand { get; set; }
        public ICommand StopCommand  { get; set; }

        #endregion

        #region Constructor

        public PDLViewModel(Window window)
        {
            //Initialize private members
            mWindow = window;
            mAppModel = new PDLAppModel();

            //Set AppModel callback
            mAppModel.StateChangedCallback = () =>
            {
                OnPropertyChanged(nameof(SuccessfulDownloads));
                OnPropertyChanged(nameof(FailedDownloads));
            };

            //Create commands
            LoadFileCommand = new RelayCommand(() => LoadFile_Click());

            StartCommand = new RelayCommand(() => Start_Click());
            ResetCommand = new RelayCommand(() => Reset_Click());
            StopCommand  = new RelayCommand(() => Stop_Click());
        }

        #endregion

        #region Private Fns

        private void LoadFile_Click()
        {
            //Create dialog and set variables
            OpenFileDialog load_diag = new OpenFileDialog();
            load_diag.DefaultExt = ".txt";
            load_diag.Filter = "Text documents (.txt)|*.txt";
            //load_diag.Multiselect = true;

            if (load_diag.ShowDialog() == true) //Fires if a file was selected
            {
                mAppModel.LoadFile(load_diag.FileName); //Pass path into App Model
                NotifyAll(); //Notify of changed properties
                
            }
        }

        private void Start_Click()
        {
            PatentData patent = mAppModel.PatentList[0];
            mAppModel.Download();
        }

        private void Reset_Click()
        {
            mAppModel.Reset();
            NotifyAll();
        }

        private void Stop_Click()
        {
            //Gonna want to do something with this
        }

        private void NotifyAll()
        {
            //Labels
            OnPropertyChanged(nameof(SelectedFile));
            OnPropertyChanged(nameof(DistinctPatentsString));
            //Lists
            OnPropertyChanged(nameof(SuccessfulDownloads));
            OnPropertyChanged(nameof(FailedDownloads));
            //Buttons
            OnPropertyChanged(nameof(SuccessfulExportEnabled));
            OnPropertyChanged(nameof(FailedExportEnabled));
            OnPropertyChanged(nameof(ExportAllEnabled));
        }

        #endregion
    }
}
