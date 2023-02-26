using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Client.Infrastructure;

public class Persistence
{
    public async Task SaveAsync(string path, SaveData data)
    {
        ZipArchiveMode mode;
        if (Path.Exists(path))
        {
            File.Move(path, $"{path}.backup");
            mode = ZipArchiveMode.Create;
        }
        else
        {
            mode = ZipArchiveMode.Create;
        }

        try
        {

            //var path = Path.Combine(directory, fileNameWithoutExtension, ".dr");
            using ZipArchive zip = ZipFile.Open(path, mode);
            // Add files to the archive
            await zip.CreateEntryFromText("Data.json", data.json);
            await zip.CreateEntryFromText("Json.cs", data.parsedJson);
            await zip.CreateEntryFromText("Code.json", data.code);
        }
        catch
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (File.Exists($"{path}.backup"))
            {
                File.Copy($"{path}.backup", path);
            }
        }
        finally
        {
            if (File.Exists($"{path}.backup"))
            {
                File.Delete($"{path}.backup");
            }
        }
        // ...
    }

    public async Task<SaveData> LoadAsync(string path)
    {
        using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Read))
        {
            // To read a file from the zip file
            var data = await zip.LoadFromText("Data.json");
            var json = await zip.LoadFromText("Json.cs");
            var code = await zip.LoadFromText("Code.json");
            return new SaveData(data, json, code);
        }
    }
}

public record SaveData(string json, string parsedJson, string code)
{

}

