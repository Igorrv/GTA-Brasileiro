using System;
using System.Collections.Generic;
using Caos.Core;
using Caos.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Caos.Customization
{
    /// <summary>
    /// Tela de Personagem (docs/08 — T02): gênero, tom de pele, cabelo e roupas do protagonista.
    ///
    /// Mobile-first (docs/08 §8.1): painel na <b>zona do polegar direito</b>, alvos de toque
    /// ≥ 56 px (ref 1080p), setas ◀ ▶ em vez de dropdown (dropdown é ruim de dedo). A prévia é o
    /// <b>próprio boneco no mundo</b> — cada troca aplica na hora (sem salvar), e os botões de
    /// giro viram o personagem para conferir costas e perfil. <b>Salvar</b> persiste no slot;
    /// <b>◄</b> desfaz tudo e volta como estava.
    ///
    /// Abre pela tecla <b>K</b> ou pelo botão flutuante "VISUAL". Congela o jogo como a pausa
    /// (timeScale = 0) sem tocar no PauseMenu.
    /// </summary>
    public sealed class CustomizationUI : MonoBehaviour
    {
        private static readonly Color kOuro  = new Color(0.98f, 0.82f, 0.20f);
        private static readonly Color kVerde = new Color(0.35f, 0.78f, 0.45f);
        private static readonly Color kCinza = new Color(0.62f, 0.62f, 0.66f);
        private static readonly Color kFundo = new Color(0.05f, 0.06f, 0.08f, 0.94f);

        private CustomizationController _controller;
        private Font     _font;
        private GameObject _painel;
        private GameObject _botaoFlutuante;
        private Text       _aviso;
        private float      _avisoAte;

        private bool  _aberta;
        private float _escalaAnterior = 1f;
        private CosmeticLoadout _rascunho;
        private CosmeticLoadout _snapshot;

        private readonly List<Linha> _aparencia = new List<Linha>();
        private readonly List<Linha> _roupas    = new List<Linha>();
        private GameObject _paginaAparencia;
        private GameObject _paginaRoupas;
        private Image _abaAparenciaImg;
        private Image _abaRoupasImg;

        private sealed class Linha
        {
            public Text  valor;
            public Image amostra;
            public Func<string> NomeAtual;
            public Func<Color?> CorAtual;
            public Action<int>  Ciclar;   // -1 / +1
        }

        private void Awake()
        {
            _controller = GetComponent<CustomizationController>();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Montar();
        }

        private void Update()
        {
            // aviso temporário ("desça do veículo...") usa tempo real: o jogo pode estar congelado
            if (_aviso != null && _aviso.gameObject.activeSelf && Time.unscaledTime > _avisoAte)
                _aviso.gameObject.SetActive(false);

            if (_botaoFlutuante != null)
                _botaoFlutuante.SetActive(!_aberta && GameSession.Iniciado && _controller != null && _controller.VisualAplicado);

            if (!Input.GetKeyDown(KeyCode.K)) return;
            if (_aberta) Fechar(true);
            else TentarAbrir();
        }

        // ------------------------------------------------------------ abrir / fechar
        private void TentarAbrir()
        {
            if (!GameSession.Iniciado || !CustomizationService.Pronto) return;
            if (_controller == null || !_controller.VisualAplicado) return;

            var link = FindObjectOfType<PlayerVehicleLink>();
            if (link != null && !link.OnFoot)
            {
                Aviso("Desça do veículo para trocar de roupa.");
                return;
            }

            _snapshot  = CustomizationService.Atual.Clone();
            _rascunho  = CustomizationService.Atual.Clone();
            _aberta    = true;
            _escalaAnterior = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;

            _painel.SetActive(true);
            MostrarAba(true);
            AtualizarLinhas();
        }

        /// <summary><paramref name="salvar"/> = false desfaz a prévia e volta ao visual de antes.</summary>
        private void Fechar(bool salvar)
        {
            if (salvar)
            {
                CustomizationService.Confirmar(_rascunho);
                _controller.Reaplicar();
            }
            else
            {
                _controller.Prever(_snapshot);   // desfaz as trocas não salvas
            }

            _aberta = false;
            _painel.SetActive(false);
            Time.timeScale = _escalaAnterior;
        }

        private void Aviso(string msg)
        {
            if (_aviso == null) return;
            _aviso.text = msg;
            _aviso.gameObject.SetActive(true);
            _avisoAte = Time.unscaledTime + 2.5f;
        }

        // ------------------------------------------------------------ prévia
        private void Trocou()
        {
            _controller.Prever(_rascunho);
            AtualizarLinhas();
        }

        private void Girar(float graus)
        {
            var j = _controller != null ? _controller.Jogador : null;
            if (j != null) j.Rotate(0f, graus, 0f, Space.World);
        }

        // ------------------------------------------------------------ dados <-> rascunho
        private static string CiclarId(IReadOnlyList<CosmeticItemDto> lista, string atual, int dir)
        {
            if (lista == null || lista.Count == 0) return atual;
            int i = 0;
            for (int k = 0; k < lista.Count; k++)
                if (lista[k].id == atual) { i = k; break; }
            return lista[((i + dir) % lista.Count + lista.Count) % lista.Count].id;
        }

        private string CiclarGenero(int dir)
        {
            var g = CustomizationService.Catalogo.Generos;
            if (g.Count == 0) return _rascunho.genero;
            int i = 0;
            for (int k = 0; k < g.Count; k++)
                if (g[k].id == _rascunho.genero) { i = k; break; }
            return g[((i + dir) % g.Count + g.Count) % g.Count].id;
        }

        private static string NomeDe(IList<CosmeticItemDto> lista, string id)
        {
            if (lista != null)
                for (int i = 0; i < lista.Count; i++)
                    if (lista[i].id == id) return lista[i].nome;
            return id;
        }

        private static Color? CorDe(IList<CosmeticItemDto> lista, string id)
        {
            if (lista != null)
                for (int i = 0; i < lista.Count; i++)
                    if (lista[i].id == id && !string.IsNullOrEmpty(lista[i].corHex))
                        return CityPalette.Parse(lista[i].corHex, Color.gray);
            return null;
        }

        private void AtualizarLinhas()
        {
            foreach (var l in _aparencia) AtualizarLinha(l);
            foreach (var l in _roupas)    AtualizarLinha(l);
        }

        private static void AtualizarLinha(Linha l)
        {
            l.valor.text = l.NomeAtual();
            var cor = l.CorAtual();
            l.amostra.gameObject.SetActive(cor.HasValue);
            if (cor.HasValue) l.amostra.color = cor.Value;
        }

        // ------------------------------------------------------------ construção da UI
        private void Montar()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                               typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var canvasGo = new GameObject("TelaPersonagem", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;   // acima do HUD e da pausa, abaixo do menu inicial
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            MontarBotaoFlutuante(canvasGo.transform);
            MontarPainel(canvasGo.transform);
        }

        private void MontarBotaoFlutuante(Transform pai)
        {
            _botaoFlutuante = new GameObject("BotaoVisual", typeof(RectTransform));
            var rt = (RectTransform)_botaoFlutuante.transform;
            rt.SetParent(pai, false);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-24f, -110f);
            rt.sizeDelta = new Vector2(220f, 64f);

            var img = _botaoFlutuante.AddComponent<Image>();
            img.color = new Color(0.13f, 0.14f, 0.17f, 0.92f);
            var btn = _botaoFlutuante.AddComponent<Button>();
            btn.targetGraphic = img;
            var cores = btn.colors;
            cores.highlightedColor = new Color(0.98f, 0.82f, 0.20f, 0.35f);
            cores.pressedColor     = new Color(0.98f, 0.82f, 0.20f, 0.6f);
            btn.colors = cores;
            btn.onClick.AddListener(TentarAbrir);

            var lbl = Rotulo(rt, "VISUAL  (K)", 26, Color.white);
            lbl.alignment = TextAnchor.MiddleCenter;

            _aviso = TextoLivre(pai, "", 24, kOuro, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(900f, 50f));
            _aviso.gameObject.SetActive(false);

            _botaoFlutuante.SetActive(false);
        }

        private void MontarPainel(Transform pai)
        {
            _painel = new GameObject("Painel", typeof(RectTransform));
            var rt = (RectTransform)_painel.transform;
            rt.SetParent(pai, false);
            // painel à direita: a esquerda da tela fica livre para a prévia do boneco no mundo
            rt.anchorMin = new Vector2(0.58f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _painel.AddComponent<Image>().color = kFundo;

            // faixa verde no topo, assinatura visual das telas do jogo
            var faixa = Filho(rt, "Faixa");
            faixa.anchorMin = new Vector2(0f, 1f); faixa.anchorMax = new Vector2(1f, 1f);
            faixa.pivot = new Vector2(0.5f, 1f);
            faixa.anchoredPosition = Vector2.zero;
            faixa.sizeDelta = new Vector2(0f, 6f);
            faixa.gameObject.AddComponent<Image>().color = kVerde;

            // ---- cabeçalho: ◄ voltar · PERSONAGEM · SALVAR ----
            Botao(rt, "◄", new Vector2(0f, 1f), new Vector2(56f, -64f), new Vector2(88f, 72f),
                  new Color(0.20f, 0.21f, 0.25f), () => Fechar(false), 40);
            var titulo = TextoLivre(rt, "PERSONAGEM", 40, kOuro, new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(400f, 60f));
            titulo.fontStyle = FontStyle.Bold;
            Botao(rt, "SALVAR", new Vector2(1f, 1f), new Vector2(-110f, -64f), new Vector2(180f, 72f),
                  kVerde, () => Fechar(true), 28);

            // ---- abas ----
            _abaAparenciaImg = Aba(rt, "APARÊNCIA", new Vector2(-190f, -150f), () => MostrarAba(true));
            _abaRoupasImg    = Aba(rt, "ROUPAS",    new Vector2( 190f, -150f), () => MostrarAba(false));

            // ---- páginas ----
            _paginaAparencia = Pagina(rt, "PaginaAparencia");
            _paginaRoupas    = Pagina(rt, "PaginaRoupas");
            MontarLinhasAparencia((RectTransform)_paginaAparencia.transform);
            MontarLinhasRoupas((RectTransform)_paginaRoupas.transform);

            // ---- rodapé: giro da prévia + dica ----
            var rodape = TextoLivre(rt, "Girar a prévia:", 24, kCinza, new Vector2(0.5f, 0f), new Vector2(-120f, 96f), new Vector2(260f, 40f));
            rodape.alignment = TextAnchor.MiddleRight;
            Botao(rt, "⟲", new Vector2(0.5f, 0f), new Vector2(60f, 96f), new Vector2(96f, 64f),
                  new Color(0.20f, 0.21f, 0.25f), () => Girar(-40f), 36);
            Botao(rt, "⟳", new Vector2(0.5f, 0f), new Vector2(170f, 96f), new Vector2(96f, 64f),
                  new Color(0.20f, 0.21f, 0.25f), () => Girar(40f), 36);
            var dica = TextoLivre(rt, "K fecha  ·  ◄ volta sem salvar", 20, kCinza, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(600f, 34f));
            dica.alignment = TextAnchor.MiddleCenter;

            _painel.SetActive(false);
        }

        private void MontarLinhasAparencia(RectTransform pagina)
        {
            Func<CosmeticCatalog> cat = () => CustomizationService.Catalogo;
            _aparencia.Add(LinhaOpcao(pagina, 0, "GÊNERO",
                () => CustomizationService.Catalogo.NomeGenero(_rascunho.genero),
                () => (Color?)null,
                dir => { _rascunho.genero = CiclarGenero(dir); Trocou(); }));
            _aparencia.Add(LinhaOpcao(pagina, 1, "TOM DE PELE",
                () => NomeDe(cat().TonsDePele, _rascunho.pele),
                () => CorDe(cat().TonsDePele, _rascunho.pele),
                dir => { _rascunho.pele = CiclarId(cat().TonsDePele, _rascunho.pele, dir); Trocou(); }));
            _aparencia.Add(LinhaOpcao(pagina, 2, "CABELO",
                () => NomeDe(cat().Cabelos, _rascunho.cabelo),
                () => (Color?)null,
                dir => { _rascunho.cabelo = CiclarId(cat().Cabelos, _rascunho.cabelo, dir); Trocou(); }));
            _aparencia.Add(LinhaOpcao(pagina, 3, "COR DO CABELO",
                () => NomeDe(cat().CoresCabelo, _rascunho.corCabelo),
                () => CorDe(cat().CoresCabelo, _rascunho.corCabelo),
                dir => { _rascunho.corCabelo = CiclarId(cat().CoresCabelo, _rascunho.corCabelo, dir); Trocou(); }));
        }

        private void MontarLinhasRoupas(RectTransform pagina)
        {
            Func<CosmeticCatalog> cat = () => CustomizationService.Catalogo;
            _roupas.Add(LinhaOpcao(pagina, 0, "TRONCO",
                () => NomeDe(cat().Tops, _rascunho.top),
                () => CorDe(cat().Tops, _rascunho.top),
                dir => { _rascunho.top = CiclarId(cat().Tops, _rascunho.top, dir); Trocou(); }));
            _roupas.Add(LinhaOpcao(pagina, 1, "PERNAS",
                () => NomeDe(cat().Bottoms, _rascunho.bottom),
                () => CorDe(cat().Bottoms, _rascunho.bottom),
                dir => { _rascunho.bottom = CiclarId(cat().Bottoms, _rascunho.bottom, dir); Trocou(); }));
            _roupas.Add(LinhaOpcao(pagina, 2, "CALÇADO",
                () => NomeDe(cat().Calcados, _rascunho.calcado),
                () => CorDe(cat().Calcados, _rascunho.calcado),
                dir => { _rascunho.calcado = CiclarId(cat().Calcados, _rascunho.calcado, dir); Trocou(); }));
            _roupas.Add(LinhaOpcao(pagina, 3, "CABEÇA",
                () => NomeDe(cat().Chapeus, _rascunho.chapeu),
                () => CorDe(cat().Chapeus, _rascunho.chapeu),
                dir => { _rascunho.chapeu = CiclarId(cat().Chapeus, _rascunho.chapeu, dir); Trocou(); }));

            var nota = TextoLivre(pagina, "Vestido cobre as pernas — a escolha de PERNAS\nvale de novo quando você vestir outro tronco.",
                                  20, kCinza, new Vector2(0.5f, 1f), new Vector2(0f, -560f), new Vector2(560f, 80f));
            nota.alignment = TextAnchor.UpperCenter;
        }

        // ------------------------------------------------------------ widgets
        private Linha LinhaOpcao(RectTransform pagina, int indice, string rotulo,
                                 Func<string> nomeAtual, Func<Color?> corAtual, Action<int> ciclar)
        {
            float y = -70f - indice * 130f;

            var lbl = TextoLivre(pagina, rotulo, 22, kCinza, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(500f, 34f));
            lbl.alignment = TextAnchor.MiddleCenter;

            var linha = new Linha { NomeAtual = nomeAtual, CorAtual = corAtual, Ciclar = ciclar };

            Botao(pagina, "◀", new Vector2(0.5f, 1f), new Vector2(-260f, y - 62f), new Vector2(96f, 72f),
                  new Color(0.20f, 0.21f, 0.25f), () => ciclar(-1), 36);
            Botao(pagina, "▶", new Vector2(0.5f, 1f), new Vector2(260f, y - 62f), new Vector2(96f, 72f),
                  new Color(0.20f, 0.21f, 0.25f), () => ciclar(1), 36);

            // caixa do valor: nome da opção + amostra de cor
            var caixa = Filho(pagina, "Valor_" + rotulo);
            caixa.anchorMin = caixa.anchorMax = new Vector2(0.5f, 1f);
            caixa.pivot = new Vector2(0.5f, 1f);
            caixa.anchoredPosition = new Vector2(0f, y - 62f);
            caixa.sizeDelta = new Vector2(380f, 72f);
            caixa.gameObject.AddComponent<Image>().color = new Color(0.11f, 0.12f, 0.15f, 0.95f);

            linha.valor = Rotulo(caixa, "", 26, Color.white);
            linha.valor.alignment = TextAnchor.MiddleCenter;

            var amostraGo = Filho(caixa, "Amostra");
            amostraGo.anchorMin = amostraGo.anchorMax = new Vector2(1f, 0.5f);
            amostraGo.pivot = new Vector2(1f, 0.5f);
            amostraGo.anchoredPosition = new Vector2(-12f, 0f);
            amostraGo.sizeDelta = new Vector2(48f, 48f);
            linha.amostra = amostraGo.gameObject.AddComponent<Image>();

            return linha;
        }

        private Image Aba(RectTransform pai, string rotulo, Vector2 pos, UnityEngine.Events.UnityAction acao)
        {
            var rt = Filho(pai, "Aba_" + rotulo);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(340f, 64f);
            var img = rt.gameObject.AddComponent<Image>();
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(acao);
            var lbl = Rotulo(rt, rotulo, 26, Color.white);
            lbl.alignment = TextAnchor.MiddleCenter;
            return img;
        }

        private void MostrarAba(bool aparencia)
        {
            _paginaAparencia.SetActive(aparencia);
            _paginaRoupas.SetActive(!aparencia);
            _abaAparenciaImg.color = aparencia ? kOuro : new Color(0.13f, 0.14f, 0.17f, 0.95f);
            _abaRoupasImg.color    = aparencia ? new Color(0.13f, 0.14f, 0.17f, 0.95f) : kOuro;
        }

        private static GameObject Pagina(RectTransform pai, string nome)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(pai, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(24f, 150f);
            rt.offsetMax = new Vector2(-24f, -210f);
            return go;
        }

        // ------------------------------------------------------------ helpers de fábrica
        private static RectTransform Filho(Transform pai, string nome)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            return (RectTransform)go.transform;
        }

        private Text Rotulo(RectTransform pai, string txt, int tamanho, Color cor)
        {
            var rt = Filho(pai, "Rotulo");
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font; t.fontSize = tamanho; t.color = cor; t.text = txt;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Text TextoLivre(Transform pai, string txt, int tamanho, Color cor, Vector2 ancora, Vector2 pos, Vector2 caixa)
        {
            var rt = Filho(pai, "Txt");
            rt.anchorMin = rt.anchorMax = ancora;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = caixa;
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font; t.fontSize = tamanho; t.color = cor; t.text = txt;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private void Botao(Transform pai, string rotulo, Vector2 ancora, Vector2 pos, Vector2 tamanho,
                           Color cor, UnityEngine.Events.UnityAction acao, int tamanhoFonte = 28)
        {
            var rt = Filho(pai, "Botao_" + rotulo);
            rt.anchorMin = rt.anchorMax = ancora;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = tamanho;

            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(cor.r * 0.55f, cor.g * 0.55f, cor.b * 0.55f, 1f);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cores = btn.colors;
            cores.highlightedColor = cor;
            cores.pressedColor     = Color.Lerp(cor, Color.white, 0.4f);
            btn.colors = cores;
            btn.onClick.AddListener(acao);

            var lbl = Rotulo(rt, rotulo, tamanhoFonte, Color.white);
            lbl.alignment = TextAnchor.MiddleCenter;
        }
    }
}
