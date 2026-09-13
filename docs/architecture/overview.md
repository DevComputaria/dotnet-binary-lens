# Visão geral da arquitetura

## Missão

Analisar uma DLL gerenciada sem executar seu código, construir uma representação formal do CIL, explicar riscos de CPU/memória/GC/concorrência e produzir relatórios rastreáveis.

## Princípios

1. O analisador vem antes do otimizador.
2. A semântica do CIL é baseada na ECMA-335.
3. A IR é própria e não expõe Cecil ao núcleo.
4. Fluxo normal e excepcional são modelados separadamente.
5. Custos estáticos não são apresentados como tempo absoluto.
6. Alocação, memória viva, retenção e working set são grandezas diferentes.
7. Incerteza é preservada no relatório.
8. Reescrita nunca sobrescreve a entrada e exige validação.

## Pipeline

```text
DLL/PDB
  -> PE + Metadata
  -> CIL Decoder
  -> Typed Stack IR
  -> CFG + EH + SSA-like + Memory model
  -> Abstract Interpretation / Fixed Point
  -> Loops + Ranges + Cardinality + Escape
  -> CPU + Memory + GC + Concurrency Cost
  -> Findings + Evidence
  -> JSON/SARIF/Markdown/HTML
  -> Optional Rewrite
  -> Translation Validation
  -> ILVerify + Differential Tests
```

## Escopo inicial

Suportar assemblies gerenciados .NET Framework, .NET 6, .NET 8, .NET 9 e .NET 10 quando houver CIL. NativeAOT e mixed-mode ficam fora do escopo. ReadyToRun pode ser analisado, mas sua reescrita exige política específica de rebuild/strip.

## Produto principal

O relatório é o principal valor do produto. Ele deve responder:

1. Onde está o problema?
2. Por que ele pode ocorrer?
3. Qual é o impacto matemático ou operacional?
4. É auto-fix, revisão ou mudança no código-fonte?
