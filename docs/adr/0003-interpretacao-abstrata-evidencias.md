# ADR-0003 — Interpretação abstrata e evidências

- **Status:** Aceita
- **Data:** 2026-09-13

## Contexto

O analisador precisa produzir conclusões úteis sem fingir que toda propriedade é decidível ou observável.

## Decisão

Usar produto reduzido de domínios com join, fixpoint, widening e narrowing. Toda conclusão carrega classificação epistemológica:

```text
PROVEN
PROVEN_UNDER_ASSUMPTIONS
STATIC_UPPER_BOUND
STATIC_LOWER_BOUND
INFERRED
SYMBOLIC
ESTIMATED
OBSERVED
HEURISTIC
EXTERNAL_CONTRACT
UNKNOWN
```

## Consequências

- Ausência de prova não vira certeza.
- Scores são heurísticos.
- Fórmulas e assumptions aparecem no finding.
- Relatórios conseguem separar proof, estimate e measurement.
