# T11 — Implementar Scalar Evolution

**Fase:** 4 — Análise matemática  
**Status:** Não iniciado  
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
