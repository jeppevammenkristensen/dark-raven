using System.Runtime.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Client.Infrastructure;

public static class TypeExtension {

    public static readonly HashSet<Type> SingleTypes = new HashSet<Type>()
    {
        typeof(string), typeof(int), typeof(bool), typeof(uint), typeof(ulong), typeof(ushort), typeof(long),
        typeof(short), typeof(double), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(Guid),
        typeof(DateOnly), typeof(TimeOnly), typeof(Single), typeof(char), typeof(byte)
    };

    public static async Task CreateEntryFromText(this ZipArchive zip, string fileName, string text)
    {
        var zipArchiveEntry = zip.CreateEntry(fileName);
        await using var stream = zipArchiveEntry.Open();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(text);
    }

    public static async Task<string> LoadFromText(this ZipArchive zip, string fileName)
    {
        ZipArchiveEntry? entry = zip.GetEntry(fileName) ?? throw new InvalidOperationException($"Failed to open file {fileName} from file");

        var stream = entry.Open();
        using StreamReader reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
        // Do something with the content
    }

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