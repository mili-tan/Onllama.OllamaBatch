using System.Text.Json;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace Onllama.OllamaBatch
{
    internal class Program
    {
        public static HttpClient httpClient = new HttpClient()
            {BaseAddress = new Uri("http://192.168.31.25:11434"), Timeout = TimeSpan.FromMinutes(10)};
        public static OllamaApiClient client = new OllamaApiClient(httpClient);

        static void Main(string[] args)
        {
            var lines = File.ReadLines("1.jsonl");
            var tasks = new List<Task>();
            foreach (var line in lines)
            {
                var req = JsonSerializer.Deserialize<Req>(line);
                Console.WriteLine(req.custom_id);
                req?.body.messages.Insert(0, new Message(ChatRole.System, "/no_think"));
                var chat = new ChatRequest() { Model = "qwen3:1.7b", Messages = req?.body.messages, Stream = false };
                tasks.Add(Task.Run(async () =>
                {
                    await foreach (var item in client.ChatAsync(chat))
                    {
                        req.body.model = item.Model;
                        req.body.messages.Add(item.Message);
                        await File.AppendAllLinesAsync("2.jsonl", [
                            JsonSerializer.Serialize(req, new JsonSerializerOptions
                            {
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                WriteIndented = false
                            })
                        ]);
                    }
                }));
                if (tasks.Count < 4) continue;
                Task.WaitAll(tasks.ToArray());
                tasks.Clear();
            }
        }

        public class Body
        {
            public string model { get; set; }
            public List<Message> messages { get; set; }
            public int max_tokens { get; set; }
        }

        public class Req
        {
            public string custom_id { get; set; }
            public string method { get; set; }
            public string url { get; set; }
            public Body body { get; set; }
        }
    }
}
