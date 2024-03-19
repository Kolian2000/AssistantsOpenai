using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    const string ApiKey = "Your Openai api key";
    const string ApiUrl = "https://api.openai.com/v1/";
    public static string threadId = "";
    public static string runId = "";
    static async Task Main()
    {
        var assistantId = "Your assistantId";
        //var threadId = await CreateThread();

        while (true)
        {
            Console.Write("Вопрос: ");
            var userMessage = Console.ReadLine();

            if (userMessage.ToLower() == "exit")
                break;

            if (string.IsNullOrEmpty(threadId))
            {
               await RunAssistant(assistantId,userMessage);
               await CheckRunStatus();
               var responses = await GetMessages(threadId);
                Console.WriteLine($"Ответ: {responses}");
                continue;
            }
            await AddMessageToThread( "user", userMessage);
            RunAssistantss(assistantId);
            await CheckRunStatus();
            var response = await GetMessages(threadId);
            Console.WriteLine($"Ответ: {response}");
        }
    }

    static async Task AddMessageToThread(string role, string content)
    {
        var client = new HttpClient();
       
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
        client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v1");

        var requestContent = $"{{\"role\": \"{role}\", \"content\": \"{content}\"}}";

        var ss = await client.PostAsync($"{ApiUrl}threads/{threadId}/messages", new StringContent(requestContent, Encoding.UTF8, "application/json"));
    }

    static async Task RunAssistant(string assistantId,string userMessage)
    {
        var client = new HttpClient();

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
        
        client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v1");

        var requestContent = $"{{\"assistant_id\": \"{assistantId}\",\"thread\":{{ \"messages\": [{{\"role\": \"user\", \"content\": \"{userMessage}\"}}]}}}}";

        var response = await client.PostAsync($"{ApiUrl}threads/runs", new StringContent(requestContent, Encoding.UTF8, "application/json"));
        var ss= await response.Content.ReadAsStringAsync();
        runId = JObject.Parse(ss)["id"]?.ToString();
        threadId = JObject.Parse(ss)["thread_id"]?.ToString();
    }

    static async Task<string> GetMessages(string threadId)
    {
        var client = new HttpClient();

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
       
        client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v1");

        var response = await client.GetAsync($"{ApiUrl}threads/{threadId}/messages");
       

        var responseBody = await response.Content.ReadAsStringAsync();
        return JObject.Parse(responseBody)["data"]?[0]?["content"]?[0]?["text"]?["value"]?.ToString();
    }
    static async Task<string> CheckRunStatus()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
        client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v1");
        string status = "queued";
        while (status != "completed")
        {
            var response = await client.GetAsync($"{ApiUrl}threads/{threadId}/runs/{runId}");
            var responseBody = await response.Content.ReadAsStringAsync();
            var runStatus = JObject.Parse(responseBody)["status"]?.ToString();

            if (runStatus != null)
            {
                status = runStatus.ToLower();
            }
            else
            {
                Console.WriteLine("Не удалось получить статус выполнения.");
                break;
            }
            if (status != "completed")
            {
                await Task.Delay(TimeSpan.FromSeconds(5)); 
            }
        }
         

        return status;
    }
    static async Task RunAssistantss(string assistantId)
    {
        var client = new HttpClient();

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
        client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v1");
        var requestContent = new
        {
            assistant_id = assistantId
        };

        var response = await client.PostAsync($"{ApiUrl}threads/{threadId}/runs",
                                              new StringContent(JsonConvert.SerializeObject(requestContent),
                                                               Encoding.UTF8,
                                                               "application/json"));
        var ss = await response.Content.ReadAsStringAsync();
    }

}
