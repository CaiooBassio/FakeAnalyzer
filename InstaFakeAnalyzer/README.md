# InstaFakeAnalyzer

## 📜 Descrição

O **InstaFakeAnalyzer** é uma aplicação desktop desenvolvida em C# com WinForms/WPF que se conecta a uma API REST para combater a disseminação de fake news. O sistema permite que usuários:

-   ✅ **Cadastrem novas notícias** para análise.
-   🔍 **Validem notícias existentes** no banco de dados, marcando como verdadeiras ou falsas, com justificativas.
-   🤖 **Consultem um chatbot com inteligência artificial**, que analisa uma notícia e retorna uma avaliação automática, além de uma justificativa baseada em IA.

O projeto promove o uso de inteligência artificial aliada à curadoria humana para verificação de informações.

---

## 🏗️ Tecnologias Utilizadas

-   💻 **C# (.NET) — WinForms / WPF**
-   🧠 **Inteligência Artificial via Endpoint de IA**
-   🔧 **HTTPClient** para comunicação com backend
-   ♻️ **Injeção de Dependência (DI)**

---

## 🔗 Funcionalidades Principais

### 📥 Cadastro de Notícias

-   Permite inserir novas notícias no banco.
-   As notícias ficam com status **"Não Verificada"** até serem avaliadas.

### ✅ Verificação Manual de Notícias

-   Exibe uma notícia aleatória ainda não verificada.
-   Usuário Verificador pode:
    -   Marcar como **"Fake News"** ou **"Verdadeira"**.
    -   Inserir uma **justificativa** ou alterar a da IA sobre o motivo da validação.
-   Após salvar, uma nova notícia é carregada para continuar o processo.

### 🤖 Consulta à IA (Chatbot)

-   Envia uma notícia para um modelo de IA treinado para avaliar seu conteúdo.
-   A IA retorna:
    -   Se a notícia parece **falsa** ou **verdadeira**.
    -   Uma **justificativa textual** explicando sua análise.
-   A nova pergunta(noticia) e justificativa da IA são salvas no banco e marcadas para analise dos verificadores.

---

## 🚀 Como Executar o Projeto

### É NECESSÁRIO TER A API RODANDO PARA O FUNCIONAMENTO DO CLIENT, CASO SEJA LOCALMENTE A APIURL DEVERÁ SER

`http://localhost:5170/api` (Ajustar a porta conforme necessário)

### 🔧 Client (Interface)

1.  Clone o repositório da API.
2.  Configure o `appsettings.json` com os dados da api (base url e apiurl).
3.  Build o Projeto
4.  Execute ou Debug

### Configurar o provedor de IA da API

A API escolhe primeiro o provedor em `AI:Provider` e tenta, em ordem, os provedores listados em `AI:FallbackProviders` se houver falha transitória. Exemplo: Gemini como principal e DeepSeek como alternativa:

```json
"AI": {
  "Provider": "Gemini",
  "FallbackProviders": ["DeepSeek"]
}
```

Para usar o Gemini, configure `AI:Gemini:Model` (por padrão, `gemini-flash-latest`) e defina a chave fora do arquivo de configuração, na variável de ambiente `AI__Gemini__ApiKey`. Feche e abra novamente o terminal depois de criar uma variável de ambiente do Windows. O DeepSeek via OpenRouter precisa da própria chave em `AI__DeepSeek__ApiKey`; a chave do Gemini não serve para ele. O fallback ocorre para indisponibilidade HTTP (429/5xx), timeout, falha de rede ou resposta JSON inválida. Erros de configuração e autenticação não fazem fallback, para não mascarar credenciais incorretas.

Para executar a API, abra um terminal na pasta `InstaFakeAnalyzer` e rode `dotnet run`. Para voltar ao DeepSeek, altere `AI:Provider` para `DeepSeek`; a chave correspondente deve estar configurada separadamente.

### Eventos repetidos do Instagram

O webhook deduplica eventos usando o ID `message.mid`, mantendo até 10.000 IDs concluídos por 24 horas na memória do processo. Isso impede chamadas duplicadas ao analyzer quando o Instagram reenvia a mesma mensagem ou a entrega em formatos diferentes. A lista em memória é reiniciada quando a API reinicia; eventos sem `message.mid` não podem ser deduplicados.

Após a confirmação inicial ao usuário, as análises são enfileiradas e processadas em segundo plano; o webhook não fica aguardando o Gemini antes de responder à Meta. A fila comporta até 100 mensagens em memória e é perdida se o processo da API for encerrado antes do processamento. Se a análise falhar, o bot envia uma mensagem informando que o serviço está indisponível e que o usuário pode tentar novamente.

Se o Gemini devolver JSON truncado ou inválido, o analyzer repete a solicitação uma vez com um limite de saída maior. Se continuar inválido, a API retorna HTTP 502 e não salva uma análise como se tivesse sido concluída. Erros temporários do Gemini (HTTP 429, 500, 502, 503 ou 504) recebem uma repetição automática, respeitando o `Retry-After` quando enviado (com espera limitada a 5 segundos). Se o provedor continuar indisponível, a API retorna erro e a análise não é gravada como concluída.

Os timeouts dos provedores são limitados para permitir o failover antes que a chamada interna do webhook expire:
20 segundos por tentativa do Gemini, 30 segundos para DeepSeek e 90 segundos para a API interna.

Antes de chamar o provedor principal ou qualquer fallback, a API pesquisa a alegação na Web usando a Brave Search API,
independente dos provedores de IA, e passa os trechos de até cinco resultados e suas referências a todos os analyzers.
A resposta enviada ao Instagram contém somente a síntese, sem lista de links. Se a pesquisa não retornar fontes
utilizáveis, os analyzers recebem instrução explícita para não afirmar que a alegação foi confirmada ou desmentida.
Configure uma chave própria da Brave Search API na variável de ambiente `AI__Research__ApiKey`; a chave do Gemini ou
OpenRouter não serve para a busca. Região, idioma e endpoint podem ser ajustados em `AI:Research:Country`,
`AI:Research:Language` e `AI:Research:BaseUrl`. Isso evita consumir a cota do Gemini para pesquisar, embora a análise
de IA ainda use as chaves e cotas configuradas para Gemini/DeepSeek.