using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using HtmlAgilityPack;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using DotNetEnv;
namespace RelatoriosLogs
{
    public class Program
    {
        // ========== Configurações ==========
        // Data específica para buscar os logs (07/08/2025)
        private static readonly DateTime DataInicio = new DateTime(2025, 8, 7, 0, 0, 0);
        private static readonly DateTime DataFim = new DateTime(2025, 8, 7, 23, 59, 59);
        
        private static string API_URL => $"https://api-jobs.retornar.com.br/v1/Log/logs/agrupados?dataInicio={DataInicio:yyyy-MM-ddTHH:mm:ss}&dataFim={DataFim:yyyy-MM-ddTHH:mm:ss}&incluirDescricoes=true";
        //private const string API_URL = "https://backend-api-develop.retornar.com.br/v1/Log/logs/agrupados?dataInicio={DataInicio:yyyy-MM-ddTHH:mm:ss}&dataFim={DataFim:yyyy-MM-ddTHH:mm:ss}&incluirDescricoes=true";
        private const string TEMPLATE_PATH = "templates/RelatorioLogs.html";
        private const string SAIDA_PATH = "relatorios/RelatorioLogs_atualizado.html";

        private static readonly HttpClient httpClient = new HttpClient();

        static Program()
        {
            // Carrega as variáveis de ambiente do arquivo .env
            Env.Load();
            
            // Obtém o token do arquivo .env
            var bearerToken = Environment.GetEnvironmentVariable("RETORNAR_API_TOKEN");
            if (string.IsNullOrEmpty(bearerToken))
            {
                Console.WriteLine("❌ Token não encontrado no arquivo .env");
                Console.WriteLine("📝 Crie um arquivo .env na raiz do projeto com:");
                Console.WriteLine("   RETORNAR_API_TOKEN=seu_token_aqui");
                Console.WriteLine("🔑 Obtenha o token válido no sistema Retornar");
                throw new Exception("Token não configurado. Verifique o arquivo .env");
            }
            
            // Verifica se o token parece válido (formato JWT)
            if (!bearerToken.Contains(".") || bearerToken.Split('.').Length != 3)
            {
                Console.WriteLine("⚠️ Token parece estar em formato inválido");
                Console.WriteLine("🔑 O token deve estar no formato JWT válido");
            }
            
            Console.WriteLine($"🔑 Token carregado: {bearerToken.Substring(0, Math.Min(20, bearerToken.Length))}...");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("🔍 Gerando relatório de logs...");
            
            try
            {
                var dadosApi = await ObterDadosApi();
                if (dadosApi == null || !dadosApi.Any())
                {
                    Console.WriteLine("ℹ️ Nenhum dado retornado pela API. Relatório não será modificado.");
                    return;
                }

                var soup = CarregarHtml();
                AtualizarHtml(soup, dadosApi);
                SalvarHtml(soup);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro: {ex.Message}");
            }
        }

        private static HtmlDocument CarregarHtml()
        {
            var doc = new HtmlDocument();
            doc.Load(TEMPLATE_PATH, Encoding.UTF8);
            return doc;
        }

        private static void SalvarHtml(HtmlDocument soup)
        {
            var directory = Path.GetDirectoryName(SAIDA_PATH);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            soup.Save(SAIDA_PATH, Encoding.UTF8);
            Console.WriteLine($"✅ Relatório salvo em: {SAIDA_PATH}");
        }

        private static async Task<List<ApiResponse>> ObterDadosApi()
        {
            Console.WriteLine($"🌐 Consultando API para logs de {DataInicio:dd/MM/yyyy}...");
            
            var response = await httpClient.GetAsync(API_URL);
            Console.WriteLine($"🔁 Status: {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"📄 Resposta completa da API: {content}");
                throw new Exception($"❌ API retornou status {response.StatusCode}: {content}");
            }

            try
            {
                Console.WriteLine($"📄 Resposta bruta da API (primeiros 500 chars): {content.Substring(0, Math.Min(500, content.Length))}...");
                
                var data = JsonSerializer.Deserialize<ApiData>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data?.Success != true)
                {
                    Console.WriteLine($"⚠️ Aviso da API: {data?.Message ?? "Erro desconhecido"}");
                    return new List<ApiResponse>();
                }

                if (data.Content == null || !data.Content.Any())
                {
                    throw new Exception("❌ Estrutura inesperada da resposta. Campo 'content' ausente ou inválido.");
                }

                Console.WriteLine($"✅ {data.Content.Count} tipos de eventos carregados.");
                
                // Debug: Mostrar estrutura dos dados
                foreach (var item in data.Content)
                {
                    Console.WriteLine($"📊 Tipo Evento: {item.TipoEvento}");
                    foreach (var metodo in item.Metodos)
                    {
                        Console.WriteLine($"   📋 Método: '{metodo.NomeMetodo}' | Qtd: {metodo.Quantidade} | Msg: '{metodo.MensagemBase}'");
                    }
                }
                
                return AgruparMetodos(data.Content);
            }
            catch (JsonException ex)
            {
                throw new Exception($"❌ Erro ao interpretar resposta JSON: {ex.Message}");
            }
        }

        private static List<ApiResponse> AgruparMetodos(List<ApiResponse> dados)
        {
            var agrupado = new Dictionary<string, MetodoInfo>();

            foreach (var item in dados)
            {
                var tipo = item.TipoEvento;
                foreach (var metodo in item.Metodos)
                {
                    var chave = $"{tipo}_{metodo.NomeMetodo}";
                    
                    if (!agrupado.ContainsKey(chave))
                    {
                        agrupado[chave] = new MetodoInfo
                        {
                            Quantidade = 0,
                            MensagemBase = null
                        };
                    }

                    agrupado[chave].Quantidade += metodo.Quantidade;
                    
                    // Usa a primeira descrição se MensagemBase estiver vazia
                    var mensagemParaUsar = metodo.MensagemBase;
                    if (string.IsNullOrEmpty(mensagemParaUsar) && metodo.Descricoes != null && metodo.Descricoes.Any())
                    {
                        mensagemParaUsar = metodo.Descricoes.First();
                    }
                    
                    if (string.IsNullOrEmpty(agrupado[chave].MensagemBase) && !string.IsNullOrEmpty(mensagemParaUsar))
                    {
                        agrupado[chave].MensagemBase = mensagemParaUsar;
                    }
                }
            }

            var resultado = new Dictionary<string, List<Metodo>>();
            
            foreach (var kvp in agrupado)
            {
                var partes = kvp.Key.Split('_', 2);
                var tipo = partes[0];
                var metodo = partes[1];

                if (!resultado.ContainsKey(tipo))
                {
                    resultado[tipo] = new List<Metodo>();
                }

                resultado[tipo].Add(new Metodo
                {
                    NomeMetodo = metodo,
                    Quantidade = kvp.Value.Quantidade,
                    MensagemBase = kvp.Value.MensagemBase
                });
            }

            return resultado.Select(kvp => new ApiResponse
            {
                TipoEvento = kvp.Key,
                Metodos = kvp.Value
            }).ToList();
        }

        private static string ExtrairNomeMetodo(string nomeCompleto)
        {
            if (string.IsNullOrEmpty(nomeCompleto))
            {
                return "Método Desconhecido";
            }
            
            var match = Regex.Match(nomeCompleto, @"<([^>]+)>");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            // Se não encontrar o padrão <nome>, retorna o nome completo
            return nomeCompleto.Trim();
        }

        private static string ResumirMensagem(string? mensagem)
        {
            if (string.IsNullOrEmpty(mensagem) || mensagem.Trim().ToLower() is "null" or "none")
            {
                return "";
            }
            
            // Pega apenas a primeira linha da mensagem
            var primeiraLinha = mensagem.Split('\n', '\r').FirstOrDefault()?.Trim();
            if (string.IsNullOrEmpty(primeiraLinha))
            {
                return "";
            }
            
            // Remove caracteres especiais e limita o tamanho
            var mensagemLimpa = primeiraLinha
                .Replace("\t", " ")
                .Replace("  ", " ") // Remove espaços duplos
                .Trim();
            
            // Limita a 200 caracteres para não ficar muito longo
            if (mensagemLimpa.Length > 200)
            {
                mensagemLimpa = mensagemLimpa.Substring(0, 197) + "...";
            }
            
            return mensagemLimpa;
        }

        private static void AtualizarHtml(HtmlDocument soup, List<ApiResponse> dadosApi)
        {
            var novosMetodos = new List<NovoMetodo>();
            int totalLogs = 0;

            var containerDiv = soup.DocumentNode.SelectSingleNode("//div[@class='container']");
            if (containerDiv == null)
            {
                Console.WriteLine("❌ Não foi encontrada a <div class='container'> no HTML.");
                return;
            }

            var tableContainer = containerDiv.SelectSingleNode(".//div[@class='table-container']");
            if (tableContainer == null)
            {
                Console.WriteLine("❌ Não foi encontrada a <div class='table-container'> no HTML.");
                return;
            }

            var tbody = tableContainer.SelectSingleNode(".//tbody");
            if (tbody == null)
            {
                Console.WriteLine("❌ Não foi encontrada a <tbody> na tabela existente.");
                return;
            }

            foreach (var tipoEvento in dadosApi)
            {
                var tipo = tipoEvento.TipoEvento;
                foreach (var metodo in tipoEvento.Metodos)
                {
                    var nomeCompleto = metodo.NomeMetodo;
                    var nomeFormatado = ExtrairNomeMetodo(nomeCompleto);
                    var qtdLogs = metodo.Quantidade;
                    
                    // Usa a primeira descrição se MensagemBase estiver vazia
                    var mensagemBase = ResumirMensagem(metodo.MensagemBase);
                    if (string.IsNullOrEmpty(mensagemBase) && metodo.Descricoes != null && metodo.Descricoes.Any())
                    {
                        mensagemBase = ResumirMensagem(metodo.Descricoes.First());
                    }
                    
                    // Adiciona ao total de logs
                    totalLogs += qtdLogs;
                    
                    Console.WriteLine($"🔍 Debug - Nome completo: '{nomeCompleto}' | Formatado: '{nomeFormatado}' | Msg: '{mensagemBase}'");

                    var tdMetodo = soup.DocumentNode.SelectSingleNode($"//td[text()='{nomeFormatado}']");
                    if (tdMetodo != null)
                    {
                        var linha = tdMetodo.ParentNode;
                        var tds = linha.SelectNodes(".//td");
                        if (tds != null && tds.Count >= 4)
                        {
                            tds[1].InnerHtml = tipo;
                            tds[2].InnerHtml = mensagemBase;
                            tds[3].InnerHtml = qtdLogs.ToString();
                        }
                    }
                    else
                    {
                        novosMetodos.Add(new NovoMetodo
                        {
                            MetodoCompleto = nomeCompleto,
                            MetodoFormatado = nomeFormatado,
                            Tipo = tipo,
                            MensagemBase = mensagemBase,
                            Quantidade = qtdLogs
                        });
                    }
                }
            }

            if (novosMetodos.Any())
            {
                Console.WriteLine($"➕ Adicionando {novosMetodos.Count} métodos novos à tabela 'NEW'...");

                var divNewContainer = soup.CreateElement("div");
                divNewContainer.SetAttributeValue("class", "table-container");

                var spanNew = soup.CreateElement("span");
                spanNew.SetAttributeValue("class", "category-title");

                var imgTag = soup.CreateElement("img");
                imgTag.SetAttributeValue("src", "../images/new.svg");
                imgTag.SetAttributeValue("alt", "New");
                spanNew.AppendChild(imgTag);

                var textNode = soup.CreateTextNode(" New");
                spanNew.AppendChild(textNode);

                divNewContainer.AppendChild(spanNew);

                var tabelaNew = soup.CreateElement("table");
                var colgroup = soup.CreateElement("colgroup");
                
                var col1 = soup.CreateElement("col");
                col1.SetAttributeValue("style", "width: 38%");
                colgroup.AppendChild(col1);
                
                var col2 = soup.CreateElement("col");
                col2.SetAttributeValue("style", "width: 9%");
                colgroup.AppendChild(col2);
                
                var col3 = soup.CreateElement("col");
                col3.SetAttributeValue("style", "width: 45%");
                colgroup.AppendChild(col3);
                
                var col4 = soup.CreateElement("col");
                col4.SetAttributeValue("style", "width: 8%");
                colgroup.AppendChild(col4);
                
                tabelaNew.AppendChild(colgroup);

                var thead = soup.CreateElement("thead");
                var trHead = soup.CreateElement("tr");
                
                var titulos = new[] { "Método", "Tipo Evento", "Mensagem Base", "Quantidade Logs" };
                foreach (var titulo in titulos)
                {
                    var th = soup.CreateElement("th");
                    th.InnerHtml = titulo;
                    trHead.AppendChild(th);
                }
                
                thead.AppendChild(trHead);
                tabelaNew.AppendChild(thead);

                var tbodyNew = soup.CreateElement("tbody");
                tabelaNew.AppendChild(tbodyNew);
                divNewContainer.AppendChild(tabelaNew);

                foreach (var item in novosMetodos)
                {
                    var novaLinha = soup.CreateElement("tr");

                    var tdMetodo = soup.CreateElement("td");
                    tdMetodo.InnerHtml = item.MetodoFormatado;
                    novaLinha.AppendChild(tdMetodo);

                    var tdTipo = soup.CreateElement("td");
                    tdTipo.InnerHtml = item.Tipo;
                    novaLinha.AppendChild(tdTipo);

                    var tdMsg = soup.CreateElement("td");
                    tdMsg.InnerHtml = item.MensagemBase;
                    novaLinha.AppendChild(tdMsg);

                    var tdQtd = soup.CreateElement("td");
                    tdQtd.InnerHtml = item.Quantidade.ToString();
                    novaLinha.AppendChild(tdQtd);

                    tbodyNew.AppendChild(novaLinha);
                    Console.WriteLine($"   ➕ Método novo: {item.MetodoFormatado}");
                }

                containerDiv.AppendChild(divNewContainer);
            }

            // Adiciona informações de total e data de atualização
            AdicionarInformacoesFinais(soup, totalLogs);
        }

        private static void AdicionarInformacoesFinais(HtmlDocument soup, int totalLogs)
        {
            var containerDiv = soup.DocumentNode.SelectSingleNode("//div[@class='container']");
            if (containerDiv == null) return;

            // Cria div para informações finais
            var divInfoFinal = soup.CreateElement("div");
            divInfoFinal.SetAttributeValue("style", "margin-top: 40px; padding: 20px; background-color: #f8fafc; border-radius: 8px; border-left: 4px solid #0ea5e9;");

            // Adiciona total de logs
            var divTotal = soup.CreateElement("div");
            divTotal.SetAttributeValue("style", "margin-bottom: 10px;");
            divTotal.InnerHtml = $"<strong>📊 Total de Logs Processados:</strong> {totalLogs:N0}";
            divInfoFinal.AppendChild(divTotal);

            // Adiciona data de atualização
            var dataAtualizacao = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            var divData = soup.CreateElement("div");
            divData.SetAttributeValue("style", "color: #64748b; font-size: 0.9rem;");
            divData.InnerHtml = $"<strong>🕒 Última Atualização:</strong> {dataAtualizacao}";
            divInfoFinal.AppendChild(divData);

            containerDiv.AppendChild(divInfoFinal);

            Console.WriteLine($"📊 Total de logs processados: {totalLogs:N0}");
            Console.WriteLine($"🕒 Data de atualização: {dataAtualizacao}");
        }
    }

    // Classes para deserialização JSON
    public class ApiData
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<ApiResponse>? Content { get; set; }
    }

    public class ApiResponse
    {
        public string TipoEvento { get; set; } = "";
        public List<Metodo> Metodos { get; set; } = new();
    }

    public class Metodo
    {
        [JsonPropertyName("metodo")]
        public string NomeMetodo { get; set; } = "";
        public int Quantidade { get; set; }
        public string? MensagemBase { get; set; }
        [JsonPropertyName("descricoes")]
        public List<string>? Descricoes { get; set; }
    }

    public class MetodoInfo
    {
        public int Quantidade { get; set; }
        public string? MensagemBase { get; set; }
    }

    public class NovoMetodo
    {
        public string MetodoCompleto { get; set; } = "";
        public string MetodoFormatado { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string MensagemBase { get; set; } = "";
        public int Quantidade { get; set; }
    }
}
