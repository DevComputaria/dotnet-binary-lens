# T10 — Implementar domínios abstratos

**Fase:** 4 — Análise matemática  
**Status:** Não iniciado  
**Prioridade:** Alta

## Objetivo
Implementar abstract interpretation com produto reduzido de domínios.

## Implementação
Intervals, congruence, nullability, cardinality, allocation, escape, alias e concurrency; join, widening, narrowing, transfer functions e limites de convergência.

## Dependências
T06–T09.

## Critérios de aceite
- Join é monotônico.
- Loops convergem ou reportam limite.
- Perda de precisão é registrada.
- Fixed-width metadata não é perdida.
