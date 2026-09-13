# T16 — Implementar reescrita IL

**Fase:** 6 — Reescrita e validação  
**Status:** Não iniciado  
**Prioridade:** Média / opt-in

## Objetivo
Aplicar transformações simples apenas quando pré-condições forem satisfeitas.

## Passes iniciais
Constant folding/propagation, branch folding, remoção de NOPs, blocos inalcançáveis e cópias locais redundantes.

## Dependências
T06, T07, T09 e T15 estáveis.

## Critérios de aceite
- Original nunca é sobrescrito.
- Diff IL e proof object são gerados.
- UnknownEffect bloqueia transformação insegura.
- EH, metadata, stack e API são preservados.
