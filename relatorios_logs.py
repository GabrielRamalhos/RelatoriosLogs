import requests
from bs4 import BeautifulSoup
import os
import re

# ========== Token fixo ==========
BEARER_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6IkdhYnJpZWwgUmFtYWxobyIsIlJldG9ybmFyIjoiVXN1YXJpbyIsImVtYWlsIjoiZ2FicmllbC5yYW1hbGhvc0ByZXRvcm5hci5jb20uYnIiLCJJZFVzdWFyaW8iOiI2MzdmNzE4ZGNjNGU2MzM2Yjk1ZmVhYTUiLCJJZExvamEiOiI1ZjU3OGEzMmI5NGM5MzI1MmIzM2Y5ODAiLCJuYmYiOjE3NTM4NzYzNzcsImV4cCI6MTc1MzkxOTU3NywiaWF0IjoxNzUzODc2Mzc3fQ.kEAQ6GVcNLXPrIEWcXNaLWfLp8z71Dijs4ry3SmKqX8"

# ========== Configurações ==========
API_URL = "https://api-jobs.retornar.com.br/v1/Log/logs/agrupados"
HEADERS = {
    "Authorization": f"Bearer {BEARER_TOKEN}"
}
TEMPLATE_PATH = "templates/RelatorioLogs.html"
SAIDA_PATH = "relatorios/RelatorioLogs_atualizado.html"

def carregar_html():
    with open(TEMPLATE_PATH, "r", encoding="utf-8") as f:
        return BeautifulSoup(f, "html.parser")

def salvar_html(soup):
    os.makedirs(os.path.dirname(SAIDA_PATH), exist_ok=True)
    with open(SAIDA_PATH, "w", encoding="utf-8") as f:
        f.write(str(soup.prettify()))
    print(f"✅ Relatório salvo em: {SAIDA_PATH}")

def obter_dados_api():
    print("🌐 Consultando API...")
    response = requests.get(API_URL, headers=HEADERS)

    print("🔁 Status:", response.status_code)

    try:
        data = response.json()
    except Exception as e:
        raise Exception(f"❌ Erro ao interpretar resposta JSON: {e}")

    if response.status_code != 200:
        raise Exception(f"❌ API retornou status {response.status_code}: {data}")

    if not data.get("success", False):
        print(f"⚠️ Aviso da API: {data.get('message', 'Erro desconhecido')}")
        return []

    if "content" not in data or not isinstance(data["content"], list):
        raise Exception("❌ Estrutura inesperada da resposta. Campo 'content' ausente ou inválido.")

    print(f"✅ {len(data['content'])} tipos de eventos carregados.")
    return data["content"]

def extrair_nome_metodo(nome_completo):
    """Extrai apenas o nome do método entre < > ou retorna o nome original."""
    match = re.search(r"<([^>]+)>", nome_completo)
    if match:
        return match.group(1)
    return nome_completo

def atualizar_html(soup, dados_api):
    novos_metodos = []

    # Localiza a <div class="container"> onde devemos adicionar a nova div.table-container
    container_div = soup.find("div", class_="container")

    if not container_div:
        print("❌ Não foi encontrada a <div class='container'> no HTML.")
        return

    # Localiza a <div class="table-container"> onde vamos adicionar os métodos
    table_container = container_div.find("div", class_="table-container")

    if not table_container:
        print("❌ Não foi encontrada a <div class='table-container'> no HTML.")
        return

    tbody = table_container.find("tbody")
    if not tbody:
        print("❌ Não foi encontrada a <tbody> na tabela existente.")
        return

    # Atualiza métodos existentes
    for tipo_evento in dados_api:
        tipo = tipo_evento["tipoEvento"]
        for metodo in tipo_evento["metodos"]:
            nome_completo = metodo["metodo"]
            nome_formatado = extrair_nome_metodo(nome_completo)
            qtd_logs = metodo["quantidade"]

            # Busca o método na tabela existente
            td_metodo = soup.find("td", string=nome_formatado)
            if td_metodo:
                linha = td_metodo.find_parent("tr")
                tds = linha.find_all("td")
                if len(tds) >= 4:
                    tds[1].string = tipo
                    tds[3].string = str(qtd_logs)
            else:
                novos_metodos.append({
                    "metodo_completo": nome_completo,
                    "metodo_formatado": nome_formatado,
                    "tipo": tipo,
                    "quantidade": qtd_logs
                })

    # Adiciona métodos novos à nova tabela NEW
    if novos_metodos:
        print(f"➕ Adicionando {len(novos_metodos)} métodos novos à tabela 'NEW'...")

        # Cria a nova div.table-container para a nova tabela
        div_new_container = soup.new_tag("div", **{"class": "table-container"})
        
        # Cria o título para a nova seção
        span_new = soup.new_tag("span", **{"class": "category-title"})
        span_new.string = "NEW"
        div_new_container.append(span_new)

        # Criação da nova tabela "NEW"
        tabela_new = soup.new_tag("table")
        colgroup = soup.new_tag("colgroup")
        colgroup.append(soup.new_tag("col", style="width: 38%"))
        colgroup.append(soup.new_tag("col", style="width: 9%"))
        colgroup.append(soup.new_tag("col", style="width: 45%"))
        colgroup.append(soup.new_tag("col", style="width: 8%"))
        tabela_new.append(colgroup)

        # Cabeçalho da nova tabela
        thead = soup.new_tag("thead")
        tr_head = soup.new_tag("tr")
        for titulo in ["Método", "Tipo Evento", "Mensagem Base", "Quantidade Logs"]:
            th = soup.new_tag("th")
            th.string = titulo
            tr_head.append(th)
        thead.append(tr_head)
        tabela_new.append(thead)

        # Corpo da nova tabela
        tbody_new = soup.new_tag("tbody")
        tabela_new.append(tbody_new)
        
        # Adiciona a nova tabela "NEW" à nova div.table-container
        div_new_container.append(tabela_new)

        # Preenche a tabela NEW com os novos métodos
        for item in novos_metodos:
            nova_linha = soup.new_tag("tr")

            # Campo 1: método formatado com title (tooltip)
            td_metodo = soup.new_tag("td")
            td_metodo.string = item["metodo_formatado"]
            nova_linha.append(td_metodo)

            # Demais campos
            for valor in [item["tipo"], "", str(item["quantidade"])]:
                td = soup.new_tag("td")
                td.string = valor
                nova_linha.append(td)

            tbody_new.append(nova_linha)
            print(f"   ➕ Método novo: {item['metodo_formatado']} (de {item['metodo_completo']})")

        # Adiciona a nova div.table-container com a tabela ao documento dentro da div.container
        container_div.append(div_new_container)  # Isso coloca a nova div dentro da div.container

def main():
    print("🔍 Gerando relatório de logs...")
    dados_api = obter_dados_api()
    if not dados_api:
        print("ℹ️ Nenhum dado retornado pela API. Relatório não será modificado.")
        return
    soup = carregar_html()
    atualizar_html(soup, dados_api)
    salvar_html(soup)

if __name__ == "__main__":
    main()