using System;
using IdleRPG.Domain;
using IdleRPG.Runtime.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleRPG.Runtime.Stages
{
    [DisallowMultipleComponent]
    public sealed class StageSceneFlowController : MonoBehaviour
    {
        private static bool HasPendingBattleStageNumber;

        [SerializeField] private MvpSceneFlowSettings Settings = new MvpSceneFlowSettings();

        public static int PendingBattleStageNumber { get; private set; } = 1;
        public StageFlowMode CurrentMode { get; private set; } = StageFlowMode.Battle;
        public int RequestedStageNumber { get; private set; } = 1;
        public bool HasBattleStageRequest { get; private set; }

        public event Action<StageFlowMode, int> FlowChanged;

        public void Initialize(MvpSceneFlowSettings _Settings)
        {
            Settings = _Settings ?? new MvpSceneFlowSettings();
            Settings.EnsureDefaults();
            CurrentMode = Settings.InitialMode;
            HasBattleStageRequest = HasPendingBattleStageNumber;
            RequestedStageNumber = HasBattleStageRequest ? Mathf.Max(1, PendingBattleStageNumber) : 1;
            FlowChanged?.Invoke(CurrentMode, RequestedStageNumber);
        }

        public void EnterField()
        {
            ClearBattleStageRequest();
            CurrentMode = StageFlowMode.Field;
            FlowChanged?.Invoke(CurrentMode, RequestedStageNumber);
            LoadSceneIfConfigured(Settings.FieldSceneName);
        }

        public void EnterBattle(int _StageNumber)
        {
            RequestedStageNumber = Mathf.Max(1, _StageNumber);
            PendingBattleStageNumber = RequestedStageNumber;
            HasPendingBattleStageNumber = true;
            HasBattleStageRequest = true;
            CurrentMode = StageFlowMode.Battle;
            FlowChanged?.Invoke(CurrentMode, RequestedStageNumber);
            LoadSceneIfConfigured(Settings.BattleSceneName);
        }

        public void ClearBattleStageRequest()
        {
            HasBattleStageRequest = false;
            ClearPendingBattleStage();
        }

        public static void ClearPendingBattleStage()
        {
            PendingBattleStageNumber = 1;
            HasPendingBattleStageNumber = false;
        }

        private void LoadSceneIfConfigured(string _SceneName)
        {
            if (!Settings.LoadConfiguredScenes || string.IsNullOrWhiteSpace(_SceneName))
                return;

            SceneManager.LoadScene(_SceneName);
        }
    }
}
