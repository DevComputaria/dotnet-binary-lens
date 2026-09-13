# Fundamentos matemáticos

## Objetivo

O CLR Lens usa engenharia de compiladores, interpretação abstrata, teoria de grafos, análise algorítmica, teoria de filas, modelo de memória do CLR e métodos formais.

## Camadas de conhecimento

```text
Semântica CIL/ECMA-335
  -> IR tipada
  -> CFG normal e excepcional
  -> SSA-like + MemorySSA-like
  -> Abstract State
  -> Fixed Point
  -> Loop/Range/Cardinality/Heap
  -> Cost Functions
  -> Findings
  -> Proof ou Recommendation
```

## Produto de domínios

$$
State = Intervals \times Congruence \times Nullability \times Cardinality \times Allocation \times Escape \times Alias \times Concurrency
$$

A análise rápida usa intervalos e congruências. Provas de transformações usam bit-vectors de largura fixa e SMT.

## Evidência epistemológica

```text
PROVEN
PROVEN_UNDER_ASSUMPTIONS
STATIC_UPPER_BOUND
STATIC_LOWER_BOUND
INFERRED
SYMBOLIC
ESTIMATED
OBSERVED
HEURISTIC
EXTERNAL_CONTRACT
UNKNOWN
```

A regra fundamental é:

$$
Static\ Proof \neq Static\ Estimate \neq Runtime\ Measurement
$$

## Documentos relacionados

- [Interpretação abstrata](abstract-interpretation.md)
- [CFG, loops e Scalar Evolution](cfg-loops.md)
- [Memória, GC e concorrência](memory-concurrency.md)
- [Provas e translation validation](formal-verification.md)
- [Custos e evidências](cost-and-evidence.md)
