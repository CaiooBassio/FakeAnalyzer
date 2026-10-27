# InstaFakeAnalyzer

## 📜 Descrição

O **InstaFakeAnalyzer** é uma aplicação desktop desenvolvida em C# com WinForms/WPF que se conecta a uma API REST para
combater a disseminação de fake news. O sistema permite que usuários:

- ✅ **Cadastrem novas notícias** para análise.
- 🔍 **Validem notícias existentes** no banco de dados, marcando como verdadeiras ou falsas, com justificativas.
- 🤖 **Consultem um chatbot com inteligência artificial**, que analisa uma notícia e retorna uma avaliação automática,
  além de uma justificativa baseada em IA.

O projeto promove o uso de inteligência artificial aliada à curadoria humana para verificação de informações.

---

## 🏗️ Tecnologias Utilizadas

- 💻 **C# (.NET) — WinForms / WPF**
- 🧠 **Inteligência Artificial via Endpoint de IA**
- 🔧 **HTTPClient** para comunicação com backend
- ♻️ **Injeção de Dependência (DI)**

---

## 🔗 Funcionalidades Principais

### 📥 Cadastro de Notícias

- Permite inserir novas notícias no banco.
- As notícias ficam com status **"Não Verificada"** até serem avaliadas.

### ✅ Verificação Manual de Notícias

- Exibe uma notícia aleatória ainda não verificada.
- Usuário Verificador pode:
    - Marcar como **"Fake News"** ou **"Verdadeira"**.
    - Inserir uma **justificativa** ou alterar a da IA sobre o motivo da validação.
- Após salvar, uma nova notícia é carregada para continuar o processo.

### 🤖 Consulta à IA (Chatbot)

- Envia uma notícia para um modelo de IA treinado para avaliar seu conteúdo.
- A IA retorna:
    - Se a notícia parece **falsa** ou **verdadeira**.
    - Uma **justificativa textual** explicando sua análise.
- A nova pergunta(noticia) e justificativa da IA são salvas no banco e marcadas para analise dos verificadores.

---

## 🚀 Como Executar o Projeto

### É NECESSÁRIO TER A API RODANDO PARA O FUNCIONAMENTO DO CLIENT, CASO SEJA LOCALMENTE A APIURL DEVERÁ SER
`http://localhost:5170/api` (Ajustar a porta conforme necessário)

### 🔧 Client (Interface)

1. Clone o repositório da API.
2. Configure o `appsettings.json` com os dados da api (base url e apiurl).
3. Build o Projeto
4. Execute ou Debug