# Custos e evidências

## CPU estrutural

Para um bloco:

$$
C(B)=\sum_{i\in B}w(i)
$$

Para um método:

$$
C(Method)=\sum_{B\in CFG}Visits(B)\times C(B)
$$

`w(i)` é SCU, não nanossegundos.

## Caminhos

Sem profile, a frequência pode ser desconhecida. Com profile:

$$
E[C]=\sum_b P(b)C(b)
$$

No modo pessimista, usamos o caminho alcançável mais caro:

$$
C_{worst}(if)=\max(C_{then},C_{else})
$$

## Entradas externas

Valores vindos de rede, arquivo, banco ou serializer devem permanecer simbólicos quando não houver bound. Um contrato externo reduz o bound como `PROVEN_UNDER_ASSUMPTIONS` ou `STATIC_UPPER_BOUND`, nunca como fato universal.

## Score

Um Hot Path Score é heurístico:

$$
RiskScore=SeverityWeight\times FrequencyFactor\times CostFactor\times ConfidenceFactor
$$

Os fatores devem aparecer separadamente no relatório.
