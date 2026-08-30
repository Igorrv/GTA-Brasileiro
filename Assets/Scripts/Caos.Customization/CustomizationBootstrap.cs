using System.Collections.Generic;
using Caos.Core;
using Caos.Simulation;
using UnityEngine;

namespace Caos.Customization
{
    /// <summary>
    /// Auto-bootstrap da customização (mesmo padrão do GameBootstrapper/WorldBuilder): sobe em
    /// qualquer cena, sem editar nenhum arquivo de boot existente.
    /// </summary>
    public static class CustomizationBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Object.FindObjectOfType<CustomizationController>() != null) return;
            var go = new GameObject("[Customizacao]");
            go.AddComponent<CustomizationController>();
            go.AddComponent<CustomizationUI>();
        }
    }

    /// <summary>
    /// Liga o visual salvo ao protagonista: espera a partida começar e o WorldBuilder montar o
    /// player, então aplica o loadout do slot. Depois disso mantém as peças extras (<c>Look_*</c>)
    /// com a mesma visibilidade do corpo — o PlayerVehicleLink esconde o boneco ao dirigir e não
    /// conhece as peças criadas depois do boot, então sem isso uma saia ficaria flutuando no carro.
    /// </summary>
    public sealed class CustomizationController : MonoBehaviour
    {
        private CharacterRig   _rig;
        private Transform      _player;
        private List<Renderer> _extras = new List<Renderer>();
        private Renderer       _referenciaCorpo;             // "Peito": termômetro de visibilidade do boneco
        private bool           _aplicado;
        private float          _proxSincronia;

        /// <summary>Rig do protagonista (null até o mundo montar). Usado pela tela de personagem.</summary>
        public CharacterRig RigDoJogador => _rig;
        public Transform    Jogador      => _player;
        public bool         VisualAplicado => _aplicado;

        private void Start()
        {
            CustomizationService.Iniciar(this);
        }

        private void Update()
        {
            if (!_aplicado)
            {
                TentarAplicarNoJogador();
                return;
            }
            SincronizarVisibilidade();
        }

        private void TentarAplicarNoJogador()
        {
            if (!CustomizationService.Pronto) return;
            if (!GameSession.Iniciado) return;

            var go = GameObject.Find("Player");
            if (go == null) return;
            var rig = go.GetComponentInChildren<CharacterRig>();
            if (rig == null) return;

            _rig    = rig;
            _player = go.transform;
            CustomizationService.CarregarDoSlot();
            Reaplicar();

            var peito = rig.Tronco != null ? rig.Tronco.Find("Peito") : null;
            _referenciaCorpo = peito != null ? peito.GetComponent<Renderer>() : null;

            _aplicado = true;
            Debug.Log("[Cosméticos] Visual do slot " + GameSession.Slot + " aplicado ao protagonista.");
        }

        /// <summary>Reaplica o visual atual (pós-troca na tela de personagem).</summary>
        public void Reaplicar()
        {
            _extras = CustomizationService.AplicarEm(_rig);
        }

        /// <summary>Aplica um rascunho sem salvar (prévia ao vivo na tela de personagem).</summary>
        public void Prever(CosmeticLoadout rascunho)
        {
            _extras = CustomizationService.PreverEm(_rig, rascunho);
        }

        /// <summary>
        /// Espelha a visibilidade das peças extras na do corpo. Barato: roda a cada 0,15 s e só
        /// toca nos renderers quando o estado muda.
        /// </summary>
        private void SincronizarVisibilidade()
        {
            // unscaled: a tela de personagem e a pausa congelam Time.time, e a sincronia não pode
            // congelar junto
            if (Time.unscaledTime < _proxSincronia) return;
            _proxSincronia = Time.unscaledTime + 0.15f;
            if (_referenciaCorpo == null || _extras.Count == 0) return;

            bool visivel = _referenciaCorpo.enabled;
            for (int i = 0; i < _extras.Count; i++)
                if (_extras[i] != null && _extras[i].enabled != visivel)
                    _extras[i].enabled = visivel;
        }
    }
}
