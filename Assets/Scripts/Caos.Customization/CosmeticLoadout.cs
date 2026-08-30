using System;
using Caos.Core;
using UnityEngine;

namespace Caos.Customization
{
    /// <summary>
    /// O "visual atual" do protagonista: um id por categoria (docs/03 §1.2).
    ///
    /// Persistência <b>propositalmente fora do SaveSystem</b> (que tem PR aberto): vai em
    /// PlayerPrefs, uma chave por slot de save (<c>caos_look_slot{N}</c>), serializada com
    /// JsonUtility. Cosmético é dado pequeno e estável — se a chave sumir, o padrão (a
    /// amarelinha de sempre) é aplicado e nenhum progresso se perde.
    /// </summary>
    [Serializable]
    public class CosmeticLoadout
    {
        public string genero    = "masculino";
        public string pele      = "pele_5";
        public string cabelo    = "curto";
        public string corCabelo = "preto";
        public string top       = "camiseta_amarelinha";
        public string bottom    = "calca_jeans";
        public string calcado   = "tenis_preto";
        public string chapeu    = "bone_verde";

        private static string Chave(int slot) => "caos_look_slot" + Mathf.Max(1, slot);

        /// <summary>Cópia profunda (a tela de personagem edita um rascunho, não o original).</summary>
        public CosmeticLoadout Clone() => (CosmeticLoadout)MemberwiseClone();

        public void CopiarDe(CosmeticLoadout outro)
        {
            genero    = outro.genero;
            pele      = outro.pele;
            cabelo    = outro.cabelo;
            corCabelo = outro.corCabelo;
            top       = outro.top;
            bottom    = outro.bottom;
            calcado   = outro.calcado;
            chapeu    = outro.chapeu;
        }

        /// <summary>Lê o visual salvo do slot; sem chave (ou JSON corrompido) volta o padrão.</summary>
        public static CosmeticLoadout Carregar(int slot)
        {
            string json = PlayerPrefs.GetString(Chave(slot), "");
            if (string.IsNullOrEmpty(json)) return new CosmeticLoadout();
            try
            {
                var l = JsonUtility.FromJson<CosmeticLoadout>(json);
                return l ?? new CosmeticLoadout();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Cosméticos] Save de visual inválido (" + e.Message + ") — voltando ao padrão.");
                return new CosmeticLoadout();
            }
        }

        public void Salvar(int slot)
        {
            PlayerPrefs.SetString(Chave(slot), JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        /// <summary>Apaga o visual do slot (usado quando o jogador começa vida nova).</summary>
        public static void Apagar(int slot) => PlayerPrefs.DeleteKey(Chave(slot));

        /// <summary>Slot da sessão atual, para não vazar visual de uma vida para outra.</summary>
        public static int SlotAtual => GameSession.Slot;
    }
}
