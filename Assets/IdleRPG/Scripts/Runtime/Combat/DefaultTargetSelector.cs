using System.Collections.Generic;
using IdleRPG.Domain;
using IdleRPG.Runtime.Actors;
using IdleRPG.Runtime.Configuration;
using UnityEngine;

namespace IdleRPG.Runtime.Combat
{
    public sealed class DefaultTargetSelector : ITargetSelector
    {
        public CombatActor SelectTarget(CombatActor _Requester, IReadOnlyList<CombatActor> _Candidates, MvpTargetingSettings _Settings)
        {
            if (_Requester == null || !_Requester.IsAlive || _Candidates == null)
                return null;

            MvpTargetingSettings settings = _Settings ?? new MvpTargetingSettings();
            settings.EnsureDefaults();

            CombatActor selected = null;
            float selectedScore = float.MaxValue;
            float selectedDistance = float.MaxValue;

            for (int i = 0; i < _Candidates.Count; i++)
            {
                CombatActor candidate = _Candidates[i];
                if (candidate == null || candidate.Team == _Requester.Team || !candidate.IsAlive)
                    continue;

                float distance = Vector2.Distance(_Requester.transform.position, candidate.transform.position);
                if (settings.LimitSearchRange && distance > settings.SearchRange)
                    continue;

                float score = GetScore(candidate, settings.SelectionMode, distance);
                if (score < selectedScore || (Mathf.Approximately(score, selectedScore) && distance < selectedDistance))
                {
                    selected = candidate;
                    selectedScore = score;
                    selectedDistance = distance;
                }
            }

            return selected;
        }

        private static float GetScore(CombatActor _Candidate, TargetSelectionMode _Mode, float _Distance)
        {
            switch (_Mode)
            {
                case TargetSelectionMode.LowestHp:
                    return _Candidate.Model != null ? _Candidate.Model.CurrentHp : float.MaxValue;
                case TargetSelectionMode.HighestAttack:
                    return _Candidate.Model != null ? -_Candidate.Model.Stats.AttackPower : float.MaxValue;
                default:
                    return _Distance;
            }
        }
    }
}
