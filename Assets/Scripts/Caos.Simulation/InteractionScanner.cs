using System.Collections.Generic;
using Caos.Core;
using Caos.Data;
using Caos.Gameplay;
using Caos.World;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Detecta a <see cref="Interactable"/> mais próxima do jogador e executa a ação ao apertar F.
    /// Fecha o loop econômico (docs/05, docs/13 S4/S8):
    ///  • <b>Posto</b>  — abastece o veículo a R$/L (corrigido pelo IPC-Caos via <see cref="EconomyService.PriceFor"/>).
    ///  • <b>Oficina</b>— repara o motor (<see cref="VehicleHealth"/>) por R$.
    ///  • <b>Trabalho</b> — paga R$, avança o relógio e cobra Energia/Fome.
    ///  • <b>Comércio</b> (padaria, boteco, mercadinho, lotérica, farmácia, barraca) — compra um
    ///    <see cref="ItemDto"/> do catálogo e aplica fome/energia/sanidade/saúde.
    ///
    /// Detecção por distância (sem física) — barata e sem colisões espúrias.
    /// </summary>
    public class InteractionScanner : MonoBehaviour
    {
        private Transform          _player;
        private PlayerVehicleLink  _link;
        private VehicleController  _vehicle;
        private VehicleHealth      _health;
        private List<Interactable> _list;

        private EconomyService    _econ;
        private PlayerAttributes  _attrs;
        private TimeOfDayService  _time;
        private GameCatalogs      _catalogs;
        private PlayerActions     _acoes;

        public string Prompt      { get; private set; } = "";
        public string Toast       { get; private set; } = "";
        public float  ToastUntil  { get; private set; }

        // cache do prompt: o texto só é reconstruído quando o alvo (ou o estado de alcance) muda.
        // Antes, a interpolação de string + o EscolherItem rodavam a cada quadro, alocando GC
        // mesmo parado perto de uma loja (docs/12 §12.10 — evitar alloc em hot path).
        private Interactable _lastNear;
        private bool _lastInRange;

        public void Init(Transform player, PlayerVehicleLink link, VehicleController vehicle,
                         VehicleHealth health, List<Interactable> list)
        {
            _player  = player;
            _link    = link;
            _vehicle = vehicle;
            _health  = health;
            _list    = list;
            if (player != null) _acoes = player.GetComponent<PlayerActions>();
            ServiceLocator.TryGet(out _econ);
            ServiceLocator.TryGet(out _attrs);
            ServiceLocator.TryGet(out _time);
            ServiceLocator.TryGet(out _catalogs);
        }

        private void Update()
        {
            Interactable near = Nearest();
            bool alcance = near != null && Dist(near) <= near.radius;

            // só recompõe o prompt quando algo muda: trocou de loja, entrou/saiu do alcance.
            // No estado comum (parado ou andando longe) não aloca string nenhuma.
            if (near != _lastNear || alcance != _lastInRange)
            {
                _lastNear = near;
                _lastInRange = alcance;
                Prompt = alcance ? $"{near.rotulo}  —  [F] {Verb(near)}" : "";
            }

            if (alcance && GameInput.Use) Execute(near);
        }

        private Interactable Nearest()
        {
            if (_list == null) return null;
            Interactable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _list.Count; i++)
            {
                var it = _list[i];
                if (it == null) continue;
                float d = Dist(it);
                if (d < bestDist) { bestDist = d; best = it; }
            }
            return best;
        }

        private float Dist(Interactable it)
        {
            if (it == null || _player == null) return float.MaxValue;
            Vector3 a = _player.position;        a.y = 0f;
            Vector3 b = it.transform.position;   b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private string Verb(Interactable it)
        {
            switch (it.tipo)
            {
                case TipoInteracao.Posto:    return "abastecer";
                case TipoInteracao.Oficina:  return "reparar";
                case TipoInteracao.Trabalho: return $"fazer um turno (R$ {it.pagamento:F0})";
                case TipoInteracao.Loterica: return "tentar a sorte";
                default:                     return DescricaoDaCompra(it);
            }
        }

        /// <summary>"[F] comprar Pastel de Carne (R$ 12,40)" — o preço já vem com IPC-Caos.</summary>
        private string DescricaoDaCompra(Interactable it)
        {
            var item = EscolherItem(it);
            if (item == null) return "olhar a vitrine";
            float preco = _econ != null ? _econ.PriceFor(item.preco) : item.preco;
            return $"comprar {item.nome} (R$ {preco:F2})";
        }

        /// <summary>
        /// Escolhe o item mais útil do estabelecimento: prioriza o que resolve a carência mais grave
        /// (saúde baixa → remédio; fome → comida; energia → bebida). É o balconista sugerindo.
        /// </summary>
        private ItemDto EscolherItem(Interactable it)
        {
            if (_catalogs == null || it.itens == null || it.itens.Count == 0) return null;

            ItemDto melhor = null;
            float melhorNota = float.MinValue;
            for (int i = 0; i < it.itens.Count; i++)
            {
                if (!_catalogs.ItemById.TryGetValue(it.itens[i], out var item) || item == null) continue;

                float faltaFome    = _attrs != null ? 100f - _attrs.Fome     : 50f;
                float faltaSede    = _attrs != null ? 100f - _attrs.Sede     : 50f;
                float faltaEnergia = _attrs != null ? 100f - _attrs.Energia  : 50f;
                float faltaSan     = _attrs != null ? 100f - _attrs.Sanidade : 50f;
                float faltaSaude   = _attrs != null ? 100f - _attrs.Saude    : 50f;

                float nota = item.fome     * faltaFome
                           + item.sede     * faltaSede * 1.2f   // sede aperta antes da fome
                           + item.energia  * faltaEnergia
                           + item.sanidade * faltaSan
                           + item.saude    * faltaSaude * 1.5f;

                if (nota > melhorNota) { melhorNota = nota; melhor = item; }
            }
            return melhor ?? (_catalogs.ItemById.TryGetValue(it.itens[0], out var primeiro) ? primeiro : null);
        }

        private void Execute(Interactable it)
        {
            switch (it.tipo)
            {
                case TipoInteracao.Posto:    ExecPosto(it);    break;
                case TipoInteracao.Oficina:  ExecOficina(it);  break;
                case TipoInteracao.Trabalho: ExecTrabalho(it); break;
                default:                     ExecComercio(it); break;
            }
        }

        private void ExecPosto(Interactable it)
        {
            if (_vehicle == null) { Feedback("Sem veículo para abastecer."); return; }
            if (_link != null && _link.OnFoot) { Feedback("Chegue com o carro na bomba."); return; }

            float needed = _vehicle.TankLiters - _vehicle.Fuel;
            if (needed <= 0.05f) { Feedback("Tanque já está cheio."); return; }

            float cost = _econ != null ? _econ.PriceFor(it.precoBase) * needed : 0f;
            if (_econ == null || _econ.TrySpend(cost))
            {
                _vehicle.FillTank();
                Feedback($"{it.rotulo}: {needed:F1}L (−R$ {cost:F2}).  \"{it.bordao}\"");
            }
            else Feedback("Dinheiro insuficiente p/ abastecer.");
        }

        private void ExecOficina(Interactable it)
        {
            if (_health == null) { Feedback("Sem veículo para reparar."); return; }
            if (!_health.Broken && _health.Hp01 > 0.98f) { Feedback("Veículo em ótimo estado."); return; }

            float cost = _econ != null ? _econ.PriceFor(it.precoBase) : 0f;
            if (_econ == null || _econ.TrySpend(cost))
            {
                _health.RepairFull();
                Feedback($"{it.rotulo}: reparo completo (−R$ {cost:F2}).  \"{it.bordao}\"");
            }
            else Feedback("Dinheiro insuficiente p/ reparar.");
        }

        private void ExecTrabalho(Interactable it)
        {
            if (_attrs != null && _attrs.Energia < 8f) { Feedback("Você está exausto. Coma e descanse antes."); return; }

            if (_econ != null) _econ.Add(it.pagamento, 0f);
            if (_attrs != null)
            {
                _attrs.Apply("energia", -it.energiaCost);
                _attrs.Apply("fome",    -it.fomeCost);
            }
            if (_time != null) _time.AdvanceHours(it.horasTrabalho);
            Feedback($"{it.rotulo}: turno fechado, +R$ {it.pagamento:F2} (−{it.horasTrabalho:F0}h).");
        }

        private void ExecComercio(Interactable it)
        {
            var item = EscolherItem(it);
            if (item == null) { Feedback("Hoje não tem nada pra vender."); return; }

            float preco = _econ != null ? _econ.PriceFor(item.preco) : item.preco;
            if (_econ != null && preco > 0.01f && !_econ.TrySpend(preco))
            {
                Feedback($"Faltou grana: {item.nome} custa R$ {preco:F2}.");
                return;
            }

            // o gesto vem primeiro: o jogador leva à boca e SÓ ENTÃO o efeito entra.
            // Sem PlayerActions (ex.: pedestre de teste), aplica direto.
            if (_acoes == null) _acoes = FindObjectOfType<PlayerActions>();
            void Aplicar()
            {
                if (_attrs != null)
                {
                    if (!Mathf.Approximately(item.fome, 0f))     _attrs.Apply("fome",     item.fome);
                    if (!Mathf.Approximately(item.sede, 0f))     _attrs.Apply("sede",     item.sede);
                    if (!Mathf.Approximately(item.energia, 0f))  _attrs.Apply("energia",  item.energia);
                    if (!Mathf.Approximately(item.sanidade, 0f)) _attrs.Apply("sanidade", item.sanidade);
                    if (!Mathf.Approximately(item.saude, 0f))    _attrs.Apply("saude",    item.saude);
                }

                // lotérica: às vezes a fezinha dá certo (é o único item que devolve dinheiro)
                if (it.tipo == TipoInteracao.Loterica && Random.value < 0.12f)
                {
                    float premio = preco * Random.Range(8f, 40f);
                    _econ?.Add(premio);
                    Feedback($"DEU SORTE! {item.nome} pagou R$ {premio:F2}!");
                    return;
                }

                string fala = string.IsNullOrEmpty(it.bordao) ? "" : "  \"" + it.bordao + "\"";
                Feedback($"{item.nome} (−R$ {preco:F2}).{fala}");
            }

            if (_acoes != null) _acoes.Consumir(item, Aplicar);
            else                Aplicar();
        }

        private void Feedback(string msg)
        {
            Toast      = msg;
            ToastUntil = Time.time + 3f;
            Debug.Log("[Interação] " + msg);
        }
    }
}
