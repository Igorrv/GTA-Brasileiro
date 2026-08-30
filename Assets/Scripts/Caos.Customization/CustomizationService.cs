using Caos.Core;
using UnityEngine;

namespace Caos.Customization
{
    /// <summary>Publicado no <see cref="EventBus{T}"/> quando o jogador confirma um novo visual —
    /// sistemas futuros (reação de NPC à aparência, docs/03 §1.7) podem assinar sem acoplar.</summary>
    public struct CustomizacaoSalvaEvt : IGameEvent
    {
        public int slot;
        public string genero;
        public string top;
    }

    /// <summary>
    /// Fachada estática da customização: guarda o catálogo e o visual atual, aplica no rig e
    /// persiste em PlayerPrefs (uma chave por slot — fora do SaveSystem de propósito, ver
    /// <see cref="CosmeticLoadout"/>).
    ///
    /// Não usa ServiceLocator porque o GameManager o limpa/registra no boot dele; um singleton
    /// estático simples não tem esse problema de ordem e mantém o wiring em zero arquivos alheios.
    /// </summary>
    public static class CustomizationService
    {
        public static CosmeticCatalog Catalogo { get; private set; }
        public static CosmeticLoadout Atual    { get; private set; } = new CosmeticLoadout();

        /// <summary>Verdadeiro quando o catálogo terminou de carregar (JSON ou fallback).</summary>
        public static bool Pronto => Catalogo != null;

        /// <summary>Dispara a carga do catálogo (idempotente — chamadas extras são ignoradas).</summary>
        public static void Iniciar(MonoBehaviour host)
        {
            if (Catalogo != null) return;
            CosmeticCatalog.LoadAsync(host, cat => Catalogo = cat);
        }

        /// <summary>Relê o visual salvo do slot da sessão (chamado quando a partida começa).</summary>
        public static void CarregarDoSlot()
        {
            Atual = CosmeticLoadout.Carregar(GameSession.Slot);
        }

        /// <summary>Aplica o visual atual num rig. Devolve os renderers extras (Look_*), se houver.</summary>
        public static System.Collections.Generic.List<Renderer> AplicarEm(Caos.Simulation.CharacterRig rig)
        {
            if (rig == null || Catalogo == null) return new System.Collections.Generic.List<Renderer>();
            return CharacterStyler.Aplicar(rig, Atual, Catalogo);
        }

        /// <summary>Aplica um rascunho (prévia da tela) sem salvar.</summary>
        public static System.Collections.Generic.List<Renderer> PreverEm(Caos.Simulation.CharacterRig rig, CosmeticLoadout rascunho)
        {
            if (rig == null || Catalogo == null || rascunho == null)
                return new System.Collections.Generic.List<Renderer>();
            return CharacterStyler.Aplicar(rig, rascunho, Catalogo);
        }

        /// <summary>Confirma o rascunho: vira o visual atual, persiste no slot e avisa o jogo.</summary>
        public static void Confirmar(CosmeticLoadout rascunho)
        {
            Atual.CopiarDe(rascunho);
            Atual.Salvar(GameSession.Slot);
            EventBus<CustomizacaoSalvaEvt>.Publish(new CustomizacaoSalvaEvt
            {
                slot   = GameSession.Slot,
                genero = Atual.genero,
                top    = Atual.top,
            });
            Debug.Log($"[Cosméticos] Visual salvo no slot {GameSession.Slot}: {Atual.genero}, {Atual.top}, {Atual.bottom}.");
        }
    }
}
