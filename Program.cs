using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using File = System.IO.File;

HttpClient client = new HttpClient();

string imagePath = "";
if (args.Length > 1)
{
    imagePath = Environment.GetCommandLineArgs()[1];
}

string host = Environment.GetEnvironmentVariable("HOST_NAME") ?? "localhost";

string imageName = imagePath.Length > 0 ? imagePath : "OCRMultipleLanguages.png";

// First, make a post request to upload your image and get back a workfileId.
string workfileId = await UploadWorkfile(imageName, client);

// Use that workfileId to start an OCR Reader process with the workfile you submitted.
string processId = await PostOcrReaderRequest(workfileId, client);

// Wait until the process is complete and print out OCR results.
string ocrResults = await GetOCRResults(processId, client);
Console.Write(ocrResults);

async Task<string> UploadWorkfile(string fileName, HttpClient client)
{
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
    string workfileId = "";
    var workFileUrl = "http://" + host + ":18681/PCCIS/V1/WorkFile?FileExtension=png";

    try
    {
        await using var stream = File.OpenRead(fileName);
        HttpResponseMessage response;

        response = await client.PostAsync(workFileUrl, new StreamContent(stream));

        string responseBody = await response.Content.ReadAsStringAsync();
        workfileId = JObject.Parse(responseBody)["fileId"]!.ToString();
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
    

    return workfileId;
}

async Task<string> PostOcrReaderRequest(string workfileId, HttpClient client)
{
    var jsonData = new
    {
        input = new
        {
            source = new
            {
                fileId = workfileId
            },
            fields = new[] {
            new {
                area = new {
                    x = 0,
                    y = 0,
                    width = 0,
                    height = 0
                }
            }
        },
            minimumCharacterConfidence = 0,
            defaultHorizontalResolution = 300
        }
    };

    client.DefaultRequestHeaders.Clear();
    var contentType = new MediaTypeWithQualityHeaderValue("application/json");
    client.DefaultRequestHeaders.Accept.Add(contentType);
    client.DefaultRequestHeaders.Add("Accusoft-Tenant-Id", "TestTenantId");

    // Start ocr reader process
    var content = new StringContent(JsonConvert.SerializeObject(jsonData), Encoding.UTF8, "application/json");
    var startOcrReaderWorker = await client.PostAsync("http://" + host + ":3000/v1/processes/ocrReaders/", content);

    // After our POST request, the response should contain a processId that we will use later.
    string responseBody = await startOcrReaderWorker.Content.ReadAsStringAsync();
    string processId = JObject.Parse(responseBody)["processId"]!.ToString();
    return processId;
}

async Task<string> GetOCRResults(string processId, HttpClient client)
{
    string ocrResults = "";
    string state = "processing";
    string responseBody = "";

    // Use the processId to check the status of our request and get OCR results.
    // OCR takes some time, so this request might not be complete the first time.
    // Continuously poll until the state is "complete".
    while (state.Equals("processing"))
    {
        Thread.Sleep(500);
        responseBody = await client.GetStringAsync("http://" + host + ":3000/v1/processes/ocrReaders/" + processId);

        state = JObject.Parse(responseBody)["state"]!.ToString();
    }

    if (state.Equals("complete"))
    {
        var outputFileId = JObject.Parse(responseBody)["output"]!["fileId"];

        var response = await client.GetAsync("http://" + host + ":18681/PCCIS/V1/WorkFile/" + outputFileId);
        ocrResults = await response.Content.ReadAsStringAsync();
    }
    else if (state.Equals("error"))
    {
        Console.WriteLine(responseBody);
    }

    return ocrResults;
}
