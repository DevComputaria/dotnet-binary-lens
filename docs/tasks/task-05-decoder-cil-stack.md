# T05 — Implementar decoder CIL e stack

**Fase:** 2 — PE, CIL e IR  
**Status:** Concluída  
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

## Resultado da implementação

- Criados modelos `CilInstruction`, `CilExceptionRegion`, `CilDiagnostic` e `DecodedMethodBody`.
- Criado `CilDecoder` usando `System.Reflection.Emit.OpCodes` como tabela de opcodes.
- Implementada leitura de operandos inline, branches curtos/longos, switch, tokens, strings, números e variáveis.
- Implementada validação de destinos de branch e stack height/underflow conservadora.
- Implementada preservação de regiões `try/catch/finally/filter`.
- Harness da T03 atualizado para decodificar todos os corpos da fixture sem executar seus métodos.
- Stack behavior variável de `call`/`ret` é diagnosticado como não resolvido e será refinado na IR tipada da T06.
