#if UNITY_EDITOR
using System.IO;
using System.Text;
using Caos.Data;
using Caos.Simulation;
using UnityEditor;
using UnityEngine;

namespace Caos.EditorTools
{
    /// <summary>
    /// Prova que a cidade é <b>determinística</b>: gera o mundo duas vezes com a mesma semente e
    /// compara um hash da posição e do nome de cada peça. Se bater, os dois clientes de uma partida
    /// em rede verão exatamente a mesma cidade — que é o pré-requisito de qualquer multiplayer neste
    /// projeto, já que o mundo não é uma cena salva, é gerado em runtime.
    ///
    /// Também gera com uma semente <b>diferente</b> e confere que o hash muda — senão o teste estaria
    /// passando por acidente (por exemplo, se alguém tivesse removido a aleatoriedade sem querer).
    ///
    /// Uso: menu <b>Caos ▸ Verificar determinismo do mundo</b>, ou em lote:
    /// <c>-executeMethod Caos.EditorTools.CaosDeterminismCheck.Run</c>
    /// </summary>
    public static class CaosDeterminismCheck
    {
        [MenuItem("Caos/Verificar determinismo do mundo")]
        public static void Run()
        {
            var catalogos = CarregarCatalogos();

            string a = HashDoMundo(catalogos, semente: 12345);
            string b = HashDoMundo(catalogos, semente: 12345);
            string c = HashDoMundo(catalogos, semente: 99999);

            bool iguais    = a == b;
            bool diferente = a != c;

            var sb = new StringBuilder();
            sb.AppendLine("=== DETERMINISMO DO MUNDO ===");
            sb.AppendLine($"semente 12345 (1ª): {a}");
            sb.AppendLine($"semente 12345 (2ª): {b}");
            sb.AppendLine($"semente 99999     : {c}");
            sb.AppendLine($"mesma semente → mesmo mundo : {(iguais ? "OK" : "FALHOU")}");
            sb.AppendLine($"semente nova  → mundo novo  : {(diferente ? "OK" : "FALHOU")}");

            string resultado = sb.ToString();
            Debug.Log(resultado);
            try
            {
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "caos_determinismo.txt"), resultado);
            }
            catch { /* ignora falha de I/O */ }

            if (Application.isBatchMode) EditorApplication.Exit(iguais && diferente ? 0 : 1);
        }

        /// <summary>Constrói a cidade num objeto descartável e devolve o hash do resultado.</summary>
        private static string HashDoMundo(GameCatalogs catalogos, int semente)
        {
            var raiz = new GameObject("[VerificacaoDeterminismo]");
            try
            {
                var layout = new CityLayout(13, catalogos);
                var gen    = new CityGenerator(layout, catalogos, raiz.transform);
                CityRuntime.GerarDeterministico(gen, semente);

                // hash de tudo que a geração produziu: nome + posição de cada peça, arredondada ao mm
                // (o float bruto varia no último bit entre execuções; o milímetro é o que importa)
                var sb = new StringBuilder();
                var todos = raiz.GetComponentsInChildren<Transform>(true);
                foreach (var t in todos)
                {
                    Vector3 p = t.position;
                    sb.Append(t.name).Append('|')
                      .Append(Mathf.RoundToInt(p.x * 1000f)).Append(',')
                      .Append(Mathf.RoundToInt(p.y * 1000f)).Append(',')
                      .Append(Mathf.RoundToInt(p.z * 1000f)).Append(';');
                }
                sb.Append("#pecas=").Append(todos.Length);

                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                    var hex = new StringBuilder(bytes.Length * 2);
                    foreach (byte x in bytes) hex.Append(x.ToString("x2"));
                    return hex.ToString() + $"  ({todos.Length} peças)";
                }
            }
            finally
            {
                Object.DestroyImmediate(raiz);
                CityPalette.Clear();
                PlayerActions.Assentos.Clear();
                CityProps.AssentosPendentes.Clear();
            }
        }

        /// <summary>Lê os catálogos direto do disco (sem depender do GameManager estar de pé).</summary>
        private static GameCatalogs CarregarCatalogos()
        {
            var c = new GameCatalogs();
            string pasta = Path.Combine(Application.streamingAssetsPath, "Data");

            Ler<VehicleDto>(pasta, "vehicles.json",  c.Vehicles);
            Ler<FactionDto>(pasta, "factions.json",  c.Factions);
            Ler<DistrictDto>(pasta, "districts.json",c.Districts);
            Ler<ItemDto>(pasta, "items.json",        c.Items);
            Ler<MissionDto>(pasta, "missions.json",  c.Missions);
            Ler<ShopDto>(pasta, "shops.json",        c.Shops);

            string ruas = Path.Combine(pasta, "streets.json");
            if (File.Exists(ruas)) c.Streets = JsonUtility.FromJson<StreetNamesDto>(File.ReadAllText(ruas));

            c.IndexAll();
            return c;
        }

        [System.Serializable] private class Lista<T> { public System.Collections.Generic.List<T> items; }

        private static void Ler<T>(string pasta, string arquivo, System.Collections.Generic.List<T> destino)
        {
            string caminho = Path.Combine(pasta, arquivo);
            if (!File.Exists(caminho)) return;
            var lista = JsonUtility.FromJson<Lista<T>>(File.ReadAllText(caminho));
            if (lista?.items != null) destino.AddRange(lista.items);
        }
    }
}
#endif
