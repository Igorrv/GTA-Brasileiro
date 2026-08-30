using System;

namespace Caos.Core
{
    /// <summary>Severidade da mensagem. <see cref="CaosLog.Nivel"/> corta tudo que estiver abaixo.</summary>
    public enum NivelDeLog
    {
        Detalhe = 0,
        Info    = 1,
        Aviso   = 2,
        Erro    = 3,
        Nenhum  = 4
    }

    /// <summary>
    /// Fachada de log das camadas de regra. Existe por dois motivos concretos:
    ///
    ///  • <b>Núcleo sem engine</b> — Core, Data, World e Gameplay compilam sem UnityEngine, o que
    ///    permite rodar os testes fora do Editor. <c>UnityEngine.Debug</c> aqui dentro impediria isso.
    ///  • <b>Custo no celular</b> — <c>Debug.Log($"...")</c> monta a string mesmo quando o log está
    ///    desligado. Com <see cref="Nivel"/> em <see cref="NivelDeLog.Aviso"/> na build de loja, os
    ///    <c>Info</c> somem; onde a mensagem é cara de montar, o chamador consulta <see cref="Ativo"/>
    ///    antes de interpolar.
    ///
    /// Quem imprime de fato é o <see cref="Destino"/>, instalado pelo Bootstrap (que aponta para o
    /// Console do Unity). Sem destino instalado o log é descartado — é o que os testes usam.
    /// </summary>
    public static class CaosLog
    {
        /// <summary>Piso de severidade. Mensagens abaixo disso nem chegam ao destino.</summary>
        public static NivelDeLog Nivel { get; set; } = NivelDeLog.Info;

        /// <summary>Para onde a mensagem vai. Nulo = descarta (padrão fora do Unity).</summary>
        public static Action<NivelDeLog, string> Destino { get; set; }

        /// <summary>Consulte antes de interpolar strings caras em caminho quente.</summary>
        public static bool Ativo(NivelDeLog nivel) => nivel >= Nivel && Destino != null;

        public static void Detalhe(string mensagem) => Escrever(NivelDeLog.Detalhe, mensagem);
        public static void Info(string mensagem)    => Escrever(NivelDeLog.Info, mensagem);
        public static void Aviso(string mensagem)   => Escrever(NivelDeLog.Aviso, mensagem);
        public static void Erro(string mensagem)    => Escrever(NivelDeLog.Erro, mensagem);

        public static void Escrever(NivelDeLog nivel, string mensagem)
        {
            if (nivel < Nivel) return;
            var destino = Destino;
            if (destino == null) return;
            destino(nivel, mensagem);
        }
    }
}
