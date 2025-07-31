import requests
from bs4 import BeautifulSoup
import os
import re

# ========== Token fixo ==========
BEARER_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6IkdhYnJpZWwgUmFtYWxobyIsIlJldG9ybmFyIjoiVXN1YXJpbyIsImVtYWlsIjoiZ2FicmllbC5yYW1hbGhvc0ByZXRvcm5hci5jb20uYnIiLCJJZFVzdWFyaW8iOiI2MzdmNzE4ZGNjNGU2MzM2Yjk1ZmVhYTUiLCJJZExvamEiOiI1ZjU3OGEzMmI5NGM5MzI1MmIzM2Y5ODAiLCJuYmYiOjE3NTM5NjM0MDMsImV4cCI6MTc1NDAwNjYwMywiaWF0IjoxNzUzOTYzNDAzfQ.M26fOQlTU8jEu2627YS0kOxZ18ppViVvZYZx7DAwzOs"

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
    match = re.search(r"<([^>]+)>", nome_completo)
    return match.group(1) if match else nome_completo

def resumir_mensagem(mensagem):
    """Retorna a mensagem base resumida até 'Parâmetros' ou string vazia se for nula."""
    if not mensagem or mensagem.strip().lower() in ("null", "none"):
        return ""
    return mensagem.split("Parâmetros")[0].strip()

def atualizar_html(soup, dados_api):
    novos_metodos = []

    container_div = soup.find("div", class_="container")
    if not container_div:
        print("❌ Não foi encontrada a <div class='container'> no HTML.")
        return

    table_container = container_div.find("div", class_="table-container")
    if not table_container:
        print("❌ Não foi encontrada a <div class='table-container'> no HTML.")
        return

    tbody = table_container.find("tbody")
    if not tbody:
        print("❌ Não foi encontrada a <tbody> na tabela existente.")
        return

    for tipo_evento in dados_api:
        tipo = tipo_evento["tipoEvento"]
        for metodo in tipo_evento["metodos"]:
            nome_completo = metodo["metodo"]
            nome_formatado = extrair_nome_metodo(nome_completo)
            qtd_logs = metodo["quantidade"]
            mensagem_base = resumir_mensagem(metodo.get("mensagemBase", ""))

            td_metodo = soup.find("td", string=nome_formatado)
            if td_metodo:
                linha = td_metodo.find_parent("tr")
                tds = linha.find_all("td")
                if len(tds) >= 4:
                    tds[1].string = tipo
                    tds[2].string = mensagem_base
                    tds[3].string = str(qtd_logs)
            else:
                novos_metodos.append({
                    "metodo_completo": nome_completo,
                    "metodo_formatado": nome_formatado,
                    "tipo": tipo,
                    "mensagem_base": mensagem_base,
                    "quantidade": qtd_logs
                })

    if novos_metodos:
        print(f"➕ Adicionando {len(novos_metodos)} métodos novos à tabela 'NEW'...")

        div_new_container = soup.new_tag("div", **{"class": "table-container"})
        span_new = soup.new_tag("span", **{"class": "category-title"})
        img_tag = soup.new_tag("img", src="../images/new.svg", alt="New")
        span_new.append(img_tag)
        span_new.append(" New")

        # Adiciona o span ao div
        div_new_container.append(span_new)

        tabela_new = soup.new_tag("table")
        colgroup = soup.new_tag("colgroup")
        colgroup.append(soup.new_tag("col", style="width: 38%"))
        colgroup.append(soup.new_tag("col", style="width: 9%"))
        colgroup.append(soup.new_tag("col", style="width: 45%"))
        colgroup.append(soup.new_tag("col", style="width: 8%"))
        tabela_new.append(colgroup)

        thead = soup.new_tag("thead")
        tr_head = soup.new_tag("tr")
        for titulo in ["Método", "Tipo Evento", "Mensagem Base", "Quantidade Logs"]:
            th = soup.new_tag("th")
            th.string = titulo
            tr_head.append(th)
        thead.append(tr_head)
        tabela_new.append(thead)

        tbody_new = soup.new_tag("tbody")
        tabela_new.append(tbody_new)
        div_new_container.append(tabela_new)

        for item in novos_metodos:
            nova_linha = soup.new_tag("tr")

            td_metodo = soup.new_tag("td")
            td_metodo.string = item["metodo_formatado"]
            nova_linha.append(td_metodo)

            td_tipo = soup.new_tag("td")
            td_tipo.string = item["tipo"]
            nova_linha.append(td_tipo)

            td_msg = soup.new_tag("td")
            td_msg.string = item["mensagem_base"]
            nova_linha.append(td_msg)

            td_qtd = soup.new_tag("td")
            td_qtd.string = str(item["quantidade"])
            nova_linha.append(td_qtd)

            tbody_new.append(nova_linha)
            print(f"   ➕ Método novo: {item['metodo_formatado']}")

        container_div.append(div_new_container)

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
