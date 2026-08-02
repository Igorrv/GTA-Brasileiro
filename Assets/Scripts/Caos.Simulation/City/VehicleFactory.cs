using System.Collections.Generic;
using Caos.Data;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Monta a carroceria dos veículos a partir do <see cref="VehicleDto"/> (campo <c>carroceria</c>).
    /// O mesmo montador serve pro carro do jogador, pro tráfego, pra polícia e pros carros estacionados —
    /// por isso o Fusca parece Fusca, a Kombi parece Kombi e o busão tem 12 metros.
    ///
    /// Convenção: <b>frente = +Z</b> (mesma do <see cref="VehicleController"/>, que põe as rodas
    /// dianteiras em +halfWheelBase). A origem fica no chão.
    /// </summary>
    public static class VehicleFactory
    {
        /// <summary>Monta a carroceria como filha de <paramref name="root"/>. Devolve o colisor principal.</summary>
        public static BoxCollider BuildBody(Transform root, VehicleDto dto, Color cor, bool rodasVisuais)
        {
            float L = dto != null && dto.comprimento > 0.5f ? dto.comprimento : 4.2f;
            float W = dto != null && dto.largura     > 0.3f ? dto.largura     : 1.7f;
            float H = dto != null && dto.altura      > 0.3f ? dto.altura      : 1.45f;

            var style = ParseStyle(dto);
            // lataria: textura de verniz com flake metálico, tingida pela cor do modelo
            var mat   = CityPalette.MatTex(Superficie.Pintura, cor, L, H, 0.78f, 0.30f);

            GameObject chassi;
            switch (style)
            {
                case BodyStyle.Moto:     chassi = Moto(root, L, W, H, mat);            break;
                case BodyStyle.Bike:     chassi = Bicicleta(root, L, W, H, mat);       break;
                case BodyStyle.Onibus:   chassi = Onibus(root, L, W, H, mat);          break;
                case BodyStyle.Caminhao: chassi = Caminhao(root, L, W, H, mat);        break;
                case BodyStyle.Van:      chassi = Van(root, L, W, H, mat);             break;
                case BodyStyle.Picape:   chassi = Picape(root, L, W, H, mat);          break;
                case BodyStyle.Buggy:    chassi = Buggy(root, L, W, H, mat);           break;
                case BodyStyle.Trator:   chassi = Trator(root, L, W, H, mat);          break;
                case BodyStyle.Sedan:    chassi = Carro(root, L, W, H, mat, true);     break;
                default:                 chassi = Carro(root, L, W, H, mat, false);    break;
            }

            if (rodasVisuais && style != BodyStyle.Moto && style != BodyStyle.Bike)
                RodasDecorativas(root, L, W, style);

            var col = chassi.GetComponent<BoxCollider>();
            return col;
        }

        public static BodyStyle ParseStyle(VehicleDto dto)
        {
            if (dto != null && System.Enum.TryParse<BodyStyle>(dto.carroceria, true, out var s)) return s;
            if (dto == null) return BodyStyle.Hatch;
            switch (dto.classe)
            {
                case "Moto":        return BodyStyle.Moto;
                case "Bicicleta":   return BodyStyle.Bike;
                case "Onibus":      return BodyStyle.Onibus;
                case "Caminhao":    return BodyStyle.Caminhao;
                case "Van":         return BodyStyle.Van;
                case "Caminhonete": return BodyStyle.Picape;
                case "Rural":       return BodyStyle.Trator;
                default:            return BodyStyle.Hatch;
            }
        }

        public static Color CorDe(VehicleDto dto) => CityPalette.Parse(dto != null ? dto.corHex : null, new Color(0.75f, 0.15f, 0.15f));

        /// <summary>Sorteia um modelo do catálogo com peso pela raridade (1 = comum na rua).</summary>
        public static VehicleDto SortearParaTrafego(GameCatalogs c, params string[] excluirClasses)
        {
            if (c == null || c.Vehicles.Count == 0) return null;
            var pool = new List<VehicleDto>();
            foreach (var v in c.Vehicles)
            {
                bool excluido = false;
                for (int i = 0; i < excluirClasses.Length; i++)
                    if (v.classe == excluirClasses[i]) { excluido = true; break; }
                if (excluido) continue;

                int peso = Mathf.Clamp(6 - Mathf.Max(1, v.raridade), 1, 5);
                for (int k = 0; k < peso; k++) pool.Add(v);
            }
            if (pool.Count == 0) return c.Vehicles[Random.Range(0, c.Vehicles.Count)];
            return pool[Random.Range(0, pool.Count)];
        }

        // ================================================================== carrocerias
        private static GameObject Carro(Transform root, float L, float W, float H, Material mat, bool sedan)
        {
            float hChassi = H * 0.46f;
            var chassi = CityPalette.Box(root, "Carroceria", new Vector3(0f, hChassi * 0.5f + 0.28f, 0f),
                                         new Vector3(W, hChassi, L), mat);

            float cabW = W * 0.92f;
            float cabL = sedan ? L * 0.42f : L * 0.46f;
            float cabZ = sedan ? -L * 0.06f : -L * 0.02f;
            float cabY = hChassi + 0.28f + H * 0.20f;

            // teto em cápsula achatada: carro não tem quina no teto, e é essa curva que separa
            // "carro" de "caixote com rodas" quando a câmera está atrás
            var cabine = CityPalette.Capsule(root, "Cabine", new Vector3(0f, cabY, cabZ), cabW, cabL, mat);
            cabine.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cabine.transform.localScale    = new Vector3(cabW, cabL * 0.5f, H * 0.44f);

            // capô e porta-malas: planos que quebram o volume único
            CityPalette.Box(root, "Capo", new Vector3(0f, hChassi + 0.30f, cabZ + cabL * 0.5f + L * 0.14f),
                            new Vector3(W * 0.94f, 0.08f, L * 0.26f), mat, 0f, false);
            CityPalette.Box(root, "PortaMalas", new Vector3(0f, hChassi + 0.30f, cabZ - cabL * 0.5f - L * 0.11f),
                            new Vector3(W * 0.94f, 0.08f, L * 0.20f), mat, 0f, false);

            // para-lamas sobre as rodas (a "sobrancelha" que todo carro tem)
            for (int s = -1; s <= 1; s += 2)
            for (int f = -1; f <= 1; f += 2)
                CityPalette.Box(root, "Paralama", new Vector3(s * W * 0.49f, hChassi * 0.35f + 0.30f, f * L * 0.33f),
                                new Vector3(0.09f, 0.16f, L * 0.30f), mat, 0f, false);

            // vidros (para-brisa, traseiro e laterais numa peça só de cada lado)
            CityPalette.Box(root, "ParaBrisa", new Vector3(0f, hChassi + 0.28f + H * 0.20f, cabZ + cabL * 0.5f),
                            new Vector3(cabW * 0.92f, H * 0.30f, 0.08f), VidroDoCarro(), 0f, false);
            CityPalette.Box(root, "Traseiro", new Vector3(0f, hChassi + 0.28f + H * 0.20f, cabZ - cabL * 0.5f),
                            new Vector3(cabW * 0.92f, H * 0.28f, 0.08f), VidroDoCarro(), 0f, false);
            CityPalette.Box(root, "VidroE", new Vector3(-cabW * 0.5f, hChassi + 0.28f + H * 0.20f, cabZ),
                            new Vector3(0.06f, H * 0.26f, cabL * 0.86f), VidroDoCarro(), 0f, false);
            CityPalette.Box(root, "VidroD", new Vector3( cabW * 0.5f, hChassi + 0.28f + H * 0.20f, cabZ),
                            new Vector3(0.06f, H * 0.26f, cabL * 0.86f), VidroDoCarro(), 0f, false);

            Luzes(root, L, W, hChassi * 0.5f + 0.28f);
            Placa(root, L, hChassi * 0.35f + 0.28f);
            return chassi;
        }

        private static GameObject Picape(Transform root, float L, float W, float H, Material mat)
        {
            float hChassi = H * 0.42f;
            var chassi = CityPalette.Box(root, "Carroceria", new Vector3(0f, hChassi * 0.5f + 0.34f, 0f),
                                         new Vector3(W, hChassi, L), mat);

            float cabL = L * 0.34f;
            float cabZ = L * 0.12f;
            CityPalette.Box(root, "Cabine", new Vector3(0f, hChassi + 0.34f + H * 0.22f, cabZ),
                            new Vector3(W * 0.94f, H * 0.44f, cabL), mat, 0f, false);
            CityPalette.Box(root, "ParaBrisa", new Vector3(0f, hChassi + 0.34f + H * 0.22f, cabZ + cabL * 0.5f),
                            new Vector3(W * 0.86f, H * 0.32f, 0.08f), VidroDoCarro(), 0f, false);

            // caçamba
            float cacL = L * 0.40f, cacZ = -L * 0.22f;
            CityPalette.Box(root, "CacambaE", new Vector3(-W * 0.47f, hChassi + 0.34f + H * 0.10f, cacZ), new Vector3(0.10f, H * 0.22f, cacL), mat, 0f, false);
            CityPalette.Box(root, "CacambaD", new Vector3( W * 0.47f, hChassi + 0.34f + H * 0.10f, cacZ), new Vector3(0.10f, H * 0.22f, cacL), mat, 0f, false);
            CityPalette.Box(root, "Tampa",    new Vector3(0f, hChassi + 0.34f + H * 0.10f, cacZ - cacL * 0.5f), new Vector3(W * 0.94f, H * 0.22f, 0.10f), mat, 0f, false);

            Luzes(root, L, W, hChassi * 0.5f + 0.34f);
            Placa(root, L, hChassi * 0.35f + 0.34f);
            return chassi;
        }

        private static GameObject Van(Transform root, float L, float W, float H, Material mat)
        {
            var chassi = CityPalette.Box(root, "Carroceria", new Vector3(0f, H * 0.5f + 0.22f, 0f),
                                         new Vector3(W, H * 0.86f, L), mat);
            CityPalette.Box(root, "ParaBrisa", new Vector3(0f, H * 0.72f, L * 0.5f),
                            new Vector3(W * 0.88f, H * 0.34f, 0.08f), VidroDoCarro(), 0f, false);
            CityPalette.Box(root, "JanelaE", new Vector3(-W * 0.5f, H * 0.68f, -L * 0.08f),
                            new Vector3(0.06f, H * 0.26f, L * 0.5f), VidroDoCarro(), 0f, false);
            CityPalette.Box(root, "JanelaD", new Vector3( W * 0.5f, H * 0.68f, -L * 0.08f),
                            new Vector3(0.06f, H * 0.26f, L * 0.5f), VidroDoCarro(), 0f, false);
            Luzes(root, L, W, H * 0.42f);
            Placa(root, L, H * 0.28f);
            return chassi;
        }

        private static GameObject Onibus(Transform root, float L, float W, float H, Material mat)
        {
            var chassi = CityPalette.Box(root, "Carroceria", new Vector3(0f, H * 0.5f + 0.35f, 0f),
                                         new Vector3(W, H * 0.82f, L), mat);
            // faixa corrida de janelas (o jeito mais barato de "ler" um ônibus)
            CityPalette.Box(root, "JanelasE", new Vector3(-W * 0.5f, H * 0.72f, -L * 0.05f),
                            new Vector3(0.08f, H * 0.26f, L * 0.82f), VidroDoCarro(), 0f, false);
            CityPalette.Box(root, "JanelasD", new Vector3( W * 0.5f, H * 0.72f, -L * 0.05f),
                            new Vector3(0.08f, H * 0.26f, L * 0.82f), VidroDoCarro(), 0f, false);
            CityPalette.Box(root, "ParaBrisa", new Vector3(0f, H * 0.70f, L * 0.5f),
                            new Vector3(W * 0.9f, H * 0.36f, 0.1f), VidroDoCarro(), 0f, false);
            CityPalette.Box(root, "Porta", new Vector3(W * 0.5f, H * 0.42f, L * 0.28f),
                            new Vector3(0.12f, H * 0.6f, 1.1f), CityPalette.MetalEscuro, 0f, false);
            CityPalette.Box(root, "Letreiro", new Vector3(0f, H * 0.95f, L * 0.5f),
                            new Vector3(W * 0.6f, 0.35f, 0.1f), CityPalette.Mat(new Color(0.1f, 0.1f, 0.12f)), 0f, false);
            Luzes(root, L, W, 0.9f);
            return chassi;
        }

        private static GameObject Caminhao(Transform root, float L, float W, float H, Material mat)
        {
            float cabL = L * 0.28f;
            var chassi = CityPalette.Box(root, "Cabine", new Vector3(0f, H * 0.42f + 0.5f, L * 0.5f - cabL * 0.5f),
                                         new Vector3(W, H * 0.7f, cabL), mat);
            CityPalette.Box(root, "ParaBrisa", new Vector3(0f, H * 0.60f + 0.5f, L * 0.5f - 0.05f),
                            new Vector3(W * 0.88f, H * 0.3f, 0.08f), VidroDoCarro(), 0f, false);
            CityPalette.Box(root, "Bau", new Vector3(0f, H * 0.45f + 0.6f, -L * 0.5f + (L - cabL) * 0.5f),
                            new Vector3(W, H * 0.72f, L - cabL), CityPalette.Mat(new Color(0.86f, 0.86f, 0.84f)));
            CityPalette.Box(root, "Chassi", new Vector3(0f, 0.55f, 0f), new Vector3(W * 0.7f, 0.3f, L), CityPalette.MetalEscuro, 0f, false);
            CityPalette.Cyl(root, "Escapamento", new Vector3(W * 0.45f, H * 0.8f + 0.5f, L * 0.5f - cabL), 0.18f, H * 0.9f, CityPalette.Metal);
            Luzes(root, L, W, 1.0f);
            return chassi;
        }

        /// <summary>
        /// Moto de verdade — a CG e a Biz são metade do trânsito brasileiro e mereciam mais que duas
        /// caixas. Tem tanque em cápsula, banco em dois níveis (piloto e garupa), motor aparente entre
        /// as rodas, garfo inclinado como o de moto real (~26° de cáster), escapamento saindo pelo
        /// lado direito, paralama, guidão com manoplas e retrovisores.
        /// </summary>
        private static GameObject Moto(Transform root, float L, float W, float H, Material mat)
        {
            var preto  = CityPalette.Mat(new Color(0.11f, 0.11f, 0.12f), 0.35f, 0.2f);
            var cromo  = CityPalette.Mat(new Color(0.74f, 0.75f, 0.78f), 0.85f, 0.95f);
            var motorM = CityPalette.Mat(new Color(0.45f, 0.46f, 0.48f), 0.55f, 0.85f);

            // quadro central (é ele que carrega o colisor)
            var chassi = CityPalette.Box(root, "Quadro", new Vector3(0f, H * 0.55f, -L * 0.02f),
                                         new Vector3(W * 0.42f, H * 0.26f, L * 0.50f), mat);

            // tanque: cápsula deitada, estreitando para trás
            var tanque = CityPalette.Capsule(root, "Tanque", new Vector3(0f, H * 0.74f, L * 0.12f), W * 0.78f, L * 0.34f, mat);
            tanque.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tanque.transform.localScale    = new Vector3(W * 0.80f, L * 0.19f, H * 0.34f);

            // banco em dois níveis: piloto mais baixo, garupa mais alta
            CityPalette.Box(root, "BancoPiloto", new Vector3(0f, H * 0.72f, -L * 0.10f),
                            new Vector3(W * 0.62f, H * 0.10f, L * 0.22f), preto, 0f, false);
            CityPalette.Box(root, "BancoGarupa", new Vector3(0f, H * 0.78f, -L * 0.28f),
                            new Vector3(W * 0.56f, H * 0.10f, L * 0.18f), preto, 0f, false);
            CityPalette.Box(root, "Rabeta", new Vector3(0f, H * 0.70f, -L * 0.42f),
                            new Vector3(W * 0.40f, H * 0.14f, L * 0.16f), mat, 0f, false);

            // motor aparente + escapamento pelo lado direito
            CityPalette.Box(root, "Motor", new Vector3(0f, H * 0.40f, L * 0.02f),
                            new Vector3(W * 0.66f, H * 0.30f, L * 0.22f), motorM, 0f, false);
            var escape = CityPalette.Capsule(root, "Escapamento", new Vector3(W * 0.34f, H * 0.30f, -L * 0.20f), 0.11f, L * 0.44f, cromo);
            escape.transform.localRotation = Quaternion.Euler(84f, 0f, 0f);

            // garfo inclinado (cáster) + coluna de direção
            for (int s = -1; s <= 1; s += 2)
            {
                var haste = CityPalette.Capsule(root, "Garfo", new Vector3(s * W * 0.16f, H * 0.55f, L * 0.33f), 0.07f, H * 0.85f, cromo);
                haste.transform.localRotation = Quaternion.Euler(26f, 0f, 0f);
            }
            CityPalette.Box(root, "Guidao", new Vector3(0f, H * 0.97f, L * 0.30f),
                            new Vector3(W * 1.20f, 0.05f, 0.05f), preto, 0f, false);
            for (int s = -1; s <= 1; s += 2)
            {
                CityPalette.Capsule(root, "Manopla", new Vector3(s * W * 0.52f, H * 0.97f, L * 0.30f), 0.055f, 0.13f, preto)
                           .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                CityPalette.Box(root, "Retrovisor", new Vector3(s * W * 0.46f, H * 1.12f, L * 0.30f),
                                new Vector3(0.11f, 0.075f, 0.04f), preto, 0f, false);
            }

            // paralama dianteiro + farol + lanterna
            CityPalette.Box(root, "Paralama", new Vector3(0f, H * 0.52f, L * 0.40f),
                            new Vector3(W * 0.34f, 0.06f, L * 0.20f), mat, 0f, false);
            CityPalette.Box(root, "Farol", new Vector3(0f, H * 0.88f, L * 0.40f),
                            new Vector3(0.20f, 0.17f, 0.07f), CityPalette.LuzAcesa, 0f, false);
            CityPalette.Box(root, "Lanterna", new Vector3(0f, H * 0.76f, -L * 0.49f),
                            new Vector3(0.13f, 0.08f, 0.05f), CityPalette.Mat(new Color(0.72f, 0.09f, 0.09f), 0.85f, 0.1f), 0f, false);

            RodaDeMoto(root, new Vector3(0f, 0.33f,  L * 0.38f), 0.66f, 0.13f);
            RodaDeMoto(root, new Vector3(0f, 0.33f, -L * 0.36f), 0.66f, 0.17f);
            return chassi;
        }

        /// <summary>Roda de moto: pneu fino + disco de freio + raios sugeridos pela calota estreita.</summary>
        private static void RodaDeMoto(Transform root, Vector3 pos, float diametro, float largura)
        {
            var pneu = CityPalette.Cyl(root, "Pneu", pos, diametro, largura,
                                       CityPalette.MatTex(Superficie.Pneu, Color.white, diametro * 3f, largura * 3f, 0.12f, 0f));
            pneu.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            pneu.transform.localScale    = new Vector3(diametro, largura * 0.5f, diametro);

            var aro = CityPalette.Cyl(root, "Aro", pos, diametro * 0.62f, largura * 1.08f,
                                      CityPalette.Mat(new Color(0.68f, 0.69f, 0.72f), 0.70f, 0.90f));
            aro.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            aro.transform.localScale    = new Vector3(diametro * 0.62f, largura * 0.54f, diametro * 0.62f);

            var disco = CityPalette.Cyl(root, "Disco", pos, diametro * 0.42f, largura * 1.3f,
                                        CityPalette.Mat(new Color(0.35f, 0.36f, 0.38f), 0.75f, 0.9f));
            disco.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            disco.transform.localScale    = new Vector3(diametro * 0.42f, largura * 0.10f, diametro * 0.42f);
        }

        private static GameObject Bicicleta(Transform root, float L, float W, float H, Material mat)
        {
            var chassi = CityPalette.Box(root, "Quadro", new Vector3(0f, H * 0.62f, 0f), new Vector3(0.08f, 0.08f, L * 0.7f), mat);
            CityPalette.Box(root, "Selim",  new Vector3(0f, H * 0.85f, -L * 0.22f), new Vector3(0.18f, 0.08f, 0.34f), CityPalette.Mat(Color.black), 0f, false);
            CityPalette.Box(root, "Guidao", new Vector3(0f, H * 0.88f, L * 0.30f), new Vector3(0.55f, 0.05f, 0.05f), CityPalette.Metal, 0f, false);
            Roda(root, new Vector3(0f, 0.33f,  L * 0.36f), 0.66f, 0.06f);
            Roda(root, new Vector3(0f, 0.33f, -L * 0.36f), 0.66f, 0.06f);
            return chassi;
        }

        private static GameObject Buggy(Transform root, float L, float W, float H, Material mat)
        {
            var chassi = CityPalette.Box(root, "Casco", new Vector3(0f, 0.55f, 0f), new Vector3(W, H * 0.42f, L), mat);
            // santantônio
            CityPalette.Box(root, "ArcoE", new Vector3(-W * 0.42f, H * 0.75f, -L * 0.10f), new Vector3(0.1f, H * 0.8f, 0.1f), CityPalette.Metal, 0f, false);
            CityPalette.Box(root, "ArcoD", new Vector3( W * 0.42f, H * 0.75f, -L * 0.10f), new Vector3(0.1f, H * 0.8f, 0.1f), CityPalette.Metal, 0f, false);
            CityPalette.Box(root, "ArcoT", new Vector3(0f, H * 1.14f, -L * 0.10f), new Vector3(W * 0.92f, 0.1f, 0.1f), CityPalette.Metal, 0f, false);
            CityPalette.Box(root, "Bancos", new Vector3(0f, 0.85f, -L * 0.08f), new Vector3(W * 0.75f, 0.5f, 0.6f), CityPalette.Mat(new Color(0.12f, 0.12f, 0.13f)), 0f, false);
            Luzes(root, L, W, 0.6f);
            return chassi;
        }

        private static GameObject Trator(Transform root, float L, float W, float H, Material mat)
        {
            var chassi = CityPalette.Box(root, "Corpo", new Vector3(0f, 0.95f, 0f), new Vector3(W * 0.6f, H * 0.45f, L * 0.8f), mat);
            CityPalette.Box(root, "Cabine", new Vector3(0f, H * 0.75f, -L * 0.18f), new Vector3(W * 0.55f, H * 0.5f, L * 0.34f), VidroDoCarro(), 0f, false);
            CityPalette.Cyl(root, "Escapamento", new Vector3(W * 0.22f, H * 0.85f, L * 0.28f), 0.16f, H * 0.8f, CityPalette.MetalEscuro);
            Roda(root, new Vector3(-W * 0.52f, 0.95f, -L * 0.28f), 1.9f, 0.55f);
            Roda(root, new Vector3( W * 0.52f, 0.95f, -L * 0.28f), 1.9f, 0.55f);
            Roda(root, new Vector3(-W * 0.45f, 0.5f,  L * 0.32f), 1.0f, 0.35f);
            Roda(root, new Vector3( W * 0.45f, 0.5f,  L * 0.32f), 1.0f, 0.35f);
            return chassi;
        }

        // ================================================================== detalhes
        /// <summary>
        /// Frente e traseira completas: para-choque, grade, farol com lente e refletor, lanterna
        /// vermelha e escapamento. São peças pequenas, mas é a ausência delas que faz um carro
        /// parecer um sabonete — o olho procura justamente esses contornos.
        /// </summary>
        private static void Luzes(Transform root, float L, float W, float y)
        {
            Grade(root, L, W, y);

            var cromo   = CityPalette.Mat(new Color(0.72f, 0.73f, 0.76f), 0.80f, 0.95f);
            var escuro  = CityPalette.Mat(new Color(0.12f, 0.12f, 0.13f), 0.30f, 0.2f);
            var lente   = CityPalette.Mat(new Color(0.92f, 0.90f, 0.80f), 0.90f, 0.1f);
            var vermelho= CityPalette.Mat(new Color(0.72f, 0.09f, 0.09f), 0.85f, 0.1f);
            var ambar   = CityPalette.Mat(new Color(0.95f, 0.55f, 0.10f), 0.85f, 0.1f);

            // para-choques
            CityPalette.Box(root, "ParaChoqueF", new Vector3(0f, y - 0.16f, L * 0.5f + 0.05f),
                            new Vector3(W * 1.01f, 0.26f, 0.16f), escuro, 0f, false);
            CityPalette.Box(root, "ParaChoqueT", new Vector3(0f, y - 0.16f, -L * 0.5f - 0.05f),
                            new Vector3(W * 1.01f, 0.26f, 0.16f), escuro, 0f, false);

            // faróis: refletor + lente por cima (dá profundidade em vez de adesivo)
            for (int s = -1; s <= 1; s += 2)
            {
                CityPalette.Box(root, "FarolCaixa", new Vector3(s * W * 0.32f, y, L * 0.5f - 0.02f),
                                new Vector3(W * 0.24f, 0.18f, 0.10f), cromo, 0f, false);
                CityPalette.Box(root, "FarolLente", new Vector3(s * W * 0.32f, y, L * 0.5f + 0.04f),
                                new Vector3(W * 0.21f, 0.15f, 0.05f), lente, 0f, false);
                CityPalette.Box(root, "Pisca", new Vector3(s * W * 0.46f, y, L * 0.5f + 0.02f),
                                new Vector3(W * 0.09f, 0.12f, 0.06f), ambar, 0f, false);

                CityPalette.Box(root, "Lanterna", new Vector3(s * W * 0.32f, y, -L * 0.5f - 0.03f),
                                new Vector3(W * 0.22f, 0.20f, 0.07f), vermelho, 0f, false);
            }

            // retrovisores
            for (int s = -1; s <= 1; s += 2)
            {
                CityPalette.Box(root, "Retrovisor", new Vector3(s * (W * 0.55f), y + 0.42f, L * 0.20f),
                                new Vector3(0.16f, 0.11f, 0.09f), escuro, 0f, false);
                CityPalette.Box(root, "Braco", new Vector3(s * (W * 0.50f), y + 0.42f, L * 0.20f),
                                new Vector3(0.10f, 0.04f, 0.04f), escuro, 0f, false);
            }

            // escapamento saindo por baixo, atrás
            CityPalette.Cyl(root, "Escapamento", new Vector3(W * 0.28f, y - 0.26f, -L * 0.5f - 0.06f), 0.09f, 0.22f, cromo)
                       .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // frisos laterais / maçanetas
            for (int s = -1; s <= 1; s += 2)
            {
                CityPalette.Box(root, "Friso", new Vector3(s * W * 0.505f, y - 0.10f, 0f),
                                new Vector3(0.03f, 0.07f, L * 0.66f), escuro, 0f, false);
                CityPalette.Box(root, "Macaneta", new Vector3(s * W * 0.51f, y + 0.30f, L * 0.04f),
                                new Vector3(0.03f, 0.05f, 0.20f), cromo, 0f, false);
            }
        }

        /// <summary>Vidro do carro: fumê com reflexo — material compartilhado por toda a frota.</summary>
        private static Material VidroDoCarro()
            => CityPalette.MatTex(Superficie.VidroCarro, Color.white, 3f, 3f, 0.90f, 0.35f);

        private static void Placa(Transform root, float L, float y)
        {
            // placa Mercosul, com a tarja azul em cima
            CityPalette.Box(root, "Placa", new Vector3(0f, y, -L * 0.5f - 0.03f), new Vector3(0.4f, 0.13f, 0.03f),
                            CityPalette.MatTex(Superficie.Placa, Color.white, 0.5f, 0.5f, 0.25f, 0f), 0f, false);
        }

        /// <summary>Grade do radiador na frente — detalhe pequeno que "fecha" a leitura do carro.</summary>
        private static void Grade(Transform root, float L, float W, float y)
        {
            CityPalette.Box(root, "Grade", new Vector3(0f, y, L * 0.5f + 0.02f), new Vector3(W * 0.52f, 0.20f, 0.04f),
                            CityPalette.MatTex(Superficie.Grade, Color.white, 0.5f, 0.5f, 0.45f, 0.6f), 0f, false);
        }

        private static void Roda(Transform root, Vector3 pos, float diametro, float largura)
        {
            // pneu com sulco de verdade + calota clara no centro
            var go = CityPalette.Cyl(root, "Pneu", pos, diametro, largura,
                                     CityPalette.MatTex(Superficie.Pneu, Color.white, diametro * 3f, largura * 3f, 0.12f, 0f));
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            go.transform.localScale    = new Vector3(diametro, largura * 0.5f, diametro);

            var calota = CityPalette.Cyl(root, "Calota", pos, diametro * 0.55f, largura * 1.06f,
                                         CityPalette.Mat(new Color(0.62f, 0.63f, 0.66f), 0.65f, 0.85f));
            calota.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            calota.transform.localScale    = new Vector3(diametro * 0.55f, largura * 0.53f, diametro * 0.55f);
        }

        /// <summary>Rodas puramente visuais (tráfego/estacionados — sem WheelCollider).</summary>
        private static void RodasDecorativas(Transform root, float L, float W, BodyStyle style)
        {
            float r  = style == BodyStyle.Onibus || style == BodyStyle.Caminhao ? 1.0f : 0.66f;
            float lw = style == BodyStyle.Onibus || style == BodyStyle.Caminhao ? 0.4f : 0.26f;
            float zf = L * 0.33f, zr = -L * 0.33f, x = W * 0.5f;

            Roda(root, new Vector3(-x, r * 0.5f,  zf), r, lw);
            Roda(root, new Vector3( x, r * 0.5f,  zf), r, lw);
            Roda(root, new Vector3(-x, r * 0.5f,  zr), r, lw);
            Roda(root, new Vector3( x, r * 0.5f,  zr), r, lw);
        }
    }
}
