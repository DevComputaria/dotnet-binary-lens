# T03 — Criar fixtures e testes-base

**Fase:** 1 — Fundação  
**Status:** Não iniciado  
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
