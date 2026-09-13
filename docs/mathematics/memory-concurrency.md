# Memória, GC e concorrência

## Grandezas separadas

```text
AllocationVolume
LiveManagedMemory
RetainedMemory
PeakWorkingSetEstimate
NativeMemory
```

Alocação total:

$$
AllocatedBytes=\sum_i Size_i\times Executions_i
$$

Memória viva:

$$
LiveBytes(p)=\sum_{a\in Live(p)}Size(a)
$$

## Abstract heap

O heap abstrato é um grafo $H=(O,R)$ de objetos e referências. GC roots podem ser static fields, stacks, handles e outros pontos conhecidos.

Retenção potencial:

$$
M(t)\approx M_0+\lambda st
$$

A conclusão deve ser marcada como potencial quando o alias ou o caminho de remoção não puder ser provado.

## LOH

O threshold deve ser configurável por `RuntimeModel.LOHThreshold`; 85.000 bytes é apenas o default documentado.

## Concorrência

Para $P$ operações simultâneas:

$$
PeakParallel\leq Base+P\times PeakTask
$$

Para chegada de trabalhos, usamos a Lei de Little:

$$
L=\lambda W
$$

Esses resultados são upper bounds ou estimativas, nunca working set observado sem runtime profile.
