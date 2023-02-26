using System.Windows;
using Client.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ICSharpCode.AvalonEdit.Document;

namespace Client.ViewModels;

public partial class GeneratedClassViewModel : ObservableObject
{
    [ObservableProperty]
    private TextDocument _document;

    public GeneratedClassViewModel()
    {
        _document = new TextDocument();
        WeakReferenceMessenger.Default.Register<CSharpChanged>(this, Handler);
        WeakReferenceMessenger.Default.Register<RequestJsonCsharp>(this, (recipient, message) =>
        {
            message.Reply(Document.Text);
        });
    }

    private void Handler(object recipient, CSharpChanged message)
    {
        Document.Text = message.Value.Code;
    }
}