using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Caos.Data
{
    /// <summary>
    /// Catálogos em tempo de execução, carregados dos JSON em StreamingAssets/Data.
    /// Indexados por id para lookup O(1). Substituível por ScriptableObjects no editor (ver docs/12).
    /// </summary>
    public sealed class GameCatalogs
    {
        public readonly List<VehicleDto>  Vehicles  = new List<VehicleDto>();
        public readonly List<FactionDto>  Factions  = new List<FactionDto>();
        public readonly List<DistrictDto> Districts = new List<DistrictDto>();
        public readonly List<ItemDto>     Items     = new List<ItemDto>();
        public readonly List<EventDto>    Events    = new List<EventDto>();
        public readonly List<MissionDto>  Missions  = new List<MissionDto>();
        public readonly List<ShopDto>     Shops     = new List<ShopDto>();
        public readonly List<RadioStationDto> Radio = new List<RadioStationDto>();
        public StreetNamesDto             Streets   = null;

        public readonly Dictionary<string, VehicleDto>  VehicleById  = new Dictionary<string, VehicleDto>();
        public readonly Dictionary<string, ItemDto>     ItemById     = new Dictionary<string, ItemDto>();
        public readonly Dictionary<string, EventDto>    EventById    = new Dictionary<string, EventDto>();
        public readonly Dictionary<string, MissionDto>  MissionById  = new Dictionary<string, MissionDto>();
        public readonly Dictionary<string, DistrictDto> DistrictById = new Dictionary<string, DistrictDto>();
        public readonly Dictionary<string, ShopDto>     ShopById     = new Dictionary<string, ShopDto>();

        public void IndexAll()
        {
            Index(Vehicles, VehicleById);
            Index(Items, ItemById);
            Index(Events, EventById);
            Index(Missions, MissionById);
            Index(Districts, DistrictById);
            Index(Shops, ShopById);
        }
        private static void Index<T>(List<T> src, Dictionary<string, T> dst) where T : class
        {
            dst.Clear();
            foreach (var t in src)
            {
                var id = (string)typeof(T).GetField("id")?.GetValue(t);
                if (!string.IsNullOrEmpty(id)) dst[id] = t;
            }
        }

        /// <summary>
        /// Catálogo mínimo garantido (veículo + missão) para o mundo <b>sempre</b> abrir, mesmo que os JSON em
        /// StreamingAssets falhem ao carregar. Elimina o ponto único de falha do boot.
        /// </summary>
        public static GameCatalogs CreateFallback()
        {
            var c = new GameCatalogs();
            c.Vehicles.Add(new VehicleDto { id = "uno_escada", nome = "Fiasco Unus c/ Escada", classe = "Popular", massa = 950f, potencia = 60f, zeroACem = 14f, consumoKmPorL = 12f, tanqueL = 48f, preco = 12000f, dirigibilidade = 3, spawnBairro = "Centro", carroceria = "Hatch", corHex = "#C8352B", comprimento = 3.7f, largura = 1.6f, altura = 1.4f, velMaxKmh = 145f, apelido = "escadinha", raridade = 1 });
            c.Vehicles.Add(new VehicleDto { id = "fusca_besouro", nome = "Volksmann Besouro 1300", classe = "Popular", massa = 800f, potencia = 40f, zeroACem = 18f, consumoKmPorL = 14f, tanqueL = 40f, preco = 9000f, dirigibilidade = 3, spawnBairro = "Centro", carroceria = "Hatch", corHex = "#3D6BB3", comprimento = 4.0f, largura = 1.6f, altura = 1.5f, velMaxKmh = 130f, apelido = "fusca", raridade = 2 });
            c.Districts.Add(new DistrictDto { id = "Centro", nome = "Centro Histórico", tipo = "Centro", probEventoBase = 0.35f, centroX = 0f, centroZ = 0f, raio = 160f, corHex = "#B9A88F", alturaMin = 3f, alturaMax = 9f, policiamento = 3 });
            c.Missions.Add(new MissionDto
            {
                id = "M01", tipo = "Principal", titulo = "Chegada de Van", dador = "tonho_van",
                recompensaRs = 0f, recompensaXp = 50f, faccao = "", preRequisitos = new List<string>(),
                objetivos = new List<MissionObjectiveDto> { new MissionObjectiveDto { tipo = "ir", alvo = "van_tonho", quantidade = 1, local = "Centro" } }
            });
            c.IndexAll();
            return c;
        }
    }

    // Wrappers para JsonUtility (não desserializa array JSON top-level)
    [Serializable] class VehicleList  { public List<VehicleDto>  items; }
    [Serializable] class FactionList  { public List<FactionDto>  items; }
    [Serializable] class DistrictList { public List<DistrictDto> items; }
    [Serializable] class ItemList     { public List<ItemDto>     items; }
    [Serializable] class EventList    { public List<EventDto>    items; }
    [Serializable] class MissionList  { public List<MissionDto>  items; }
    [Serializable] class ShopList     { public List<ShopDto>     items; }
    [Serializable] class RadioList    { public List<RadioStationDto> items; }

    /// <summary>
    /// Carrega todos os catálogos. No editor/PC usa File (síncrono); em builds usa UnityWebRequest (async).
    /// Chama <paramref name="onDone"/> quando pronto (GameManager espera isso antes de iniciar o tick).
    /// </summary>
    public static class CatalogLoader
    {
        private const string kFolder = "Data";

        public static void LoadAsync(MonoBehaviour host, Action<GameCatalogs> onDone)
        {
            host.StartCoroutine(LoadAll(onDone));
        }

        private static System.Collections.IEnumerator LoadAll(Action<GameCatalogs> onDone)
        {
            var c = new GameCatalogs();

            yield return Read<VehicleList>("vehicles.json",  list => c.Vehicles.AddRange(list.items ?? Empty<VehicleDto>()));
            yield return Read<FactionList>("factions.json",  list => c.Factions.AddRange(list.items ?? Empty<FactionDto>()));
            yield return Read<DistrictList>("districts.json",list => c.Districts.AddRange(list.items ?? Empty<DistrictDto>()));
            yield return Read<ItemList>("items.json",        list => c.Items.AddRange(list.items ?? Empty<ItemDto>()));
            yield return Read<EventList>("events.json",      list => c.Events.AddRange(list.items ?? Empty<EventDto>()));
            yield return Read<MissionList>("missions.json",  list => c.Missions.AddRange(list.items ?? Empty<MissionDto>()));
            yield return Read<ShopList>("shops.json",        list => c.Shops.AddRange(list.items ?? Empty<ShopDto>()));
            yield return Read<RadioList>("radio.json",       list => c.Radio.AddRange(list.items ?? Empty<RadioStationDto>()));
            yield return Read<StreetNamesDto>("streets.json",dto  => c.Streets = dto);

            c.IndexAll();
            UnityEngine.Debug.Log($"[Catalogs] Cfg carregada: {c.Vehicles.Count} veículos, {c.Factions.Count} facções, {c.Districts.Count} bairros, {c.Items.Count} itens, {c.Events.Count} eventos, {c.Missions.Count} missões, {c.Shops.Count} estabelecimentos, {c.Radio.Count} estações de rádio.");
            onDone?.Invoke(c);
        }

        private static List<T> Empty<T>() => new List<T>();

        private static System.Collections.IEnumerator Read<T>(string fileName, Action<T> onParsed) where T : class
        {
            string path = Path.Combine(Application.streamingAssetsPath, kFolder, fileName);
            string json = null;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS
            if (File.Exists(path)) json = File.ReadAllText(path);
            else UnityEngine.Debug.LogWarning($"[Catalogs] Arquivo não encontrado: {path}");
#else
            // Android: StreamingAssets fica no APK; precisa de web request
            using (var req = UnityWebRequest.Get(path))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) json = req.downloadHandler.text;
                else UnityEngine.Debug.LogWarning($"[Catalogs] Falha ao baixar {fileName}: {req.error}");
            }
#endif
            if (!string.IsNullOrEmpty(json))
            {
                var parsed = JsonUtility.FromJson<T>(json);
                onParsed?.Invoke(parsed);
            }
            else
            {
                onParsed?.Invoke(null);
            }
            yield break;
        }
    }
}
