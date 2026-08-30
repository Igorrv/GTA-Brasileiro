using System;

namespace Caos.Core
{
    /// <summary>
    /// As poucas funções de <c>UnityEngine.Mathf</c> que as camadas de regra usavam, reescritas para
    /// que Core, World e Gameplay compilem sem a engine. As fórmulas são as mesmas do Mathf — inclusive
    /// a tolerância de <see cref="Aproximadamente"/> — para que nenhum número mude de valor.
    /// </summary>
    public static class CaosMath
    {
        public static float Limitar(float valor, float minimo, float maximo)
        {
            if (valor < minimo) return minimo;
            if (valor > maximo) return maximo;
            return valor;
        }

        public static int Limitar(int valor, int minimo, int maximo)
        {
            if (valor < minimo) return minimo;
            if (valor > maximo) return maximo;
            return valor;
        }

        public static float Limitar01(float valor)
        {
            if (valor < 0f) return 0f;
            if (valor > 1f) return 1f;
            return valor;
        }

        public static float Potencia(float baseValor, float expoente) => (float)Math.Pow(baseValor, expoente);

        /// <summary>Mesma tolerância relativa do <c>Mathf.Approximately</c>.</summary>
        public static bool Aproximadamente(float a, float b)
        {
            float escala = 1E-06f * Math.Max(Math.Abs(a), Math.Abs(b));
            float piso   = float.Epsilon * 8f;
            return Math.Abs(b - a) < Math.Max(escala, piso);
        }
    }
}
