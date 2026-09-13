# T12 — Implementar memória e summaries

**Fase:** 4 — Análise matemática  
**Status:** Não iniciado  
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
