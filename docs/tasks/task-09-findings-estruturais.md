# T09 — Implementar findings estruturais

**Fase:** 3 — CFG e findings  
**Status:** Não iniciado  
**Prioridade:** Alta

## Objetivo
Criar as primeiras regras acionáveis de CPU e memória.

## Regras
Loop excessivo, traversal quadrático, busy wait, call cara em loop, `newobj/newarr/box` repetidos e alocação dentro de loop.

## Dependências
T02, T06, T07 e T08.

## Critérios de aceite
- Finding possui ID, severidade, evidência IL e remediation.
- Bounds desconhecidos permanecem `UNKNOWN`.
- JSON/SARIF são produzidos.
- Não há auto-fix nesta tarefa.
