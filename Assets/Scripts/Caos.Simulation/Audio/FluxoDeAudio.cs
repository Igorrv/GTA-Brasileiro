using UnityEngine;

namespace Caos.Simulation.Audio
{
    /// <summary>
    /// Quem sabe preencher um bloco de amostras. Implementado pelos sintetizadores (rádio, motor,
    /// ambiente) e chamado pela <b>thread de áudio</b> da Unity.
    /// </summary>
    public interface IFluxoPcm
    {
        /// <summary>
        /// Escreve <paramref name="quadros"/> amostras mono em <paramref name="destino"/>.
        /// <paramref name="amostraInicial"/> é o contador contínuo do fluxo desde o começo — quem
        /// precisa de relógio musical usa ele, e não o <c>Time.time</c> (que é da thread principal).
        ///
        /// Contrato: sem alocação, sem API da Unity, sem exceção.
        /// </summary>
        void Render(float[] destino, int quadros, int taxa, long amostraInicial);
    }

    /// <summary>
    /// Ponte entre um <see cref="IFluxoPcm"/> e a Unity.
    ///
    /// <b>Por que isso existe.</b> O rádio antigo materializava a faixa inteira: um <c>float[]</c> de
    /// até ~270 mil posições (≈1 MB) sintetizado na thread principal a cada troca de faixa, virando um
    /// <see cref="AudioClip"/> residente de meio megabyte. No celular isso é o pior dos dois mundos —
    /// engasgo de quadro na troca e memória parada. Aqui o clipe é criado com <c>stream: true</c> e um
    /// <i>PCM reader callback</i>: a Unity mantém só um anel de alguns milissegundos e pede blocos
    /// pequenos conforme toca. A síntese sai da thread principal, a alocação por faixa vira zero e a
    /// música deixa de ser um laço de 4 compassos — ela simplesmente continua.
    ///
    /// O <c>lengthSamples</c> abaixo é só o tamanho nominal do clipe; como ignoramos a posição pedida e
    /// mantemos nosso próprio contador, o áudio nunca "volta ao início" de fato.
    /// </summary>
    public sealed class FluxoDeAudio
    {
        /// <summary>Comprimento nominal do clipe. Um minuto é folgado e mantém o anel interno pequeno.</summary>
        private const int kSegundosNominais = 60;

        private readonly IFluxoPcm _fonte;
        private readonly int       _taxa;
        private long _amostra;

        public AudioClip Clip { get; private set; }
        public bool      Valido => Clip != null;

        private FluxoDeAudio(IFluxoPcm fonte, int taxa)
        {
            _fonte = fonte;
            _taxa  = taxa;
        }

        /// <summary>
        /// Cria o clipe em streaming. Devolve <c>null</c> se a plataforma recusar (aí o chamador decide
        /// se cai num plano B ou fica em silêncio, em vez de o jogo estourar).
        /// </summary>
        public static FluxoDeAudio Criar(string nome, IFluxoPcm fonte, int taxa)
        {
            if (fonte == null) return null;

            var fluxo = new FluxoDeAudio(fonte, taxa);
            try
            {
                fluxo.Clip = AudioClip.Create(nome, taxa * kSegundosNominais, 1, taxa, true,
                                              fluxo.Ler, fluxo.Posicionar);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Áudio] Fluxo '{nome}' indisponível nesta plataforma: {e.Message}");
                return null;
            }
            return fluxo.Clip != null ? fluxo : null;
        }

        /// <summary>Monta a fonte já configurada para o fluxo: 2D, em laço e começando muda.</summary>
        public AudioSource Instalar(GameObject alvo)
        {
            var src = alvo.AddComponent<AudioSource>();
            src.clip = Clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = 0f;
            src.bypassReverbZones = true;
            return src;
        }

        // ------------------------------------------------------------------ thread de áudio
        private void Ler(float[] dados)
        {
            int n = dados.Length;
            try
            {
                _fonte.Render(dados, n, _taxa, _amostra);
            }
            catch
            {
                // uma exceção aqui mataria a thread de áudio em silêncio pelo resto da sessão
                for (int i = 0; i < n; i++) dados[i] = 0f;
            }
            _amostra += n;
        }

        /// <summary>
        /// A Unity avisa que "voltou ao início" do clipe nominal. Ignoramos de propósito: o contador
        /// contínuo é o que faz a música seguir a estrutura em vez de repetir um trecho.
        /// </summary>
        private void Posicionar(int posicao) { }

        public void Destruir()
        {
            if (Clip == null) return;
            Object.Destroy(Clip);
            Clip = null;
        }
    }
}
