# T02 — Definir contratos de domínio

**Fase:** 1 — Fundação  
**Status:** Não iniciado  
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
