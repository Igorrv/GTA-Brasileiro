using Caos.Core;

namespace Caos.Gameplay
{
    /// <summary>
    /// Estado vivo do personagem: Fome (saciedade), <b>Sede</b>, Energia, Sanidade e Saúde — escala 0–100.
    /// Decaimento e efeitos conforme docs/00 (Bíblia) e docs/05/10.
    ///
    /// A Sede é a necessidade mais agressiva: cai quase o dobro da fome e acelera no sol forte e quando
    /// o jogador está ativo. É o motivo de existir água de coco, caldo de cana e a latinha do isopor.
    /// </summary>
    public sealed class PlayerAttributes : ITickable
    {
        public float Fome     { get; private set; } = 70f;
        public float Sede     { get; private set; } = 70f;
        public float Energia  { get; private set; } = 70f;
        public float Sanidade { get; private set; } = 60f;
        public float Saude    { get; private set; } = 100f;

        /// <true> se correndo ou dirigindo (Energia decai 3× mais rápido).</true>
        public bool Ativo { get; set; } = false;

        /// <summary>Ligado pelo mundo quando o clima é de sol forte — dá mais sede.</summary>
        public bool Calor { get; set; } = false;

        public int Order => 20;

        private bool _morteAnunciada;

        // decaimentos por SEGUNDO (convertidos dos valores/min do docs/00)
        private const float kFomeDecay     = 0.5f / 60f;     // -0,5/min
        private const float kSedeDecay     = 0.9f / 60f;     // -0,9/min — sede aperta antes da fome
        private const float kEnergiaDecay  = 0.4f / 60f;     // -0,4/min
        private const float kEnergiaDecayAtivo = 1.2f / 60f; // -1,2/min (correndo/dirigindo)

        public void Tick(float dt)
        {
            Fome = Clamp(Fome - kFomeDecay * dt);

            float sedeDecay = kSedeDecay * (Calor ? 1.6f : 1f) * (Ativo ? 1.4f : 1f);
            Sede = Clamp(Sede - sedeDecay * dt);

            float energiaDecay = kEnergiaDecay;
            if (Ativo) energiaDecay = kEnergiaDecayAtivo;
            if (Sede <= 20f) energiaDecay *= 1.5f;           // desidratado cansa mais rápido
            Energia = Clamp(Energia - energiaDecay * dt);

            // Saúde: regenera bem alimentado e hidratado; drena passando fome ou sede
            if (Fome <= 0f) Saude -= (2f / 60f) * dt;
            if (Sede <= 0f) Saude -= (3f / 60f) * dt;        // desidratação machuca mais rápido
            else if (Fome >= 85f && Sede >= 85f && Saude < 100f) Saude += (1f / 60f) * dt;
            Saude = Clamp(Saude);

            if (Energia <= 0f)
            {
                Energia = 25f;      // desmaio por exaustão; recupera um pouco para evitar loop de morte
                AnunciarMorte();
            }
            else if (Saude <= 0f) AnunciarMorte();
            else _morteAnunciada = false;
        }

        /// <summary>
        /// Publica <see cref="PlayerMorreu"/> uma vez por queda. Sem a trava, saúde zerada republicava o
        /// evento a cada frame durante toda a tela de WASTED — e quem escuta (o ciclo de vida) refazia o
        /// respawn 60 vezes por segundo.
        /// </summary>
        private void AnunciarMorte()
        {
            if (_morteAnunciada) return;
            _morteAnunciada = true;
            EventBus<PlayerMorreu>.Publish(new PlayerMorreu { });
        }

        /// <summary>Aplica delta bruto num atributo (vindo de ImpactResolver ou de um item comprado).</summary>
        public void Apply(string atributo, float delta)
        {
            switch (atributo)
            {
                case "fome":     Fome = Clamp(Fome + delta); break;
                case "sede":     Sede = Clamp(Sede + delta); break;
                case "energia":  Energia = Clamp(Energia + delta); break;
                case "sanidade": Sanidade = Clamp(Sanidade + delta); break;
                case "saude":    Saude = Clamp(Saude + delta); break;
            }
            PublishSnapshot();
        }

        public void PublishSnapshot()
        {
            EventBus<AtributosMudou>.Publish(new AtributosMudou
            { fome = Fome, sede = Sede, energia = Energia, sanidade = Sanidade, saude = Saude });
        }

        /// <summary>Restaura estado a partir do save.</summary>
        public void Hydrate(float fome, float sede, float energia, float sanidade, float saude)
        {
            Fome = fome; Sede = sede; Energia = energia; Sanidade = sanidade; Saude = saude;
            _morteAnunciada = false;
            PublishSnapshot();
        }

        private static float Clamp(float v) => v < 0f ? 0f : (v > 100f ? 100f : v);
    }
}
