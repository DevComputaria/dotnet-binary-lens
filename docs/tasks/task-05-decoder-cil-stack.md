# T05 — Implementar decoder CIL e stack

**Fase:** 2 — PE, CIL e IR  
**Status:** Não iniciado  
**Prioridade:** Crítica

## Objetivo
Decodificar method bodies, opcodes, tokens, assinaturas e regiões de exceção.

## Implementação
Criar decoder tipado, validar evaluation stack, tipos, alturas, branches e fluxo excepcional. Preservar offsets originais.

## Dependências
T04.

## Critérios de aceite
- Opcodes suportados são decodificados corretamente.
- Stack inconsistente gera erro localizado.
- Branch inválido é reportado.
- `try/catch/finally/filter` são preservados.
