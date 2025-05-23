using OllamaSharp;
using OllamaSharp.Models.Chat;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Onllama.OllamaBatch
{
    internal class Program
    {
        public static HttpClient httpClient = new HttpClient()
            {BaseAddress = new Uri("http://xw-gpu:11434"), Timeout = TimeSpan.FromMinutes(10)};
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
                var res = new ConcurrentBag<string>();
                tasks.Add(Task.Run(async () =>
                {
                    await foreach (var item in client.ChatAsync(chat))
                    {
                        item.Message.Content = item.Message.Content?.Split("</think>").LastOrDefault()?.Trim();
                        req.body.model = item.Model;
                        req.body.messages.Add(item.Message);
                        res.Add(JsonSerializer.Serialize(req, new JsonSerializerOptions
                        {
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                            WriteIndented = false
                        }));
                    }
                }));

                if (tasks.Count < 8) continue;
                Task.WaitAll(tasks.ToArray());
                tasks.Clear();
                File.AppendAllLines("2.jsonl", res);
                res.Clear();
            }
        }

        public class Body
        {
            public string model { get; set; }
            public List<Message> messages { get; set; }
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
