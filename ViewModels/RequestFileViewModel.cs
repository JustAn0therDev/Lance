using Lance.Models;

namespace Lance.ViewModels;

public class RequestFileViewModel(string fileName, RequestModel requestModel)
{
    public string FileName { get; } = fileName;
    public RequestModel RequestModel { get; } = requestModel;
}