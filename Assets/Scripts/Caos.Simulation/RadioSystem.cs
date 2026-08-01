using System.Collections.Generic;
using Caos.Data;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Rádio do carro (docs/12 §12.6). As estações vêm de <c>radio.json</c> — nome, gênero, slogan,
    /// alinhamento de faixas e as falas do locutor — e a <b>música é sintetizada em runtime</b> por
    /// gênero: funk com tamborzão, sertanejo em terças, forró com zabumba e triângulo, samba/MPB
    /// sincopado, gospel em pad e a AM de notícias só com a voz do locutor no ar.
    ///
    /// Sem nenhum arquivo de áudio no projeto: tudo é PCM gerado com a semente da faixa, então a mesma
    /// música toca igual em toda máquina. Só toca dentro do veículo, como manda a tradição do gênero.
    /// </summary>
    public class RadioSystem : MonoBehaviour
    {
        private const int   kSampleRate   = 22050;
        private const float kCompassos    = 4f;     // duração do loop sintetizado
        private const float kDuracaoFaixa = 52f;    // segundos até trocar de faixa
        private const float kDuracaoFala  = 4.5f;   // quanto tempo a locução fica no HUD

        private readonly List<RadioStationDto> _estacoes = new List<RadioStationDto>();
        private PlayerVehicleLink _link;
        private AudioSource       _source;
        private AudioClip         _clipAtual;

        private int   _estacao, _faixa;
        private float _tempoFaixa, _falaAte;

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

            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true; _source.spatialBlend = 0f; _source.playOnAwake = false; _source.volume = 0f;

            _estacao = Random.Range(0, _estacoes.Count);
            TrocarFaixa(0);
        }

        private void Update()
        {
            if (_estacoes.Count == 0) return;

            if (GameInput.RadioNext)   Sintonizar(+1);
            if (GameInput.RadioToggle) Ligado = !Ligado;

            bool noAr = NoAr;
            if (_source != null)
                _source.volume = Mathf.Lerp(_source.volume, noAr ? 0.22f : 0f, Time.deltaTime * 3f);

            if (!noAr) return;

            _tempoFaixa += Time.deltaTime;
            if (_tempoFaixa >= kDuracaoFaixa) TrocarFaixa(_faixa + 1);
        }

        /// <summary>Passa pra próxima estação (mantém a memória de faixa de cada uma).</summary>
        public void Sintonizar(int delta)
        {
            _estacao = (_estacao + delta + _estacoes.Count) % _estacoes.Count;
            Ligado = true;
            TrocarFaixa(0);
        }

        private void TrocarFaixa(int indice)
        {
            var est = _estacoes[_estacao];
            Estacao = est.nome;
            Slogan  = est.slogan;
            Cor     = CityPalette.Parse(est.corHex, Color.white);

            int n = est.faixas != null ? est.faixas.Count : 0;
            _faixa = n > 0 ? ((indice % n) + n) % n : 0;
            _tempoFaixa = 0f;

            var f = n > 0 ? est.faixas[_faixa] : null;
            Faixa = f != null ? $"{f.titulo} — {f.artista}" : est.nome;

            // vinheta/locução ao entrar na faixa
            if (est.locucoes != null && est.locucoes.Count > 0)
            {
                Locucao  = est.locucoes[Random.Range(0, est.locucoes.Count)];
                _falaAte = Time.time + kDuracaoFala;
            }

            if (_source == null) return;
            float bpm = f != null && f.bpm > 20f ? f.bpm : (est.bpm > 20f ? est.bpm : 100f);
            int semente = f != null ? f.semente : _estacao * 31 + _faixa;

            var novo = Sintetizar(est.genero, bpm, semente);
            _source.Stop();
            if (_clipAtual != null) Destroy(_clipAtual);
            _clipAtual = novo;
            _source.clip = novo;
            if (novo != null) _source.Play();
        }

        // ================================================================== síntese por gênero
        private AudioClip Sintetizar(string genero, float bpm, int semente)
        {
            if (genero == "noticias") return null;   // AM de notícias: só a voz do locutor no HUD

            var rnd = new System.Random(semente);
            float beat = 60f / Mathf.Max(40f, bpm);
            float dur  = beat * 4f * kCompassos;
            int   len  = Mathf.Max(1024, Mathf.RoundToInt(dur * kSampleRate));
            var   buf  = new float[len];

            // progressão I–V–vi–IV (a mais brasileira das progressões de rádio)
            float[] graus = { 1f, 1.5f, 1.6818f, 1.3348f };
            float raiz = 110f * Mathf.Pow(2f, (float)rnd.NextDouble() * 0.25f);

            for (int i = 0; i < len; i++)
            {
                float t     = (float)i / kSampleRate;
                float pos   = t / beat;                 // posição em batidas
                int   compasso = Mathf.FloorToInt(pos / 4f) % 4;
                float noBeat= pos - Mathf.Floor(pos);   // 0..1 dentro da batida
                float acorde= raiz * graus[compasso];

                float s = 0f;
                switch (genero)
                {
                    case "funk":      s = Funk(t, pos, noBeat, acorde); break;
                    case "sertanejo": s = Sertanejo(t, pos, noBeat, acorde); break;
                    case "forro":     s = Forro(t, pos, noBeat, acorde); break;
                    case "gospel":    s = Gospel(t, acorde); break;
                    default:          s = Samba(t, pos, noBeat, acorde); break;   // mpb/samba/rock
                }
                buf[i] = Mathf.Clamp(s, -1f, 1f) * 0.5f;
            }

            var clip = AudioClip.Create("radio_" + genero + "_" + semente, len, 1, kSampleRate, false);
            clip.SetData(buf, 0);
            return clip;
        }

        // ---- vozes ----
        private static float Env(float x, float k) => Mathf.Exp(-x * k);
        /// <summary>Ruído determinístico (chiado/percussão) — hash barato, sem Random por amostra.</summary>
        private static float Ruido(float t) => (Mathf.Abs(Mathf.Sin(t * 99991f) * 43758.5453f) % 1f) * 2f - 1f;

        /// <summary>Funk: bumbo curto e sub grave (o "tamborzão"), caixa no contratempo, hat em colcheias.</summary>
        private static float Funk(float t, float pos, float noBeat, float acorde)
        {
            float b = pos % 4f;
            bool kick = (b < 0.06f) || (b > 0.98f && b < 1.06f) || (b > 2.48f && b < 2.56f) || (b > 3.48f && b < 3.56f);
            float k = kick ? Mathf.Sin(2f * Mathf.PI * 52f * t) * Env(noBeat, 22f) * 1.1f : 0f;
            float caixa = (b > 1.98f && b < 2.10f) || (b > 3.98f) ? Ruido(t) * Env(noBeat, 30f) * 0.35f : 0f;
            float hat   = (noBeat < 0.04f || (noBeat > 0.48f && noBeat < 0.52f)) ? Ruido(t * 3f) * 0.12f : 0f;
            float sub   = Mathf.Sin(2f * Mathf.PI * (acorde * 0.5f) * t) * 0.30f;
            return k + caixa + hat + sub;
        }

        /// <summary>Sertanejo: violão em terças, baixo alternando fundamental e quinta.</summary>
        private static float Sertanejo(float t, float pos, float noBeat, float acorde)
        {
            float baixo = Mathf.Sin(2f * Mathf.PI * (Mathf.FloorToInt(pos) % 2 == 0 ? acorde * 0.5f : acorde * 0.75f) * t) * 0.28f;
            float corda1 = Mathf.Sin(2f * Mathf.PI * acorde * 2f * t) * Env(noBeat, 3.5f) * 0.20f;
            float corda2 = Mathf.Sin(2f * Mathf.PI * acorde * 2.52f * t) * Env(noBeat, 3.5f) * 0.16f;   // a terça
            float kick   = noBeat < 0.05f ? Mathf.Sin(2f * Mathf.PI * 60f * t) * Env(noBeat, 26f) * 0.5f : 0f;
            return baixo + corda1 + corda2 + kick;
        }

        /// <summary>Forró: zabumba (grave + tapa), triângulo em colcheias e sanfona segurando o acorde.</summary>
        private static float Forro(float t, float pos, float noBeat, float acorde)
        {
            float b = pos % 2f;
            float zabumba = (b < 0.06f) ? Mathf.Sin(2f * Mathf.PI * 70f * t) * Env(noBeat, 18f) * 0.9f : 0f;
            float tapa    = (b > 1.45f && b < 1.55f) ? Ruido(t) * Env(noBeat, 40f) * 0.25f : 0f;
            float triangulo = (noBeat < 0.03f || (noBeat > 0.5f && noBeat < 0.53f)) ? Ruido(t * 7f) * 0.10f : 0f;
            float sanfona = (Mathf.Sin(2f * Mathf.PI * acorde * 2f * t) + Mathf.Sin(2f * Mathf.PI * acorde * 3f * t)) * 0.16f;
            return zabumba + tapa + triangulo + sanfona;
        }

        /// <summary>Samba/MPB: surdo no 2 e no 4, cavaco sincopado e um contrabaixo redondo.</summary>
        private static float Samba(float t, float pos, float noBeat, float acorde)
        {
            float b = pos % 4f;
            float surdo = (b > 1.95f && b < 2.06f) || (b > 3.95f) ? Mathf.Sin(2f * Mathf.PI * 64f * t) * Env(noBeat, 14f) * 0.8f : 0f;
            float cavaco = (noBeat > 0.24f && noBeat < 0.30f) || (noBeat > 0.72f && noBeat < 0.78f)
                ? (Mathf.Sin(2f * Mathf.PI * acorde * 3f * t) + Mathf.Sin(2f * Mathf.PI * acorde * 3.78f * t)) * Env(noBeat, 6f) * 0.18f : 0f;
            float baixo = Mathf.Sin(2f * Mathf.PI * acorde * 0.5f * t) * 0.26f;
            float ganza = Ruido(t * 5f) * 0.05f;
            return surdo + cavaco + baixo + ganza;
        }

        /// <summary>Gospel: pad sustentado em acorde maior, sem percussão.</summary>
        private static float Gospel(float t, float acorde)
        {
            float pad = Mathf.Sin(2f * Mathf.PI * acorde * t) * 0.22f
                      + Mathf.Sin(2f * Mathf.PI * acorde * 1.26f * t) * 0.16f
                      + Mathf.Sin(2f * Mathf.PI * acorde * 1.5f  * t) * 0.16f;
            float lfo = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.15f * t);
            return pad * lfo;
        }

        private void OnDestroy()
        {
            if (_clipAtual != null) Destroy(_clipAtual);
        }
    }
}
