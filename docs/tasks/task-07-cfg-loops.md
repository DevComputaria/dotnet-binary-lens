# T07 — Construir CFG e loops

**Fase:** 3 — CFG e findings  
**Status:** Concluída  
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

## Resultado da implementação

- Criados `BasicBlock`, `ControlFlowEdge`, `NaturalLoop` e `ControlFlowGraph`.
- Criado `ControlFlowGraphBuilder` em `src/ClrLens.Analysis/ControlFlowGraph.cs`.
- Implementados CFG normal (`E_N`) e excepcional (`E_X`).
- Implementados líderes de bloco, dominadores, pós-dominadores, SCCs, backedges e loops naturais.
- Loops naturais são restringidos por dominância do header; ciclos irreduzíveis permanecem representados como SCCs.
- Harness validou CFGs, edges válidas, loops e fluxo excepcional nas fixtures.
