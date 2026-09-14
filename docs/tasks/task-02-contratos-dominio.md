# T02 — Definir contratos de domínio

**Fase:** 1 — Fundação  
**Status:** Concluída  
**Prioridade:** Crítica

## Objetivo
Definir modelos estáveis para evidência, findings, cenários, contratos, supressões e `analysis.json`.

## Implementação
Criar tipos imutáveis, enumerações epistemológicas, proveniência, localização IL, assumptions, remediation e configuração. Criar schemas versionados para JSON, contratos e supressões.

## Dependências
T01.

## Critérios de aceite
- Schemas validam exemplos válidos e rejeitam inválidos.
- `EXTERNAL_CONTRACT` é suportado.
- Serialização é determinística.
- O contrato registra hash do assembly e versão da ferramenta.

## Resultado da implementação

- Criados contratos imutáveis em `src/ClrLens.Core/Domain/AnalysisContracts.cs`.
- Criados `AnalysisReportSerializer` e `DomainContractValidator`.
- Criados schemas em `schemas/analysis.schema.json`, `schemas/contracts.schema.json` e `schemas/suppressions.schema.json`.
- Criados exemplos em `examples/contracts.valid.json` e `examples/contracts.invalid.json`.
- Validação smoke confirmou `EXTERNAL_CONTRACT`, rejeição de contrato inválido e serialização determinística.
