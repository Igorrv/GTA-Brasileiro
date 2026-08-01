using System;
using System.Collections.Generic;
using System.IO;
using Caos.Data;
using Caos.Gameplay;
using Caos.World;
using UnityEngine;

namespace Caos.Save
{
    /// <summary>
    /// Persistência local (JSON em persistentDataPath), com <b>3 slots</b> independentes — o menu
    /// inicial lista os três e o jogo grava sempre no slot escolhido. Pronto para cloud sync (docs/12 §12.3.2).
    ///
    /// O slot 1 continua sendo o arquivo antigo <c>save.json</c>, para não perder o progresso de quem
    /// já jogava antes dos slots existirem.
    /// </summary>
    public static class SaveSystem
    {
        public const int Slots = 3;

        /// <summary>Slot em uso. O menu inicial define antes do jogo carregar.</summary>
        public static int SlotAtual { get; set; } = 1;

        private static string Arquivo(int slot) => slot <= 1 ? "save.json" : $"save_{slot}.json";
        private static string Caminho(int slot) => System.IO.Path.Combine(Application.persistentDataPath, Arquivo(slot));

        public static bool Existe(int slot) => File.Exists(Caminho(slot));
        public static bool HasSave => Existe(SlotAtual);

        // ------------------------------------------------------------------ gravar / ler
        public static void Capture(PlayerAttributes attrs, EconomyService econ, ReputationService rep,
            WorldStateService world, TimeOfDayService time, MissionService missions)
            => Capture(SlotAtual, attrs, econ, rep, world, time, missions);

        public static void Capture(int slot, PlayerAttributes attrs, EconomyService econ, ReputationService rep,
            WorldStateService world, TimeOfDayService time, MissionService missions)
        {
            var data = new SaveData
            {
                pFome = attrs.Fome, pSede = attrs.Sede, pEnergia = attrs.Energia,
                pSanidade = attrs.Sanidade, pSaude = attrs.Saude,
                eRs = econ.Rs, eCaosCash = econ.CaosCash, eIpc = econ.IpcCaos,
                wCaos = world.Caos, wStars = world.Stars,
                wDistrict = world.CurrentDistrict.ToString(), wWeather = world.Weather.ToString(),
                wHour = time.Hour, wDay = time.Day,
                repFaction = ToSave(rep.FactionSnapshot()), repDistrict = ToSave(rep.DistrictSnapshot()),
                missionsCompleted = missions.CompletedSnapshot(), missionsActive = missions.ActiveSnapshot(),
                savedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            };
            File.WriteAllText(Caminho(slot), JsonUtility.ToJson(data, prettyPrint: true));
            Debug.Log($"[Save] Slot {slot} salvo em {Caminho(slot)}.");
        }

        public static SaveData Load() => Load(SlotAtual);

        public static SaveData Load(int slot)
        {
            if (!Existe(slot)) return null;
            try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(Caminho(slot))); }
            catch (Exception e) { Debug.LogWarning($"[Save] Falha ao carregar slot {slot}: {e.Message}"); return null; }
        }

        public static void Apply(SaveData d, PlayerAttributes attrs, EconomyService econ, ReputationService rep,
            WorldStateService world, TimeOfDayService time, MissionService missions)
        {
            if (d == null) return;
            // saves da versão 1 não têm Sede: entra hidratado em vez de nascer morrendo de sede
            float sede = d.saveVersion >= 2 ? d.pSede : 70f;
            attrs.Hydrate(d.pFome, sede, d.pEnergia, d.pSanidade, d.pSaude);
            econ.Hydrate(d.eRs, d.eCaosCash, d.eIpc);
            Enum.TryParse(d.wDistrict, true, out DistrictId dist);
            Enum.TryParse(d.wWeather, true, out WeatherState wthr);
            world.Hydrate(d.wCaos, d.wStars, dist, wthr);
            time.Hydrate(d.wHour, d.wDay);
            rep.Hydrate(FromSave(d.repFaction), FromSave(d.repDistrict));
            missions.Hydrate(d.missionsCompleted, d.missionsActive);
            Debug.Log("[Save] Estado restaurado.");
        }

        public static void Delete() => Delete(SlotAtual);

        public static void Delete(int slot)
        {
            if (Existe(slot)) File.Delete(Caminho(slot));
        }

        // ------------------------------------------------------------------ resumo p/ o menu
        /// <summary>Lê só o cabeçalho de um slot, sem tocar nos serviços — alimenta o menu inicial.</summary>
        public static SaveSlotInfo Peek(int slot)
        {
            var info = new SaveSlotInfo { slot = slot, existe = false, bairro = "—" };
            var d = Load(slot);
            if (d == null) return info;

            info.existe   = true;
            info.dia      = d.wDay;
            info.hora     = d.wHour;
            info.rs       = d.eRs;
            info.bairro   = NomeDeBairro(d.wDistrict);
            info.missoes  = d.missionsCompleted != null ? d.missionsCompleted.Count : 0;
            info.estrelas = d.wStars;
            info.salvoEm  = d.savedAt;
            return info;
        }

        public static SaveSlotInfo[] PeekTodos()
        {
            var r = new SaveSlotInfo[Slots];
            for (int i = 0; i < Slots; i++) r[i] = Peek(i + 1);
            return r;
        }

        private static string NomeDeBairro(string id)
        {
            switch (id)
            {
                case "VistaAlegre": return "Comunidade Vista Alegre";
                case "MonteVerde":  return "Polo Monte Verde";
                case "SitioCapim":  return "Sítio do Capim";
                case "Belvedere":   return "Jardim Belvedere";
                case "Itauna":      return "Praia de Itaúna";
                case "Rodoviaria":  return "Terminal Rodoviário";
                case "Marginal":    return "Marginal do Rio Sujo";
                case "Cohab":       return "COHAB Bom Retiro";
                case "Centro":      return "Centro Histórico";
                default:            return string.IsNullOrEmpty(id) ? "—" : id;
            }
        }

        // ---- conversões ReputationService.RepEntry <-> RepEntrySave ----
        private static List<RepEntrySave> ToSave(List<ReputationService.RepEntry> src)
        {
            var r = new List<RepEntrySave>();
            foreach (var e in src) r.Add(new RepEntrySave { alvo = e.alvo, valor = e.valor });
            return r;
        }
        private static List<ReputationService.RepEntry> FromSave(List<RepEntrySave> src)
        {
            var r = new List<ReputationService.RepEntry>();
            if (src == null) return r;
            foreach (var e in src) r.Add(new ReputationService.RepEntry { alvo = e.alvo, valor = e.valor });
            return r;
        }
    }
}
