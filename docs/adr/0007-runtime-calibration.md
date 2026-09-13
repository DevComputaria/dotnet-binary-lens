# ADR-0007 — Runtime calibration

- **Status:** Aceita
- **Data:** 2026-09-13

## Contexto

O CIL não determina sozinho o custo de machine code. RyuJIT, CPU, cache, branch prediction, tiered compilation e PGO alteram o comportamento real.

## Decisão

Adicionar overlay opcional de EventPipe, `dotnet-counters` e `dotnet-trace`. O relatório separa Structural Cost Unit, Runtime Calibrated Cost e Observed.

$$
Cost_{hybrid}=StaticModel+RuntimeProfile
$$

## Consequências

- Runtime profile valida/calibra, mas não substitui a análise formal.
- Workload, runtime e configuração devem ser registrados.
- Benchmark A/B não é prova de equivalência.
- Não serão prometidos nanossegundos a partir de opcodes isolados.
