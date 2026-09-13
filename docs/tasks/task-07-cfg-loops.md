# T07 — Construir CFG e loops

**Fase:** 3 — CFG e findings  
**Status:** Não iniciado  
**Prioridade:** Crítica

## Objetivo
Construir CFG normal/excepcional e análises de grafo.

## Implementação
Calcular dominators, post-dominators, SCCs, backedges, loops naturais, nesting e ciclos irreduzíveis.

## Dependências
T06.

## Critérios de aceite
- `E_N` e `E_X` são distinguíveis.
- Loops naturais não são confundidos com ciclos irreduzíveis.
- Consultas são determinísticas.
- Fixtures de EH e loops passam.
