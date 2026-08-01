# Segurança

## Versões suportadas

| Branch / release | Suporte de segurança |
|---|---|
| `main` (Unity 6 slice atual) | ✅ |

## Como reportar

Se encontrar vulnerabilidade que afete builds, saves, integrações futuras de cloud/IAP ou dados do jogador:

1. **Não** abra issue pública com exploit.
2. Descreva impacto, passos de reprodução e versão do Editor/commit.
3. Envie para os mantenedores (CaosStudio) por canal privado.

Resposta esperada em até **7 dias úteis**.

## Fora de escopo (por enquanto)

- Cheats locais em single-player offline
- Balanceamento de economia / missões (use issues de design)
- Crashes sem vetor de abuso de dados

## Builds mobile

Não commite keystores, `google-services.json`, certificados ou secrets. Use variáveis de ambiente / secrets do CI.
