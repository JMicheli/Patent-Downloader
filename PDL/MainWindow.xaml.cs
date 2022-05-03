using System.Windows;

using PDL.ViewModels;

namespace PDL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PDLViewModel mViewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Create the viewmodel and set it to be our datacontext
            mViewModel = new PDLViewModel(this);
            this.DataContext = mViewModel;
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //Grab only the first element in case of multiple files
                string file = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                //Send through to the ViewModel
                mViewModel.DragAndDrop(file);
            }
        }
    }
}
