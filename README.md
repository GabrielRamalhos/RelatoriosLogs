
# 📊 Gerador de Relatório de Logs - Retornar

![Python](https://img.shields.io/badge/python-3.7%2B-blue)
![Status](https://img.shields.io/badge/status-em%20uso-green)
![License](https://img.shields.io/badge/license-Proprietary-red)

Este projeto gera automaticamente um relatório HTML com logs agrupados por método e tipo de evento, consumindo os dados da API da Retornar. Ele atualiza um template existente e adiciona novos métodos encontrados à seção `NEW`.

---

## 🚀 Funcionalidades

- ✅ Consumo automático da API de logs da Retornar.
- ✅ Atualização de campos existentes no HTML (`Tipo Evento`, `Quantidade Logs`).
- ✅ Inserção automática de métodos não encontrados no HTML na seção **NEW**.
- ✅ Nome dos métodos exibido de forma limpa (ex: `PostEventCompletoAsync`).
- ✅ Remoção da coluna "Condição de Disparo" do relatório.

---

## 📁 Estrutura de Pastas

```
relatorio-logs/
├── relatorios/                  # Relatórios gerados
│   └── RelatorioLogs_atualizado.html
├── templates/                   # Template base
│   └── RelatorioLogs.html       # HTML que será atualizado
├── relatorios_logs.py          # Script principal
└── README.md
```

---

## ⚙️ Requisitos

- Python 3.7+
- Bibliotecas:
  - `requests`
  - `beautifulsoup4`

### Instale com:

```bash
pip install -r requirements.txt
```

> Crie um arquivo `requirements.txt` com:
> ```
> requests
> beautifulsoup4
> ```

---

## 🔐 Configuração

O script já está configurado com um **Bearer Token fixo** (por sua conta e risco). Se desejar, você pode mover isso para um `.env` no futuro.

---

## ▶️ Como usar

1. Certifique-se de que o arquivo `templates/RelatorioLogs.html` existe.
2. Execute o script:

```bash
python relatorios_logs.py
```

3. O resultado será salvo em:

```
relatorios/RelatorioLogs_atualizado.html
```

---

## 🆕 Sobre a seção `NEW`

- Todos os métodos **não encontrados no HTML original** são adicionados automaticamente em uma tabela separada chamada `NEW`.
- Apenas o **nome real do método** será exibido (`<PostEventCompletoAsync>` → `PostEventCompletoAsync`).
- O caminho completo aparece como tooltip ao passar o mouse.

---

## 🛠️ Personalizações futuras

- Exportar para PDF ou Excel
- Agrupar métodos por tipo de evento dentro da seção `NEW`
- Adicionar destaques visuais para métodos recém-adicionados

---

## 🧑‍💻 Autor

Gabriel Ramalho  
Retornar Tecnologia
