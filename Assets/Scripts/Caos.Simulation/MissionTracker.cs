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
            _active   = _catalogs.MissionById[pick];
            AnalyzeMission();
            RefreshPanel();
            Debug.Log("[Missão] Ativa: " + _active.titulo + " — " + (_isVehicleMission ? "entre no veículo" : "vá até " + _destLabel));
        }

        private void AnalyzeMission()
        {
            _isVehicleMission = false;
            if (_active == null || _active.objetivos == null || _active.objetivos.Count == 0)
            {
                _dest = Vector3.zero; _destLabel = "Centro"; return;
            }

            foreach (var o in _active.objetivos)
            {
                string alvo = o.alvo == null ? "" : o.alvo.ToLower();
                if (o.tipo == "ir" && (alvo.Contains("van") || alvo.Contains("veic") || alvo.Contains("uno")))
                {
                    _isVehicleMission = true;
                    break;
                }
            }

            string local = LastLocal();
            _dest      = AnchorFor(local);
            _destLabel = LocalLabel(local);
        }

        private string LastLocal()
        {
            if (_active == null || _active.objetivos == null || _active.objetivos.Count == 0) return "Centro";
            var last = _active.objetivos[_active.objetivos.Count - 1];
            return string.IsNullOrEmpty(last.local) ? "Centro" : last.local;
        }

        private void Update()
        {
            if (_active == null || _missions == null)
            {
                if (_beacon != null) _beacon.SetActive(false);
                return;
            }

            bool done;
            if (_isVehicleMission)
                done = _link != null && !_link.OnFoot;                                   // entrou no veículo
            else
                done = _player != null && SqrHoriz(_player.position - _dest) <= kReachRadius * kReachRadius;

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

            if (done) _missions.Complete(_activeId);   // OnConcluida cuida do chime + próxima
        }

        // -------------------------------------------------------- UI
        private void RefreshPanel()
        {
            if (_active == null) { ShowEmpty(); return; }
            if (_titleText  != null) _titleText.text  = _active.titulo;
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

            if (dist <= kReachRadius * 1.5f) return "Chegou! " + _destLabel;
            if (aPe && dist > 120f)          return $"Pegue o carro [E] e siga a linha azul  ·  {dist:F0} m";
            if (aPe)                         return $"Siga a linha azul até {_destLabel}  ·  {dist:F0} m";
            return $"Siga a linha azul até {_destLabel}  ·  {dist:F0} m";
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
