using IdleRPG.Domain;
using IdleRPG.Runtime.Configuration;
using UnityEngine;

namespace IdleRPG.Runtime.Stages
{
    [DisallowMultipleComponent]
    public sealed class FieldEncounterController : MonoBehaviour
    {
        [SerializeField] private MvpFieldEncounterSettings Settings = new MvpFieldEncounterSettings();
        [SerializeField] private Transform Player;
        [SerializeField] private Transform EncounterPoint;
        [SerializeField] private StageSceneFlowController SceneFlow;

        private bool RuntimeActive = true;

        public bool HasTriggered { get; private set; }
        public bool IsRuntimeActive => RuntimeActive && enabled;

        public void Initialize(
            MvpFieldEncounterSettings _Settings,
            Transform _Player,
            Transform _EncounterPoint,
            StageSceneFlowController _SceneFlow)
        {
            Settings = _Settings ?? new MvpFieldEncounterSettings();
            Settings.EnsureDefaults();
            Player = _Player;
            EncounterPoint = _EncounterPoint;
            SceneFlow = _SceneFlow;
            HasTriggered = false;
            SetRuntimeActive(true);
        }

        public void ResetEncounter()
        {
            HasTriggered = false;
        }

        public void SetRuntimeActive(bool _Active)
        {
            RuntimeActive = _Active;
            enabled = RuntimeActive;
        }

        public bool TriggerEncounter()
        {
            if (!CanTrigger())
                return false;

            HasTriggered = true;
            if (SceneFlow != null)
                SceneFlow.EnterBattle(Settings.BattleStageNumber);

            return true;
        }

        private void Update()
        {
            if (!CanTrigger() || Settings.TriggerMode != EncounterTriggerMode.Distance)
                return;

            if (Player == null || EncounterPoint == null)
                return;

            if (Vector2.Distance(Player.position, EncounterPoint.position) <= Settings.TriggerDistance)
                TriggerEncounter();
        }

        private bool CanTrigger()
        {
            return RuntimeActive && Settings.Enabled && (!Settings.TriggerOnce || !HasTriggered);
        }
    }
}
