namespace AssistenteIA.Models;

public enum ProvedorIA
{
    GitHubModels,
    Gemini,
    OpenAI,
    Ollama
}

/// <summary>
/// Opção exibida no ComboBox de provedores.
/// Os três provedores de nuvem falam o mesmo "idioma" (API compatível com a OpenAI);
/// só mudam o endereço (Endpoint), a chave e o nome do modelo.
/// </summary>
public sealed record OpcaoProvedor(
    ProvedorIA Tipo,
    string Nome,
    string ModeloPadrao,
    string Descricao,
    string? Endpoint = null,
    string? VariavelDeAmbienteChave = null,
    string? ComoObterChave = null)
{
    public bool PrecisaDeChave => VariavelDeAmbienteChave is not null;

    public static IReadOnlyList<OpcaoProvedor> Todas { get; } =
    [
        new(ProvedorIA.GitHubModels,
            "Nuvem · GitHub Models (grátis)",
            "openai/gpt-4o-mini",
            "Gratuito para qualquer conta do GitHub, com limite diário de uso. Não precisa instalar nada.",
            Endpoint: "https://models.github.ai/inference/",
            VariavelDeAmbienteChave: "GITHUB_TOKEN",
            ComoObterChave: "GitHub → Settings → Developer settings → Personal access tokens → Fine-grained tokens → " +
                            "Generate new token → em Permissions, adicione \"Models\" (Read-only)."),

        new(ProvedorIA.Gemini,
            "Nuvem · Google Gemini (grátis)",
            "gemini-3.5-flash",
            "Plano gratuito com limite de requisições por minuto e por dia. Não precisa instalar nada.",
            Endpoint: "https://generativelanguage.googleapis.com/v1beta/openai/",
            VariavelDeAmbienteChave: "GEMINI_API_KEY",
            ComoObterChave: "Entre em aistudio.google.com/apikey com uma conta Google e clique em \"Create API key\"."),

        new(ProvedorIA.OpenAI,
            "Nuvem · OpenAI (pago)",
            "gpt-4o-mini",
            "Modelos da OpenAI direto da fonte. Exige créditos pagos na conta.",
            VariavelDeAmbienteChave: "OPENAI_API_KEY",
            ComoObterChave: "Crie a chave em platform.openai.com/api-keys (é preciso ter créditos)."),

        new(ProvedorIA.Ollama,
            "Local · Ollama",
            "llama3.2",
            "Roda no próprio PC: gratuito, offline e os dados não saem da máquina. Exige o Ollama instalado.")
    ];
}
