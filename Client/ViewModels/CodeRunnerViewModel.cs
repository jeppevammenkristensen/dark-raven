using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Client.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TextDocument = ICSharpCode.AvalonEdit.Document.TextDocument;

namespace Client.ViewModels;

public partial class CodeRunnerViewModel : ObservableRecipient
{
    [ObservableProperty] private TextDocument _document;

    [ObservableProperty] private object? _output;

    public CodeRunnerViewModel()
    {
        Document = new TextDocument();
        Document.Text = "return source;";
    }

    [RelayCommand]
    public async Task Compile()
    {
        var json = Messenger.Send<RequestJson>().Response;
        var code = Messenger.Send<RequestJsonCsharp>().Response;
        var context = new RunContext(json, code, Document.Text, Messenger);
        if (await context.Compile() is {succeeded: true} res)
        {
            var type = res.assembly.GetType("TrblxDux.Runner");
            var instance = Activator.CreateInstance(type);
            try
            {
                var result = type.GetMethod("Run").Invoke(instance, Array.Empty<object>());


                if (!typeof(IEnumerable).IsAssignableFrom(result.GetType()))
                {
                    if (result is IEnumerator enumerator)
                    {
                        Output = new Result(new FakeEnumerable(enumerator));
                    }
                    else
                    {
                        Output = new Result(new object[] {result});
                    }
                }
                else
                {
                    Output = new Result(result);
                }
            }
            catch (Exception e)
            {
                Output = e.ToString();
            }
        }
    }
}

public class FakeEnumerable : IEnumerable
{
    private IEnumerator m_enumerator;

    public FakeEnumerable(IEnumerator e)
    {
        m_enumerator = e;
    }

    public IEnumerator GetEnumerator()
    {
        return m_enumerator;
    }

    // Rest omitted 
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class Result
{
    public object Obj { get; }


    public Result(Object obj)
    {
        Obj = obj;
    }
}

public class RunContext
{
    private readonly IMessenger _messenger;
    private readonly string _runner;


    public RunContext(string json, string code, string custom, IMessenger messenger)
    {
        var jToken = JToken.Parse(json);
        var isArray = jToken is JArray;
        var type = isArray ? "Anonymous[]" : "Anonymous";

        _messenger = messenger;
        _runner = $$""""
using System.Linq;
using MyNamespace;


namespace TrblxDux 
{
    public class Runner 
    {
        public object Process({{type}} source)
        {
            {{custom}}
        }

        public object Run()
        {
            var item =  Newtonsoft.Json.JsonConvert.DeserializeObject<{{type}}>(_json, new Newtonsoft.Json.JsonSerializerSettings());
            return Process(item);
        }

        private string _json = """
{{json}}
""";
    }
}

{{code}}
            
"""";
    }

    public async Task<(Assembly assembly, bool succeeded)> Compile()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(_runner);

        var references = new List<MetadataReference>();
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        references.Add(MetadataReference.CreateFromFile(typeof(JsonConvert).Assembly.Location));

        var compilation = CSharpCompilation.Create("foo", new SyntaxTree[] {syntaxTree}, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // TODO: Uncomment this line if you want to fail tests when the injected program isn't valid _before_ running generators
        var compileDiagnostics = compilation.GetDiagnostics();

        if (compileDiagnostics.Any())
        {
            _messenger.Send(new DiagnosticsCreated(ImmutableArray<DiagnosticModel>.Empty.AddRange(
                compileDiagnostics.Select(x => new DiagnosticModel(x.GetMessage(),
                    x.Location.GetMappedLineSpan().StartLinePosition.Line,
                    x.Location.GetMappedLineSpan().StartLinePosition.Character, DiagnosticSource.Code)))));
        }

        if (compileDiagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
        {
            return (default!, false);
        }

        var memoryStream = new MemoryStream();
        compilation.Emit(memoryStream);
        return (Assembly.Load(memoryStream.ToArray()), true);
    }
}