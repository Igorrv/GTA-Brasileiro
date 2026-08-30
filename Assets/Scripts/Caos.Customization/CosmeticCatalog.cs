using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// campos dos DTOs são preenchidos pelo JsonUtility via reflexão — nunca por construtor
#pragma warning disable 0649

namespace Caos.Customization
{
    /// <summary>Item cosmético do catálogo (docs/03 §1.2). <paramref name="estilo"/> guia a geometria
    /// procedural que o <see cref="CharacterStyler"/> monta; <paramref name="corHex"/> tinge o material.</summary>
    [Serializable]
    public class CosmeticItemDto
    {
        public string id;
        public string nome;
        public string estilo;
        public string corHex;
    }

    /// <summary>Gênero do protagonista (docs/03 §1.1) — afeta silhueta e pronomes futuros.</summary>
    [Serializable]
    public class GeneroDto
    {
        public string id;
        public string nome;
    }

    [Serializable] internal class GeneroList   { public List<GeneroDto> items; }

    /// <summary>Formato do cosmetics.json: uma lista nomeada por categoria.</summary>
    [Serializable]
    internal class CosmeticFile
    {
        public List<GeneroDto> generos;
        public List<CosmeticItemDto> tonsDePele;
        public List<CosmeticItemDto> cabelos;
        public List<CosmeticItemDto> coresCabelo;
        public List<CosmeticItemDto> tops;
        public List<CosmeticItemDto> bottoms;
        public List<CosmeticItemDto> calcados;
        public List<CosmeticItemDto> chapeus;
    }

    /// <summary>
    /// Catálogo de cosméticos carregado de <c>StreamingAssets/Data/cosmetics.json</c>.
    /// Segue o mesmo contrato dos demais catálogos (ver CatalogLoader): leitura síncrona no
    /// editor/PC/iOS e UnityWebRequest no Android, com <b>fallback embutido</b> para o jogo
    /// nunca abrir sem opções de customização.
    /// </summary>
    public sealed class CosmeticCatalog
    {
        public List<GeneroDto>       Generos      = new List<GeneroDto>();
        public List<CosmeticItemDto> TonsDePele   = new List<CosmeticItemDto>();
        public List<CosmeticItemDto> Cabelos      = new List<CosmeticItemDto>();
        public List<CosmeticItemDto> CoresCabelo  = new List<CosmeticItemDto>();
        public List<CosmeticItemDto> Tops         = new List<CosmeticItemDto>();
        public List<CosmeticItemDto> Bottoms      = new List<CosmeticItemDto>();
        public List<CosmeticItemDto> Calcados     = new List<CosmeticItemDto>();
        public List<CosmeticItemDto> Chapeus      = new List<CosmeticItemDto>();

        public CosmeticItemDto Pele(string id)    => Buscar(TonsDePele, id);
        public CosmeticItemDto Cabelo(string id)  => Buscar(Cabelos, id);
        public CosmeticItemDto CorCabelo(string id) => Buscar(CoresCabelo, id);
        public CosmeticItemDto Top(string id)     => Buscar(Tops, id);
        public CosmeticItemDto Bottom(string id)  => Buscar(Bottoms, id);
        public CosmeticItemDto Calcado(string id) => Buscar(Calcados, id);
        public CosmeticItemDto Chapeu(string id)  => Buscar(Chapeus, id);

        public string NomeGenero(string id)
        {
            var g = Generos.Find(x => x.id == id);
            return g != null ? g.nome : id;
        }

        private static CosmeticItemDto Buscar(List<CosmeticItemDto> lista, string id)
        {
            if (lista == null || lista.Count == 0) return null;
            for (int i = 0; i < lista.Count; i++)
                if (lista[i].id == id) return lista[i];
            return lista[0];   // id desconhecido (save antigo?) cai na primeira opção, nunca em erro
        }

        /// <summary>Carrega o JSON e chama <paramref name="onDone"/> — nunca com null.</summary>
        public static void LoadAsync(MonoBehaviour host, Action<CosmeticCatalog> onDone)
        {
            host.StartCoroutine(Load(onDone));
        }

        private static System.Collections.IEnumerator Load(Action<CosmeticCatalog> onDone)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Data", "cosmetics.json");
            string json = null;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS
            if (File.Exists(path)) json = File.ReadAllText(path);
            else Debug.LogWarning("[Cosméticos] Arquivo não encontrado: " + path);
#else
            // Android: StreamingAssets fica dentro do APK; precisa de web request
            using (var req = UnityWebRequest.Get(path))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) json = req.downloadHandler.text;
                else Debug.LogWarning("[Cosméticos] Falha ao baixar cosmetics.json: " + req.error);
            }
#endif
            CosmeticCatalog cat = null;
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var f = JsonUtility.FromJson<CosmeticFile>(json);
                    if (f != null)
                    {
                        cat = new CosmeticCatalog
                        {
                            Generos     = f.generos      ?? new List<GeneroDto>(),
                            TonsDePele  = f.tonsDePele   ?? new List<CosmeticItemDto>(),
                            Cabelos     = f.cabelos      ?? new List<CosmeticItemDto>(),
                            CoresCabelo = f.coresCabelo  ?? new List<CosmeticItemDto>(),
                            Tops        = f.tops         ?? new List<CosmeticItemDto>(),
                            Bottoms     = f.bottoms      ?? new List<CosmeticItemDto>(),
                            Calcados    = f.calcados     ?? new List<CosmeticItemDto>(),
                            Chapeus     = f.chapeus      ?? new List<CosmeticItemDto>(),
                        };
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Cosméticos] JSON inválido (" + e.Message + ") — usando fallback.");
                }
            }

            if (cat == null || cat.Tops.Count == 0 || cat.TonsDePele.Count == 0)
            {
                cat = CreateFallback();
                Debug.LogWarning("[Cosméticos] Catálogo ausente/incompleto — usando FALLBACK embutido.");
            }

            Debug.Log($"[Cosméticos] Catálogo: {cat.Generos.Count} gêneros, {cat.TonsDePele.Count} tons de pele, " +
                      $"{cat.Cabelos.Count} cabelos, {cat.Tops.Count} troncos, {cat.Bottoms.Count} pernas, " +
                      $"{cat.Calcados.Count} calçados, {cat.Chapeus.Count} chapéus.");
            onDone?.Invoke(cat);
        }

        /// <summary>
        /// Mínimo garantido do MVP (docs/13 §13.2: gênero, penteados, tons de pele e roupas) para a
        /// tela de personagem abrir mesmo se o JSON falhar — espelha o cosmetics.json.
        /// </summary>
        public static CosmeticCatalog CreateFallback()
        {
            var c = new CosmeticCatalog();
            c.Generos.Add(new GeneroDto { id = "masculino", nome = "Masculino" });
            c.Generos.Add(new GeneroDto { id = "feminino", nome = "Feminino" });
            c.Generos.Add(new GeneroDto { id = "nao_binario", nome = "Não-binário" });

            string[] peles  = { "#F2D2B6", "#E8BF9C", "#D9A877", "#C78E5B", "#9E7354", "#7E5638", "#5F3E28", "#452C1C" };
            for (int i = 0; i < peles.Length; i++)
                c.TonsDePele.Add(new CosmeticItemDto { id = "pele_" + (i + 1), nome = "Tom " + (i + 1), corHex = peles[i] });

            c.Cabelos.Add(new CosmeticItemDto { id = "raspado", nome = "Raspado", estilo = "raspado" });
            c.Cabelos.Add(new CosmeticItemDto { id = "curto", nome = "Curto de Sempre", estilo = "curto" });
            c.Cabelos.Add(new CosmeticItemDto { id = "black_power", nome = "Black Power", estilo = "blackpower" });
            c.Cabelos.Add(new CosmeticItemDto { id = "moicano", nome = "Moicano", estilo = "moicano" });
            c.Cabelos.Add(new CosmeticItemDto { id = "longo", nome = "Longo", estilo = "longo" });
            c.Cabelos.Add(new CosmeticItemDto { id = "coque", nome = "Coque", estilo = "coque" });

            c.CoresCabelo.Add(new CosmeticItemDto { id = "preto", nome = "Preto", corHex = "#1A1412" });
            c.CoresCabelo.Add(new CosmeticItemDto { id = "castanho", nome = "Castanho", corHex = "#4A2E1A" });
            c.CoresCabelo.Add(new CosmeticItemDto { id = "loiro", nome = "Loiro", corHex = "#C9A24B" });
            c.CoresCabelo.Add(new CosmeticItemDto { id = "ruivo", nome = "Ruivo", corHex = "#8A3B1E" });
            c.CoresCabelo.Add(new CosmeticItemDto { id = "grisalho", nome = "Grisalho", corHex = "#9A958C" });
            c.CoresCabelo.Add(new CosmeticItemDto { id = "azul_caos", nome = "Azul do Caos (fantasia)", corHex = "#2E5FA8" });

            c.Tops.Add(new CosmeticItemDto { id = "camiseta_amarelinha", nome = "Camiseta Amarelinha", estilo = "camiseta", corHex = "#F2D12E" });
            c.Tops.Add(new CosmeticItemDto { id = "camiseta_helenico", nome = "Camisa do Helênico FC", estilo = "camiseta", corHex = "#D6222A" });
            c.Tops.Add(new CosmeticItemDto { id = "regata_branca", nome = "Regata Branca", estilo = "regata", corHex = "#EDEDE8" });
            c.Tops.Add(new CosmeticItemDto { id = "camisa_linho", nome = "Camisa de Linho", estilo = "camisa", corHex = "#F5F2E8" });
            c.Tops.Add(new CosmeticItemDto { id = "jaqueta_couro", nome = "Jaqueta de Couro", estilo = "jaqueta", corHex = "#3A2A22" });
            c.Tops.Add(new CosmeticItemDto { id = "uniforme_motoboy", nome = "Uniforme de Motoboy", estilo = "jaqueta", corHex = "#E8841E" });
            c.Tops.Add(new CosmeticItemDto { id = "moletom_caos", nome = "Moletom do Caos", estilo = "jaqueta", corHex = "#2E4A8A" });
            c.Tops.Add(new CosmeticItemDto { id = "vestido_floral", nome = "Vestido Floral", estilo = "vestido", corHex = "#D96A8B" });

            c.Bottoms.Add(new CosmeticItemDto { id = "calca_jeans", nome = "Calça Jeans", estilo = "calca", corHex = "#2A4D9E" });
            c.Bottoms.Add(new CosmeticItemDto { id = "calca_moletom", nome = "Calça de Moletom", estilo = "calca", corHex = "#7A7A80" });
            c.Bottoms.Add(new CosmeticItemDto { id = "bermuda_cargo", nome = "Bermuda Cargo", estilo = "bermuda", corHex = "#6B6A4E" });
            c.Bottoms.Add(new CosmeticItemDto { id = "short_praia", nome = "Short de Praia", estilo = "bermuda", corHex = "#3FA7C4" });
            c.Bottoms.Add(new CosmeticItemDto { id = "saia_jeans", nome = "Saia Jeans", estilo = "saia", corHex = "#3E5F9E" });
            c.Bottoms.Add(new CosmeticItemDto { id = "saia_floral", nome = "Saia Floral", estilo = "saia", corHex = "#D96A8B" });

            c.Calcados.Add(new CosmeticItemDto { id = "tenis_branco", nome = "Tênis Branco", estilo = "tenis", corHex = "#E8E8E8" });
            c.Calcados.Add(new CosmeticItemDto { id = "tenis_preto", nome = "Tênis Preto", estilo = "tenis", corHex = "#232326" });
            c.Calcados.Add(new CosmeticItemDto { id = "chinelo", nome = "Chinelo de Dedo", estilo = "chinelo", corHex = "#2A4D9E" });
            c.Calcados.Add(new CosmeticItemDto { id = "bota_couro", nome = "Bota de Couro", estilo = "bota", corHex = "#4A3222" });

            c.Chapeus.Add(new CosmeticItemDto { id = "nenhum", nome = "Cabeça Livre", estilo = "nenhum", corHex = "#000000" });
            c.Chapeus.Add(new CosmeticItemDto { id = "bone_verde", nome = "Boné Aba Reta", estilo = "bone", corHex = "#267347" });
            c.Chapeus.Add(new CosmeticItemDto { id = "chapeu_palha", nome = "Chapéu de Palha", estilo = "chapeu", corHex = "#D9C27A" });
            c.Chapeus.Add(new CosmeticItemDto { id = "bandana", nome = "Bandana", estilo = "bandana", corHex = "#B03030" });
            return c;
        }
    }
}
