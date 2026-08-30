using System;
using System.Collections.Generic;
using System.IO;
using Caos.Core;
using Caos.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace Caos.Content
{
    // Wrappers para JsonUtility (não desserializa array JSON top-level)
    [Serializable] class VehicleList  { public List<VehicleDto>  items; }
    [Serializable] class FactionList  { public List<FactionDto>  items; }
    [Serializable] class DistrictList { public List<DistrictDto> items; }
    [Serializable] class ItemList     { public List<ItemDto>     items; }
    [Serializable] class EventList    { public List<EventDto>    items; }
    [Serializable] class MissionList  { public List<MissionDto>  items; }
    [Serializable] class ShopList     { public List<ShopDto>     items; }
    [Serializable] class RadioList    { public List<RadioStationDto> items; }
    [Serializable] class WorldList    { public List<WorldDto>    items; }

    /// <summary>
    /// Carrega todos os catálogos. No editor/PC usa File (síncrono); em builds usa UnityWebRequest (async).
    /// Chama <paramref name="onDone"/> quando pronto (GameManager espera isso antes de iniciar o tick).
    ///
    /// Vive num assembly separado de <see cref="GameCatalogs"/> porque é a única parte dos dados que
    /// precisa da engine (arquivo, web request, JsonUtility). Com a divisão, <c>Caos.Data</c> —
    /// e portanto <c>Caos.Gameplay</c>, que depende dele — compila e é testado fora do Unity.
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
            yield return Read<WorldList>("worlds.json",     list => c.Worlds.AddRange(list.items ?? Empty<WorldDto>()));

            c.IndexAll();
            CaosLog.Info($"[Catalogs] Cfg carregada: {c.Vehicles.Count} veículos, {c.Factions.Count} facções, {c.Districts.Count} bairros, {c.Items.Count} itens, {c.Events.Count} eventos, {c.Missions.Count} missões, {c.Shops.Count} estabelecimentos, {c.Radio.Count} estações de rádio.");
            onDone?.Invoke(c);
        }

        private static List<T> Empty<T>() => new List<T>();

        private static System.Collections.IEnumerator Read<T>(string fileName, Action<T> onParsed) where T : class
        {
            string path = Path.Combine(Application.streamingAssetsPath, kFolder, fileName);
            string json = null;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS
            if (File.Exists(path)) json = File.ReadAllText(path);
            else CaosLog.Aviso($"[Catalogs] Arquivo não encontrado: {path}");
#else
            // Android: StreamingAssets fica no APK; precisa de web request
            using (var req = UnityWebRequest.Get(path))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) json = req.downloadHandler.text;
                else CaosLog.Aviso($"[Catalogs] Falha ao baixar {fileName}: {req.error}");
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
