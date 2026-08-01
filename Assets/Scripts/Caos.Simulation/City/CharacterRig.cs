using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Esqueleto do personagem montado com primitivas, agora <b>em dois segmentos por membro</b>:
    /// braço → antebraço → mão, e coxa → canela → pé. Cada segmento tem o <b>pivô na articulação</b>
    /// (e não no centro da malha), que é o que permite o cotovelo e o joelho dobrarem de verdade na
    /// passada — com membro rígido, correr parece andar de perna de pau.
    ///
    /// Também ganhou pescoço, ombros arredondados, cabelo e orelhas: de longe é silhueta, de perto
    /// tem leitura. Serve para o protagonista e para os pedestres.
    ///
    /// Medidas em metros, origem na cintura (pés em −0,95 · topo da cabeça em +0,95 = 1,90 m).
    /// </summary>
    public sealed class CharacterRig : MonoBehaviour
    {
        public Transform Corpo    { get; private set; }   // nó que sobe/desce (bob) e inclina
        public Transform Tronco   { get; private set; }
        public Transform Cabeca   { get; private set; }
        public Transform BracoE   { get; private set; }
        public Transform BracoD   { get; private set; }
        public Transform AnteBracoE { get; private set; }
        public Transform AnteBracoD { get; private set; }
        public Transform PernaE   { get; private set; }
        public Transform PernaD   { get; private set; }
        public Transform CanelaE  { get; private set; }
        public Transform CanelaD  { get; private set; }

        /// <summary>Comprimento total da perna — o animador usa para a queda do quadril na passada.</summary>
        public const float ComprimentoPerna = 0.90f;

        private const float kCoxa      = 0.48f;
        private const float kCanela    = ComprimentoPerna - kCoxa;   // 0,42
        private const float kBraco     = 0.32f;
        private const float kAnteBraco = 0.28f;
        private const float kQuadrilY  = -0.05f;
        private const float kOmbroY    =  0.42f;

        public static CharacterRig Construir(Transform pai, Color camisa, Color calca, Color pele, Color bone)
        {
            var raiz = new GameObject("Rig");
            raiz.transform.SetParent(pai, false);
            var rig = raiz.AddComponent<CharacterRig>();

            // texturas: pele com poro, camiseta em trama, calça em sarja
            var mCamisa = CityPalette.MatTex(Superficie.Tecido, camisa, 0.5f, 0.6f, 0.10f, 0f);
            var mCalca  = CityPalette.MatTex(Superficie.Jeans,  calca,  0.5f, 0.9f, 0.08f, 0f);
            var mPele   = CityPalette.MatTex(Superficie.Pele,   pele,   0.4f, 0.6f, 0.16f, 0f);
            var mBone   = CityPalette.MatTex(Superficie.Tecido, bone,   0.4f, 0.3f, 0.10f, 0f);
            var mCabelo = CityPalette.Mat(new Color(0.10f, 0.08f, 0.07f), 0.30f, 0f);
            var mSapato = CityPalette.Mat(new Color(0.13f, 0.13f, 0.14f), 0.35f, 0f);

            rig.Corpo = No(raiz.transform, "Corpo", Vector3.zero);

            // ---------------- tronco (cápsulas: sem quina, forma de gente) ----------------
            rig.Tronco = No(rig.Corpo, "Tronco", new Vector3(0f, kQuadrilY, 0f));
            var quadril = CityPalette.Capsule(rig.Tronco, "Quadril", new Vector3(0f, 0.08f, 0f), 0.38f, 0.30f, mCalca);
            quadril.transform.localScale = new Vector3(0.38f, 0.13f, 0.26f);
            var abdomen = CityPalette.Capsule(rig.Tronco, "Abdomen", new Vector3(0f, 0.26f, 0f), 0.36f, 0.34f, mCamisa);
            abdomen.transform.localScale = new Vector3(0.36f, 0.16f, 0.24f);
            var peito = CityPalette.Capsule(rig.Tronco, "Peito", new Vector3(0f, 0.44f, 0f), 0.44f, 0.34f, mCamisa);
            peito.transform.localScale = new Vector3(0.44f, 0.17f, 0.27f);

            CityPalette.Sphere(rig.Tronco, "OmbroE", new Vector3(-0.23f, kOmbroY, 0f), 0.20f, mCamisa);
            CityPalette.Sphere(rig.Tronco, "OmbroD", new Vector3( 0.23f, kOmbroY, 0f), 0.20f, mCamisa);
            CityPalette.Capsule(rig.Tronco, "Pescoco", new Vector3(0f, 0.60f, 0f), 0.14f, 0.18f, mPele);

            // ---------------- cabeça ----------------
            rig.Cabeca = No(rig.Tronco, "Cabeca", new Vector3(0f, 0.72f, 0f));
            var cranio = CityPalette.Sphere(rig.Cabeca, "Cranio", Vector3.zero, 0.28f, mPele);
            cranio.transform.localScale = new Vector3(0.26f, 0.30f, 0.28f);   // cabeça não é bola
            CityPalette.Sphere(rig.Cabeca, "OrelhaE", new Vector3(-0.135f, 0.0f, 0f), 0.07f, mPele);
            CityPalette.Sphere(rig.Cabeca, "OrelhaD", new Vector3( 0.135f, 0.0f, 0f), 0.07f, mPele);
            CityPalette.Box(rig.Cabeca, "Cabelo", new Vector3(0f, 0.10f, -0.01f), new Vector3(0.27f, 0.10f, 0.29f), mCabelo, 0f, false);
            CityPalette.Box(rig.Cabeca, "Bone",   new Vector3(0f, 0.15f, 0.01f), new Vector3(0.29f, 0.09f, 0.29f), mBone, 0f, false);
            CityPalette.Box(rig.Cabeca, "Aba",    new Vector3(0f, 0.12f, 0.19f), new Vector3(0.27f, 0.035f, 0.15f), mBone, 0f, false);

            // ---------------- braços (ombro → cotovelo → mão) ----------------
            rig.BracoE = Segmento(rig.Tronco, "BracoE", new Vector3(-0.27f, kOmbroY, 0f), kBraco, 0.115f, mPele);
            rig.BracoD = Segmento(rig.Tronco, "BracoD", new Vector3( 0.27f, kOmbroY, 0f), kBraco, 0.115f, mPele);
            rig.AnteBracoE = Segmento(rig.BracoE, "AnteBracoE", new Vector3(0f, -kBraco, 0f), kAnteBraco, 0.10f, mPele);
            rig.AnteBracoD = Segmento(rig.BracoD, "AnteBracoD", new Vector3(0f, -kBraco, 0f), kAnteBraco, 0.10f, mPele);
            CityPalette.Sphere(rig.AnteBracoE, "MaoE", new Vector3(0f, -kAnteBraco - 0.04f, 0f), 0.11f, mPele);
            CityPalette.Sphere(rig.AnteBracoD, "MaoD", new Vector3(0f, -kAnteBraco - 0.04f, 0f), 0.11f, mPele);

            // ---------------- pernas (quadril → joelho → pé) ----------------
            rig.PernaE = Segmento(rig.Corpo, "PernaE", new Vector3(-0.115f, kQuadrilY, 0f), kCoxa, 0.17f, mCalca);
            rig.PernaD = Segmento(rig.Corpo, "PernaD", new Vector3( 0.115f, kQuadrilY, 0f), kCoxa, 0.17f, mCalca);
            rig.CanelaE = Segmento(rig.PernaE, "CanelaE", new Vector3(0f, -kCoxa, 0f), kCanela, 0.14f, mCalca);
            rig.CanelaD = Segmento(rig.PernaD, "CanelaD", new Vector3(0f, -kCoxa, 0f), kCanela, 0.14f, mCalca);
            // pé: cápsula deitada (bico arredondado) em vez de tijolinho
            var peE = CityPalette.Capsule(rig.CanelaE, "PeE", new Vector3(0f, -kCanela - 0.02f, 0.05f), 0.16f, 0.28f, mSapato);
            peE.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            peE.transform.localScale = new Vector3(0.16f, 0.13f, 0.12f);
            var peD = CityPalette.Capsule(rig.CanelaD, "PeD", new Vector3(0f, -kCanela - 0.02f, 0.05f), 0.16f, 0.28f, mSapato);
            peD.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            peD.transform.localScale = new Vector3(0.16f, 0.13f, 0.12f);

            return rig;
        }

        private static Transform No(Transform pai, string nome, Vector3 pos)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.transform.localPosition = pos;
            return go.transform;
        }

        /// <summary>
        /// Segmento com pivô na articulação: a malha desce metade do comprimento e a junta ganha uma
        /// esfera, que preenche o vão quando o membro dobra.
        /// </summary>
        private static Transform Segmento(Transform pai, string nome, Vector3 articulacao, float comprimento, float espessura, Material mat)
        {
            var pivo = No(pai, nome, articulacao);
            // cápsula: as pontas já são esféricas, então a junta fecha sem quina ao dobrar
            CityPalette.Capsule(pivo, "Malha", new Vector3(0f, -comprimento * 0.5f, 0f),
                                espessura, comprimento + espessura, mat);
            return pivo;
        }
    }
}
