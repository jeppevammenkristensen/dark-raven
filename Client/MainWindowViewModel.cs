using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Client.Messages;
using Client.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.CodeAnalysis.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using MessageBox = System.Windows.MessageBox;

namespace Client;

public partial class MainWindowViewModel : ObservableRecipient
{
    private string? _fileSource = null;

    [ObservableProperty]
    private SourceViewModel _source;

    [ObservableProperty]
    private GeneratedClassViewModel _generatedClass;

    [ObservableProperty]
    private CodeRunnerViewModel _codeRunner;

    [ObservableProperty] private ErrorsViewModel _errors;

    [RelayCommand]
    public async Task Open()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dark Raven");
        Directory.CreateDirectory(path);

        OpenFileDialog dialog = new OpenFileDialog();
        dialog.InitialDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dark Raven");
        var result = dialog.ShowDialog();
        
    }

    public bool CanSave()
    {
        return _fileSource != null;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    public void Save()
    {
        MessageBox.Show("Awaiting implementation");
    }

    [RelayCommand]
    public void SaveAs()
    {

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
    

        public (bool success, string details) InputIsValid(string source)
        {
            try
            {
                JToken.Parse(source);
                return (true, default!);
            }
            catch (JsonReaderException e)
            {
                return (false, e.Message);
            }
        }

        public string GenerateCsharp(string input)
        {
            var csharpSettings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
            };

            var schema = JsonSchema.FromSampleJson(input);

            var cSharpGenerator = new CSharpGenerator(schema,csharpSettings);
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
                var arr = new JArray { jObject };
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