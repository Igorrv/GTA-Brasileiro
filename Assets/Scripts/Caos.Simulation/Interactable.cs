using System.Collections.Generic;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Tipos de estabelecimento de rua. Os quatro primeiros existem desde a S4; do <see cref="Padaria"/>
    /// em diante são o comércio brasileiro (docs/05) — vendem <c>ItemDto</c> do catálogo.
    /// </summary>
    public enum TipoInteracao
    {
        Posto, Oficina, Trabalho,
        Padaria, Boteco, Mercadinho, Loterica, Farmacia, Barraca
    }

    /// <summary>
    /// Zona de interação (docs/13 S4/S8): posto (abastecer), oficina (reparar), trabalho (ganhar R$) e
    /// comércio (comprar item do catálogo). É só um marcador de dados; a detecção de proximidade fica no
    /// <see cref="InteractionScanner"/> (distância, sem trigger de física → mais barato e sem colisão espúria).
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [Header("Identidade")]
        public TipoInteracao tipo;
        public string rotulo = "Posto";
        public string bordao = "";
        public Color  cor    = Color.white;

        [Header("Raio de uso (m)")]
        public float radius = 5.5f;

        [Header("Posto / Oficina")]
        [Tooltip("Posto: R$/litro base (corrigido por IPC-Caos). Oficina: R$ base do reparo.")]
        public float precoBase = 6.5f;

        [Header("Trabalho")]
        public float pagamento     = 80f;   // R$ por turno
        public float energiaCost   = 12f;   // cansa
        public float fomeCost      = 8f;    // dá fome
        public float horasTrabalho = 2f;    // avança o relógio do jogo

        [Header("Comércio")]
        [Tooltip("Ids de ItemDto vendidos aqui. O jogador compra o primeiro que ainda faz efeito nele.")]
        public List<string> itens = new List<string>();

        /// <summary>Comércio = vende item (o [F] vira 'comprar').</summary>
        public bool IsComercio =>
            tipo == TipoInteracao.Padaria || tipo == TipoInteracao.Boteco || tipo == TipoInteracao.Mercadinho ||
            tipo == TipoInteracao.Loterica || tipo == TipoInteracao.Farmacia || tipo == TipoInteracao.Barraca;
    }
}
