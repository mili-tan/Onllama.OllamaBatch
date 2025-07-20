# OllamaBatch
```
Onllama.OllamaBatch - Simple Ollama batch inference tool. / 简单的 Ollama 批量推理工具。
Copyright (c) 2025 Milkey Tan. Code released under the MIT License

Usage: Onllama.OllamaBatch [options]

Options:
  -?|-he|--help            Show help information.
  -i|--input <path>        输入 Jsonl 路径。 / Set input Jsonl path. (input.jsonl) 
  -o|--output <path>       输出 Jsonl 路径。 / Set output Jsonl path. (output.jsonl)
  -m|--model <name>        覆盖模型名称。 / Set overwrite model name.
  -s|--skip <number>       跳过的请求 ID。 / Set skip request ID
  -nt|--no-think           设置 Qwen3 不思考 / Set qwen3 no think (/no_think)。
  -t|--trim-think          修剪思考过程 / Set trim think (<think>)。
  -u|--url <URL>           Ollama 服务端点。 / Set ollama service URL [http://127.0.0.1:11434]
  --timeout <minutes>      设置超时时间（分钟） / Set timeout minutes。
  --max-parallel <number>  最大并行请求数 / Set max parallel requests。
  --wait-time <seconds>    每批次请求之间的等待时间（秒） / Set wait time between batch requests (seconds)。
  -oai|--use-oai           使用 OpenAI 风格的 API 调用 / Use OpenAI style API call 。
  -ou|--oai-url <url>      OpenAI 风格的 API URL / Set OpenAI style API URL [https://example.com/v1/chat/completions]。
  -ok|--oai-sk <sk>        OpenAI 风格的 API 密钥 / Set OpenAI style API Key [sk-xxxx]。
  -oo|--oai-output         使用 OpenAI 风格的输出格式  / Use OpenAI style output format。
  -wa|--wait-all           等待所有完成再请求下一批次 / Wait for complete before the next batch。
```
