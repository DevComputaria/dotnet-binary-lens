# T11 — Implementar Scalar Evolution

**Fase:** 4 — Análise matemática  
**Status:** Concluída  
**Prioridade:** Alta

## Objetivo
Inferir induction variables, trip counts e terminação.

## Implementação
Representar `{Start,+,Step}`, distinguir `TripCount`/`BackedgeTakenCount`, tratar overflow e emitir estados de terminação.

## Dependências
T07 e T10.

## Critérios de aceite
- Loops lineares e stepped produzem bounds simbólicos.
- `Foo()` desconhecido produz `TERMINATION_UNKNOWN`.
- Não terminação só é declarada com evidência formal.

## Resultado da implementação

- Criado `ScalarEvolutionAnalyzer` em `src/ClrLens.Analysis/ScalarEvolution.cs`.
- Identificadas recurrences lineares e stepped para loops naturais, com expressão simbólica e trip count quando a estrita prova existe.
- `TerminationState` preserva `ProvenUnderAssumptions` para evoluções observáveis e `Unknown` para ausência de evidência formal.
- A análise não declara não terminação sem prova formal.
- Testes xUnit cobrem loop linear e ausência de non-termination proof para progressos desconhecidos.
