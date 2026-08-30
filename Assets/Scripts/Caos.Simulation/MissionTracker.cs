using Caos.Core;
using Caos.Data;
using Caos.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Rastreia e exibe a missão ativa (docs/07 + docs/08 T04), ativando o <see cref="MissionService"/> do backend.
    /// Aceita automaticamente a primeira missão disponível e, ao concluir, pega a próxima (cadeia por
    /// pré-requisitos). Toca o chime de sucesso via <see cref="AudioManager"/>.
    ///
    /// Resolução de objetivo (slice jogável): cada missão tem um "destino":
    ///  • se algum objetivo é "ir" ao veículo (van/veículo/uno) → conclui ao <b>entrar no veículo</b>;
    ///  • caso contrário → conclui ao <b>chegar ao bairro</b> do último objetivo (beacon no mundo).
    /// Objetivos intermediários são narrativos; a resolução jogável é "alcançar o destino".
    /// </summary>
    public class MissionTracker : MonoBehaviour
    {
        private const float kReachRadius = 16f;   // o destino é um bairro inteiro, não uma marca no chão

        private Transform         _player;
        private Transform         _vehicleT;
        private PlayerVehicleLink _link;
        private AudioManager      _audio;

        private MissionService _missions;
        private GameCatalogs   _catalogs;

        private string     _activeId;
        private MissionDto _active;
        private int        _passo;              // objetivo em curso dentro da missão
        private bool       _isVehicleMission;
        private Vector3    _dest;
        private string     _destLabel = "";

        /// <summary>Destino atual no mundo (o minimapa desenha o blip dourado aqui).</summary>
        public Vector3 Destino      => _isVehicleMission && _vehicleT != null ? _vehicleT.position : _dest;
        public bool    TemDestino   => _active != null;
        /// <summary>Título da missão ativa e nome do destino — usados pelo celular (app VaiJá).</summary>
        public string  TituloAtivo  => _active != null ? _active.titulo : "";
        public string  DestinoLabel => _isVehicleMission ? "o veículo" : _destLabel;

        // UI
        private float _refreshAccum;
        private Font _font;
        private Text _titleText, _objText, _rewardText;
        private GameObject _beacon;
        private TextMesh   _beaconLabel;

        public void Init(Transform player, PlayerVehicleLink link, Transform vehicle, AudioManager audio)
        {
            _player   = player;
            _link     = link;
            _vehicleT = vehicle;
            _audio    = audio;
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            BuildUi();
            BuildBeacon();
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _missions);
            ServiceLocator.TryGet(out _catalogs);
            AcceptNext();
        }

        private void OnEnable()  => EventBus<MissaoConcluida>.Subscribe(OnConcluida);
        private void OnDisable() => EventBus<MissaoConcluida>.Unsubscribe(OnConcluida);

        private void OnConcluida(MissaoConcluida e)
        {
            _audio?.Chime();
            if (e.id == _activeId) { _activeId = null; _active = null; }
            AcceptNext();
        }

        // -------------------------------------------------------- cadeia
        private void AcceptNext()
        {
            if (_missions == null || _catalogs == null) { ShowEmpty(); return; }

            // percorre a LISTA (ordem do JSON) e não o dicionário: a cadeia da campanha tem que
            // seguir M01, M02, M03... e Dictionary não garante ordem.
            //
            // 1º) retoma a missão que já estava em andamento no save — senão, quem carrega um jogo
            //     salvo no meio de uma missão fica com "sem missões disponíveis" pra sempre
            //     (ela não está concluída, e IsAvailable é falso justamente por estar ativa).
            string pick = null;
            bool retomada = false;
            for (int i = 0; i < _catalogs.Missions.Count; i++)
            {
                var m = _catalogs.Missions[i];
                if (m != null && _missions.IsActive(m.id)) { pick = m.id; retomada = true; break; }
            }
            // 2º) senão, aceita a próxima liberada pelos pré-requisitos
            if (pick == null)
            {
                for (int i = 0; i < _catalogs.Missions.Count; i++)
                {
                    var m = _catalogs.Missions[i];
                    if (m != null && _missions.IsAvailable(m.id)) { pick = m.id; break; }
                }
            }
            if (pick == null) { _activeId = null; _active = null; ShowNone(); return; }

            if (!retomada) _missions.Accept(pick);
            _activeId = pick;
            if (_catalogs.MissionById == null || !_catalogs.MissionById.TryGetValue(pick, out _active) || _active == null)
            { _activeId = null; _active = null; ShowNone(); return; }
            AnalyzeMission();
            RefreshPanel();
            Debug.Log("[Missão] Ativa: " + _active.titulo + " — " + (_isVehicleMission ? "entre no veículo" : "vá até " + _destLabel));
        }

        /// <summary>
        /// Prepara a missão para ser jogada <b>passo a passo</b>. Antes, todos os objetivos do JSON
        /// eram decorativos e a missão inteira resolvia como "chegue no bairro do último objetivo".
        /// Agora cada objetivo é um passo com destino e condição próprios, e o beacon, a rota do GPS
        /// e o painel acompanham o passo atual.
        /// </summary>
        private void AnalyzeMission()
        {
            _passo = 0;
            PrepararPasso();
        }

        /// <summary>Objetivo do passo atual (nulo se a missão não tem objetivos).</summary>
        private MissionObjectiveDto ObjetivoAtual =>
            _active != null && _active.objetivos != null && _passo < _active.objetivos.Count
                ? _active.objetivos[_passo] : null;

        public int  PassoAtual  => _passo + 1;
        public int  TotalPassos => _active != null && _active.objetivos != null ? Mathf.Max(1, _active.objetivos.Count) : 1;

        private void PrepararPasso()
        {
            var o = ObjetivoAtual;
            if (o == null)
            {
                // missão sem objetivos: cai no centro, e conclui ao chegar
                _isVehicleMission = false;
                _dest = AnchorFor("Centro");
                _destLabel = LocalLabel("Centro");
                return;
            }

            // "ir" até um veículo (van do Tonho, o próprio carro) resolve embarcando
            string alvo = o.alvo == null ? "" : o.alvo.ToLower();
            _isVehicleMission = o.tipo == "ir" &&
                                (alvo.Contains("van") || alvo.Contains("veic") || alvo.Contains("uno") || alvo.Contains("carro"));

            string local = string.IsNullOrEmpty(o.local) ? "Centro" : o.local;

            // se o alvo é um estabelecimento do catálogo, o destino é a LOJA, não o centro do bairro
            _dest      = AncoraDoAlvo(alvo, local);
            _destLabel = RotuloDoAlvo(alvo, local);
        }

        /// <summary>
        /// Destino do passo: primeiro tenta casar o alvo com um estabelecimento real da cidade
        /// (a barraca da Tia Marlene é um ponto concreto, não "o bairro da rodoviária"); se não casar,
        /// usa o centro do bairro.
        /// </summary>
        private Vector3 AncoraDoAlvo(string alvo, string local)
        {
            var loja = LojaPorId(alvo);
            if (loja != null) return loja.transform.position;
            return AnchorFor(local);
        }

        private string RotuloDoAlvo(string alvo, string local)
        {
            var loja = LojaPorId(alvo);
            if (loja != null) return loja.rotulo;
            return LocalLabel(local);
        }

        private Interactable LojaPorId(string alvo)
        {
            var gen = CityRuntime.Generator;
            if (gen == null || string.IsNullOrEmpty(alvo)) return null;
            for (int i = 0; i < gen.Shops.Count; i++)
            {
                var s = gen.Shops[i];
                if (s == null) continue;
                if (s.name.EndsWith(alvo, System.StringComparison.OrdinalIgnoreCase)) return s;
            }
            return null;
        }

        /// <summary>Verbo do passo, para o painel dizer o que fazer e não só onde ir.</summary>
        private string VerboDoPasso()
        {
            var o = ObjetivoAtual;
            if (o == null) return "Vá até";
            switch (o.tipo)
            {
                case "coletar": return "Pegue em";
                case "levar":   return "Entregue em";
                case "falar":   return "Fale com quem está em";
                default:        return "Vá até";
            }
        }

        private void Update()
        {
            if (_active == null || _missions == null)
            {
                if (_beacon != null) _beacon.SetActive(false);
                return;
            }

            bool passoCumprido;
            if (_isVehicleMission)
                passoCumprido = _link != null && !_link.OnFoot;                          // entrou no veículo
            else
                passoCumprido = _player != null && SqrHoriz(_player.position - _dest) <= kReachRadius * kReachRadius;

            if (_beacon != null)
            {
                _beacon.SetActive(true);
                _beacon.transform.position = (_isVehicleMission && _vehicleT != null) ? _vehicleT.position : _dest;
            }

            // a orientação (distância / próximo gesto) atualiza a 5 Hz, não a cada quadro
            _refreshAccum += Time.deltaTime;
            if (_refreshAccum >= 0.2f)
            {
                _refreshAccum = 0f;
                if (_objText != null) _objText.text = Orientacao();
            }

            if (!passoCumprido) return;

            // passo cumprido: avança. Só o ÚLTIMO conclui a missão — os do meio dão o chime curto
            // e mandam o jogador para o próximo destino (o GPS recalcula sozinho).
            int total = _active.objetivos != null ? _active.objetivos.Count : 0;
            if (_passo + 1 < total)
            {
                _passo++;
                PrepararPasso();
                RefreshPanel();
                _audio?.Chime();
                Debug.Log($"[Missão] Passo {_passo}/{total} de '{_active.titulo}' — agora: {VerboDoPasso()} {_destLabel}.");
                return;
            }

            _missions.Complete(_activeId);   // OnConcluida cuida do chime + próxima missão
        }

        // -------------------------------------------------------- UI
        private void RefreshPanel()
        {
            if (_active == null) { ShowEmpty(); return; }
            if (_titleText  != null) _titleText.text  = $"{_active.titulo}   ({PassoAtual}/{TotalPassos})";
            if (_objText    != null) _objText.text    = Orientacao();
            if (_rewardText != null) _rewardText.text = "R$ " + (_active.recompensaRs).ToString("F0") + "   ·   " + (_active.recompensaXp).ToString("F0") + " XP";
            if (_beaconLabel != null) _beaconLabel.text = _isVehicleMission ? "VEICULO" : _destLabel.ToUpper();
        }

        /// <summary>
        /// Dica de contexto: o painel não diz só "vá até X", ele diz o <b>próximo gesto</b> — pegar o
        /// carro, seguir a linha do mapa, ou que já chegou. É o que evita o jogador travar no começo.
        /// </summary>
        private string Orientacao()
        {
            if (_active == null) return "";
            if (_isVehicleMission) return "Entre no veículo  ·  [E]";

            float dist = _player != null ? Vector3.Distance(_player.position, _dest) : 0f;
            bool aPe = _link == null || _link.OnFoot;
            string verbo = VerboDoPasso();

            if (dist <= kReachRadius * 1.5f) return $"Chegou!  {verbo} {_destLabel}";
            if (aPe && dist > 120f)          return $"Pegue o carro [E]  ·  {verbo} {_destLabel}  ·  {dist:F0} m";
            return $"{verbo} {_destLabel}  ·  siga a linha azul  ·  {dist:F0} m";
        }

        private void ShowEmpty()
        {
            if (_titleText  != null) _titleText.text  = "Carregando missoes...";
            if (_objText    != null) _objText.text    = "";
            if (_rewardText != null) _rewardText.text = "";
            if (_beacon != null) _beacon.SetActive(false);
        }

        private void ShowNone()
        {
            if (_titleText  != null) _titleText.text  = "Sem missoes disponiveis";
            if (_objText    != null) _objText.text    = "Volte mais tarde";
            if (_rewardText != null) _rewardText.text = "";
            if (_beacon != null) _beacon.SetActive(false);
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("MissionUI", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var rt = new GameObject("Panel", typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(canvasGo.transform, false);
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -10);
            rt.sizeDelta = new Vector2(640, 116);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.5f); bg.raycastTarget = false;

            _titleText  = MakeText(rt, 30, new Color(1f, 0.95f, 0.5f), TextAnchor.UpperCenter, 6);
            _objText    = MakeText(rt, 22, Color.white,                TextAnchor.MiddleCenter, 44);
            _rewardText = MakeText(rt, 18, new Color(0.6f, 1f, 0.6f),  TextAnchor.LowerCenter,  78);
        }

        private Text MakeText(Transform parent, int size, Color c, TextAnchor align, float yFromTop)
        {
            var go = new GameObject("t", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -yFromTop);
            rt.sizeDelta = new Vector2(-16, size + 8);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.color = c; t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; t.text = "";
            return t;
        }

        private void BuildBeacon()
        {
            _beacon = new GameObject("MissionBeacon");
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(beam.GetComponent<Collider>());
            beam.transform.SetParent(_beacon.transform, false);
            beam.transform.localScale = new Vector3(2.2f, 26f, 2.2f);   // visível do outro lado da cidade
            beam.transform.localPosition = new Vector3(0f, 26f, 0f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            mat.color = new Color(1f, 0.85f, 0.2f);
            beam.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var lblGo = new GameObject("BeaconLabel");
            lblGo.transform.SetParent(_beacon.transform, false);
            lblGo.transform.localPosition = new Vector3(0f, 13.5f, 0f);
            _beaconLabel = lblGo.AddComponent<TextMesh>();
            _beaconLabel.fontSize = 48; _beaconLabel.characterSize = 0.5f;
            _beaconLabel.anchor = TextAnchor.MiddleCenter; _beaconLabel.color = new Color(1f, 0.85f, 0.2f);
            if (_font != null) _beaconLabel.font = _font;
            _beacon.SetActive(false);
        }

        // -------------------------------------------------------- anchors / labels
        private static float SqrHoriz(Vector3 v) { v.y = 0f; return v.sqrMagnitude; }

        /// <summary>Ponto de destino: o centro real do bairro na cidade gerada.</summary>
        private static Vector3 AnchorFor(string local)
        {
            var layout = CityRuntime.Layout;
            if (layout != null)
            {
                Vector3 c = layout.DistrictCenter(local);
                // o centro do morro não é dirigível: puxa o destino pra via mais próxima
                if (!layout.IsDrivable(c) && layout.TryNearestLanePoint(c, out var lane, out _)) return lane;
                return c;
            }
            return Vector3.zero;
        }

        private static string LocalLabel(string local)
        {
            var layout = CityRuntime.Layout;
            var dto = layout != null ? layout.DistrictById(local) : null;
            if (dto != null && !string.IsNullOrEmpty(dto.nome)) return dto.nome;

            switch (local)
            {
                case "VistaAlegre": return "Vista Alegre";
                case "MonteVerde":  return "Polo Monte Verde";
                case "SitioCapim":  return "Sitio do Capim";
                case "Belvedere":   return "Jardim Belvedere";
                case "Itauna":      return "Praia de Itauna";
                case "Rodoviaria":  return "Terminal Rodoviario";
                case "Marginal":    return "Marginal do Rio Sujo";
                case "Cohab":       return "COHAB Bom Retiro";
                default:            return "Centro";
            }
        }
    }
}
