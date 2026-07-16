export const fallbackModelCatalog: Record<string, string[]> = {
  Gemini: [
    'gemini-3.1-flash-lite',
    'gemini-3.5-flash',
    'gemini-2.5-pro',
  ],
  OpenRouter: [
    'openrouter/free',
    'google/gemma-4-31b-it:free',
    'google/gemma-4-26b-a4b-it:free',
    'qwen/qwen3-coder:free',
  ],
  Groq: [
    'meta-llama/llama-4-scout-17b-16e-instruct',
    'llama-3.1-8b-instant',
    'llama-3.3-70b-versatile',
    'qwen/qwen3-32b',
  ],
}

export const providerDefaults: Record<string, { model: string; baseUrl: string }> = {
  Gemini: { model: 'gemini-3.1-flash-lite', baseUrl: 'https://generativelanguage.googleapis.com/v1beta' },
  OpenRouter: { model: 'openrouter/free', baseUrl: 'https://openrouter.ai/api/v1' },
  Groq: { model: 'meta-llama/llama-4-scout-17b-16e-instruct', baseUrl: 'https://api.groq.com/openai/v1' },
}
