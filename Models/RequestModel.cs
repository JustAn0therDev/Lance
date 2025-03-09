using Lance.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lance.Models;

public class RequestModel
{
    private const string REQUEST_FILES_DIRECTORY = "RequestFiles";
    private const string DEFAULT_FILE_EXTENSION = "json";
    public Guid Id { get; }
    public string FileName { get; }
    public string? Body { get; }
    public IEnumerable<HttpHeaderViewModel> Headers { get; }
    public string Url { get; }
    public HttpMethod Method { get; }

    public RequestModel(string fileName,
        HttpMethod method,
        string url = "",
        string? body = null,
        IEnumerable<HttpHeaderViewModel>? headers = null)
    {
        Id = Guid.NewGuid();
        FileName = fileName;
        Method = method;
        Url = url;
        Body = body;
        Headers = headers ?? Array.Empty<HttpHeaderViewModel>();
    }

    private static string GetFullPathWithExtension(string fileName)
    {
        return $"{REQUEST_FILES_DIRECTORY}\\{fileName}.{DEFAULT_FILE_EXTENSION}";
    }

    public static IEnumerable<RequestModel> ReadAllSaved()
    {
        List<RequestModel> allSavedFiles = new();

        if (!Directory.Exists(REQUEST_FILES_DIRECTORY))
        {
            Directory.CreateDirectory(REQUEST_FILES_DIRECTORY);
        }

        IEnumerable<string> filePaths = Directory.GetFiles(REQUEST_FILES_DIRECTORY)
            .Select(s => s.Split("\\")[1].Split(".")[0]);

        foreach (string file in filePaths)
        {
            RequestModel? requestModel = ReadFromFile(file);

            if (requestModel == null)
            {
                continue;
            }

            allSavedFiles.Add(requestModel);
        }

        return allSavedFiles;
    }

    public static RequestModel? ReadFromFile(string fileName)
    {
        string jsonFileContent =
            File.ReadAllText(GetFullPathWithExtension(fileName));

        RequestModel? requestModel =
            JsonSerializer.Deserialize<RequestModel>(jsonFileContent,
                new JsonSerializerOptions());

        return requestModel;
    }

    public async Task WriteToFileAsync()
    {
        string filePathWithExtension = GetFullPathWithExtension(FileName);

        if (!File.Exists(filePathWithExtension))
        {
            File.Create(filePathWithExtension);
        }

        await File.WriteAllTextAsync(filePathWithExtension, JsonSerializer.Serialize(this));
    }
}