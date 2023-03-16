using FluentAssertions;

namespace JsonParsing.Tests;

public class ParserTests
{
    [Fact]
    public void Generate_Single_ReturnsExpected()
    {
        var parser = new Parser();
        var result = parser.Generate("""
    {
        id: 12,
        name: "Jeppe",
        date: "2022-12-12"
    }
    """);
        int i = 0;
    }

    [Fact]
    public void Generate_SingleArray_ReturnsExpected()
    {
        var parser = new Parser();
        var result = parser.Generate("""
    [{
        id: 12,
        name: "Jeppe",
        date: "2022-12-12"
    }, 
    {
        id: 13,
        name: "Jeppe",
        date: "2022-12-12",
        bit: true
    }, {
        id: 13,        
        date: "2022-12-12",
        bit: true
    }]
    """);
        int i = 0;
        result.Should().NotBeNull();
        result!.Type.Should().Be(JsonObjectType.Array);

        var resultDefinition = result.Definitions.Should().HaveCount(1).And.Subject.First();
        resultDefinition.Key.Should().Be(parser.RootName);
        var properties = resultDefinition.Value.Properties;
        properties.Should().HaveCount(4);
        properties.Should().ContainKeys("id", "name", "date", "bit");
        properties["id"].Should().BeEquivalentTo(new {Name = "Id", Type = JsonObjectType.Integer, CanBeNull = false});
        properties["name"].Should().BeEquivalentTo(new {Name = "Name", Type = JsonObjectType.String, CanBeNull = true});
        properties["date"].Should().BeEquivalentTo(new {Name = "Date", Type = JsonObjectType.String, CanBeNull = false});
        properties["bit"].Should().BeEquivalentTo(new {Name = "Bit", Type = JsonObjectType.Boolean, CanBeNull = true});


    }

    [Fact]
    public void Generate_ArrayWithNested_ReturnsExpected()
    {
        var parser = new Parser();
        var result = parser.Generate("""
    [{
        id: 12,
        name: "Jeppe",
        date: "2022-12-12",
        name: {
            "first": "Jeppe",
            "last" : "Kristensen"
        }
    }, 
    {
        id: 13,
        name: "Jeppe",
        date: "2022-12-12",
        bit: true,
        name: {
            "first": "Jeppe",
            "last" : "Kristensen"
        }
    }, {
        id: 13,        
        date: "2022-12-12",
        bit: true,
         name: {
            "first": "Jeppe",
            "middle": "Roi",
            "last" : "Kristensen"
        }
    }]
    """);
        int i = 0;
        result.Should().NotBeNull();
        result!.Type.Should().Be(JsonObjectType.Array);

        var resultDefinition = result.Definitions.Should().HaveCount(1).And.Subject.First();
        resultDefinition.Key.Should().Be(parser.RootName);
        var properties = resultDefinition.Value.Properties;
        properties.Should().HaveCount(4);
        properties.Should().ContainKeys("id", "name", "date", "bit");
        properties["id"].Should().BeEquivalentTo(new {Name = "Id", Type = JsonObjectType.Integer, CanBeNull = false});
        properties["name"].Should().BeEquivalentTo(new {Name = "Name", Type = JsonObjectType.String, CanBeNull = true});
        properties["date"].Should().BeEquivalentTo(new {Name = "Date", Type = JsonObjectType.String, CanBeNull = false});
        properties["bit"].Should().BeEquivalentTo(new {Name = "Bit", Type = JsonObjectType.Boolean, CanBeNull = true});


    }

    [Fact]
    public void Generate_ObjectWithArrayPropertyWithNested_ReturnsExpected()
    {
        var parser = new Parser();
        var result = parser.Generate("""
    { "properties" :[{
        id: 12,
        name: "Jeppe",
        date: "2022-12-12",
        name: {
            "first": "Jeppe",
            "last" : "Kristensen"
        }
    }, 
    {
        id: 13,
        name: "Jeppe",
        date: "2022-12-12",
        bit: true,
        name: {
            "first": "Jeppe",
            "last" : "Kristensen"
        }
    }, {
        id: 13,        
        date: "2022-12-12",
        bit: true,
         name: {
            "first": "Jeppe",
            "middle": "Roi",
            "last" : "Kristensen"
        }
    }]}
    """);
        int i = 0;
        result.Should().NotBeNull();
        result!.Type.Should().Be(JsonObjectType.Array);

        var resultDefinition = result.Definitions.Should().HaveCount(1).And.Subject.First();
        resultDefinition.Key.Should().Be(parser.RootName);
        var properties = resultDefinition.Value.Properties;
        properties.Should().HaveCount(4);
        properties.Should().ContainKeys("id", "name", "date", "bit");
        properties["id"].Should().BeEquivalentTo(new {Name = "Id", Type = JsonObjectType.Integer, CanBeNull = false});
        properties["name"].Should().BeEquivalentTo(new {Name = "Name", Type = JsonObjectType.String, CanBeNull = true});
        properties["date"].Should().BeEquivalentTo(new {Name = "Date", Type = JsonObjectType.String, CanBeNull = false});
        properties["bit"].Should().BeEquivalentTo(new {Name = "Bit", Type = JsonObjectType.Boolean, CanBeNull = true});


    }
}