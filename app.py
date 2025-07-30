from flask import Flask, render_template_string
from relatorios_logs import main as gerar_relatorio
import os

app = Flask(__name__)

ARQUIVO_RELATORIO = "relatorios/RelatorioLogs_atualizado.html"

@app.route("/")
def exibir_relatorio():
    if not os.path.exists(ARQUIVO_RELATORIO):
        return "<h2>❌ Relatório ainda não foi gerado.</h2><a href='/gerar'>Clique aqui para gerar</a>"

    with open(ARQUIVO_RELATORIO, "r", encoding="utf-8") as f:
        html = f.read()
    return render_template_string(html)

@app.route("/gerar")
def gerar():
    try:
        gerar_relatorio()
        return "<h2>✅ Relatório gerado com sucesso!</h2><a href='/'>Ver relatório</a>"
    except Exception as e:
        return f"<h2>❌ Erro ao gerar relatório:</h2><pre>{e}</pre>"

if __name__ == "__main__":
    app.run(debug=True)
