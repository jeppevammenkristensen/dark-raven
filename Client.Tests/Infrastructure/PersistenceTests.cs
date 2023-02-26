using Client.Infrastructure;

namespace Client.Tests.Infrastructure;

public class PersistenceTests
{
    [Fact]
    public async Task SaveAsync()
    {
        var path = "super.dr";
        Persistence persistence = new();
        await persistence.SaveAsync(path, new SaveData("red", "tre", "rrme"));
    }

    
}