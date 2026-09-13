# ADR-0002 — IR, CFG e fluxo excepcional

- **Status:** Aceita
- **Data:** 2026-09-13

## Contexto

CIL é stack-based e possui exceções implícitas e regiões EH. Trabalhar diretamente com `Instruction` criaria regras frágeis.

## Decisão

Criar IR própria, typed stack normalization, SSA-like values e:

$$
CFG=(V,E_N,E_X)
$$

`E_N` representa fluxo normal e `E_X` fluxo excepcional. Arestas excepcionais não transformam toda instrução em terminador, mas ficam disponíveis para análise e provas.

## Consequências

- Offsets IL permanecem na provenance.
- EH participa desde a primeira versão.
- Movimentação de calls/loads deve considerar throw behavior.
- Cecil fica isolado no rewriter.
