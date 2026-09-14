# T03 — Criar fixtures e testes-base

**Fase:** 1 — Fundação  
**Status:** Concluída  
**Prioridade:** Crítica

## Objetivo
Criar assemblies pequenos para validar todas as etapas do analisador.

## Cobertura
Loops, EH, branches, alocações, boxing, payload externo, static retention, generics, delegates, strong name, R2R e formatos não suportados.

## Dependências
T01.

## Critérios de aceite
- Fixtures compilam independentemente.
- Nenhuma fixture é executada pelo analisador estático.
- Casos esperados possuem metadata/hash de referência.
- Harness executa testes determinísticos.

## Resultado da implementação

- Criada a fixture `tests/fixtures/ClrLens.SampleFixtures`.
- Cobertos loops, EH, branches, allocations, boxing, payload externo, static retention, generics e delegates.
- Criada fixture de input inválido em `tests/fixtures/unsupported/invalid-metadata.bin`.
- Criado manifesto em `tests/fixtures/fixture-manifest.json` com contagens e hash esperado.
- Convertido `ClrLens.Tests.IL` em harness executável baseado em `PEReader`/metadata.
- Harness não invoca métodos da fixture; apenas lê bytes, metadata e calcula SHA-256.
- Strong-name, R2R e NativeAOT ficaram documentados como artefatos de pipeline seguro, sem chaves privadas ou binários gerados no repositório.
