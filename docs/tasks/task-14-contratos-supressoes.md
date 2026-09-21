# T14 — Implementar contratos e supressões

**Fase:** 5 — Pessimismo e relatórios  
**Status:** Concluída  
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

## Resultado da implementação

- Adicionados classificadores de contrato em `src/ClrLens.Core/Validation/DomainContractValidator.cs` para distinguir evidência externa de evidência local validada.
- Implementada a aplicação de supressão sem converter UNKNOWN em PROVEN, preservando o valor epistemológico original do finding.
- Adicionado suporte a metadados de supressão e status no SARIF em `src/ClrLens.Reporting/SarifReportSerializer.cs`.
- Criação de testes unitários para contrato externo, supressão conservadora e serialização SARIF em `tests/ClrLens.Tests.Unit/DomainContractTests.cs`.
