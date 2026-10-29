using System.Collections;
using System.Collections.Generic;
using Olam.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Olam.Kitchen
{
    /// <summary>
    /// Самопроверка миссии «Первая лепёшка» без окна и мыши: шаги проходятся вызовами тех же целей,
    /// что ловят предмет под курсором. Запуск: OLAM.exe -batchmode -nographics -olamSelfTest -logFile путь.
    /// В журнале — «OLAM SELFTEST PASS» или «OLAM SELFTEST FAIL: причина», потом игра закрывается.
    /// Без ключа -olamSelfTest ничего не делает.
    /// </summary>
    public class KitchenSelfTest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-olamSelfTest") < 0) return;
            var go = new GameObject("OLAM_SelfTest");
            DontDestroyOnLoad(go);
            go.AddComponent<KitchenSelfTest>();
        }

        readonly List<string> log = new();
        string fail;

        IEnumerator Start()
        {
            Time.timeScale = 4f;
            yield return SceneManager.LoadSceneAsync("Tandoor");
            yield return null;
            yield return Run();
            foreach (var goal in QuestManager.Instance != null ? QuestManager.Instance.Goals : new List<QuestManager.QuestGoal>())
                log.Add($"{goal.id} {goal.current}/{goal.required} {(goal.done ? "done" : "—")}");
            Debug.Log("OLAM SELFTEST steps: " + string.Join("; ", log));
            Debug.Log(fail == null ? "OLAM SELFTEST PASS" : "OLAM SELFTEST FAIL: " + fail);
            Application.Quit(fail == null ? 0 : 1);
        }

        IEnumerator Run()
        {
            var quests = QuestManager.Instance;
            var thermal = FindAnyObjectByType<TandoorThermal>();
            var fuel = FindAnyObjectByType<FuelZone>();
            var wall = FindAnyObjectByType<TandoorWallZone>();
            var bowl = FindAnyObjectByType<WaterBowl>();
            var serve = FindAnyObjectByType<ServeZone>();
            var cushion = FindAnyObjectByType<BakerCushion>();
            var dough = FindAnyObjectByType<Dough>();
            KitchenItem chekich = null;
            var logs = new List<KitchenItem>();
            foreach (var item in FindObjectsByType<KitchenItem>())
            {
                if (item.ItemKind == KitchenItem.Kind.Chekich) chekich = item;
                if (item.ItemKind == KitchenItem.Kind.Firewood) logs.Add(item);
            }
            if (quests == null || thermal == null || fuel == null || wall == null || bowl == null || serve == null ||
                cushion == null || dough == null || chekich == null || logs.Count < 3)
            {
                fail = "в сцене не хватает частей миссии";
                yield break;
            }
            var doughItem = dough.GetComponent<KitchenItem>();
            var cushionItem = cushion.GetComponent<KitchenItem>();
            bool Done(string id)
            {
                foreach (var g in quests.Goals)
                    if (g.id == id) return g.done;
                return false;
            }
            bool Check(bool ok, string what)
            {
                if (!ok && fail == null) fail = what;
                return ok;
            }

            // голое тесто к стенке не прилипает (горячо), сухое на подушке — тоже
            if (!Check(!wall.TryAccept(doughItem), "голое тесто прилипло к стенке")) yield break;
            for (int i = 0; i < 3; i++) fuel.TryAccept(logs[i]);
            if (!Check(Done("fuel"), "три полена не засчитаны")) yield break;
            yield return Wait(() => Done("heat"), 30f);
            if (!Check(Done("heat"), $"печь не разогрелась: {thermal.Temperature:0} °C")) yield break;
            log.Add($"печь {thermal.Temperature:0} °C");

            if (!Check(dough.TryAccept(chekich) && Done("stamp"), "чекич не наколол тесто")) yield break;
            if (!Check(cushion.TryAccept(doughItem) && dough.State == Dough.DoughState.OnCushion, "тесто не легло на подушку")) yield break;
            if (!Check(!wall.TryAccept(cushionItem), "сухое тесто прилипло")) yield break;
            if (!Check(bowl.TryAccept(cushionItem) && Done("wet"), "тесто на подушке не смочилось")) yield break;
            if (!Check(wall.TryAccept(cushionItem) && Done("stick") && dough.State == Dough.DoughState.OnWall,
                    "тесто не прилипло к стенке")) yield break;

            yield return Wait(() => dough.Golden, 60f);
            if (!Check(dough.Golden && Done("bake_bread"), $"лепёшка не зарумянилась: {dough.BakeProgress:0.00}, печь {thermal.Temperature:0} °C")) yield break;
            log.Add($"румяная при {thermal.Temperature:0} °C");
            dough.GetComponent<OlamInteractable>().NotifySelected();   // как нажатие на лепёшку
            if (!Check(Done("take_out") && dough.State == Dough.DoughState.Free, "лепёшку не сняли со стенки")) yield break;
            if (!Check(serve.TryAccept(doughItem) && Done("serve"), "лепёшка не легла на дастархан")) yield break;
            yield return null;
            foreach (var g in quests.Goals)
                if (!Check(g.done, $"шаг «{g.id}» не сделан")) yield break;
        }

        static IEnumerator Wait(System.Func<bool> until, float seconds)
        {
            for (float t = 0f; t < seconds && !until(); t += Time.unscaledDeltaTime)
                yield return null;
        }
    }
}
