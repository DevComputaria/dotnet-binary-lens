# T08 — Construir call graph e efeitos

**Fase:** 3 — CFG e findings  
**Status:** Concluída  
**Prioridade:** Alta

## Objetivo
Mapear chamadas intra-assembly e efeitos desconhecidos.

## Implementação
Resolver calls diretas, virtuais e interfaces quando possível; formar SCCs; anexar `UnknownEffect` para métodos sem corpo/summary.

## Dependências
T06 e T07.

## Critérios de aceite
- Call graph possui localização dos call sites.
- Chamadas externas não desaparecem.
- Efeitos incluem alocação, throw, memória, retenção e bloqueio.

## Resultado da implementação

- Criados `CallSite`, `CallGraph`, `CallResolutionKind` e `UnknownEffect`.
- Criado `CallGraphBuilder` em `src/ClrLens.Analysis/CallGraph.cs`.
- Resolvidos tokens para definições internas, member references externas e method specifications.
- Preservados caller, offset IL, opcode, token, nome do alvo, resolução e efeitos conservadores.
- Calculados edges internos e SCCs do call graph.
- Harness validou call sites, chamadas externas, efeitos desconhecidos e cobertura de métodos.
