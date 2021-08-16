using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SANYU2021.Commands
{
    public class RelayCommand : ICommand
    {
        readonly Action<object> _execute;
        readonly Predicate<object> _canExecute;
        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<object> execute) : this(execute,null)
        {

        }
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute ?? throw new
                ArgumentNullException("execute");
            _canExecute = canExecute;
        }
       
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }
       
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
