using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Camada única de leitura de input. Mescla <b>teclado</b> (Input Manager legado, funciona em qualquer
    /// "Active Input Handling") com <b>estado virtual</b> preenchido pelos <see cref="TouchControls"/>
    /// (joystick + botões) — assim o mesmo código roda em PC e mobile. Os leitores (controladores,
    /// scanner) não distinguem a origem.
    ///
    /// Eixos:
    ///   Move.x = direção lateral/esterço  (A/D, ←/→, joystick X)
    ///   Move.y = frente/trás/acelerador   (W/S, ↑/↓, joystick Y)
    /// </summary>
    public static class GameInput
    {
        // ---- estado virtual (TouchControls escreve) ----
        private const float ZonaMortaMovimento = 0.14f;
        private static Vector2 _virtualMove;

        /// <summary>
        /// Eixo bruto do joystick convertido para uma resposta radial com zona morta. A faixa restante
        /// é remapeada até 1, então eliminar tremor no centro não sacrifica esterço ou corrida máximos.
        /// </summary>
        public static Vector2 VirtualMove
        {
            get => _virtualMove;
            set => _virtualMove = AplicarZonaMortaRadial(value, ZonaMortaMovimento);
        }

        public static bool   VirtualRun, VirtualBrake, VirtualOrbitActive, VirtualLookBehind;
        private static Vector2 _virtualOrbit;
        /// <summary>Compatibilidade para produtores antigos; prefira <see cref="AcumularOrbitaVirtual"/>.</summary>
        public static Vector2 VirtualOrbit
        {
            get => _virtualOrbit;
            set => _virtualOrbit = value;
        }
        private static bool _qJump, _qInteract, _qUse, _qRefuel, _qRadio, _qPause;

        public static void QueueJump()       { _qJump     = true; }
        public static void CancelarPuloVirtual()
        {
            _qJump = false;
            VirtualJump = false;
        }
        public static void QueueInteract()   { _qInteract = true; }
        public static void QueueUse()        { _qUse      = true; }
        public static void QueueRefuel()     { _qRefuel   = true; }
        public static void QueueRadioNext()  { _qRadio    = true; }
        public static void QueuePause()      { _qPause    = true; }

        // ---- teclado ----
        private static Vector2 KeyboardMove
        {
            get
            {
                float x = 0f, y = 0f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    y += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  y -= 1f;
                return new Vector2(x, y);
            }
        }
        private static bool KeyboardRun   => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        private static bool KeyboardBrake => Input.GetKey(KeyCode.Space);

        // ---- estado virtual extra (freio de mão, pulo, agachar) ----
        public static bool VirtualHandbrake, VirtualJump, VirtualCrouch;
        private static bool _qSit, _qPhone, _qHorn;

        public static void QueueSit()   { _qSit   = true; }
        public static void QueuePhone() { _qPhone = true; }
        public static void QueueHorn()  { _qHorn  = true; }

        // ---- getters unificados ----
        public static Vector2 Move
        {
            get
            {
                Vector2 k = KeyboardMove;
                // prevalece o de maior magnitude (teclado ou joystick)
                return k.sqrMagnitude >= VirtualMove.sqrMagnitude ? k : VirtualMove;
            }
        }
        public static bool   Run         => KeyboardRun || VirtualRun;
        public static bool   Brake       => KeyboardBrake || VirtualBrake;

        /// <summary>Espaço — pular (a pé). No veículo a mesma tecla é o freio; os dois controladores
        /// nunca estão ativos ao mesmo tempo, então não há conflito.</summary>
        public static bool Jump
        {
            get
            {
                bool t = _qJump || VirtualJump;
                _qJump = false;
                VirtualJump = false;
                return Input.GetKeyDown(KeyCode.Space) || t;
            }
        }

        /// <summary>Ctrl esquerdo / C — freio de mão (trava a traseira e derrapa).</summary>
        public static bool   Handbrake   => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C) || VirtualHandbrake;
        public static bool   CameraOrbit => Input.GetMouseButton(1) || VirtualOrbitActive;
        public static Vector2 Orbit      => VirtualOrbitActive ? ConsumirOrbitaVirtual() : new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        /// <summary>V/botão central do mouse ou botão touch — segura a câmera olhando para trás.</summary>
        public static bool   LookBehind  => Input.GetKey(KeyCode.V) || Input.GetMouseButton(2) || VirtualLookBehind;

        /// <summary>
        /// Soma deltas que podem chegar mais de uma vez no mesmo frame. A câmera consome o total uma
        /// única vez, impedindo que o último delta continue girando quando o dedo para sobre a tela.
        /// </summary>
        public static void AcumularOrbitaVirtual(Vector2 delta)
            => _virtualOrbit = Vector2.ClampMagnitude(_virtualOrbit + delta, 18f);
        public static void LimparOrbitaVirtual() => _virtualOrbit = Vector2.zero;

        private static Vector2 ConsumirOrbitaVirtual()
        {
            Vector2 delta = _virtualOrbit;
            _virtualOrbit = Vector2.zero;
            return delta;
        }

        /// <summary>
        /// Remove ruído no centro de qualquer stick sem criar um degrau na saída da zona morta.
        /// Público para que controles alternativos (gamepad/gyro) usem exatamente a mesma curva.
        /// </summary>
        public static Vector2 AplicarZonaMortaRadial(Vector2 valor, float zonaMorta)
        {
            float magnitude = Mathf.Min(1f, valor.magnitude);
            zonaMorta = Mathf.Clamp(zonaMorta, 0f, 0.95f);
            if (magnitude <= zonaMorta || magnitude <= Mathf.Epsilon) return Vector2.zero;

            float normalizada = (magnitude - zonaMorta) / (1f - zonaMorta);
            normalizada = Mathf.Pow(normalizada, 1.08f);
            return valor / valor.magnitude * normalizada;
        }

        /// <summary>Evita comandos presos após perder foco, trocar cena ou desmontar o HUD touch.</summary>
        public static void ResetVirtualControls()
        {
            _virtualMove       = Vector2.zero;
            _virtualOrbit      = Vector2.zero;
            VirtualRun         = false;
            VirtualBrake       = false;
            VirtualHandbrake   = false;
            VirtualJump        = false;
            VirtualCrouch      = false;
            VirtualOrbitActive = false;
            VirtualLookBehind  = false;
            _qJump = _qInteract = _qUse = _qRefuel = _qRadio = _qPause = false;
            _qSit = _qPhone = _qHorn = false;
        }

        // ---- one-shot (borda): consome o flag virtual no primeiro acesso do frame ----
        public static bool Interact
        {
            get { bool t = _qInteract; _qInteract = false; return Input.GetKeyDown(KeyCode.E) || t; }
        }
        public static bool Use
        {
            get { bool t = _qUse; _qUse = false; return Input.GetKeyDown(KeyCode.F) || t; }
        }
        public static bool Refuel
        {
            get { bool t = _qRefuel; _qRefuel = false; return Input.GetKeyDown(KeyCode.R) || t; }
        }

        /// <summary>Q — próxima estação de rádio.</summary>
        public static bool RadioNext
        {
            get { bool t = _qRadio; _qRadio = false; return Input.GetKeyDown(KeyCode.Q) || t; }
        }
        /// <summary>Z — liga/desliga o rádio.</summary>
        public static bool RadioToggle => Input.GetKeyDown(KeyCode.Z);

        /// <summary>Tab/Esc — pausa.</summary>
        public static bool Pause
        {
            get { bool t = _qPause; _qPause = false; return Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape) || t; }
        }

        /// <summary>M — abre/fecha o mapa grande.</summary>
        public static bool MapToggle => Input.GetKeyDown(KeyCode.M);

        /// <summary>Ctrl / C — agachar (a pé). A mesma tecla é freio de mão no carro; os controladores
        /// nunca estão ativos ao mesmo tempo.</summary>
        public static bool Crouch => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C) || VirtualCrouch;

        /// <summary>G — sentar / levantar do banco.</summary>
        public static bool Sit
        {
            get { bool t = _qSit; _qSit = false; return Input.GetKeyDown(KeyCode.G) || t; }
        }

        /// <summary>
        /// Shift esquerdo / seta cima+Ctrl — subir marcha à mão. Enquanto o jogador não tocar em
        /// nenhuma das duas, o câmbio segue automático; ao tocar, passa a manual e o motor pode bater
        /// no corte se ele esquecer de trocar. É o mesmo esquema de jogo de corrida arcade.
        /// </summary>
        public static bool MarchaAcima  => Input.GetKeyDown(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftAlt)
                                        || Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus);
        public static bool MarchaAbaixo => Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus);

        /// <summary>H — buzina (no Brasil, item de série).</summary>
        public static bool Horn
        {
            get { bool t = _qHorn; _qHorn = false; return Input.GetKeyDown(KeyCode.H) || t; }
        }

        /// <summary>P — abre/fecha o celular.</summary>
        public static bool Phone
        {
            get { bool t = _qPhone; _qPhone = false; return Input.GetKeyDown(KeyCode.P) || t; }
        }
    }
}
