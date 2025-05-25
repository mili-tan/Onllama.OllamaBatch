using McMaster.Extensions.CommandLineUtils;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Onllama.OllamaBatch
{
    internal class Program
    {
        public static HttpClient httpClient = new HttpClient()
            {BaseAddress = new Uri("http://127.0.0.1:11434"), Timeout = TimeSpan.FromMinutes(5)};
        public static OllamaApiClient client = new OllamaApiClient(httpClient);

        public static string InputFile = "input.jsonl";
        public static string OutputFile = "output.jsonl";
        public static string ModelName = string.Empty;
        public static int Skip = 0;
        public static int MaxParallel = 8;
        public static bool NoThink = false;
        public static bool TrimThink = false;

        static void Main(string[] args)
        {
            var isZh = Thread.CurrentThread.CurrentCulture.Name.Contains("zh");

            var cmd = new CommandLineApplication
            {
                Name = "Onllama.OllamaBatch",
                Description = $"Onllama.OllamaBatch - {(isZh ? "简单的 Ollama 批量推理工具。" : "Simple Ollama batch inference tool.")}" +
                              Environment.NewLine +
                              $"Copyright (c) {DateTime.Now.Year} Milkey Tan. Code released under the MIT License"
            };
            cmd.HelpOption("-?|-h|--help|-he");

            var inputOption = cmd.Option<string>("-i|--input <path>",
                isZh ? "输入 Jsonl 路径。" : "Set input Jsonl path",
                CommandOptionType.SingleValue);
            var outputOption = cmd.Option<string>("-o|--output <path>",
                isZh ? "输出 Jsonl 路径。" : "Set output Jsonl path",
                CommandOptionType.SingleValue);
            var modelOption = cmd.Option<string>("-m|--model <name>",
                isZh ? "覆盖模型名称。" : "Set overwrite model name",
                CommandOptionType.SingleValue);
            var skipOption = cmd.Option<int>("-s|--skip <number>",
                isZh ? "跳过的请求 ID。" : "Set skip request ID",
                CommandOptionType.SingleValue);
            var noThinkOption = cmd.Option<bool>("-nt|--no-think",
                isZh ? "设置 Qwen3 不思考 (/no_think)。" : "Set qwen3 /no_think",
                CommandOptionType.NoValue);
            var trimThinkOption = cmd.Option<bool>("-t|--trim-think",
                isZh ? "修剪思考过程 (<think>)。" : "Set trim <think>",
                CommandOptionType.NoValue);
            var urlOption = cmd.Option<string>("-u|--uurl <URL>",
                isZh ? "Ollama 服务端点。[http://127.0.0.1:11434]" : "Set ollama service URL [http://127.0.0.1:11434]",
                CommandOptionType.SingleValue);
            var timeOutOption = cmd.Option<int>("--timeout <minutes>",
                isZh ? "设置超时时间（分钟）。" : "Set timeout minutes",
                CommandOptionType.SingleValue);
            var maxParallelOption = cmd.Option<int>("--max-parallel <number>",
                isZh ? "最大并行请求数。" : "Set max parallel requests",
                CommandOptionType.SingleValue);

            cmd.OnExecute(() =>
            {
                if (inputOption.HasValue()) InputFile = inputOption.ParsedValue;
                if (outputOption.HasValue()) OutputFile = outputOption.ParsedValue;
                if (modelOption.HasValue()) ModelName = modelOption.ParsedValue;
                if (skipOption.HasValue()) Skip = skipOption.ParsedValue;
                if (noThinkOption.HasValue()) NoThink = noThinkOption.ParsedValue;
                if (trimThinkOption.HasValue()) TrimThink = trimThinkOption.ParsedValue;
                if (urlOption.HasValue()) httpClient.BaseAddress = new Uri(urlOption.ParsedValue);
                if (timeOutOption.HasValue()) httpClient.Timeout = TimeSpan.FromMinutes(timeOutOption.ParsedValue);
                if (maxParallelOption.HasValue()) MaxParallel = maxParallelOption.ParsedValue;

                var lines = File.ReadLines(InputFile);
                var tasks = new List<Task>();
                var answers = new ConcurrentBag<string>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var req = JsonSerializer.Deserialize<Req>(line);
                    Console.WriteLine("Q:" + req?.custom_id);
                    if (req is {custom_id: not null} && long.Parse(req.custom_id.Split('-').Last()) <= Skip) continue;
                    if (NoThink) req?.body.messages.Insert(0, new Message(ChatRole.System, "/no_think"));
                    if (!string.IsNullOrWhiteSpace(ModelName)) req.body.model = ModelName;

                    var chat = new ChatRequest()
                    {
                        Model = req?.body.model ?? "",
                        Messages = req?.body.messages, Stream = false, KeepAlive = "-1s", Options = new RequestOptions()
                    };

                    if (req is {body.temperature: not null}) chat.Options.Temperature = req.body.temperature;
                    if (req is {body.seed: not null}) chat.Options.Seed = req.body.seed;
                    if (req is {body.top_p: not null}) chat.Options.TopP = req.body.top_p;
                    if (req is {body.max_tokens: not null }) chat.Options.NumCtx = req.body.max_tokens;
                    if (req is {body.frequency_penalty: not null})
                        chat.Options.FrequencyPenalty = req.body.frequency_penalty;
                    if (req is {body.presence_penalty: not null})
                        chat.Options.PresencePenalty = req.body.presence_penalty;

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            //using var httpClient = new HttpClient();
                            //using var request = new HttpRequestMessage(new HttpMethod("POST"), "https://www.sophnet.com/api/open-apis/v1/chat/completions");
                            //request.Headers.TryAddWithoutValidation("Authorization", "Bearer ");
                            //request.Content = new StringContent(JsonSerializer.Serialize(req.body));
                            //request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                            //var response = await httpClient.SendAsync(request);
                            //if (response.IsSuccessStatusCode)
                            //{
                            //    var jObj = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                            //    req.body.messages.Add(new Message(ChatRole.Assistant, jObj?["choices"]?[0]?["message"]?["content"]?.ToString()));

                            //    answers.Add(JsonSerializer.Serialize(req, new JsonSerializerOptions
                            //    {
                            //        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                            //        WriteIndented = false
                            //    }));
                            //    Console.WriteLine("R:" + req.custom_id);
                            //}
                            //else
                            //{
                            //    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                            //}


                            await foreach (var item in client.ChatAsync(chat))
                            {
                                if (TrimThink)
                                    item.Message.Content =
                                        item.Message.Content?.Split("</think>").LastOrDefault()?.Trim();

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

                    if (tasks.Count < MaxParallel) continue;

                    Task.WaitAll(tasks.ToArray());
                    tasks.Clear();

                    File.AppendAllLines(OutputFile, answers);
                    answers.Clear();
                }
            });

            cmd.Execute(args);
        }

        public class Body
        {
            public string? model { get; set; }
            public List<Message> messages { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public float? temperature { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public float? top_p { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public float? frequency_penalty { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public float? presence_penalty { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public int? max_tokens { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public int? seed { get; set; }
        }

        public class Req
        {
            public string? custom_id { get; set; }
            public string? method { get; set; }
            public string? url { get; set; }
            public Body body { get; set; }
        }
    }
}
