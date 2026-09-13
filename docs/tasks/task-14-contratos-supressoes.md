# T14 — Implementar contratos e supressões

**Fase:** 5 — Pessimismo e relatórios  
**Status:** Não iniciado  
**Prioridade:** Alta

## Objetivo
Permitir reduzir incerteza com governança auditável.

## Implementação
Carregar contratos YAML/JSON/atributos; validar owner, source, scope, ticket e expiration; preservar finding suprimido; criar policies de CI.

## Dependências
T02 e T13.

## Critérios de aceite
- Contrato gera `EXTERNAL_CONTRACT` ou `PROVEN_UNDER_ASSUMPTIONS`.
- Supressão nunca converte UNKNOWN em PROVEN.
- Contratos expirados falham policy.
- JSON/SARIF registram a supressão.
