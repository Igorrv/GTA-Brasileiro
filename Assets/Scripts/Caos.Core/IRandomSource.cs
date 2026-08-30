using System;

namespace Caos.Core
{
    /// <summary>
    /// Fonte de números aleatórios das regras de jogo. Além de tirar UnityEngine do Gameplay, resolve
    /// um problema real de determinismo: <c>UnityEngine.Random</c> é <b>um único fluxo global</b>, o
    /// mesmo que a geração da cidade precisa semear e restaurar com cuidado. Enquanto os sistemas de
    /// regra sorteiam desse fluxo, qualquer mudança de ordem em um deles desloca a sequência do outro.
    ///
    /// Cada sistema recebendo o seu próprio fluxo (ver <see cref="CaosRandom"/>) torna um sorteio
    /// independente do outro e reproduzível a partir da semente.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Valor em [0,1), equivalente a <c>Random.value</c> para efeito de probabilidade.</summary>
        float Valor01();

        /// <summary>Inteiro em [minimo, maximoExclusivo), como <c>Random.Range(int, int)</c>.</summary>
        int Intervalo(int minimo, int maximoExclusivo);

        /// <summary>Float em [minimo, maximo).</summary>
        float Intervalo(float minimo, float maximo);
    }

    /// <summary>
    /// Fluxo determinístico baseado em <see cref="System.Random"/>. Mesma semente, mesma sequência,
    /// em qualquer máquina e independente do que a cidade ou o tráfego estejam sorteando.
    /// </summary>
    public sealed class CaosRandom : IRandomSource
    {
        private readonly Random _rng;

        public CaosRandom(int semente) => _rng = new Random(semente);

        public float Valor01() => (float)_rng.NextDouble();

        public int Intervalo(int minimo, int maximoExclusivo)
            => maximoExclusivo <= minimo ? minimo : _rng.Next(minimo, maximoExclusivo);

        public float Intervalo(float minimo, float maximo)
            => minimo + (float)_rng.NextDouble() * (maximo - minimo);
    }
}
