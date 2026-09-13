# Interpretação abstrata

## Ideia central

O analisador não executa todos os valores possíveis. Ele executa o programa sobre domínios abstratos. Se o estado concreto é $S$, usamos uma abstração:

$$
\alpha(S)=\hat{S}
$$

Cada instrução aplica uma função de transferência. Para intervalos:

$$
F_{add}([a,b],[c,d])=[a+c,b+d]
$$

## CFG e fixpoint

Para um bloco com múltiplos predecessores:

$$
S_B = \bigsqcup_{p \in Pred(B)} F_p(S_p)
$$

Loops exigem iteração até um ponto fixo. Quando o domínio pode crescer indefinidamente, usamos widening; narrowing pode refinar o resultado posteriormente.

## Estado abstrato

```text
Type
BitWidth
Signedness
Range
Congruence
Constant/Symbol
Nullability
Cardinality
AllocationSite
Escape
PointsTo
SideEffects
```

## Restrições

- Abstract interpretation é soundness-oriented, não promete completude.
- Ausência de prova não é prova de ausência.
- Valores desconhecidos devem propagar imprecisão.
- Overflow CIL precisa respeitar `int32`, `int64`, `native int`, signed/unsigned e checked/unchecked.
