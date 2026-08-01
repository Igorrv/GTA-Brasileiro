using Caos.Core;
using Caos.Data;
using Caos.World;

namespace Caos.Gameplay
{
    /// <summary>
    /// Roteia um <see cref="AttributeImpact"/> para os serviços corretos (atributos, economia,
    /// reputação, caos/estrelas). Ponto único de aplicação de consequências (eventos, missões, combate).
    /// </summary>
    public sealed class ImpactResolver
    {
        private readonly PlayerAttributes _attrs;
        private readonly EconomyService _econ;
        private readonly ReputationService _rep;
        private readonly WorldStateService _world;

        public ImpactResolver(PlayerAttributes attrs, EconomyService econ, ReputationService rep, WorldStateService world)
        {
            _attrs = attrs; _econ = econ; _rep = rep; _world = world;
        }

        public void Apply(AttributeImpact impact)
        {
            if (impact.fome != 0)     _attrs.Apply("fome", impact.fome);
            if (impact.sede != 0)     _attrs.Apply("sede", impact.sede);
            if (impact.energia != 0)  _attrs.Apply("energia", impact.energia);
            if (impact.sanidade != 0) _attrs.Apply("sanidade", impact.sanidade);
            if (impact.saude != 0)    _attrs.Apply("saude", impact.saude);

            if (impact.rs != 0 || impact.caosCash != 0)
                _econ.Add(impact.rs, impact.caosCash);

            if (impact.rep != null)
                foreach (var r in impact.rep)
                    _rep.ApplyDelta(r.alvo, r.delta);

            if (impact.caos != 0) _world.ApplyCaos(impact.caos);
            if (impact.stars != 0) _world.SetStars(_world.Stars + impact.stars);

            EventBus<ImpactAplicado>.Publish(new ImpactAplicado { impacto = impact });
        }
    }
}
