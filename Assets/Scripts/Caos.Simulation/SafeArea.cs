using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Encolhe um painel para dentro da <b>área segura</b> da tela.
    ///
    /// O jogo é mobile-first e o HUD estava ancorado nas bordas absolutas — o que em qualquer celular
    /// moderno significa dinheiro embaixo do notch, radar cortado pelo canto arredondado e botão de
    /// pausa colado na barra de gestos. <see cref="Screen.safeArea"/> é a região que o sistema garante
    /// visível, e é nela que a interface tem que caber.
    ///
    /// Aplicado ao <b>painel</b> e não ao Canvas: elementos de fundo (o escurecido de pausa, a tela de
    /// carga) continuam ocupando a tela inteira, como devem — só o conteúdo recua.
    ///
    /// Reage a rotação e a mudança de resolução, porque o mesmo aparelho tem safe area diferente em
    /// pé e deitado.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect  _ultimaArea  = new Rect(0f, 0f, 0f, 0f);
        private Vector2Int _ultimaTela = Vector2Int.zero;

        /// <summary>Margem extra além da área segura, em pixels de referência (respiro visual).</summary>
        [SerializeField] private float folga = 8f;

        /// <summary>Pendura o componente num painel já existente.</summary>
        public static SafeArea Aplicar(RectTransform painel, float folga = 8f)
        {
            if (painel == null) return null;
            var sa = painel.gameObject.GetComponent<SafeArea>() ?? painel.gameObject.AddComponent<SafeArea>();
            sa.folga = folga;
            sa.Atualizar();
            return sa;
        }

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            Atualizar();
        }

        private void Update()
        {
            // barato: só recalcula quando a tela muda de fato (rotação, split-screen, desktop redimensionado)
            if (Screen.safeArea == _ultimaArea &&
                _ultimaTela.x == Screen.width && _ultimaTela.y == Screen.height) return;
            Atualizar();
        }

        private void Atualizar()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (_rt == null || Screen.width <= 0 || Screen.height <= 0) return;

            _ultimaArea = Screen.safeArea;
            _ultimaTela = new Vector2Int(Screen.width, Screen.height);

            // safe area vem em pixels; as âncoras são em fração da tela
            Vector2 min = _ultimaArea.position;
            Vector2 max = _ultimaArea.position + _ultimaArea.size;
            min.x /= Screen.width;  min.y /= Screen.height;
            max.x /= Screen.width;  max.y /= Screen.height;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = new Vector2( folga,  folga);
            _rt.offsetMax = new Vector2(-folga, -folga);
        }
    }
}
