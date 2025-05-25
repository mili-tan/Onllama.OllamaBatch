using OllamaSharp;
using OllamaSharp.Models.Chat;
using System.Collections.Concurrent;
using System.Text.Json;
using McMaster.Extensions.CommandLineUtils;

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

            cmd.OnExecute(() =>
            {
                if (inputOption.HasValue()) InputFile = inputOption.ParsedValue;
                if (outputOption.HasValue()) OutputFile = outputOption.ParsedValue;
                if (modelOption.HasValue()) ModelName = modelOption.ParsedValue;
                if (skipOption.HasValue()) Skip = skipOption.ParsedValue;
                if (noThinkOption.HasValue()) NoThink = noThinkOption.ParsedValue;
                if (trimThinkOption.HasValue()) TrimThink = trimThinkOption.ParsedValue;
                if (urlOption.HasValue()) httpClient.BaseAddress = new Uri(urlOption.ParsedValue);

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

                    var chat = new ChatRequest()
                    {
                        Model = (string.IsNullOrWhiteSpace(ModelName) ? req.body.model : ModelName) ?? "",
                        Messages = req?.body.messages, Stream = false, KeepAlive = "-1s"
                    };
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await foreach (var item in client.ChatAsync(chat))
                            {
                                if (TrimThink)
                                    item.Message.Content =
                                        item.Message.Content?.Split("</think>").LastOrDefault()?.Trim();

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
