export const fallbackModelCatalog: Record<string, string[]> = {
  Gemini: [
    'gemini-2.5-flash',
    'gemini-2.5-pro',
    'gemini-2.0-flash',
    'gemini-2.0-flash-lite',
  ],
  OpenRouter: [
    'openrouter/free',
    'google/gemma-4-31b-it:free',
    'google/gemma-4-26b-a4b-it:free',
    'qwen/qwen3-coder:free',
  ],
  Groq: ['llama-3.1-8b-instant', 'llama-3.3-70b-versatile', 'qwen/qwen3-32b'],
}

export const providerDefaults: Record<string, { model: string; baseUrl: string }> = {
  Gemini: { model: 'gemini-2.5-flash', baseUrl: 'https://generativelanguage.googleapis.com/v1beta' },
  OpenRouter: { model: 'openrouter/free', baseUrl: 'https://openrouter.ai/api/v1' },
  Groq: { model: 'llama-3.1-8b-instant', baseUrl: 'https://api.groq.com/openai/v1' },
}
