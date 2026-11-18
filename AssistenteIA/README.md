# Assistente IA: Integração de IA em aplicações desktop .NET

Projeto de demonstração do seminário **"Integração de Inteligência Artificial em aplicações desktop .NET"**
(Ciência da Computação, UVV).

É um app **WPF (.NET 10)** com um assistente de IA que:

- conversa em **streaming** (o texto aparece aos poucos, sem travar a janela);
- funciona com **IA local (Ollama)** ou **IA na nuvem (OpenAI)**, trocando só uma opção na tela;
- **resume arquivos .txt**, e depois você pode fazer perguntas sobre eles;
- usa **function calling**: a IA chama métodos C# do próprio app (estoque, data/hora);
- permite **cancelar** a resposta no meio.

---

## 1. Requisitos

| Item | Observação |
|---|---|
| Windows 10/11 | O WPF só roda no Windows |
| Visual Studio 2026 (ou .NET 10 SDK) | Carga de trabalho "Desenvolvimento para desktop com .NET" |
| Uma chave **gratuita** (GitHub ou Google) | **Não é preciso instalar nada**, ideal para o computador da faculdade |
| [Ollama](https://ollama.com/download) *(opcional)* | Só para demonstrar a IA local, num PC onde dê para instalar |

> **O PC não tem o .NET 10?** No `AssistenteIA.csproj`, troque `net10.0-windows` por `net8.0-windows`.
> O código funciona igual.

## 2. Como rodar

### Opção A: GitHub Models (grátis, recomendado)
1. No GitHub: **Settings → Developer settings → Personal access tokens → Fine-grained tokens → Generate new token**.
2. Em **Permissions**, adicione **Models** com acesso **Read-only** e gere o token (começa com `github_pat_`).
3. Abra `AssistenteIA.sln` no Visual Studio e aperte **F5**.
4. Com o provedor "GitHub Models" selecionado, cole o token no campo **Chave de API** e converse.

### Opção B: Google Gemini (grátis)
1. Entre em **aistudio.google.com/apikey** com uma conta Google e clique em **Create API key**.
2. No app, escolha "Google Gemini" e cole a chave.

### Opção C: IA local com Ollama (em casa)
1. Instale o Ollama e rode `ollama pull llama3.2`.
2. No app, escolha "Local · Ollama". Não precisa de chave.

### Sobre as chaves
- A chave digitada no app **fica só na memória**: não é salva em arquivo.
- Para não precisar digitar toda vez (no seu PC), defina a variável de ambiente e reabra o Visual Studio:
  `setx GITHUB_TOKEN "sua-chave"` (ou `GEMINI_API_KEY` / `OPENAI_API_KEY`).
- ⚠️ Nunca coloque a chave no código nem a envie para o GitHub. Se vazar, revogue-a e gere outra.
- Os planos gratuitos têm **limite de requisições**. Se aparecer erro de limite, espere um pouco ou troque de provedor.

---

## 3. Estrutura do projeto

```
AssistenteIA/
├── App.xaml(.cs)              → Inicialização e montagem das dependências
├── MainWindow.xaml(.cs)       → Interface (View); o code-behind só cuida de rolagem e foco
├── ViewModels/
│   └── MainViewModel.cs       → Lógica da tela (MVVM): comandos, estados, tratamento de erros
├── Services/
│   ├── ChatClientFactory.cs   → Cria o IChatClient (Ollama ou OpenAI)  ★ abstração
│   ├── AssistenteService.cs   → Histórico da conversa + streaming       ★ uso da IA
│   └── IServicoDialogo.cs     → Abstração da janela "Abrir arquivo"
├── Ferramentas/
│   └── FerramentasApp.cs      → Métodos que a IA pode chamar             ★ function calling
├── Models/                    → MensagemChat, Produto, OpcaoProvedor
└── Infra/                     → ObservableObject e RelayCommand (base do MVVM)
```

## 4. Conceitos do seminário e onde estão no código

| Conceito | Onde ver |
|---|---|
| **Abstração `IChatClient`** (trocar de provedor sem mudar o resto do código) | `ChatClientFactory.Criar` |
| **IA local vs nuvem** | `OpcaoProvedor.cs` e `ChatClientFactory.cs` |
| **APIs compatíveis com a OpenAI** (mesmo código para GitHub, Gemini e OpenAI, mudando só o endereço) | `ChatClientFactory.CriarClienteCompativelComOpenAI` |
| **Pipeline/middleware** (padrão Decorator) | `ChatClientBuilder(...).UseFunctionInvocation()` |
| **System prompt** | `AssistenteService.PromptDeSistema` |
| **Histórico/contexto** (o LLM não tem memória) | `AssistenteService._historico` |
| **Streaming + async/await** (UI não trava) | `GetStreamingResponseAsync` + `await foreach` no ViewModel |
| **Function calling** | `FerramentasApp` (`[Description]` + `AIFunctionFactory.Create`) |
| **Engenharia de prompt** e **limite de contexto** | `MainViewModel.ResumirArquivoAsync` |
| **Cancelamento** | `CancellationToken` e botão "Parar" |
| **Segurança da chave** | variável de ambiente `OPENAI_API_KEY` |
| **Threads na UI** | `Dispatcher.InvokeAsync` em `AoExecutarFerramenta` |

## 5. Roteiro sugerido para a demonstração no vídeo

1. **Conversa simples:** "Explique o que é um LLM em 3 frases". Mostre o streaming.
2. **Function calling:** pergunte "Quantos mouses temos no estoque?" e "Que dia é hoje?".
   Aparece um aviso amarelo 🔧 mostrando qual método C# a IA chamou.
3. **Sem ferramentas:** desmarque a opção e pergunte de novo sobre o estoque.
   A IA não sabe a resposta (ou inventa: é uma **alucinação**). Ótimo gancho para a análise crítica.
4. **Resumo:** clique em "Resumir arquivo .txt", escolha `exemplos/texto-exemplo.txt` e depois faça uma pergunta sobre o texto.
5. **Cancelamento:** peça um texto longo e clique em "Parar".
6. **Troca de provedor:** com as duas chaves gratuitas, mude de GitHub Models para Gemini no meio da conversa.
   O histórico continua, porque só a implementação do `IChatClient` mudou.
7. **Erro tratado:** apague um caractere da chave e envie uma mensagem. O app mostra um erro amigável em vez de travar.

## 6. Limitações conhecidas (para a análise crítica)

- Nos planos gratuitos da nuvem, as mensagens são enviadas para servidores de terceiros e há limite de uso.
- Modelos locais pequenos são mais lentos (dependem do hardware) e menos precisos que os da nuvem.
- Modelos pequenos às vezes chamam a ferramenta errada ou não chamam nenhuma.
- O arquivo resumido é cortado em 8.000 caracteres por causa da janela de contexto do modelo.
  Para documentos grandes, o caminho correto seria **RAG** (embeddings + busca vetorial).
- O histórico cresce a cada mensagem: em conversas longas, isso aumenta o custo e o tempo de resposta.

## 7. Pacotes usados

- `Microsoft.Extensions.AI` 10.11.0
- `Microsoft.Extensions.AI.OpenAI` 10.11.0
- `OllamaSharp` 5.5.2
