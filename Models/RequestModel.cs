using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Lance.ViewModels;

namespace Lance.Models;

public class RequestModel(
    string filePath,
    HttpMethod method,
    string url = "",
    string? body = null,
    IEnumerable<HttpHeaderViewModel>? headers = null)
{
    // TODO: The whole logic of saving files should be in the Model, not the ViewModel.
    private const string? _defaultFileExtension = "json";
    public string FilePath { get; } = filePath;
    public string? Body { get; } = body;
    public IEnumerable<HttpHeaderViewModel> Headers { get; } = headers ?? Array.Empty<HttpHeaderViewModel>();
    public string Url { get; } = url;
    public HttpMethod Method { get; } = method;

    public static RequestModel? ReadFromFile(string filePath)
    {
        string jsonFileContent = File.ReadAllText(filePath);

        RequestModel? requestModel =
            JsonSerializer.Deserialize<RequestModel>(jsonFileContent, new JsonSerializerOptions());

        return requestModel;
    }

    public async Task WriteToFileAsync()
    {
        if (!File.Exists(FilePath))
        {
            File.Create(FilePath);
        }

        await File.WriteAllTextAsync(
            string.Concat(FilePath, ".", _defaultFileExtension) ??
            throw new Exception("Request does not have a file name."),
            JsonSerializer.Serialize(this));
    }
}