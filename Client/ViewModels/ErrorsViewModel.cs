using System.Collections.ObjectModel;
using Client.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace Client.ViewModels;

public partial class ErrorsViewModel : ObservableRecipient, IRecipient<DiagnosticsCreated>
{
    [ObservableProperty] private ObservableCollection<DiagnosticModel> _items;

    public void Receive(DiagnosticsCreated message)
    {
        Items = new ObservableCollection<DiagnosticModel>(message.Value);
    }
}