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
}
