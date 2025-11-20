using McMaster.Extensions.CommandLineUtils;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Onllama.OllamaBatch
{
    internal class Program
    {
        public static HttpClient httpClient = new HttpClient()
            {BaseAddress = new Uri("http://127.0.0.1:11434"), Timeout = TimeSpan.FromMinutes(3)};
        public static OllamaApiClient client = new OllamaApiClient(httpClient);

        public static string OaiStyleUrl = "https://api.deepseek.com/v1/chat/completions";
        public static string OaiStyleSK = "sk-";
        public static bool UseOaiStyleApi = false;
        public static bool UseOaiStyleOutput = false;

        public static string ModelName = string.Empty;
        public static string InputFile = "input.jsonl";
        public static string OutputFile = "output.jsonl";
        public static int Skip = 0;
        public static int MaxParallel = 8;
        public static int WaitTime = 0;
        public static bool NoThink = false;
        public static bool TrimThink = false;
        public static bool WaitAll = false;
        public static WebProxy? MyWebProxy = null;

        static void Main(string[] args)
        {

            //var count = 1;
            //foreach (var line in File.ReadLines(""))
            //{
            //    var requestData = new
            //    {
            //        custom_id = $"request-{count}",
            //        method = "POST",
            //        url = "/v1/chat/completions",
            //        body = new
            //        {
            //            model = "MiniMax-M2",
            //            messages = new object[]
            //            {
            //                new { role = "system", content = "你是一个学术写作者。请重写润色输入，禁止改变原意、解释、Markdown" },
            //                new { role = "user", content = line.Split("\t")[1] }
            //            }
            //        }
            //    };
            //    string jsonLine = JsonConvert.SerializeObject(requestData, Formatting.None) + "\r\n";
            //    // 写入文件
            //    File.AppendAllText("2.jsonl", jsonLine);
            //    count++;
            //}
            //return;

            //var from = 165000;
            //var to = 166500;
            //var now = 0;
            //foreach (var readLine in File.ReadLines(InputFile))
            //{
            //    now++;

            //    if (now <= from)
            //    {
            //        continue;
            //    }
            //    if (now >= to)
            //    {
            //        break;
            //    }

            //    File.AppendAllText(to + ".jsonl", readLine + Environment.NewLine);
            //}
            //return;

            //var jList = new List<JObject>();
            //Parallel.ForEach(File.ReadLines("1.jsonl"), i =>
            //{
            //    var jArray = (JArray)JObject.Parse(i)["body"]["messages"];
            //    var q = jArray.FirstOrDefault(x => x["role"]?.ToString() == "assistant")?["content"]?.ToString() ??
            //            string.Empty;
            //    var a = jArray.FirstOrDefault(x => x["role"]?.ToString() == "user")?["content"]?.ToString() ??
            //            string.Empty;
            //    jList.Add(new JObject
            //    {
            //        ["instruction"] = q,
            //        ["input"] = "",
            //        ["output"] = a
            //    });
            //});
            //File.WriteAllText("1.json",
            //    JArray.FromObject(jList).ToString());
            //return;

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
                (isZh ? "输入 Jsonl 路径。" : "Set input Jsonl path") + " (input.jsonl)",
                CommandOptionType.SingleValue);
            var outputOption = cmd.Option<string>("-o|--output <path>",
                (isZh ? "输出 Jsonl 路径。" : "Set output Jsonl path") + " (output.jsonl)",
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
            var urlOption = cmd.Option<string>("-u|--url <URL>",
                isZh ? "Ollama 服务端点。[http://127.0.0.1:11434]" : "Set ollama service URL [http://127.0.0.1:11434]",
                CommandOptionType.SingleValue);
            var timeOutOption = cmd.Option<int>("--timeout <minutes>",
                isZh ? "设置超时时间（分钟）。" : "Set timeout minutes",
                CommandOptionType.SingleValue);
            var maxParallelOption = cmd.Option<int>("--max-parallel <number>",
                isZh ? "最大并行请求数。" : "Set max parallel requests",
                CommandOptionType.SingleValue);
            var waitTimeOption = cmd.Option<int>("--wait-time <seconds>",
                isZh ? "每批次请求之间的等待时间（秒）。" : "Set wait time between batch requests (seconds)",
                CommandOptionType.SingleValue);
            var useUseOaiStyleOption = cmd.Option<bool>("-oai|--use-oai",
                isZh ? "使用 OpenAI 风格的 API 调用。" : "Use OpenAI style API call",
                CommandOptionType.NoValue);
            var oaiStyleUrlOption = cmd.Option<string>("-ou|--oai-url <url>",
                isZh ? "OpenAI 风格的 API URL。 [https://example.com/v1/chat/completions]" : "Set OpenAI style API URL [https://example.com/v1/chat/completions]",
                CommandOptionType.SingleValue);
            var oaiStyleSkOption = cmd.Option<string>("-ok|--oai-sk <sk>",
                isZh ? "OpenAI 风格的 API 密钥。" : "Set OpenAI style API Key",
                CommandOptionType.SingleValue);
            var useOaiStyleOutputOption = cmd.Option<bool>("-oo|--oai-output",
                isZh ? "使用 OpenAI 风格的输出格式。" : "Use OpenAI style output format",
                CommandOptionType.NoValue);
            var waitAllOption = cmd.Option<bool>("-wa|--wait-all",
                isZh ? "等待所有完成再请求下一批次。" : "Wait for complete before the next batch",
                CommandOptionType.NoValue);
            var proxyOption = cmd.Option<string>("--proxy",
                isZh ? "设置与使用代理 [http://127.0.0.1:7890]" : "Set and use proxy [http://127.0.0.1:7890]",
                CommandOptionType.NoValue);

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
                if (waitTimeOption.HasValue()) WaitTime = waitTimeOption.ParsedValue;

                if (useUseOaiStyleOption.HasValue()) UseOaiStyleApi = useUseOaiStyleOption.ParsedValue;
                if (oaiStyleUrlOption.HasValue()) OaiStyleUrl = oaiStyleUrlOption.ParsedValue;
                if (oaiStyleSkOption.HasValue()) OaiStyleSK = oaiStyleSkOption.ParsedValue;
                if (useOaiStyleOutputOption.HasValue()) UseOaiStyleOutput = useOaiStyleOutputOption.ParsedValue;
                if (waitAllOption.HasValue()) WaitAll = waitAllOption.ParsedValue;
                if (proxyOption.HasValue())
                {
                    MyWebProxy = new WebProxy(proxyOption.ParsedValue);
                    httpClient = new HttpClient(new HttpClientHandler() {Proxy = MyWebProxy, UseProxy = true});
                }

                var lines = File.ReadLines(InputFile);
                var tasks = new List<Task>();
                var answers = new ConcurrentBag<string>();

                foreach (var line in lines)
                {
                    //if (DateTime.Now.Hour == 8)
                    //{
                    //    File.WriteAllText("stop-at.txt", line);
                    //    Environment.Exit(0);
                    //}

                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var req = JsonSerializer.Deserialize<Req>(line);
                    Console.WriteLine("Q:" + req?.custom_id);
                    if (req is {custom_id: not null} && long.Parse(req.custom_id.Split('-').Last()) <= Skip) continue;
                    //if (NoThink) req?.body.messages.Insert(0, new Message(ChatRole.System, "/no_think"));
                    if (!string.IsNullOrWhiteSpace(ModelName)) req.body.model = ModelName;

                    var chat = new ChatRequest()
                    {
                        Model = req?.body.model ?? "",
                        Messages = req?.body.messages, Stream = false, KeepAlive = "-1s",
                        Options = new RequestOptions(),
                        Think = NoThink ? false : null
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
                            if (UseOaiStyleApi)
                            {
                                using var handler = new HttpClientHandler();
                                if (MyWebProxy != null)
                                {
                                    handler.Proxy = MyWebProxy;
                                    handler.UseProxy = true;
                                }

                                using var oaiClient = new HttpClient(handler) {Timeout = TimeSpan.FromMinutes(5)};
                                using var request = new HttpRequestMessage(new HttpMethod("POST"), OaiStyleUrl);
                                request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + OaiStyleSK);
                                request.Content = new StringContent(JsonSerializer.Serialize(req.body));
                                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                                var response = await oaiClient.SendAsync(request);
                                if (response.IsSuccessStatusCode)
                                {
                                    var jObj = JsonNode.Parse((await response.Content.ReadAsStringAsync()).Trim());

                                    if (UseOaiStyleOutput)
                                    {
                                        req.response = new Res();
                                        req.response.body.choices = new List<Choice>();
                                        foreach (var choice in jObj?["choices"]?.AsArray() ?? [])
                                        {
                                            if (!string.IsNullOrWhiteSpace(choice?["message"]?["content"].ToJsonString()))
                                            {
                                                req.response.body.choices.Add(new Choice
                                                {
                                                    message = new Message(ChatRole.Assistant, choice?["message"]?["content"]?.ToString()),
                                                    finish_reason = choice?["finish_reason"]?.ToString(),
                                                    index = choice?["index"]?.GetValue<int>() ?? 0
                                                });
                                            }
                                        }
                                    }
                                    else if (!string.IsNullOrWhiteSpace(jObj?["choices"]?[0]?["message"]?["content"].ToJsonString()))
                                        req.body.messages.Add(new Message(ChatRole.Assistant,
                                            jObj?["choices"]?[0]?["message"]?["content"]?.ToString()));

                                    answers.Add(JsonSerializer.Serialize(req, new JsonSerializerOptions
                                        {
                                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                            WriteIndented = false
                                        }));
                                    Console.WriteLine("R:" + req.custom_id);
                                }
                                else
                                {
                                    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                                }
                            }
                            else
                            {
                                await foreach (var item in client.ChatAsync(chat))
                                {
                                    if (TrimThink)
                                        item.Message.Content =
                                            item.Message.Content?.Split("</think>").LastOrDefault()?.Trim();

                                    if (UseOaiStyleOutput && item is ChatDoneResponseStream done)
                                    {
                                        req.response = new Res();
                                        req.response.body.choices = [
                                            new Choice
                                            {
                                                message = done.Message,
                                                finish_reason = done.DoneReason
                                            }
                                        ];
                                    }
                                    else req.body.messages.Add(item.Message);


                                    answers.Add(JsonSerializer.Serialize(req, new JsonSerializerOptions
                                        {
                                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                            WriteIndented = false
                                        }));
                                    Console.WriteLine("R:" + req.custom_id);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }));

                    if (tasks.Count < MaxParallel) continue;

                    Thread.Sleep(WaitTime == 0 ? 0 : WaitTime * 1000);
                    if (WaitAll) Task.WaitAll(tasks.ToArray());
                    else Task.WaitAny(tasks.ToArray());
                    tasks.RemoveAll(x => x.IsCompleted || x.IsCanceled || x.IsFaulted);

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

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public List<Choice> choices { get; set; }
        }

        public class Req
        {
            public string? custom_id { get; set; }
            public string? method { get; set; }
            public string? url { get; set; }
            public Body body { get; set; }
            
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public Res? response { get; set; }
        }

        public class Res
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public Body body { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public int status_code = 200;
        }

        public class Choice
        {
            public Message message { get; set; }
            public string? finish_reason { get; set; } = "stop";
            public int? index { get; set; } = 0;
        }
    }
}
