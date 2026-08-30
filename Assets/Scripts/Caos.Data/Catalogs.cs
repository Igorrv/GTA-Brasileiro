using System.Collections.Generic;

namespace Caos.Data
{
    /// <summary>
    /// Catálogos em tempo de execução, carregados dos JSON em StreamingAssets/Data.
    /// Indexados por id para lookup O(1). Substituível por ScriptableObjects no editor (ver docs/12).
    ///
    /// Esta classe é só o <b>conteúdo</b>: quem lê arquivo é o <c>Caos.Content.CatalogLoader</c>, que
    /// mora em outro assembly justamente para que <c>Caos.Data</c> continue sem engine e testável.
    /// </summary>
    public sealed class GameCatalogs
    {
        public readonly List<VehicleDto>  Vehicles  = new List<VehicleDto>();
        public readonly List<FactionDto>  Factions  = new List<FactionDto>();
        public readonly List<DistrictDto> Districts = new List<DistrictDto>();
        public readonly List<ItemDto>     Items     = new List<ItemDto>();
        public readonly List<EventDto>    Events    = new List<EventDto>();
        public readonly List<MissionDto>  Missions  = new List<MissionDto>();
        public readonly List<DailyDto>    Dailies   = new List<DailyDto>();
        public readonly List<ShopDto>     Shops     = new List<ShopDto>();
        public readonly List<RadioStationDto> Radio = new List<RadioStationDto>();
        public readonly List<WorldDto>    Worlds    = new List<WorldDto>();
        public StreetNamesDto             Streets   = null;

        public readonly Dictionary<string, VehicleDto>  VehicleById  = new Dictionary<string, VehicleDto>();
        public readonly Dictionary<string, ItemDto>     ItemById     = new Dictionary<string, ItemDto>();
        public readonly Dictionary<string, EventDto>    EventById    = new Dictionary<string, EventDto>();
        public readonly Dictionary<string, MissionDto>  MissionById  = new Dictionary<string, MissionDto>();
        public readonly Dictionary<string, DailyDto>    DailyById    = new Dictionary<string, DailyDto>();
        public readonly Dictionary<string, DistrictDto> DistrictById = new Dictionary<string, DistrictDto>();
        public readonly Dictionary<string, ShopDto>     ShopById     = new Dictionary<string, ShopDto>();

        public void IndexAll()
        {
            Index(Vehicles, VehicleById);
            Index(Items, ItemById);
            Index(Events, EventById);
            Index(Missions, MissionById);
            Index(Dailies, DailyById);
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
            // diárias mínimas no Centro, para o app nunca abrir vazio mesmo sem o JSON
            c.Dailies.Add(new DailyDto
            {
                id = "D01", titulo = "Rota da Manhã", dador = "app_vaija",
                descricao = "Entrega rápida do VaiJá pelo Centro.",
                recompensaRs = 120f, recompensaXp = 80f, recompensaRep = new List<RepDelta>(),
                objetivos = new List<MissionObjectiveDto> { new MissionObjectiveDto { tipo = "levar", alvo = "encomenda", quantidade = 1, local = "Centro" } }
            });
            c.Dailies.Add(new DailyDto
            {
                id = "D02", titulo = "Volta no Calçadão", dador = "bia",
                descricao = "Passa no calçadão e confere o movimento.",
                recompensaRs = 60f, recompensaXp = 40f, recompensaRep = new List<RepDelta>(),
                objetivos = new List<MissionObjectiveDto> { new MissionObjectiveDto { tipo = "ir", alvo = "calcadao", quantidade = 1, local = "Centro" } }
            });
            c.IndexAll();
            return c;
        }
    }
}
