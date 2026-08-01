using System;
using System.Collections.Generic;
using Caos.Data;
using UnityEngine;

namespace Caos.Simulation
{
    /// <summary>
    /// Ações corporais do protagonista, separadas da locomoção: <b>agachar</b>, <b>sentar</b> em banco
    /// de praça/ponto/coreto e <b>comer ou beber</b> com o gesto de levar à boca.
    ///
    /// A regra é que a ação tem <b>duração e consequência</b>: comer trava o passo por um instante e só
    /// então aplica o efeito do item; sentar recupera Energia enquanto você fica; agachar diminui a
    /// silhueta e a velocidade. Nada disso é enfeite — o <see cref="InteractionScanner"/> espera o
    /// gesto terminar antes de creditar o item, e o <see cref="PlayerController"/> lê o estado para
    /// mudar a altura do CharacterController e a velocidade.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerActions : MonoBehaviour
    {
        public enum Estado { Livre, Agachado, Sentado, Consumindo }

        private const float kRaioAssento   = 2.6f;
        private const float kAlturaEmPe    = 1.9f;
        private const float kAlturaAgachado= 1.15f;

        [SerializeField] private float duracaoConsumo = 1.4f;

        private CharacterController _cc;
        private CharacterAnimator   _anim;
        private PlayerController    _controller;

        private Estado    _estado = Estado.Livre;
        private Transform _assento;
        private Vector3   _posAntesDeSentar;
        private float     _consumoAte;
        private Action    _aoTerminarConsumo;

        /// <summary>Assentos registrados pela cidade (bancos de praça, ponto de ônibus, coreto).</summary>
        public static readonly List<Transform> Assentos = new List<Transform>();

        public Estado EstadoAtual => _estado;
        public bool   Agachado    => _estado == Estado.Agachado;
        public bool   Sentado     => _estado == Estado.Sentado;
        public bool   Ocupado     => _estado == Estado.Sentado || _estado == Estado.Consumindo;

        /// <summary>Texto de dica que o HUD mostra (ex.: "[G] sentar no banco").</summary>
        public string Dica { get; private set; } = "";

        private void Awake()
        {
            _cc         = GetComponent<CharacterController>();
            _anim       = GetComponent<CharacterAnimator>();
            _controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            AtualizarDica();

            if (_estado == Estado.Consumindo)
            {
                if (Time.time >= _consumoAte) TerminarConsumo();
                AplicarPose();
                return;
            }

            if (GameInput.Sit)
            {
                if (_estado == Estado.Sentado) Levantar();
                else Sentar(AssentoMaisProximo());
            }

            // sair do banco ao tentar andar
            if (_estado == Estado.Sentado && GameInput.Move.sqrMagnitude > 0.2f) Levantar();

            if (_estado != Estado.Sentado)
                _estado = GameInput.Crouch ? Estado.Agachado : Estado.Livre;

            AplicarPose();
        }

        // ------------------------------------------------------------------ pose e colisor
        private void AplicarPose()
        {
            bool agachar = _estado == Estado.Agachado;
            float alturaAlvo = agachar ? kAlturaAgachado : kAlturaEmPe;

            // o CharacterController encolhe junto: agachado passa por vão baixo e é alvo menor
            _cc.height = Mathf.MoveTowards(_cc.height, alturaAlvo, 4f * Time.deltaTime);
            _cc.center = Vector3.zero;

            if (_anim != null)
            {
                _anim.Agachar    = agachar ? 1f : 0f;
                _anim.Sentado    = _estado == Estado.Sentado;
                _anim.Consumindo = _estado == Estado.Consumindo ? 1f : 0f;
            }
        }

        // ------------------------------------------------------------------ sentar
        private Transform AssentoMaisProximo()
        {
            Transform melhor = null;
            float melhorD = kRaioAssento;
            for (int i = 0; i < Assentos.Count; i++)
            {
                var a = Assentos[i];
                if (a == null) continue;
                Vector3 d = a.position - transform.position; d.y = 0f;
                float dist = d.magnitude;
                if (dist < melhorD) { melhorD = dist; melhor = a; }
            }
            return melhor;
        }

        public void Sentar(Transform assento)
        {
            if (assento == null || _estado == Estado.Sentado) return;

            _assento = assento;
            _posAntesDeSentar = transform.position;
            _estado = Estado.Sentado;

            // encosta no banco, virado pra frente dele
            _cc.enabled = false;
            transform.position = assento.position + Vector3.up * 0.62f;
            transform.rotation = Quaternion.Euler(0f, assento.eulerAngles.y, 0f);
            _cc.enabled = true;

            if (_controller != null) _controller.enabled = false;
        }

        public void Levantar()
        {
            if (_estado != Estado.Sentado) return;
            _estado = Estado.Livre;

            _cc.enabled = false;
            transform.position = _posAntesDeSentar + Vector3.up * 0.1f;
            _cc.enabled = true;

            if (_controller != null) _controller.enabled = true;
            _assento = null;
        }

        // ------------------------------------------------------------------ comer / beber
        /// <summary>
        /// Toca o gesto de levar à boca e só então executa <paramref name="aoTerminar"/> — é ele que
        /// aplica os efeitos do item. Devolve falso se já estiver ocupado (não dá pra comer duas coisas).
        /// </summary>
        public bool Consumir(ItemDto item, Action aoTerminar)
        {
            if (_estado == Estado.Consumindo) return false;
            if (item == null) { aoTerminar?.Invoke(); return true; }

            // serviço (lotérica, recarga) não tem gesto de boca: resolve na hora
            if (item.tipo == "servico" || item.tipo == "utilidade") { aoTerminar?.Invoke(); return true; }

            if (_estado == Estado.Sentado) Levantar();

            _estado = Estado.Consumindo;
            _consumoAte = Time.time + duracaoConsumo;
            _aoTerminarConsumo = aoTerminar;
            if (_controller != null) _controller.enabled = false;
            return true;
        }

        private void TerminarConsumo()
        {
            _estado = Estado.Livre;
            if (_controller != null) _controller.enabled = true;
            var cb = _aoTerminarConsumo;
            _aoTerminarConsumo = null;
            cb?.Invoke();
        }

        // ------------------------------------------------------------------ dica de contexto
        private void AtualizarDica()
        {
            if (_estado == Estado.Consumindo) { Dica = "..."; return; }
            if (_estado == Estado.Sentado)    { Dica = "[G] levantar"; return; }
            Dica = AssentoMaisProximo() != null ? "[G] sentar" : "";
        }

        private void OnDestroy()
        {
            // a lista é estática: limpa ao sair do Play para não vazar entre sessões
            Assentos.Clear();
        }
    }
}
