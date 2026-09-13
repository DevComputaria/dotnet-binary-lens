# T13 — Implementar modo pessimista

**Fase:** 5 — Pessimismo e relatórios  
**Status:** Não iniciado  
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
