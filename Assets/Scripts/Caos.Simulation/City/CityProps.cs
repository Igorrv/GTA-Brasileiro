using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Mobiliário urbano brasileiro montado com primitivas (docs/09 — "primitivo estilizado", sem assets).
    /// São essas peças que fazem a cidade parecer o Brasil e não uma maquete genérica: poste de concreto
    /// com braço e fiação farta, orelhão, ponto de ônibus, banca de jornal, barraca de camelô, caixa d'água
    /// na laje, quebra-molas, muro com pichação e trave de várzea.
    ///
    /// Nada aqui tem Collider (exceto onde o carro precisa bater), e tudo usa <see cref="CityPalette"/>.
    /// </summary>
    public static class CityProps
    {
        /// <summary>
        /// Posições de assento produzidas pelos props (banco do ponto de ônibus). O
        /// <see cref="CityGenerator"/> drena esta lista e registra em <see cref="PlayerActions.Assentos"/>
        /// — os props não conhecem o jogador, então só deixam o recado.
        /// </summary>
        public static readonly System.Collections.Generic.List<Vector3> AssentosPendentes = new System.Collections.Generic.List<Vector3>();

        // ------------------------------------------------------------ poste + fiação
        /// <summary>Devolve o renderer da luminária — o ciclo dia/noite acende os postes ao anoitecer.</summary>
        public static MeshRenderer PosteDeLuz(Transform parent, Vector3 pos, float yaw, bool aceso)
        {
            var go = new GameObject("Poste");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CityPalette.Cyl(go.transform, "Mastro", new Vector3(0f, 4.5f, 0f), 0.28f, 9f, CityPalette.Poste, collide: true);
            CityPalette.Box(go.transform, "Braco", new Vector3(0.9f, 8.8f, 0f), new Vector3(1.9f, 0.14f, 0.14f), CityPalette.Poste, collide: false);
            var luminaria = CityPalette.Box(go.transform, "Luminaria", new Vector3(1.8f, 8.65f, 0f), new Vector3(0.75f, 0.22f, 0.36f),
                            aceso ? CityPalette.LuzAcesa : CityPalette.MetalEscuro, collide: false);

            // transformador + gambiarra de fios (o visual mais brasileiro que existe)
            if (Random.value < 0.35f)
                CityPalette.Cyl(go.transform, "Trafo", new Vector3(0f, 7.4f, 0.32f), 0.55f, 0.9f, CityPalette.MetalEscuro);
            int fios = Random.Range(3, 7);
            for (int i = 0; i < fios; i++)
                CityPalette.Box(go.transform, "Fio", new Vector3(0f, 7.9f + i * 0.16f, 0f),
                                new Vector3(0.04f, 0.04f, CityLayout.Cell), CityPalette.Pichacao, collide: false);

            return luminaria.GetComponent<MeshRenderer>();
        }

        // ------------------------------------------------------------ semáforo
        public static GameObject Semaforo(Transform parent, Vector3 pos, float yaw)
        {
            var go = new GameObject("Semaforo");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CityPalette.Cyl(go.transform, "Haste", new Vector3(0f, 1.7f, 0f), 0.18f, 3.4f, CityPalette.MetalEscuro, collide: true);
            CityPalette.Box(go.transform, "Caixa", new Vector3(0f, 3.6f, 0f), new Vector3(0.42f, 1.1f, 0.34f), CityPalette.MetalEscuro, collide: false);
            return go;
        }

        /// <summary>Lâmpada do semáforo — o <see cref="TrafficSystem"/> troca a cor conforme a fase.</summary>
        public static MeshRenderer LuzSemaforo(Transform semaforo)
        {
            var luz = CityPalette.Sphere(semaforo, "Luz", new Vector3(0f, 3.6f, -0.22f), 0.30f, CityPalette.Mat(Color.red));
            return luz.GetComponent<MeshRenderer>();
        }

        // ------------------------------------------------------------ ponto de ônibus
        public static void PontoDeOnibus(Transform parent, Vector3 pos, float yaw)
        {
            var go = new GameObject("PontoDeOnibus");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            // (o banco abaixo vira assento; ver AssentosPendentes)

            CityPalette.Box(go.transform, "Cobertura", new Vector3(0f, 2.5f, 0f), new Vector3(4.2f, 0.12f, 1.6f), CityPalette.Metal, collide: false);
            CityPalette.Box(go.transform, "Fundo",     new Vector3(0f, 1.3f, 0.75f), new Vector3(4.2f, 2.4f, 0.1f), CityPalette.Vidro, collide: false);
            var banco = CityPalette.Box(go.transform, "Banco", new Vector3(0f, 0.5f, 0.35f), new Vector3(3.6f, 0.12f, 0.45f), CityPalette.Madeira, collide: false);
            // banco do ponto também é assento: quem espera o busão senta
            AssentosPendentes.Add(banco.transform.position);
            CityPalette.Cyl(go.transform, "PeE", new Vector3(-2.0f, 1.25f, 0.7f), 0.1f, 2.5f, CityPalette.Metal);
            CityPalette.Cyl(go.transform, "PeD", new Vector3( 2.0f, 1.25f, 0.7f), 0.1f, 2.5f, CityPalette.Metal);
            CityPalette.Label(go.transform, "PONTO 402", new Vector3(0f, 2.9f, 0f), new Color(0.95f, 0.9f, 0.6f), 0.22f);
        }

        // ------------------------------------------------------------ orelhão
        public static void Orelhao(Transform parent, Vector3 pos, float yaw)
        {
            var go = new GameObject("Orelhao");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CityPalette.Cyl(go.transform, "Poste", new Vector3(0f, 0.9f, 0f), 0.12f, 1.8f, CityPalette.Metal);
            var casco = CityPalette.Sphere(go.transform, "Casco", new Vector3(0f, 1.9f, 0f), 1.0f, CityPalette.Mat(new Color(0.85f, 0.75f, 0.15f)));
            casco.transform.localScale = new Vector3(1.0f, 0.85f, 0.9f);
            CityPalette.Box(go.transform, "Aparelho", new Vector3(0f, 1.75f, -0.28f), new Vector3(0.3f, 0.45f, 0.16f), CityPalette.MetalEscuro, collide: false);
        }

        // ------------------------------------------------------------ banca de jornal / camelô
        public static void BancaDeJornal(Transform parent, Vector3 pos, float yaw)
        {
            var go = new GameObject("BancaDeJornal");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CityPalette.Box(go.transform, "Corpo",   new Vector3(0f, 1.2f, 0f), new Vector3(3.4f, 2.4f, 2.0f), CityPalette.Mat(new Color(0.20f, 0.45f, 0.30f)));
            CityPalette.Box(go.transform, "Toldo",   new Vector3(0f, 2.6f, -1.3f), new Vector3(3.8f, 0.1f, 1.2f), CityPalette.Mat(new Color(0.80f, 0.25f, 0.25f)), collide: false);
            CityPalette.Box(go.transform, "Balcao",  new Vector3(0f, 1.0f, -1.1f), new Vector3(3.2f, 0.1f, 0.6f), CityPalette.Madeira, collide: false);
            CityPalette.Label(go.transform, "BANCA", new Vector3(0f, 2.75f, -1.35f), Color.white, 0.24f);
        }

        public static void BarracaCamelo(Transform parent, Vector3 pos, float yaw, Color toldo)
        {
            var go = new GameObject("BarracaCamelo");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CityPalette.Box(go.transform, "Toldo", new Vector3(0f, 2.3f, 0f), new Vector3(3.0f, 0.1f, 2.4f), CityPalette.Mat(toldo), collide: false);
            CityPalette.Box(go.transform, "Mesa",  new Vector3(0f, 0.85f, 0f), new Vector3(2.8f, 0.1f, 1.4f), CityPalette.Madeira);
            for (int i = 0; i < 4; i++)
            {
                float sx = (i % 2 == 0) ? -1.4f : 1.4f;
                float sz = (i < 2) ? -1.1f : 1.1f;
                CityPalette.Cyl(go.transform, "Pe", new Vector3(sx, 1.15f, sz), 0.07f, 2.3f, CityPalette.Metal);
            }
            // mercadoria empilhada
            for (int i = 0; i < 4; i++)
                CityPalette.Box(go.transform, "Caixa", new Vector3(Random.Range(-1.1f, 1.1f), 1.0f, Random.Range(-0.5f, 0.5f)),
                                new Vector3(0.5f, 0.22f, 0.35f), CityPalette.MatViva(), collide: false);
        }

        // ------------------------------------------------------------ lixeira, árvore, jardineira
        public static void Lixeira(Transform parent, Vector3 pos)
        {
            CityPalette.Cyl(parent, "Lixeira", pos + new Vector3(0f, 0.55f, 0f), 0.6f, 1.1f, CityPalette.Mat(new Color(0.20f, 0.35f, 0.25f)), collide: true);
        }

        public static void Arvore(Transform parent, Vector3 pos, float escala = 1f)
        {
            var go = new GameObject("Arvore");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            float h = Random.Range(3.5f, 6.5f) * escala;
            CityPalette.Cyl(go.transform, "Tronco", new Vector3(0f, h * 0.4f, 0f), 0.35f, h * 0.8f, CityPalette.Tronco, collide: true);
            var copa = CityPalette.Sphere(go.transform, "Copa", new Vector3(0f, h * 0.95f, 0f), h * 0.75f, CityPalette.Folhagem);
            copa.transform.localScale = new Vector3(h * 0.8f, h * 0.6f, h * 0.8f);
        }

        public static void Coqueiro(Transform parent, Vector3 pos)
        {
            var go = new GameObject("Coqueiro");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(Random.Range(-6f, 6f), Random.Range(0f, 360f), Random.Range(-6f, 6f));

            float h = Random.Range(7f, 11f);
            CityPalette.Cyl(go.transform, "Tronco", new Vector3(0f, h * 0.5f, 0f), 0.4f, h, CityPalette.Mat(new Color(0.55f, 0.45f, 0.30f)), collide: true);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                var folha = CityPalette.Box(go.transform, "Folha", Vector3.zero, new Vector3(3.4f, 0.12f, 0.6f), CityPalette.Folhagem, collide: false);
                folha.transform.localPosition = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 1.5f, h - 0.2f, Mathf.Sin(a * Mathf.Deg2Rad) * 1.5f);
                folha.transform.localRotation = Quaternion.Euler(0f, -a, 14f);
            }
        }

        // ------------------------------------------------------------ muro, portão, cerca
        public static void Muro(Transform parent, Vector3 pos, float comprimento, float yaw, float altura = 2.4f, bool pichado = true)
        {
            var go = new GameObject("Muro");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // muro pichado usa a textura de lambe-lambe (cartaz + tinta por cima); muro limpo, chapisco
            var superficie = pichado && Random.value < 0.65f ? Superficie.Lambe : Superficie.Chapisco;
            CityPalette.Box(go.transform, "Parede", new Vector3(0f, altura * 0.5f, 0f), new Vector3(comprimento, altura, 0.25f),
                            CityPalette.MatTex(superficie, new Color(0.90f, 0.89f, 0.86f), comprimento, altura));

            // rodapé de umidade também no muro
            CityPalette.Box(go.transform, "RodapeMuro", new Vector3(0f, altura * 0.35f, 0f),
                            new Vector3(comprimento * 0.998f, altura * 0.7f, 0.28f),
                            CityPalette.MatTex(Superficie.Rodape, new Color(0.88f, 0.87f, 0.84f), comprimento, altura * 0.7f), 0f, false);
        }

        public static void CercaArame(Transform parent, Vector3 pos, float comprimento, float yaw)
        {
            var go = new GameObject("Cerca");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            int postes = Mathf.Max(2, Mathf.RoundToInt(comprimento / 4f));
            for (int i = 0; i <= postes; i++)
            {
                float x = -comprimento * 0.5f + (comprimento / postes) * i;
                CityPalette.Cyl(go.transform, "Mourao", new Vector3(x, 0.7f, 0f), 0.12f, 1.4f, CityPalette.Madeira);
            }
            for (int i = 0; i < 3; i++)
                CityPalette.Box(go.transform, "Arame", new Vector3(0f, 0.5f + i * 0.35f, 0f), new Vector3(comprimento, 0.03f, 0.03f), CityPalette.Metal, collide: false);
        }

        // ------------------------------------------------------------ laje: caixa d'água, antena, varal
        public static void CoberturaDeLaje(Transform parent, Vector3 topo, float largura, float profundidade)
        {
            if (Random.value < 0.75f)
            {
                var pos = topo + new Vector3(Random.Range(-largura * 0.25f, largura * 0.25f), 0.8f, Random.Range(-profundidade * 0.25f, profundidade * 0.25f));
                CityPalette.Cyl(parent, "CaixaDagua", pos, 1.5f, 1.6f, CityPalette.CaixaDagua);
            }
            if (Random.value < 0.5f)
            {
                CityPalette.Cyl(parent, "Antena", topo + new Vector3(largura * 0.3f, 1.6f, 0f), 0.06f, 3.2f, CityPalette.Metal);
                CityPalette.Box(parent, "Parabolica", topo + new Vector3(largura * 0.3f, 3.0f, 0f), new Vector3(1.1f, 0.08f, 1.1f), CityPalette.Mat(Color.white), 0f, false);
            }
            if (Random.value < 0.45f)
            {
                // varal de roupa — cor é o que dá vida à laje
                for (int i = 0; i < 4; i++)
                    CityPalette.Box(parent, "Roupa",
                        topo + new Vector3(-largura * 0.3f + i * 0.7f, 0.7f, profundidade * 0.25f),
                        new Vector3(0.45f, 0.6f, 0.03f), CityPalette.MatViva(), 0f, false);
            }
        }

        // ------------------------------------------------------------ via: quebra-molas, buraco, placa
        public static void QuebraMolas(Transform parent, Vector3 pos, float largura, float yaw)
        {
            CityPalette.Box(parent, "QuebraMolas", pos + new Vector3(0f, 0.09f, 0f),
                            new Vector3(largura, 0.18f, 1.2f), CityPalette.Mat(new Color(0.85f, 0.80f, 0.25f)), yaw, collide: true);
        }

        public static void PlacaDeRua(Transform parent, Vector3 pos, string nomeX, string nomeZ)
        {
            var go = new GameObject("PlacaDeRua");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            CityPalette.Cyl(go.transform, "Poste", new Vector3(0f, 1.4f, 0f), 0.1f, 2.8f, CityPalette.Metal, collide: true);
            CityPalette.Box(go.transform, "PlacaA", new Vector3(0f, 2.9f, 0f), new Vector3(2.2f, 0.34f, 0.05f), CityPalette.Mat(new Color(0.16f, 0.30f, 0.55f)), 0f, false);
            CityPalette.Box(go.transform, "PlacaB", new Vector3(0f, 2.5f, 0f), new Vector3(2.2f, 0.34f, 0.05f), CityPalette.Mat(new Color(0.16f, 0.30f, 0.55f)), 90f, false);
            CityPalette.Label(go.transform, nomeX, new Vector3(0f, 2.9f, -0.05f), Color.white, 0.14f);
            CityPalette.Label(go.transform, nomeZ, new Vector3(-0.05f, 2.5f, 0f), Color.white, 0.14f, 90f);
        }

        // ------------------------------------------------------------ praia / lazer
        public static void Quiosque(Transform parent, Vector3 pos, string nome)
        {
            var go = new GameObject("Quiosque");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            CityPalette.Box(go.transform, "Balcao", new Vector3(0f, 1.1f, 0f), new Vector3(4.5f, 2.2f, 3.5f), CityPalette.Mat(new Color(0.85f, 0.80f, 0.62f)));
            CityPalette.Box(go.transform, "Teto",   new Vector3(0f, 2.45f, 0f), new Vector3(6.0f, 0.25f, 5.0f), CityPalette.Mat(new Color(0.65f, 0.52f, 0.30f)), 0f, false);
            CityPalette.Label(go.transform, nome, new Vector3(0f, 3.1f, -2.4f), Color.white, 0.26f);
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = new Vector3(Random.Range(-7f, 7f), 0f, Random.Range(-8f, -4f));
                CityPalette.Cyl(go.transform, "Haste", p + new Vector3(0f, 1.1f, 0f), 0.08f, 2.2f, CityPalette.Metal);
                CityPalette.Box(go.transform, "GuardaSol", p + new Vector3(0f, 2.2f, 0f), new Vector3(2.6f, 0.08f, 2.6f),
                                CityPalette.MatViva(), Random.Range(0f, 90f), false);
            }
        }

        public static void CampoDeVarzea(Transform parent, Vector3 centro, float largura, float profundidade)
        {
            var go = new GameObject("CampoDeVarzea");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centro;

            CityPalette.Box(go.transform, "Gramado", new Vector3(0f, 0.03f, 0f), new Vector3(largura, 0.06f, profundidade), CityPalette.GramaSeca, 0f, false);
            for (int s = -1; s <= 1; s += 2)
            {
                var trave = new GameObject("Trave");
                trave.transform.SetParent(go.transform, false);
                trave.transform.localPosition = new Vector3(0f, 0f, s * profundidade * 0.45f);
                CityPalette.Box(trave.transform, "Travessao", new Vector3(0f, 2.2f, 0f), new Vector3(7.0f, 0.14f, 0.14f), CityPalette.Mat(Color.white), 0f, false);
                CityPalette.Cyl(trave.transform, "PosteE", new Vector3(-3.5f, 1.1f, 0f), 0.14f, 2.2f, CityPalette.Mat(Color.white));
                CityPalette.Cyl(trave.transform, "PosteD", new Vector3( 3.5f, 1.1f, 0f), 0.14f, 2.2f, CityPalette.Mat(Color.white));
            }
        }

        // ------------------------------------------------------------ marcos da cidade
        public static void IgrejaMatriz(Transform parent, Vector3 pos, float yaw)
        {
            var go = new GameObject("IgrejaMatriz");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var creme = CityPalette.Mat(new Color(0.90f, 0.86f, 0.76f));
            CityPalette.Box(go.transform, "Nave",  new Vector3(0f, 5f, 0f), new Vector3(14f, 10f, 26f), creme);
            CityPalette.Box(go.transform, "Telhado", new Vector3(0f, 10.4f, 0f), new Vector3(15f, 0.8f, 27f), CityPalette.Telha, 0f, false);
            CityPalette.Box(go.transform, "Torre", new Vector3(0f, 11f, -12f), new Vector3(5.5f, 22f, 5.5f), creme);
            CityPalette.Box(go.transform, "Sino",  new Vector3(0f, 22.6f, -12f), new Vector3(4.0f, 1.2f, 4.0f), CityPalette.Mat(new Color(0.55f, 0.45f, 0.20f)), 0f, false);
            CityPalette.Box(go.transform, "CruzV", new Vector3(0f, 25.4f, -12f), new Vector3(0.3f, 3.4f, 0.3f), creme, 0f, false);
            CityPalette.Box(go.transform, "CruzH", new Vector3(0f, 25.6f, -12f), new Vector3(1.8f, 0.3f, 0.3f), creme, 0f, false);
            CityPalette.Box(go.transform, "Porta", new Vector3(0f, 2.2f, -13.1f), new Vector3(3.0f, 4.4f, 0.3f), CityPalette.Madeira, 0f, false);
        }

        public static void CruzeiroDoMirante(Transform parent, Vector3 pos)
        {
            var go = new GameObject("CruzeiroDoMirante");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            CityPalette.Box(go.transform, "Base",  new Vector3(0f, 0.6f, 0f), new Vector3(6f, 1.2f, 6f), CityPalette.Concreto);
            CityPalette.Box(go.transform, "Haste", new Vector3(0f, 8f, 0f), new Vector3(1.0f, 15f, 1.0f), CityPalette.Mat(Color.white));
            CityPalette.Box(go.transform, "Braco", new Vector3(0f, 12.5f, 0f), new Vector3(6.5f, 1.0f, 1.0f), CityPalette.Mat(Color.white), 0f, false);
            CityPalette.Label(go.transform, "MIRANTE DO CRUZEIRO", new Vector3(0f, 2.4f, -3.2f), new Color(1f, 0.95f, 0.7f), 0.35f);
        }

        public static void TorreDeRadio(Transform parent, Vector3 pos)
        {
            var go = new GameObject("TorreDeRadio");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            for (int i = 0; i < 4; i++)
            {
                float s = (i < 2) ? -1.2f : 1.2f;
                float t = (i % 2 == 0) ? -1.2f : 1.2f;
                var perna = CityPalette.Box(go.transform, "Perna", new Vector3(s, 14f, t), new Vector3(0.25f, 28f, 0.25f), CityPalette.MetalEscuro, 0f, false);
                perna.transform.localRotation = Quaternion.Euler(-t * 1.6f, 0f, s * 1.6f);
            }
            for (int i = 1; i < 8; i++)
                CityPalette.Box(go.transform, "Trelica", new Vector3(0f, i * 3.5f, 0f), new Vector3(2.6f, 0.12f, 2.6f), CityPalette.MetalEscuro, 45f, false);
            CityPalette.Sphere(go.transform, "LuzTopo", new Vector3(0f, 28.5f, 0f), 0.8f, CityPalette.Mat(new Color(1f, 0.25f, 0.2f)));
        }

        public static void CaixaDaguaGigante(Transform parent, Vector3 pos)
        {
            var go = new GameObject("CaixaDaguaMunicipal");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            for (int i = 0; i < 4; i++)
            {
                float s = (i < 2) ? -3f : 3f;
                float t = (i % 2 == 0) ? -3f : 3f;
                CityPalette.Cyl(go.transform, "Perna", new Vector3(s, 6f, t), 0.5f, 12f, CityPalette.Concreto, collide: true);
            }
            CityPalette.Cyl(go.transform, "Reservatorio", new Vector3(0f, 14f, 0f), 11f, 5.5f, CityPalette.Mat(new Color(0.80f, 0.78f, 0.72f)), collide: true);
            CityPalette.Label(go.transform, "SÃO GENÉSIO", new Vector3(0f, 14.2f, -5.6f), new Color(0.2f, 0.35f, 0.6f), 0.6f);
        }
    }
}
