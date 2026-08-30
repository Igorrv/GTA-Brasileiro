using UnityEngine;

namespace Caos.Simulation.Audio
{
    /// <summary>
    /// O <b>transmissor</b>: dado um programa (estação + faixa), entrega amostras. Quem decide o que
    /// entra no ar e quando é o <see cref="RadioSystem"/>; aqui só se toca.
    ///
    /// <b>Identidade de estação.</b> Antes, "estação" era só um nome no HUD e uma função de gênero: as
    /// seis soavam com o mesmo timbre, o mesmo volume e a mesma progressão I–V–vi–IV. Agora cada uma
    /// carrega um dossiê montado a partir de <c>radio.json</c> — banda (o nome diz se é FM ou AM, e a
    /// AM soa como AM: estreita, comprimida, com chiado e desvanecimento), tonalidade própria, uma
    /// vinheta de quatro notas derivada do <c>id</c>, e um locutor com tom, ritmo e rouquidão próprios.
    /// Trocar de estação passa a ser uma mudança de <i>lugar</i>, não de faixa.
    ///
    /// <b>Estrutura.</b> Um sequenciador de semicolcheias dispara instrumentos com padrões de verdade
    /// (tamborzão, baião de zabumba, teleco-teco de samba), organizados em frases de 8 compassos com
    /// abertura, estrofe, refrão e virada. Nada é laço de 4 compassos: a frase muda porque a semente
    /// da melodia é resemeada a cada frase.
    ///
    /// <b>Custo.</b> Tudo isso cabe num fluxo mono de 22,05 kHz gerado sob demanda — algumas dezenas de
    /// operações por amostra, nenhuma alocação e nenhum buffer residente além do anel da Unity.
    /// </summary>
    public sealed class RadioBroadcast : IFluxoPcm
    {
        public enum Genero { Funk, Sertanejo, Forro, Samba, Gospel, Rock, Noticias }

        // ================================================================== identidade
        /// <summary>Dossiê de uma estação. Montado uma vez, na thread principal, e só lido depois.</summary>
        public sealed class Estacao
        {
            public string Id = "", Nome = "";
            public Genero Genero;
            public bool   Am;
            public int    Raiz;                    // tônica, em semitons acima de Lá
            public readonly int[] Motivo = new int[4];   // vinheta: graus da escala
            public LocutorVoz.Perfil Voz;
            public float  Chiado, GraveHz, AgudoHz, Compressao, Presenca;
            public float  Ganho = 1f;
        }

        private sealed class Programa
        {
            public Estacao Estacao;
            public float   Bpm;
            public int     Semente;
            public int     Versao;
        }

        // ================================================================== estado publicado
        // Pool em anel: a thread de áudio pode estar lendo um programa enquanto a principal monta o
        // próximo. Quatro posições garantem que ela nunca escreva por cima do que está no ar.
        private volatile Programa _programa;
        private readonly Programa[] _poolProgramas = { new Programa(), new Programa(), new Programa(), new Programa() };
        private int _proximoPrograma, _versao;

        private readonly LocutorVoz _locutor = new LocutorVoz();

        // Agendamento por contagem regressiva, e não por instante absoluto: um relógio em float que só
        // cresce perde resolução depois de algumas horas de sessão e os prazos começam a errar.
        // Pedido por contador: a thread principal só incrementa o gatilho e a de áudio compara com o
        // último que viu. Nenhuma das duas escreve no campo da outra, então nenhum pedido se perde.
        private volatile float _estaticaPedida;
        private volatile int   _estaticaGatilho;
        private volatile float _vinhetaPedida;
        private volatile int   _vinhetaGatilho;

        /// <summary>Envelope da voz do locutor (0..1). A thread principal usa para abafar motor e cidade.</summary>
        public float NivelDeVoz { get; private set; }

        /// <summary>Há locução no ar agora.</summary>
        public bool LocutorNoAr => _locutor.NoAr;

        /// <summary>Duração da última locução montada — o HUD segura a legenda por esse tempo.</summary>
        public float DuracaoDaFala => _locutor.Duracao;

        // ================================================================== estado da thread de áudio
        private Programa _progAtivo;
        private int      _versaoAtiva = -1;
        private Genero   _generoConfigurado = (Genero)(-1);
        private int      _taxa = 22050;

        private float _fracaoPasso;
        private long  _passoGlobal;

        private BandaProcedural.Tambor   _bumbo, _surdo;
        private BandaProcedural.Caixa    _caixa, _tamborim;
        private BandaProcedural.Chocalho _chimbal, _ganza;
        private BandaProcedural.Metal    _triangulo;
        private BandaProcedural.Baixo    _baixo;
        private BandaProcedural.Solo     _solo;
        private BandaProcedural.Palheta  _palheta;
        private BandaProcedural.Naipe    _naipe;
        private readonly BandaProcedural.Corda[] _cordas =
        {
            new BandaProcedural.Corda(7331), new BandaProcedural.Corda(7332), new BandaProcedural.Corda(7333)
        };

        private CaosDsp.Ruido  _rndMelodia = new CaosDsp.Ruido(1);
        private CaosDsp.Ruido  _rndPortadora = new CaosDsp.Ruido(99);
        private CaosDsp.Ruido  _rndSintonia = new CaosDsp.Ruido(4242);
        private CaosDsp.PassaAlta _hpTx;
        private CaosDsp.Biquad    _lpTx, _lpTxFi;
        private CaosDsp.Biquad _presenca, _sintoniaBanda;
        private CaosDsp.Compressor _comp;
        private bool  _cadeiaPronta;

        private float _envVoz, _fadeDesvanece = 1f, _faseDesvanece, _faseAssobio, _faseVarredura;
        private float _alvoPalheta, _alvoNaipe;
        private float _freqPalheta = 220f, _freqNaipe = 220f;
        private int   _acordeAtual;
        // o que a semente da faixa decide: sem isso, duas faixas da mesma estação soam idênticas —
        // o padrão, a harmonia e a tonalidade vinham só da estação, e a semente só mexia no solo
        private int   _transpose, _variante, _rotacao;
        private float _densidade = 1f;
        private int   _vinhetaNota = -1, _vinhetaVisto, _estaticaVista;
        private float _vinhetaT, _vinhetaDur = 1.15f, _estaticaT;

        public RadioBroadcast(int taxa) { _taxa = Mathf.Clamp(taxa, 8000, 48000); }

        // ================================================================== thread principal
        /// <summary>
        /// Traduz a estação do catálogo num dossiê sonoro. Tudo é derivado de campos que o
        /// <c>radio.json</c> já tem — nome, id, gênero — porque o modelo de dados é território de
        /// outra frente e não deve ganhar campos por causa do áudio.
        /// </summary>
        public static Estacao MontarEstacao(string id, string nome, string genero)
        {
            var e = new Estacao { Id = id ?? "", Nome = nome ?? "", Genero = GeneroDe(genero) };
            int h = CaosDsp.Hash(e.Id.Length > 0 ? e.Id : e.Nome);

            // FM ou AM sai do próprio nome da estação ("Boemia AM 720"); notícia sem sigla é AM.
            string n = e.Nome.ToUpperInvariant();
            e.Am = n.Contains(" AM") || n.EndsWith("AM") || (!n.Contains("FM") && e.Genero == Genero.Noticias);

            e.Raiz = h % 12;
            for (int i = 0; i < 4; i++) e.Motivo[i] = (h >> (i * 3 + 2)) % 7;

            if (e.Am)
            {
                e.GraveHz = 330f; e.AgudoHz = 3400f; e.Chiado = 0.030f; e.Compressao = 0.85f; e.Presenca = 6.5f;
            }
            else
            {
                e.GraveHz = 55f;  e.AgudoHz = 9500f; e.Chiado = 0.006f; e.Compressao = 0.50f; e.Presenca = 3.0f;
            }

            e.Voz = PerfilDeVoz(e.Genero, h);

            // emissora de verdade é nivelada em loudness: a AM perde nível na banda estreita e no
            // desvanecimento, e a de notícias é quase só voz. Sem compensar, girar o dial vira um
            // sobe-e-desce de volume e a AM parece "a estação fraca".
            e.Ganho = e.Genero == Genero.Noticias ? 1.7f : (e.Am ? 1.45f : 1f);
            return e;
        }

        private static Genero GeneroDe(string g)
        {
            switch (g)
            {
                case "funk":      return Genero.Funk;
                case "sertanejo": return Genero.Sertanejo;
                case "forro":     return Genero.Forro;
                case "gospel":    return Genero.Gospel;
                case "rock":      return Genero.Rock;
                case "noticias":  return Genero.Noticias;
                default:          return Genero.Samba;    // mpb, samba, pagode
            }
        }

        /// <summary>Perfil do locutor: base pelo gênero, variação pela estação. Nenhum DJ soa igual.</summary>
        private static LocutorVoz.Perfil PerfilDeVoz(Genero g, int hash)
        {
            var p = LocutorVoz.Perfil.Padrao;
            switch (g)
            {
                case Genero.Funk:      p.Tom = 122f; p.Ritmo = 1.35f; p.Energia = 1.00f; p.Aspereza = 0.22f; break;
                case Genero.Sertanejo: p.Tom = 106f; p.Ritmo = 0.92f; p.Energia = 0.50f; p.Aspereza = 0.30f; break;
                case Genero.Forro:     p.Tom = 128f; p.Ritmo = 1.12f; p.Energia = 0.85f; p.Aspereza = 0.24f; break;
                case Genero.Samba:     p.Tom =  99f; p.Ritmo = 0.86f; p.Energia = 0.32f; p.Aspereza = 0.32f; break;
                case Genero.Gospel:    p.Tom = 114f; p.Ritmo = 0.80f; p.Energia = 0.45f; p.Aspereza = 0.10f; break;
                case Genero.Rock:      p.Tom = 118f; p.Ritmo = 1.10f; p.Energia = 0.75f; p.Aspereza = 0.35f; break;
                default:               p.Tom = 134f; p.Ritmo = 1.02f; p.Energia = 0.22f; p.Aspereza = 0.08f; break;
            }
            p.Tom   *= 1f + ((hash >> 3) % 17 - 8) * 0.010f;    // ±8%
            p.Trato  = 0.94f + ((hash >> 9) % 21) * 0.010f;     // 0,94..1,14
            p.Ritmo *= 1f + ((hash >> 5) % 11 - 5) * 0.012f;
            return p;
        }

        /// <summary>Entra no ar com uma faixa. Troca atômica: a thread de áudio nunca vê meio programa.</summary>
        public void Sintonizar(Estacao estacao, float bpm, int semente)
        {
            if (estacao == null) { _programa = null; return; }

            var p = _poolProgramas[_proximoPrograma];
            _proximoPrograma = (_proximoPrograma + 1) & 3;
            p.Estacao = estacao;
            p.Bpm     = Mathf.Clamp(bpm > 20f ? bpm : 100f, 50f, 190f);
            p.Semente = semente;
            p.Versao  = ++_versao;
            _programa = p;
        }

        public void Desligar() { _programa = null; _locutor.Calar(); }

        /// <summary>Chiado de sintonia por alguns instantes — o gesto de girar o dial.</summary>
        public void Sintonia(float segundos)
        {
            _estaticaPedida = Mathf.Max(0.05f, segundos);
            _estaticaGatilho++;
        }

        /// <summary>Vinheta da estação (as quatro notas do <c>id</c>).</summary>
        public void Vinheta(float segundos = 1.15f)
        {
            _vinhetaPedida = Mathf.Max(0.8f, segundos);
            _vinhetaGatilho++;
        }

        /// <summary>Bota o locutor no ar com a frase que o HUD está mostrando. Devolve a duração.</summary>
        public float Falar(string texto)
        {
            var p = _programa;
            if (p == null || string.IsNullOrEmpty(texto)) return 0f;
            _locutor.Falar(texto, p.Estacao.Voz, CaosDsp.Hash(texto) ^ p.Semente);
            return _locutor.Duracao;
        }

        public void Calar() => _locutor.Calar();

        // ================================================================== thread de áudio
        public void Render(float[] destino, int quadros, int taxa, long amostraInicial)
        {
            _taxa = taxa;
            float dt = 1f / taxa;
            var prog = _programa;

            if (!_cadeiaPronta) MontarCadeia(prog);
            if (prog != null && prog.Versao != _versaoAtiva) TrocarPrograma(prog);

            // pedidos da thread principal viram contagem regressiva local
            int gatilhoEstatica = _estaticaGatilho;
            if (gatilhoEstatica != _estaticaVista)
            {
                _estaticaVista = gatilhoEstatica;
                _estaticaT = Mathf.Max(_estaticaT, _estaticaPedida);
            }
            int gatilho = _vinhetaGatilho;
            if (gatilho != _vinhetaVisto)
            {
                _vinhetaVisto = gatilho;
                _vinhetaDur = Mathf.Max(0.8f, _vinhetaPedida);
                _vinhetaT   = _vinhetaDur;
                _vinhetaNota = -1;
            }

            if (prog == null)
            {
                for (int i = 0; i < quadros; i++) destino[i] = 0f;
                _estaticaT = Mathf.Max(0f, _estaticaT - quadros * dt);
                _vinhetaT  = Mathf.Max(0f, _vinhetaT  - quadros * dt);
                NivelDeVoz = 0f;
                return;
            }

            var est = prog.Estacao;
            float passosPorAmostra = prog.Bpm / 60f * 4f * dt;

            for (int i = 0; i < quadros; i++)
            {
                // ---- sequenciador ----
                _fracaoPasso += passosPorAmostra;
                while (_fracaoPasso >= 1f)
                {
                    _fracaoPasso -= 1f;
                    _passoGlobal++;
                    Disparar(prog);
                }

                // ---- programa ----
                // a banda continua sendo renderizada durante a vinheta, só bem embaixo: parar de
                // renderizar deixaria envelopes congelados e o retorno soaria com sobra de ataques
                bool naVinheta = _vinhetaT > 0f;
                float vinheta = naVinheta ? RenderVinheta(dt, est) : 0f;
                float banda   = RenderBanda(dt, est) * (naVinheta ? 0.14f : 1f);
                float voz     = _locutor.Render(taxa);

                // sidechain interna: a cama abaixa embaixo da voz, como em qualquer rádio
                float nivel = voz < 0f ? -voz : voz;
                _envVoz += (nivel > _envVoz ? 0.010f : 0.0009f) * (nivel - _envVoz);
                float espaco = 1f - 0.80f * Mathf.Clamp01(_envVoz * 5f);

                float mix = (banda + vinheta) * espaco + voz * 0.85f;
                mix = CadeiaDeTransmissao(mix, est, dt);

                // ---- sintonia entre estações ----
                if (_estaticaT > 0f)
                {
                    _estaticaT -= dt;
                    mix = Mathf.Lerp(mix, Estatica(dt), Mathf.Clamp01(_estaticaT * 4f));
                }

                // a saturação de verdade já aconteceu dentro da cadeia; esta é só rede de segurança
                destino[i] = CaosDsp.Saturar(mix * est.Ganho);
            }

            NivelDeVoz = Mathf.Clamp01(_envVoz * 5f);
        }

        private void TrocarPrograma(Programa prog)
        {
            _progAtivo   = prog;
            _versaoAtiva = prog.Versao;
            _acordeAtual = 0;

            // a faixa herda a estação (tonalidade, banda, locutor) e escolhe o resto: a levada, o giro
            // da progressão, quanto sobe ou desce do tom da casa e quão cheio é o arranjo
            var r = new CaosDsp.Ruido(prog.Semente);
            _transpose = (int)(r.Sorte() * 5f) - 2;
            _variante  = (int)(r.Sorte() * 3f) % 3;
            _rotacao   = (int)(r.Sorte() * 4f) & 3;
            _densidade = 0.72f + r.Sorte() * 0.34f;
            _rndMelodia = new CaosDsp.Ruido(prog.Semente);
            if (_generoConfigurado != prog.Estacao.Genero)
            {
                _generoConfigurado = prog.Estacao.Genero;
                ConfigurarInstrumentos(prog.Estacao.Genero);
            }
            MontarCadeia(prog);
        }

        private void MontarCadeia(Programa prog)
        {
            var est = prog?.Estacao;
            _hpTx.Ajustar(est != null ? est.GraveHz : 60f, _taxa);
            _lpTx.PassaBaixa(est != null ? est.AgudoHz : 9000f, 0.707f, _taxa);
            _lpTxFi.PassaBaixa(est != null ? est.AgudoHz : 9000f, 0.54f, _taxa);
            _presenca.Pico(2600f, 1.1f, est != null ? est.Presenca : 3f, _taxa);
            _sintoniaBanda.PassaBanda(2200f, 3.5f, _taxa);
            _comp.Configurar();
            _cadeiaPronta = true;
        }

        private void ConfigurarInstrumentos(Genero g)
        {
            switch (g)
            {
                case Genero.Funk:
                    _bumbo.Configurar(78f, 44f, 42f, 7.5f, 0.10f, 11);       // tamborzão: grave e longo
                    _caixa.Configurar(220f, 1900f, 1.1f, 26f, 0.72f, _taxa, 12);
                    _tamborim.Configurar(410f, 1200f, 2.2f, 34f, 0.55f, _taxa, 13);
                    _chimbal.Configurar(4600f, 62f, _taxa, 14);
                    break;
                case Genero.Forro:
                    _bumbo.Configurar(92f, 58f, 26f, 6.0f, 0.05f, 21);       // zabumba
                    _caixa.Configurar(300f, 2600f, 1.6f, 44f, 0.85f, _taxa, 22);   // o tapa
                    _tamborim.Configurar(380f, 1500f, 2.0f, 30f, 0.6f, _taxa, 23);
                    _chimbal.Configurar(4200f, 70f, _taxa, 24);
                    _triangulo.Configurar(2950f, 16f, _taxa);
                    break;
                case Genero.Samba:
                    _surdo.Configurar(88f, 62f, 20f, 4.2f, 0.04f, 31);       // surdo de marcação
                    _bumbo.Configurar(70f, 52f, 30f, 9f, 0.05f, 32);
                    _tamborim.Configurar(520f, 2400f, 2.4f, 40f, 0.7f, _taxa, 33);
                    _caixa.Configurar(240f, 1700f, 1.2f, 30f, 0.75f, _taxa, 34);
                    _ganza.Configurar(3600f, 55f, _taxa, 35);
                    break;
                case Genero.Sertanejo:
                    _bumbo.Configurar(72f, 50f, 30f, 9f, 0.08f, 41);
                    _caixa.Configurar(230f, 1800f, 1.2f, 24f, 0.78f, _taxa, 42);
                    _chimbal.Configurar(4300f, 58f, _taxa, 43);
                    break;
                case Genero.Rock:
                    _bumbo.Configurar(80f, 48f, 34f, 10f, 0.14f, 51);
                    _caixa.Configurar(200f, 1500f, 0.9f, 20f, 0.80f, _taxa, 52);
                    _chimbal.Configurar(5000f, 46f, _taxa, 53);
                    break;
                case Genero.Gospel:
                    _bumbo.Configurar(66f, 48f, 26f, 8f, 0.03f, 61);
                    _caixa.Configurar(210f, 1600f, 1.0f, 22f, 0.6f, _taxa, 62);
                    _tamborim.Configurar(430f, 2400f, 1.6f, 26f, 0.85f, _taxa, 63);   // pandeiro do louvor
                    _chimbal.Configurar(4000f, 54f, _taxa, 64);
                    break;
                default:  // notícias — redação, não banda
                    _chimbal.Configurar(4200f, 120f, _taxa, 71);
                    _tamborim.Configurar(900f, 3000f, 3f, 90f, 0.9f, _taxa, 72);
                    _triangulo.Configurar(2600f, 24f, _taxa);
                    break;
            }
        }

        // ------------------------------------------------------------------ sequenciador
        private static int M(int a = -1, int b = -1, int c = -1, int d = -1, int e = -1, int f = -1, int g = -1, int h = -1)
        {
            int m = 0;
            if (a >= 0) m |= 1 << a; if (b >= 0) m |= 1 << b; if (c >= 0) m |= 1 << c; if (d >= 0) m |= 1 << d;
            if (e >= 0) m |= 1 << e; if (f >= 0) m |= 1 << f; if (g >= 0) m |= 1 << g; if (h >= 0) m |= 1 << h;
            return m;
        }

        private static bool Bate(int mascara, int passo) => (mascara & (1 << passo)) != 0;

        // Padrões de 16 semicolcheias — a assinatura rítmica de cada gênero, em três levadas. Qual
        // delas toca é escolha da semente da faixa, e é o que faz "Bota o Pé na Laje" não ser
        // "Automotivo do Beco 3" com outro nome.
        private static readonly int[] kFunkBumbo    = { M(0, 3, 6, 10, 13), M(0, 3, 6, 8, 11, 14), M(0, 4, 6, 10, 12) };
        private static readonly int[] kFunkClap     = { M(4, 12), M(4, 12), M(4, 11, 12) };
        private static readonly int[] kFunkAtabaque = { M(2, 5, 8, 11, 14), M(2, 7, 10, 13), M(1, 5, 9, 13) };
        private static readonly int[] kSertBumbo    = { M(0, 8), M(0, 8, 11), M(0, 6, 8) };
        private static readonly int[] kSertCaixa    = { M(4, 12), M(4, 12), M(4, 12, 14) };
        private static readonly int[] kSertViola    = { M(0, 3, 4, 6, 8, 11, 12, 14), M(0, 2, 4, 7, 8, 10, 12, 15), M(2, 3, 6, 7, 10, 11, 14, 15) };
        private static readonly int[] kForroGrave   = { M(0, 6), M(0, 6, 10), M(0, 5, 8, 14) };          // baião · xote · arrasta-pé
        private static readonly int[] kForroTapa    = { M(4, 12), M(3, 11), M(4, 10, 12) };
        private static readonly int[] kSambaSurdo   = { M(4, 12), M(4, 12), M(4, 11, 12) };
        private static readonly int[] kTelecoTeco   = { M(0, 3, 6, 10, 12), M(0, 2, 5, 8, 11, 14), M(0, 3, 4, 7, 10, 13) };
        private static readonly int[] kCavaco       = { M(2, 6, 10, 14), M(3, 7, 11, 15), M(2, 3, 10, 11) };
        private static readonly int[] kRockBumbo    = { M(0, 6, 8, 14), M(0, 3, 8, 11), M(0, 8) };
        private static readonly int   kSambaGhost   = M(2, 10);
        private static readonly int   kOitavos      = M(0, 2, 4, 6, 8, 10, 12, 14);

        private void Disparar(Programa prog)
        {
            var est = prog.Estacao;
            int passo    = (int)(_passoGlobal & 15);
            long compasso = _passoGlobal >> 4;
            int naFrase  = (int)(compasso & 7);
            int frase    = (int)(compasso >> 3);

            // a frase é o que impede a fadiga de laço: a cada 8 compassos a melodia é resemeada
            if (passo == 0 && naFrase == 0) _rndMelodia = new CaosDsp.Ruido(prog.Semente + frase * 7919);

            int secao = frase & 3;                       // 0 abertura · 1 estrofe · 2 refrão · 3 refrão+solo
            bool virada = naFrase == 7 && passo >= 12;   // virada no fim da frase
            float intensidade = (secao == 0 ? 0.62f : secao == 1 ? 0.82f : 1f) * _densidade;

            if (passo == 0) _acordeAtual = (int)(compasso & 3);

            switch (est.Genero)
            {
                case Genero.Funk:      DispararFunk(est, passo, intensidade, virada); break;
                case Genero.Sertanejo: DispararSertanejo(est, passo, intensidade, virada); break;
                case Genero.Forro:     DispararForro(est, passo, intensidade, virada); break;
                case Genero.Samba:     DispararSamba(est, passo, intensidade, virada); break;
                case Genero.Gospel:    DispararGospel(est, passo, intensidade); break;
                case Genero.Rock:      DispararRock(est, passo, intensidade, virada); break;
                default:               DispararRedacao(passo); break;
            }

            if (secao >= 1 && est.Genero != Genero.Noticias) Solar(est, passo, secao == 3);
        }

        // ---- funk: tamborzão, clap no contratempo e o sub que faz a laje tremer ----
        private void DispararFunk(Estacao est, int passo, float forca, bool virada)
        {
            if (Bate(kFunkBumbo[_variante], passo) || (virada && (passo & 1) == 0)) _bumbo.Tocar(1.0f * forca);
            if (Bate(kFunkClap[_variante], passo))     _caixa.Tocar(0.85f * forca);
            if (Bate(kFunkAtabaque[_variante], passo)) _tamborim.Tocar(0.42f * forca);
            if (Bate(kOitavos, passo))      _chimbal.Tocar(passo % 4 == 0 ? 0.30f : 0.18f);
            if (virada)                     _caixa.Tocar(0.5f + 0.12f * passo);

            if (Bate(kFunkBumbo[_variante], passo)) _baixo.Tocar(Nota(est, 0, 1), 1f);   // sub casado com o bumbo
            if (passo == 0 || passo == 8) _alvoPalheta = 0.22f * forca;
            _freqPalheta = Nota(est, GrauDoAcorde(est, 2), 3);
        }

        // ---- sertanejo: viola em terças, baixo alternando fundamental e quinta ----
        private void DispararSertanejo(Estacao est, int passo, float forca, bool virada)
        {
            if (Bate(kSertBumbo[_variante], passo)) _bumbo.Tocar(0.9f * forca);
            if (Bate(kSertCaixa[_variante], passo)) _caixa.Tocar(0.75f * forca);
            if (Bate(kOitavos, passo))   _chimbal.Tocar(passo % 4 == 0 ? 0.22f : 0.13f);
            if (virada)                  _caixa.Tocar(0.55f);

            if (Bate(kSertViola[_variante], passo))
            {
                // duas cordas em terça: a marca do sertanejo de raiz
                _cordas[0].Tocar(Nota(est, GrauDoAcorde(est, 0), 4), 0.36f * forca, _taxa, 0.55f, 0.55f);
                _cordas[1].Tocar(Nota(est, GrauDoAcorde(est, 1), 4), 0.30f * forca, _taxa, 0.55f, 0.6f);
            }
            if (passo == 0 || passo == 8)
                _baixo.Tocar(Nota(est, passo == 0 ? 0 : 7, 1), 0.9f);
            else if (passo == 4 || passo == 12)
                _baixo.Tocar(Nota(est, passo == 4 ? 7 : 0, 1), 0.7f);
        }

        // ---- forró: zabumba (grave + tapa), triângulo nas colcheias, sanfona segurando ----
        private void DispararForro(Estacao est, int passo, float forca, bool virada)
        {
            if (Bate(kForroGrave[_variante], passo)) _bumbo.Tocar(1.0f * forca);
            if (Bate(kForroTapa[_variante], passo))  _caixa.Tocar(0.62f * forca);
            if (Bate(kOitavos, passo))    _triangulo.Tocar(passo % 4 == 0 ? 0.30f : 0.16f);
            if (virada)                   _bumbo.Tocar(0.8f);

            if (passo == 0 || passo == 6 || passo == 8 || passo == 14)
                _baixo.Tocar(Nota(est, passo < 8 ? 0 : 7, 1), 0.85f);

            _alvoPalheta = 0.34f * forca;      // sanfona não para
            _freqPalheta = Nota(est, GrauDoAcorde(est, 1), 3);
        }

        // ---- samba/MPB: surdo no 2 e no 4, teleco-teco no tamborim, cavaco no contratempo ----
        private void DispararSamba(Estacao est, int passo, float forca, bool virada)
        {
            if (Bate(kSambaSurdo[_variante], passo)) _surdo.Tocar(1.0f * forca);
            if (Bate(kSambaGhost, passo)) _surdo.Tocar(0.24f * forca);
            if (Bate(kTelecoTeco[_variante], passo)) _tamborim.Tocar(0.55f * forca);
            _ganza.Tocar((passo & 1) == 0 ? 0.22f : 0.13f);     // ganzá nas 16 semicolcheias
            if (virada) _caixa.Tocar(0.45f);

            if (Bate(kCavaco[_variante], passo))
            {
                _cordas[0].Tocar(Nota(est, GrauDoAcorde(est, 1), 4), 0.28f * forca, _taxa, 0.35f, 0.7f);
                _cordas[1].Tocar(Nota(est, GrauDoAcorde(est, 2), 4), 0.24f * forca, _taxa, 0.35f, 0.72f);
                _cordas[2].Tocar(Nota(est, GrauDoAcorde(est, 3), 5), 0.18f * forca, _taxa, 0.35f, 0.75f);
            }
            if (passo == 0 || passo == 6 || passo == 8 || passo == 14)
                _baixo.Tocar(Nota(est, passo == 6 || passo == 14 ? 7 : 0, 1), 0.85f);
        }

        // ---- gospel: órgão, coral e piano; percussão discreta ----
        private void DispararGospel(Estacao est, int passo, float forca)
        {
            if (passo == 0 || passo == 8) _bumbo.Tocar(0.45f * forca);
            if (passo == 8)               _caixa.Tocar(0.30f * forca);
            if (passo == 4 || passo == 12) _tamborim.Tocar(0.34f * forca);   // pandeiro no 2 e no 4
            if (Bate(kOitavos, passo))     _chimbal.Tocar(passo % 4 == 0 ? 0.16f : 0.10f);
            if (passo == 0 || passo == 8)
            {
                _baixo.Tocar(Nota(est, 0, 1), 0.75f);
                _cordas[0].Tocar(Nota(est, GrauDoAcorde(est, 0), 4), 0.22f, _taxa, 1.1f, 0.35f);
                _cordas[1].Tocar(Nota(est, GrauDoAcorde(est, 2), 4), 0.18f, _taxa, 1.1f, 0.35f);
                _cordas[2].Tocar(Nota(est, GrauDoAcorde(est, 1), 5), 0.14f, _taxa, 0.8f, 0.3f);
            }
            _alvoPalheta = 0.26f;
            _alvoNaipe   = 0.30f * forca;
            _freqPalheta = Nota(est, GrauDoAcorde(est, 0), 3);
            _freqNaipe   = Nota(est, GrauDoAcorde(est, 2), 4);
        }

        private void DispararRock(Estacao est, int passo, float forca, bool virada)
        {
            if (Bate(kRockBumbo[_variante], passo)) _bumbo.Tocar(0.95f * forca);
            if (Bate(kSertCaixa[_variante], passo)) _caixa.Tocar(0.85f * forca);
            if (Bate(kOitavos, passo))   _chimbal.Tocar(passo % 4 == 0 ? 0.26f : 0.16f);
            if (virada)                  _caixa.Tocar(0.6f);
            if (passo % 4 == 0)          _baixo.Tocar(Nota(est, 0, 1), 0.9f);
            _alvoPalheta = 0.24f * forca;
            _freqPalheta = Nota(est, GrauDoAcorde(est, 0), 2);
        }

        /// <summary>
        /// Redação de notícias: zumbido de sala, teletipo e o tique do relógio de parede. É a cama que
        /// sustenta a locução — antes essa estação sintetizava <c>null</c> e ficava literalmente muda.
        /// </summary>
        private void DispararRedacao(int passo)
        {
            if (passo == 0) _triangulo.Tocar(0.10f);                 // o tique da hora certa
            if (_rndMelodia.Sorte() < 0.22f) _tamborim.Tocar(0.05f); // teletipo ao fundo
            if (_rndMelodia.Sorte() < 0.10f) _chimbal.Tocar(0.03f);
            _alvoPalheta = 0f;
            _alvoNaipe   = 0f;
        }

        /// <summary>Melodia: caminhada determinística pelas notas do acorde, com passagens.</summary>
        private void Solar(Estacao est, int passo, bool insistente)
        {
            if ((passo & 1) != 0) return;
            float chance = (insistente ? 0.55f : 0.30f) * _densidade;
            if (_rndMelodia.Sorte() > chance) return;

            int grau = GrauDoAcorde(est, (int)(_rndMelodia.Sorte() * 4f));
            if (_rndMelodia.Sorte() < 0.25f) grau += 2;              // nota de passagem
            _solo.Tocar(Nota(est, grau, 5), insistente ? 0.30f : 0.22f, _rndMelodia.Sorte() < 0.4f);
        }

        // ------------------------------------------------------------------ harmonia
        // progressões por gênero, em semitons a partir da tônica
        private static readonly int[]  kProgFunk      = { 0, 0, 8, 7 };
        private static readonly int[]  kProgSertanejo = { 0, 7, 9, 5 };
        private static readonly int[]  kProgForro     = { 0, 5, 7, 0 };
        private static readonly int[]  kProgSamba     = { 2, 7, 0, 9 };
        private static readonly int[]  kProgGospel    = { 0, 5, 9, 7 };
        private static readonly int[]  kProgRock      = { 0, 10, 5, 0 };

        // 0 = maior · 1 = menor · 2 = dominante com sétima
        private static readonly byte[] kQualFunk      = { 1, 1, 0, 2 };
        private static readonly byte[] kQualSertanejo = { 0, 2, 1, 0 };
        private static readonly byte[] kQualForro     = { 0, 0, 2, 0 };
        private static readonly byte[] kQualSamba     = { 1, 2, 0, 2 };
        private static readonly byte[] kQualGospel    = { 0, 0, 1, 2 };
        private static readonly byte[] kQualRock      = { 0, 0, 0, 0 };

        private int[] Progressao(Genero g)
        {
            switch (g)
            {
                case Genero.Funk:      return kProgFunk;
                case Genero.Sertanejo: return kProgSertanejo;
                case Genero.Forro:     return kProgForro;
                case Genero.Gospel:    return kProgGospel;
                case Genero.Rock:      return kProgRock;
                default:               return kProgSamba;
            }
        }

        private byte[] Qualidades(Genero g)
        {
            switch (g)
            {
                case Genero.Funk:      return kQualFunk;
                case Genero.Sertanejo: return kQualSertanejo;
                case Genero.Forro:     return kQualForro;
                case Genero.Gospel:    return kQualGospel;
                case Genero.Rock:      return kQualRock;
                default:               return kQualSamba;
            }
        }

        /// <summary>Semitom da <paramref name="voz"/>-ésima nota do acorde da vez (0 = baixo do acorde).</summary>
        private int GrauDoAcorde(Estacao est, int voz)
        {
            var prog = Progressao(est.Genero);
            var qual = Qualidades(est.Genero);
            int i = (_acordeAtual + _rotacao) & 3;    // a faixa entra em outro ponto do ciclo
            int fundamental = prog[i];
            int terca = qual[i] == 1 ? 3 : 4;
            int setima = qual[i] == 0 ? 11 : 10;
            switch (voz & 3)
            {
                case 0:  return fundamental;
                case 1:  return fundamental + terca;
                case 2:  return fundamental + 7;
                default: return fundamental + setima;
            }
        }

        /// <summary>
        /// Semitom + oitava → Hz. Lá1 = 55 Hz é a referência. A estação define a tônica da casa e a
        /// faixa desloca alguns semitons em volta dela: as músicas se distinguem sem a emissora perder
        /// a cor.
        /// </summary>
        private float Nota(Estacao est, int semitom, int oitava)
            => 55f * Mathf.Pow(2f, oitava - 1 + (est.Raiz + _transpose + semitom) / 12f);

        // ------------------------------------------------------------------ render
        private float RenderBanda(float dt, Estacao est)
        {
            float s = 0f;

            switch (est.Genero)
            {
                case Genero.Funk:
                    s += _bumbo.Render(dt) * 1.15f
                       + _caixa.Render(dt, _taxa) * 0.55f
                       + _tamborim.Render(dt, _taxa) * 0.30f
                       + _chimbal.Render(dt) * 0.28f
                       + _baixo.Render(dt, _taxa, 2.2f, 700f) * 0.95f
                       + _palheta.Render(dt, _taxa, _freqPalheta, _alvoPalheta, 0.010f, 2600f, 0f) * 0.5f;
                    _alvoPalheta *= 0.9994f;
                    break;

                case Genero.Sertanejo:
                    s += _bumbo.Render(dt) * 0.85f
                       + _caixa.Render(dt, _taxa) * 0.45f
                       + _chimbal.Render(dt) * 0.22f
                       + _baixo.Render(dt, _taxa, 1.4f, 500f) * 0.75f
                       + (_cordas[0].Render() + _cordas[1].Render()) * 0.42f;
                    break;

                case Genero.Forro:
                    s += _bumbo.Render(dt) * 1.0f
                       + _caixa.Render(dt, _taxa) * 0.42f
                       + _triangulo.Render(dt) * 0.20f
                       + _baixo.Render(dt, _taxa, 1.6f, 520f) * 0.72f
                       + _palheta.Render(dt, _taxa, _freqPalheta, _alvoPalheta, 0.016f, 3400f, 5.2f) * 0.62f;
                    break;

                case Genero.Samba:
                    s += _surdo.Render(dt) * 0.95f
                       + _tamborim.Render(dt, _taxa) * 0.34f
                       + _caixa.Render(dt, _taxa) * 0.25f
                       + _ganza.Render(dt) * 0.16f
                       + _baixo.Render(dt, _taxa, 1.5f, 450f) * 0.78f
                       + (_cordas[0].Render() + _cordas[1].Render() + _cordas[2].Render()) * 0.34f;
                    break;

                case Genero.Gospel:
                    s += _bumbo.Render(dt) * 0.55f
                       + _caixa.Render(dt, _taxa) * 0.25f
                       + _tamborim.Render(dt, _taxa) * 0.42f
                       + _chimbal.Render(dt) * 0.30f
                       + _baixo.Render(dt, _taxa, 0.9f, 420f) * 0.70f
                       + (_cordas[0].Render() + _cordas[1].Render() + _cordas[2].Render()) * 0.42f
                       + _palheta.Render(dt, _taxa, _freqPalheta, _alvoPalheta, 0.002f, 5200f, 6.5f) * 0.50f
                       + _naipe.Render(dt, _freqNaipe, _alvoNaipe, 0.008f, 0.9f) * 0.45f;
                    break;

                case Genero.Rock:
                    s += _bumbo.Render(dt) * 0.95f
                       + _caixa.Render(dt, _taxa) * 0.6f
                       + _chimbal.Render(dt) * 0.26f
                       + _baixo.Render(dt, _taxa, 1.2f, 600f) * 0.8f
                       + CaosDsp.Saturar(_palheta.Render(dt, _taxa, _freqPalheta, _alvoPalheta, 0.008f, 3000f, 0f) * 3f) * 0.35f;
                    break;

                default:   // redação
                    s += _tamborim.Render(dt, _taxa) * 0.20f
                       + _chimbal.Render(dt) * 0.20f
                       + _triangulo.Render(dt) * 0.12f
                       + _rndPortadora.Proximo() * 0.010f;
                    break;
            }

            if (est.Genero != Genero.Noticias) s += _solo.Render(dt, _taxa, 2.6f, 2400f, 2.4f, 40f) * 0.34f;
            return s * 0.62f;
        }

        /// <summary>
        /// A vinheta: quatro notas tiradas do <c>id</c> da estação, na tonalidade dela. É o logotipo
        /// sonoro — toca ao sintonizar e antes de cada bloco, e é o que faz o jogador reconhecer a
        /// emissora de ouvido, antes de ler o HUD.
        /// </summary>
        private static readonly int[] kEscalaMaior = { 0, 2, 4, 5, 7, 9, 11 };

        private float RenderVinheta(float dt, Estacao est)
        {
            const float kPorNota = 0.19f;
            _vinhetaT -= dt;

            int nota = Mathf.Clamp((int)((_vinhetaDur - _vinhetaT) / kPorNota), 0, 3);
            if (nota != _vinhetaNota)
            {
                _vinhetaNota = nota;
                int grau = kEscalaMaior[Mathf.Clamp(est.Motivo[nota], 0, 6)];
                _cordas[2].Tocar(Nota(est, grau, 5), 0.42f, _taxa, 0.6f, 0.45f);
            }

            return _cordas[2].Render() * 0.55f
                 + _naipe.Render(dt, Nota(est, 0, 3), 0.30f, 0.006f, 0.6f) * 0.42f;
        }

        /// <summary>
        /// Cadeia do transmissor: banda passante, compressão de rádio, presença, chiado de portadora e
        /// (na AM) o desvanecimento lento que toda onda média tem à noite. É o que dá <b>lugar</b> ao
        /// som — a mesma música soa diferente em cada estação porque passa por caminhos diferentes.
        /// </summary>
        private float CadeiaDeTransmissao(float x, Estacao est, float dt)
        {
            // A ordem aqui é a da cadeia real, e ela importa mais do que parece. O chiado de portadora
            // e a distorção do processador nascem ANTES do filtro de banda, e o filtro de banda é a
            // última coisa do caminho — no rádio de verdade quem corta por último é a FI do receptor.
            //
            // Com o chiado somado no fim e a saturação depois do filtro, a medição mostrava a AM com
            // mais energia acima de 4 kHz do que a FM: exatamente o contrário do que uma onda média é.
            // O ruído passava por fora do filtro e os harmônicos da saturação nasciam depois dele.
            x += _rndPortadora.Proximo() * est.Chiado;
            x = _presenca.Filtrar(x);
            x = _comp.Processar(x, 0.22f, 2.5f + 7f * est.Compressao, 0.02f, 0.0006f) * (1f + est.Compressao * 0.9f);
            x = CaosDsp.Saturar(x);

            x = _hpTx.Filtrar(x);
            x = _lpTx.Filtrar(x);

            if (est.Am)
            {
                // segundo estágio só na AM: 24 dB/oitava é a ordem de um filtro de FI de onda média, e
                // é o que faz a estação soar realmente estreita em vez de só um pouco abafada
                x = _lpTxFi.Filtrar(x);

                _faseDesvanece = CaosDsp.Avancar(_faseDesvanece, 0.043f * dt);
                float alvo = 0.72f + 0.28f * CaosDsp.Seno(_faseDesvanece);
                _fadeDesvanece += (alvo - _fadeDesvanece) * dt * 1.5f;
                x *= _fadeDesvanece;
            }

            return x;
        }

        /// <summary>Chiado de dial: ruído em banda varrendo, com o assobio de batimento entre portadoras.</summary>
        private float Estatica(float dt)
        {
            _faseVarredura = CaosDsp.Avancar(_faseVarredura, 0.9f * dt);
            float alvo = 900f + 2600f * Mathf.Abs(CaosDsp.Seno(_faseVarredura));
            _faseAssobio = CaosDsp.Avancar(_faseAssobio, alvo * dt);
            float assobio = CaosDsp.Seno(_faseAssobio) * 0.10f;
            return _sintoniaBanda.Filtrar(_rndSintonia.Proximo()) * 1.8f + assobio;
        }
    }
}
