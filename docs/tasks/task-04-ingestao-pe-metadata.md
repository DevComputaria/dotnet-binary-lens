# T04 — Implementar ingestão PE e metadata

**Fase:** 2 — PE, CIL e IR  
**Status:** Concluída  
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

## Resultado da implementação

- Criado `AssemblyReader` em `src/ClrLens.PE/AssemblyIngestion.cs`.
- Criados `AssemblyModel`, `AssemblyDiagnostic`, códigos de diagnóstico e opções de limites.
- Implementados SHA-256, nome, versão, TFM, arquitetura, referências, contagem de tipos/métodos, PDB, strong name e R2R.
- Implementados limites de tamanho, timeout e cancelamento.
- Implementado tratamento estruturado para arquivo ausente, PE inválido, metadata ausente e falhas de leitura.
- Harness atualizado para validar diretamente o `AssemblyReader` sem invocar métodos da fixture.
- PDB é tratado como metadado opcional; a ingestão depende de PE/metadata e preserva offsets para análise.
