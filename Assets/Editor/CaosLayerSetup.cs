#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Caos.EditorTools
{
    /// <summary>
    /// Cria as camadas de física do projeto e monta a matriz de colisão.
    ///
    /// Camadas não podem ser criadas em runtime — vivem no <c>TagManager.asset</c>. Este script grava
    /// lá e depois configura o <c>Physics.IgnoreLayerCollision</c> para cada par que não importa.
    ///
    /// A matriz que interessa:
    ///  • <b>Cidade</b> (estático) não precisa testar contra ela mesma nem contra props;
    ///  • <b>Pedestre</b> não colide com pedestre (senão viram um engarrafamento de gente) nem com prop;
    ///  • <b>Prop</b> não colide com prop;
    ///  • <b>Gatilho</b> só interessa a veículo e jogador;
    ///  • <b>Veículo × Veículo</b> continua ligado — batida entre carros é conteúdo, não desperdício.
    ///
    /// Uso: menu <b>Caos ▸ Configurar camadas de física</b> ou
    /// <c>-executeMethod Caos.EditorTools.CaosLayerSetup.Run</c>.
    /// </summary>
    public static class CaosLayerSetup
    {
        private static readonly string[] kCamadas =
        {
            "CaosCidade", "CaosProp", "CaosVeiculo", "CaosPedestre", "CaosJogador", "CaosGatilho"
        };

        [MenuItem("Caos/Configurar camadas de física")]
        public static void Run()
        {
            int criadas = Criar();
            AplicarMatriz();

            AssetDatabase.SaveAssets();
            Debug.Log($"[Camadas] {criadas} camada(s) criada(s); matriz de colisão aplicada.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Grava os nomes nas primeiras camadas de usuário livres (8..31).</summary>
        private static int Criar()
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            int criadas = 0;

            foreach (var nome in kCamadas)
            {
                if (LayerMask.NameToLayer(nome) >= 0) continue;   // já existe

                bool colocou = false;
                for (int i = 8; i < layers.arraySize; i++)        // 0..7 são reservadas da Unity
                {
                    var slot = layers.GetArrayElementAtIndex(i);
                    if (!string.IsNullOrEmpty(slot.stringValue)) continue;
                    slot.stringValue = nome;
                    colocou = true;
                    criadas++;
                    break;
                }
                if (!colocou) Debug.LogWarning($"[Camadas] Sem espaço livre para '{nome}'.");
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();
            return criadas;
        }

        private static void AplicarMatriz()
        {
            int cidade   = LayerMask.NameToLayer("CaosCidade");
            int prop     = LayerMask.NameToLayer("CaosProp");
            int veiculo  = LayerMask.NameToLayer("CaosVeiculo");
            int pedestre = LayerMask.NameToLayer("CaosPedestre");
            int jogador  = LayerMask.NameToLayer("CaosJogador");
            int gatilho  = LayerMask.NameToLayer("CaosGatilho");
            if (cidade < 0 || veiculo < 0) { Debug.LogWarning("[Camadas] Camadas ausentes; matriz não aplicada."); return; }

            // a cidade é estática: peça de cidade nunca bate em peça de cidade nem em prop
            Ignorar(cidade, cidade);
            Ignorar(cidade, prop);
            Ignorar(prop, prop);

            // gente não empurra gente, e não tromba em poste
            Ignorar(pedestre, pedestre);
            Ignorar(pedestre, prop);

            // gatilho (buraco, zona) só interessa a quem pode ativá-lo
            Ignorar(gatilho, cidade);
            Ignorar(gatilho, prop);
            Ignorar(gatilho, pedestre);
            Ignorar(gatilho, gatilho);

            // o jogador a pé não precisa colidir com gatilho de buraco (é problema do carro)
            Ignorar(jogador, gatilho);

            Debug.Log("[Camadas] Matriz: cidade×cidade, cidade×prop, prop×prop, pedestre×pedestre, " +
                      "pedestre×prop e os pares de gatilho foram desligados. Veículo×veículo segue ligado.");
        }

        private static void Ignorar(int a, int b)
        {
            if (a < 0 || b < 0) return;
            Physics.IgnoreLayerCollision(a, b, true);
        }
    }
}
#endif
