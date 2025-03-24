using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lance.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(MakeRequestCommand))]
    private string _url = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(MakeRequestCommand))]
    private HttpMethod _selectedMethod = HttpMethod.Get;

    [ObservableProperty] private string? _body;

    [ObservableProperty] private bool _requestCompleted;

    [ObservableProperty] private string? _formattedRequestTime;

    [ObservableProperty] private string? _formattedStatusCode;

    [ObservableProperty] private HttpStatusCode? _statusCode;

    [ObservableProperty] private string? _responseContent;

    [ObservableProperty] private ObservableCollection<HttpHeaderViewModel> _inputRequestHeaders =
    [
        new("Accept", "application/json"),
        new("Content-Type", "application/json")
    ];

    [ObservableProperty] private ObservableCollection<HttpHeaderViewModel> _responseHeaders = new();

    [ObservableProperty] private ObservableCollection<HttpHeaderViewModel> _outputRequestHeaders = new();

    private readonly string[] _contentHeaders =
    [
        "Allow", "Content-Disposition", "Content-Encoding", "Content-Language", "Content-Length", "Content-Location",
        "Content-MD5", "Content-Range", "Content-Type", "Expires", "Last-Modified", "Non-Validated"
    ];

    // TODO: FOR TESTING TAB SELECTION AFTER THE USER RECEIVES A RESPONSE. IT IS 0 BY DEFAULT.
    [ObservableProperty] private int _selectedResponseTabControlIndex;

    public HttpMethod[] HttpMethods { get; } =
        [HttpMethod.Get, HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete, HttpMethod.Options];

    private void ClearFinalHeaders()
    {
        ResponseHeaders.Clear();
        OutputRequestHeaders.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanMakeRequest))]
    private async Task MakeRequest()
    {
        ClearFinalHeaders();
        using HttpClient client = new();
        HttpRequestMessage request = new(SelectedMethod, Url);

        SetRequestHeaders(request);
        SetBodyWithHeaders(request);

        Stopwatch stopwatch = new();
        stopwatch.Start();
        HttpResponseMessage response = await client.SendAsync(request);
        stopwatch.Stop();

        await UpdateResponseData(response, stopwatch.Elapsed);
        SetFinalHeaders(response, request);
        SelectedResponseTabControlIndex = 0;
    }

    private void SetBodyWithHeaders(HttpRequestMessage request)
    {
        if (string.IsNullOrEmpty(Body))
        {
            return;
        }

        request.Content = new StringContent(Body);

        foreach (HttpHeaderViewModel header in InputRequestHeaders)
        {
            SetContentHeader(request, header);
        }
    }

    private static void SetContentHeader(HttpRequestMessage request, HttpHeaderViewModel header)
    {
        if (request.Content is null)
        {
            return;
        }

        switch (header.Key)
        {
            case "Content-Type":
                request.Content.Headers.ContentType = new(header.Value);
                break;
            case "Content-Length":
                // TODO: This should also account for files. Those should have their sizes measured in bytes, not chars.
                request.Content.Headers.ContentLength = header.Value.Length;
                break;
        }
    }

    private void SetRequestHeaders(HttpRequestMessage request)
    {
        if (InputRequestHeaders is not { Count: > 0 })
        {
            return;
        }

        foreach (HttpHeaderViewModel header in InputRequestHeaders)
        {
            if (!_contentHeaders.Contains(header.Key))
                request.Headers.Add(header.Key, header.Value);
        }
    }

    private void SetFinalHeaders(HttpResponseMessage response, HttpRequestMessage request)
    {
        SetResponseContentHeaders(response);
        SetOutputRequestHeaders(request);

        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
        {
            ResponseHeaders.Add(new(header.Key, string.Join(", ", header.Value)));
        }
    }

    private void SetOutputRequestHeaders(HttpRequestMessage request)
    {
        SetOutputContentRequestHeaders(request);
        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
        {
            OutputRequestHeaders.Add(new(header.Key, string.Join(", ", header.Value)));
        }
    }

    private void SetOutputContentRequestHeaders(HttpRequestMessage request)
    {
        if (request.Content is null or { Headers: null })
        {
            return;
        }

        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
        {
            OutputRequestHeaders.Add(new(header.Key, string.Join(", ", header.Value)));
        }
    }

    private void SetResponseContentHeaders(HttpResponseMessage response)
    {
        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
        {
            ResponseHeaders.Add(new(header.Key, string.Join(", ", header.Value)));
        }
    }

    private bool CanMakeRequest() =>
        Uri.TryCreate(Url, UriKind.Absolute, out Uri? _);

    [RelayCommand]
    private void AddRequestHeader() =>
        InputRequestHeaders.Add(new HttpHeaderViewModel(string.Empty, string.Empty));

    [RelayCommand]
    private void RemoveRequestHeader(object? parameter)
    {
        if (parameter is not Guid id)
            return;

        InputRequestHeaders.Remove(InputRequestHeaders.First(f => f.Id == id));
    }

    private async Task UpdateResponseData(HttpResponseMessage response, TimeSpan requestDuration)
    {
        Task<string> stringResponseContent = response.Content.ReadAsStringAsync();

        FormattedRequestTime = $"{Math.Round(requestDuration.TotalMilliseconds)} ms";

        StatusCode = response.StatusCode;
        FormattedStatusCode = $"{(int)response.StatusCode} - {response.ReasonPhrase}";

        await SetResponseContent(response, stringResponseContent);

        RequestCompleted = true;
    }

    private async Task SetResponseContent(HttpResponseMessage response, Task<string> stringResponseContent)
    {
        if (response.Content.Headers.TryGetValues("Content-Type", out IEnumerable<string>? contentTypes))
        {
            if (contentTypes.Any(a => a.Contains("application/json")))
            {
                object? obj = JsonSerializer.Deserialize<object?>(await stringResponseContent);

                if (obj is not null)
                {
                    ResponseContent = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
                    return;
                }
            }
        }

        ResponseContent = await stringResponseContent;
    }
}