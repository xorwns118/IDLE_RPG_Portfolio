using System;
using System.Collections.Generic;
using IdleRPG.Domain;
using IdleRPG.Domain.Actors;
using IdleRPG.Domain.Data;
using IdleRPG.Domain.Skills;
using UnityEngine;

namespace IdleRPG.Runtime.Configuration
{
    [Serializable]
    public sealed class MvpGameContentSettings
    {
        [Header("Player")]
        [Tooltip("The player definition used by the MVP battle loop.")]
        public MvpPlayerContentSettings Player = MvpPlayerContentSettings.CreateDefault();

        [Header("Skills")]
        [Tooltip("Reusable skills that player and monster loadouts can reference by Id.")]
        public MvpSkillContentSettings[] Skills = CreateDefaultSkills();

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
                Player = MvpPlayerContentSettings.CreateDefault();

            Player.EnsureDefaults();

            if (Skills == null || Skills.Length == 0)
                Skills = CreateDefaultSkills();

            for (int i = 0; i < Skills.Length; i++)
            {
                if (Skills[i] == null)
                    Skills[i] = MvpSkillContentSettings.CreateDefault(i);

                Skills[i].EnsureDefaults(i);
            }

            if (Monsters == null || Monsters.Length == 0)
                Monsters = CreateDefaultMonsters();

            for (int i = 0; i < Monsters.Length; i++)
            {
                if (Monsters[i] == null)
                    Monsters[i] = MvpMonsterContentSettings.CreateDefault(i);

                Monsters[i].EnsureDefaults(i);
            }

            if (Stages == null || Stages.Length == 0)
                Stages = CreateDefaultStages();

            for (int i = 0; i < Stages.Length; i++)
            {
                if (Stages[i] == null)
                    Stages[i] = MvpStageContentSettings.CreateDefault(i);

                Stages[i].EnsureDefaults(i, Monsters);
            }
        }

        public RuntimeContentDatabase CreateDatabase()
        {
            EnsureDefaults();

            SkillDefinition[] skillDefinitions = CreateSkillDefinitions();
            Dictionary<string, SkillDefinition> skillsById = CreateSkillMap(skillDefinitions);

            MonsterDefinition[] monsterDefinitions = new MonsterDefinition[Monsters.Length];
            for (int i = 0; i < Monsters.Length; i++)
            {
                monsterDefinitions[i] = Monsters[i].ToDefinition(skillsById);
            }

            StageDefinition[] stageDefinitions = new StageDefinition[Stages.Length];
            for (int i = 0; i < Stages.Length; i++)
            {
                stageDefinitions[i] = Stages[i].ToDefinition();
            }

            return new RuntimeContentDatabase(Player.ToDefinition(skillsById), monsterDefinitions, stageDefinitions, skillDefinitions);
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
                    return Mathf.Max(1, stage.RequiredKills);
            }

            return Stages[Stages.Length - 1].RequiredKills;
        }

        public Color ResolveMonsterColor(string _MonsterId, Color _FallbackColor)
        {
            EnsureDefaults();

            foreach (MvpMonsterContentSettings monster in Monsters)
            {
                if (monster != null && monster.Id == _MonsterId)
                    return monster.WorldColor;
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

        private SkillDefinition[] CreateSkillDefinitions()
        {
            SkillDefinition[] skillDefinitions = new SkillDefinition[Skills.Length];
            for (int i = 0; i < Skills.Length; i++)
            {
                skillDefinitions[i] = Skills[i].ToDefinition();
            }

            return skillDefinitions;
        }

        private static Dictionary<string, SkillDefinition> CreateSkillMap(IEnumerable<SkillDefinition> _Skills)
        {
            Dictionary<string, SkillDefinition> skillsById = new Dictionary<string, SkillDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (SkillDefinition skill in _Skills)
            {
                if (skill == null)
                    continue;

                skillsById.Add(skill.Id, skill);
            }

            return skillsById;
        }

        private static MvpSkillContentSettings[] CreateDefaultSkills()
        {
            return new[]
            {
                MvpSkillContentSettings.CreatePowerStrike(),
                MvpSkillContentSettings.CreateBattleFocus(),
                MvpSkillContentSettings.CreateMonsterBite()
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

        [Tooltip("Up to four skill ids used by the player in priority order. Empty slots are ignored.")]
        public MvpSkillLoadoutSettings SkillLoadout = MvpSkillLoadoutSettings.CreateHeroDefault();

        public static MvpPlayerContentSettings CreateDefault()
        {
            return new MvpPlayerContentSettings();
        }

        public void EnsureDefaults()
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = "player.hero";

            if (string.IsNullOrWhiteSpace(DisplayName))
                DisplayName = Id;

            if (Stats == null)
                Stats = MvpStatBlockSettings.Create(140f, 16f, 3f, 1.05f, 0.7f, 2.35f, 0.15f, 1.5f);

            if (SkillLoadout == null)
                SkillLoadout = MvpSkillLoadoutSettings.CreateHeroDefault();
        }

        public PlayerDefinition ToDefinition()
        {
            EnsureDefaults();
            return new PlayerDefinition(Id, DisplayName, Stats.ToStatBlock());
        }

        public PlayerDefinition ToDefinition(IReadOnlyDictionary<string, SkillDefinition> _SkillsById)
        {
            EnsureDefaults();
            return new PlayerDefinition(Id, DisplayName, Stats.ToStatBlock(), SkillLoadout.ResolveSkills(_SkillsById));
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

        [Tooltip("Up to four skill ids used by this monster in priority order. Empty slots are ignored.")]
        public MvpSkillLoadoutSettings SkillLoadout = MvpSkillLoadoutSettings.CreateMonsterDefault();

        public static MvpMonsterContentSettings CreateDefault(int _Index)
        {
            if (_Index == 1)
                return CreateGoblin();

            if (_Index == 2)
                return CreateTrainingKnight();

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
                Id = "monster.custom." + (_Index + 1);

            if (string.IsNullOrWhiteSpace(DisplayName))
                DisplayName = Id;

            if (Stats == null)
                Stats = MvpStatBlockSettings.Create(32f, 5f, 0f, 0.85f, 1.25f, 1.2f, 0.02f, 1.25f);

            if (SkillLoadout == null)
                SkillLoadout = MvpSkillLoadoutSettings.CreateMonsterDefault();
        }

        public MonsterDefinition ToDefinition()
        {
            EnsureDefaults(0);
            return new MonsterDefinition(Id, DisplayName, Stats.ToStatBlock(), GoldReward, ExpReward);
        }

        public MonsterDefinition ToDefinition(IReadOnlyDictionary<string, SkillDefinition> _SkillsById)
        {
            EnsureDefaults(0);
            return new MonsterDefinition(Id, DisplayName, Stats.ToStatBlock(), GoldReward, ExpReward, SkillLoadout.ResolveSkills(_SkillsById));
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
    public sealed class MvpSkillContentSettings
    {
        [Tooltip("Stable id referenced by player and monster loadout slots.")]
        public string Id = "skill.hero.power_strike";

        [Tooltip("Name used by logs and future combat UI.")]
        public string DisplayName = "Power Strike";

        [Tooltip("Main target requirement checked before the skill can run.")]
        public SkillTargetType TargetType = SkillTargetType.Enemy;

        [Min(0.01f)]
        [Tooltip("Seconds before this skill can be used again.")]
        public float CooldownSeconds = 2.5f;

        [Min(0f)]
        [Tooltip("World distance required to use this skill.")]
        public float Range = 1.25f;

        [Tooltip("Higher priority skills are selected first when several skills are ready.")]
        public int Priority = 20;

        [Tooltip("Effects are executed from top to bottom.")]
        public MvpSkillEffectSettings[] Effects = new[]
        {
            MvpSkillEffectSettings.CreateDamage("damage", SkillTargetType.Enemy, 1.75f)
        };

        public static MvpSkillContentSettings CreateDefault(int _Index)
        {
            if (_Index == 1)
                return CreateBattleFocus();

            if (_Index == 2)
                return CreateMonsterBite();

            return CreatePowerStrike();
        }

        public static MvpSkillContentSettings CreatePowerStrike()
        {
            return new MvpSkillContentSettings();
        }

        public static MvpSkillContentSettings CreateBattleFocus()
        {
            return new MvpSkillContentSettings
            {
                Id = "skill.hero.battle_focus",
                DisplayName = "Battle Focus",
                TargetType = SkillTargetType.Self,
                CooldownSeconds = 6f,
                Range = 1.05f,
                Priority = 10,
                Effects = new[]
                {
                    MvpSkillEffectSettings.CreateBuff(
                        "focus.attack",
                        SkillTargetType.Self,
                        MvpStatModifierSettings.CreateAttackPowerMultiplier(1.25f),
                        3f)
                }
            };
        }

        public static MvpSkillContentSettings CreateMonsterBite()
        {
            return new MvpSkillContentSettings
            {
                Id = "skill.monster.bite",
                DisplayName = "Bite",
                TargetType = SkillTargetType.Enemy,
                CooldownSeconds = 3f,
                Range = 0.95f,
                Priority = 8,
                Effects = new[]
                {
                    MvpSkillEffectSettings.CreateDamage("bite.damage", SkillTargetType.Enemy, 1.2f)
                }
            };
        }

        public void EnsureDefaults(int _Index)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = "skill.custom." + (_Index + 1);

            if (string.IsNullOrWhiteSpace(DisplayName))
                DisplayName = Id;

            if (!Enum.IsDefined(typeof(SkillTargetType), TargetType))
                TargetType = SkillTargetType.Enemy;

            CooldownSeconds = Mathf.Max(0.01f, CooldownSeconds);
            Range = Mathf.Max(0f, Range);

            if (Effects == null || Effects.Length == 0)
            {
                Effects = new[]
                {
                    MvpSkillEffectSettings.CreateDamage("damage", TargetType, 1f)
                };
            }

            for (int i = 0; i < Effects.Length; i++)
            {
                if (Effects[i] == null)
                    Effects[i] = MvpSkillEffectSettings.CreateDamage("damage." + (i + 1), TargetType, 1f);

                Effects[i].EnsureDefaults(i);
            }
        }

        public SkillDefinition ToDefinition()
        {
            EnsureDefaults(0);

            SkillEffectDefinition[] effectDefinitions = new SkillEffectDefinition[Effects.Length];
            for (int i = 0; i < Effects.Length; i++)
            {
                effectDefinitions[i] = Effects[i].ToDefinition();
            }

            return new SkillDefinition(Id, DisplayName, TargetType, CooldownSeconds, Range, Priority, effectDefinitions);
        }
    }

    [Serializable]
    public sealed class MvpSkillEffectSettings
    {
        [Tooltip("Stable id for this effect within a skill.")]
        public string Id = "damage";

        public SkillEffectKind Kind = SkillEffectKind.Damage;
        public SkillTargetType TargetType = SkillTargetType.Enemy;

        [Min(0f)]
        [Tooltip("Damage multiplier applied to the caster attack power for Damage effects.")]
        public float PowerMultiplier = 1f;

        [Min(0f)]
        [Tooltip("Duration in seconds for Buff effects. Zero means the modifier does not expire.")]
        public float DurationSeconds;

        [Tooltip("Stat modifier used by Buff effects.")]
        public MvpStatModifierSettings Modifier = MvpStatModifierSettings.CreateNone();

        public static MvpSkillEffectSettings CreateDamage(string _Id, SkillTargetType _TargetType, float _PowerMultiplier)
        {
            return new MvpSkillEffectSettings
            {
                Id = _Id,
                Kind = SkillEffectKind.Damage,
                TargetType = _TargetType,
                PowerMultiplier = _PowerMultiplier,
                DurationSeconds = 0f,
                Modifier = MvpStatModifierSettings.CreateNone()
            };
        }

        public static MvpSkillEffectSettings CreateBuff(
            string _Id,
            SkillTargetType _TargetType,
            MvpStatModifierSettings _Modifier,
            float _DurationSeconds)
        {
            return new MvpSkillEffectSettings
            {
                Id = _Id,
                Kind = SkillEffectKind.Buff,
                TargetType = _TargetType,
                PowerMultiplier = 0f,
                DurationSeconds = _DurationSeconds,
                Modifier = _Modifier ?? MvpStatModifierSettings.CreateNone()
            };
        }

        public void EnsureDefaults(int _Index)
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Kind.ToString().ToLowerInvariant() + "." + (_Index + 1);

            if (!Enum.IsDefined(typeof(SkillEffectKind), Kind))
                Kind = SkillEffectKind.Damage;

            if (!Enum.IsDefined(typeof(SkillTargetType), TargetType))
                TargetType = SkillTargetType.Enemy;

            PowerMultiplier = Mathf.Max(0f, PowerMultiplier);
            DurationSeconds = Mathf.Max(0f, DurationSeconds);

            if (Modifier == null)
                Modifier = MvpStatModifierSettings.CreateNone();

            Modifier.EnsureDefaults();
        }

        public SkillEffectDefinition ToDefinition()
        {
            EnsureDefaults(0);

            if (Kind == SkillEffectKind.Buff)
                return SkillEffectDefinition.Buff(Id, TargetType, Modifier.ToStatModifier(), DurationSeconds);

            return SkillEffectDefinition.Damage(Id, TargetType, PowerMultiplier);
        }
    }

    [Serializable]
    public sealed class MvpSkillLoadoutSettings
    {
        [Tooltip("First skill slot. Empty value means unused.")]
        public string Slot1SkillId = "skill.hero.power_strike";

        [Tooltip("Second skill slot. Empty value means unused.")]
        public string Slot2SkillId = "skill.hero.battle_focus";

        [Tooltip("Third skill slot. Empty value means unused.")]
        public string Slot3SkillId = string.Empty;

        [Tooltip("Fourth skill slot. Empty value means unused.")]
        public string Slot4SkillId = string.Empty;

        public static MvpSkillLoadoutSettings CreateHeroDefault()
        {
            return new MvpSkillLoadoutSettings();
        }

        public static MvpSkillLoadoutSettings CreateMonsterDefault()
        {
            return new MvpSkillLoadoutSettings
            {
                Slot1SkillId = "skill.monster.bite",
                Slot2SkillId = string.Empty,
                Slot3SkillId = string.Empty,
                Slot4SkillId = string.Empty
            };
        }

        public IReadOnlyList<SkillDefinition> ResolveSkills(IReadOnlyDictionary<string, SkillDefinition> _SkillsById)
        {
            List<SkillDefinition> skills = new List<SkillDefinition>(SkillLoadout.MaxSlots);
            string[] skillIds =
            {
                Slot1SkillId,
                Slot2SkillId,
                Slot3SkillId,
                Slot4SkillId
            };

            for (int i = 0; i < skillIds.Length; i++)
            {
                string skillId = skillIds[i];
                if (string.IsNullOrWhiteSpace(skillId))
                    continue;

                if (_SkillsById == null || !_SkillsById.TryGetValue(skillId, out SkillDefinition skill))
                    throw new KeyNotFoundException("Loadout references missing skill id: " + skillId);

                skills.Add(skill);
            }

            return skills;
        }
    }

    [Serializable]
    public sealed class MvpStatModifierSettings
    {
        [Header("Additive")]
        public float MaxHpAdd;
        public float AttackPowerAdd;
        public float DefenseAdd;
        public float AttackRangeAdd;
        public float AttackIntervalAdd;
        public float MoveSpeedAdd;
        public float CriticalChanceAdd;
        public float CriticalMultiplierAdd;

        [Header("Multiplier")]
        [Min(0f)] public float MaxHpMultiplier = 1f;
        [Min(0f)] public float AttackPowerMultiplier = 1f;
        [Min(0f)] public float DefenseMultiplier = 1f;
        [Min(0f)] public float AttackRangeMultiplier = 1f;
        [Min(0f)] public float AttackIntervalMultiplier = 1f;
        [Min(0f)] public float MoveSpeedMultiplier = 1f;
        [Min(0f)] public float CriticalChanceMultiplier = 1f;
        [Min(0f)] public float CriticalMultiplierMultiplier = 1f;

        public static MvpStatModifierSettings CreateNone()
        {
            return new MvpStatModifierSettings();
        }

        public static MvpStatModifierSettings CreateAttackPowerMultiplier(float _Multiplier)
        {
            return new MvpStatModifierSettings
            {
                AttackPowerMultiplier = _Multiplier
            };
        }

        public void EnsureDefaults()
        {
            MaxHpMultiplier = Mathf.Max(0f, MaxHpMultiplier);
            AttackPowerMultiplier = Mathf.Max(0f, AttackPowerMultiplier);
            DefenseMultiplier = Mathf.Max(0f, DefenseMultiplier);
            AttackRangeMultiplier = Mathf.Max(0f, AttackRangeMultiplier);
            AttackIntervalMultiplier = Mathf.Max(0f, AttackIntervalMultiplier);
            MoveSpeedMultiplier = Mathf.Max(0f, MoveSpeedMultiplier);
            CriticalChanceMultiplier = Mathf.Max(0f, CriticalChanceMultiplier);
            CriticalMultiplierMultiplier = Mathf.Max(0f, CriticalMultiplierMultiplier);
        }

        public StatModifier ToStatModifier()
        {
            EnsureDefaults();
            return new StatModifier(
                MaxHpAdd,
                AttackPowerAdd,
                DefenseAdd,
                AttackRangeAdd,
                AttackIntervalAdd,
                MoveSpeedAdd,
                CriticalChanceAdd,
                CriticalMultiplierAdd,
                MaxHpMultiplier,
                AttackPowerMultiplier,
                DefenseMultiplier,
                AttackRangeMultiplier,
                AttackIntervalMultiplier,
                MoveSpeedMultiplier,
                CriticalChanceMultiplier,
                CriticalMultiplierMultiplier);
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
