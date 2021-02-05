using System.Windows;

using PDL4.ViewModels;

namespace PDL4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Create the viewmodel and set it to be our datacontext
            this.DataContext = new PDLViewModel(this);
        }
    }
}
