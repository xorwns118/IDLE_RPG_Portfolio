namespace IdleRPG.Domain
{
    public enum ActorTeam
    {
        Player,
        Monster
    }

    public enum ActorState
    {
        Idle,
        Search,
        Move,
        Attack,
        Skill,
        Hit,
        Dead
    }

    public enum TileKind
    {
        Walkable,
        Blocked
    }

    public enum TileVisualKind
    {
        Ground,
        Wall,
        Tree,
        Water,
        Decoration
    }

    public enum TargetSelectionMode
    {
        Nearest,
        LowestHp,
        HighestAttack
    }

    public enum CombatLoopMode
    {
        Realtime,
        TurnBased
    }

    public enum StageFlowMode
    {
        Field,
        Battle
    }

    public enum EncounterTriggerMode
    {
        Manual,
        Distance
    }

    public enum MonsterSpawnSelectionMode
    {
        Sequential,
        Random
    }

    public enum SkillTargetType
    {
        Enemy,
        Self
    }

    public enum SkillEffectKind
    {
        Damage,
        Buff
    }
}
