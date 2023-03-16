using Microsoft.VisualBasic.CompilerServices;

namespace JsonParsing;

public static class Utils
{
    public static string ConvertToUpperCamelCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        input = ConvertDashesToCamelCase(input);
        return input;

    }

    public static string ConvertDashesToCamelCase(ReadOnlySpan<char> input)
    {
        if (input.Length == 0) return string.Empty;


        Span<char> output = stackalloc char[input.Length]; // Create a stack-allocated output span
        input.CopyTo(output); // Copy the input to the output span

        int index;

        output[0] = char.ToUpper(output[0]);

        while ((index = output.IndexOf('-')) != -1)
        {
            output[index] = '_';
        }

        while ((index = output.IndexOf('/')) != -1)
        {
            output[index] = '_';
        }

        return new string(output);
    }
}