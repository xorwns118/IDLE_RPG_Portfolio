using IdleRPG.Domain;
using IdleRPG.Domain.Data;
using IdleRPG.Runtime.Configuration;
using UnityEngine;

namespace IdleRPG.Runtime.Actors
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Idle RPG/Actor Prefab Profile")]
    public sealed class ActorPrefabProfile : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private ActorTeam TeamValue = ActorTeam.Player;
        [SerializeField] private string IdValue = "player.hero";
        [SerializeField] private string DisplayNameValue = "Training Hero";

        [Header("Visual")]
        [SerializeField] private Color WorldColorValue = new Color(0.25f, 0.7f, 1f);

        [Header("Stats")]
        [SerializeField] private MvpStatBlockSettings StatsValue = MvpStatBlockSettings.Create(140f, 16f, 3f, 1.05f, 0.7f, 2.35f, 0.15f, 1.5f);

        [Header("Monster Reward")]
        [SerializeField, Min(0)] private int GoldRewardValue;
        [SerializeField, Min(0)] private int ExpRewardValue;

        public ActorTeam Team => TeamValue;
        public string Id => IdValue;
        public string DisplayName => DisplayNameValue;
        public Color WorldColor => WorldColorValue;
        public MvpStatBlockSettings Stats => StatsValue;
        public int GoldReward => GoldRewardValue;
        public int ExpReward => ExpRewardValue;

        public void Configure(
            ActorTeam _Team,
            string _Id,
            string _DisplayName,
            Color _WorldColor,
            MvpStatBlockSettings _Stats,
            int _GoldReward,
            int _ExpReward)
        {
            TeamValue = _Team;
            IdValue = _Id;
            DisplayNameValue = _DisplayName;
            WorldColorValue = _WorldColor;
            StatsValue = CloneStats(_Stats ?? CreateDefaultStats(_Team));
            GoldRewardValue = Mathf.Max(0, _GoldReward);
            ExpRewardValue = Mathf.Max(0, _ExpReward);
            EnsureDefaults();
        }

        public PlayerDefinition ToPlayerDefinition()
        {
            EnsureDefaults();
            return new PlayerDefinition(IdValue, DisplayNameValue, StatsValue.ToStatBlock());
        }

        public MonsterDefinition ToMonsterDefinition()
        {
            EnsureDefaults();
            return new MonsterDefinition(IdValue, DisplayNameValue, StatsValue.ToStatBlock(), GoldRewardValue, ExpRewardValue);
        }

        private void OnValidate()
        {
            EnsureDefaults();
        }

        private void EnsureDefaults()
        {
            if (string.IsNullOrWhiteSpace(IdValue))
            {
                IdValue = TeamValue == ActorTeam.Player ? "player.hero" : "monster.base";
            }

            if (string.IsNullOrWhiteSpace(DisplayNameValue))
            {
                DisplayNameValue = IdValue;
            }

            if (StatsValue == null)
            {
                StatsValue = CreateDefaultStats(TeamValue);
            }

            NormalizeStats(StatsValue);
            GoldRewardValue = Mathf.Max(0, GoldRewardValue);
            ExpRewardValue = Mathf.Max(0, ExpRewardValue);
        }

        private static MvpStatBlockSettings CreateDefaultStats(ActorTeam _Team)
        {
            return _Team == ActorTeam.Player
                ? MvpStatBlockSettings.Create(140f, 16f, 3f, 1.05f, 0.7f, 2.35f, 0.15f, 1.5f)
                : MvpStatBlockSettings.Create(32f, 5f, 0f, 0.85f, 1.25f, 1.2f, 0.02f, 1.25f);
        }

        private static MvpStatBlockSettings CloneStats(MvpStatBlockSettings _Stats)
        {
            return MvpStatBlockSettings.Create(
                _Stats.MaxHp,
                _Stats.AttackPower,
                _Stats.Defense,
                _Stats.AttackRange,
                _Stats.AttackInterval,
                _Stats.MoveSpeed,
                _Stats.CriticalChance,
                _Stats.CriticalMultiplier);
        }

        private static void NormalizeStats(MvpStatBlockSettings _Stats)
        {
            _Stats.MaxHp = Mathf.Max(1f, _Stats.MaxHp);
            _Stats.AttackPower = Mathf.Max(0f, _Stats.AttackPower);
            _Stats.Defense = Mathf.Max(0f, _Stats.Defense);
            _Stats.AttackRange = Mathf.Max(0.1f, _Stats.AttackRange);
            _Stats.AttackInterval = Mathf.Max(0.1f, _Stats.AttackInterval);
            _Stats.MoveSpeed = Mathf.Max(0f, _Stats.MoveSpeed);
            _Stats.CriticalChance = Mathf.Clamp01(_Stats.CriticalChance);
            _Stats.CriticalMultiplier = Mathf.Max(1f, _Stats.CriticalMultiplier);
        }
    }
}
