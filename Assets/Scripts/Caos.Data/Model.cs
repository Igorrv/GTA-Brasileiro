using System;
using System.Collections.Generic;

namespace Caos.Data
{
    // ===================== ENUMS CANÔNICOS (ver docs/00-biblia-do-mundo.md) =====================

    // Valores novos SEMPRE no fim: o save serializa por índice/nome e não pode quebrar.
    public enum FactionId   { Caminhoneiros, Milicia, Motoclube, FrentePopular, Camelos, Torcida, Fiscais, Radio }
    public enum DistrictId  { VistaAlegre, Centro, MonteVerde, SitioCapim, Belvedere, Itauna, Rodoviaria, Marginal, Cohab }
    public enum VehicleClass{ Popular, Esportivo, Caminhonete, Caminhao, Onibus, Moto, Bicicleta, Viatura, Taxi, App, Van, Emergencia, Rural }
    public enum WeatherState{ SolForte, SolLeve, Garoa, Chuva, Tempestade, Enchente, Neblina }
    public enum MissionType { Principal, Secundaria, Faccao, Diaria, GeradaIA }
    public enum JobType     { VaiJa, Motoboy, Entregador, Pedreiro, Frentista, Garcom, Contrabando }

    /// <summary>Silhueta usada pelo montador de veículos em runtime (docs/09 — estilo "primitivo estilizado").</summary>
    public enum BodyStyle   { Hatch, Sedan, Picape, Van, Onibus, Caminhao, Moto, Bike, Buggy, Trator }

    /// <summary>
    /// Delta de atributos aplicado por uma escolha de evento, conclusão de missão ou consequência.
    /// Campos em 0 = sem efeito. `rs` é em Reais (soft currency).
    /// </summary>
    [Serializable]
    public struct AttributeImpact
    {
        public float fome;      // saciedade (100 = cheio)
        public float sede;      // hidratação (100 = matou a sede)
        public float energia;
        public float sanidade;
        public float saude;     // HP
        public float rs;        // dinheiro soft
        public float caosCash;  // dinheiro premium
        public float caos;      // Nível de Caos global
        public int   stars;     // estrelas de procurado
        public List<RepDelta> rep; // reputação por facção/bairro

        public static AttributeImpact Zero => new AttributeImpact();
    }

    /// <summary>Alvo de reputação: nome do enum FactionId OU DistrictId.</summary>
    [Serializable]
    public struct RepDelta { public string alvo; public int delta; }

    // ===================== DTOs (JSON-serializáveis, JsonUtility-friendly) =====================

    [Serializable]
    public class VehicleDto
    {
        public string id;
        public string nome;
        public string classe;        // VehicleClass
        public float  massa;         // kg
        public float  potencia;      // cv
        public float  zeroACem;      // s
        public float  consumoKmPorL;
        public float  tanqueL;
        public float  preco;         // R$
        public int    dirigibilidade; // 1..5
        public string spawnBairro;   // DistrictId onde costuma spawnar

        // ---- apresentação / montagem em runtime (docs/09) ----
        public string carroceria;    // BodyStyle — silhueta montada pelo VehicleFactory
        public string corHex;        // cor de fábrica (#RRGGBB); tráfego sorteia variações
        public float  comprimento;   // m
        public float  largura;       // m
        public float  altura;        // m
        public float  velMaxKmh;     // teto de velocidade
        public string apelido;       // como o povo chama ("carroça", "escadinha", "brasilinha")
        public int    raridade;      // 1 = comum no trânsito ... 5 = raro
    }

    [Serializable]
    public class FactionDto
    {
        public string id;            // FactionId
        public string nome;
        public string corHex;
        public string lider;
        public string territorio;
    }

    [Serializable]
    public class DistrictDto
    {
        public string id;            // DistrictId
        public string nome;
        public string tipo;
        public float  probEventoBase; // 0..1 (ver docs/06 §H)

        // ---- geração da cidade (CityGenerator) ----
        public float  centroX;       // centro do bairro no mundo (m)
        public float  centroZ;
        public float  raio;          // alcance para detecção de bairro atual (m)
        public string corHex;        // cor dominante do bairro (fachadas/minimapa)
        public float  alturaMin;     // andares mín./máx. das construções
        public float  alturaMax;
        public int    policiamento;  // 0..5 — quanto a PM aparece por lá
        public string descricao;
    }

    [Serializable]
    public class ItemDto
    {
        public string id;
        public string nome;
        public float  preco;         // R$
        public float  fome;
        public float  sede;          // hidratação
        public float  energia;
        public float  sanidade;
        public float  saude;         // cura (kit, remédio)
        public string tipo;          // comida, bebida, remedio, servico, utilidade
        public string descricao;
    }

    /// <summary>
    /// Estabelecimento de rua (padaria, boteco, mercadinho, lotérica...). O <c>CityGenerator</c>
    /// sorteia a posição num lote do bairro; o JSON define o que ele é e o que vende.
    /// </summary>
    [Serializable]
    public class ShopDto
    {
        public string id;
        public string nome;
        public string tipo;          // TipoInteracao (Padaria, Boteco, Mercadinho, Loterica, Farmacia, Posto, Oficina, Trabalho, Barraca)
        public string bairro;        // DistrictId preferido ("" = qualquer)
        public string corHex;
        public float  precoBase;     // posto: R$/L · oficina: R$ do reparo · trabalho: irrelevante
        public float  pagamento;     // trabalho: R$ por turno
        public List<string> itens;   // ids de ItemDto vendidos
        public string bordao;        // fala do balcão ("Tá quentinho, saiu agora!")
    }

    /// <summary>Faixa tocada por uma estação (título/artista fictícios + semente da trilha procedural).</summary>
    [Serializable]
    public class RadioTrackDto
    {
        public string titulo;
        public string artista;
        public float  bpm;
        public int    semente;
    }

    [Serializable]
    public class RadioStationDto
    {
        public string id;
        public string nome;          // "Caos FM 92,5"
        public string genero;        // funk, sertanejo, forro, mpb, gospel, rock, noticias
        public string slogan;
        public string corHex;
        public float  bpm;
        public List<RadioTrackDto> faixas;
        public List<string> locucoes; // falas do locutor entre faixas
    }

    /// <summary>
    /// Um mundo do hub de servidores. A <b>semente</b> é a identidade: ela determina a cidade inteira,
    /// então entrar no mesmo mundo é partir da mesma semente. <c>endereco</c> vazio significa mundo
    /// local (roda na sua máquina); quando o netcode entrar, ele guarda o host:porta do servidor e a
    /// mesma tela passa a listar mundos remotos sem mudar de formato.
    /// </summary>
    [Serializable]
    public class WorldDto
    {
        public string id;
        public string nome;
        public int    semente;
        public string lema;
        public string regiao;
        public int    lotacaoMax;
        public int    dificuldade;   // 1..5 — afeta buraco, policiamento e preço
        public string endereco;      // "" = local · "host:porta" = servidor remoto

        public bool EhLocal => string.IsNullOrEmpty(endereco);
    }

    /// <summary>Nomes de logradouro sorteados pelo gerador de cidade (placas + HUD).</summary>
    [Serializable]
    public class StreetNamesDto
    {
        public List<string> avenidas;
        public List<string> ruas;
        public List<string> vielas;
    }

    [Serializable]
    public class EventOptionDto
    {
        public string rotulo;
        public AttributeImpact impacto;
    }

    [Serializable]
    public class EventDto
    {
        public string id;            // ex.: "E01"
        public string nome;
        public string descricao;
        public List<string> bairros; // DistrictId (vazio = qualquer um)
        public List<string> horarios;// ["noite","dia","madrugada"] (vazio = qualquer)
        public List<string> climas;  // WeatherState (vazio = qualquer)
        public float peso;           // peso base de spawn
        public List<EventOptionDto> opcoes;
    }

    [Serializable]
    public class MissionObjectiveDto
    {
        public string tipo;          // ir, coletar, levar, falar, minigame, baliza, seguir
        public string alvo;
        public int    quantidade;
        public string local;         // DistrictId / marcador
    }

    [Serializable]
    public class MissionDto
    {
        public string id;            // ex.: "M01"
        public string tipo;          // MissionType
        public string titulo;
        public string dador;         // NPC id
        public float  recompensaRs;
        public float  recompensaXp;
        public string faccao;        // FactionId (opcional)
        public List<string> preRequisitos; // ids de missões
        public List<MissionObjectiveDto> objetivos;
    }
}
