namespace Caos.Core
{
    /// <summary>
    /// Sistemas que precisam ser atualizados a cada frame (tempo, clima, atributos, economia, eventos).
    /// O GameManager mantém a lista e chama Tick(dt) na ordem de registro.
    /// </summary>
    public interface ITickable
    {
        /// <summary>dt em SEGUNDOS de tempo real.</summary>
        void Tick(float dt);

        /// <summary>Ordem relativa de execução (menor executa antes).</summary>
        int Order { get; }
    }
}
