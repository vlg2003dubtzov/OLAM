using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Olam.Core
{
    /// <summary>
    /// Простой учёт квестов: Report(id, amount) и событие завершения всех.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Serializable]
        public class QuestGoal
        {
            public string id;
            public LText title;
            public int required = 1;
            [HideInInspector] public int current;
            [HideInInspector] public bool done;
        }

        [SerializeField] List<QuestGoal> goals = new List<QuestGoal>();
        [SerializeField] UnityEvent onAllCompleted;

        public event Action onAllCompletedEvent;
        public event Action<QuestGoal> onGoalProgress;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Report(string id, int amount = 1)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return;

            foreach (var goal in goals)
            {
                if (goal.done || goal.id != id) continue;

                goal.current = Mathf.Min(goal.required, goal.current + amount);
                onGoalProgress?.Invoke(goal);

                if (goal.current >= goal.required)
                {
                    goal.done = true;
                    CheckAllCompleted();
                }
                return;
            }
        }

        void CheckAllCompleted()
        {
            foreach (var goal in goals)
            {
                if (!goal.done) return;
            }

            onAllCompleted?.Invoke();
            onAllCompletedEvent?.Invoke();
        }

        public IReadOnlyList<QuestGoal> Goals => goals;
    }
}
