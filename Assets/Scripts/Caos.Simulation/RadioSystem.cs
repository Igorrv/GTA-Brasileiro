using System.Collections.Generic;
using Caos.Core;
using Caos.Data;
using Caos.Simulation.Audio;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Rádio do carro (docs/12 §12.6). As estações vêm de <c>radio.json</c> — nome, gênero, slogan,
    /// alinhamento de faixas e as falas do locutor — e tudo o que sai no ar é <b>sintetizado em
    /// runtime</b>: nenhum arquivo de áudio no projeto.
    ///
    /// Esta classe é a <b>direção de programação</b>; quem toca é o <see cref="RadioBroadcast"/>. A
    /// divisão importa porque as duas coisas vivem em threads diferentes: aqui se decide, no relógio
    /// do jogo, o que entra no ar (sintonia → vinheta → locutor → música → locutor → ...); lá se gera
    /// PCM, na thread de áudio, sem tocar em nada da Unity.
    ///
    /// O que mudou em relação à primeira versão:
    ///
    ///  • <b>Nada de buffer gigante.</b> Antes cada faixa materializava um <c>float[]</c> de até ~1 MB
    ///    na thread principal e virava um clipe residente. Agora o áudio é um fluxo em streaming, então
    ///    trocar de faixa custa uma atribuição de referência — sem engasgo e sem memória parada.
    ///  • <b>Estação tem identidade.</b> Banda (AM/FM), tonalidade, vinheta de quatro notas e um locutor
    ///    com voz própria, tudo derivado do que já existe no catálogo.
    ///  • <b>O locutor fala.</b> A linha que o HUD mostra é sintetizada como voz, e a AM de notícias —
    ///    que antes gerava <c>null</c> e ficava muda — finalmente vai ao ar.
    ///  • <b>Respeita os Ajustes e a pausa.</b> O volume passa pelo barramento de música da
    ///    <see cref="AudioDirector"/>, que também abaixa o rádio quando um SFX importante toca e corta
    ///    tudo quando o jogo congela.
    ///
    /// Como sempre, só toca dentro do veículo — é da tradição do gênero.
    /// </summary>
    public class RadioSystem : MonoBehaviour
    {
        private const int   kTaxa        = 22050;
        private const float kNivelBase   = 0.62f;   // teto do rádio antes do barramento de música
        private const float kSintonia    = 0.42f;   // chiado ao girar o dial
        private const float kVinheta     = 1.15f;
        private const float kFaixaMin    = 44f;
        private const float kFaixaMax    = 72f;
        private const float kBoletim     = 4.0f;    // respiro entre notas da AM de notícias

        /// <summary>O que está no ar agora. A ordem dos blocos é o que dá cara de emissora.</summary>
        private enum Bloco { Sintonia, Vinheta, Locucao, Musica }

        private readonly List<RadioStationDto>       _estacoes = new List<RadioStationDto>();
        private readonly List<RadioBroadcast.Estacao> _dossies = new List<RadioBroadcast.Estacao>();
        private int[] _faixaDaEstacao;

        private PlayerVehicleLink _link;
        private AudioSource       _source;
        private FluxoDeAudio      _fluxo;
        private RadioBroadcast    _tx;
        private System.Random     _sorteio;

        private Bloco _bloco = Bloco.Musica;
        private float _restante, _volume;
        private int   _estacao, _faixa;
        private float _falaAte;
        private int   _locucaoAnterior = -1;

        public bool   Ligado    { get; private set; } = true;
        public string Estacao   { get; private set; } = "";
        public string Slogan    { get; private set; } = "";
        public string Faixa     { get; private set; } = "";
        public string Locucao   { get; private set; } = "";
        public Color  Cor       { get; private set; } = Color.white;
        public bool   FalaNoAr  => Time.time < _falaAte;
        public bool   NoAr      => Ligado && _link != null && !_link.OnFoot && _estacoes.Count > 0;

        public void Init(GameCatalogs catalogs, PlayerVehicleLink link)
        {
            _link = link;
            if (catalogs != null && catalogs.Radio != null) _estacoes.AddRange(catalogs.Radio);
            if (_estacoes.Count == 0) return;

            // sorteio próprio, semeado pela sessão: não encosta no Random global da Unity, que é o que
            // mantém a cidade e os eventos reproduzíveis a partir da semente do mundo
            _sorteio = new System.Random(unchecked(GameSession.Semente * 31 + 17) & 0x7fffffff);

            _faixaDaEstacao = new int[_estacoes.Count];
            for (int i = 0; i < _estacoes.Count; i++)
            {
                var e = _estacoes[i];
                _dossies.Add(RadioBroadcast.MontarEstacao(e.id, e.nome, e.genero));
            }

            _tx    = new RadioBroadcast(kTaxa);
            _fluxo = FluxoDeAudio.Criar("radio", _tx, kTaxa);
            if (_fluxo == null)
            {
                Debug.LogWarning("[Rádio] Sem fluxo de áudio nesta plataforma — o rádio fica só no HUD.");
            }
            else
            {
                _source = _fluxo.Instalar(gameObject);
            }

            _estacao = _sorteio.Next(_estacoes.Count);
            AplicarEstacao(reiniciarFaixa: true);
            _tx.Sintonia(kSintonia);          // a primeira vez que se entra no carro também é uma sintonia
            Entrar(Bloco.Sintonia, kSintonia);
        }

        private void Update()
        {
            if (_estacoes.Count == 0) return;

            // Com o jogo congelado, Input.GetKeyDown continua disparando: sem esta guarda, apertar Q no
            // menu de pausa trocava de estação por trás do painel. O toque do botão virtual é lido de
            // qualquer jeito para não ficar engatilhado e disparar sozinho quando a pausa sair.
            bool jogando = Time.timeScale > 0f;
            bool proxima = GameInput.RadioNext;
            if (jogando)
            {
                if (proxima)               Sintonizar(+1);
                if (GameInput.RadioToggle) Alternar();
            }

            bool noAr = NoAr;
            AtualizarVolume(noAr);
            if (!noAr || _tx == null) return;

            // a programação anda no relógio do jogo: pausou, a locução espera
            _restante -= Time.deltaTime;
            if (_restante <= 0f) Avancar();

            if (_bloco == Bloco.Locucao && _tx.LocutorNoAr)
                AudioDirector.LocutorNoAr(_tx.NivelDeVoz);
        }

        // ------------------------------------------------------------------ volume e mixagem
        private void AtualizarVolume(bool noAr)
        {
            if (_source == null) return;

            // tempo não escalado de propósito: o fade não pode congelar junto com o jogo, senão o rádio
            // ficaria preso no volume em que estava quando a pausa entrou
            float dt = Time.unscaledDeltaTime;
            _volume = Mathf.MoveTowards(_volume, noAr ? 1f : 0f, dt * (noAr ? 2.2f : 4.5f));

            // compensação de mascaramento: acima de 80 km/h o motor e o vento comem a música, e todo
            // rádio de carro decente sobe um pouco com a velocidade
            float compensa = Mathf.Lerp(1f, 1.20f, AudioDirector.RuidoDeFundo);

            _source.volume = _volume * kNivelBase * compensa * AudioDirector.Ganho(Barramento.Musica);

            // fora do ar a fonte é parada: sem callback de PCM, sem CPU e sem bateria queimando à toa
            bool deveTocar = _volume > 0.001f;
            if (deveTocar && !_source.isPlaying)      _source.Play();
            else if (!deveTocar && _source.isPlaying) _source.Stop();
        }

        // ------------------------------------------------------------------ controles
        /// <summary>Liga/desliga. Desligado, o transmissor cala de vez (nada rodando em segundo plano).</summary>
        public void Alternar()
        {
            Ligado = !Ligado;
            if (Ligado)
            {
                AplicarEstacao(reiniciarFaixa: false);
                _tx?.Sintonia(kSintonia * 0.6f);
                Entrar(Bloco.Sintonia, kSintonia * 0.6f);
            }
            else
            {
                _tx?.Desligar();
                _falaAte = 0f;
            }
        }

        /// <summary>
        /// Passa pra próxima estação. Cada uma <b>lembra em que faixa parou</b> — o comentário original
        /// prometia isso, mas o código voltava para a faixa 0 toda vez.
        /// </summary>
        public void Sintonizar(int delta)
        {
            if (_estacoes.Count == 0) return;

            _faixaDaEstacao[_estacao] = _faixa;
            _estacao = (_estacao + delta + _estacoes.Count) % _estacoes.Count;
            Ligado = true;

            AplicarEstacao(reiniciarFaixa: false);
            _tx?.Sintonia(kSintonia);
            Entrar(Bloco.Sintonia, kSintonia);
        }

        // ------------------------------------------------------------------ programação
        private void Entrar(Bloco bloco, float duracao)
        {
            _bloco = bloco;
            _restante = duracao;
        }

        /// <summary>Fecha o bloco atual e escolhe o próximo — é aqui que a emissora "roda".</summary>
        private void Avancar()
        {
            var dto = _estacoes[_estacao];
            var dossie = _dossies[_estacao];
            bool noticias = dossie.Genero == RadioBroadcast.Genero.Noticias;

            switch (_bloco)
            {
                case Bloco.Sintonia:
                    _tx?.Vinheta(kVinheta);
                    Entrar(Bloco.Vinheta, kVinheta);
                    break;

                case Bloco.Vinheta:
                    if (!Locutar(dto)) Entrar(Bloco.Musica, DuracaoDaFaixa(noticias));
                    break;

                case Bloco.Locucao:
                    Entrar(Bloco.Musica, DuracaoDaFaixa(noticias));
                    break;

                default:
                    // fim da faixa: avança o alinhamento e decide entre chamada do locutor e vinheta
                    TrocarFaixa(_faixa + 1);
                    if (!noticias && _sorteio.NextDouble() < 0.3)
                    {
                        _tx?.Vinheta(kVinheta);
                        Entrar(Bloco.Vinheta, kVinheta);
                    }
                    else if (!Locutar(dto))
                    {
                        Entrar(Bloco.Musica, DuracaoDaFaixa(noticias));
                    }
                    break;
            }
        }

        /// <summary>Notícia é bloco curto e emendado; música respira quase um minuto.</summary>
        private float DuracaoDaFaixa(bool noticias)
            => noticias ? kBoletim : Mathf.Lerp(kFaixaMin, kFaixaMax, (float)_sorteio.NextDouble());

        /// <summary>Bota o locutor no ar com uma fala que ainda não foi a última — e sincroniza o HUD.</summary>
        private bool Locutar(RadioStationDto dto)
        {
            if (_tx == null || dto.locucoes == null || dto.locucoes.Count == 0) return false;

            int i = _sorteio.Next(dto.locucoes.Count);
            if (dto.locucoes.Count > 1 && i == _locucaoAnterior) i = (i + 1) % dto.locucoes.Count;
            _locucaoAnterior = i;

            Locucao = dto.locucoes[i];
            float dur = _tx.Falar(Locucao);
            if (dur <= 0f) return false;

            _falaAte = Time.time + dur;
            Entrar(Bloco.Locucao, dur + 0.25f);
            return true;
        }

        private void AplicarEstacao(bool reiniciarFaixa)
        {
            var dto = _estacoes[_estacao];
            Estacao = dto.nome;
            Slogan  = dto.slogan;
            Cor     = CityPalette.Parse(dto.corHex, Color.white);
            _locucaoAnterior = -1;

            TrocarFaixa(reiniciarFaixa ? 0 : _faixaDaEstacao[_estacao]);
        }

        private void TrocarFaixa(int indice)
        {
            var dto = _estacoes[_estacao];
            int n = dto.faixas != null ? dto.faixas.Count : 0;
            _faixa = n > 0 ? ((indice % n) + n) % n : 0;
            _faixaDaEstacao[_estacao] = _faixa;

            var f = n > 0 ? dto.faixas[_faixa] : null;
            Faixa = f != null ? $"{f.titulo} — {f.artista}" : dto.nome;

            float bpm = f != null && f.bpm > 20f ? f.bpm : (dto.bpm > 20f ? dto.bpm : 100f);
            int semente = f != null && f.semente != 0 ? f.semente : _estacao * 31 + _faixa;
            _tx?.Sintonizar(_dossies[_estacao], bpm, semente);
        }

        private void OnDestroy()
        {
            _tx?.Desligar();
            if (_source != null) _source.Stop();
            _fluxo?.Destruir();
        }
    }
}
