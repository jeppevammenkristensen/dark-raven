using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Client.Messages;

public class CSharpChanged : ValueChangedMessage<string>
{
    public CSharpChanged(string value) : base(value)
    {
    }
}