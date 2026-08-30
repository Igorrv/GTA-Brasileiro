using System.Collections.Generic;
using Caos.Data;

namespace Caos.Gameplay
{
    /// <summary>
    /// Reputação por facção e por bairro (−100 a +100, docs/05 §C).
    /// `alvo` é o nome do enum <see cref="FactionId"/> ou <see cref="DistrictId"/>.
    /// </summary>
    public sealed class ReputationService
    {
        private readonly Dictionary<FactionId, int>  _faction  = new Dictionary<FactionId, int>();
        private readonly Dictionary<DistrictId, int> _district = new Dictionary<DistrictId, int>();

        public ReputationService() => Zerar();

        public int Get(string alvo) => TryGet(alvo, out var v) ? v : 0;

        public bool TryGet(string alvo, out int valor)
        {
            if (System.Enum.TryParse<FactionId>(alvo, true, out var f))
            { valor = _faction[f]; return true; }
            if (System.Enum.TryParse<DistrictId>(alvo, true, out var d))
            { valor = _district[d]; return true; }
            valor = 0; return false;
        }

        public void ApplyDelta(string alvo, int delta)
        {
            if (System.Enum.TryParse<FactionId>(alvo, true, out var f))
                _faction[f] = Clamp(_faction[f] + delta);
            else if (System.Enum.TryParse<DistrictId>(alvo, true, out var d))
                _district[d] = Clamp(_district[d] + delta);
        }

        /// <summary>Tono textual da reputação (para UI/diálogos).</summary>
        public string Tone(string alvo)
        {
            int v = Get(alvo);
            if (v >= 75) return "Ídolo";
            if (v >= 50) return "Aliado";
            if (v >= 25) return "Amigo";
            if (v <= -75) return "Odiado";
            if (v <= -50) return "Inimigo";
            if (v <= -25) return "Frio";
            return "Neutro";
        }

        public System.Collections.Generic.List<RepEntry> FactionSnapshot()
        {
            var r = new System.Collections.Generic.List<RepEntry>();
            foreach (var kv in _faction) r.Add(new RepEntry { alvo = kv.Key.ToString(), valor = kv.Value });
            return r;
        }
        public System.Collections.Generic.List<RepEntry> DistrictSnapshot()
        {
            var r = new System.Collections.Generic.List<RepEntry>();
            foreach (var kv in _district) r.Add(new RepEntry { alvo = kv.Key.ToString(), valor = kv.Value });
            return r;
        }

        /// <summary>
        /// Restaura estado a partir do save. Escreve o valor salvo <b>por cima</b> do atual, não soma:
        /// carregar um save é reposicionar o jogador onde ele parou, e somar faria a reputação dobrar
        /// se o save fosse aplicado sobre uma sessão que já tinha reputação (voltar ao menu e carregar,
        /// por exemplo).
        /// </summary>
        public void Hydrate(System.Collections.Generic.List<RepEntry> faction, System.Collections.Generic.List<RepEntry> district)
        {
            Zerar();
            if (faction != null)  foreach (var e in faction)  Definir(e.alvo, e.valor);
            if (district != null) foreach (var e in district) Definir(e.alvo, e.valor);
        }

        private void Zerar()
        {
            foreach (var f in System.Enum.GetValues(typeof(FactionId)))  _faction[(FactionId)f] = 0;
            foreach (var d in System.Enum.GetValues(typeof(DistrictId))) _district[(DistrictId)d] = 0;
        }

        private void Definir(string alvo, int valor)
        {
            if (System.Enum.TryParse<FactionId>(alvo, true, out var f))
                _faction[f] = Clamp(valor);
            else if (System.Enum.TryParse<DistrictId>(alvo, true, out var d))
                _district[d] = Clamp(valor);
        }

        public struct RepEntry { public string alvo; public int valor; }

        private static int Clamp(int v) => v < -100 ? -100 : (v > 100 ? 100 : v);
    }
}
