using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Client.Infrastructure;
using Client.Messages;
using Client.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;

namespace Client;

public partial class MainWindowViewModel : ObservableRecipient
{
    private Persistence _persistence = new();
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor("SaveCommand")]
    private string? _fileSource = null;

    [ObservableProperty] private SourceViewModel _source;

    [ObservableProperty] private GeneratedClassViewModel _generatedClass;

    [ObservableProperty] private CodeRunnerViewModel _codeRunner;

    [ObservableProperty] private ErrorsViewModel _errors;

    [RelayCommand]
    public async Task Open()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dark Raven");
        Directory.CreateDirectory(path);

        OpenFileDialog dialog = new OpenFileDialog();
        dialog.Filter = "(*.dr)|*.dr";
        dialog.InitialDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dark Raven");
        var result = dialog.ShowDialog();
        if (result == DialogResult.OK)
        {
            var data = await _persistence.LoadAsync(dialog.FileName);
            Source.Document.Text = data.json;
            GeneratedClass.Document.Text = data.parsedJson;
            Messenger.Send(new CSharpChanged(new CsharpChangedMessage(data.parsedJson, data.json)));
            CodeRunner.Document.Text = data.code;
            FileSource = dialog.FileName;
            OnPropertyChanged(nameof(CanSave));
        }
    }

    public bool CanSave()
    {
        return FileSource != null;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    public async Task Save()
    {
        await _persistence.SaveAsync(FileSource!,
            new SaveData(this.Source.Document.Text, this.GeneratedClass.Document.Text,
                this.CodeRunner.Document.Text));
    }

    [RelayCommand]
    public async Task SaveAs()
    {
        SaveFileDialog dialog = new SaveFileDialog();
        dialog.Filter = "(*.dr)|*.dr";
        var result = dialog.ShowDialog();
        if (result == DialogResult.OK)
        {
            await _persistence.SaveAsync(dialog.FileName,
                new SaveData(this.Source.Document.Text, this.GeneratedClass.Document.Text,
                    this.CodeRunner.Document.Text));
            FileSource = dialog.FileName;
        }
    }

    [RelayCommand]
    public void Close()
    {
    }

    public MainWindowViewModel()
    {
        Source = new SourceViewModel();
        GeneratedClass = new GeneratedClassViewModel();
        CodeRunner = new CodeRunnerViewModel();
        Errors = new ErrorsViewModel();
        Errors.IsActive = true;
        this.IsActive = true;
    }
}

public class JsonLanguageConverter
{
    public (bool success, DiagnosticModel details) InputIsValid(string source)
    {
        try
        {
            JToken.Parse(source);
            return (true, default!);
        }
        catch (JsonReaderException e)
        {
            return (false, new DiagnosticModel(e.Message, e.LinePosition, e.LineNumber, DiagnosticSource.Json) );
        }
    }

    public string GenerateCsharp(string input)
    {
        var csharpSettings = new CSharpGeneratorSettings
        {
            ClassStyle = CSharpClassStyle.Poco,
            RequiredPropertiesMustBeDefined = false,
            GenerateNullableReferenceTypes = true,
        };

        var schema = JsonSchema.FromSampleJson(input);

        var cSharpGenerator = new CSharpGenerator(schema, csharpSettings);
        var result = cSharpGenerator.GenerateFile();

        return result;
    }


    public string PrimaryClass { get; set; } = "ReturnObject";

    public object Deserialize(Type parameterType, string requestSource)
    {
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(parameterType);

        var result = JToken.Parse(requestSource);
        if (result is JObject jObject)
        {
            var arr = new JArray {jObject};
            requestSource = arr.ToString();
        }

        return JsonConvert.DeserializeObject(
            requestSource, enumerableType,
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
    }
}