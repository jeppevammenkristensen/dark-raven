using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Client.Messages;

public class CSharpChanged : ValueChangedMessage<CsharpChangedMessage>
{
    public CSharpChanged(CsharpChangedMessage value) : base(value)
    {
    }
}

public record CsharpChangedMessage(string Code, string Json)
{

}
