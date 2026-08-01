using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Passada procedural para o <see cref="CharacterRig"/> — sem nenhum clipe de animação importado.
    ///
    /// A cadência é derivada da <b>velocidade real</b> medida no transform, e não de um timer solto: a
    /// frequência do passo é <c>velocidade / comprimento_da_passada</c>. Por isso o pé acompanha o chão
    /// em vez de "patinar", e o mesmo componente serve para o jogador (que acelera e freia) e para o
    /// pedestre (que anda em velocidade fixa).
    ///
    /// Camadas: passada (pernas/braços) · bob vertical no dobro da cadência · inclinação para a frente
    /// proporcional à velocidade · rolagem lateral na curva · respiração parado · agachada ao aterrissar.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        [Header("Passada")]
        [SerializeField] private float passoCaminhada = 1.55f;   // m por passada completa
        [SerializeField] private float amplitudePerna = 32f;     // graus no pico da corrida
        [SerializeField] private float amplitudeBraco = 42f;
        [SerializeField] private float velocidadeCorrida = 6.5f; // referência p/ escalar amplitude

        [Header("Corpo")]
        [SerializeField] private float inclinacaoMax = 9f;
        [SerializeField] private float rolagemMax   = 6f;

        private CharacterRig _rig;
        private Vector3 _posAnterior;
        private float   _fase;
        private float   _velSuave;
        private float   _giroSuave;
        private float   _aterrissagem;   // 0..1, decai depois de cair
        private float   _yAnterior;
        private bool    _noChaoAnterior = true;

        /// <summary>Velocidade horizontal medida (m/s) — o HUD/áudio pode usar.</summary>
        public float Velocidade => _velSuave;

        // ---- poses controladas pelo PlayerActions (0..1, suavizadas aqui dentro) ----
        /// <summary>Agachamento: dobra as pernas e abaixa o corpo.</summary>
        public float Agachar { get; set; }
        /// <summary>Sentado: coxas à frente, tronco ereto — para banco de praça, coreto e ponto.</summary>
        public bool  Sentado { get; set; }
        /// <summary>Levando algo à boca (comer/beber): 0..1.</summary>
        public float Consumindo { get; set; }

        private float _agacharSuave, _sentarSuave, _consumirSuave;

        public void Init(CharacterRig rig)
        {
            _rig = rig;
            Reancorar();
        }

        /// <summary>
        /// Reancora a medição de velocidade. Sem isso, todo teleporte (sair do carro, renascer, ou um
        /// pedestre voltar do pool noutro quarteirão) viraria uma velocidade absurda por um frame e o
        /// boneco daria um espasmo.
        /// </summary>
        private void OnEnable() => Reancorar();

        private void Reancorar()
        {
            _posAnterior = transform.position;
            _yAnterior   = transform.position.y;
            _velSuave    = 0f;
            _giroSuave   = transform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (_rig == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // ---- medição de movimento real ----
            Vector3 delta = transform.position - _posAnterior;
            _posAnterior = transform.position;

            Vector3 plano = new Vector3(delta.x, 0f, delta.z);
            float velInstantanea = Mathf.Min(plano.magnitude / dt, 12f);   // teto: teleporte não vira corrida
            _velSuave = Mathf.Lerp(_velSuave, velInstantanea, 1f - Mathf.Exp(-12f * dt));

            // giro (para a rolagem lateral do tronco na curva)
            float giro = Mathf.DeltaAngle(_giroSuave, transform.eulerAngles.y) / Mathf.Max(dt, 0.0001f);
            _giroSuave = Mathf.LerpAngle(_giroSuave, transform.eulerAngles.y, 1f - Mathf.Exp(-10f * dt));

            // queda/aterrissagem
            float quedaVel = (transform.position.y - _yAnterior) / dt;
            _yAnterior = transform.position.y;
            bool noChao = Mathf.Abs(quedaVel) < 2.5f;
            if (noChao && !_noChaoAnterior) _aterrissagem = 1f;
            _noChaoAnterior = noChao;
            _aterrissagem = Mathf.MoveTowards(_aterrissagem, 0f, dt * 3.2f);

            // ---- ciclo de passada ----
            float intensidade = Mathf.Clamp01(_velSuave / velocidadeCorrida);
            float passo = Mathf.Lerp(passoCaminhada, passoCaminhada * 1.45f, intensidade);
            _fase += (_velSuave / Mathf.Max(0.2f, passo)) * Mathf.PI * 2f * dt;
            if (_fase > Mathf.PI * 2f) _fase -= Mathf.PI * 2f;

            float ampPerna = Mathf.Lerp(amplitudePerna * 0.55f, amplitudePerna, intensidade) * Mathf.Clamp01(_velSuave / 0.6f);
            float ampBraco = Mathf.Lerp(amplitudeBraco * 0.40f, amplitudeBraco, intensidade) * Mathf.Clamp01(_velSuave / 0.6f);

            float sE = Mathf.Sin(_fase);
            float sD = Mathf.Sin(_fase + Mathf.PI);

            // ---- poses (suavizadas para nunca "estalar" de um quadro pro outro) ----
            float k = 1f - Mathf.Exp(-9f * dt);
            _agacharSuave  = Mathf.Lerp(_agacharSuave,  Mathf.Clamp01(Agachar), k);
            _sentarSuave   = Mathf.Lerp(_sentarSuave,   Sentado ? 1f : 0f, k);
            _consumirSuave = Mathf.Lerp(_consumirSuave, Mathf.Clamp01(Consumindo), k);
            float livre = 1f - _sentarSuave;   // sentado, a passada não vale mais

            // ---- pernas: coxa oscila, joelho dobra ----
            // O joelho só dobra quando a perna vai PARA TRÁS (na volta do passo) — dobrar na ida
            // enfiaria o pé no chão. Daí o Max(0, −seno): é meia onda, e é o que dá a leitura de corrida.
            float pernaSentada = 78f, joelhoSentado = -84f;
            float coxaE = sE * ampPerna * livre + _sentarSuave * pernaSentada;
            float coxaD = sD * ampPerna * livre + _sentarSuave * pernaSentada;
            _rig.PernaE.localRotation = Quaternion.Euler(coxaE + _agacharSuave * 52f, 0f, 0f);
            _rig.PernaD.localRotation = Quaternion.Euler(coxaD + _agacharSuave * 52f, 0f, 0f);

            float joelhoE = Mathf.Max(0f, -sE) * ampPerna * 1.7f * livre;
            float joelhoD = Mathf.Max(0f, -sD) * ampPerna * 1.7f * livre;
            _rig.CanelaE.localRotation = Quaternion.Euler(-joelhoE + _sentarSuave * joelhoSentado - _agacharSuave * 88f, 0f, 0f);
            _rig.CanelaD.localRotation = Quaternion.Euler(-joelhoD + _sentarSuave * joelhoSentado - _agacharSuave * 88f, 0f, 0f);

            // ---- braços: contrabalançam a perna oposta; o direito sobe à boca ao comer/beber ----
            float bracoBoca = -74f;
            _rig.BracoE.localRotation = Quaternion.Euler(sD * ampBraco * livre, 0f, 8f);
            _rig.BracoD.localRotation = Quaternion.Euler(
                Mathf.Lerp(sE * ampBraco * livre, bracoBoca, _consumirSuave), 0f,
                Mathf.Lerp(-8f, -26f, _consumirSuave));

            // cotovelo: dobra mais quanto mais rápido (correndo o braço fecha em ~90°)
            float cotovelo = Mathf.Lerp(12f, 62f, intensidade);
            _rig.AnteBracoE.localRotation = Quaternion.Euler(-cotovelo - Mathf.Max(0f, sD) * ampBraco * 0.5f * livre, 0f, 0f);
            _rig.AnteBracoD.localRotation = Quaternion.Euler(
                Mathf.Lerp(-cotovelo - Mathf.Max(0f, sE) * ampBraco * 0.5f * livre, -96f, _consumirSuave), 0f, 0f);

            // ---- corpo: a queda do quadril é GEOMÉTRICA, não um seno inventado ----
            // A perna é um bastão rígido: com a coxa a θ do vertical, o quadril fica L·cos(θ) acima do pé.
            // Descer o quadril por L·(1−cos θ) mantém o pé colado no chão — sem isso o personagem flutua
            // no meio da passada. Como sin(f+π) = −sin(f), o resultado já oscila no dobro da cadência.
            float theta   = Mathf.Abs(sE) * ampPerna * Mathf.Deg2Rad * livre;
            float queda   = CharacterRig.ComprimentoPerna * (1f - Mathf.Cos(theta)) * 0.65f;   // com joelho, cai menos
            float respirar = Mathf.Sin(Time.time * 1.6f) * 0.012f * (1f - intensidade);

            // agachar e sentar abaixam o corpo de verdade (a perna dobrada encurta o vão)
            float abaixar = _agacharSuave * 0.42f + _sentarSuave * 0.38f;
            _rig.Corpo.localPosition = new Vector3(0f, -queda + respirar - _aterrissagem * 0.22f - abaixar, 0f);

            // ---- tronco: inclina, rola na curva e faz a CONTRA-ROTAÇÃO do ombro ----
            // Quem anda gira o tronco no sentido contrário ao da pelve — sem isso o boneco parece
            // um manequim deslizando. É o detalhe que mais "humaniza" a caminhada.
            float inclinacao = intensidade * inclinacaoMax + _aterrissagem * 10f
                             + _agacharSuave * 22f;                 // agachado o tronco vai pra frente
            inclinacao *= (1f - _sentarSuave * 0.85f);              // sentado, tronco ereto
            float rolagem    = Mathf.Clamp(-giro * 0.03f, -rolagemMax, rolagemMax) * intensidade * livre;
            float contraGiro = -sE * 9f * intensidade * livre;      // ombro contra a perna da frente
            _rig.Tronco.localRotation = Quaternion.Euler(inclinacao, contraGiro, rolagem);

            // ---- quadril: balanço lateral + rotação própria ----
            // A pelve cai para o lado da perna que está no ar (Trendelenburg) e gira junto com o passo.
            float balanco = sE * 3.2f * intensidade * livre;
            _rig.Corpo.localRotation = Quaternion.Euler(0f, -contraGiro * 0.55f, balanco);

            // cabeça compensa parte da inclinação (o olhar tende a ficar no horizonte)
            _rig.Cabeca.localRotation = Quaternion.Euler(-inclinacao * 0.55f + _consumirSuave * 14f, 0f, 0f);
        }
    }
}
