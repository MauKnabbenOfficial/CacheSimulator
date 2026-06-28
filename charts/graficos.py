#!/usr/bin/env python3
"""
Gerador de gráficos — Cache Simulator (TDE 2)
Fundamentos de Arquitetura de Computadores

Uso:
    python graficos.py <pasta_resultados>

Saída (na mesma pasta dos CSVs):
    graficos_cache.html      dashboard interativo com abas

Instalação das dependências:
    pip install plotly pandas
"""

import sys
import os

# Força UTF-8 no output para evitar caracteres corrompidos no console Windows
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

import pandas as pd
import plotly.graph_objects as go
from plotly.subplots import make_subplots

# ── Paleta de cores ──────────────────────────────────────────────────────────

C = {
    "hit":    "#2E86AB",   # azul — hit rate
    "amat":   "#E84855",   # vermelho — AMAT
    "lru":    "#3A86FF",   # azul-vivo — LRU
    "rnd":    "#FF6B6B",   # salmão — Aleatória
    "wt_r":   "#E63946",   # vermelho — WriteThrough leituras
    "wb_r":   "#2A9D8F",   # verde-teal — WriteBack leituras
    "wt_w":   "#FF8FA3",   # rosa — WriteThrough escritas
    "wb_w":   "#74C69D",   # verde-claro — WriteBack escritas
}

LAYOUT_BASE = dict(
    plot_bgcolor="#FAFAFA",
    paper_bgcolor="white",
    hovermode="x unified",
    font=dict(family="Segoe UI, Arial, sans-serif", size=13),
    margin=dict(l=70, r=70, t=80, b=70),
    legend=dict(bgcolor="rgba(255,255,255,0.85)", bordercolor="#ddd", borderwidth=1),
)

# ── Helpers ──────────────────────────────────────────────────────────────────

def load(results_dir: str, filename: str) -> pd.DataFrame | None:
    """Carrega CSV ignorando linhas de comentário (#) e linhas em branco."""
    path = os.path.join(results_dir, filename)
    if not os.path.exists(path):
        print(f"  [aviso] não encontrado: {filename}", file=sys.stderr)
        return None
    try:
        return pd.read_csv(path, comment="#", skip_blank_lines=True)
    except Exception as e:
        print(f"  [erro] ao ler {filename}: {e}", file=sys.stderr)
        return None


def pct(series: pd.Series) -> pd.Series:
    return (series * 100).round(2)


def dual_fig(title: str, x, x_label: str, hr, amat,
             x_log=False, x_tickvals=None) -> go.Figure:
    """Figura com eixo duplo: Hit Rate (esq) e AMAT (dir)."""
    fig = make_subplots(specs=[[{"secondary_y": True}]])

    fig.add_trace(go.Scatter(
        x=x, y=pct(hr),
        name="Hit Rate (%)",
        mode="lines+markers",
        line=dict(color=C["hit"], width=2.5),
        marker=dict(size=9, symbol="circle"),
    ), secondary_y=False)

    fig.add_trace(go.Scatter(
        x=x, y=amat,
        name="AMAT (ns)",
        mode="lines+markers",
        line=dict(color=C["amat"], width=2.5, dash="dash"),
        marker=dict(size=9, symbol="diamond"),
    ), secondary_y=True)

    xaxis = dict(title=x_label, showgrid=True, gridcolor="#eee")
    if x_log:
        xaxis["type"] = "log"
    if x_tickvals is not None:
        xaxis["tickvals"] = x_tickvals
        xaxis["ticktext"] = [str(v) for v in x_tickvals]

    fig.update_layout(
        title=dict(text=title, font=dict(size=15), x=0.5, xanchor="center"),
        xaxis=xaxis,
        **LAYOUT_BASE,
    )
    fig.update_yaxes(
        title_text="Hit Rate (%)", secondary_y=False,
        range=[0, 100], ticksuffix="%", showgrid=True, gridcolor="#eee",
    )
    fig.update_yaxes(
        title_text="AMAT (ns)", secondary_y=True,
        showgrid=False,
    )
    return fig


# ── Análise 1 — Tamanho da Cache ─────────────────────────────────────────────

def fig_analise1(df: pd.DataFrame) -> go.Figure:
    return dual_fig(
        title="Análise 1 — Impacto do Tamanho da Cache"
              "<br><sup>Fixos: bloco=128B | Write-Through | LRU | Assoc=4 | HitTime=4ns | MP=60ns</sup>",
        x=df["Tam. Cache (KB)"],
        x_label="Tamanho da Cache (KB)",
        hr=df["Hit Rate Global"],
        amat=df["AMAT (ns)"],
        x_log=True,
        x_tickvals=df["Tam. Cache (KB)"].tolist(),
    )


# ── Análise 2 — Tamanho do Bloco ─────────────────────────────────────────────

def fig_analise2(df: pd.DataFrame) -> go.Figure:
    fig = dual_fig(
        title="Análise 2 — Impacto do Tamanho do Bloco"
              "<br><sup>Fixos: cache=8KB | Write-Through | LRU | Assoc=2 | HitTime=4ns | MP=60ns</sup>",
        x=df["Tam. Bloco (B)"],
        x_label="Tamanho do Bloco (B)",
        hr=df["Hit Rate Global"],
        amat=df["AMAT (ns)"],
        x_log=True,
        x_tickvals=df["Tam. Bloco (B)"].tolist(),
    )
    # Marca o ponto de melhor hit rate
    best_idx = df["Hit Rate Global"].idxmax()
    best_x   = df.loc[best_idx, "Tam. Bloco (B)"]
    best_hr  = round(df.loc[best_idx, "Hit Rate Global"] * 100, 2)
    fig.add_annotation(
        x=best_x, y=best_hr, xref="x", yref="y",
        text=f"  ótimo: {best_x}B ({best_hr}%)",
        showarrow=True, arrowhead=2, arrowcolor=C["hit"],
        font=dict(color=C["hit"], size=11),
        ax=40, ay=-30,
    )
    return fig


# ── Análise 3 — Associatividade ───────────────────────────────────────────────

def fig_analise3(df: pd.DataFrame) -> go.Figure:
    return dual_fig(
        title="Análise 3 — Impacto da Associatividade"
              "<br><sup>Fixos: bloco=128B | Write-Back | LRU | cache=8KB | HitTime=4ns | MP=60ns</sup>",
        x=df["Associatividade"],
        x_label="Associatividade (N-way)",
        hr=df["Hit Rate Global"],
        amat=df["AMAT (ns)"],
        x_log=True,
        x_tickvals=df["Associatividade"].tolist(),
    )


# ── Análise 4 — Política de Substituição ─────────────────────────────────────

def fig_analise4(df: pd.DataFrame) -> go.Figure:
    fig = make_subplots(specs=[[{"secondary_y": True}]])

    # Hit rates (eixo esquerdo)
    for col, name, color, dash in [
        ("Hit Rate LRU",    "Hit Rate LRU (%)",         C["lru"], "solid"),
        ("Hit Rate Aleat.", "Hit Rate Aleatória (%)",   C["rnd"], "dot"),
    ]:
        fig.add_trace(go.Scatter(
            x=df["Tam. Cache (KB)"], y=pct(df[col]),
            name=name, mode="lines+markers",
            line=dict(color=color, width=2.5, dash=dash),
            marker=dict(size=9),
        ), secondary_y=False)

    # AMATs (eixo direito)
    for col, name, color, dash in [
        ("AMAT LRU (ns)",    "AMAT LRU (ns)",         C["lru"], "dash"),
        ("AMAT Aleat. (ns)", "AMAT Aleatória (ns)",   C["rnd"], "dashdot"),
    ]:
        fig.add_trace(go.Scatter(
            x=df["Tam. Cache (KB)"], y=df[col],
            name=name, mode="lines+markers",
            line=dict(color=color, width=1.5, dash=dash),
            marker=dict(size=6),
        ), secondary_y=True)

    fig.update_layout(
        title=dict(
            text="Análise 4 — LRU vs Substituição Aleatória"
                 "<br><sup>Fixos: bloco=128B | Write-Through | Assoc=4 | HitTime=4ns | MP=60ns</sup>",
            font=dict(size=15), x=0.5, xanchor="center"),
        xaxis=dict(title="Tamanho da Cache (KB)", showgrid=True, gridcolor="#eee"),
        **LAYOUT_BASE,
    )
    fig.update_yaxes(title_text="Hit Rate (%)", secondary_y=False,
                     range=[0, 100], ticksuffix="%", showgrid=True, gridcolor="#eee")
    fig.update_yaxes(title_text="AMAT (ns)", secondary_y=True, showgrid=False)
    return fig


# ── Análise 5 — Largura de Banda ─────────────────────────────────────────────

def fig_analise5(df: pd.DataFrame) -> go.Figure:
    # Remove linha de média e filtra apenas as políticas conhecidas
    data = df[df["Política"].isin(["WriteThrough", "WriteBack"])].copy()
    data["Config"] = (
        data["Tam. Cache (KB)"].astype(str) + "KB / " +
        data["Tam. Bloco (B)"].astype(str) + "B / " +
        data["Assoc."].astype(str) + "-way"
    )
    wt = data[data["Política"] == "WriteThrough"]
    wb = data[data["Política"] == "WriteBack"]

    fig = go.Figure()

    # Leituras
    fig.add_trace(go.Bar(
        name="WriteThrough — Leituras", x=wt["Config"], y=wt["Leituras MP"],
        marker_color=C["wt_r"], opacity=0.9,
    ))
    fig.add_trace(go.Bar(
        name="WriteBack — Leituras", x=wb["Config"], y=wb["Leituras MP"],
        marker_color=C["wb_r"], opacity=0.9,
    ))
    # Escritas
    fig.add_trace(go.Bar(
        name="WriteThrough — Escritas", x=wt["Config"], y=wt["Escritas MP"],
        marker_color=C["wt_w"], opacity=0.85,
        marker_pattern_shape="/",
    ))
    fig.add_trace(go.Bar(
        name="WriteBack — Escritas", x=wb["Config"], y=wb["Escritas MP"],
        marker_color=C["wb_w"], opacity=0.85,
        marker_pattern_shape="/",
    ))

    fig.update_layout(
        barmode="group",
        title=dict(
            text="Análise 5 — Largura de Banda: Acessos à Memória Principal"
                 "<br><sup>Write-Through vs Write-Back — 16 combinações de configuração</sup>",
            font=dict(size=15), x=0.5, xanchor="center"),
        xaxis=dict(title="Configuração", tickangle=-30, showgrid=False),
        yaxis=dict(title="Acessos à Memória Principal", showgrid=True, gridcolor="#eee"),
        legend=dict(x=1.01, y=1, bgcolor="rgba(255,255,255,0.85)",
                    bordercolor="#ddd", borderwidth=1),
        **{k: v for k, v in LAYOUT_BASE.items() if k not in ("legend", "margin")},
        margin=dict(l=70, r=200, t=90, b=120),
    )
    return fig


# ── Dashboard HTML com abas ───────────────────────────────────────────────────

def build_dashboard(figs: dict) -> str:
    TABS = {
        "a1": "1 — Tamanho da Cache",
        "a2": "2 — Tamanho do Bloco",
        "a3": "3 — Associatividade",
        "a4": "4 — Pol. Substituição",
        "a5": "5 — Largura de Banda",
    }

    tabs_html    = ""
    content_html = ""
    first        = True

    for key, label in TABS.items():
        if key not in figs:
            continue
        active = "active" if first else ""
        disp   = "block"  if first else "none"
        first  = False
        inner  = figs[key].to_html(full_html=False, include_plotlyjs=False)
        tabs_html    += f'<button class="tab-btn {active}" onclick="showTab(\'{key}\')" id="btn-{key}">{label}</button>\n'
        content_html += f'<div class="tab-content" id="tab-{key}" style="display:{disp}">{inner}</div>\n'

    return f"""<!DOCTYPE html>
<html lang="pt-BR">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Cache Simulator — Gráficos TDE 2</title>
<script src="https://cdn.plot.ly/plotly-2.35.2.min.js"></script>
<style>
* {{ box-sizing: border-box; margin: 0; padding: 0; }}
body {{ font-family: 'Segoe UI', Arial, sans-serif; background: #eef1f5; color: #222; }}
header {{
  background: linear-gradient(135deg, #1a3a5c 0%, #2d6a9f 100%);
  color: white; padding: 20px 32px;
  display: flex; align-items: center; gap: 16px;
}}
header .icon {{ font-size: 2rem; }}
header h1   {{ font-size: 1.3rem; font-weight: 700; }}
header p    {{ font-size: 0.82rem; opacity: 0.75; margin-top: 3px; }}
.tabs {{
  display: flex; gap: 3px; padding: 14px 32px 0;
  background: linear-gradient(135deg, #1a3a5c 0%, #2d6a9f 100%);
  flex-wrap: wrap;
}}
.tab-btn {{
  background: rgba(255,255,255,0.18); border: none; color: white;
  padding: 9px 20px; border-radius: 8px 8px 0 0; cursor: pointer;
  font-size: 0.84rem; font-weight: 500; transition: background 0.15s;
  white-space: nowrap;
}}
.tab-btn:hover  {{ background: rgba(255,255,255,0.32); }}
.tab-btn.active {{ background: #eef1f5; color: #1a3a5c; font-weight: 700; }}
.wrapper  {{ padding: 20px 32px 32px; }}
.tab-content {{
  background: white; border-radius: 0 10px 10px 10px;
  padding: 12px 16px; box-shadow: 0 3px 12px rgba(0,0,0,0.09);
}}
</style>
</head>
<body>
<header>
  <div class="icon">📊</div>
  <div>
    <h1>Cache Simulator — Análises de Desempenho</h1>
    <p>Fundamentos de Arquitetura de Computadores &nbsp;|&nbsp; TDE 2</p>
  </div>
</header>
<div class="tabs">
{tabs_html}
</div>
<div class="wrapper">
{content_html}
</div>
<script>
function showTab(key) {{
  document.querySelectorAll('.tab-content').forEach(el => el.style.display = 'none');
  document.querySelectorAll('.tab-btn').forEach(el => el.classList.remove('active'));
  document.getElementById('tab-' + key).style.display = 'block';
  document.getElementById('btn-' + key).classList.add('active');
}}
</script>
</body>
</html>"""


# ── Main ─────────────────────────────────────────────────────────────────────

def main():
    if len(sys.argv) < 2:
        print("Uso: python graficos.py <pasta_resultados>", file=sys.stderr)
        sys.exit(1)

    results_dir = sys.argv[1]

    if not os.path.isdir(results_dir):
        print(f"[ERRO] Pasta nao encontrada: {results_dir}", file=sys.stderr)
        sys.exit(1)

    print(f"Lendo CSVs em: {results_dir}")
    figs: dict[str, go.Figure] = {}

    ANALISES = [
        ("a1", "analise1_tamanho_cache.csv",        fig_analise1),
        ("a2", "analise2_tamanho_bloco.csv",         fig_analise2),
        ("a3", "analise3_associatividade.csv",       fig_analise3),
        ("a4", "analise4_politica_substituicao.csv", fig_analise4),
        ("a5", "analise5_largura_banda.csv",         fig_analise5),
    ]

    for key, filename, builder in ANALISES:
        df = load(results_dir, filename)
        if df is not None:
            print(f"  [{key}] {filename} ({len(df)} linhas)")
            figs[key] = builder(df)

    if not figs:
        print("[ERRO] Nenhum CSV encontrado. Execute as análises primeiro.", file=sys.stderr)
        sys.exit(1)

    html_path = os.path.join(results_dir, "graficos_cache.html")
    with open(html_path, "w", encoding="utf-8") as f:
        f.write(build_dashboard(figs))
    print(f"\n  Dashboard → {html_path}")
    print("Concluído.")


if __name__ == "__main__":
    main()
