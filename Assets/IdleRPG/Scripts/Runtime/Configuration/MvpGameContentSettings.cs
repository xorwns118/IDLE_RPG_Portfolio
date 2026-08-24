using System;
using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Data;
using UnityEngine;

namespace IdleRPG.Runtime.Configuration
{
    [Serializable]
    public sealed class MvpGameContentSettings
    {
        [Header("Player")]
        [Tooltip("The player definition used by the MVP battle loop.")]
        public MvpPlayerContentSettings Player = MvpPlayerContentSettings.CreateDefault();

        [Header("Monsters")]
        [Tooltip("Monster definitions that stages can reference by Id.")]
        public MvpMonsterContentSettings[] Monsters = CreateDefaultMonsters();

        [Header("Stages")]
        [Tooltip("Stage order and the monster Id each stage should spawn.")]
        public MvpStageContentSettings[] Stages = CreateDefaultStages();

        public static MvpGameContentSettings CreateDefault()
        {
            return new MvpGameContentSettings();
        }

        public void EnsureDefaults()
        {
            if (Player == null)
            {
                Player = MvpPlayerContentSettings.CreateDefault();
            }

            Player.EnsureDefaults();

            if (Monsters == null || Monsters.Length == 0)
            {
                Monsters = CreateDefaultMonsters();
            }

            for (int i = 0; i < Monsters.Length; i++)
            {
                if (Monsters[i] == null)
                {
                    Monsters[i] = MvpMonsterContentSettings.CreateDefault(i);
                }

                Monsters[i].EnsureDefaults(i);
            }

            if (Stages == null || Stages.Length == 0)
            {
                Stages = CreateDefaultStages();
            }

            for (int i = 0; i < Stages.Length; i++)
            {
                if (Stages[i] == null)
                {
                    Stages[i] = MvpStageContentSettings.CreateDefault(i);
                }

                Stages[i].EnsureDefaults(i, Monsters);
            }
        }

        public RuntimeContentDatabase CreateDatabase()
        {
            EnsureDefaults();

            MonsterDefinition[] monsterDefinitions = new MonsterDefinition[Monsters.Length];
            for (int i = 0; i < Monsters.Length; i++)
            {
                monsterDefinitions[i] = Monsters[i].ToDefinition();
            }

            StageDefinition[] stageDefinitions = new StageDefinition[Stages.Length];
            for (int i = 0; i < Stages.Length; i++)
            {
                stageDefinitions[i] = Stages[i].ToDefinition();
            }

            return new RuntimeContentDatabase(Player.ToDefinition(), monsterDefinitions, stageDefinitions);
        }

        public string PlayerDisplayName
        {
            get
            {
                EnsureDefaults();
                return Player.DisplayName;
            }
        }

        public int GetRequiredKillsForStage(int _StageNumber)
        {
            EnsureDefaults();

            foreach (MvpStageContentSettings stage in Stages)
            {
                if (stage.StageNumber == _StageNumber)
                {
                    return Mathf.Max(1, stage.RequiredKills);
                }
            }

            return Stages[Stages.Length - 1].RequiredKills;
        }

        public Color ResolveMonsterColor(string _MonsterId, Color _FallbackColor)
        {
            EnsureDefaults();

            foreach (MvpMonsterContentSettings monster in Monsters)
            {
                if (monster != null && monster.Id == _MonsterId)
                {
                    return monster.WorldColor;
                }
            }

            return _FallbackColor;
        }

        private static MvpMonsterContentSettings[] CreateDefaultMonsters()
        {
            return new[]
            {
                MvpMonsterContentSettings.CreateSlime(),
                MvpMonsterContentSettings.CreateGoblin(),
                MvpMonsterContentSettings.CreateTrainingKnight()
            };
        }

        private static MvpStageContentSettings[] CreateDefaultStages()
        {
            return new[]
            {
                new MvpStageContentSettings { StageNumber = 1, MonsterId = "monster.slime", RequiredKills = 3 },
                new MvpStageContentSettings { StageNumber = 2, MonsterId = "monster.goblin", RequiredKills = 4 },
                new MvpStageContentSettings { StageNumber = 3, MonsterId = "monster.knight", RequiredKills = 5 },
                new MvpStageContentSettings { StageNumber = 4, MonsterId = "monster.goblin", RequiredKills = 5 },
                new MvpStageContentSettings { StageNumber = 5, MonsterId = "monster.knight", RequiredKills = 6 }
            };
        }
    }

    [Serializable]
    public sealed class MvpPlayerContentSettings
    {
        [Tooltip("Stable id used by code and save data.")]
        public string Id = "player.hero";

        [Tooltip("Name shown in world labels and HUD.")]
        public string DisplayName = "Training Hero";

        [Tooltip("Battle numbers for the player.")]
        public MvpStatBlockSettings Stats = MvpStatBlockSettings.Create(140f, 16f, 3f, 1.05f, 0.7f, 2.35f, 0.15f, 1.5f);

        public static MvpPlayerContentSettings CreateDefault()
        {
            return new MvpPlayerContentSettings();
        }

        public void EnsureDefaults()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                Id = "player.hero";
            }

            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                DisplayName = Id;
            }

            if (Stats == null)
            {
                Stats = MvpStatBlockSettings.Create(140f, 16f, 3f, 1.05f, 0.7f, 2.35f, 0.15f, 1.5f);
            }
        }

        public PlayerDefinition ToDefinition()
        {
            EnsureDefaults();
            return new PlayerDefinition(Id, DisplayName, Stats.ToStatBlock());
        }
    }

    [Serializable]
    public sealed class MvpMonsterContentSettings
    {
        [Tooltip("Stable id referenced by Stage settings.")]
        public string Id = "monster.slime";

        [Tooltip("Base name shown before the stage suffix is applied.")]
        public string DisplayName = "Slime";

        [Tooltip("Color used by the generated MVP sprite.")]
        public Color WorldColor = new Color(0.35f, 0.9f, 0.55f);

        [Tooltip("Battle numbers before stage scaling.")]
        public MvpStatBlockSettings Stats = MvpStatBlockSettings.Create(32f, 5f, 0f, 0.85f, 1.25f, 1.2f, 0.02f, 1.25f);

        [Min(0)]
        [Tooltip("Base gold reward before stage scaling.")]
        public int GoldReward = 5;

        [Min(0)]
        [Tooltip("Base EXP reward before stage scaling.")]
        public int ExpReward = 2;

        public static MvpMonsterContentSettings CreateDefault(int _Index)
        {
            if (_Index == 1)
            {
                return CreateGoblin();
            }

            if (_Index == 2)
            {
                return CreateTrainingKnight();
            }

            return CreateSlime();
        }

        public static MvpMonsterContentSettings CreateSlime()
        {
            return new MvpMonsterContentSettings();
        }

        public static MvpMonsterContentSettings CreateGoblin()
        {
            return new MvpMonsterContentSettings
            {
                Id = "monster.goblin",
                DisplayName = "Goblin",
                WorldColor = new Color(0.95f, 0.75f, 0.35f),
                Stats = MvpStatBlockSettings.Create(46f, 7f, 1f, 0.9f, 1.1f, 1.45f, 0.05f, 1.3f),
                GoldReward = 8,
                ExpReward = 3
            };
        }

        public static MvpMonsterContentSettings CreateTrainingKnight()
        {
            return new MvpMonsterContentSettings
            {
                Id = "monster.knight",
                DisplayName = "Training Knight",
                WorldColor = new Color(0.55f, 0.65f, 0.85f),
                Stats = MvpStatBlockSettings.Create(70f, 9f, 2f, 0.95f, 1.2f, 1f, 0.08f, 1.35f),
                GoldReward = 12,
                ExpReward = 5
            };
        }

        public void EnsureDefaults(int _Index)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                Id = "monster.custom." + (_Index + 1);
            }

            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                DisplayName = Id;
            }

            if (Stats == null)
            {
                Stats = MvpStatBlockSettings.Create(32f, 5f, 0f, 0.85f, 1.25f, 1.2f, 0.02f, 1.25f);
            }
        }

        public MonsterDefinition ToDefinition()
        {
            EnsureDefaults(0);
            return new MonsterDefinition(Id, DisplayName, Stats.ToStatBlock(), GoldReward, ExpReward);
        }
    }

    [Serializable]
    public sealed class MvpStageContentSettings
    {
        [Min(1)]
        [Tooltip("Stage number shown in the HUD.")]
        public int StageNumber = 1;

        [Tooltip("Monster Id to spawn for this stage.")]
        public string MonsterId = "monster.slime";

        [Min(1)]
        [Tooltip("Kills required before advancing to the next stage.")]
        public int RequiredKills = 3;

        public static MvpStageContentSettings CreateDefault(int _Index)
        {
            return new MvpStageContentSettings
            {
                StageNumber = _Index + 1,
                MonsterId = "monster.slime",
                RequiredKills = 3
            };
        }

        public void EnsureDefaults(int _Index, MvpMonsterContentSettings[] _Monsters)
        {
            StageNumber = Mathf.Max(1, StageNumber);
            RequiredKills = Mathf.Max(1, RequiredKills);

            if (string.IsNullOrWhiteSpace(MonsterId))
            {
                MonsterId = _Monsters != null && _Monsters.Length > 0 && _Monsters[0] != null
                    ? _Monsters[0].Id
                    : "monster.slime";
            }
        }

        public StageDefinition ToDefinition()
        {
            EnsureDefaults(0, null);
            return new StageDefinition(StageNumber, MonsterId, RequiredKills);
        }
    }

    [Serializable]
    public sealed class MvpStatBlockSettings
    {
        [Min(1f)] public float MaxHp = 100f;
        [Min(0f)] public float AttackPower = 10f;
        [Min(0f)] public float Defense = 0f;
        [Min(0.1f)] public float AttackRange = 1f;
        [Min(0.1f)] public float AttackInterval = 1f;
        [Min(0f)] public float MoveSpeed = 1f;
        [Range(0f, 1f)] public float CriticalChance = 0.05f;
        [Min(1f)] public float CriticalMultiplier = 1.5f;

        public static MvpStatBlockSettings Create(
            float _MaxHp,
            float _AttackPower,
            float _Defense,
            float _AttackRange,
            float _AttackInterval,
            float _MoveSpeed,
            float _CriticalChance,
            float _CriticalMultiplier)
        {
            return new MvpStatBlockSettings
            {
                MaxHp = _MaxHp,
                AttackPower = _AttackPower,
                Defense = _Defense,
                AttackRange = _AttackRange,
                AttackInterval = _AttackInterval,
                MoveSpeed = _MoveSpeed,
                CriticalChance = _CriticalChance,
                CriticalMultiplier = _CriticalMultiplier
            };
        }

        public StatBlock ToStatBlock()
        {
            return new StatBlock(MaxHp, AttackPower, Defense, AttackRange, AttackInterval, MoveSpeed, CriticalChance, CriticalMultiplier);
        }
    }
}
