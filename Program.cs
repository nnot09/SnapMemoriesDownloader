using Newtonsoft.Json;
using SnapMemoriesDownloader;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

Console.Write("Please enter the path to 'memories_history.json': ");

string path = Console.ReadLine();

if (path == null)
{
    Console.WriteLine("Invalid value");
    return;
}

if (!File.Exists(path))
{
    Console.WriteLine("File '{0}' was not found.", path);
    return;
}

Console.WriteLine("Parsing file...");

SavedMedia savedMedia;

try
{
    string jsonContent = File.ReadAllText(path);

    savedMedia = JsonConvert.DeserializeObject<SavedMedia>(jsonContent);
}
catch (Exception ex)
{
    Console.WriteLine("Parsing failed: {0}", ex.Message);
    return;
}

if (savedMedia == null || savedMedia.MediaItems == null || savedMedia.MediaItems.Count == 0)
{
    Console.WriteLine("No media available.");
    return;
}

Console.WriteLine("Preparing data...");

int i = 1;
foreach (var m in savedMedia.MediaItems)
{
    m.Id = i++;
    m.Date = DateTime.Parse(m.DateString.Replace(" UTC", ""));
    m.Type = (m.Type == "Image") ? "jpg" : "mp4";
}

Console.Write("Enable multi threading (yes or no): ");

string choice = Console.ReadLine();
bool enableMT;

if (choice == "yes")
    enableMT = true;
else if (choice == "no")
    enableMT = false;
else
{
    Console.WriteLine("Invalid value");
    return;
}

Console.WriteLine("Summary");
Console.WriteLine(" >> Media: {0}", savedMedia.MediaItems.Count);
Console.WriteLine(" >> Pictures: {0}", savedMedia.MediaItems.Where(i => i.Type == "jpg").Count());
Console.WriteLine(" >> Videos: {0}", savedMedia.MediaItems.Where(i => i.Type == "mp4").Count());
Console.WriteLine(" >> Multithreading: {0}", enableMT ? "enabled" : "disabled");
Console.WriteLine(" >>> Press any key to start the download <<<");

Console.Read();

Console.Clear();

string baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), $"Downloads_{DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss")}");

Console.WriteLine("Target Path: {0}", baseDirectory);

try
{
    await ResolveUrls(savedMedia, enableMT);
    await DownloadMedia(savedMedia, enableMT, baseDirectory);
}
catch (Exception ex) { Console.WriteLine("Error: {0}", ex.Message); }

Console.WriteLine("Download Completed. You can close this window by pressing any key.");

Console.Read();

static async Task GetDownloadUrl(Media media, HttpClient client)
{
    string[] paramParts = media.Url.Split('?')[1].Split('&');

    List<KeyValuePair<string, string>> paramPairs = new List<KeyValuePair<string, string>>();

    foreach (var paramPart in paramParts)
    {
        string[] kv = paramPart.Split('=');
        paramPairs.Add(new KeyValuePair<string, string>(kv[0], kv[1]));
    }

    var content = new FormUrlEncodedContent(paramPairs);
    content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("application/x-www-form-urlencoded");
    try
    {
        using (var post = await client.PostAsync("/dmd/memories", content))
        {
            using (var responseContent = post.Content)
            {
                if (post.IsSuccessStatusCode)
                {
                    var awsDownloadUrl = await responseContent.ReadAsStringAsync();

                    if (!Uri.TryCreate(awsDownloadUrl, UriKind.Absolute, out Uri aws))
                        Console.WriteLine("Failed to acquire legitimate URI: {0}", awsDownloadUrl);
                    else
                    {
                        media.ResolvedAWSUrl = aws;
                        media.AwsHost = aws.Host;
                    }
                }
                else
                {
                    Console.WriteLine("Failed to resolve '{0}'", media.Url);
                }
            }
        }
    }
    catch { Console.WriteLine("Error: {0}", media.Url); }
}
static async Task DownloadMediaFromUrl(Media media, string baseDirectory, HttpClient client)
{
    string fileName = media.Date.ToString("yyyy-MM-dd_HH-mm-ss") + $".{media.Type}";
    string outputPath = Path.Combine(baseDirectory, fileName);

    try
    {
        byte[] file = await client.GetByteArrayAsync(media.ResolvedAWSUrl);
        await File.WriteAllBytesAsync(outputPath, file);

        Console.WriteLine("+ Downloaded media '{0}'", fileName);
    }
    catch (Exception ex)
    {
        Console.WriteLine("- Failed to download '{0}': {1}", media.ResolvedAWSUrl, ex.Message);

        if (File.Exists(outputPath))
            try { File.Delete(outputPath); } catch { }
    }
}
static async Task ResolveUrls(SavedMedia savedMedia, bool enableMT)
{
    using (HttpClient client = new HttpClient())
    {
        client.BaseAddress = new Uri("https://app.snapchat.com");

        Console.WriteLine("Resolving URLs...");

        if (enableMT)
        {
            await Parallel.ForEachAsync(savedMedia.MediaItems, async (media, token) =>
            {
                await GetDownloadUrl(media, client);
            });
        }
        else
        {
            foreach (var media in savedMedia.MediaItems)
            {
                await GetDownloadUrl(media, client);
            }
        }
    }
}
static async Task DownloadMedia(SavedMedia savedMedia, bool enableMT, string baseDirectory)
{
    Directory.CreateDirectory(baseDirectory);

    var resolved = savedMedia.MediaItems.Where(sm => sm.ResolvedAWSUrl != null);

    var host = resolved.FirstOrDefault()?.AwsHost;

    if ( host == null )
    {
        Console.WriteLine("No data available.");
        return;
    }

    using (HttpClient client = new HttpClient())
    {
        client.BaseAddress = new Uri("https://" + host);

        Console.WriteLine("Downloading Media...");

        if (enableMT)
        {
            await Parallel.ForEachAsync(resolved, async (media, token) =>
            {
                await DownloadMediaFromUrl(media, baseDirectory, client);
            });
        }
        else
        {
            foreach (var media in resolved)
            {
                await DownloadMediaFromUrl(media, baseDirectory, client);
            }
        }
    }
}