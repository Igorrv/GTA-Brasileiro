using System.Collections.Generic;
using Caos.Simulation;
using UnityEngine;

namespace Caos.Customization
{
    /// <summary>
    /// Aplica um <see cref="CosmeticLoadout"/> num <see cref="CharacterRig"/> já montado —
    /// <b>sem nenhum asset importado</b>: só troca materiais (cacheados pela <see cref="CityPalette"/>),
    /// remolda as primitivas existentes e cria peças extras procedurais (saia, cano de bota)
    /// marcadas com o prefixo <c>Look_</c>.
    ///
    /// Truques importantes:
    ///  • <b>estado base</b>: posição/escala/malha originais de cada peça são capturadas na primeira
    ///    aplicação, então trocar de visual cem vezes nunca acumula distorção;
    ///  • <b>reuso de malha</b>: o "Cabelo" vira moicano trocando o <c>sharedMesh</c> pela malha
    ///    built-in do cubo — nada de instanciar primitiva nova por troca;
    ///  • <b>peças Look_</b> são devolvidas numa lista para o chamador sincronizar a visibilidade
    ///    com o resto do corpo (o PlayerVehicleLink esconde o boneco dentro do carro e não conhece
    ///    as peças criadas depois do boot).
    /// </summary>
    public static class CharacterStyler
    {
        public const string PrefixoExtra = "Look_";

        // ------------------------------------------------------------ estado base
        private sealed class Base
        {
            public Vector3 pos, scale;
            public Quaternion rot;
            public Mesh mesh;
        }

        private static readonly Dictionary<Transform, Base> _base = new Dictionary<Transform, Base>();
        private static readonly Dictionary<PrimitiveType, Mesh> _meshes = new Dictionary<PrimitiveType, Mesh>();

        private static Base BaseDe(Transform t)
        {
            if (_base.TryGetValue(t, out var b)) return b;
            var mf = t.GetComponent<MeshFilter>();
            b = new Base
            {
                pos   = t.localPosition,
                scale = t.localScale,
                rot   = t.localRotation,
                mesh  = mf != null ? mf.sharedMesh : null,
            };
            _base[t] = b;
            return b;
        }

        /// <summary>Malha built-in de uma primitiva, cacheada (o asset sobrevive à destruição do GO temporário).</summary>
        private static Mesh MalhaDe(PrimitiveType tipo)
        {
            if (_meshes.TryGetValue(tipo, out var m) && m != null) return m;
            var tmp = GameObject.CreatePrimitive(tipo);
            m = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(tmp);
            _meshes[tipo] = m;
            return m;
        }

        // ------------------------------------------------------------ helpers
        private static Transform Filho(Transform pai, string nome) => pai == null ? null : pai.Find(nome);

        private static void Pintar(Transform t, Material m)
        {
            if (t == null) return;
            var r = t.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }

        private static void Mostrar(Transform t, bool on)
        {
            if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
        }

        private static void Moldar(Transform t, Vector3 pos, Vector3 escala, PrimitiveType? malha = null)
        {
            if (t == null) return;
            var b = BaseDe(t);
            t.localPosition = pos;
            t.localScale    = escala;
            t.localRotation = b.rot;
            if (malha.HasValue)
            {
                var mf = t.GetComponent<MeshFilter>();
                if (mf != null) mf.sharedMesh = MalhaDe(malha.Value);
            }
        }

        /// <summary>Volta a peça ao estado original do rig (base capturada na 1ª aplicação).</summary>
        private static void Restaurar(Transform t)
        {
            if (t == null) return;
            var b = BaseDe(t);
            Moldar(t, b.pos, b.scale);
            var mf = t.GetComponent<MeshFilter>();
            if (mf != null && b.mesh != null) mf.sharedMesh = b.mesh;
        }

        private static void Escalar(Transform t, float fx, float fy, float fz)
        {
            if (t == null) return;
            var b = BaseDe(t);
            t.localScale = new Vector3(b.scale.x * fx, b.scale.y * fy, b.scale.z * fz);
        }

        // ------------------------------------------------------------ materiais
        private static Material MatPele(Color c)    => CityPalette.MatTex(Superficie.Pele,   c, 0.4f, 0.6f, 0.16f, 0f);
        private static Material MatTecido(Color c)  => CityPalette.MatTex(Superficie.Tecido, c, 0.5f, 0.6f, 0.10f, 0f);
        private static Material MatJeans(Color c)   => CityPalette.MatTex(Superficie.Jeans,  c, 0.5f, 0.9f, 0.08f, 0f);
        private static Material MatCabelo(Color c)  => CityPalette.Mat(c, 0.30f, 0f);
        private static Material MatCouro(Color c)   => CityPalette.Mat(c, 0.45f, 0.05f);

        private static Material MatRoupa(CosmeticItemDto item)
        {
            Color c = CorDe(item, Color.gray);
            bool jeans = item != null && item.id != null && item.id.Contains("jeans");
            return jeans ? MatJeans(c) : MatTecido(c);
        }

        private static Color CorDe(CosmeticItemDto item, Color fallback)
            => item != null ? CityPalette.Parse(item.corHex, fallback) : fallback;

        // ------------------------------------------------------------ ponto de entrada
        /// <summary>
        /// Aplica o visual completo. Devolve os renderers extras (<c>Look_*</c>) criados —
        /// o chamador guarda a lista para sincronizar visibilidade ao entrar/sair do carro.
        /// </summary>
        public static List<Renderer> Aplicar(CharacterRig rig, CosmeticLoadout look, CosmeticCatalog cat)
        {
            var extras = DestruirExtrasAntigos(rig);

            var pele    = cat != null ? cat.Pele(look.pele)         : null;
            var cabelo  = cat != null ? cat.Cabelo(look.cabelo)     : null;
            var corCab  = cat != null ? cat.CorCabelo(look.corCabelo) : null;
            var top     = cat != null ? cat.Top(look.top)           : null;
            var bottom  = cat != null ? cat.Bottom(look.bottom)     : null;
            var calcado = cat != null ? cat.Calcado(look.calcado)   : null;
            var chapeu  = cat != null ? cat.Chapeu(look.chapeu)     : null;

            var mPele = MatPele(CorDe(pele, new Color(0.62f, 0.45f, 0.33f)));

            AplicarPele(rig, mPele);
            AplicarGenero(rig, look.genero);
            AplicarTop(rig, top, mPele);
            AplicarBottom(rig, top, bottom, mPele, extras);
            AplicarCalcado(rig, calcado, extras);
            AplicarCabelo(rig, cabelo, MatCabelo(CorDe(corCab, new Color(0.10f, 0.08f, 0.07f))));
            AplicarChapeu(rig, chapeu);

            return extras;
        }

        /// <summary>Remove as peças procedurais da aplicação anterior e devolve a lista (vazia) de extras.</summary>
        private static List<Renderer> DestruirExtrasAntigos(CharacterRig rig)
        {
            var extras = new List<Renderer>();
            if (rig == null) return extras;

            var todos = rig.GetComponentsInChildren<Transform>(true);
            var paraDestruir = new List<GameObject>();
            foreach (var t in todos)
            {
                if (!t.name.StartsWith(PrefixoExtra, System.StringComparison.Ordinal)) continue;
                // só a raiz da peça: destruir o pai já leva os filhos
                if (t.parent != null && t.parent.name.StartsWith(PrefixoExtra, System.StringComparison.Ordinal)) continue;
                paraDestruir.Add(t.gameObject);
            }
            foreach (var go in paraDestruir) Object.Destroy(go);
            return extras;
        }

        // ------------------------------------------------------------ pele
        private static void AplicarPele(CharacterRig rig, Material mPele)
        {
            Pintar(Filho(rig.Tronco, "Pescoco"), mPele);
            foreach (var n in new[] { "Cranio", "Maxilar", "OrelhaE", "OrelhaD", "Nariz" })
                Pintar(Filho(rig.Cabeca, n), mPele);

            foreach (var braco in new[] { rig.BracoE, rig.BracoD })
            {
                Pintar(Filho(braco, "Malha"), mPele);
                Pintar(Filho(braco, "MalhaPonta"), mPele);
            }
            foreach (var ante in new[] { rig.AnteBracoE, rig.AnteBracoD })
            {
                Pintar(Filho(ante, "Malha"), mPele);
                Pintar(Filho(ante, "MalhaPonta"), mPele);
                Pintar(Filho(ante, "Mao"), mPele);
                Pintar(Filho(ante, "Polegar"), mPele);
            }
        }

        // ------------------------------------------------------------ gênero (silhueta)
        /// <summary>
        /// Ajustes sutis de proporção — hitbox e animações ficam idênticas (docs/03 §1.2.1:
        /// "não altera atributos"). Tudo multiplicado sobre a base capturada, nunca acumulado.
        /// </summary>
        private static void AplicarGenero(CharacterRig rig, string genero)
        {
            var peito   = Filho(rig.Tronco, "Peito");
            var abdomen = Filho(rig.Tronco, "Abdomen");
            var quadril = Filho(rig.Tronco, "Quadril");
            var ombroE  = Filho(rig.Tronco, "OmbroE");
            var ombroD  = Filho(rig.Tronco, "OmbroD");
            var pescoco = Filho(rig.Tronco, "Pescoco");

            // sempre parte da base (desfaz a escolha anterior) — inclusive os pivôs dos braços,
            // que o AproximarOmbros desloca
            foreach (var t in new[] { peito, abdomen, quadril, ombroE, ombroD, pescoco, rig.BracoE, rig.BracoD })
                Restaurar(t);

            if (genero == "feminino")
            {
                Escalar(peito,   0.94f, 1.02f, 1.16f);   // busto
                Escalar(abdomen, 0.92f, 1.00f, 0.94f);   // cintura
                Escalar(quadril, 1.14f, 1.00f, 1.10f);   // quadril mais largo
                Escalar(ombroE,  0.88f, 0.88f, 0.88f);
                Escalar(ombroD,  0.88f, 0.88f, 0.88f);
                Escalar(pescoco, 0.90f, 0.90f, 0.90f);
                AproximarOmbros(rig, 0.90f);
            }
            else if (genero == "nao_binario")
            {
                Escalar(peito,   0.97f, 1.00f, 1.07f);
                Escalar(quadril, 1.06f, 1.00f, 1.04f);
                Escalar(ombroE,  0.94f, 0.94f, 0.94f);
                Escalar(ombroD,  0.94f, 0.94f, 0.94f);
                AproximarOmbros(rig, 0.95f);
            }
        }

        private static void AproximarOmbros(CharacterRig rig, float fator)
        {
            foreach (var ombro in new[] { Filho(rig.Tronco, "OmbroE"), Filho(rig.Tronco, "OmbroD") })
            {
                if (ombro == null) continue;
                var b = BaseDe(ombro);
                ombro.localPosition = new Vector3(b.pos.x * fator, b.pos.y, b.pos.z);
            }
            // os pivôs dos braços acompanham os ombros, senão o braço fica flutuando longe do corpo
            foreach (var braco in new[] { rig.BracoE, rig.BracoD })
            {
                if (braco == null) continue;
                var b = BaseDe(braco);
                braco.localPosition = new Vector3(b.pos.x * fator, b.pos.y, b.pos.z);
            }
        }

        // ------------------------------------------------------------ tronco (top)
        private static void AplicarTop(CharacterRig rig, CosmeticItemDto top, Material mPele)
        {
            string estilo = top != null ? top.estilo : "camiseta";
            var mTop = MatRoupa(top);

            var abdomen = Filho(rig.Tronco, "Abdomen");
            var peito   = Filho(rig.Tronco, "Peito");
            var gola    = Filho(rig.Tronco, "Gola");
            var ombroE  = Filho(rig.Tronco, "OmbroE");
            var ombroD  = Filho(rig.Tronco, "OmbroD");
            var mangaE  = Filho(rig.BracoE, "MangaE");
            var mangaD  = Filho(rig.BracoD, "MangaD");

            Restaurar(gola);
            Mostrar(mangaE, true);
            Mostrar(mangaD, true);

            Pintar(abdomen, mTop);
            Pintar(peito, mTop);
            Pintar(gola, mTop);

            switch (estilo)
            {
                case "regata":
                    // sem manga e sem ombro de tecido: ombro e braço ficam na pele
                    Mostrar(mangaE, false);
                    Mostrar(mangaD, false);
                    Pintar(ombroE, mPele);
                    Pintar(ombroD, mPele);
                    break;

                case "camisa":
                    // social: gola alta e ombros cobertos
                    Escalar(gola, 1.05f, 1.7f, 1.05f);
                    Pintar(ombroE, mTop);
                    Pintar(ombroD, mTop);
                    Pintar(mangaE, mTop);
                    Pintar(mangaD, mTop);
                    break;

                case "jaqueta":
                    // manga comprida: cobre braço e antebraço inteiros (a mão continua na pele)
                    Pintar(ombroE, mTop);
                    Pintar(ombroD, mTop);
                    Pintar(mangaE, mTop);
                    Pintar(mangaD, mTop);
                    foreach (var braco in new[] { rig.BracoE, rig.BracoD })
                    {
                        Pintar(Filho(braco, "Malha"), mTop);
                        Pintar(Filho(braco, "MalhaPonta"), mTop);
                    }
                    foreach (var ante in new[] { rig.AnteBracoE, rig.AnteBracoD })
                    {
                        Pintar(Filho(ante, "Malha"), mTop);
                        Pintar(Filho(ante, "MalhaPonta"), mTop);
                    }
                    break;

                default: // camiseta / vestido: manga curta, braço na pele
                    Pintar(ombroE, mTop);
                    Pintar(ombroD, mTop);
                    Pintar(mangaE, mTop);
                    Pintar(mangaD, mTop);
                    break;
            }
        }

        // ------------------------------------------------------------ pernas (bottom)
        private static void AplicarBottom(CharacterRig rig, CosmeticItemDto top, CosmeticItemDto bottom,
                                          Material mPele, List<Renderer> extras)
        {
            bool vestido = top != null && top.estilo == "vestido";
            string estilo = vestido ? "saia" : (bottom != null ? bottom.estilo : "calca");
            var mBottom = vestido ? MatRoupa(top) : MatRoupa(bottom);

            var quadril = Filho(rig.Tronco, "Quadril");
            Pintar(quadril, mBottom);

            var barraE = Filho(rig.PernaE, "BarraE");
            var barraD = Filho(rig.PernaD, "BarraD");
            Mostrar(barraE, true);
            Mostrar(barraD, true);

            foreach (var (coxa, canela, barra) in new[]
            {
                (rig.PernaE, rig.CanelaE, barraE),
                (rig.PernaD, rig.CanelaD, barraD),
            })
            {
                switch (estilo)
                {
                    case "bermuda":
                        Pintar(Filho(coxa, "Malha"), mBottom);
                        Pintar(Filho(coxa, "MalhaPonta"), mBottom);
                        Pintar(barra, mBottom);                       // barra da bermuda no joelho
                        Pintar(Filho(canela, "Malha"), mPele);        // canela de fora
                        Pintar(Filho(canela, "MalhaPonta"), mPele);
                        break;

                    case "saia":
                        Pintar(Filho(coxa, "Malha"), mPele);          // perna toda de fora,
                        Pintar(Filho(coxa, "MalhaPonta"), mPele);     // a saia cobre o quadril
                        Pintar(Filho(canela, "Malha"), mPele);
                        Pintar(Filho(canela, "MalhaPonta"), mPele);
                        Mostrar(barra, false);
                        break;

                    default: // calça comprida
                        Pintar(Filho(coxa, "Malha"), mBottom);
                        Pintar(Filho(coxa, "MalhaPonta"), mBottom);
                        Pintar(barra, mBottom);
                        Pintar(Filho(canela, "Malha"), mBottom);
                        Pintar(Filho(canela, "MalhaPonta"), mBottom);
                        break;
                }
            }

            if (estilo == "saia") ConstruirSaia(rig, mBottom, vestido, extras);
        }

        /// <summary>
        /// Saia evasê em dois cilindros sobrepostos (cilindro não afunila — o de baixo mais largo
        /// dá a leitura do caimento). Fica presa ao tronco: solta como saia de verdade na passada.
        /// </summary>
        private static void ConstruirSaia(CharacterRig rig, Material m, bool longa, List<Renderer> extras)
        {
            var raiz = new GameObject(PrefixoExtra + "Saia");
            raiz.transform.SetParent(rig.Tronco, false);

            float comp = longa ? 1.25f : 1f;
            var cima  = CityPalette.Cyl(raiz.transform, "SaiaA", new Vector3(0f, 0.02f, 0f), 0.46f, 0.20f * comp, m);
            var baixo = CityPalette.Cyl(raiz.transform, "SaiaB", new Vector3(0f, -0.13f * comp, 0f), 0.55f, 0.17f * comp, m);
            // afunila menos em largura que em profundidade — saia não é um tambor
            cima.transform.localScale  = new Vector3(cima.transform.localScale.x,  cima.transform.localScale.y,  0.40f);
            baixo.transform.localScale = new Vector3(baixo.transform.localScale.x, baixo.transform.localScale.y, 0.48f);

            foreach (var r in raiz.GetComponentsInChildren<Renderer>()) extras.Add(r);
        }

        // ------------------------------------------------------------ calçado
        private static void AplicarCalcado(CharacterRig rig, CosmeticItemDto calcado, List<Renderer> extras)
        {
            string estilo = calcado != null ? calcado.estilo : "tenis";
            Color cor = CorDe(calcado, new Color(0.13f, 0.13f, 0.14f));
            var m = estilo == "bota" ? MatCouro(cor) : CityPalette.Mat(cor, 0.35f, 0f);

            foreach (var (canela, pe) in new[] { (rig.CanelaE, "PeE"), (rig.CanelaD, "PeD") })
            {
                var peT = Filho(canela, pe);
                Restaurar(peT);
                Pintar(peT, m);
                if (estilo == "chinelo") Escalar(peT, 1f, 0.62f, 1f);   // chinelo é chato
            }

            if (estilo == "bota")
            {
                // cano da bota: cápsula extra abraçando a canela
                foreach (var (canela, lado) in new[] { (rig.CanelaE, "E"), (rig.CanelaD, "D") })
                {
                    var cano = CityPalette.Capsule(canela, PrefixoExtra + "Cano" + lado,
                                                   new Vector3(0f, -0.26f, 0f), 0.165f, 0.34f, m);
                    var r = cano.GetComponent<Renderer>();
                    if (r != null) extras.Add(r);
                }
            }
        }

        // ------------------------------------------------------------ cabelo
        /// <summary>
        /// Reusa as esferas "Cabelo"/"Nuca" do rig: cada penteado é uma combinação de
        /// posição/escala/malha — black power estufa, moicano vira uma crista de cubo,
        /// coque manda a "Nuca" pro alto da cabeça. A sobrancelha acompanha a cor.
        /// </summary>
        private static void AplicarCabelo(CharacterRig rig, CosmeticItemDto cabelo, Material mCabelo)
        {
            string estilo = cabelo != null ? cabelo.estilo : "curto";

            var topo = Filho(rig.Cabeca, "Cabelo");
            var nuca = Filho(rig.Cabeca, "Nuca");
            Restaurar(topo);
            Restaurar(nuca);
            Mostrar(topo, true);
            Mostrar(nuca, true);
            Pintar(topo, mCabelo);
            Pintar(nuca, mCabelo);
            // sobrancelha acompanha a cor do cabelo (há duas com o mesmo nome — pinta todas)
            var todas = rig.Cabeca.GetComponentsInChildren<Transform>(true);
            foreach (var t in todas)
                if (t.name == "Sobrancelha") Pintar(t, mCabelo);

            var b = BaseDe(topo);
            switch (estilo)
            {
                case "raspado":
                    Moldar(topo, b.pos + new Vector3(0f, -0.008f, 0.004f),
                           new Vector3(b.scale.x * 0.97f, b.scale.y * 0.80f, b.scale.z * 0.99f));
                    Mostrar(nuca, false);
                    break;

                case "blackpower":
                    Moldar(topo, b.pos + new Vector3(0f, 0.035f, 0f),
                           new Vector3(b.scale.x * 1.28f, b.scale.y * 1.42f, b.scale.z * 1.24f));
                    Mostrar(nuca, false);
                    break;

                case "moicano":
                    Moldar(topo, b.pos + new Vector3(0f, 0.075f, 0f),
                           new Vector3(0.07f, 0.10f, 0.30f), PrimitiveType.Cube);
                    Mostrar(nuca, false);
                    break;

                case "longo":
                    // a "nuca" desce pelas costas — cabelo comprido de verdade
                    Moldar(nuca, new Vector3(0f, -0.10f, -0.085f), new Vector3(0.24f, 0.34f, 0.19f));
                    break;

                case "coque":
                    Moldar(topo, b.pos, b.scale * 0.96f);
                    Moldar(nuca, new Vector3(0f, 0.155f, -0.135f), Vector3.one * 0.13f);
                    break;

                default: // curto — o corte original do rig
                    break;
            }
        }

        // ------------------------------------------------------------ chapéu / cabeça
        private static void AplicarChapeu(CharacterRig rig, CosmeticItemDto chapeu)
        {
            string estilo = chapeu != null ? chapeu.estilo : "bone";
            var bone = Filho(rig.Cabeca, "Bone");
            var aba  = Filho(rig.Cabeca, "Aba");

            Restaurar(bone);
            Restaurar(aba);
            Mostrar(bone, true);
            Mostrar(aba, true);

            var m = MatTecido(CorDe(chapeu, new Color(0.15f, 0.45f, 0.28f)));
            Pintar(bone, m);
            Pintar(aba, m);

            switch (estilo)
            {
                case "nenhum":
                    Mostrar(bone, false);
                    Mostrar(aba, false);
                    break;

                case "chapeu":
                    // chapéu de palha: copo cilíndrico + aba redonda larga
                    Moldar(bone, new Vector3(0f, 0.155f, 0f), new Vector3(0.30f, 0.055f, 0.30f), PrimitiveType.Cylinder);
                    Moldar(aba,  new Vector3(0f, 0.115f, 0.01f), new Vector3(0.50f, 0.012f, 0.50f), PrimitiveType.Cylinder);
                    break;

                case "bandana":
                    Moldar(bone, new Vector3(0f, 0.095f, 0f), new Vector3(0.29f, 0.05f, 0.29f));
                    Mostrar(aba, false);
                    break;

                default: // boné — o original do rig, só muda a cor
                    break;
            }
        }
    }
}
