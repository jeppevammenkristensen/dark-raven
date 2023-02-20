using System.Runtime.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;

namespace Client.Infrastructure;

public static class TypeExtension {

    public static readonly HashSet<Type> SingleTypes = new HashSet<Type>()
    {
        typeof(string), typeof(int), typeof(bool), typeof(uint), typeof(ulong), typeof(ushort), typeof(long),
        typeof(short), typeof(double), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(Guid),
        typeof(DateOnly), typeof(TimeOnly), typeof(Single), typeof(char), typeof(byte)
    };

    public static bool IsSingleType(this Type type)
    {
        return SingleTypes.Contains(type);
    }

    public static bool IsAnonymousType(this Type type) {
        if (type == null) throw new ArgumentNullException(nameof(type));
        var hasCompilerGeneratedAttribute = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any();
        var nameContainsAnonymousType = type.FullName?.Contains("AnonymousType") == true;
        var isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;

        return isAnonymousType;
    }

    public static List<object> ToList(this IEnumerator source)
    {
        
        List<object> result = new();

        while (source.MoveNext())
        {
            result.Add(source.Current);
        }

        return result;
    }
}