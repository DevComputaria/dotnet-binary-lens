# CLR Lens fixtures

As fixtures são assemblies de entrada para análise estática. O harness deve ler PE/metadata e nunca invocar métodos dessas assemblies.

## Cobertura

- `ClrLens.SampleFixtures`: loops lineares/aninhados, branches, EH, allocations, boxing, stream materialization, generics, delegates e retenção por static collection.
- `unsupported/invalid-metadata.bin`: reservado para fixture de metadata inválida criada pelo pipeline de testes.
- `unsupported/native-aot/`: reservado para artefato NativeAOT gerado em ambiente que possua o toolchain.
- `unsupported/r2r/`: reservado para artefato ReadyToRun gerado durante a validação de publicação.
- Strong-name: deve ser produzido em pipeline seguro com chave de teste fora do repositório; nunca armazenar chave privada no Git.
