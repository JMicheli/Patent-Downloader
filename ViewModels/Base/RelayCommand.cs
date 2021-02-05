using System;
using System.Windows.Input;

namespace PDL4.ViewModels
{
    /// <summary>
    /// For passing commands in from XAML
    /// </summary>
    public class RelayCommand : ICommand
    {
        // The action to be performed
        private Action mAction;

        // Event when execution state is changed, won't happen
        public event EventHandler CanExecuteChanged = (sender, e) => { };

        // Default constructor
        public RelayCommand(Action action)
        {
            mAction = action;
        }

        public bool CanExecute(object parameter) { return true; } //Always executable
        public void Execute(object parameter) { mAction(); } //Perform the action
    }
}