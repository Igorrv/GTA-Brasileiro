using Caos.Core;
using Caos.Data;

namespace Caos.Gameplay
{
    // Eventos de domínio trocados via EventBus<T>. (structs = zero alloc no publish)
    public struct AtributosMudou   : IGameEvent { public float fome; public float sede; public float energia; public float sanidade; public float saude; }
    public struct DinheiroMudou    : IGameEvent { public float rs; public float caosCash; }
    public struct ImpactAplicado   : IGameEvent { public AttributeImpact impacto; }
    public struct EventoDisparado  : IGameEvent { public string id; public string nome; public string opcao; public AttributeImpact impacto; }
    public struct MissaoAceita     : IGameEvent { public string id; }
    public struct MissaoConcluida  : IGameEvent { public string id; public float rs; public float xp; }
    public struct SanidadeBaixa    : IGameEvent { public float valor; }  // quando sanidade <= 15
    public struct PlayerMorreu     : IGameEvent { }
}
