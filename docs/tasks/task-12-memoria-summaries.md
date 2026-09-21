# T12 — Implementar memória e summaries

**Fase:** 4 — Análise matemática  
**Status:** Concluída  
**Prioridade:** Crítica

## Objetivo
Modelar custo semântico de APIs e memória CLR.

## Implementação
Criar summaries versionados para coleções/LINQ/streams/Tasks; abstract heap; escape; retention; live/allocated/peak; LOH configurável; concorrência.

## Dependências
T08–T11.

## Critérios de aceite
- Allocation volume é separado de retained/live memory.
- Static cache mostra caminho GC root.
- LOH threshold é configurável.
- Summary usado e versão aparecem no finding.

## Resultado da implementação

- Criados `AbstractHeap`, `AbstractHeapObject` e `MemorySummary` em `src/ClrLens.Analysis/AbstractDomains.cs`.
- Implementado `MemorySummaryAnalyzer` para separar `AllocationVolume`, `LiveManagedMemory`, `RetainedMemory`, `PeakWorkingSetEstimate` e `LargeObjectHeapBytes`.
- O caminho de GC root do static cache é preservado em `StaticCacheRootPaths` e no summary.
- O threshold de LOH é configurável e a versão do summary está exposta no `CostModel` (`summaryVersion`, `summaryId`, `lohThresholdBytes`).
- Testes xUnit cobrem alocação separada, retention por static root e threshold LOH configurável.
