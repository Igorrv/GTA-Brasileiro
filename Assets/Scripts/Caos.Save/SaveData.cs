using System;
using System.Collections.Generic;

namespace Caos.Save
{
    /// <summary>Snapshot serializável de todo o estado de jogo (JsonUtility-friendly).</summary>
    [Serializable]
    public class SaveData
    {
        // Player
        public float pFome, pSede, pEnergia, pSanidade, pSaude;
        // Economy
        public float eRs, eCaosCash, eIpc;
        // World
        public float wCaos;
        public int   wStars;
        public string wDistrict;
        public string wWeather;
        public float wHour;
        public int   wDay;
        // Reputation
        public List<RepEntrySave> repFaction;
        public List<RepEntrySave> repDistrict;
        // Missions
        public List<string> missionsCompleted;
        public List<string> missionsActive;

        public int saveVersion = 2;   // 2 = inclui Sede
        public string savedAt = "";
    }

    [Serializable]
    public struct RepEntrySave { public string alvo; public int valor; }

    /// <summary>
    /// Resumo de um slot, lido sem carregar o jogo — é o que o menu inicial mostra em cada cartão.
    /// </summary>
    public struct SaveSlotInfo
    {
        public int    slot;
        public bool   existe;
        public int    dia;
        public float  hora;
        public float  rs;
        public string bairro;
        public int    missoes;
        public int    estrelas;
        public string salvoEm;

        public string Resumo => existe
            ? $"Dia {dia} · {(int)hora:00}:{(int)((hora - (int)hora) * 60f):00} · R$ {rs:N2}\n{bairro} · {missoes} missões concluídas"
            : "Slot vazio";
    }
}
