# 🧠 Cache Simulator

> Simulador de memória cache desenvolvido como trabalho final da disciplina de **Fundamentos de Arquitetura de Computadores**.

O simulador implementa uma cache configurável com suporte a diferentes políticas de escrita, substituição e associatividades, permitindo tanto simulação manual quanto análises automáticas com geração de gráficos interativos.

---

## 📋 Requisitos

### Obrigatórios
| Ferramenta | Versão mínima | Observação |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 | Para compilar e executar o projeto |

### Opcionais (para geração de gráficos)
| Ferramenta | Versão mínima | Observação |
|---|---|---|
| [Python](https://www.python.org/downloads/) | 3.8+ | Deve estar disponível no PATH (`py`, `python3` ou `python`) |
| plotly | 5.22+ | `pip install plotly` |
| pandas | 2.0+ | `pip install pandas` |

Instale as dependências Python de uma vez:
```bash
pip install -r charts/requirements.txt
```

---

## 🚀 Como rodar

### 1. Clone o repositório
```bash
git clone <url-do-repositorio>
cd CacheSimulator
```

### 2. Compile e execute
```bash
dotnet run
```

Ou, se preferir compilar separadamente:
```bash
dotnet build
dotnet run --project CacheSimulator.csproj
```

> No Windows também é possível usar o `build_and_run.bat` na raiz do projeto.

---

## 💡 Sobre o projeto

Este simulador modela o comportamento de uma memória cache de nível único com endereçamento de 32 bits. O objetivo é analisar empiricamente como diferentes parâmetros de configuração afetam o desempenho do sistema de memória — especificamente a **taxa de acerto (hit rate)** e o **tempo médio de acesso (AMAT)**.

O projeto foi desenvolvido para o TDE 2 da disciplina, cobrindo as seguintes análises:

- Impacto do **tamanho total da cache**
- Impacto do **tamanho do bloco**
- Impacto da **associatividade**
- Comparação entre **políticas de substituição** (LRU vs. Aleatória)
- Análise de **largura de banda** da memória principal

---

## 🖥️ Executando o programa

### Seleção de arquivo de entrada

Ao iniciar, o simulador pede que você escolha um arquivo de sequência de acessos à memória:

| Opção | Arquivo | Descrição |
|---|---|---|
| `1` | `teste_cache.txt` | Arquivo de teste reduzido |
| `2` | `oficial_cache.txt` | Arquivo oficial da disciplina |
| `3` | *(manual)* | Informe um caminho personalizado |

O formato do arquivo é simples — cada linha contém um endereço hexadecimal de 32 bits seguido do tipo de acesso:
```
07b243a0 R
00228d40 R
001fe308 W
```
Linhas em branco e comentários com `#` são ignorados.

---

### Menu Principal

| Opção | O que faz |
|---|---|
| `1` **Modo Manual** | Você configura todos os parâmetros da cache manualmente e executa uma única simulação. Ao final, exibe hit rate, AMAT e acessos à memória principal. Permite exportar os resultados em CSV. |
| `2` **Modo Análise** | Executa análises automáticas variando parâmetros sistematicamente. Gera tabelas no console e arquivos CSV na pasta de resultados. |
| `3` **Trocar arquivo** | Troca o arquivo de sequência de acessos sem reiniciar o programa. |
| `4` **Pasta de resultados** | Redefine onde os CSVs e gráficos serão salvos. Padrão: `bin/Debug/net10.0/results/`. |
| `0` **Sair** | Encerra o programa. |

---

### Modo Manual — Parâmetros configuráveis

| Parâmetro | Opções | Padrão |
|---|---|---|
| Política de escrita | `0` = Write-Through, `1` = Write-Back | Write-Through |
| Tamanho do bloco (bytes) | Potência de 2, ex: 64, 128, 256 | 128 |
| Número de linhas | Potência de 2, ex: 64, 256, 1024 | 256 |
| Associatividade | Potência de 2, de 1 até nº de linhas | 4 |
| Hit time (ns) | Inteiro positivo | 4 |
| Política de substituição | `0` = LRU, `1` = Aleatória | LRU |
| Tempo acesso MP (ns) | Inteiro positivo | 60 |

Pressione Enter em qualquer campo para aceitar o valor padrão.

---

### Modo Análise — Subopções

| Opção | Análise | Parâmetros fixos | Variável |
|---|---|---|---|
| `1` | **Tamanho da Cache** | bloco=128B, write-through, LRU, assoc=4 | nº de blocos (dobra até estabilizar) |
| `2` | **Tamanho do Bloco** | cache=8KB, write-through, LRU, assoc=2 | bloco de 8B a 4096B |
| `3` | **Associatividade** | bloco=128B, write-back, LRU, cache=8KB | 1-way a 64-way |
| `4` | **Política de Substituição** | bloco=128B, write-through, assoc=4 | nº de blocos × LRU vs Aleatória |
| `5` | **Largura de Banda** | — | 16 combinações de cache/bloco/assoc/política |
| `6` | **Todas** | — | Executa 1–5 em sequência e oferece gerar gráficos |
| `7` | **Gráficos** | — | Lê os CSVs gerados e abre dashboard no navegador |

Cada análise exibe uma tabela no console com **hit rate**, **AMAT** e **acessos à memória principal**, e salva um CSV correspondente na pasta de resultados.

---

### Gráficos em Python

A opção `7` (ou a pergunta ao final da opção `6`) executa o script `charts/graficos.py`, que lê os CSVs gerados pelas análises e produz um dashboard interativo em HTML usando **Plotly**.

O simulador localiza automaticamente o Python no PATH (testa `py`, `python3` e `python` nessa ordem). Ao concluir, o arquivo `graficos_cache.html` é salvo na pasta de resultados e o programa oferece abri-lo diretamente no **navegador padrão**.

> **Pré-requisito:** execute as análises (opções 1–5 ou a opção 6) antes de gerar os gráficos, pois o script lê os CSVs produzidos por elas.

---

## 📄 Relatório Final

O arquivo `RelatorioFinal.pdf` contém:

- Fundamentação teórica sobre hierarquia de memória e funcionamento da cache
- Descrição da metodologia de simulação e dos parâmetros utilizados
- Resultados das cinco análises com tabelas e gráficos
- Discussão sobre os efeitos de cada parâmetro no desempenho (hit rate e AMAT)
- Análise da largura de banda e impacto das políticas de escrita
- Conclusões sobre as configurações com melhor relação custo-benefício

---

## 🗂️ Estrutura do projeto

```
CacheSimulator/
├── Core/                   # Motor de simulação (engine, configuração, estatísticas)
├── IO/                     # Leitura de arquivos, exportação CSV, runner de gráficos
├── UI/                     # Renderização do console (menus, tabelas, progresso)
├── charts/
│   ├── graficos.py         # Script Python para geração do dashboard Plotly
│   └── requirements.txt    # Dependências Python
├── ArquivosSimulacao/
│   ├── teste_cache.txt     # Sequência de acessos reduzida
│   └── oficial_cache.txt   # Sequência oficial da disciplina
├── RelatorioFinal.pdf
└── Program.cs
```
