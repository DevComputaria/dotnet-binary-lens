# T10 — Implementar domínios abstratos

**Fase:** 4 — Análise matemática  
**Status:** Concluída  
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

## Resultado da implementação

- Criados domínios `Interval`, `Congruence`, `NullabilityState`, `Cardinality`, `AllocationState`, `EscapeState`, `AliasState` e `ConcurrencyState`.
- Criado produto reduzido `AbstractValue` com `LessOrEqual`, `Join` e `Widen`.
- Criado `AbstractState` com values por `ValueId`, notas de perda de precisão, `Join`, `Widen`, `Narrow` e clone.
- Criado `FixpointEngine` com limite de iterações, widening configurável, resultado de convergência e diagnósticos.
- Implementada aritmética de intervalos com saturação para overflow de `long`.
- Fixed-width, signedness e tipo são preservados no `AbstractValue` quando compatíveis.
- Testes xUnit cobrem monotonicidade, widening, congruência, largura fixa, perda de precisão e convergência.
