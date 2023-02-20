using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows;
using Client.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Client.ViewModels;



public partial class OutputViewModel : ObservableObject
{
    
    [ObservableProperty]
    private DataTable _data;

    [ObservableProperty]
    private string _errorText;

    [ObservableProperty]
    private bool _displayError;

    public void SetErrorText(string text)
    {
        ErrorText = text;
        DisplayError = true;
    }

    public void SetErrorText(Exception exception)
    {
        SetErrorText(exception.ToString());
    }

    public void SetData(object? data)
    {
        if (data is null)
            Data = new DataTable();

        else if (data.GetType().IsSingleType())
        {
            Data = CreateDataTableFromAnyCollection(new List<object> {data});
        }

        else if (data is IEnumerable enumerator)
        {
            Data = CreateDataTableFromAnyCollection<object>(enumerator.OfType<object>().ToList());
        }
        else
        {
            Data = CreateDataTableFromAnyCollection(new List<object>() {data});
        }

        DisplayError = false;
    }

    public static DataTable CreateDataTableFromAnyCollection<T>(IEnumerable<T> data)
    {
        if (data.ToList() is not {Count: > 0} list)
            return new DataTable();

        Type type = list[0].GetType();
        var properties = new List<(DataColumn, IValueAccess)>();
        GetProperties(ref properties, type, null, null);

        DataTable dataTable = new DataTable();
        dataTable.Columns.AddRange(properties.Select(x => x.Item1).ToArray());
        
        foreach (T entity in list)
        {
            var row = dataTable.NewRow();

            for (int i = 0; i < properties.Count; i++)
            {
                row.SetField(properties[i].Item1,properties[i].Item2.GetValue(entity));
            }

            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    private static void GetProperties(ref List<(DataColumn, IValueAccess)> result, Type type, string? prepend, PropertyInfo[]? prePropertyInfo)
    {
        if (SingleTypes.Contains(type))
        {
            result.Add((new DataColumn(type.Name, type), new SingleAccess()));
            return;
        }

        var properties = type.GetProperties();
         
        foreach (PropertyInfo info in properties)
        {
            var candidateType = Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType;
            if (SingleTypes.Contains(candidateType))
            {
                var column = new DataColumn(prepend == null ? info.Name : $"{prepend}{info.Name}",
                    candidateType);
                result.Add((column, new PropertyAccess(prePropertyInfo == null ? new [] {info} : prePropertyInfo.Concat(new []{info}))));
            }
            else if (typeof(IEnumerable).IsAssignableFrom(candidateType))
            {
                continue;
            }
                
            else
            {
                GetProperties(ref result, candidateType, prepend == null ? $"{info.Name}\u2024" : $"{prepend}{info.Name}\u2024" , (prePropertyInfo == null ? new [] {info} : prePropertyInfo.Concat(new []{info}).ToArray()));
            }
        }
    }

    public class SingleAccess : IValueAccess
    {
        public object? GetValue(object entity)
        {
            return entity;
        }
    }

    public interface IValueAccess
    {
        object? GetValue(object entity);
    }

    public class PropertyAccess : IValueAccess
    {
        private readonly PropertyInfo[] _properties;

        public PropertyAccess(IEnumerable<PropertyInfo> properties)
        {
            _properties = properties.ToArray();
        }

        public object? GetValue(object entity)
        {
            object? result = null;
            foreach (var propertyInfo in _properties)
            {
                if (result == null)
                {
                    result = propertyInfo.GetValue(entity, null);
                }
                else
                {
                    result = propertyInfo.GetValue(result, null);
                }

                if (result == null)
                    return null;
            }

            return result;
        }
    }


    public static readonly HashSet<Type> SingleTypes = new HashSet<Type>()
    {
        typeof(string), typeof(int), typeof(bool), typeof(uint), typeof(ulong), typeof(ushort), typeof(long),
        typeof(short), typeof(double), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(Guid),
        typeof(DateOnly), typeof(TimeOnly), typeof(Single), typeof(char), typeof(byte)
    };

    
}