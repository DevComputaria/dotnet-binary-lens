# CFG, loops e Scalar Evolution

## Grafo de controle

O CFG é:

$$
CFG=(V,E_N,E_X)
$$

- $V$: basic blocks;
- $E_N$: fluxo normal;
- $E_X$: fluxo excepcional.

Calls e operações potencialmente excepcionais não precisam dividir todos os blocos, mas devem carregar possíveis arestas excepcionais para EH.

## Análises

- dominators;
- post-dominators;
- SCCs;
- backedges;
- natural loops;
- loop nesting;
- ciclos irreduzíveis.

## Scalar Evolution

Recorrências são representadas como:

```text
{Start,+,Step}<LoopId>
```

Para $i_0=0$ e $i_{k+1}=i_k+2$:

$$
i_k=2k$$

A distinção entre `BackedgeTakenCount` e `TripCount` deve permanecer explícita. Overflow e condições de entrada devem fazer parte do resultado.

## Terminação

```text
TERMINATION_PROVEN
TERMINATION_PROVEN_UNDER_ASSUMPTIONS
NON_TERMINATION_PROVEN
NON_TERMINATION_POSSIBLE
TERMINATION_UNKNOWN
```

Uma ranking function $R$ deve satisfazer:

$$
R(S)\geq0 \land R(S_{next})<R(S)
$$

Não encontrar $R$ produz `UNKNOWN`, não `NON_TERMINATION_PROVEN`.
