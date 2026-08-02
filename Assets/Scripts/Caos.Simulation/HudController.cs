using System.Collections.Generic;
using Caos.Core;
using Caos.Data;
using Caos.Gameplay;
using Caos.World;
using UnityEngine;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// HUD (docs/08 T01/T02) montado em runtime, sem prefab. Liga-se aos eventos de domínio via
    /// <see cref="EventBus{T}"/> para atualizar atributos/dinheiro/caos/estrelas no instante, e faz um
    /// poll throttled (10 Hz) só para relógio, bairro, logradouro, painel do veículo e prompt —
    /// evitando alocação de string por frame (docs/12 §12.7).
    ///
    /// Layout: atributos em cima à esquerda; dinheiro, relógio, bairro/rua e o procurado (estrelas
    /// desenhadas, sem depender de glifo da fonte) em cima à direita; painel do veículo embaixo à
    /// direita (o radar ocupa a esquerda); rádio no topo; e um feed de notificações acima do prompt.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        private PlayerVehicleLink  _link;
        private VehicleController  _vehicle;
        private VehicleHealth      _health;
        private InteractionScanner _scanner;
        private Transform          _player;
        private RadioSystem        _radio;

        private PlayerAttributes  _attrs;
        private EconomyService    _econ;
        private WorldStateService _world;
        private TimeOfDayService  _time;
        private ExperienceService _exp;
        private Font              _font;

        // valores em cache (atualizados por evento ou poll)
        private float _fome, _sede, _energia, _san, _saude, _rs, _cc, _caos;
        private int   _stars;

        // referências de UI
        private Image _barSaude, _barFome, _barSede, _barEnergia, _barSan;
        private Text  _valSaude, _valFome, _valSede, _valEnergia, _valSan;
        private Text  _moneyText, _clockText, _districtText, _streetText, _caosText, _wantedText;
        private Text  _nivelText;
        private Image _barXp;
        private readonly Image[] _estrelas = new Image[5];
        private GameObject _promptGo, _toastGo, _radioGo;
        private Text  _promptText, _toastText, _radioEstacao, _radioFaixa;
        private readonly List<Text> _feed = new List<Text>();
        private readonly List<float> _feedAte = new List<float>();
        private float _pollAccum;

        private static readonly Color kOuro   = new Color(1f, 0.84f, 0.28f);
        private static readonly Color kApagado= new Color(0.28f, 0.28f, 0.30f, 0.85f);

        public void Init(PlayerVehicleLink link, VehicleController vehicle, VehicleHealth health,
                         InteractionScanner scanner, Transform player, RadioSystem radio)
        {
            _link = link; _vehicle = vehicle; _health = health; _scanner = scanner;
            _player = player; _radio = radio;
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            BuildUi();
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _attrs);
            ServiceLocator.TryGet(out _econ);
            ServiceLocator.TryGet(out _world);
            ServiceLocator.TryGet(out _time);
            ServiceLocator.TryGet(out _exp);

            if (_attrs != null) { _fome = _attrs.Fome; _sede = _attrs.Sede; _energia = _attrs.Energia; _san = _attrs.Sanidade; _saude = _attrs.Saude; }
            if (_econ != null)  { _rs = _econ.Rs; _cc = _econ.CaosCash; }
            if (_world != null) { _caos = _world.Caos; _stars = _world.Stars; }
            if (_exp != null) OnXp(new XpMudou { xp = _exp.Xp, nivel = _exp.Nivel, progresso = _exp.Progresso01 });
            RefreshAll();
        }

        // ---------------- eventos ----------------
        private void OnEnable()
        {
            EventBus<AtributosMudou>.Subscribe(OnAttrs);
            EventBus<DinheiroMudou>.Subscribe(OnMoney);
            EventBus<CaosMudou>.Subscribe(OnCaos);
            EventBus<EstrelasMudou>.Subscribe(OnStars);
            EventBus<EventoDisparado>.Subscribe(OnEvento);
            EventBus<MissaoConcluida>.Subscribe(OnMissao);
            EventBus<XpMudou>.Subscribe(OnXp);
            EventBus<SubiuDeNivel>.Subscribe(OnNivel);
        }
        private void OnDisable()
        {
            EventBus<AtributosMudou>.Unsubscribe(OnAttrs);
            EventBus<DinheiroMudou>.Unsubscribe(OnMoney);
            EventBus<CaosMudou>.Unsubscribe(OnCaos);
            EventBus<EstrelasMudou>.Unsubscribe(OnStars);
            EventBus<EventoDisparado>.Unsubscribe(OnEvento);
            EventBus<MissaoConcluida>.Unsubscribe(OnMissao);
            EventBus<XpMudou>.Unsubscribe(OnXp);
            EventBus<SubiuDeNivel>.Unsubscribe(OnNivel);
        }
        private void OnAttrs(AtributosMudou e)  { _fome = e.fome; _sede = e.sede; _energia = e.energia; _san = e.sanidade; _saude = e.saude; RefreshAttributes(); }
        private void OnMoney(DinheiroMudou e)   { _rs = e.rs; _cc = e.caosCash; RefreshMoney(); }
        private void OnCaos(CaosMudou e)        { _caos = e.valor; RefreshCaosStars(); }
        private void OnStars(EstrelasMudou e)
        {
            bool subiu = e.valor > _stars;
            _stars = e.valor;
            RefreshCaosStars();
            if (subiu) Notificar(PoliceSystem.NomeDoNivel(_stars), new Color(1f, 0.45f, 0.4f));
        }
        private void OnXp(XpMudou e)
        {
            if (_barXp != null) _barXp.fillAmount = e.progresso;
            if (_nivelText != null && _exp != null) _nivelText.text = $"Nível {e.nivel}  ·  {_exp.Titulo}";
        }
        private void OnNivel(SubiuDeNivel e) => Notificar($"Subiu para o nível {e.nivel} — {e.titulo}", kOuro);

        private void OnEvento(EventoDisparado e) => Notificar(e.nome + " — " + e.opcao, new Color(0.75f, 0.85f, 1f));
        private void OnMissao(MissaoConcluida e) => Notificar($"Missão concluída  ·  +R$ {e.rs:F0}", new Color(0.6f, 1f, 0.65f));

        /// <summary>Empurra uma linha no feed de notificações (canto inferior central).</summary>
        public void Notificar(string msg, Color cor)
        {
            if (string.IsNullOrEmpty(msg)) return;
            for (int i = _feed.Count - 1; i > 0; i--)
            {
                _feed[i].text  = _feed[i - 1].text;
                _feed[i].color = _feed[i - 1].color;
                _feedAte[i]    = _feedAte[i - 1];
            }
            _feed[0].text  = msg;
            _feed[0].color = cor;
            _feedAte[0]    = Time.time + 6f;
        }

        // ---------------- loop (throttled) ----------------
        private void Update()
        {
            _pollAccum += Time.deltaTime;
            if (_pollAccum < 0.1f) return;
            _pollAccum = 0f;
            PollDynamic();
        }

        private void PollDynamic()
        {
            if (_time != null)
            {
                int h = (int)_time.Hour;
                int m = (int)((_time.Hour - h) * 60f);
                _clockText.text = $"Dia {_time.Day}   {h:00}:{m:00}";
            }

            // bairro pela posição real na cidade (e mantém o backend em dia)
            var layout = CityRuntime.Layout;
            if (layout != null && _player != null)
            {
                string id = layout.DistrictIdAt(_player.position);
                var dto = layout.DistrictById(id);
                if (_districtText != null)
                    _districtText.text = (dto != null ? dto.nome : id) + (_world != null ? "  ·  " + Clima(_world.Weather) : "");
                if (_streetText != null)
                    _streetText.text = layout.StreetNameAt(_player.position);

                if (_world != null && System.Enum.TryParse<DistrictId>(id, out var did) && _world.CurrentDistrict != did)
                    _world.CurrentDistrict = did;
            }
            else if (_world != null && _districtText != null)
            {
                _districtText.text = _world.CurrentDistrict + "  ·  " + Clima(_world.Weather);
            }

            RefreshAttributes();   // mantém o pisca das necessidades críticas vivo

            // sol forte dá mais sede (o backend só precisa saber que está calor)
            if (_attrs != null && _world != null)
                _attrs.Calor = _world.Weather == WeatherState.SolForte;

            // o painel do veículo agora é o DashboardUI (mostradores redondos embaixo ao centro)

            // rádio
            bool radioNoAr = _radio != null && _radio.NoAr;
            if (_radioGo != null) _radioGo.SetActive(radioNoAr);
            if (radioNoAr)
            {
                _radioEstacao.text  = _radio.Estacao;
                _radioEstacao.color = _radio.Cor;
                _radioFaixa.text    = _radio.FalaNoAr ? "\"" + _radio.Locucao + "\"" : _radio.Faixa;
            }

            string prompt = _scanner != null ? _scanner.Prompt : "";
            if (_promptGo != null) _promptGo.SetActive(!string.IsNullOrEmpty(prompt));
            if (!string.IsNullOrEmpty(prompt) && _promptText != null) _promptText.text = prompt;

            bool toast = _scanner != null && Time.time < _scanner.ToastUntil;
            if (_toastGo != null) _toastGo.SetActive(toast);
            if (toast && _toastText != null) _toastText.text = _scanner.Toast;

            for (int i = 0; i < _feed.Count; i++)
            {
                bool vivo = Time.time < _feedAte[i];
                if (_feed[i].gameObject.activeSelf != vivo) _feed[i].gameObject.SetActive(vivo);
            }
        }

        private static string Clima(WeatherState w)
        {
            switch (w)
            {
                case WeatherState.SolForte:   return "Sol forte";
                case WeatherState.SolLeve:    return "Sol entre nuvens";
                case WeatherState.Garoa:      return "Garoa";
                case WeatherState.Chuva:      return "Chuva";
                case WeatherState.Tempestade: return "Temporal";
                case WeatherState.Enchente:   return "Alagamento";
                case WeatherState.Neblina:    return "Neblina";
                default:                      return w.ToString();
            }
        }

        // ---------------- refresh ----------------
        private void RefreshAll() { RefreshAttributes(); RefreshMoney(); RefreshCaosStars(); PollDynamic(); }

        private void RefreshAttributes()
        {
            Preencher(_barSaude,   _valSaude,   _saude,   new Color(0.90f, 0.25f, 0.30f));
            Preencher(_barFome,    _valFome,    _fome,    new Color(0.92f, 0.58f, 0.20f));
            Preencher(_barSede,    _valSede,    _sede,    new Color(0.25f, 0.68f, 0.92f));
            Preencher(_barEnergia, _valEnergia, _energia, new Color(0.35f, 0.78f, 0.42f));
            Preencher(_barSan,     _valSan,     _san,     new Color(0.62f, 0.52f, 0.95f));
        }

        /// <summary>Preenche a barra e pisca de vermelho quando a necessidade entra em zona crítica.</summary>
        private static void Preencher(Image barra, Text valor, float v, Color cor)
        {
            if (barra != null)
            {
                barra.fillAmount = v / 100f;
                barra.color = v <= 20f
                    ? Color.Lerp(cor, new Color(1f, 0.18f, 0.15f), 0.5f + 0.5f * Mathf.Sin(Time.time * 6f))
                    : cor;
            }
            if (valor != null) valor.text = Mathf.RoundToInt(v).ToString();
        }
        private void RefreshMoney()
        {
            if (_moneyText != null) _moneyText.text = $"R$ {_rs:N2}     CC$ {_cc:F0}";
        }
        private void RefreshCaosStars()
        {
            if (_caosText != null) _caosText.text = $"Caos {(int)_caos}";
            for (int i = 0; i < _estrelas.Length; i++)
                if (_estrelas[i] != null) _estrelas[i].color = i < _stars ? kOuro : kApagado;
            if (_wantedText != null)
            {
                _wantedText.text  = PoliceSystem.NomeDoNivel(_stars);
                _wantedText.color = new Color(1f, 0.45f, 0.4f);
            }
        }

        // ---------------- construção da UI ----------------
        private void BuildUi()
        {
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Tudo do HUD pendura num painel de área segura: em celular com notch, o dinheiro ficava
            // atrás do recorte e o radar era comido pelo canto arredondado.
            var seguro = new GameObject("AreaSegura", typeof(RectTransform));
            var seguroRt = (RectTransform)seguro.transform;
            seguroRt.SetParent(canvas.transform, false);
            SafeArea.Aplicar(seguroRt, 10f);
            Transform root = seguroRt;

            // ---- necessidades (sup. esquerdo), em painel alinhado: rótulo | barra | valor ----
            var att = Child("Necessidades", root);
            att.anchorMin = new Vector2(0, 1); att.anchorMax = new Vector2(0, 1);
            att.pivot = new Vector2(0, 1);
            att.anchoredPosition = new Vector2(24, -24);
            att.sizeDelta = new Vector2(330, 216);
            var attBg = att.gameObject.AddComponent<Image>();
            Cartao(attBg, 0.42f);

            // faixa de nível no topo do painel: título + barra de XP
            var faixaXp = Child("Nivel", att);
            faixaXp.anchorMin = new Vector2(0, 1); faixaXp.anchorMax = new Vector2(1, 1);
            faixaXp.pivot = new Vector2(0.5f, 1);
            faixaXp.anchoredPosition = new Vector2(0, 2);
            faixaXp.sizeDelta = new Vector2(0, 40);

            _nivelText = Texto(faixaXp, "NivelTxt", new Vector2(0f, 1), 17, kOuro, TextAnchor.UpperLeft, -4);
            ((RectTransform)_nivelText.transform).anchoredPosition = new Vector2(10, -4);

            var xpBg = Child("Xp_bg", faixaXp);
            xpBg.anchorMin = new Vector2(0, 1); xpBg.anchorMax = new Vector2(1, 1);
            xpBg.pivot = new Vector2(0, 1);
            xpBg.anchoredPosition = new Vector2(10, -26);
            xpBg.sizeDelta = new Vector2(-20, 8);
            var xpBgImg = xpBg.gameObject.AddComponent<Image>();
            xpBgImg.color = new Color(0f, 0f, 0f, 0.6f); xpBgImg.raycastTarget = false;

            var xpFill = Child("Xp_fill", xpBg);
            xpFill.anchorMin = Vector2.zero; xpFill.anchorMax = Vector2.one;
            xpFill.offsetMin = new Vector2(1f, 1f); xpFill.offsetMax = new Vector2(-1f, -1f);
            _barXp = xpFill.gameObject.AddComponent<Image>();
            _barXp.color = kOuro; _barXp.raycastTarget = false;
            _barXp.type = Image.Type.Filled; _barXp.fillMethod = Image.FillMethod.Horizontal; _barXp.fillAmount = 0f;

            _barSaude   = Necessidade(att, "Vida",     new Color(0.90f, 0.25f, 0.30f), 0, out _valSaude);
            _barFome    = Necessidade(att, "Fome",     new Color(0.92f, 0.58f, 0.20f), 1, out _valFome);
            _barSede    = Necessidade(att, "Sede",     new Color(0.25f, 0.68f, 0.92f), 2, out _valSede);
            _barEnergia = Necessidade(att, "Energia",  new Color(0.35f, 0.78f, 0.42f), 3, out _valEnergia);
            _barSan     = Necessidade(att, "Sanidade", new Color(0.62f, 0.52f, 0.95f), 4, out _valSan);

            // ---- dinheiro / relógio / bairro / rua (sup. direito) ----
            var topR = Child("SuperiorDireito", root);
            Anchors(topR, new Vector2(0.62f, 1), new Vector2(1, 1));
            topR.offsetMin = new Vector2(0, -230);
            topR.offsetMax = new Vector2(-24, -20);
            _moneyText    = Texto(topR, "Dinheiro", new Vector2(1, 1), 38, kOuro, TextAnchor.UpperRight, -4);
            _clockText    = Texto(topR, "Relogio",  new Vector2(1, 1), 26, Color.white, TextAnchor.UpperRight, -50);
            _districtText = Texto(topR, "Bairro",   new Vector2(1, 1), 23, new Color(0.88f, 0.88f, 0.88f), TextAnchor.UpperRight, -84);
            _streetText   = Texto(topR, "Rua",      new Vector2(1, 1), 20, new Color(0.70f, 0.70f, 0.72f), TextAnchor.UpperRight, -112);

            // ---- procurado: 5 estrelas desenhadas (losangos) + nome do nível ----
            var wanted = Child("Procurado", topR);
            wanted.anchorMin = new Vector2(1, 1); wanted.anchorMax = new Vector2(1, 1);
            wanted.pivot = new Vector2(1, 1);
            wanted.anchoredPosition = new Vector2(0, -142);
            wanted.sizeDelta = new Vector2(200, 32);
            for (int i = 0; i < 5; i++)
            {
                var e = Child("Estrela" + i, wanted);
                e.anchorMin = new Vector2(1, 1); e.anchorMax = new Vector2(1, 1);
                e.pivot = new Vector2(1, 1);
                e.anchoredPosition = new Vector2(-i * 34f, 0f);
                e.sizeDelta = new Vector2(24, 24);
                e.localRotation = Quaternion.Euler(0, 0, 45f);
                var img = e.gameObject.AddComponent<Image>();
                img.color = kApagado; img.raycastTarget = false;
                _estrelas[i] = img;
            }
            _wantedText = Texto(topR, "NivelProcurado", new Vector2(1, 1), 20, new Color(1f, 0.45f, 0.4f), TextAnchor.UpperRight, -178);
            _caosText   = Texto(topR, "Caos", new Vector2(1, 1), 20, new Color(1f, 0.6f, 0.55f), TextAnchor.UpperRight, -204);

            // (o painel do veículo é o DashboardUI, num canvas próprio embaixo ao centro)

            // ---- rádio (topo central) ----
            var radioRt = Child("Radio", root);
            Anchors(radioRt, new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            radioRt.pivot = new Vector2(0.5f, 1);
            radioRt.anchoredPosition = new Vector2(0, -136);
            radioRt.sizeDelta = new Vector2(620, 62);
            var rbg = radioRt.gameObject.AddComponent<Image>();
            Cartao(rbg, 0.48f);
            _radioGo = radioRt.gameObject;
            _radioEstacao = Texto(radioRt, "Estacao", new Vector2(0.5f, 1), 22, Color.white, TextAnchor.UpperCenter, -4);
            _radioFaixa   = Texto(radioRt, "Faixa",   new Vector2(0.5f, 1), 18, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperCenter, -32);
            _radioGo.SetActive(false);

            // ---- feed de notificações (acima do prompt) ----
            var feedRt = Child("Feed", root);
            Anchors(feedRt, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            feedRt.pivot = new Vector2(0.5f, 0);
            feedRt.anchoredPosition = new Vector2(0, 132);
            feedRt.sizeDelta = new Vector2(900, 110);
            for (int i = 0; i < 3; i++)
            {
                var t = Texto(feedRt, "Linha" + i, new Vector2(0.5f, 1), 22, Color.white, TextAnchor.UpperCenter, -i * 30f);
                t.gameObject.SetActive(false);
                _feed.Add(t);
                _feedAte.Add(0f);
            }

            // ---- prompt (centro inferior) ----
            var promptRt = Child("Prompt", root);
            Anchors(promptRt, new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            promptRt.anchoredPosition = new Vector2(0, 72);
            promptRt.sizeDelta = new Vector2(900, 52);
            var pbg = promptRt.gameObject.AddComponent<Image>();
            Cartao(pbg, 0.52f);
            _promptGo = promptRt.gameObject;
            _promptText = Texto(promptRt, "PromptTxt", new Vector2(0.5f, 0.5f), 28, new Color(1f, 1f, 0.75f), TextAnchor.MiddleCenter, 0);

            // ---- toast (centro) ----
            var toastRt = Child("Toast", root);
            Anchors(toastRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            toastRt.anchoredPosition = new Vector2(0, -120);
            toastRt.sizeDelta = new Vector2(980, 60);
            _toastGo = toastRt.gameObject;
            _toastText = Texto(toastRt, "ToastTxt", new Vector2(0.5f, 0.5f), 30, Color.white, TextAnchor.MiddleCenter, 0);
            _toastGo.SetActive(false);
            _promptGo.SetActive(false);
        }


        /// <summary>
        /// Aplica o visual de cartão do HUD: canto arredondado (sprite gerado em runtime) em vez do
        /// retângulo duro padrão do uGUI. É o detalhe que separa "protótipo" de "interface".
        /// </summary>
        private static void Cartao(Image img, float opacidade)
        {
            img.sprite = UiTextures.Arredondado(0.16f);
            img.type   = Image.Type.Sliced;
            img.color  = new Color(0.03f, 0.035f, 0.05f, opacidade);
            img.raycastTarget = false;
        }

        // ---- helpers de layout ----
        private static RectTransform Child(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        private static void Anchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min; rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }
        private Text Texto(Transform parent, string name, Vector2 anchor, int size, Color c, TextAnchor align, float yOffset)
        {
            var rt = Child(name, parent);
            rt.anchorMin = new Vector2(anchor.x, 1); rt.anchorMax = new Vector2(anchor.x, 1);
            rt.pivot = new Vector2(anchor.x, 1);
            rt.anchoredPosition = new Vector2(0, yOffset);
            rt.sizeDelta = new Vector2(0, size + 8);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.color = c; t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; t.text = "";
            return t;
        }
        /// <summary>
        /// Linha de necessidade com colunas fixas — rótulo (90 px), barra (elástica) e valor (36 px).
        /// Colunas fixas é o que faz as cinco linhas ficarem alinhadas em qualquer resolução.
        /// </summary>
        private Image Necessidade(RectTransform parent, string rotulo, Color cor, int linha, out Text valor)
        {
            const float alturaLinha = 32f, margem = 10f, colRotulo = 92f, colValor = 40f;
            const float topoNivel = 40f;                 // a faixa de XP ocupa o topo do painel
            float y = -topoNivel - margem - linha * alturaLinha;

            var lbl = Child(rotulo + "_lbl", parent);
            lbl.anchorMin = new Vector2(0, 1); lbl.anchorMax = new Vector2(0, 1);
            lbl.pivot = new Vector2(0, 1);
            lbl.anchoredPosition = new Vector2(margem, y);
            lbl.sizeDelta = new Vector2(colRotulo, alturaLinha - 8f);
            var lt = lbl.gameObject.AddComponent<Text>();
            lt.font = _font; lt.fontSize = 18; lt.color = new Color(0.86f, 0.86f, 0.88f);
            lt.alignment = TextAnchor.MiddleLeft; lt.raycastTarget = false; lt.text = rotulo;

            var bg = Child(rotulo + "_bg", parent);
            bg.anchorMin = new Vector2(0, 1); bg.anchorMax = new Vector2(1, 1);
            bg.pivot = new Vector2(0, 1);
            bg.anchoredPosition = new Vector2(margem + colRotulo, y - 5f);
            bg.sizeDelta = new Vector2(-(margem * 2f + colRotulo + colValor), 16f);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.55f); bgImg.raycastTarget = false;

            var fillRt = Child(rotulo + "_fill", bg);
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2f, 2f); fillRt.offsetMax = new Vector2(-2f, -2f);
            var fImg = fillRt.gameObject.AddComponent<Image>();
            fImg.color = cor; fImg.raycastTarget = false;
            fImg.type = Image.Type.Filled; fImg.fillMethod = Image.FillMethod.Horizontal; fImg.fillAmount = 1f;

            var val = Child(rotulo + "_val", parent);
            val.anchorMin = new Vector2(1, 1); val.anchorMax = new Vector2(1, 1);
            val.pivot = new Vector2(1, 1);
            val.anchoredPosition = new Vector2(-margem, y);
            val.sizeDelta = new Vector2(colValor, alturaLinha - 8f);
            valor = val.gameObject.AddComponent<Text>();
            valor.font = _font; valor.fontSize = 18; valor.color = Color.white;
            valor.alignment = TextAnchor.MiddleRight; valor.raycastTarget = false; valor.text = "0";

            return fImg;
        }

        private Image Bar(RectTransform parent, string label, Color fillCol, float yOffset)
        {
            var bg = Child(label + "_bg", parent);
            bg.anchorMin = new Vector2(0, 1); bg.anchorMax = new Vector2(1, 1);
            bg.pivot = new Vector2(0, 1);
            bg.anchoredPosition = new Vector2(8, yOffset);
            bg.sizeDelta = new Vector2(-16, 20);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.55f); bgImg.raycastTarget = false;

            var lbl = Child(label + "_lbl", parent);
            lbl.anchorMin = new Vector2(0, 1); lbl.anchorMax = new Vector2(0, 1);
            lbl.pivot = new Vector2(0, 1);
            lbl.anchoredPosition = new Vector2(8, yOffset - 19);
            lbl.sizeDelta = new Vector2(180, 16);
            var lt = lbl.gameObject.AddComponent<Text>();
            lt.font = _font; lt.fontSize = 15; lt.color = new Color(0.85f, 0.85f, 0.85f);
            lt.alignment = TextAnchor.LowerLeft; lt.raycastTarget = false; lt.text = label;

            var fillRt = Child(label + "_fill", bg);
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            var fImg = fillRt.gameObject.AddComponent<Image>();
            fImg.color = fillCol; fImg.raycastTarget = false;
            fImg.type = Image.Type.Filled; fImg.fillMethod = Image.FillMethod.Horizontal; fImg.fillAmount = 1f;

            return fImg;
        }
    }
}
