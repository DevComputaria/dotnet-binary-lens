# T17 — Integrar ILVerify e validação diferencial

**Fase:** 6 — Reescrita e validação  
**Status:** Não iniciado  
**Prioridade:** Alta

## Objetivo
Garantir que assemblies reescritos são estrutural e comportamentalmente aceitáveis.

## Implementação
Integrar ILVerify, references resolver, strong-name policy, R2R policy, API/metadata checks e differential harness.

## Dependências
T16.

## Critérios de aceite
- Saída inválida é rejeitada.
- Strong name e R2R têm status explícito.
- Returns/exceptions são comparados em fixtures confiáveis.
- Falha em gate impede publicação.
