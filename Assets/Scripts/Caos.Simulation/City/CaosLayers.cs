using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Camadas de física do jogo.
    ///
    /// Sem elas, todo colisor testa contra todo colisor: a cidade tem ~7.700 peças, mais 26 carros de
    /// trânsito, 18 pedestres, a polícia e os props. O motor gastava tempo perguntando se o pedestre
    /// bateu no outro pedestre, se o meio-fio bateu no muro, se o cone bateu na calçada — respostas
    /// que nunca importam.
    ///
    /// Além do custo, havia efeito colateral: pedestres se empurrando, carros do trânsito enroscando
    /// em poste. A matriz resolve os dois de uma vez.
    ///
    /// Os índices são resolvidos por nome no boot. Se o projeto não tiver as camadas definidas
    /// (<c>CaosLayerSetup</c> cria), tudo cai para <c>Default</c> e o jogo roda como antes — sem
    /// otimização, mas sem quebrar.
    /// </summary>
    public static class CaosLayers
    {
        public static int Cidade   { get; private set; }   // asfalto, calçada, prédio, muro — estático
        public static int Prop     { get; private set; }   // poste, banco, cone, lixeira
        public static int Veiculo  { get; private set; }   // carro do jogador, tráfego, polícia
        public static int Pedestre { get; private set; }
        public static int Jogador  { get; private set; }
        public static int Gatilho  { get; private set; }   // buraco, zona de interação

        private static bool _pronto;

        /// <summary>Máscara do que a câmera deve considerar ao evitar parede (ignora gatilho e gente).</summary>
        public static int MascaraCamera => (1 << Cidade) | (1 << Prop) | (1 << Veiculo);

        public static void Resolver()
        {
            if (_pronto) return;
            _pronto = true;

            Cidade   = Achar("CaosCidade");
            Prop     = Achar("CaosProp");
            Veiculo  = Achar("CaosVeiculo");
            Pedestre = Achar("CaosPedestre");
            Jogador  = Achar("CaosJogador");
            Gatilho  = Achar("CaosGatilho");

            bool tudoDefault = Cidade == 0 && Veiculo == 0 && Pedestre == 0;
            Debug.Log(tudoDefault
                ? "[Física] Camadas não definidas no projeto — rodando tudo em Default (rode Caos ▸ Configurar camadas)."
                : $"[Física] Camadas: cidade={Cidade} prop={Prop} veículo={Veiculo} pedestre={Pedestre} jogador={Jogador} gatilho={Gatilho}.");
        }

        private static int Achar(string nome)
        {
            int i = LayerMask.NameToLayer(nome);
            return i < 0 ? 0 : i;   // 0 = Default: degrada sem quebrar
        }

        /// <summary>Marca o objeto e toda a sua descendência com a camada dada.</summary>
        public static void Marcar(GameObject go, int camada)
        {
            if (go == null) return;
            go.layer = camada;
            var filhos = go.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < filhos.Length; i++) filhos[i].gameObject.layer = camada;
        }

        public static void Marcar(Transform t, int camada) { if (t != null) Marcar(t.gameObject, camada); }
    }
}
