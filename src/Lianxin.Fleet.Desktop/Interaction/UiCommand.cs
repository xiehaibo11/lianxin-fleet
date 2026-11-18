using System.Windows.Input;

namespace Lianxin.Fleet.Desktop.Interaction;

public sealed class UiCommand(Action execute) : ICommand
{
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => execute();
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}

