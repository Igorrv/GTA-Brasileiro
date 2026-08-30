using System.Collections;
using System.Collections.Generic;
using Caos.Content;
using Caos.Core;
using Caos.Data;
using Caos.Gameplay;
using Caos.Save;
using Caos.World;
using UnityEngine;

namespace Caos.Bootstrap
{
    /// <summary>
    /// Composition root: cria/os serviços, carrega catálogos e save, registra no <see cref="ServiceLocator"/>
    /// e roda o tick dos sistemas (<see cref="ITickable"/>) na ordem de <see cref="ITickable.Order"/>.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ---- serviços públicos (UI/outras camadas acessam via Instance ou ServiceLocator) ----
        public PlayerAttributes  Attributes { get; private set; }
        public EconomyService    Economy    { get; private set; }
        public ExperienceService Experience { get; private set; }
        public ReputationService Reputation { get; private set; }
        public WorldStateService World      { get; private set; }
        public TimeOfDayService  Time       { get; private set; }
        public MissionService    Missions   { get; private set; }
        public DailyMissionService Dailies  { get; private set; }
        public ImpactResolver    Impact     { get; private set; }
        public EventSystem       Events     { get; private set; }
        public GameCatalogs      Catalogs   { get; private set; }
        public GameStateMachine  Fsm        { get; private set; }
        public bool              Ready      { get; private set; }

        private readonly List<ITickable> _tickables = new List<ITickable>();
        private float _autosaveAccum;
        private const float kAutosaveInterval = 60f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Fsm = new GameStateMachine();
            Fsm.ChangeState(new BootState());
        }

        private void Start() => StartCoroutine(Boot());

        private IEnumerator Boot()
        {
            CaosLog.Info("[GameManager] Boot: criando serviços base...");

            // 1) serviços que não dependem de catálogos
            Time       = new TimeOfDayService();
            World      = new WorldStateService();
            Attributes = new PlayerAttributes();
            Economy    = new EconomyService(Time);
            Experience = new ExperienceService();
            Reputation = new ReputationService();

            // XP das missões: o valor já existia no catálogo, mas não era creditado a ninguém.
            // Método nomeado (e não lambda) para que dê para cancelar a assinatura no OnDestroy — uma
            // closure nova a cada boot nunca casa com a lista e o barramento acumularia um handler
            // morto por sessão.
            EventBus<MissaoConcluida>.Subscribe(CreditarXpDaMissao);

            // 2) registra os que já existem (permite UI conectar cedo)
            RegisterServices();

            // 3) carrega catálogos (StreamingAssets/Data/*.json)
            CaosLog.Info("[GameManager] Carregando catálogos...");
            bool catalogsDone = false;
            CatalogLoader.LoadAsync(this, catalogs => { Catalogs = catalogs; catalogsDone = true; });
            yield return new WaitUntil(() => catalogsDone);

            // 3.5) segurança: se os catálogos não carregaram (JSON ausente/inválido), usa fallback p/ o mundo abrir.
            if (Catalogs == null || Catalogs.Vehicles.Count == 0)
            {
                Catalogs = GameCatalogs.CreateFallback();
                CaosLog.Aviso("[GameManager] Catálogos vazios/falha ao carregar — usando FALLBACK. O mundo abre mesmo assim.");
            }

            // 4) serviços que dependem de catálogos
            Impact   = new ImpactResolver(Attributes, Economy, Reputation, World);
            Events   = new EventSystem(World, Time, Catalogs, Impact);
            Missions = new MissionService(Catalogs, Economy);
            Dailies  = new DailyMissionService(Catalogs, Economy, Experience, Reputation, Impact, Time);

            // 5) espera o menu inicial dizer em qual slot vamos jogar
            CaosLog.Info("[GameManager] Aguardando escolha de slot no menu inicial...");
            yield return new WaitUntil(() => GameSession.Iniciado);
            SaveSystem.SlotAtual = GameSession.Slot;

            // 5.1) carrega save do slot (se houver e se não for jogo novo) e hidrata serviços
            var save = GameSession.NovoJogo ? null : SaveSystem.Load(GameSession.Slot);
            if (save != null)
            {
                SaveSystem.Apply(save, Attributes, Economy, Reputation, World, Time, Missions, Experience, Dailies);
                CaosLog.Info($"[GameManager] Slot {GameSession.Slot}: save restaurado.");
            }
            else
            {
                CaosLog.Info($"[GameManager] Slot {GameSession.Slot}: começando novo jogo.");
            }

            // 6) re-registra (agora inclui todos) e monta a lista de tickables
            RegisterServices();
            BuildTickables();

            // 7) ao jogo
            Fsm.ChangeState(new PlayingState());
            Ready = true;
            CaosLog.Info("[GameManager] Pronto. Mundo rodando. (Console mostra ticks/eventos.)");
            Attributes.PublishSnapshot();
            Economy.Add(0, 0); // força publicar estado inicial de dinheiro
        }

        private void Update()
        {
            if (!Ready) return;
            if (!(Fsm.Current is PlayingState)) return;

            // UnityEngine.Time explicitamente qualificado (a propriedade `Time` é o TimeOfDayService)
            float dt = UnityEngine.Time.deltaTime;
            for (int i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick(dt);

            // autosave
            _autosaveAccum += dt;
            if (_autosaveAccum >= kAutosaveInterval)
            {
                _autosaveAccum = 0f;
                SaveSystem.Capture(Attributes, Economy, Reputation, World, Time, Missions, Experience, Dailies);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && Ready)
                SaveSystem.Capture(Attributes, Economy, Reputation, World, Time, Missions, Experience, Dailies);
        }

        private void CreditarXpDaMissao(MissaoConcluida e)
        {
            if (Experience != null) Experience.Adicionar(e.xp, "missão " + e.id);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;   // duplicata sendo descartada no Awake: não é dona de nada
            EventBus<MissaoConcluida>.Unsubscribe(CreditarXpDaMissao);
            Instance = null;
        }

        /// <summary>
        /// Publica os serviços já criados. <b>Não limpa a tabela</b>: o registro é compartilhado entre
        /// assemblies (a Simulation registra o catálogo de resgate quando o boot demora), e um Reset no
        /// meio do boot apagaria o que a outra camada acabou de pôr lá. Quem zera é o
        /// <see cref="CaosRuntime.Reiniciar"/>, uma única vez, antes de qualquer cena.
        /// </summary>
        private void RegisterServices()
        {
            if (Time != null)       ServiceLocator.Register(Time);
            if (World != null)      ServiceLocator.Register(World);
            if (Attributes != null) ServiceLocator.Register(Attributes);
            if (Economy != null)    ServiceLocator.Register(Economy);
            if (Experience != null) ServiceLocator.Register(Experience);
            if (Reputation != null) ServiceLocator.Register(Reputation);
            if (Catalogs != null)   ServiceLocator.Register(Catalogs);
            if (Impact != null)     ServiceLocator.Register(Impact);
            if (Events != null)     ServiceLocator.Register(Events);
            if (Missions != null)   ServiceLocator.Register(Missions);
            if (Dailies != null)    ServiceLocator.Register(Dailies);
        }

        private void BuildTickables()
        {
            _tickables.Clear();
            _tickables.Add(Time);
            _tickables.Add(World);
            _tickables.Add(Economy);
            _tickables.Add(Attributes);
            if (Events != null) _tickables.Add(Events);
            if (Dailies != null) _tickables.Add(Dailies);
            _tickables.Sort((a, b) => a.Order.CompareTo(b.Order));
        }
    }
}
