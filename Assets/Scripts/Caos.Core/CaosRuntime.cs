namespace Caos.Core
{
    /// <summary>
    /// Ponto único de reinício do estado estático compartilhado.
    ///
    /// O jogo guarda estado em estáticos de propósito (barramento de eventos, registro de serviços,
    /// sessão) porque são coisas que atravessam assemblies sem criar dependência entre eles. O preço é
    /// que estático não morre junto com a cena: ele vive enquanto o domínio de scripts viver. No Editor
    /// com recarga de domínio isso passa despercebido, mas ele existe em dois casos que importam:
    ///
    ///  • <b>Enter Play Mode sem recarga de domínio</b> — o modo rápido de iterar, que o projeto vai
    ///    querer ligar na fase de ajuste de 60 fps;
    ///  • <b>build de celular</b> — voltar ao menu sem reiniciar o processo mantém tudo de pé.
    ///
    /// Chamado uma vez pelo Bootstrap, na primeira fase de inicialização, antes de qualquer cena.
    /// </summary>
    public static class CaosRuntime
    {
        /// <summary>Zera barramento, serviços e sessão. Idempotente.</summary>
        public static void Reiniciar()
        {
            EventBus.LimparTudo();
            ServiceLocator.Reset();
            GameSession.Reset();
        }
    }
}
