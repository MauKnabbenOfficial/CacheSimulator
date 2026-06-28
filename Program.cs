using System.Diagnostics;
using CacheSimulator.Core;
using CacheSimulator.IO;
using CacheSimulator.UI;

// ── Inicialização ─────────────────────────────────────────────────────────────

string resultsDir = Path.Combine(AppContext.BaseDirectory, "results");
Directory.CreateDirectory(resultsDir);
ConsoleRenderer.PrintBanner();
ConsoleRenderer.Info($"Resultados em: {resultsDir}");
string cacheFile = SelecionarArquivo();

// ── Loop principal ────────────────────────────────────────────────────────────

while (true)
{
    ConsoleRenderer.PrintMenu("MENU PRINCIPAL", [
        ("1", "Modo Manual    — configurar parâmetros e simular"),
        ("2", "Modo Análise   — análises automáticas do TDE 2"),
        ("3", $"Trocar arquivo\n        (atual: {Path.GetFileName(cacheFile)})\n"),
        ("4", $"Pasta de resultados\n        (atual: {resultsDir})\n"),
        ("0", "Sair")
    ]);

    switch (Console.ReadLine()?.Trim())
    {
        case "1": ModoManual(cacheFile);   break;
        case "2": ModoAnalise(cacheFile);  break;
        case "3": cacheFile  = SelecionarArquivo(); break;
        case "4": resultsDir = SelecionarDiretorioResultados(resultsDir); break;
        case "0":
            Console.WriteLine();
            ConsoleRenderer.Info("Encerrando. Até logo!");
            Console.WriteLine();
            return;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// SELEÇÃO DE ARQUIVO
// ═════════════════════════════════════════════════════════════════════════════

string SelecionarArquivo()
{
    ConsoleRenderer.PrintSectionHeader("ARQUIVO DE ENTRADA");

    Console.WriteLine("  O arquivo de entrada contém a sequência de acessos à memória que");
    Console.WriteLine("  será simulada. Cada linha deve seguir o formato:");
    Console.WriteLine();
    Console.WriteLine("      <tipo> <endereço_hex>");
    Console.WriteLine();
    Console.WriteLine("  onde <tipo> é 'R' (leitura) ou 'W' (escrita) e <endereço_hex> é");
    Console.WriteLine("  um endereço de 32 bits em hexadecimal (ex: R 0x1A2B3C4D).");
    Console.WriteLine("  Linhas em branco e comentários iniciados com '#' são ignorados.");
    Console.WriteLine();

    string defaultDir = Path.Combine(AppContext.BaseDirectory, "ArquivosSimulacao");
    string testeFile  = Path.Combine(defaultDir, "teste_cache.txt");
    string oficialFile = Path.Combine(defaultDir, "oficial_cache.txt");

    ConsoleRenderer.PrintMenu("ESCOLHA O ARQUIVO", [
        ("1", $"Teste   (padrão)\n        Path: {testeFile}\n"),
        ("2", $"Oficial (padrão)\n        Path: {oficialFile}\n"),
        ("3", "Informar caminho manualmente")
    ]);

    string escolha = Console.ReadLine()?.Trim() ?? "1";

    string path = escolha switch
    {
        "2" => oficialFile,
        "3" => null!,
        _   => testeFile
    };

    if (escolha == "3")
    {
        while (true)
        {
            path = ConsoleRenderer.ReadInput("Caminho do arquivo");

            if (!File.Exists(path))
            {
                ConsoleRenderer.Error($"Arquivo não encontrado: {path}");
                continue;
            }
            break;
        }
    }
    else if (!File.Exists(path))
    {
        ConsoleRenderer.Error($"Arquivo padrão não encontrado: {path}");
        ConsoleRenderer.Info("Verifique se o build foi executado com 'dotnet build'.");
        ConsoleRenderer.Info("Informe o caminho manualmente:");

        while (true)
        {
            path = ConsoleRenderer.ReadInput("Caminho do arquivo");
            if (File.Exists(path)) break;
            ConsoleRenderer.Error($"Arquivo não encontrado: {path}");
        }
    }

    int count = AddressFileReader.CountLines(path);
    if (count == 0)
    {
        ConsoleRenderer.Error("Arquivo vazio ou sem entradas válidas.");
        return SelecionarArquivo();
    }

    ConsoleRenderer.Success($"{Path.GetFileName(path)} carregado  ({count:N0} entradas)");
    return path;
}

// ═════════════════════════════════════════════════════════════════════════════
// SELEÇÃO DE PASTA DE RESULTADOS
// ═════════════════════════════════════════════════════════════════════════════

string SelecionarDiretorioResultados(string atual)
{
    ConsoleRenderer.PrintSectionHeader("PASTA DE RESULTADOS");
    Console.WriteLine($"  Pasta atual: {atual}");
    Console.WriteLine("  (Enter sem digitar mantém a pasta atual)\n");

    string input = ConsoleRenderer.ReadInput("Nova pasta");

    if (string.IsNullOrWhiteSpace(input))
    {
        ConsoleRenderer.Info("Pasta mantida.");
        return atual;
    }

    try
    {
        Directory.CreateDirectory(input);
        ConsoleRenderer.Success($"Pasta alterada para: {input}");
        return input;
    }
    catch (Exception ex)
    {
        ConsoleRenderer.Error($"Não foi possível usar '{input}': {ex.Message}");
        ConsoleRenderer.Info($"Pasta mantida: {atual}");
        return atual;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// MODO MANUAL
// ═════════════════════════════════════════════════════════════════════════════

void ModoManual(string arquivo)
{
    ConsoleRenderer.PrintSectionHeader("MODO MANUAL — Configuração");
    Console.WriteLine("  Configure os parâmetros abaixo. Enter = usar valor padrão.\n");

    var config = LerConfiguracao();
    if (config is null) return;

    // Exibe a configuração antes de rodar
    ConsoleRenderer.PrintSectionHeader("CONFIGURAÇÃO ATIVA");
    ConsoleRenderer.PrintConfig(config);

    // Simulação com barra de progresso
    Console.WriteLine();
    ConsoleRenderer.Info("Simulando...");

    var engine = new CacheEngine(config);
    long total  = AddressFileReader.CountLines(arquivo);
    long atual  = 0;

    foreach (var acesso in AddressFileReader.Read(arquivo))
    {
        engine.Access(acesso.Address, acesso.IsWrite);
        atual++;
        if (atual % 500 == 0 || atual == total)
            ConsoleRenderer.UpdateProgress(atual, total);
    }
    ConsoleRenderer.ClearProgress();
    ConsoleRenderer.Success($"Concluído — {total:N0} acessos processados.");

    // Resultados
    ConsoleRenderer.PrintSectionHeader("RESULTADOS");
    ConsoleRenderer.PrintStats(engine.Stats, config);

    // Export opcional
    Console.WriteLine();
    string resp = ConsoleRenderer.ReadInput("Exportar para CSV? (s/n)", "s");
    if (resp.Equals("s", StringComparison.OrdinalIgnoreCase))
    {
        string csv = Path.Combine(resultsDir, $"manual_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        ResultExporter.ExportManual(csv, config, engine.Stats);
        ConsoleRenderer.Success($"Salvo em: {csv}");
    }

    ConsoleRenderer.PressAnyKey();
}

CacheConfig? LerConfiguracao()
{
    try
    {
        int wp  = ConsoleRenderer.ReadInt(
                    "Política de escrita  [0=WriteThrough, 1=WriteBack]", 0, 0, 1);

        int bs  = ConsoleRenderer.ReadPowerOfTwo(
                    "Tamanho do bloco em bytes (ex: 64, 128, 256...)", 128, 1, 65536);

        int nl  = ConsoleRenderer.ReadPowerOfTwo(
                    "Número de linhas da cache (ex: 64, 256, 1024...)", 256, 1, 65536);

        int maxAssoc = nl;
        int assoc = ConsoleRenderer.ReadPowerOfTwo(
                    $"Associatividade (1 a {maxAssoc}, potência de 2)", Math.Min(4, maxAssoc), 1, maxAssoc);

        int ht  = ConsoleRenderer.ReadInt("Hit time em ns", 4, 1, 100000);

        int rp  = ConsoleRenderer.ReadInt(
                    "Política de substituição  [0=LRU, 1=Aleatória]", 0, 0, 1);

        int mpt = ConsoleRenderer.ReadInt("Tempo de acesso à MP em ns", 60, 1, 100000);

        var config = new CacheConfig
        {
            WritePolicy       = (WritePolicy)wp,
            BlockSize         = bs,
            NumLines          = nl,
            Associativity     = assoc,
            HitTimeNs         = ht,
            ReplacementPolicy = (ReplacementPolicy)rp,
            MainMemoryTimeNs  = mpt
        };
        config.Validate();
        return config;
    }
    catch (ArgumentException ex)
    {
        ConsoleRenderer.Error($"Configuração inválida: {ex.Message}");
        return null;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// MODO ANÁLISE (TDE 2)
// ═════════════════════════════════════════════════════════════════════════════

void ModoAnalise(string arquivo)
{
    while (true)
    {
        ConsoleRenderer.PrintMenu("MODO ANÁLISE — TDE 2", [
            ("1", "Impacto do Tamanho da Cache"),
            ("2", "Impacto do Tamanho do Bloco"),
            ("3", "Impacto da Associatividade"),
            ("4", "Impacto da Política de Substituição"),
            ("5", "Largura de Banda da Memória"),
            ("6", "Executar TODAS as análises (1–5)"),
            ("7", "Gerar Gráficos             (requer Python + plotly)"),
            ("0", "Voltar ao menu principal")
        ]);

        switch (Console.ReadLine()?.Trim())
        {
            case "1": Analise1(arquivo); break;
            case "2": Analise2(arquivo); break;
            case "3": Analise3(arquivo); break;
            case "4": Analise4(arquivo); break;
            case "5": Analise5(arquivo); break;
            case "6":
                Analise1(arquivo);
                Analise2(arquivo);
                Analise3(arquivo);
                Analise4(arquivo);
                Analise5(arquivo);
                ConsoleRenderer.PrintSectionHeader("TODAS AS ANÁLISES CONCLUÍDAS");
                ConsoleRenderer.Success($"CSVs salvos em: {resultsDir}");
                Console.WriteLine();
                string resp = ConsoleRenderer.ReadInput("Gerar gráficos agora? (s/n)", "s");
                if (resp.Equals("s", StringComparison.OrdinalIgnoreCase))
                    GerarGraficos();
                else
                    ConsoleRenderer.PressAnyKey();
                break;
            case "7": GerarGraficos(); break;
            case "0": return;
        }
    }
}

// ── Análise 1 ─────────────────────────────────────────────────────────────────

void Analise1(string arquivo)
{
    ConsoleRenderer.PrintSectionHeader("ANÁLISE 1 — Impacto do Tamanho da Cache");
    Console.WriteLine("  Fixos   : bloco=128B | write-through | LRU | assoc=4");
    Console.WriteLine("  Variável: número de blocos (início em 8, dobrando até estabilizar)");
    Console.WriteLine("  Parada  : |Δhit rate| < 0,5% por 2 passos consecutivos\n");

    var rows = BatchAnalysis.CacheSizeImpact(arquivo);

    ConsoleRenderer.PrintTable(
        ["Nº Blocos", "Cache (B)", "Cache (KB)", "Hit Rate Global", "AMAT (ns)", "Leit. MP", "Escrit. MP"],
        rows.Select(r => new[]
        {
            r.Config.NumLines.ToString(),
            r.Config.CacheSizeBytes.ToString(),
            $"{r.Config.CacheSizeBytes / 1024.0:F1}",
            $"{r.Stats.GlobalHitRate:P4}",
            $"{r.Stats.ComputeAMAT(r.Config.HitTimeNs, r.Config.MainMemoryTimeNs):F4}",
            r.Stats.MainMemoryReads.ToString(),
            r.Stats.MainMemoryWrites.ToString()
        }).ToArray()
    );

    string csv = Path.Combine(resultsDir, "analise1_tamanho_cache.csv");
    ResultExporter.ExportAnalysis1(csv, rows);
    ConsoleRenderer.Success($"CSV: {csv}");
    ConsoleRenderer.PressAnyKey();
}

// ── Análise 2 ─────────────────────────────────────────────────────────────────

void Analise2(string arquivo)
{
    ConsoleRenderer.PrintSectionHeader("ANÁLISE 2 — Impacto do Tamanho do Bloco");
    Console.WriteLine("  Fixos   : cache=8KB | write-through | LRU | assoc=2");
    Console.WriteLine("  Variável: tamanho do bloco de 8 a 4096 bytes\n");

    var rows = BatchAnalysis.BlockSizeImpact(arquivo);

    ConsoleRenderer.PrintTable(
        ["Tam. Bloco", "Nº Linhas", "Hit Rate Global", "AMAT (ns)", "Leit. MP", "Escrit. MP"],
        rows.Select(r => new[]
        {
            $"{r.Config.BlockSize} B",
            r.Config.NumLines.ToString(),
            $"{r.Stats.GlobalHitRate:P4}",
            $"{r.Stats.ComputeAMAT(r.Config.HitTimeNs, r.Config.MainMemoryTimeNs):F4}",
            r.Stats.MainMemoryReads.ToString(),
            r.Stats.MainMemoryWrites.ToString()
        }).ToArray()
    );

    string csv = Path.Combine(resultsDir, "analise2_tamanho_bloco.csv");
    ResultExporter.ExportAnalysis2(csv, rows);
    ConsoleRenderer.Success($"CSV: {csv}");
    ConsoleRenderer.PressAnyKey();
}

// ── Análise 3 ─────────────────────────────────────────────────────────────────

void Analise3(string arquivo)
{
    ConsoleRenderer.PrintSectionHeader("ANÁLISE 3 — Impacto da Associatividade");
    Console.WriteLine("  Fixos   : bloco=128B | write-back | LRU | cache=8KB (64 linhas)");
    Console.WriteLine("  Variável: associatividade de 1-way a 64-way\n");

    var rows = BatchAnalysis.AssociativityImpact(arquivo);

    ConsoleRenderer.PrintTable(
        ["Assoc.", "Nº Conjuntos", "Hit Rate Global", "AMAT (ns)", "Leit. MP", "Escrit. MP"],
        rows.Select(r => new[]
        {
            $"{r.Config.Associativity}-way",
            r.Config.NumSets.ToString(),
            $"{r.Stats.GlobalHitRate:P4}",
            $"{r.Stats.ComputeAMAT(r.Config.HitTimeNs, r.Config.MainMemoryTimeNs):F4}",
            r.Stats.MainMemoryReads.ToString(),
            r.Stats.MainMemoryWrites.ToString()
        }).ToArray()
    );

    string csv = Path.Combine(resultsDir, "analise3_associatividade.csv");
    ResultExporter.ExportAnalysis3(csv, rows);
    ConsoleRenderer.Success($"CSV: {csv}");
    ConsoleRenderer.PressAnyKey();
}

// ── Análise 4 ─────────────────────────────────────────────────────────────────

void Analise4(string arquivo)
{
    ConsoleRenderer.PrintSectionHeader("ANÁLISE 4 — Impacto da Política de Substituição");
    Console.WriteLine("  Fixos   : bloco=128B | write-through | assoc=4");
    Console.WriteLine("  Variável: nº de blocos (início em 16) × LRU vs Aleatória\n");

    var (lru, rnd) = BatchAnalysis.ReplacementPolicyImpact(arquivo);

    ConsoleRenderer.PrintTable(
        ["Nº Blocos", "Cache (KB)", "Hit LRU", "AMAT LRU", "Hit Aleat.", "AMAT Aleat."],
        lru.Select((r, i) => new[]
        {
            r.Config.NumLines.ToString(),
            $"{r.Config.CacheSizeBytes / 1024.0:F1}",
            $"{r.Stats.GlobalHitRate:P4}",
            $"{r.Stats.ComputeAMAT(r.Config.HitTimeNs, r.Config.MainMemoryTimeNs):F4}",
            $"{rnd[i].Stats.GlobalHitRate:P4}",
            $"{rnd[i].Stats.ComputeAMAT(rnd[i].Config.HitTimeNs, rnd[i].Config.MainMemoryTimeNs):F4}"
        }).ToArray()
    );

    string csv = Path.Combine(resultsDir, "analise4_politica_substituicao.csv");
    ResultExporter.ExportAnalysis4(csv, lru, rnd);
    ConsoleRenderer.Success($"CSV: {csv}");
    ConsoleRenderer.PressAnyKey();
}

// ── Geração de Gráficos ───────────────────────────────────────────────────────

void GerarGraficos()
{
    ConsoleRenderer.PrintSectionHeader("GERAÇÃO DE GRÁFICOS");
    Console.WriteLine("  Executando charts/graficos.py...\n");

    bool ok = ChartRunner.Run(resultsDir);

    Console.WriteLine();
    if (ok)
    {
        ConsoleRenderer.Success($"Gráficos salvos em: {resultsDir}");
        ConsoleRenderer.Info("Abra o arquivo 'graficos_cache.html' no navegador para visualizar.");

        // Oferece abrir o dashboard automaticamente
        string resp = ConsoleRenderer.ReadInput("Abrir dashboard no navegador? (s/n)", "s");
        if (resp.Equals("s", StringComparison.OrdinalIgnoreCase))
        {
            string htmlPath = Path.Combine(resultsDir, "graficos_cache.html");
            if (File.Exists(htmlPath))
            {
                Process.Start(new ProcessStartInfo(htmlPath) { UseShellExecute = true });
            }
            else
            {
                ConsoleRenderer.Error("graficos_cache.html não encontrado.");
            }
        }
    }
    else
    {
        ConsoleRenderer.Error("Falha ao gerar gráficos.");
        ConsoleRenderer.Info("Verifique se Python está instalado e rode:");
        ConsoleRenderer.Info("  pip install plotly pandas");
    }

    ConsoleRenderer.PressAnyKey();
}

// ── Análise 5 ─────────────────────────────────────────────────────────────────

void Analise5(string arquivo)
{
    ConsoleRenderer.PrintSectionHeader("ANÁLISE 5 — Largura de Banda da Memória");
    Console.WriteLine("  Combinações: cache 8/16KB × bloco 64/128B × assoc 2/4 × write-through/back");
    Console.WriteLine("  Total: 16 simulações\n");

    var rows = BatchAnalysis.BandwidthAnalysis(arquivo);

    // Monta tabela com linha de média
    var dataRows = rows.Select(r => new[]
    {
        r.Config.WritePolicy.ToString(),
        $"{r.Config.CacheSizeBytes / 1024} KB",
        $"{r.Config.BlockSize} B",
        $"{r.Config.Associativity}-way",
        r.Stats.MainMemoryReads.ToString("N0"),
        r.Stats.MainMemoryWrites.ToString("N0"),
        (r.Stats.MainMemoryReads + r.Stats.MainMemoryWrites).ToString("N0")
    }).ToList();

    // Linha de média
    if (rows.Count > 0)
    {
        long avgR = (long)rows.Average(r => r.Stats.MainMemoryReads);
        long avgW = (long)rows.Average(r => r.Stats.MainMemoryWrites);
        dataRows.Add(["── MÉDIA ──", "", "", "", avgR.ToString("N0"), avgW.ToString("N0"), (avgR + avgW).ToString("N0")]);
    }

    ConsoleRenderer.PrintTable(
        ["Política", "Cache", "Bloco", "Assoc.", "Leit. MP", "Escrit. MP", "Total MP"],
        [.. dataRows]
    );

    string csv = Path.Combine(resultsDir, "analise5_largura_banda.csv");
    ResultExporter.ExportAnalysis5(csv, rows);
    ConsoleRenderer.Success($"CSV: {csv}");
    ConsoleRenderer.PressAnyKey();
}
