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
            // gola: o anel de tecido que separa pescoço de camiseta
            var gola = CityPalette.Capsule(rig.Tronco, "Gola", new Vector3(0f, 0.565f, 0f), 0.20f, 0.10f, mCamisa);
            gola.transform.localScale = new Vector3(0.20f, 0.045f, 0.185f);

            // ---------------- cabeça ----------------
            rig.Cabeca = No(rig.Tronco, "Cabeca", new Vector3(0f, 0.72f, 0f));

            // crânio ovalado + mandíbula: cabeça de gente é mais alta que larga e afina embaixo
            var cranio = CityPalette.Sphere(rig.Cabeca, "Cranio", new Vector3(0f, 0.015f, 0f), 0.28f, mPele);
            cranio.transform.localScale = new Vector3(0.255f, 0.295f, 0.275f);
            var maxilar = CityPalette.Sphere(rig.Cabeca, "Maxilar", new Vector3(0f, -0.085f, 0.012f), 0.22f, mPele);
            maxilar.transform.localScale = new Vector3(0.205f, 0.175f, 0.235f);

            CityPalette.Sphere(rig.Cabeca, "OrelhaE", new Vector3(-0.128f, -0.005f, 0f), 0.068f, mPele);
            CityPalette.Sphere(rig.Cabeca, "OrelhaD", new Vector3( 0.128f, -0.005f, 0f), 0.068f, mPele);

            // ---- rosto: com a câmera no ombro, o jogador vê isso o tempo todo ----
            var mOlho     = CityPalette.Mat(new Color(0.96f, 0.96f, 0.94f), 0.55f, 0f);
            var mIris     = CityPalette.Mat(new Color(0.16f, 0.11f, 0.08f), 0.60f, 0f);
            var mBoca     = CityPalette.Mat(new Color(0.45f, 0.24f, 0.22f), 0.25f, 0f);
            for (int s = -1; s <= 1; s += 2)
            {
                var olho = CityPalette.Sphere(rig.Cabeca, "Olho", new Vector3(s * 0.062f, 0.025f, 0.118f), 0.052f, mOlho);
                olho.transform.localScale = new Vector3(0.055f, 0.042f, 0.030f);
                CityPalette.Sphere(rig.Cabeca, "Iris", new Vector3(s * 0.062f, 0.023f, 0.134f), 0.026f, mIris);
                CityPalette.Box(rig.Cabeca, "Sobrancelha", new Vector3(s * 0.063f, 0.068f, 0.124f),
                                new Vector3(0.062f, 0.014f, 0.020f), mCabelo, 0f, false);
            }
            var nariz = CityPalette.Capsule(rig.Cabeca, "Nariz", new Vector3(0f, -0.018f, 0.135f), 0.042f, 0.075f, mPele);
            nariz.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            CityPalette.Box(rig.Cabeca, "Boca", new Vector3(0f, -0.088f, 0.125f), new Vector3(0.070f, 0.014f, 0.018f), mBoca, 0f, false);

            // cabelo com volume (calota + nuca), não uma tampa quadrada
            var cabeloTopo = CityPalette.Sphere(rig.Cabeca, "Cabelo", new Vector3(0f, 0.045f, -0.012f), 0.29f, mCabelo);
            cabeloTopo.transform.localScale = new Vector3(0.268f, 0.245f, 0.285f);
            var nuca = CityPalette.Sphere(rig.Cabeca, "Nuca", new Vector3(0f, -0.030f, -0.075f), 0.22f, mCabelo);
            nuca.transform.localScale = new Vector3(0.235f, 0.190f, 0.180f);

            CityPalette.Box(rig.Cabeca, "Bone", new Vector3(0f, 0.145f, 0.005f), new Vector3(0.285f, 0.085f, 0.285f), mBone, 0f, false);
            CityPalette.Box(rig.Cabeca, "Aba",  new Vector3(0f, 0.115f, 0.185f), new Vector3(0.265f, 0.032f, 0.150f), mBone, 0f, false);

            // ---------------- braços (ombro → cotovelo → mão) ----------------
            // manga curta cobrindo o topo do braço: divide o membro e dá leitura de roupa
            rig.BracoE = Segmento(rig.Tronco, "BracoE", new Vector3(-0.27f, kOmbroY, 0f), kBraco, 0.118f, 0.098f, mPele);
            rig.BracoD = Segmento(rig.Tronco, "BracoD", new Vector3( 0.27f, kOmbroY, 0f), kBraco, 0.118f, 0.098f, mPele);
            CityPalette.Capsule(rig.BracoE, "MangaE", new Vector3(0f, -0.055f, 0f), 0.132f, 0.145f, mCamisa);
            CityPalette.Capsule(rig.BracoD, "MangaD", new Vector3(0f, -0.055f, 0f), 0.132f, 0.145f, mCamisa);

            rig.AnteBracoE = Segmento(rig.BracoE, "AnteBracoE", new Vector3(0f, -kBraco, 0f), kAnteBraco, 0.098f, 0.076f, mPele);
            rig.AnteBracoD = Segmento(rig.BracoD, "AnteBracoD", new Vector3(0f, -kBraco, 0f), kAnteBraco, 0.098f, 0.076f, mPele);

            // mão achatada com polegar, em vez de uma bola na ponta do braço
            for (int s = -1; s <= 1; s += 2)
            {
                var antebraco = s < 0 ? rig.AnteBracoE : rig.AnteBracoD;
                var palma = CityPalette.Capsule(antebraco, "Mao", new Vector3(0f, -kAnteBraco - 0.045f, 0f), 0.095f, 0.135f, mPele);
                palma.transform.localScale = new Vector3(0.092f, 0.062f, 0.048f);
                CityPalette.Capsule(antebraco, "Polegar", new Vector3(s * 0.042f, -kAnteBraco - 0.030f, 0.012f), 0.036f, 0.062f, mPele)
                           .transform.localRotation = Quaternion.Euler(0f, 0f, s * 38f);
            }

            // ---------------- pernas (quadril → joelho → pé) ----------------
            rig.PernaE = Segmento(rig.Corpo, "PernaE", new Vector3(-0.115f, kQuadrilY, 0f), kCoxa, 0.178f, 0.132f, mCalca);
            rig.PernaD = Segmento(rig.Corpo, "PernaD", new Vector3( 0.115f, kQuadrilY, 0f), kCoxa, 0.178f, 0.132f, mCalca);
            rig.CanelaE = Segmento(rig.PernaE, "CanelaE", new Vector3(0f, -kCoxa, 0f), kCanela, 0.132f, 0.098f, mCalca);
            rig.CanelaD = Segmento(rig.PernaD, "CanelaD", new Vector3(0f, -kCoxa, 0f), kCanela, 0.132f, 0.098f, mCalca);

            // barra da bermuda na altura do joelho
            CityPalette.Capsule(rig.PernaE, "BarraE", new Vector3(0f, -kCoxa + 0.03f, 0f), 0.152f, 0.10f, mCalca);
            CityPalette.Capsule(rig.PernaD, "BarraD", new Vector3(0f, -kCoxa + 0.03f, 0f), 0.152f, 0.10f, mCalca);
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
            => Segmento(pai, nome, articulacao, comprimento, espessura, espessura * 0.78f, mat);

        /// <summary>
        /// Segmento <b>afilado</b>: duas cápsulas sobrepostas, a de cima mais grossa que a de baixo.
        /// Membro humano não tem espessura constante — a coxa é bem mais grossa que o joelho, e o
        /// braço mais grosso que o punho. Com cápsula única o boneco fica com cara de tubo de PVC;
        /// com o afilamento ele ganha silhueta.
        /// </summary>
        private static Transform Segmento(Transform pai, string nome, Vector3 articulacao,
                                          float comprimento, float espessuraProximal, float espessuraDistal, Material mat)
        {
            var pivo = No(pai, nome, articulacao);

            // metade de cima (junto da articulação): mais grossa
            CityPalette.Capsule(pivo, "Malha", new Vector3(0f, -comprimento * 0.30f, 0f),
                                espessuraProximal, comprimento * 0.68f + espessuraProximal, mat);
            // metade de baixo: mais fina, e cobre até a ponta
            CityPalette.Capsule(pivo, "MalhaPonta", new Vector3(0f, -comprimento * 0.74f, 0f),
                                espessuraDistal, comprimento * 0.56f + espessuraDistal, mat);
            return pivo;
        }
    }
}
