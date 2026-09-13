# T04 — Implementar ingestão PE e metadata

**Fase:** 2 — PE, CIL e IR  
**Status:** Não iniciado  
**Prioridade:** Crítica

## Objetivo
Ler assemblies com segurança usando `PEReader`/`MetadataReader`.

## Implementação
Calcular SHA-256, identificar TFM/arquitetura/referências/PDB, detectar NativeAOT, mixed-mode, R2R, strong name e metadata inválida. Aplicar limites de tamanho, tempo e memória.

## Dependências
T01–T03.

## Critérios de aceite
- Input nunca é executado.
- DLL válida gera `AssemblyModel`.
- Input inválido gera diagnóstico estruturado.
- PDB ausente não impede análise por offsets IL.
