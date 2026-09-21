# T13 — Implementar modo pessimista

**Fase:** 5 — Pessimismo e relatórios  
**Status:** Concluída  
**Prioridade:** Crítica

## Objetivo
Modelar entradas externas, branches caros e upper bounds sem inventar números.

## Implementação
Adicionar modos typical/pessimistic/observed, símbolos `UnknownExternal`, cenários baseline/stress/untrusted, pior branch, loops sem bound e regra MEM012.

## Dependências
T10–T12.

## Critérios de aceite
- Payload sem limite permanece simbólico.
- `ReadToEnd/ToArray/ToList` são detectados.
- Upper bound é distinguido de observed.
- Cenários são independentes.

## Resultado da implementação

- Modelados `InputBoundKind`, `ExternalInputBound`, `MaterializationClassifier` e `PessimisticAnalysis` em `src/ClrLens.Analysis/AbstractDomains.cs`.
- `UnknownExternal` permanece simbólico e não vira número inventado em cenário pessimista.
- `ReadToEnd`, `ToArray` e `ToList` são reconhecidos como materialização de I/O.
- Cenários `Baseline`, `Stress` e `Untrusted` continuam independentes e preservam `ExpectedConcurrency` e `ContainerMemoryBytes` por cenário.
- Testes xUnit cobrem payload simbólico, materialização e independência de cenário.
