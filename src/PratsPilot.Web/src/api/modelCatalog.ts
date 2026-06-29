export const fallbackModelCatalog: Record<string, string[]> = {
  Gemini: [
    'gemini-2.5-flash',
    'gemini-2.5-pro',
    'gemini-2.0-flash',
    'gemini-2.0-flash-lite',
  ],
  OpenRouter: [
    'openrouter/free',
  ],
  Ollama: ['llama3.1', 'llama3.2', 'qwen2.5', 'mistral', 'deepseek-r1'],
}

export const providerDefaults: Record<string, { model: string; baseUrl: string }> = {
  Gemini: { model: 'gemini-2.5-flash', baseUrl: 'https://generativelanguage.googleapis.com/v1beta' },
  OpenRouter: { model: 'openrouter/free', baseUrl: 'https://openrouter.ai/api/v1' },
  Ollama: { model: 'llama3.1', baseUrl: 'http://localhost:11434' },
}
