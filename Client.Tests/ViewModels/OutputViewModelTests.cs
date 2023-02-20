using Client.ViewModels;
using FluentAssertions;

namespace Client.Tests.ViewModels;


public class OutputViewModelTests
{
    [Fact]
    public void SetData_EnumerableItemNoNesting_MapCorrectlyToDataTable()
    {
        var subject = new OutputViewModel();
        var dateTime = DateTime.Now;
        subject.SetData(new List<SingleTestModel>() {new SingleTestModel("Jeppe", 43, dateTime)});

        subject.Data.Should().HaveColumns("Name", "Age", "Date").And.HaveRowCount(1);
        var dataRow = subject.Data.Rows[0];
        dataRow["Name"].Should().Be("Jeppe");
        dataRow["Age"].Should().Be(43);
        dataRow["Date"].Should().Be(dateTime);
    }

    [Fact]
    public void SetData_EnumerableItemNesting_MapCorrectlyToDataTable()
    {
        var subject = new OutputViewModel();
        var dateTime = DateTime.Now;
        subject.SetData(new List<NestedClassModel>() {new NestedClassModel("Henry", new SingleTestModel("Jeppe", 43, dateTime))});

        subject.Data.Should().HaveColumns("Name", "Nested.Name", "Nested.Age", "Nested.Date").And.HaveRowCount(1);
        var dataRow = subject.Data.Rows[0];
        dataRow["Name"].Should().Be("Henry");
        dataRow["Nested.Name"].Should().Be("Jeppe");
        dataRow["Nested.Age"].Should().Be(43);
        
    }

    
    [Fact]
    public void SetData_IEnumerator_MapCorrectlyToDataTable()
    {
        var subject = new OutputViewModel();
        var dateTime = DateTime.Now;
        subject.SetData(new List<NestedClassModel>() {new NestedClassModel("Henry", new SingleTestModel("Jeppe", 43, dateTime))}.Select(x => x.Nested.Age));

        subject.Data.Should().HaveColumn("Int32");
        var dataRow = subject.Data.Rows[0];
        dataRow[0].Should().Be(43);
        //dataRow["Nested.Name"].Should().Be("Jeppe");
        //dataRow["Nested.Age"].Should().Be(43);
        
    }


    public class SingleTestModel
    {
        public SingleTestModel(string name, int age, DateTime date)
        {
            Name = name;
            Age = age;
            Date = date;
        }

        public string Name { get;  }
        public int Age { get; }
        public DateTime Date { get; }
    }

    public class NestedClassModel
    {
        public NestedClassModel(string name, SingleTestModel nested)
        {
            Name = name;
            Nested = nested;
        }

        public string Name { get; }
        public SingleTestModel Nested{  get; }
    }
}