using HtmlExtractor.Support;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace HtmlExtractor.Chromium
{
    public class ChromiumClient : IDisposable
    {
        private const string ConnectionString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";

        private static readonly IReadOnlyList<string> ExcludedExtensions = new List<string>
        {
            ".css",
            ".gif",
            ".jpg",
            ".png",
            ".svg",
            ".woff2",
        };

        private static readonly IReadOnlyList<string> ExcludedSubstrings = new List<string>
        {
            "ads.scorecardresearch.com",
            "assets.adobedtm.com",
            "cm.everesttech.net",
            "demdex.net",
            "doubleclick.net",
            "fonts.googleapis.com",
            "googleadservices.com",
            "googletagmanager.com",
            "omtrdc.net",
            "sstats.kroger.com",
            "www.kroger.com/asset/",
            "www.kroger.com/clickstream/",
            "www.kroger.com/product/images/",
        };

        private readonly Deserializer _deserializer;
        private readonly IKrogerClientSettings _settings;
        private readonly ILogger<ChromiumClient> _logger;
        private readonly ConcurrentQueue<Func<Response, Task>> _listeners;
        private readonly Lazy<Task<Browser>> _lazyBrowser;
        private Browser _browserForDispose;
        private bool _disposed;
        private bool _signedIn;

        public ChromiumClient(
            Deserializer deserializer,
            IKrogerClientSettings settings,
            ILogger<ChromiumClient> logger)
        {
            _deserializer = deserializer;
            _settings = settings;
            _logger = logger;
            _listeners = new ConcurrentQueue<Func<Response, Task>>();
            _lazyBrowser = new Lazy<Task<Browser>>(async () =>
            {
                var revisionInfo = await EnsureDownloadsAsync();

                _logger.LogDebug($"Launching Chromium from:{Environment.NewLine}{{ExecutablePath}}", revisionInfo.ExecutablePath);
                var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    ExecutablePath = revisionInfo.ExecutablePath,
                    Headless = true,
                    DumpIO = _settings.Debug,
                });
                _browserForDispose = browser;
                return browser;
            });
        }

        private string GetDownloadsDirectory()
        {
            return Path.GetFullPath(Path.Combine(_settings.DownloadsPath, "Chromium"));
        }

        private async Task<RevisionInfo> EnsureDownloadsAsync()
        {
            var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions
            {
                Path = GetDownloadsDirectory(),
            });
            var desiredRevision = BrowserFetcher.DefaultRevision;
            var revisionInfo = browserFetcher.RevisionInfo(desiredRevision);

            if (revisionInfo.Downloaded && revisionInfo.Local)
            {
                _logger.LogDebug(
                    "Using Chromium revision {Revision} for {Platform}.",
                    revisionInfo.Revision,
                    revisionInfo.Platform);
            }
            else
            {
                _logger.LogInformation(
                    $"Downloading Chromium revision {{Revision}} for {{Platform}} to:{Environment.NewLine}{{FolderPath}}",
                    revisionInfo.Revision,
                    revisionInfo.Platform,
                    revisionInfo.FolderPath);

                // Set up the progress report.
                long? contentLength;
                using (var httpClient = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Head, revisionInfo.Url))
                using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead))
                {
                    contentLength = response.Content.Headers.ContentLength;
                }

                long progressIncrement;
                if (contentLength.HasValue)
                {
                    _logger.LogDebug("The download will be {Bytes:N0} bytes.", contentLength);

                    // 5%
                    progressIncrement = 5;
                }
                else
                {
                    // 1 megabyte
                    progressIncrement = 1024 * 1024;
                }

                long nextProgress = 0;
                var progressLock = new object();
                browserFetcher.DownloadProgressChanged += (sender, e) =>
                {
                    lock (progressLock)
                    {
                        if (contentLength.HasValue)
                        {
                            var progressPercentage = (int)Math.Round((100.0 * e.BytesReceived) / contentLength.Value);
                            if (progressPercentage > nextProgress)
                            {
                                _logger.LogInformation("Download progress: {Percentage}%", progressPercentage);
                                nextProgress += progressIncrement;
                            }
                        }
                        else
                        {
                            if (e.BytesReceived > nextProgress)
                            {
                                _logger.LogInformation("Download progress: {BytesReceived:N0} bytes", e.BytesReceived);
                                nextProgress += progressIncrement;
                            }
                        }
                    }
                };

                // Start the download.
                revisionInfo = await browserFetcher.DownloadAsync(desiredRevision);

                if (contentLength.HasValue)
                {
                    _logger.LogInformation("Download progress: {Percentage}%", 100);
                }

                _logger.LogInformation("Chromium is done downloading.");
            }

            return revisionInfo;
        }

        public void AddResponseRecordListener(Func<Response, Task> onResponseRecordAsync)
        {
            _listeners.Enqueue(onResponseRecordAsync);
        }

        private async Task<Page> GetPageAsync()
        {
            var browser = await _lazyBrowser.Value;
            var page = await browser.NewPageAsync();

            await page.SetUserAgentAsync(ConnectionString);

            await page.EvaluateOnNewDocumentAsync(@"
function () {
    Object.defineProperty(navigator, 'webdriver', {
        get: () => false,
    });
}");

            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = 1024,
                Height = 768,
            });

            await page.SetRequestInterceptionAsync(true);

            page.Request += async (sender, requestEventArgs) =>
            {
                if (!_settings.Debug)
                {
                    var url = requestEventArgs.Request.Url;

                    foreach (var extension in ExcludedExtensions)
                    {
                        if (url.EndsWith(extension))
                        {
                            await requestEventArgs.Request.AbortAsync();
                            return;
                        }
                    }

                    foreach (var substring in ExcludedSubstrings)
                    {
                        if (url.Contains(substring))
                        {
                            await requestEventArgs.Request.AbortAsync();
                            return;
                        }
                    }
                }

                await requestEventArgs.Request.ContinueAsync();
            };

            return page;
        }

        public void KillOrphanBrowsers()
        {
            _logger.LogDebug("Searching for orphan Chromium processes.");
            var downloadsDirectory = GetDownloadsDirectory();
            var allProcesses = Process.GetProcesses();
            try
            {
                var orphanProcesses = Process
                    .GetProcesses()
                    .Where(x =>
                    {
                        try
                        {
                            return Path.GetFullPath(x.MainModule.FileName).StartsWith(downloadsDirectory);
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .OrderBy(x => x.StartTime)
                    .ToList();


                if (!orphanProcesses.Any())
                {
                    _logger.LogDebug("None were found.");
                }
                else
                {
                    var fileNameList = string.Join(
                        Environment.NewLine,
                        orphanProcesses
                            .Select(x => x.MainModule.FileName)
                            .GroupBy(x => x)
                            .OrderByDescending(x => x.Count())
                            .ThenBy(x => x.Key, StringComparer.Ordinal)
                            .Select(x => $"{x.Key} (count: {x.Count()})"));

                    _logger.LogWarning(
                        $"Found {{Count}} orphan Chromium processes with the following file names:{Environment.NewLine}{{FileNameList}}",
                        orphanProcesses.Count,
                        fileNameList);

                    foreach (var process in orphanProcesses)
                    {
                        if (process.HasExited)
                        {
                            _logger.LogWarning("Process {ProcessId} has already exited.", process.Id);
                        }
                        else
                        {
                            _logger.LogWarning(
                               "Stopping process {ProcessId}, which was started on {StartTime}.",
                               process.Id,
                               process.StartTime);
                            try
                            {
                                process.Kill();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex, "Failed to stop process {ProcessId}.", process.Id);
                            }
                        }
                    }
                }
            }
            finally
            {
                foreach (var process in allProcesses)
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch
                    {
                        // Ignore this failure.
                    }
                }
            }
        }

        public async Task InitializeAsync()
        {
            ThrowIfDisposed();

            using (var page = await GetPageAsync())
            {
            }
        }

        public async Task<DeserializedResponse<SignInResponse>> SignInAsync(CancellationToken token)
        {
            ThrowIfDisposed();
            if (_signedIn)
            {
                throw new InvalidOperationException("The user is already signed in.");
            }

            using (var page = await GetPageAsync())
            using (var captureState = new CaptureState(
                _deserializer,
                OperationType.SignIn,
                null,
                _listeners.ToList(),
                _logger))
            {
                captureState.CaptureAuthenticationState(page);
                captureState.CaptureSignIn(page);

                await page.GoToAsync(
                    "https://www.kroger.com/signin?redirectUrl=/",
                    new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    });

                token.ThrowIfCancellationRequested();

                await CaptureScreenshotIfDebugAsync(page, $"{nameof(SignInAsync)}-Before");

                await page.TypeAsync("#SignIn-emailInput", _settings.Email);
                await page.TypeAsync("#SignIn-passwordInput", _settings.Password);
                await page.ClickAsync("#SignIn-submitButton");

                await page.WaitForNavigationAsync(
                    new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    });

                await CaptureScreenshotIfDebugAsync(page, $"{nameof(SignInAsync)}-After");

                token.ThrowIfCancellationRequested();

                await captureState.WaitForCompletionAsync();

                var response = captureState
                    .GetValues<SignInResponse>()
                    .LastOrDefault();

                _signedIn = response?.Response.AuthenticationState?.Authenticated == true;

                return response;
            }
        }

        private async Task CaptureScreenshotIfDebugAsync(Page page, string name)
        {
            if (!_settings.Debug)
            {
                return;
            }

            var fileName = $"{DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffffff")}-{name}.png";
            var directory = Path.Combine(_settings.DownloadsPath, "Screenshots");
            var path = Path.Combine(directory, fileName);

            _logger.LogDebug(
                $"Capturing a screenshot of URL:{Environment.NewLine}" +
                $"{{PageUrl}}{Environment.NewLine}" +
                $"The screenshot will be written to:{Environment.NewLine}" +
                $"{{ScreenshotPath}}",
                page.Url,
                path);

            Directory.CreateDirectory(directory);

            try
            {
                await page.ScreenshotAsync(path, new ScreenshotOptions
                {
                    FullPage = true,
                    Type = ScreenshotType.Png,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The screenshot could not be captured.");
            }
        }

        public async Task<DeserializedResponse<List<Receipt>>> GetReceiptSummariesAsync(CancellationToken token)
        {
            ThrowIfDisposed();
            ThrowIfNotSignedIn();

            using (var page = await GetPageAsync())
            using (var captureState = new CaptureState(
                _deserializer,
                OperationType.GetReceiptSummaries,
                null,
                _listeners.ToList(),
                _logger))
            {
                captureState.CaptureAuthenticationState(page);
                captureState.CaptureReceiptSummaryByUserId(page);

                await page.GoToAsync(
                    "https://www.kroger.com/mypurchases",
                    new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    });

                await CaptureScreenshotIfDebugAsync(
                    page,
                    nameof(GetReceiptSummariesAsync));

                await captureState.WaitForCompletionAsync();

                return captureState
                    .GetValues<List<Receipt>>()
                    .LastOrDefault();
            }
        }

        public async Task<DeserializedResponse<Receipt>> GetReceiptAsync(ReceiptId receiptId, CancellationToken token)
        {
            ThrowIfDisposed();
            ThrowIfNotSignedIn();

            var pageUrl = receiptId.GetUrl();

            using (var page = await GetPageAsync())
            using (var captureState = new CaptureState(
                _deserializer,
                OperationType.GetReceipt,
                receiptId,
                _listeners.ToList(),
                _logger))
            {
                captureState.CaptureAuthenticationState(page);
                captureState.CaptureReceiptDetail(page);

                await page.GoToAsync(
                    pageUrl,
                    new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    });

                token.ThrowIfCancellationRequested();

                await CaptureScreenshotIfDebugAsync(
                    page,
                    $"{nameof(GetReceiptAsync)}-{string.Join("_", receiptId.GetIdentifyingStrings())}");

                await captureState.WaitForCompletionAsync();

                return captureState
                    .GetValues<Receipt>()
                    .LastOrDefault();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ChromiumClient));
            }
        }

        private void ThrowIfNotSignedIn()
        {
            if (!_signedIn)
            {
                throw new InvalidOperationException("The user is not signed in.");
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _browserForDispose?.Dispose();
        }
    }

    public interface IKrogerClientSettings
    {
        string Email { get; }
        string Password { get; }
        string DownloadsPath { get; }
        bool Debug { get; }
    }

    public enum RequestType
    {
        AuthenticationState,
        SignIn,
        ReceiptDetail,
        ReceiptSummaryByUserId,
    }

    public enum OperationType
    {
        Command,
        Uncategorized,
        SignIn,
        GetReceiptSummaries,
        GetReceipt,
    }

    public class Response
    {
        public Response(
            string requestId,
            OperationType operationType,
            object operationParameters,
            RequestType requestType,
            DateTimeOffset timestamp,
            HttpMethod method,
            string url,
            string body)
        {
            RequestId = requestId;
            OperationType = operationType;
            OperationParameters = operationParameters;
            RequestType = requestType;
            CompletedTimestamp = timestamp;
            Method = method;
            Url = url;
            Body = body;
        }

        public string RequestId { get; }
        public OperationType OperationType { get; }
        public object OperationParameters { get; }
        public RequestType RequestType { get; }
        public DateTimeOffset CompletedTimestamp { get; }
        public HttpMethod Method { get; }
        public string Url { get; }
        public string Body { get; }
    }

    public class KrogerClientFactory
    {
        private readonly Func<ChromiumClient> _create;

        public KrogerClientFactory(Func<ChromiumClient> create)
        {
            _create = create;
        }

        public ChromiumClient Create() => _create();
    }
    public class Deserializer
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Unspecified,
            DateParseHandling = DateParseHandling.DateTime,
            MissingMemberHandling = MissingMemberHandling.Error,
        };

        public AuthenticationState AuthenticationState(string json)
        {
            return Deserialize<AuthenticationState>(json);
        }

        public SignInResponse SignInResponse(string json)
        {
            return Deserialize<SignInResponse>(json);
        }

        public List<Receipt> ReceiptSummaries(string json)
        {
            return Deserialize<List<Receipt>>(json);
        }

        public Receipt Receipt(string json)
        {
            return Deserialize<Receipt>(json);
        }

        public ErrorResponse ErrorResponse(string json)
        {
            return Deserialize<ErrorResponse>(json);
        }

        private T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, JsonSerializerSettings);
        }
    }

    public class CaptureState : IDisposable
    {
        private readonly ConcurrentBag<Task> _tasks = new ConcurrentBag<Task>();
        private readonly ConcurrentBag<Action> _detachActions = new ConcurrentBag<Action>();
        private readonly object _valuesLock = new object();
        private readonly Dictionary<Type, List<object>> _values = new Dictionary<Type, List<object>>();
        private readonly Deserializer _deserializer;
        private readonly OperationType _operationType;
        private readonly object _operationParameters;
        private readonly AsyncBlockingQueue<Response> _queue;
        private readonly Task _dequeueTask;
        private readonly IReadOnlyList<Func<Response, Task>> _listeners;
        private readonly ILogger _logger;

        public CaptureState(
            Deserializer deserializer,
            OperationType operationType,
            object operationParameters,
            IReadOnlyList<Func<Response, Task>> listeners,
            ILogger logger)
        {
            _deserializer = deserializer;
            _operationType = operationType;
            _operationParameters = operationParameters;
            _queue = new AsyncBlockingQueue<Response>();
            _dequeueTask = DequeueAsync();
            _listeners = listeners;
            _logger = logger;
        }

        private async Task DequeueAsync()
        {
            await Task.Yield();

            bool hasItem;
            do
            {
                var result = await _queue.TryDequeueAsync();
                hasItem = result.HasItem;

                if (hasItem)
                {
                    foreach (var listener in _listeners)
                    {
                        await listener(result.Item);
                    }
                }
            }
            while (hasItem);
        }

        public List<DeserializedResponse<T>> GetValues<T>()
        {
            var type = typeof(DeserializedResponse<T>);
            lock (_valuesLock)
            {
                if (_values.TryGetValue(type, out var values))
                {
                    return values.Cast<DeserializedResponse<T>>().ToList();
                }
                else
                {
                    return new List<DeserializedResponse<T>>();
                }
            }
        }

        public void CaptureAuthenticationState(Page page)
        {
            Capture(
                page,
                RequestType.AuthenticationState,
                HttpMethod.Get,
                "https://www.kroger.com/auth/api/authentication-state",
                x => _deserializer.AuthenticationState(x));
        }

        public void CaptureReceiptSummaryByUserId(Page page)
        {
            Capture(
                page,
                RequestType.ReceiptSummaryByUserId,
                HttpMethod.Get,
                "https://www.kroger.com/mypurchases/api/v1/receipt/summary/by-user-id",
                x => _deserializer.ReceiptSummaries(x));
        }

        public void CaptureReceiptDetail(Page page)
        {
            Capture(
                page,
                RequestType.ReceiptDetail,
                HttpMethod.Post,
                "https://www.kroger.com/mypurchases/api/v1/receipt/detail",
                x => _deserializer.Receipt(x));
        }

        public void CaptureSignIn(Page page)
        {
            Capture(
                page,
                RequestType.SignIn,
                HttpMethod.Post,
                "https://www.kroger.com/auth/api/sign-in",
                x => _deserializer.SignInResponse(x));
        }

        private void InitializeDeserializedResponseList<T>()
        {
            var type = typeof(DeserializedResponse<T>);
            lock (_valuesLock)
            {
                if (!_values.ContainsKey(type))
                {
                    _values[type] = new List<object>();
                }
            }
        }

        private void AddDeserializedResponse<T>(string requestId, T value)
        {
            lock (_values)
            {
                _values[typeof(DeserializedResponse<T>)].Add(new DeserializedResponse<T>
                {
                    RequestId = requestId,
                    Response = value,
                });
            }
        }

        private void Capture<T>(
            Page page,
            RequestType requestType,
            HttpMethod method,
            string url,
            Func<string, T> deserialize)
        {
            InitializeDeserializedResponseList<T>();
            InitializeDeserializedResponseList<ErrorResponse>();

            EventHandler<ResponseCreatedEventArgs> eventHandler = (sender, args) =>
            {
                if (args.Response.Request.Method == method
                    && args.Response.Request.Url == url)
                {
                    _logger.LogDebug("Received a response for: {Method} {Url}", method, url);
                    _tasks.Add(CaptureJsonResponseAsync<T>(requestType, args, deserialize));
                }
            };

            _detachActions.Add(() => page.Response -= eventHandler);
            page.Response += eventHandler;
        }

        public async Task WaitForCompletionAsync()
        {
            try
            {
                await Task.WhenAll(_tasks);
            }
            finally
            {
                _queue.MarkAsComplete();
                await _dequeueTask;
            }
        }

        public void Dispose()
        {
            foreach (var action in _detachActions)
            {
                action();
            }
        }

        private async Task CaptureJsonResponseAsync<T>(
            RequestType requestType,
            ResponseCreatedEventArgs args,
            Func<string, T> deserialize)
        {
            await Task.Yield();

            var body = await args.Response.TextAsync();

            var requestId = $"{DateTimeOffset.UtcNow.Ticks:D20}-{Guid.NewGuid():N}";

            _queue.Enqueue(new Response(
                requestId,
                _operationType,
                _operationParameters,
                requestType,
                DateTimeOffset.UtcNow,
                args.Response.Request.Method,
                args.Response.Request.Url,
                body));

            try
            {
                AddDeserializedResponse(
                    requestId,
                    deserialize(body));
            }
            catch (JsonException)
            {
                try
                {
                    AddDeserializedResponse(
                        requestId,
                        _deserializer.ErrorResponse(body));

                    var prettyJson = JObject.Parse(body).ToString(Formatting.Indented);
                    _logger.LogError(
                        $"An error was returned by Kroger.com:{Environment.NewLine}{{ErrorJson}}",
                        prettyJson);
                }
                catch
                {
                    // Ignore these failures.
                }

                throw;
            }
        }
    }

    public class ErrorResponse
    {
        public bool? Error { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class DeserializedResponse<T>
    {
        public string RequestId { get; set; }
        public T Response { get; set; }
    }

    public class Receipt
    {
        private List<JToken> _tenderChanges;
        private List<JToken> _priceModifiers;
        private List<JToken> _tags;

        public ReceiptAddress Address { get; set; }
        public List<Item> Items { get; set; }
        public decimal? TotalSavings { get; set; }
        public int? TotalLineItems { get; set; }
        public string LoyaltyId { get; set; }
        public ReceiptId ReceiptId { get; set; }
        public List<Tax> Tax { get; set; }
        public List<Tender> Tenders { get; set; }

        public List<JToken> TenderChanges
        {
            get => _tenderChanges;
            set => ModelHelper.SetIfNullOrEmpty(ref _tenderChanges, value);
        }

        public decimal? Total { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? TotalTax { get; set; }
        public string FulfillmentType { get; set; }

        public List<JToken> PriceModifiers
        {
            get => _priceModifiers;
            set => ModelHelper.SetIfNullOrEmpty(ref _priceModifiers, value);
        }

        public List<JToken> Tags
        {
            get => _tags;
            set => ModelHelper.SetIfNullOrEmpty(ref _tags, value);
        }

        public GrossAmount GrossAmount { get; set; }
        public Coupon Coupon { get; set; }
        public string Source { get; set; }
        public string Version { get; set; }
        public DateTime? TransactionTime { get; set; }
        public DateTimeOffset? TransactionTimeWithTimezone { get; set; }
        public decimal? TotalTender { get; set; }
        public decimal? TotalTenderChange { get; set; }
    }
}
