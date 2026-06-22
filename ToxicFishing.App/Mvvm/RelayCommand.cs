using System.Windows.Input;

namespace ToxicFishing.App.Mvvm;

/// <summary>
/// A simple parameterless <see cref="ICommand"/> for MVVM bindings, delegating execution and the
/// can-execute check to the supplied callbacks.
/// </summary>
/// <param name="execute">The action invoked when the command runs.</param>
/// <param name="canExecute">Optional predicate gating execution; when omitted the command is always
/// executable.</param>
/// <exception cref="ArgumentNullException"><paramref name="execute"/> is <see langword="null"/>.</exception>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action execute = execute ?? throw new ArgumentNullException(nameof(execute));

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => execute();

    /// <summary>
    /// Raises <see cref="CanExecuteChanged"/> so bound controls re-query <see cref="CanExecute"/>.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
