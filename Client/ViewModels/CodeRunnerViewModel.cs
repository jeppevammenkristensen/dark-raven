using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Windows.Documents;
using Client.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TextDocument = ICSharpCode.AvalonEdit.Document.TextDocument;

namespace Client.ViewModels;

public partial class CodeRunnerViewModel : ObservableRecipient, IRecipient<CSharpChanged>
{
    [ObservableProperty] private TextDocument _document;

    [ObservableProperty] private object? _output;

    [ObservableProperty] private OutputViewModel _outputView;
    private Project _project;
    private AdhocWorkspace _workspace;

    public CodeRunnerViewModel()
    {
        Document = new TextDocument();
        Document.Text = "return source;";

        OutputView = new OutputViewModel();
        Init();
    }

    private void Init()
    {
        var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
        _workspace = new AdhocWorkspace(host);

        var references = new List<MetadataReference>();
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        references.Add(MetadataReference.CreateFromFile(typeof(CodeRunnerViewModel).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(JsonConvert).Assembly.Location));

        var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "TrblxDux", "TrblxDux", LanguageNames.CSharp, compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).
            WithMetadataReferences( 
                references
            );

        _project = _workspace.AddProject(projectInfo);
        this.IsActive = true;

    }

    [RelayCommand]
    public async Task Compile()
    {
        var json = Messenger.Send<RequestJson>().Response;
        var code = Messenger.Send<RequestJsonCsharp>().Response;
        ApplyContract(code);        
        UpdateCode();

        if (await CompileWorkspace() is {succeeded: true} res)
        {
            var type = res.assembly.GetType("TrblxDux.Runner");
            var instance = Activator.CreateInstance(type);
            try
            {
                var result = type.GetMethod("Run").Invoke(instance, new []{ json});
                OutputView.SetData(result);
            }
            catch (Exception e)
            {
                OutputView.SetErrorText(e);
            }
        }
        else
        {
            OutputView.SetErrorText("Failed to compile");
        }
    }

    [RelayCommand]
    public void Reset()
    {
        Document.Text = "return source";
    }

    public string GetWord(TextViewPosition? pos)
    {
        var line = pos.Value.Line;
        var column = pos.Value.Column;
        var offset = Document.GetOffset(line, column);

        if (offset >= Document.TextLength)
            offset--;

        int offsetStart = TextUtilities.GetNextCaretPosition(Document, offset, LogicalDirection.Backward,
            CaretPositioningMode.WordBorder);
        int offsetEnd = TextUtilities.GetNextCaretPosition(Document, offset, LogicalDirection.Forward,
            CaretPositioningMode.WordBorder);

        if (offsetEnd == -1 || offsetStart == -1)
            return string.Empty;

        var currentChar = Document.GetText(offset, 1);

        if (string.IsNullOrWhiteSpace(currentChar))
            return string.Empty;

        return Document.GetText(offsetStart, offsetEnd - offsetStart);
    }

    public void Receive(CSharpChanged message)
    {
        ApplyContract(message.Value.Code);
        var code = GenerateCode(message.Value.Json);
        Document.Text = code;   
    }

    private void ApplyContract(string message)
    {
        if (_workspace.CurrentSolution.GetProject(_project.Id).Documents.FirstOrDefault(x => x.Name == "JsonContract.cs") is { } document)
        {
            var newDocument = document.WithText(SourceText.From(message));

            _workspace.TryApplyChanges(newDocument.Project.Solution);
        }
        else
        {
            _workspace.AddDocument(_project.Id, "JsonContract.cs", SourceText.From(message));
        }
    }


    private string GenerateCode(string json)
    {
        var jToken = JToken.Parse(json);
        var isArray = jToken is JArray;
        var type = isArray ? "Anonymous[]" : "Anonymous";

        string text = $$""""
        using System.Linq;
        using MyNamespace;
        
        namespace TrblxDux 
        {
            public class Runner 
            {
                public object Process({{type}} source)
                {
                    return source;
                }

                public object Run(string json)
                {
                    var item =  Newtonsoft.Json.JsonConvert.DeserializeObject<{{type}}>(json, new Newtonsoft.Json.JsonSerializerSettings());
                    return Process(item);
                }        
            }
         }
        """";

        return text;
    }

    private void UpdateCode()
    {
        if (_workspace.CurrentSolution.GetProject(_project.Id).Documents.FirstOrDefault(x => x.Name == "Source.cs") is { } document)
        {
            var newDocument = document.WithText(SourceText.From(Document.Text));

            _workspace.TryApplyChanges(newDocument.Project.Solution);
        }
        else
        {
            _workspace.AddDocument(_project.Id, "Source.cs", SourceText.From(Document.Text));
        }

    }




    public async Task<(Assembly assembly, bool succeeded)> CompileWorkspace()
    {
        
       var compilation = await _workspace.CurrentSolution.GetProject(_project.Id).GetCompilationAsync();


       // TODO: Uncomment this line if you want to fail tests when the injected program isn't valid _before_ running generators
        var compileDiagnostics = compilation.GetDiagnostics();

        if (compileDiagnostics.Any())
        {
            Messenger.Send(new DiagnosticsCreated(ImmutableArray<DiagnosticModel>.Empty.AddRange(
                compileDiagnostics.Select(x => new DiagnosticModel(x.GetMessage(),
                    x.Location.GetMappedLineSpan().StartLinePosition.Line,
                    x.Location.GetMappedLineSpan().StartLinePosition.Character, DiagnosticSource.Code)))));
        }
        else
        {
            Messenger.Send(new DiagnosticsCreated(ImmutableArray<DiagnosticModel>.Empty));
        }

        if (compileDiagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
        {
            return (default!, false);
        }

        var memoryStream = new MemoryStream();
        compilation.Emit(memoryStream);
        return (Assembly.Load(memoryStream.ToArray()), true);
    }

    public async  Task<IEnumerable<string>> GetSuggestions(int location)
    {
        var json = Messenger.Send<RequestJson>().Response;
        UpdateCode();

        if (_workspace.CurrentSolution.GetProject(_project.Id).Documents.FirstOrDefault(x => x.Name == "Source.cs") is
            { } document)
        {
            var completionService = CompletionService.GetService(document);
            var result = await completionService.GetCompletionsAsync(document, location);
            return result.ItemsList.Select(x => x.DisplayText);
        }

        return Enumerable.Empty<string>();

    }
}