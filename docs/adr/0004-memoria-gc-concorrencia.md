# ADR-0004 — Memória, GC e concorrência

- **Status:** Aceita
- **Data:** 2026-09-13

## Contexto

Alocação total, memória viva, retenção e working set não são equivalentes. Concorrência pode multiplicar memória de trabalho.

## Decisão

Modelar separadamente:

```text
AllocationVolume
LiveManagedMemory
RetainedMemory
PeakWorkingSetEstimate
NativeMemory
```

Usar abstract heap, escape/alias analysis, LOH threshold configurável e:

$$
PeakParallel\leq Base+P\times PeakTask
$$

## Consequências

- `newobj` isolado não é classificado como vazamento.
- Retenção exige caminho de GC root e ausência/presença de remoção.
- Upper bound paralelo não é working set observado.
