# T08 — Construir call graph e efeitos

**Fase:** 3 — CFG e findings  
**Status:** Não iniciado  
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
