# ADR-0001 — Produto e fronteiras

- **Status:** Aceita
- **Data:** 2026-09-13

## Contexto

Um otimizador genérico de DLL não pode garantir uma versão matematicamente ótima. O RyuJIT já otimiza machine code em runtime.

## Decisão

O produto será um **CLR Binary Performance Analyzer + Verified IL Rewriter**. O relatório/diagnóstico é primário; a reescrita é opt-in e posterior.

## Consequências

- O MVP prioriza leitura, IR, CFG, análise e findings.
- Recomendações arquiteturais não são auto-fix.
- Ganhos reais devem ser medidos por runtime profiling/benchmark.
- NativeAOT e mixed-mode ficam fora do escopo inicial.
