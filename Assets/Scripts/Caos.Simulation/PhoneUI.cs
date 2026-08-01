using System.Collections.Generic;
using Caos.Core;
using Caos.Data;
using Caos.Gameplay;
using Caos.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Celular do jogo (docs/08 T08) com navegação estilo iOS: aparelho no canto, <b>barra de status</b>
    /// (hora, sinal, bateria), <b>home screen</b> com grade de ícones e, dentro de cada app, uma barra
    /// de título com "voltar" e o <b>botão home</b> embaixo, que sempre volta pra grade.
    ///
    /// Apps ligados ao jogo de verdade:
    ///  • <b>Contatos</b> — quem te dá missão, com a reputação atual da facção;
    ///  • <b>VaiJá</b> — missão ativa, destino e recompensa;
    ///  • <b>Banco</b> — saldo em R$/CaosCash, IPC-Caos e um PIX de emergência (custa caro, como na vida);
    ///  • <b>Mapa</b> — bairro e logradouro em que você está;
    ///  • <b>Rádio</b> — estação no ar e troca de estação;
    ///  • <b>Ajustes</b> — controles e estado da partida.
    ///
    /// Abre com <b>P</b> (ou pelo botão touch). Não pausa o jogo — o mundo continua rodando, como no gênero.
    /// </summary>
    public class PhoneUI : MonoBehaviour
    {
        private enum App { Home, Contatos, VaiJa, Banco, Mapa, Radio, Ajustes }

        private const float kLargura = 420f, kAltura = 760f;

        private RadioSystem      _radio;
        private Transform        _player;
        private EconomyService   _econ;
        private ReputationService _rep;
        private WorldStateService _world;
        private TimeOfDayService _time;
        private GameCatalogs     _catalogs;
        private MissionService   _missoes;

        private Font       _font;
        private GameObject _aparelho;
        private RectTransform _tela;
        private Text       _statusHora, _statusOperadora, _tituloApp;
        private GameObject _telaHome, _telaApp, _barraTitulo;
        private Text       _conteudo;
        private App        _appAtual = App.Home;
        private bool       _aberto;
        private float      _pollAccum;

        private static readonly Color kFundo    = new Color(0.07f, 0.07f, 0.09f, 0.98f);
        private static readonly Color kBarra    = new Color(0.13f, 0.13f, 0.16f, 1f);
        private static readonly Color kTexto    = new Color(0.93f, 0.93f, 0.95f);

        public void Init(Transform player, RadioSystem radio)
        {
            _player = player;
            _radio  = radio;
            ServiceLocator.TryGet(out _econ);
            ServiceLocator.TryGet(out _rep);
            ServiceLocator.TryGet(out _world);
            ServiceLocator.TryGet(out _time);
            ServiceLocator.TryGet(out _catalogs);
            ServiceLocator.TryGet(out _missoes);
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Montar();
        }

        private void Update()
        {
            if (GameInput.Phone) Alternar();
            if (!_aberto) return;

            _pollAccum += Time.unscaledDeltaTime;
            if (_pollAccum < 0.25f) return;
            _pollAccum = 0f;
            AtualizarStatus();
            if (_appAtual != App.Home) AtualizarConteudo();
        }

        public void Alternar()
        {
            _aberto = !_aberto;
            _aparelho.SetActive(_aberto);
            if (_aberto) { Abrir(App.Home); AtualizarStatus(); }
        }

        // ------------------------------------------------------------------ navegação
        private void Abrir(App app)
        {
            _appAtual = app;
            bool home = app == App.Home;
            _telaHome.SetActive(home);
            _telaApp.SetActive(!home);
            _barraTitulo.SetActive(!home);
            if (!home)
            {
                _tituloApp.text = NomeDoApp(app);
                AtualizarConteudo();
            }
        }

        private static string NomeDoApp(App app)
        {
            switch (app)
            {
                case App.Contatos: return "Contatos";
                case App.VaiJa:    return "VaiJá";
                case App.Banco:    return "Banco Caos";
                case App.Mapa:     return "Mapa";
                case App.Radio:    return "Rádio";
                case App.Ajustes:  return "Ajustes";
                default:           return "";
            }
        }

        private void AtualizarStatus()
        {
            if (_statusHora != null && _time != null)
            {
                int h = (int)_time.Hour;
                int m = (int)((_time.Hour - h) * 60f);
                _statusHora.text = $"{h:00}:{m:00}";
            }
            if (_statusOperadora != null)
            {
                // "sinal" cai no morro e na zona rural — piadinha que também é informação
                string bairro = CityRuntime.Layout != null && _player != null
                    ? CityRuntime.Layout.DistrictIdAt(_player.position) : "Centro";
                bool ruim = bairro == "VistaAlegre" || bairro == "SitioCapim";
                _statusOperadora.text = (ruim ? "Caos Móvel  ·  1G  " : "Caos Móvel  ·  4G  ") + "|||   87%";
            }
        }

        private void AtualizarConteudo()
        {
            if (_conteudo == null) return;
            switch (_appAtual)
            {
                case App.Contatos: _conteudo.text = TextoContatos(); break;
                case App.VaiJa:    _conteudo.text = TextoVaiJa();    break;
                case App.Banco:    _conteudo.text = TextoBanco();    break;
                case App.Mapa:     _conteudo.text = TextoMapa();     break;
                case App.Radio:    _conteudo.text = TextoRadio();    break;
                case App.Ajustes:  _conteudo.text = TextoAjustes();  break;
            }
        }

        // ------------------------------------------------------------------ conteúdo dos apps
        private string TextoContatos()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Quem te liga em São Genésio:\n");
            if (_catalogs != null && _catalogs.Factions != null)
            {
                foreach (var f in _catalogs.Factions)
                {
                    string tom = _rep != null ? _rep.Tone(f.id) : "Neutro";
                    int valor = _rep != null ? _rep.Get(f.id) : 0;
                    sb.AppendLine($"<b>{f.lider}</b>");
                    sb.AppendLine($"{f.nome}");
                    sb.AppendLine($"relação: {tom} ({valor:+0;-0;0})\n");
                }
            }
            return sb.ToString();
        }

        private string TextoVaiJa()
        {
            var tracker = FindObjectOfType<MissionTracker>();
            if (tracker == null || !tracker.TemDestino)
                return "Nenhuma corrida ativa agora.\n\nPasse num ponto de trabalho (VaiJá, obra, galpão, feira ou quiosque) e aperte F para pegar um turno.";

            int feitas = _missoes != null ? _missoes.CompletedSnapshot().Count : 0;
            float dist = _player != null ? Vector3.Distance(_player.position, tracker.Destino) : 0f;
            return $"<b>Corrida em andamento</b>\n\n{tracker.TituloAtivo}\n\nDestino: {tracker.DestinoLabel}\nDistância: {dist:F0} m\n\nEntregas concluídas: {feitas}";
        }

        private string TextoBanco()
        {
            if (_econ == null) return "Sem conexão com o banco.";
            return $"<b>Conta Caos</b>\n\nSaldo:  R$ {_econ.Rs:N2}\nCaosCash:  {_econ.CaosCash:F0}\n\nIPC-Caos: {_econ.IpcCaos:P1}\n(a inflação que corrige o preço da gasolina, do pastel e do reparo)\n\nPIX de emergência: toque abaixo para receber R$ 100 agora — devolve R$ 140 na próxima semana.";
        }

        private string TextoMapa()
        {
            if (CityRuntime.Layout == null || _player == null) return "Localizando...";
            var layout = CityRuntime.Layout;
            string id = layout.DistrictIdAt(_player.position);
            var d = layout.DistrictById(id);
            string rua = layout.StreetNameAt(_player.position);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>Você está em</b>\n{(d != null ? d.nome : id)}\n{rua}\n");
            if (d != null && !string.IsNullOrEmpty(d.descricao)) sb.AppendLine(d.descricao + "\n");
            sb.AppendLine("<b>Bairros da cidade</b>");
            foreach (var b in layout.Districts)
            {
                float dist = Vector3.Distance(_player.position, new Vector3(b.centroX, 0f, b.centroZ));
                sb.AppendLine($"{b.nome} — {dist:F0} m");
            }
            sb.AppendLine("\n(M abre o mapa grande em tela cheia)");
            return sb.ToString();
        }

        private string TextoRadio()
        {
            if (_radio == null) return "Sem rádio.";
            if (!_radio.NoAr) return "O rádio só toca dentro do veículo.\n\nEntre no carro (E) e aperte Q para trocar de estação.";
            return $"<b>{_radio.Estacao}</b>\n{_radio.Slogan}\n\nNo ar agora:\n{_radio.Faixa}\n\nQ — próxima estação\nZ — liga/desliga";
        }

        private string TextoAjustes()
        {
            int dia = _time != null ? _time.Day : 1;
            int estrelas = _world != null ? _world.Stars : 0;
            return $"<b>Partida</b>\nSlot {Caos.Core.GameSession.Slot}  ·  Dia {dia}  ·  Procurado {estrelas}/5\n\n" +
                   "<b>Controles</b>\nWASD andar/dirigir · Shift correr · Espaço pular/freio\nE entrar-sair · F usar/comprar · R abastecer\nCtrl freio de mão · Q/Z rádio · M mapa · P celular · Tab pausa";
        }

        // ------------------------------------------------------------------ UI
        private void Montar()
        {
            // qualificado: Caos.Gameplay também tem um tipo chamado EventSystem (o de eventos do mundo)
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("CelularUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // ---- aparelho (canto direito, como quem levanta o celular) ----
            _aparelho = new GameObject("Aparelho", typeof(RectTransform));
            var ap = (RectTransform)_aparelho.transform;
            ap.SetParent(canvasGo.transform, false);
            ap.anchorMin = ap.anchorMax = new Vector2(1f, 0.5f);
            ap.pivot = new Vector2(1f, 0.5f);
            ap.anchoredPosition = new Vector2(-40f, 0f);
            ap.sizeDelta = new Vector2(kLargura, kAltura);
            var corpo = _aparelho.AddComponent<Image>();
            corpo.color = new Color(0.02f, 0.02f, 0.03f, 1f);   // carcaça

            _tela = Filho(ap, "Tela");
            _tela.anchorMin = Vector2.zero; _tela.anchorMax = Vector2.one;
            _tela.offsetMin = new Vector2(14f, 22f); _tela.offsetMax = new Vector2(-14f, -14f);
            _tela.gameObject.AddComponent<Image>().color = kFundo;

            // ---- barra de status ----
            var status = Filho(_tela, "Status");
            status.anchorMin = new Vector2(0f, 1f); status.anchorMax = new Vector2(1f, 1f);
            status.pivot = new Vector2(0.5f, 1f);
            status.sizeDelta = new Vector2(0f, 34f);
            status.gameObject.AddComponent<Image>().color = kBarra;
            _statusHora = Rotulo(status, "10:00", 18, kTexto, TextAnchor.MiddleLeft, new Vector2(12f, 0f));
            _statusOperadora = Rotulo(status, "Caos Móvel · 4G", 15, new Color(0.7f, 0.7f, 0.74f), TextAnchor.MiddleRight, new Vector2(-12f, 0f));

            // ---- barra de título do app (só fora da home) ----
            _barraTitulo = Filho(_tela, "BarraTitulo").gameObject;
            var bt = (RectTransform)_barraTitulo.transform;
            bt.anchorMin = new Vector2(0f, 1f); bt.anchorMax = new Vector2(1f, 1f);
            bt.pivot = new Vector2(0.5f, 1f);
            bt.anchoredPosition = new Vector2(0f, -34f);
            bt.sizeDelta = new Vector2(0f, 46f);
            _barraTitulo.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.13f, 1f);
            _tituloApp = Rotulo(bt, "", 22, kTexto, TextAnchor.MiddleCenter, Vector2.zero);
            BotaoTexto(bt, "‹ voltar", new Vector2(0f, 0.5f), new Vector2(58f, 0f), 16, () => Abrir(App.Home));

            // ---- home: grade de ícones ----
            _telaHome = Filho(_tela, "Home").gameObject;
            var hRt = (RectTransform)_telaHome.transform;
            hRt.anchorMin = Vector2.zero; hRt.anchorMax = Vector2.one;
            hRt.offsetMin = new Vector2(0f, 60f); hRt.offsetMax = new Vector2(0f, -40f);

            Icone(hRt, "Contatos", new Color(0.30f, 0.65f, 0.95f), 0, 0, () => Abrir(App.Contatos));
            Icone(hRt, "VaiJá",    new Color(0.95f, 0.72f, 0.20f), 1, 0, () => Abrir(App.VaiJa));
            Icone(hRt, "Banco",    new Color(0.25f, 0.72f, 0.45f), 2, 0, () => Abrir(App.Banco));
            Icone(hRt, "Mapa",     new Color(0.85f, 0.40f, 0.35f), 0, 1, () => Abrir(App.Mapa));
            Icone(hRt, "Rádio",    new Color(0.72f, 0.40f, 0.85f), 1, 1, () => Abrir(App.Radio));
            Icone(hRt, "Ajustes",  new Color(0.55f, 0.57f, 0.62f), 2, 1, () => Abrir(App.Ajustes));

            // ---- conteúdo do app ----
            _telaApp = Filho(_tela, "ConteudoApp").gameObject;
            var cRt = (RectTransform)_telaApp.transform;
            cRt.anchorMin = Vector2.zero; cRt.anchorMax = Vector2.one;
            cRt.offsetMin = new Vector2(16f, 60f); cRt.offsetMax = new Vector2(-16f, -84f);
            _conteudo = Rotulo(cRt, "", 18, kTexto, TextAnchor.UpperLeft, Vector2.zero);
            _conteudo.lineSpacing = 1.25f;
            var cr = (RectTransform)_conteudo.transform;
            cr.anchorMin = Vector2.zero; cr.anchorMax = Vector2.one;
            cr.offsetMin = Vector2.zero; cr.offsetMax = Vector2.zero;
            _conteudo.supportRichText = true;

            // ---- botão home (a barrinha do iPhone) ----
            var home = Filho(_tela, "BotaoHome");
            home.anchorMin = new Vector2(0.5f, 0f); home.anchorMax = new Vector2(0.5f, 0f);
            home.pivot = new Vector2(0.5f, 0f);
            home.anchoredPosition = new Vector2(0f, 14f);
            home.sizeDelta = new Vector2(160f, 26f);
            var homeImg = home.gameObject.AddComponent<Image>();
            homeImg.color = new Color(0.85f, 0.85f, 0.88f, 0.85f);
            var homeBtn = home.gameObject.AddComponent<Button>();
            homeBtn.targetGraphic = homeImg;
            homeBtn.onClick.AddListener(() => Abrir(App.Home));

            _aparelho.SetActive(false);
        }

        private void Icone(RectTransform pai, string nome, Color cor, int col, int linha, UnityEngine.Events.UnityAction acao)
        {
            const float tam = 96f, passoX = 124f, passoY = 148f;

            var rt = Filho(pai, "Icone_" + nome);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2((col - 1) * passoX, -40f - linha * passoY);
            rt.sizeDelta = new Vector2(tam, tam);

            var img = rt.gameObject.AddComponent<Image>();
            img.color = cor;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cores = btn.colors;
            cores.highlightedColor = Color.Lerp(cor, Color.white, 0.35f);
            btn.colors = cores;
            btn.onClick.AddListener(acao);

            // inicial do app dentro do ícone (sem depender de sprite)
            var ini = Rotulo(rt, nome.Substring(0, 1).ToUpper(), 44, Color.white, TextAnchor.MiddleCenter, Vector2.zero);
            ini.fontStyle = FontStyle.Bold;

            var lbl = Filho(rt, "Nome");
            lbl.anchorMin = new Vector2(0.5f, 0f); lbl.anchorMax = new Vector2(0.5f, 0f);
            lbl.pivot = new Vector2(0.5f, 1f);
            lbl.anchoredPosition = new Vector2(0f, -6f);
            lbl.sizeDelta = new Vector2(140f, 24f);
            var t = lbl.gameObject.AddComponent<Text>();
            t.font = _font; t.fontSize = 17; t.color = kTexto; t.text = nome;
            t.alignment = TextAnchor.UpperCenter; t.raycastTarget = false;
        }

        // ---- helpers ----
        private static RectTransform Filho(Transform pai, string nome)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            return (RectTransform)go.transform;
        }

        private Text Rotulo(Transform pai, string txt, int tamanho, Color cor, TextAnchor alinhamento, Vector2 pos)
        {
            var rt = Filho(pai, "Txt");
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10f, 4f); rt.offsetMax = new Vector2(-10f, -4f);
            rt.anchoredPosition += pos;
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font; t.fontSize = tamanho; t.color = cor; t.text = txt;
            t.alignment = alinhamento; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        private void BotaoTexto(Transform pai, string txt, Vector2 ancora, Vector2 pos, int tamanho, UnityEngine.Events.UnityAction acao)
        {
            var rt = Filho(pai, "Btn_" + txt);
            rt.anchorMin = rt.anchorMax = ancora;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(110f, 34f);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.06f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(acao);
            var t = Rotulo(rt, txt, tamanho, new Color(0.45f, 0.75f, 1f), TextAnchor.MiddleCenter, Vector2.zero);
            t.raycastTarget = false;
        }
    }
}
