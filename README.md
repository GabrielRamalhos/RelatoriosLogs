# Relatórios de Logs - Versão C#

Esta é a versão C# do projeto de relatórios de logs, mantendo a mesma funcionalidade da versão Python original.

## 📋 Pré-requisitos

- **.NET 8.0 SDK** ou superior
- Visual Studio 2022, VS Code ou qualquer editor que suporte C#

## 🚀 Como Executar

### 1. Instalar dependências
```bash
dotnet restore
```

### 2. Executar o programa
```bash
dotnet run
```

### 3. Compilar para distribuição
```bash
dotnet build --configuration Release
```

## 📁 Estrutura do Projeto

```
RelatoriosLogs/
├── Program.cs                 # Arquivo principal (substitui relatorios_logs.py)
├── RelatoriosLogs.csproj     # Arquivo de projeto C#
├── templates/
│   └── RelatorioLogs.html    # Template HTML (mantido igual)
├── images/                   # Imagens (mantidas iguais)
├── relatorios/              # Pasta de saída (mantida igual)
└── README_CSharp.md         # Este arquivo
```

## 🔄 Migração da Versão Python

### O que foi migrado:
- ✅ **Lógica de negócio**: Consulta à API, processamento de dados
- ✅ **Manipulação de HTML**: Parsing e modificação de templates
- ✅ **Configurações**: URLs, tokens, caminhos de arquivos
- ✅ **Funcionalidades**: Agrupamento de métodos, extração de nomes, resumo de mensagens

### Principais mudanças:
- **Linguagem**: Python → C#
- **Bibliotecas**:
  - `requests` → `HttpClient`
  - `beautifulsoup4` → `HtmlAgilityPack`
  - `json` → `System.Text.Json`
- **Estrutura**: Código orientado a objetos com classes para deserialização JSON

### Funcionalidades mantidas:
- 🌐 Consulta à API com autenticação Bearer
- 📊 Processamento e agrupamento de dados
- 🎨 Manipulação de templates HTML
- 📝 Geração de relatórios atualizados
- ➕ Adição de novos métodos na seção "NEW"

## 🛠️ Dependências

- **HtmlAgilityPack**: Para manipulação de HTML (equivalente ao BeautifulSoup)
- **Newtonsoft.Json**: Para deserialização JSON (opcional, usando System.Text.Json nativo)

## 📝 Notas

- O programa mantém exatamente a mesma funcionalidade da versão Python
- Todos os arquivos de template e imagens são reutilizados
- A saída é idêntica à versão original
- O código foi adaptado seguindo as melhores práticas do C#

## 🔧 Configuração

As configurações estão no início do arquivo `Program.cs`:
- `BEARER_TOKEN`: Token de autenticação
- `API_URL`: URL da API
- `TEMPLATE_PATH`: Caminho do template HTML
- `SAIDA_PATH`: Caminho de saída do relatório

## 🚀 Execução

Após configurar o ambiente, execute:
```bash
dotnet run
```

O programa irá:
1. Consultar a API
2. Processar os dados
3. Atualizar o template HTML
4. Salvar o relatório em `relatorios/RelatorioLogs_atualizado.html`
