using OllamaSharp;
using OllamaSharp.Models.Chat;
using System.Collections.Concurrent;
using System.Text.Json;
using OllamaSharp.Models;

namespace Onllama.OllamaBatch
{
    internal class Program
    {
        public static HttpClient httpClient = new HttpClient()
            {BaseAddress = new Uri("http://xw-gpu:11434"), Timeout = TimeSpan.FromMinutes(5)};
        public static OllamaApiClient client = new OllamaApiClient(httpClient);

        static void Main(string[] args)
        {
            var lines = File.ReadLines("1.jsonl");
            var tasks = new List<Task>();
            var answers = new ConcurrentBag<string>();

            foreach (var line in lines)
            {
                var req = JsonSerializer.Deserialize<Req>(line);

                Console.WriteLine("Q:" + req.custom_id);

                if (long.Parse(req.custom_id.Split('-').Last()) <= 20293) continue;
                //req?.body.messages.Insert(0, new Message(ChatRole.System, "/no_think"));
                var chat = new ChatRequest()
                    {Model = "hf-mirror.com/unsloth/GLM-4-9B-0414-GGUF:Q4_K_M", Messages = req?.body.messages, Stream = false, KeepAlive = "-1s"};
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await foreach (var item in client.ChatAsync(chat))
                        {
                            //item.Message.Content = item.Message.Content?.Split("</think>").LastOrDefault()?.Trim();
                            req.body.model = item.Model;
                            req.body.messages.Add(item.Message);
                            answers.Add(JsonSerializer.Serialize(req, new JsonSerializerOptions
                            {
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                WriteIndented = false
                            }));
                            Console.WriteLine("R:" + req.custom_id);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }));

                if (tasks.Count < 12) continue;

                Task.WaitAll(tasks.ToArray());
                tasks.Clear();

                File.AppendAllLines("2.jsonl", answers);
                answers.Clear();
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
