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
        public static Vector2 VirtualMove;
        public static bool   VirtualRun, VirtualBrake, VirtualOrbitActive;
        public static Vector2 VirtualOrbit;
        private static bool _qInteract, _qUse, _qRefuel, _qRadio, _qPause;

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
        public static bool   Jump        => Input.GetKeyDown(KeyCode.Space) || VirtualJump;

        /// <summary>Ctrl esquerdo / C — freio de mão (trava a traseira e derrapa).</summary>
        public static bool   Handbrake   => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C) || VirtualHandbrake;
        public static bool   CameraOrbit => Input.GetMouseButton(1) || VirtualOrbitActive;
        public static Vector2 Orbit      => VirtualOrbitActive ? VirtualOrbit : new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

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
