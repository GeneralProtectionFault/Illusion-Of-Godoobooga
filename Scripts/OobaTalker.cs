using Godot;
using System;
using System.IO;
using System.Net.Http;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;



public partial class OobaTalker : Node
{
    public static string Server { get; set; } = "127.0.0.1";
    public static string Port { get; set; } = "5000";
    public static string URL { get; set; } = $"http://{Server}:{Port}/v1/chat/completions";


    public static event EventHandler<string> TextUpdate;
    public static event EventHandler TextFinished;


    public static List<message> Prompt = new List<message>();
    private static string Response = "";
    private static System.Net.Http.HttpClient client;



    /// <summary>
    /// Takes in the next message in the conversation
    /// </summary>
    /// <param name="UserInput">The message being sent to the chatbot</param>
    /// <param name="TruncationLength">The maximum number of tokens the conversation history can store.
    /// This needs to be within the model's limits</param>
    /// <param name="MaxTokensPerSecond">Limit how fast the responses come back.
    /// If the response is fast, it may be desirable to set this at a readable rate, particularly if the text will scroll off screen</param>
    /// <param name="ClearMessages">If true, any conversation history will be cleared</param>
    public async static void PromptAI(string UserInput, int TruncationLength, int MaxTokensPerSecond, bool ClearMessages = false)
    {
        Debug.WriteLine("Prompting...");

        if (ClearMessages)
            Prompt.Clear();

        client = new System.Net.Http.HttpClient();

        // This variable is only for what the chatbot says back, not the entire prompt, so clear this variable for each prompt
        Response = "";

        var Ooba = new OobaParameters();
        Ooba.truncation_length = TruncationLength;
        Ooba.max_tokens_second = MaxTokensPerSecond;
        
        
        Prompt.Add(new message() { role = "user", content = UserInput });
        // message class defined in OobaParameters.cs
        Ooba.messages = Prompt;

        var OobaJson = JsonConvert.SerializeObject(Ooba
            ,Formatting.Indented
            ,new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
            ).Replace("\n", "");

        var content = new StringContent(OobaJson, Encoding.UTF8, "application/json");
        
        CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;
                
        var message = new HttpRequestMessage(HttpMethod.Post, URL);
        message.Content = new StringContent(OobaJson, Encoding.UTF8, "application/json");

        // Need to run this as a Task to prevent locking the UI
        await Task.Run(
            async () => { await ConnectToServerSentEvents(client, message); }
        );
        
        client.Dispose();
    }


    private static async Task ConnectToServerSentEvents(System.Net.Http.HttpClient httpClient, HttpRequestMessage message)
    {
        using (message)
        {
            var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("Failed to connect to server.");

            using (var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
            {
                Debug.WriteLine("Reading response stream...");
                while (!streamReader.EndOfStream)
                {
                    var line = await streamReader.ReadLineAsync();
                    
                    // Server-Sent Events, by standard, begin each event with "data:"
                    // This strips that out, leaving us the valid JSON of the response
                    if (line != null && line.StartsWith("data:"))
                    {
                        // Extract the event data from after the "data:" prefix and process it as needed
                        string eventData = line.Substring(5);
                        // Debug.WriteLine($"Received server sent event: {eventData}");
                        
                        ParseOobaResponseJson(eventData);
                    }
                }

                // Debug.WriteLine("Should be finished talking!");
                Prompt.Add(new message() { role = "ai", content = Response });
                TextFinished?.Invoke(null, EventArgs.Empty);
            }
        }
    }


    private static void ParseOobaResponseJson(string ResponseJson)
    {
        var JsonObject = JObject.Parse(ResponseJson);
        var AppendText = JsonObject.SelectToken("choices..message.content").ToString();

        if (AppendText != "")
        {
            Response += AppendText;
            TextUpdate?.Invoke(null, Response);
        }
    }


}
