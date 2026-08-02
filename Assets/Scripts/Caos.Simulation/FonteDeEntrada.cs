using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// De onde vêm os comandos de <b>um</b> personagem.
    ///
    /// Em jogo solo existe uma única fonte, ligada ao teclado e ao touch. Em rede, cada avatar tem a
    /// sua: o avatar local lê o dispositivo, e os avatares dos outros jogadores leem um
    /// <see cref="EntradaRemota"/> preenchido pelo pacote que chegou. Os controladores
    /// (<see cref="PlayerController"/>, <see cref="VehicleController"/>, <see cref="PlayerActions"/>)
    /// passam a perguntar "qual é o comando <i>deste</i> personagem" em vez de ler o teclado global —
    /// que é o que hoje impediria dois avatares de se moverem de forma independente.
    /// </summary>
    public interface IFonteDeEntrada
    {
        Vector2 Move { get; }
        bool Run       { get; }
        bool Brake     { get; }
        bool Handbrake { get; }
        bool Crouch    { get; }

        // ações de borda: verdadeiras por um quadro só
        bool Jump      { get; }
        bool Interact  { get; }
        bool Use       { get; }
        bool Refuel    { get; }
        bool Sit       { get; }
        bool Horn      { get; }

        // câmera é sempre local (nunca vem da rede — cada um olha pra onde quiser)
        bool    CameraOrbit { get; }
        Vector2 Orbit       { get; }
    }

    /// <summary>
    /// Fonte local: teclado + controles touch, exatamente o que o <see cref="GameInput"/> já fazia.
    /// É um adaptador fino de propósito — a lógica de leitura continua num lugar só.
    /// </summary>
    public sealed class EntradaLocal : IFonteDeEntrada
    {
        public static readonly EntradaLocal Instancia = new EntradaLocal();

        public Vector2 Move        => GameInput.Move;
        public bool    Run         => GameInput.Run;
        public bool    Brake       => GameInput.Brake;
        public bool    Handbrake   => GameInput.Handbrake;
        public bool    Crouch      => GameInput.Crouch;
        public bool    Jump        => GameInput.Jump;
        public bool    Interact    => GameInput.Interact;
        public bool    Use         => GameInput.Use;
        public bool    Refuel      => GameInput.Refuel;
        public bool    Sit         => GameInput.Sit;
        public bool    Horn        => GameInput.Horn;
        public bool    CameraOrbit => GameInput.CameraOrbit;
        public Vector2 Orbit       => GameInput.Orbit;
    }

    /// <summary>
    /// Fonte remota: campos preenchidos pela camada de rede a cada pacote recebido.
    ///
    /// As ações de borda usam <see cref="Consumir"/> — o pacote diz "apertou E", e esse verdadeiro
    /// vale por <b>um</b> quadro, senão o avatar remoto entraria e sairia do carro sem parar. É o
    /// mesmo cuidado que o input local já toma com <c>GetKeyDown</c>.
    /// </summary>
    public sealed class EntradaRemota : IFonteDeEntrada
    {
        public Vector2 move;
        public bool run, brake, handbrake, crouch;
        private bool _jump, _interact, _use, _refuel, _sit, _horn;

        public Vector2 Move        => move;
        public bool    Run         => run;
        public bool    Brake       => brake;
        public bool    Handbrake   => handbrake;
        public bool    Crouch      => crouch;
        public bool    Jump        => Consumir(ref _jump);
        public bool    Interact    => Consumir(ref _interact);
        public bool    Use         => Consumir(ref _use);
        public bool    Refuel      => Consumir(ref _refuel);
        public bool    Sit         => Consumir(ref _sit);
        public bool    Horn        => Consumir(ref _horn);

        // o avatar remoto não controla a câmera de ninguém
        public bool    CameraOrbit => false;
        public Vector2 Orbit       => Vector2.zero;

        public void PressionarJump()     => _jump     = true;
        public void PressionarInteract() => _interact = true;
        public void PressionarUse()      => _use      = true;
        public void PressionarRefuel()   => _refuel   = true;
        public void PressionarSit()      => _sit      = true;
        public void PressionarHorn()     => _horn     = true;

        private static bool Consumir(ref bool flag)
        {
            bool v = flag;
            flag = false;
            return v;
        }
    }

    /// <summary>
    /// Cola o avatar à sua fonte de entrada. Todo personagem controlável carrega este componente;
    /// sem ele, os controladores caem na fonte local — que é o comportamento de jogo solo.
    /// </summary>
    public class ControleDoJogador : MonoBehaviour
    {
        /// <summary>Quem manda neste corpo. Trocar em runtime é como a rede assume um avatar.</summary>
        public IFonteDeEntrada Fonte { get; set; } = EntradaLocal.Instancia;

        /// <summary>Verdadeiro no avatar desta máquina — só ele monta câmera, HUD e áudio.</summary>
        public bool EhLocal { get; set; } = true;

        /// <summary>Id do dono na sessão em rede (0 = solo / anfitrião).</summary>
        public ulong DonoId { get; set; }

        /// <summary>Atalho seguro: devolve a fonte do avatar, ou a local se ele não tiver controle.</summary>
        public static IFonteDeEntrada De(Component c)
        {
            if (c == null) return EntradaLocal.Instancia;
            var ctrl = c.GetComponentInParent<ControleDoJogador>();
            return ctrl != null ? ctrl.Fonte : EntradaLocal.Instancia;
        }
    }
}
