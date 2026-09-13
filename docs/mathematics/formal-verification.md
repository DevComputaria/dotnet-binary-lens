# Provas e translation validation

## Objetivo

Uma transformação $P\rightarrow P'$ só pode ser aplicada automaticamente quando a semântica observável é preservada no escopo definido.

$$
Semantic(P)=Semantic(P')
$$

ou, quando apropriado, $P'$ refina $P$.

## Duas camadas

```text
Fast analysis
  intervals/congruences

Proof analysis
  symbolic semantics + SMT bit-vectors
```

Bit-vectors são necessários para signed/unsigned, overflow, shifts e largura fixa.

## Resultados

```text
PROVEN
COUNTEREXAMPLE
UNKNOWN/TIMEOUT
```

Um contraexemplo deve registrar valores de entrada, resultado original, resultado transformado e razão da divergência.

## Invariantes

- evaluation stack válida;
- branch targets válidos;
- tipos compatíveis;
- EH preservado;
- metadata preservada;
- side effects observáveis preservados;
- volatile semantics preservada;
- API pública inalterada.

ILVerify é o gate estrutural pós-escrita; testes diferenciais e benchmark A/B são gates adicionais, não substitutos da prova.
