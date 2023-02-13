using System.Collections.Immutable;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Client.Messages;

public enum DiagnosticSource
{
    Json,
    Code
}

public record DiagnosticModel(string message, int line, int row, DiagnosticSource source)
{
    
}

public class DiagnosticsCreated : ValueChangedMessage<ImmutableArray<DiagnosticModel>>
{
    public DiagnosticsCreated(ImmutableArray<DiagnosticModel> value) : base(value)
    {
    }
}