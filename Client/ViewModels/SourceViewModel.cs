using System.Threading.Tasks;
using Client.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ICSharpCode.AvalonEdit.Document;

namespace Client.ViewModels;

public partial class SourceViewModel : ObservableObject
{


    [ObservableProperty]
    private TextDocument _document;

    private JsonLanguageConverter _jsonLanguageConverter;

    public SourceViewModel()
    {
        _document = new TextDocument();
        _jsonLanguageConverter = new JsonLanguageConverter();
        WeakReferenceMessenger.Default.Register<RequestJson>(this, (recipient, message) =>
        {
            message.Reply(Document.Text);
        });
    }

    [RelayCommand]
    public async Task UpdateSource()
    {
        if (_jsonLanguageConverter.InputIsValid(_document.Text) is {success: false} output)
        {
            int i = 0;
        }
        else 
        {
            var generateCsharp = _jsonLanguageConverter.GenerateCsharp(_document.Text);
            WeakReferenceMessenger.Default.Send(new CSharpChanged(new CsharpChangedMessage(generateCsharp, _document.Text)));
        }
    }

}