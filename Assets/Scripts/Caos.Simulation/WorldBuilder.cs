using System.Collections.Generic;
using System.IO;
using Caos.Core;
using Caos.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Caos.Simulation
{
    /// <summary>
    /// Ponteiro global para a cidade gerada — sistemas (tráfego, pedestres, polícia, HUD, missões)
    /// leem daqui em vez de se procurarem por FindObjectOfType.
    /// </summary>
    public static class CityRuntime
    {
        public static CityLayout    Layout;
        public static CityGenerator Generator;
        public static bool Pronta => Layout != null && Generator != null;
    }

    /// <summary>
    /// Monta a cena 3D em runtime assim que os catálogos estão prontos, de modo que o Play funcione
    /// numa cena vazia — sem autoria manual de prefab/cena.
    ///
    /// Ordem: cidade (<see cref="CityGenerator"/>) → céu/sol → câmera → protagonista → veículo do
    /// catálogo → frota estacionada → comércio/scanner → HUD/minimapa/rádio → tráfego, pedestres,
    /// polícia, ciclo de vida, áudio, missões e menus.
    /// </summary>
    public class WorldBuilder : MonoBehaviour
    {
        private const string kDefaultVehicle  = "uno_escada";
        private const string kFallbackVehicle = "fusca_besouro";
        private const int    kGridLines       = 13;   // 13 vias por eixo ≈ 960 x 960 m de cidade
        private const int    kCarrosParados   = 26;

        private bool _built;
        private float _waitStart = -1f;
        private GameObject _loadingOverlay;
        private Text _loadingText;
        private string _nomeVeiculoJogador = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<WorldBuilder>() != null) return;
            var go = new GameObject("[WorldBuilder]");
            go.AddComponent<WorldBuilder>();
            Diag("WorldBuilder injetado — aguardando catálogos ficarem prontos.");
        }

        private void Awake()
        {
            CityRuntime.Layout    = null;
            CityRuntime.Generator = null;
            CityPalette.Clear();
        }

        private void Update()
        {
            if (_built) return;

            // o menu inicial manda: sem slot escolhido, nada de cidade
            if (!GameSession.Iniciado) return;
            if (_loadingOverlay == null) ShowLoadingOverlay();

            if (_waitStart < 0f) _waitStart = Time.time;

            bool have = ServiceLocator.TryGet<GameCatalogs>(out var catalogs);

            if (!have)
            {
                // Resgate: se o GameManager não registrou catálogos em ~5s, cria fallback p/ o mundo abrir de qualquer jeito.
                if (Time.time - _waitStart < 5f) return;
                catalogs = GameCatalogs.CreateFallback();
                try { ServiceLocator.Register(catalogs); } catch { }
                Diag("RESGATE: catálogos de fallback criados (GameManager não registrou a tempo).");
            }
            else if (catalogs.Vehicles.Count == 0)
            {
                catalogs = GameCatalogs.CreateFallback();
                try { ServiceLocator.Register(catalogs); } catch { }
                Diag("Catálogos presentes porém vazios — usando fallback.");
            }

            _built = true;
            Diag("Catálogos OK. Construindo São Genésio do Caos...");
            Build(catalogs);
        }

        // -------------------------------------------------------------- feedback de boot
        private void ShowLoadingOverlay()
        {
            var cgo = new GameObject("BootLoader", typeof(Canvas));
            var c = cgo.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 100;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var bgGo = new GameObject("Fundo", typeof(RectTransform));
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.SetParent(cgo.transform, false);
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 1f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var tgo = new GameObject("Titulo", typeof(RectTransform));
            var rt = (RectTransform)tgo.transform;
            rt.SetParent(cgo.transform, false);
            rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 40f);
            rt.sizeDelta = new Vector2(0f, 120f);
            var t = tgo.AddComponent<Text>();
            t.text = "CIDADE DO CAOS";
            t.font = font; t.fontSize = 84; t.fontStyle = FontStyle.Bold;
            t.color = new Color(0.98f, 0.82f, 0.20f); t.alignment = TextAnchor.MiddleCenter;

            var sgo = new GameObject("Status", typeof(RectTransform));
            var srt = (RectTransform)sgo.transform;
            srt.SetParent(cgo.transform, false);
            srt.anchorMin = new Vector2(0f, 0.5f); srt.anchorMax = new Vector2(1f, 0.5f);
            srt.anchoredPosition = new Vector2(0f, -50f);
            srt.sizeDelta = new Vector2(0f, 60f);
            _loadingText = sgo.AddComponent<Text>();
            _loadingText.text = "Levantando São Genésio do Caos...";
            _loadingText.font = font; _loadingText.fontSize = 30;
            _loadingText.color = new Color(0.85f, 0.85f, 0.85f); _loadingText.alignment = TextAnchor.MiddleCenter;

            _loadingOverlay = cgo;
        }

        /// <summary>Log no Console + em arquivo (persistentDataPath/caos_boot.txt) p/ diagnóstico pós-Play.</summary>
        private static void Diag(string msg)
        {
            Debug.Log("[World] " + msg);
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "caos_boot.txt");
                File.AppendAllText(path, System.DateTime.Now.ToString("HH:mm:ss") + "  " + msg + "\n");
            }
            catch { /* ignora falha de I/O no log */ }
        }

        private void Build(GameCatalogs catalogs)
        {
            if (_loadingText != null) _loadingText.text = "Levantando os quarteirões de São Genésio...";

            // ---- cidade ----
            var layout = new CityLayout(kGridLines, catalogs);
            var cityGo = new GameObject("[Cidade]");
            var gen    = new CityGenerator(layout, catalogs, cityGo.transform);
            gen.Build();
            CityRuntime.Layout    = layout;
            CityRuntime.Generator = gen;
            Diag($"Cidade gerada: {layout.N}x{layout.N} vias, {gen.Shops.Count} estabelecimentos, " +
                 $"{gen.ParkingSpots.Count} vagas, {gen.Luminarias.Count} postes, {gen.Buracos} buracos, " +
                 $"{PlayerActions.Assentos.Count} assentos, " +
                 $"{cityGo.GetComponentsInChildren<MeshRenderer>(true).Length} peças (já combinadas por quarteirão).");

            BuildCeu();
            var cam = BuildCamera();

            var playerT = BuildPlayer(gen.PlayerSpawn);
            cam.Bind(playerT);

            BuildVehicle(catalogs, gen, playerT.position, out var vehicleT, out var vehicle, out var health);
            BuildFrotaParada(catalogs, gen, playerT.position);

            var link = BuildLink(playerT, playerT.GetComponent<PlayerController>(), vehicleT, vehicle, cam);
            cam.Contexto(link, vehicle);   // a câmera muda de enquadramento dentro do carro

            var interactables = BuildZones(gen, layout);
            var scanner = BuildScanner(playerT, link, vehicle, health, interactables);

            var radio = BuildRadio(catalogs, link);
            BuildHud(link, vehicle, health, scanner, playerT, radio);
            BuildCelular(playerT, radio);
            BuildPainelDoCarro(link, vehicle, health).DefinirModelo(_nomeVeiculoJogador);
            BuildMinimapa(playerT);
            BuildTraffic(playerT, catalogs);
            BuildCrime();
            var police = BuildPolice(playerT, link, vehicleT, catalogs);
            BuildPeds(playerT);
            BuildTouch();
            BuildLifecycle(playerT, vehicleT, police);
            BuildPause();

            var audio = BuildAudio(vehicle, link);
            BuildMissions(playerT, link, vehicleT, audio);

            Debug.Log("[World] São Genésio do Caos no ar: 9 bairros, malha viária, comércio, tráfego, pedestres, polícia, rádio, minimapa e missões.");
            Debug.Log("[World] WASD/joystick · Shift correr · E entrar/sair · Espaço freio · R abastecer · F usar/comprar · Q/Z rádio · Tab pausa · botão direito orbita.");
            if (_loadingOverlay != null) Destroy(_loadingOverlay);
            Diag("Mundo montado com sucesso.");
        }

        // -------------------------------------------------------------- céu, sol e névoa
        private void BuildCeu()
        {
            var go = new GameObject("Sol");
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            go.AddComponent<DayNightLighting>();

            // névoa dá profundidade e esconde o fim do mundo — barato e faz muita diferença
            RenderSettings.fog        = true;
            RenderSettings.fogMode    = FogMode.Linear;
            RenderSettings.fogStartDistance = 180f;
            RenderSettings.fogEndDistance   = 620f;
            RenderSettings.fogColor   = new Color(0.68f, 0.75f, 0.84f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        }

        // -------------------------------------------------------------- câmera
        private ThirdPersonCamera BuildCamera()
        {
            var go = new GameObject("MainCamera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.7f, 0.9f);
            cam.nearClipPlane = 0.15f;
            cam.farClipPlane = 700f;
            if (FindObjectOfType<AudioListener>() == null) go.AddComponent<AudioListener>();
            return go.AddComponent<ThirdPersonCamera>();
        }

        // -------------------------------------------------------------- protagonista
        private Transform BuildPlayer(Vector3 spawn)
        {
            var go = new GameObject("Player");
            go.transform.position = spawn;

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.9f; cc.radius = 0.4f; cc.center = Vector3.zero;
            cc.slopeLimit = 55f; cc.stepOffset = 0.45f; cc.minMoveDistance = 0.001f;

            // rig articulado (pés em −0,95) + passada procedural
            var rig = CharacterRig.Construir(go.transform,
                camisa: new Color(0.95f, 0.82f, 0.18f),   // amarelinha
                calca:  new Color(0.16f, 0.30f, 0.62f),
                pele:   new Color(0.62f, 0.45f, 0.33f),
                bone:   new Color(0.15f, 0.45f, 0.28f));
            go.AddComponent<CharacterAnimator>().Init(rig);

            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerActions>();     // agachar, sentar, comer/beber
            return go.transform;
        }

        // -------------------------------------------------------------- veículo do jogador
        private void BuildVehicle(GameCatalogs catalogs, CityGenerator gen, Vector3 perto,
                                  out Transform t, out VehicleController vc, out VehicleHealth health)
        {
            var dto = PickVehicle(catalogs);
            var go = new GameObject($"Veiculo_{(dto != null ? dto.id : "padrao")}");
            go.transform.position = VagaMaisProxima(gen, perto, 12f) + new Vector3(0f, 0.7f, 0f);

            VehicleFactory.BuildBody(go.transform, dto, VehicleFactory.CorDe(dto), rodasVisuais: false);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = dto != null ? Mathf.Max(400f, dto.massa) : 1200f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            vc = go.AddComponent<VehicleController>();
            vc.ConfigureFromCatalog(dto);
            health = go.AddComponent<VehicleHealth>();

            t = go.transform;
            _nomeVeiculoJogador = dto != null ? dto.nome : "Veículo";
            Diag($"Veículo do jogador: {_nomeVeiculoJogador}.");
        }

        private VehicleDto PickVehicle(GameCatalogs catalogs)
        {
            if (catalogs.VehicleById.TryGetValue(kDefaultVehicle, out var dto))  return dto;
            if (catalogs.VehicleById.TryGetValue(kFallbackVehicle, out dto))    return dto;
            return catalogs.Vehicles.Count > 0 ? catalogs.Vehicles[0] : null;
        }

        private Vector3 VagaMaisProxima(CityGenerator gen, Vector3 de, float minDist)
        {
            Vector3 best = de + new Vector3(6f, 0f, 0f);
            float bestScore = float.MaxValue;
            foreach (var v in gen.ParkingSpots)
            {
                float d = Vector3.Distance(v, de);
                if (d < minDist) continue;
                if (d < bestScore) { bestScore = d; best = v; }
            }
            return best;
        }

        /// <summary>Carros parados no meio-fio: é isso que faz a rua parecer habitada.</summary>
        private void BuildFrotaParada(GameCatalogs catalogs, CityGenerator gen, Vector3 playerPos)
        {
            var root = new GameObject("[FrotaParada]");
            int criados = 0;
            var vagas = new List<Vector3>(gen.ParkingSpots);

            for (int i = 0; i < vagas.Count && criados < kCarrosParados; i++)
            {
                Vector3 vaga = vagas[Random.Range(0, vagas.Count)];
                if (Vector3.Distance(vaga, playerPos) < 25f) continue;

                var dto = VehicleFactory.SortearParaTrafego(catalogs, "Viatura", "Emergencia", "Bicicleta");
                if (dto == null) break;

                var go = new GameObject("Parado_" + dto.id);
                go.transform.SetParent(root.transform, false);
                go.transform.position = vaga;
                go.transform.rotation = Quaternion.Euler(0f, Random.value < 0.5f ? 0f : 180f, 0f);
                VehicleFactory.BuildBody(go.transform, dto, CorDeRua(dto), rodasVisuais: true);
                criados++;
            }
            Diag($"Frota estacionada: {criados} veículos no meio-fio.");
        }

        /// <summary>Cor de fábrica na maioria; de vez em quando, uma repintura de rua.</summary>
        private static Color CorDeRua(VehicleDto dto)
            => Random.value < 0.6f ? VehicleFactory.CorDe(dto) : CityPalette.CorViva();

        // -------------------------------------------------------------- link
        private PlayerVehicleLink BuildLink(Transform player, PlayerController pc, Transform vehicle, VehicleController vc, ThirdPersonCamera cam)
        {
            var go = new GameObject("PlayerVehicleLink");
            var link = go.AddComponent<PlayerVehicleLink>();
            link.Configure(player, pc, vehicle, vc, cam);
            return link;
        }

        // -------------------------------------------------------------- comércio
        private List<Interactable> BuildZones(CityGenerator gen, CityLayout layout)
        {
            var list = new List<Interactable>(gen.Shops);
            if (list.Count > 0) return list;

            // fallback: catálogo de lojas ausente — garante o loop econômico mínimo
            list.Add(BuildZone("Posto do Zé", TipoInteracao.Posto, layout.DistrictCenter("Centro") + new Vector3(30f, 0f, 20f), new Color(0.95f, 0.75f, 0.10f), 6.5f));
            list.Add(BuildZone("Oficina do Tio Bié", TipoInteracao.Oficina, layout.DistrictCenter("Centro") + new Vector3(-40f, 0f, -30f), new Color(0.30f, 0.30f, 0.30f), 140f));
            list.Add(BuildZone("VaiJá (entregas)", TipoInteracao.Trabalho, layout.DistrictCenter("Centro") + new Vector3(10f, 0f, -50f), new Color(0.20f, 0.55f, 0.95f)));
            return list;
        }

        private Interactable BuildZone(string name, TipoInteracao tipo, Vector3 pos, Color color, float precoBase = 0f)
        {
            var go = new GameObject(name);
            go.transform.position = pos;

            CityPalette.Box(go.transform, "Piso", new Vector3(0f, 0.15f, 0f), new Vector3(7f, 0.3f, 7f), CityPalette.Mat(color));
            CityPalette.Cyl(go.transform, "Totem", new Vector3(0f, 1.6f, 0f), 0.3f, 3.2f, CityPalette.Mat(Color.white), collide: true);
            CityPalette.Label(go.transform, name, new Vector3(0f, 3.6f, 0f), color, 0.45f);

            var it = go.AddComponent<Interactable>();
            it.tipo = tipo;
            it.rotulo = name;
            it.cor = color;
            it.radius = 5.5f;
            it.precoBase = precoBase;
            if (tipo == TipoInteracao.Trabalho)
            {
                it.pagamento = 90f;
                it.energiaCost = 12f;
                it.fomeCost = 8f;
                it.horasTrabalho = 2f;
            }
            return it;
        }

        // -------------------------------------------------------------- scanner
        private InteractionScanner BuildScanner(Transform player, PlayerVehicleLink link, VehicleController vehicle, VehicleHealth health, List<Interactable> list)
        {
            var go = new GameObject("[Scanner]");
            var scanner = go.AddComponent<InteractionScanner>();
            scanner.Init(player, link, vehicle, health, list);
            return scanner;
        }

        // -------------------------------------------------------------- HUD / minimapa / rádio
        private HudController BuildHud(PlayerVehicleLink link, VehicleController vehicle, VehicleHealth health,
                                       InteractionScanner scanner, Transform player, RadioSystem radio)
        {
            var go = new GameObject("[HUD]");
            var hud = go.AddComponent<HudController>();
            hud.Init(link, vehicle, health, scanner, player, radio);
            return hud;
        }

        private void BuildCelular(Transform player, RadioSystem radio)
        {
            var go = new GameObject("[Celular]");
            go.AddComponent<PhoneUI>().Init(player, radio);
        }

        private DashboardUI BuildPainelDoCarro(PlayerVehicleLink link, VehicleController vehicle, VehicleHealth health)
        {
            var go = new GameObject("[PainelDoCarro]");
            var painel = go.AddComponent<DashboardUI>();
            painel.Init(link, vehicle, health);
            return painel;
        }

        private void BuildMinimapa(Transform player)
        {
            var go = new GameObject("[Minimapa]");
            go.AddComponent<MinimapSystem>().Init(player);
        }

        private RadioSystem BuildRadio(GameCatalogs catalogs, PlayerVehicleLink link)
        {
            var go = new GameObject("[Radio]");
            var radio = go.AddComponent<RadioSystem>();
            radio.Init(catalogs, link);
            return radio;
        }

        // -------------------------------------------------------------- tráfego
        private void BuildTraffic(Transform player, GameCatalogs catalogs)
        {
            var go = new GameObject("[Trafego]");
            var traffic = go.AddComponent<TrafficSystem>();
            traffic.Init(player, catalogs);
        }

        // -------------------------------------------------------------- crime/polícia
        private void BuildCrime()
        {
            var go = new GameObject("[Crime]");
            go.AddComponent<CrimeSystem>();
        }

        private PoliceSystem BuildPolice(Transform player, PlayerVehicleLink link, Transform vehicle, GameCatalogs catalogs)
        {
            var go = new GameObject("[Policia]");
            var police = go.AddComponent<PoliceSystem>();
            police.Init(player, link, vehicle, catalogs);
            return police;
        }

        // -------------------------------------------------------------- pedestres
        private void BuildPeds(Transform player)
        {
            var go = new GameObject("[Pedestres]");
            go.AddComponent<PedestrianSystem>().Init(player);
        }

        // -------------------------------------------------------------- touch
        private void BuildTouch()
        {
            var go = new GameObject("[Touch]");
            go.AddComponent<TouchControls>();
        }

        // -------------------------------------------------------------- ciclo de vida (busted/wasted)
        private void BuildLifecycle(Transform player, Transform vehicle, PoliceSystem police)
        {
            var go = new GameObject("[CicloDeVida]");
            go.AddComponent<PlayerLifecycle>().Init(player, vehicle, police);
        }

        // -------------------------------------------------------------- pausa
        private void BuildPause()
        {
            var go = new GameObject("[Pausa]");
            go.AddComponent<PauseMenu>();
        }

        // -------------------------------------------------------------- áudio procedural
        private AudioManager BuildAudio(VehicleController vehicle, PlayerVehicleLink link)
        {
            var go = new GameObject("[Audio]");
            var am = go.AddComponent<AudioManager>();
            am.Init(vehicle, link);
            return am;
        }

        // -------------------------------------------------------------- missões (painel + beacon)
        private void BuildMissions(Transform player, PlayerVehicleLink link, Transform vehicle, AudioManager audio)
        {
            var go = new GameObject("[Missoes]");
            go.AddComponent<MissionTracker>().Init(player, link, vehicle, audio);
        }
    }
}
