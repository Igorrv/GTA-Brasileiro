# 14 — Customização de Personagem (implementação)

> **Status:** IMPLEMENTADO (S9). Nota técnica da fatia jogável de customização do protagonista —
> a tela **T02** de [08-interface-telas.md](08-interface-telas.md) aplicada sobre o rig procedural.
> Spec de design: [03-personagens.md](03-personagens.md) §1.2 · Escopo MVP: [13-mvp-roadmap.md](13-mvp-roadmap.md) §13.2.

## O que o jogador vê

- Botão flutuante **VISUAL** (canto superior direito) ou tecla **K** abrem a tela **PERSONAGEM**.
- Duas abas: **APARÊNCIA** (gênero, tom de pele, cabelo, cor do cabelo) e **ROUPAS** (tronco, pernas, calçado, cabeça).
- Cada linha tem setas **◀ ▶** (alvo de toque ≥ 56 px, zona do polegar) e amostra de cor.
- A prévia é o **próprio boneco no mundo**: cada troca aplica na hora; botões **⟲ ⟳** giram o personagem.
- **SALVAR** persiste no slot; **◄** volta sem salvar (desfaz a prévia). O jogo congela enquanto a tela está aberta.
- A pé apenas: dentro do veículo a tela avisa "desça do veículo para trocar de roupa".

## Conteúdo (catálogo `StreamingAssets/Data/cosmetics.json`)

| Categoria | Opções |
|---|---|
| Gênero | Masculino · Feminino · Não-binário (silhueta sutil; hitbox/animações idênticas, §1.2.1) |
| Tom de pele | 8 tons (MVP pede 6) |
| Cabelo | Raspado · Curto · Black Power · Moicano · Longo · Coque (MVP pede 4) |
| Cor do cabelo | Preto · Castanho · Loiro · Ruivo · Grisalho · Azul do Caos (fantasia) |
| Tronco | 9 peças: camisetas, regatas, camisa de linho, jaqueta de couro, uniforme de motoboy, moletom, vestido |
| Pernas | 8 peças: calças (jeans/moletom/sarja), bermudas, short, saias |
| Calçado | Tênis, chinelo, sandália, bota de couro (com cano procedural) |
| Cabeça | Sem nada · bonés · chapéu de palha · bandana |

## Como funciona por dentro

- **Assembly novo `Caos.Customization`** → referencia `Caos.Core` + `Caos.Simulation` (unidirecional; ninguém referencia de volta). Zero arquivos existentes editados.
- **`CharacterStyler`** reaplica o visual sobre o `CharacterRig` já montado: troca materiais cacheados da `CityPalette` (sem vazar Material), remolda as primitivas existentes (o "Cabelo" vira moicano trocando o `sharedMesh` pela malha built-in do cubo) e cria peças extras procedurais com prefixo `Look_` (saia evasê em dois cilindros, cano de bota). O estado base de cada peça é capturado na 1ª aplicação — trocar cem vezes nunca acumula distorção.
- **Persistência fora do SaveSystem** (PRs abertos): `PlayerPrefs`, chave `caos_look_slot{N}`, JSON via `JsonUtility`. Sem a chave, volta o padrão (a camiseta amarelinha de sempre — o visual default é idêntico ao original).
- **Boot sem wiring**: `CustomizationBootstrap` usa `RuntimeInitializeOnLoadMethod` (mesmo padrão do `GameBootstrapper`), espera o `WorldBuilder` montar o player e aplica o visual do slot.
- **Dentro do carro**: as peças `Look_` sincronizam a visibilidade com o corpo (o `PlayerVehicleLink` esconde o boneco ao dirigir e não conhece peças criadas depois do boot).
- **Evento**: `CustomizacaoSalvaEvt` no `EventBus` — pronto para NPCs reagirem à aparência (docs/03 §1.7).
- **Fallback**: se o JSON falhar, catálogo embutido equivalente garante a tela (mesmo contrato dos demais catálogos).

## Pós-MVP (fora desta fatia)

Tatuagens, barba, maquiagem, lojas de roupa com R$/CaosCash, bônus de conjunto (outfit sets) e a
árvore de habilidades da T02 (a aba "Habilidades" depende da progressão de XP).
