using System.Collections.Generic;
using System.IO;
using Caos.Core;
using Caos.Data;
using Caos.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Hub de mundos — o navegador de servidores do jogo (docs/08 T00).
    ///
    /// Cada mundo é uma <b>semente</b>, e a semente determina a cidade inteira: mesma semente, mesma
    /// São Genésio, em qualquer máquina. É por isso que entrar num mundo é só definir a semente antes
    /// do <see cref="WorldBuilder"/> subir — e é também o que torna o multiplayer possível sem
    /// distribuir a cidade pela rede.
    ///
    /// Sua <b>vida é por mundo</b>: o <see cref="SaveSystem"/> indexa o save por mundo, então dinheiro,
    /// missões e reputação conquistados em São Genésio não existem em Beira-Mar. Entrar num servidor
    /// significa começar (ou retomar) uma vida ali.
    ///
    /// Mundos com <c>endereco</c> preenchido são remotos. Hoje eles aparecem marcados como
    /// indisponíveis, porque não há camada de rede no projeto ainda; quando houver, esta mesma tela
    /// passa a listá-los sem mudar de formato — o que muda é de onde a lista vem.
    /// </summary>
    public class WorldHub : MonoBehaviour
    {
        private readonly List<WorldDto> _mundos = new List<WorldDto>();
        private Font _font;
        private GameObject _raiz;
        private Text _rodape;

        /// <summary>Mundo escolhido (o menu de slots usa para carregar o perfil certo).</summary>
        public static WorldDto Escolhido { get; private set; }

        private static readonly Color kOuro  = new Color(0.98f, 0.82f, 0.20f);
        private static readonly Color kVerde = new Color(0.35f, 0.78f, 0.45f);
        private static readonly Color kCinza = new Color(0.62f, 0.62f, 0.66f);

        public void Init(Font font)
        {
            _font = font;
            CarregarMundos();
            Montar();
        }

        /// <summary>
        /// Lê <c>worlds.json</c> direto do disco. O hub aparece <b>antes</b> do GameManager carregar
        /// os catálogos, então não dá pra esperar o ServiceLocator: a lista de servidores é a primeira
        /// coisa que a tela precisa.
        /// </summary>
        private void CarregarMundos()
        {
            try
            {
                string caminho = Path.Combine(Application.streamingAssetsPath, "Data", "worlds.json");
                if (File.Exists(caminho))
                {
                    var lista = JsonUtility.FromJson<Envelope>(File.ReadAllText(caminho));
                    if (lista?.items != null) _mundos.AddRange(lista.items);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Hub] Falha ao ler worlds.json: " + e.Message);
            }

            if (_mundos.Count == 0)
            {
                // sem o arquivo, o jogo ainda abre: um mundo padrão é melhor que uma tela vazia
                _mundos.Add(new WorldDto
                {
                    id = "genesio", nome = "São Genésio do Caos", semente = 20260801,
                    lema = "O mundo original.", regiao = "Sudeste", lotacaoMax = 16, dificuldade = 2, endereco = ""
                });
            }
        }

        [System.Serializable] private class Envelope { public List<WorldDto> items; }

        // ------------------------------------------------------------------ ação
        private void Entrar(WorldDto mundo)
        {
            if (!mundo.EhLocal)
            {
                if (_rodape != null)
                    _rodape.text = $"{mundo.nome} é um servidor remoto ({mundo.endereco}) — a camada de rede ainda não está no projeto.";
                return;
            }

            Escolhido = mundo;
            GameSession.DefinirSemente(mundo.semente);
            SaveSystem.MundoAtual = mundo.id;

            Debug.Log($"[Hub] Entrando em '{mundo.nome}' (semente {mundo.semente}, perfil '{mundo.id}').");
            Fechar();
            SendMessageUpwards("HubEscolheuMundo", mundo, SendMessageOptions.DontRequireReceiver);
        }

        public void Fechar() { if (_raiz != null) _raiz.SetActive(false); }
        public void Abrir()  { if (_raiz != null) _raiz.SetActive(true);  }

        // ------------------------------------------------------------------ UI
        private void Montar()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("HubDeMundosUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;                      // acima do menu de slots
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _raiz = new GameObject("Raiz", typeof(RectTransform));
            var rt = (RectTransform)_raiz.transform;
            rt.SetParent(canvasGo.transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _raiz.AddComponent<Image>().color = new Color(0.04f, 0.045f, 0.06f, 1f);

            Texto(rt, "ESCOLHA SEU MUNDO", 72, kOuro, FontStyle.Bold, 380f, TextAnchor.MiddleCenter);
            Texto(rt, "Cada mundo é uma cidade própria — e uma vida própria. Seu dinheiro, suas missões e sua reputação ficam no mundo onde você jogou.",
                  22, kCinza, FontStyle.Normal, 320f, TextAnchor.MiddleCenter);

            // ---- cartões, um por mundo ----
            int n = _mundos.Count;
            float largura = 430f, espaco = 24f;
            float total = n * largura + (n - 1) * espaco;

            for (int i = 0; i < n; i++)
            {
                var mundo = _mundos[i];
                var cartao = Filho(rt, "Mundo_" + mundo.id);
                cartao.anchorMin = cartao.anchorMax = new Vector2(0.5f, 0.5f);
                cartao.pivot = new Vector2(0.5f, 0.5f);
                cartao.anchoredPosition = new Vector2(-total * 0.5f + largura * 0.5f + i * (largura + espaco), 20f);
                cartao.sizeDelta = new Vector2(largura, 380f);
                var bg = cartao.gameObject.AddComponent<Image>();
                bg.sprite = UiTextures.Arredondado(0.06f);
                bg.type = Image.Type.Sliced;
                bg.color = mundo.EhLocal ? new Color(0.11f, 0.12f, 0.15f, 0.98f) : new Color(0.10f, 0.10f, 0.11f, 0.85f);

                var perfil = PerfilDoMundo(mundo.id);

                Texto(cartao, mundo.nome, 32, mundo.EhLocal ? kOuro : kCinza, FontStyle.Bold, 148f, TextAnchor.MiddleCenter);
                Texto(cartao, $"{mundo.regiao}  ·  até {mundo.lotacaoMax} jogadores  ·  {Dificuldade(mundo.dificuldade)}",
                      18, kCinza, FontStyle.Normal, 112f, TextAnchor.MiddleCenter);

                var lema = Texto(cartao, mundo.lema, 19, new Color(0.85f, 0.85f, 0.88f), FontStyle.Italic, 54f, TextAnchor.UpperCenter);
                lema.lineSpacing = 1.25f;
                ((RectTransform)lema.transform).sizeDelta = new Vector2(-48f, 90f);

                Texto(cartao, perfil, 19, perfil.StartsWith("Vida") ? kVerde : kCinza, FontStyle.Normal, -40f, TextAnchor.MiddleCenter);
                Texto(cartao, $"semente {mundo.semente}", 15, new Color(0.45f, 0.45f, 0.50f), FontStyle.Normal, -74f, TextAnchor.MiddleCenter);

                string rotulo = !mundo.EhLocal ? "REMOTO (em breve)"
                              : perfil.StartsWith("Vida") ? "CONTINUAR" : "COMEÇAR VIDA AQUI";
                Botao(cartao, rotulo, new Vector2(0f, -140f), new Vector2(largura - 60f, 56f),
                      mundo.EhLocal ? kVerde : new Color(0.35f, 0.35f, 0.38f), () => Entrar(mundo));
            }

            _rodape = Texto(rt, "Mundos remotos aparecem aqui quando a camada de rede entrar — a semente já garante que todos vejam a mesma cidade.",
                            19, kCinza, FontStyle.Normal, -300f, TextAnchor.MiddleCenter);
        }

        /// <summary>Resumo da vida que existe naquele mundo, lido dos saves sem carregar o jogo.</summary>
        private static string PerfilDoMundo(string mundoId)
        {
            string anterior = SaveSystem.MundoAtual;
            SaveSystem.MundoAtual = mundoId;
            try
            {
                for (int slot = 1; slot <= SaveSystem.Slots; slot++)
                {
                    var info = SaveSystem.Peek(slot);
                    if (info.existe)
                        return $"Vida em andamento · Dia {info.dia} · R$ {info.rs:N0} · {info.missoes} missões";
                }
                return "Nenhuma vida começada aqui";
            }
            finally { SaveSystem.MundoAtual = anterior; }
        }

        private static string Dificuldade(int d)
        {
            switch (d)
            {
                case 1:  return "tranquilo";
                case 2:  return "normal";
                case 3:  return "puxado";
                case 4:  return "osso duro";
                default: return "insano";
            }
        }

        // ---- helpers ----
        private static RectTransform Filho(Transform pai, string nome)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            return (RectTransform)go.transform;
        }

        private Text Texto(Transform pai, string txt, int tamanho, Color cor, FontStyle estilo, float y, TextAnchor alinhamento)
        {
            var rt = Filho(pai, "Txt");
            rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(-40f, tamanho * 3f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font; t.fontSize = tamanho; t.color = cor; t.fontStyle = estilo;
            t.text = txt; t.alignment = alinhamento; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private void Botao(Transform pai, string rotulo, Vector2 pos, Vector2 tamanho, Color cor, UnityEngine.Events.UnityAction acao)
        {
            var rt = Filho(pai, "Botao");
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = tamanho;

            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiTextures.Arredondado(0.28f);
            img.type = Image.Type.Sliced;
            img.color = new Color(cor.r * 0.5f, cor.g * 0.5f, cor.b * 0.5f, 1f);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cores = btn.colors;
            cores.highlightedColor = cor;
            cores.pressedColor     = Color.Lerp(cor, Color.white, 0.35f);
            btn.colors = cores;
            btn.onClick.AddListener(acao);

            var lbl = Filho(rt, "Rotulo");
            lbl.anchorMin = Vector2.zero; lbl.anchorMax = Vector2.one;
            lbl.offsetMin = Vector2.zero; lbl.offsetMax = Vector2.zero;
            var t = lbl.gameObject.AddComponent<Text>();
            t.font = _font; t.fontSize = 24; t.color = Color.white; t.text = rotulo;
            t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        }
    }
}
