using System.Collections.Generic;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Configuration;

namespace IdleRPG.Runtime.Combat
{
    public interface ITargetSelector
    {
        CombatActor SelectTarget(CombatActor _Requester, IReadOnlyList<CombatActor> _Candidates, MvpTargetingSettings _Settings);
    }
}
