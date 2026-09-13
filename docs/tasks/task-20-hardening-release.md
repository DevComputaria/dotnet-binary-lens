# T20 — Hardening e release

**Fase:** 7 — Runtime e release  
**Status:** Não iniciado  
**Prioridade:** Crítica

## Objetivo
Preparar o produto para uso confiável em CI/CD.

## Implementação
Testar malformed inputs, limites de recursos, compatibilidade de frameworks, redaction, determinismo, performance do analisador, licenças, documentação e gates de release.

## Dependências
T01–T19.

## Critérios de aceite
- Input hostil não causa crash/DoS sem diagnóstico.
- Relatórios respeitam redaction.
- Todas as releases têm schema/versionamento.
- Rewrites passam ILVerify e gates definidos.
- Métricas técnicas e limitações são documentadas.
